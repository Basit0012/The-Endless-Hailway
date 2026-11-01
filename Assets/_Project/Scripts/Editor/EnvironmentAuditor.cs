using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using EndlessHallway.Interaction;

namespace EndlessHallway.Editor
{
    public static class EnvironmentAuditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const float GridSize = 0.05f; // 5 cm
        private const float MaxGapThreshold = 0.20f; // 20 cm
        private const float MinGapThreshold = 0.002f; // 2 mm
        private const float CoplanarThreshold = 0.005f; // 5 mm

        public enum Severity
        {
            Critical,
            High,
            Medium,
            Low
        }

        public class AuditFinding
        {
            public Severity Severity;
            public string Category;
            public string ObjectName;
            public string Details;
            public Vector3 Coordinates;
        }

        [MenuItem("Tools/Environment/Audit", false, 1)]
        public static void AuditMenu()
        {
            string report = RunAudit();
            Debug.Log($"[EnvironmentAuditor] Audit complete. Report saved to EnvironmentAuditReport.txt.\n{report.Substring(0, Mathf.Min(1000, report.Length))}...");
        }

        public static string RunAudit()
        {
            // Ensure Main.unity is open
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var findings = new List<AuditFinding>();
            var sb = new StringBuilder();

            sb.AppendLine("================================================================================");
            sb.AppendLine("              THE ENDLESS HALLWAY — ENVIRONMENT AUDIT REPORT                    ");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Scene: {activeScene.path}");
            sb.AppendLine($"Target Grid Size: {GridSize} m (5 cm)");
            sb.AppendLine($"Gap Detection Threshold: {MinGapThreshold * 100f:F1} cm to {MaxGapThreshold * 100f:F1} cm");
            sb.AppendLine("================================================================================\n");

            // Gather environment renderers and objects
            var allRenderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => IsEnvironmentObject(r.gameObject))
                .ToList();

            var allGameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(go => IsEnvironmentObject(go))
                .ToList();

            // 1. Audit Non-(1,1,1) scale on environment meshes
            AuditScales(allRenderers, findings, sb);

            // 2. Audit Off-Grid Positions (0.05m grid)
            AuditGridPositions(allGameObjects, findings, sb);

            // 3. Audit Duplicate Objects
            AuditDuplicates(allRenderers, findings, sb);

            // 4. Audit Doors (Pivots, gaps, openings)
            AuditDoors(findings, sb);

            // 5. Audit Wall/Floor/Ceiling Gaps (within 0.2m but not touching)
            AuditGaps(allRenderers, findings, sb);

            // 6. Audit Overlapping Coplanar Surfaces (Z-Fighting)
            AuditCoplanarSurfaces(allRenderers, findings, sb);

            // 7. Audit Colliders (Missing & Inefficient MeshColliders)
            AuditColliders(allRenderers, findings, sb);

            // Severity Summary Table
            AppendSummaryTable(findings, sb);

            string reportContent = sb.ToString();

            // Write to project root
            string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "EnvironmentAuditReport.txt");
            File.WriteAllText(reportPath, reportContent);

            // Also write inside Assets for easy inspection in Unity Project window
            string assetsReportPath = Path.Combine(Application.dataPath, "EnvironmentAuditReport.txt");
            File.WriteAllText(assetsReportPath, reportContent);
            AssetDatabase.Refresh();

            return reportContent;
        }

        private static bool IsEnvironmentObject(GameObject go)
        {
            if (go == null) return false;
            string path = GetHierarchyPath(go);

            // Ignore non-environment systems
            if (path.StartsWith("Player") || path.Contains("/Player") || path.StartsWith("Canvas") ||
                path.StartsWith("Managers") || path.StartsWith("Directional") || path.StartsWith("Lighting") ||
                path.StartsWith("EventSystem"))
            {
                return false;
            }

            // Exclude camera, light icons, etc.
            if (go.GetComponent<Camera>() != null || go.GetComponent<AudioListener>() != null)
                return false;

            return true;
        }

        private static string GetHierarchyPath(GameObject go)
        {
            if (go == null) return string.Empty;
            string path = go.name;
            Transform current = go.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        // ==============================================================================================
        // CHECK 1: Non-(1,1,1) Scale
        // ==============================================================================================
        private static void AuditScales(List<MeshRenderer> renderers, List<AuditFinding> findings, StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("1. NON-(1,1,1) SCALE ON ENVIRONMENT MESHES");
            sb.AppendLine("--------------------------------------------------------------------------------");

            int nonUnitPrimitives = 0;
            int nonUnitImportedMeshes = 0;

            foreach (var r in renderers)
            {
                Transform t = r.transform;
                Vector3 s = t.localScale;
                bool isNonUnit = Mathf.Abs(s.x - 1f) > 0.001f || Mathf.Abs(s.y - 1f) > 0.001f || Mathf.Abs(s.z - 1f) > 0.001f;

                if (!isNonUnit) continue;

                var mf = r.GetComponent<MeshFilter>();
                bool isPrimitive = mf != null && mf.sharedMesh != null && (
                    mf.sharedMesh.name.Contains("Cube") ||
                    mf.sharedMesh.name.Contains("Quad") ||
                    mf.sharedMesh.name.Contains("Plane") ||
                    mf.sharedMesh.name.Contains("Cylinder") ||
                    mf.sharedMesh.name.Contains("Sphere"));

                if (isPrimitive)
                {
                    nonUnitPrimitives++;
                }
                else
                {
                    nonUnitImportedMeshes++;
                    string path = GetHierarchyPath(r.gameObject);
                    findings.Add(new AuditFinding
                    {
                        Severity = Severity.Low,
                        Category = "Non-Unit Scale (Imported Mesh/Prefab)",
                        ObjectName = path,
                        Details = $"Mesh: {mf?.sharedMesh?.name ?? "null"}, LocalScale: ({s.x:F3}, {s.y:F3}, {s.z:F3}), LossyScale: ({t.lossyScale.x:F3}, {t.lossyScale.y:F3}, {t.lossyScale.z:F3})",
                        Coordinates = t.position
                    });
                }
            }

            sb.AppendLine($"Total Environment Renderers: {renderers.Count}");
            sb.AppendLine($"Primitive Meshes with Non-(1,1,1) Scale (Used as Procedural Boxes/Planes): {nonUnitPrimitives}");
            sb.AppendLine($"Imported/Non-Primitive Meshes with Non-(1,1,1) Scale: {nonUnitImportedMeshes}");

            var nonUnitList = findings.Where(f => f.Category.StartsWith("Non-Unit Scale")).ToList();
            if (nonUnitList.Count > 0)
            {
                sb.AppendLine("\nImported Meshes with Non-(1,1,1) Scale (UV / attachment distortion risk):");
                foreach (var f in nonUnitList)
                {
                    sb.AppendLine($"  - [{f.ObjectName}] at ({f.Coordinates.x:F2}, {f.Coordinates.y:F2}, {f.Coordinates.z:F2}) -> {f.Details}");
                }
            }
            sb.AppendLine();
        }

        // ==============================================================================================
        // CHECK 2: Off 0.05m Grid Positions
        // ==============================================================================================
        private static void AuditGridPositions(List<GameObject> objects, List<AuditFinding> findings, StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("2. OBJECTS OFF 0.05 M GRID");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var offGridList = new List<AuditFinding>();

            foreach (var go in objects)
            {
                // Only evaluate objects that have renderers or colliders (physical geometry)
                if (go.GetComponent<Renderer>() == null && go.GetComponent<Collider>() == null) continue;

                Vector3 pos = go.transform.position;
                float snapX = Mathf.Round(pos.x / GridSize) * GridSize;
                float snapY = Mathf.Round(pos.y / GridSize) * GridSize;
                float snapZ = Mathf.Round(pos.z / GridSize) * GridSize;

                float dx = Mathf.Abs(pos.x - snapX);
                float dy = Mathf.Abs(pos.y - snapY);
                float dz = Mathf.Abs(pos.z - snapZ);

                if (dx > 0.001f || dy > 0.001f || dz > 0.001f)
                {
                    float maxDev = Mathf.Max(dx, Mathf.Max(dy, dz));
                    Severity sev = maxDev >= 0.01f ? Severity.Medium : Severity.Low;
                    string path = GetHierarchyPath(go);

                    var finding = new AuditFinding
                    {
                        Severity = sev,
                        Category = "Off-Grid Position",
                        ObjectName = path,
                        Details = $"Pos: ({pos.x:F4}, {pos.y:F4}, {pos.z:F4}) -> Snap: ({snapX:F2}, {snapY:F2}, {snapZ:F2}), MaxDev: {maxDev * 100f:F2} cm",
                        Coordinates = pos
                    };
                    offGridList.Add(finding);
                    findings.Add(finding);
                }
            }

            sb.AppendLine($"Total Physical Objects Checked: {objects.Count(go => go.GetComponent<Renderer>() != null || go.GetComponent<Collider>() != null)}");
            sb.AppendLine($"Objects Off-Grid (> 1mm): {offGridList.Count} ({offGridList.Count(f => f.Severity == Severity.Medium)} with dev >= 1 cm)");

            sb.AppendLine("\nSample Off-Grid Environment Objects (Max Deviation >= 1.0 cm):");
            foreach (var f in offGridList.Where(f => f.Severity == Severity.Medium).Take(30))
            {
                sb.AppendLine($"  - [{f.ObjectName}] -> {f.Details}");
            }
            if (offGridList.Count(f => f.Severity == Severity.Medium) > 30)
            {
                sb.AppendLine($"  ... and {offGridList.Count(f => f.Severity == Severity.Medium) - 30} more off-grid objects.");
            }
            sb.AppendLine();
        }

        // ==============================================================================================
        // CHECK 3: Duplicate Objects
        // ==============================================================================================
        private static void AuditDuplicates(List<MeshRenderer> renderers, List<AuditFinding> findings, StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("3. DUPLICATE OBJECTS (IDENTICAL POSITION, ROTATION, MESH)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var duplicates = new List<AuditFinding>();
            var checkedPairs = new HashSet<string>();

            for (int i = 0; i < renderers.Count; i++)
            {
                var r1 = renderers[i];
                if (r1 == null) continue;
                var t1 = r1.transform;
                var mf1 = r1.GetComponent<MeshFilter>();
                string m1 = mf1 != null && mf1.sharedMesh != null ? mf1.sharedMesh.name : "null";

                for (int j = i + 1; j < renderers.Count; j++)
                {
                    var r2 = renderers[j];
                    if (r2 == null) continue;
                    var t2 = r2.transform;
                    var mf2 = r2.GetComponent<MeshFilter>();
                    string m2 = mf2 != null && mf2.sharedMesh != null ? mf2.sharedMesh.name : "null";

                    if (m1 != m2) continue;

                    float posDist = Vector3.Distance(t1.position, t2.position);
                    float rotAngle = Quaternion.Angle(t1.rotation, t2.rotation);
                    float scaleDist = Vector3.Distance(t1.lossyScale, t2.lossyScale);

                    if (posDist < 0.005f && rotAngle < 0.5f && scaleDist < 0.005f)
                    {
                        string path1 = GetHierarchyPath(r1.gameObject);
                        string path2 = GetHierarchyPath(r2.gameObject);
                        string pairKey = $"{r1.GetInstanceID()}_{r2.GetInstanceID()}";

                        if (!checkedPairs.Contains(pairKey))
                        {
                            checkedPairs.Add(pairKey);
                            var finding = new AuditFinding
                            {
                                Severity = Severity.Critical,
                                Category = "Duplicate Object",
                                ObjectName = $"{path1} <=> {path2}",
                                Details = $"Mesh: {m1}, Pos: ({t1.position.x:F3}, {t1.position.y:F3}, {t1.position.z:F3}), Scale: ({t1.lossyScale.x:F3}, {t1.lossyScale.y:F3}, {t1.lossyScale.z:F3})",
                                Coordinates = t1.position
                            };
                            duplicates.Add(finding);
                            findings.Add(finding);
                        }
                    }
                }
            }

            // Check specific known duplicate containers under StairwellLanding
            var landing = GameObject.Find("StairwellLanding");
            if (landing != null)
            {
                var floorGrounds = landing.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "Floor_Ground").ToList();
                if (floorGrounds.Count > 1)
                {
                    sb.AppendLine($"[CRITICAL] StairwellLanding contains {floorGrounds.Count} objects named 'Floor_Ground'!");
                }

                var lowerWalls = landing.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "Lower_Walls").ToList();
                if (lowerWalls.Count > 1)
                {
                    sb.AppendLine($"[CRITICAL] StairwellLanding contains {lowerWalls.Count} objects named 'Lower_Walls'!");
                }
            }

            sb.AppendLine($"Identical Duplicate Renderer Pairs Found: {duplicates.Count}");
            foreach (var d in duplicates)
            {
                sb.AppendLine($"  - [CRITICAL DUPLICATE] {d.ObjectName}");
                sb.AppendLine($"    -> {d.Details}");
            }
            sb.AppendLine();
        }

        // ==============================================================================================
        // CHECK 4: Door Prefabs, Pivots, Openings, and Gaps
        // ==============================================================================================
        private static void AuditDoors(List<AuditFinding> findings, StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("4. DOOR AUDIT (PIVOT LOCATION, HINGES, DOOR VS FRAME OPENING, GAPS)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var doorControllers = UnityEngine.Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var elevator = UnityEngine.Object.FindAnyObjectByType<ElevatorController>(FindObjectsInactive.Include);

            sb.AppendLine($"Found {doorControllers.Length} Swing Doors (DoorController) and {(elevator != null ? 1 : 0)} Elevator System.\n");

            // 1. Audit Swing Doors
            foreach (var dc in doorControllers)
            {
                GameObject doorObj = dc.gameObject;
                string doorId = dc.DoorId;

                // Find door slab
                var slab = doorObj.transform.Find("Pivot/DoorSlab") ?? doorObj.transform.Find("DoorSlab") ?? doorObj.transform;
                var slabRenderer = slab.GetComponent<Renderer>() ?? doorObj.GetComponentInChildren<Renderer>();
                var slabCollider = slab.GetComponent<BoxCollider>();

                // Find pivot
                Transform pivot = null;
                var pivotField = typeof(DoorController).GetField("pivotTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pivotField != null) pivot = pivotField.GetValue(dc) as Transform;
                if (pivot == null) pivot = doorObj.transform.Find("Pivot") ?? doorObj.transform;

                // Find frame
                Transform frame = doorObj.transform.Find("Frame");
                var frameRenderer = frame != null ? frame.GetComponent<Renderer>() : null;

                sb.AppendLine($"--- Swing Door: {doorId} ({doorObj.name}) ---");
                sb.AppendLine($"  World Position: ({doorObj.transform.position.x:F3}, {doorObj.transform.position.y:F3}, {doorObj.transform.position.z:F3})");

                if (slabRenderer != null)
                {
                    Bounds slabBounds = slabRenderer.bounds;
                    Vector3 slabSize = slabBounds.size;
                    Vector3 pivotPos = pivot.position;

                    // Hinge check: is the pivot near the edge of the door slab?
                    // In the corridor, door width extends along Z axis (slabSize.z ~ 0.9m)
                    float distToMinZ = Mathf.Abs(pivotPos.z - slabBounds.min.z);
                    float distToMaxZ = Mathf.Abs(pivotPos.z - slabBounds.max.z);
                    float distToCenterZ = Mathf.Abs(pivotPos.z - slabBounds.center.z);

                    bool isPivotAtHinge = (distToMinZ < 0.1f || distToMaxZ < 0.1f) && distToCenterZ > 0.2f;

                    sb.AppendLine($"  Door Slab Dimensions: Width={slabSize.x:F3}m, Height={slabSize.y:F3}m, Length(Span)={slabSize.z:F3}m");
                    sb.AppendLine($"  Pivot World Pos: ({pivotPos.x:F3}, {pivotPos.y:F3}, {pivotPos.z:F3})");
                    sb.AppendLine($"  Pivot Relative to Slab: DistToEdge={Mathf.Min(distToMinZ, distToMaxZ):F3}m, DistToCenter={distToCenterZ:F3}m");
                    sb.AppendLine($"  Pivot at Hinge Edge: {(isPivotAtHinge ? "YES (Correct)" : "NO (Wrong Pivot - Will rotate from center/offset!)")}");

                    if (!isPivotAtHinge)
                    {
                        findings.Add(new AuditFinding
                        {
                            Severity = Severity.High,
                            Category = "Door Pivot Not At Hinge",
                            ObjectName = doorId,
                            Details = $"Pivot at ({pivotPos.x:F2}, {pivotPos.y:F2}, {pivotPos.z:F2}) is {distToCenterZ:F2}m from center instead of edge.",
                            Coordinates = pivotPos
                        });
                    }

                    if (frameRenderer != null)
                    {
                        Bounds frameBounds = frameRenderer.bounds;
                        Vector3 frameSize = frameBounds.size;
                        sb.AppendLine($"  Frame Dimensions: Width={frameSize.x:F3}m, Height={frameSize.y:F3}m, Length(Opening)={frameSize.z:F3}m");

                        // Check if frame is a solid primitive cube
                        var mf = frame.GetComponent<MeshFilter>();
                        bool isSolidBox = mf != null && mf.sharedMesh != null && mf.sharedMesh.name.Contains("Cube");
                        var frameCol = frame.GetComponent<Collider>();
                        bool frameHasSolidCollider = frameCol != null && !frameCol.isTrigger;

                        if (isSolidBox && frameHasSolidCollider)
                        {
                            sb.AppendLine("  [CRITICAL/HIGH] Frame is a SOLID CUBE spanning the entire opening with an active collider!");
                            sb.AppendLine("                  When the door opens, the player cannot walk through because the frame block obstructs the doorway.");
                            findings.Add(new AuditFinding
                            {
                                Severity = Severity.High,
                                Category = "Solid Door Frame Obstruction",
                                ObjectName = $"{doorId}/Frame",
                                Details = $"Frame is a solid primitive cube ({frameSize.x:F2}x{frameSize.y:F2}x{frameSize.z:F2}m) with active collider blocking the doorway.",
                                Coordinates = frame.position
                            });
                        }

                        // Gap calculation
                        float spanGap = frameSize.z - slabSize.z; // Overall opening clearance
                        float heightGap = frameSize.y - slabSize.y;
                        sb.AppendLine($"  Clearance: Span Gap = {spanGap * 100f:F1} cm, Height Gap = {heightGap * 100f:F1} cm");

                        if (spanGap < 0.01f)
                        {
                            sb.AppendLine($"  [WARNING] Door slab overlaps/penetrates frame (Negative or sub-cm gap: {spanGap * 100f:F1} cm)!");
                            findings.Add(new AuditFinding
                            {
                                Severity = Severity.Medium,
                                Category = "Door Frame Penetration",
                                ObjectName = doorId,
                                Details = $"Door slab overlaps frame. Clearance: {spanGap * 100f:F1} cm (Target: 2-3 cm).",
                                Coordinates = doorObj.transform.position
                            });
                        }
                        else if (spanGap > 0.06f)
                        {
                            sb.AppendLine($"  [WARNING] Door gap too large ({spanGap * 100f:F1} cm > 3 cm standard)!");
                            findings.Add(new AuditFinding
                            {
                                Severity = Severity.Medium,
                                Category = "Door Gap Too Large",
                                ObjectName = doorId,
                                Details = $"Door gap {spanGap * 100f:F1} cm exceeds 2-3 cm standard.",
                                Coordinates = doorObj.transform.position
                            });
                        }
                    }
                }
                sb.AppendLine();
            }

            // 2. Audit Elevator Doors
            if (elevator != null)
            {
                sb.AppendLine("--- Elevator Doors (ElevatorController) ---");
                var elevObj = elevator.gameObject;
                var leftDoor = elevObj.transform.Find("ElevatorDoor_Left");
                var rightDoor = elevObj.transform.Find("ElevatorDoor_Right");

                if (leftDoor != null && rightDoor != null)
                {
                    var lr = leftDoor.GetComponent<Renderer>();
                    var rr = rightDoor.GetComponent<Renderer>();

                    if (lr != null && rr != null)
                    {
                        Bounds lb = lr.bounds;
                        Bounds rb = rr.bounds;

                        sb.AppendLine($"  Left Door Bounds: Center=({lb.center.x:F3}, {lb.center.y:F3}, {lb.center.z:F3}), Size=({lb.size.x:F3}, {lb.size.y:F3}, {lb.size.z:F3})");
                        sb.AppendLine($"  Right Door Bounds: Center=({rb.center.x:F3}, {rb.center.y:F3}, {rb.center.z:F3}), Size=({rb.size.x:F3}, {rb.size.y:F3}, {rb.size.z:F3})");

                        // Check meeting seam gap at X = 0
                        float centerSeamGap = rb.min.x - lb.max.x;
                        sb.AppendLine($"  Meeting Seam Gap (X-axis): {centerSeamGap * 100f:F2} cm");

                        if (centerSeamGap > 0.01f)
                        {
                            findings.Add(new AuditFinding
                            {
                                Severity = Severity.High,
                                Category = "Elevator Door Gap",
                                ObjectName = "ElevatorDoors",
                                Details = $"Doors do not meet flush when closed. Center seam gap: {centerSeamGap * 100f:F2} cm.",
                                Coordinates = elevObj.transform.position
                            });
                        }
                        else if (centerSeamGap < -0.01f)
                        {
                            findings.Add(new AuditFinding
                            {
                                Severity = Severity.Medium,
                                Category = "Elevator Door Overlap",
                                ObjectName = "ElevatorDoors",
                                Details = $"Doors overlap at center by {-centerSeamGap * 100f:F2} cm.",
                                Coordinates = elevObj.transform.position
                            });
                        }
                    }
                }
                sb.AppendLine();
            }
        }

        // ==============================================================================================
        // CHECK 5: Wall, Floor, Ceiling Gaps (Bounds within 0.2m but not touching)
        // ==============================================================================================
        private static void AuditGaps(List<MeshRenderer> renderers, List<AuditFinding> findings, StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("5. PROBABLE GAPS (BOUNDS WITHIN 0.2 M BUT NOT TOUCHING/OVERLAPPING)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var structuralRenderers = renderers.Where(r => {
                string name = r.name.ToLower();
                return name.Contains("wall") || name.Contains("floor") || name.Contains("ceiling") ||
                       name.Contains("lintel") || name.Contains("step") || name.Contains("trim");
            }).ToList();

            var gapFindings = new List<AuditFinding>();

            for (int i = 0; i < structuralRenderers.Count; i++)
            {
                var r1 = structuralRenderers[i];
                Bounds b1 = r1.bounds;

                for (int j = i + 1; j < structuralRenderers.Count; j++)
                {
                    var r2 = structuralRenderers[j];
                    Bounds b2 = r2.bounds;

                    // Calculate axis gaps
                    float gapX = Mathf.Max(0f, Mathf.Max(b1.min.x - b2.max.x, b2.min.x - b1.max.x));
                    float gapY = Mathf.Max(0f, Mathf.Max(b1.min.y - b2.max.y, b2.min.y - b1.max.y));
                    float gapZ = Mathf.Max(0f, Mathf.Max(b1.min.z - b2.max.z, b2.min.z - b1.max.z));

                    // If already overlapping on all 3 axes, distance is 0 (touching/overlapping)
                    if (gapX == 0f && gapY == 0f && gapZ == 0f) continue;

                    float minGap = Mathf.Sqrt(gapX * gapX + gapY * gapY + gapZ * gapZ);

                    // Check if within 0.2m threshold and above sub-millimeter touching threshold
                    if (minGap > MinGapThreshold && minGap <= MaxGapThreshold)
                    {
                        // Check if they are collinear / adjacent along one primary axis while overlapping on others
                        // e.g., two wall segments adjacent along Z with gapZ > 0 and gapX == 0, gapY == 0
                        int zeroGaps = (gapX == 0f ? 1 : 0) + (gapY == 0f ? 1 : 0) + (gapZ == 0f ? 1 : 0);

                        // Only consider meaningful structural gaps
                        if (zeroGaps >= 1)
                        {
                            string path1 = GetHierarchyPath(r1.gameObject);
                            string path2 = GetHierarchyPath(r2.gameObject);

                            Severity sev = minGap >= 0.02f ? Severity.High : Severity.Medium;
                            string gapAxis = gapX > 0f ? $"X={gapX * 100f:F1}cm" : (gapY > 0f ? $"Y={gapY * 100f:F1}cm" : $"Z={gapZ * 100f:F1}cm");

                            var finding = new AuditFinding
                            {
                                Severity = sev,
                                Category = "Structural Seam / Gap",
                                ObjectName = $"{path1} <=> {path2}",
                                Details = $"Gap: {minGap * 100f:F2} cm ({gapAxis}). B1: ({b1.center.x:F2}, {b1.center.y:F2}, {b1.center.z:F2}) - B2: ({b2.center.x:F2}, {b2.center.y:F2}, {b2.center.z:F2})",
                                Coordinates = (b1.center + b2.center) * 0.5f
                            };
                            gapFindings.Add(finding);
                            findings.Add(finding);
                        }
                    }
                }
            }

            sb.AppendLine($"Total Probable Structural Gaps Found: {gapFindings.Count} ({gapFindings.Count(g => g.Severity == Severity.High)} gaps >= 2 cm)");

            sb.AppendLine("\nHigh-Severity Gaps (>= 2 cm):");
            foreach (var g in gapFindings.Where(g => g.Severity == Severity.High).Take(25))
            {
                sb.AppendLine($"  - [{g.ObjectName}]");
                sb.AppendLine($"    -> {g.Details}");
            }
            if (gapFindings.Count(g => g.Severity == Severity.High) > 25)
            {
                sb.AppendLine($"  ... and {gapFindings.Count(g => g.Severity == Severity.High) - 25} more high-severity gaps.");
            }
            sb.AppendLine();
        }

        // ==============================================================================================
        // CHECK 6: Overlapping Coplanar Surfaces (Z-Fighting Risk)
        // ==============================================================================================
        private static void AuditCoplanarSurfaces(List<MeshRenderer> renderers, List<AuditFinding> findings, StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("6. OVERLAPPING COPLANAR SURFACES (Z-FIGHTING RISK)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var zFightFindings = new List<AuditFinding>();

            for (int i = 0; i < renderers.Count; i++)
            {
                var r1 = renderers[i];
                Bounds b1 = r1.bounds;

                for (int j = i + 1; j < renderers.Count; j++)
                {
                    var r2 = renderers[j];
                    Bounds b2 = r2.bounds;

                    // Compute overlap volume
                    float ox = Mathf.Min(b1.max.x, b2.max.x) - Mathf.Max(b1.min.x, b2.min.x);
                    float oy = Mathf.Min(b1.max.y, b2.max.y) - Mathf.Max(b1.min.y, b2.min.y);
                    float oz = Mathf.Min(b1.max.z, b2.max.z) - Mathf.Max(b1.min.z, b2.min.z);

                    // Check coplanar conditions (shared surface plane within threshold)
                    bool coplanarY = (Mathf.Abs(b1.min.y - b2.min.y) < CoplanarThreshold || Mathf.Abs(b1.max.y - b2.max.y) < CoplanarThreshold) && ox > 0.05f && oz > 0.05f;
                    bool coplanarX = (Mathf.Abs(b1.min.x - b2.min.x) < CoplanarThreshold || Mathf.Abs(b1.max.x - b2.max.x) < CoplanarThreshold) && oy > 0.05f && oz > 0.05f;
                    bool coplanarZ = (Mathf.Abs(b1.min.z - b2.min.z) < CoplanarThreshold || Mathf.Abs(b1.max.z - b2.max.z) < CoplanarThreshold) && ox > 0.05f && oy > 0.05f;

                    if (coplanarY || coplanarX || coplanarZ)
                    {
                        float area = coplanarY ? ox * oz : (coplanarX ? oy * oz : ox * oy);
                        string planeDesc = coplanarY ? $"Horizontal Y-plane (Area={area:F2} m²)" :
                                          (coplanarX ? $"Vertical X-plane (Area={area:F2} m²)" : $"Vertical Z-plane (Area={area:F2} m²)");

                        Severity sev = area >= 1.0f ? Severity.High : Severity.Medium;
                        string path1 = GetHierarchyPath(r1.gameObject);
                        string path2 = GetHierarchyPath(r2.gameObject);

                        var finding = new AuditFinding
                        {
                            Severity = sev,
                            Category = "Coplanar Z-Fighting",
                            ObjectName = $"{path1} <=> {path2}",
                            Details = $"Co-planar surfaces on {planeDesc}. Shared plane coordinates near ({(b1.center.x + b2.center.x) * 0.5f:F2}, {(b1.center.y + b2.center.y) * 0.5f:F2}, {(b1.center.z + b2.center.z) * 0.5f:F2})",
                            Coordinates = (b1.center + b2.center) * 0.5f
                        };
                        zFightFindings.Add(finding);
                        findings.Add(finding);
                    }
                }
            }

            sb.AppendLine($"Total Coplanar Overlapping Surface Pairs: {zFightFindings.Count} ({zFightFindings.Count(z => z.Severity == Severity.High)} large area >= 1.0 m²)");

            sb.AppendLine("\nSample Coplanar Overlaps (Z-Fighting Risk):");
            foreach (var z in zFightFindings.OrderByDescending(f => f.Severity).Take(20))
            {
                sb.AppendLine($"  - [{z.Severity}] {z.ObjectName}");
                sb.AppendLine($"    -> {z.Details}");
            }
            if (zFightFindings.Count > 20)
            {
                sb.AppendLine($"  ... and {zFightFindings.Count - 20} more coplanar overlapping pairs.");
            }
            sb.AppendLine();
        }

        // ==============================================================================================
        // CHECK 7: Colliders (Missing & Inefficient MeshColliders)
        // ==============================================================================================
        private static void AuditColliders(List<MeshRenderer> renderers, List<AuditFinding> findings, StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("7. COLLIDER AUDIT (MISSING COLLIDERS & INEFFICIENT MESHCOLLIDERS)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            int missingColliders = 0;
            int meshColliders = 0;

            foreach (var r in renderers)
            {
                var go = r.gameObject;
                string name = go.name.ToLower();

                // Check if collider exists on self or parent/child
                var col = go.GetComponent<Collider>() ?? go.GetComponentInParent<Collider>() ?? go.GetComponentInChildren<Collider>();

                // Check for missing colliders on structural objects
                bool isStructural = name.Contains("floor") || name.Contains("wall") || name.Contains("step") ||
                                    name.Contains("stairs") || name.Contains("door") || name.Contains("couch") ||
                                    name.Contains("table") || name.Contains("shelving");

                if (col == null && isStructural)
                {
                    missingColliders++;
                    Severity sev = name.Contains("floor") || name.Contains("step") ? Severity.Critical : Severity.High;
                    string path = GetHierarchyPath(go);

                    findings.Add(new AuditFinding
                    {
                        Severity = sev,
                        Category = "Missing Structural Collider",
                        ObjectName = path,
                        Details = $"Object is marked/named as solid structure ({go.name}) but has NO collider on itself or hierarchy.",
                        Coordinates = go.transform.position
                    });
                }

                // Check for MeshColliders on simple static objects
                var mc = go.GetComponent<MeshCollider>();
                if (mc != null && mc.sharedMesh != null)
                {
                    meshColliders++;
                    int triCount = mc.sharedMesh.triangles.Length / 3;
                    bool isBoxLike = name.Contains("wall") || name.Contains("floor") || name.Contains("ceiling") ||
                                     name.Contains("cube") || mc.sharedMesh.name.Contains("Cube");

                    if (isBoxLike)
                    {
                        string path = GetHierarchyPath(go);
                        findings.Add(new AuditFinding
                        {
                            Severity = Severity.Medium,
                            Category = "Inefficient MeshCollider",
                            ObjectName = path,
                            Details = $"Uses MeshCollider ({triCount} tris, mesh='{mc.sharedMesh.name}') on box-like geometry. Should be BoxCollider.",
                            Coordinates = go.transform.position
                        });
                    }
                }
            }

            sb.AppendLine($"Structural Objects Missing Colliders: {missingColliders}");
            sb.AppendLine($"Static Meshes with MeshCollider: {meshColliders}");

            var missingList = findings.Where(f => f.Category == "Missing Structural Collider").ToList();
            if (missingList.Count > 0)
            {
                sb.AppendLine("\nMissing Structural Colliders:");
                foreach (var m in missingList)
                {
                    sb.AppendLine($"  - [{m.Severity}] {m.ObjectName} at ({m.Coordinates.x:F2}, {m.Coordinates.y:F2}, {m.Coordinates.z:F2})");
                }
            }
            sb.AppendLine();
        }

        // ==============================================================================================
        // SEVERITY SUMMARY TABLE
        // ==============================================================================================
        private static void AppendSummaryTable(List<AuditFinding> findings, StringBuilder sb)
        {
            sb.AppendLine("================================================================================");
            sb.AppendLine("                         SEVERITY SUMMARY TABLE                                 ");
            sb.AppendLine("================================================================================");

            int critCount = findings.Count(f => f.Severity == Severity.Critical);
            int highCount = findings.Count(f => f.Severity == Severity.High);
            int medCount = findings.Count(f => f.Severity == Severity.Medium);
            int lowCount = findings.Count(f => f.Severity == Severity.Low);

            sb.AppendLine($"Total Issues Found: {findings.Count}");
            sb.AppendLine($"  - CRITICAL: {critCount}");
            sb.AppendLine($"  - HIGH:     {highCount}");
            sb.AppendLine($"  - MEDIUM:   {medCount}");
            sb.AppendLine($"  - LOW:      {lowCount}");
            sb.AppendLine();

            sb.AppendLine("| Severity | Category | Count | Primary Impact & Recommendation |");
            sb.AppendLine("|----------|----------|-------|---------------------------------|");

            var categories = findings.GroupBy(f => new { f.Severity, f.Category })
                                     .OrderBy(g => g.Key.Severity)
                                     .ThenByDescending(g => g.Count());

            foreach (var group in categories)
            {
                string desc = GetCategoryDescription(group.Key.Category);
                sb.AppendLine($"| {group.Key.Severity,-8} | {group.Key.Category,-28} | {group.Count(),-5} | {desc,-31} |");
            }

            sb.AppendLine("================================================================================");
        }

        private static string GetCategoryDescription(string category)
        {
            switch (category)
            {
                case "Duplicate Object":
                    return "Double geometry, z-fighting & duplicate lightmaps. Delete duplicates.";
                case "Missing Structural Collider":
                    return "Player falls through world or walks through walls. Add BoxCollider.";
                case "Door Pivot Not At Hinge":
                    return "Door rotates from center or wrong side. Align pivot to hinge edge.";
                case "Solid Door Frame Obstruction":
                    return "Doorway blocked by solid cube frame. Carve open frame or remove.";
                case "Structural Seam / Gap":
                    return "Visible black seams/holes into the void. Snap objects or add trim.";
                case "Coplanar Z-Fighting":
                    return "Flickering textures on overlapping surfaces. Offset or merge slabs.";
                case "Elevator Door Gap":
                    return "Elevator doors don't close flush. Adjust closed position.";
                case "Door Frame Penetration":
                    return "Door leaf intersects frame. Adjust door/frame dimensions to 2-3cm gap.";
                case "Door Gap Too Large":
                    return "Excessive clearance > 3cm. Resize door/frame to 2-3cm gap.";
                case "Off-Grid Position":
                    return "Misalignment across modular pieces. Snap positions to 0.05m grid.";
                case "Inefficient MeshCollider":
                    return "Physics performance overhead. Replace MeshCollider with BoxCollider.";
                case "Non-Unit Scale (Imported Mesh/Prefab)":
                    return "Distorted UVs / normals. Bake transform or use unit scale prefab.";
                default:
                    return "Review and resolve per phase standard.";
            }
        }
    }
}

