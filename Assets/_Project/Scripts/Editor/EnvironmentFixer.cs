using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using EndlessHallway.Interaction;
using EndlessHallway.Anomaly;
using Unity.AI.Navigation;

namespace EndlessHallway.Editor
{
    public static class EnvironmentFixer
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("Tools/Environment/Fix Approved Standard", false, 2)]
        public static void ExecuteFixesMenu()
        {
            string log = ExecuteFixes();
            Debug.Log($"[EnvironmentFixer] Environment fixes complete!\n{log.Substring(0, Mathf.Min(1200, log.Length))}...");
        }

        [MenuItem("Tools/Environment/Validate NavMesh Bake", false, 3)]
        public static void ValidateNavMeshBakeMenu()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var testGo = new GameObject("Test_NavMeshBake");
            try
            {
                var surface = testGo.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
                surface.BuildNavMesh();
                if (surface.navMeshData != null)
                {
                    Debug.Log($"[NavMesh Validation] SUCCESS! NavMeshData baked cleanly. Source bounds: {surface.navMeshData.sourceBounds}");
                }
                else
                {
                    Debug.LogError("[NavMesh Validation] FAILED: navMeshData is null.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testGo);
            }
        }

        public static string ExecuteFixes()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("              THE ENDLESS HALLWAY — ENVIRONMENT CHANGE LOG                      ");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Executed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Scene: {activeScene.path}");
            sb.AppendLine("================================================================================\n");

            // Load standard materials
            var trimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Trim.mat");
            var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Wall.mat");
            var metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door_Metal.mat");
            var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Staircase_Wood.mat");
            var doorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door.mat");
            var door214Mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door_214.mat");

            // 1. Remove Duplicate Objects
            RemoveDuplicates(sb);

            // 2. Standardize Corridor Wall Segments & Openings
            StandardizeCorridorWalls(wallMat, sb);

            // 3. Fix Swing Doors (Remove solid blocker frames, build open 3-piece archway, resize door leaf to 2.5cm gap)
            FixSwingDoors(trimMat, doorMat, door214Mat, woodMat, sb);

            // 4. Close Gaps with Threshold Plates (Elevator 10cm gap, Room 214/212/216 5cm gaps)
            AddThresholdPlates(metalMat, woodMat, sb);

            // 5. Fix Coplanar Z-fighting on Decals / Overlays
            FixCoplanarOverlays(sb);

            // 6. Mark Static Flags on Environment Geometry
            ApplyStaticFlags(sb);

            // Save Scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            string logContent = sb.ToString();

            // Write change logs
            string logPath = Path.Combine(Directory.GetCurrentDirectory(), "EnvironmentChangeLog.txt");
            File.WriteAllText(logPath, logContent);

            string assetsLogPath = Path.Combine(Application.dataPath, "EnvironmentChangeLog.txt");
            File.WriteAllText(assetsLogPath, logContent);
            AssetDatabase.Refresh();

            return logContent;
        }

        // ==============================================================================================
        // STEP 1: Remove Duplicate Objects
        // ==============================================================================================
        private static void RemoveDuplicates(StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("STEP 1: REMOVING APPROVED DUPLICATES");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var landing = GameObject.Find("StairwellLanding");
            if (landing != null)
            {
                // Remove duplicate Floor_Ground
                var floorGrounds = landing.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "Floor_Ground").ToList();

                if (floorGrounds.Count > 1)
                {
                    sb.AppendLine($"Found {floorGrounds.Count} Floor_Ground objects in StairwellLanding. Keeping 1, deleting {floorGrounds.Count - 1} duplicates:");
                    for (int i = 1; i < floorGrounds.Count; i++)
                    {
                        var dup = floorGrounds[i];
                        sb.AppendLine($"  - [DELETE DUPLICATE] {GetPath(dup.gameObject)} (ID: {dup.gameObject.GetInstanceID()})");
                        UnityEngine.Object.DestroyImmediate(dup.gameObject);
                    }
                }

                // Remove duplicate Lower_Walls trees
                var lowerWallsList = landing.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name == "Lower_Walls").ToList();

                if (lowerWallsList.Count > 1)
                {
                    sb.AppendLine($"Found {lowerWallsList.Count} Lower_Walls hierarchies in StairwellLanding. Keeping 1, deleting {lowerWallsList.Count - 1} duplicate trees:");
                    for (int i = 1; i < lowerWallsList.Count; i++)
                    {
                        var dupTree = lowerWallsList[i];
                        sb.AppendLine($"  - [DELETE DUPLICATE] {GetPath(dupTree.gameObject)} (ID: {dupTree.gameObject.GetInstanceID()})");
                        UnityEngine.Object.DestroyImmediate(dupTree.gameObject);
                    }
                }
            }

            // Clean redundant duplicate submesh renderers in Elevator_USSR_Entrance FBX instance
            var elevFbxRoot = GameObject.Find("Environment/Elevator/Elevator_USSR_Entrance/5d84ed5ef57b48c9b9f9151e90839e64.fbx/RootNode/default");
            if (elevFbxRoot != null)
            {
                var children = new List<Transform>();
                for (int i = 0; i < elevFbxRoot.transform.childCount; i++)
                {
                    children.Add(elevFbxRoot.transform.GetChild(i));
                }

                var seenKeys = new HashSet<string>();
                int deletedFbxDups = 0;

                foreach (var child in children)
                {
                    if (child == null) continue;
                    var mf = child.GetComponent<MeshFilter>();
                    string meshName = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : child.name;
                    string key = $"{meshName}_{child.localPosition.x:F3}_{child.localPosition.y:F3}_{child.localPosition.z:F3}_{child.localScale.x:F3}";

                    if (seenKeys.Contains(key))
                    {
                        sb.AppendLine($"  - [DELETE DUPLICATE] FBX submesh {child.name} (ID: {child.gameObject.GetInstanceID()})");
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                        deletedFbxDups++;
                    }
                    else
                    {
                        seenKeys.Add(key);
                    }
                }
                sb.AppendLine($"Cleaned {deletedFbxDups} duplicate submesh renderers from Elevator_USSR_Entrance FBX.");
            }

            sb.AppendLine();
        }

        // ==============================================================================================
        // STEP 2: Standardize Corridor Wall Segments & Openings
        // ==============================================================================================
        private static void StandardizeCorridorWalls(Material wallMat, StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("STEP 2: STANDARDIZING CORRIDOR WALLS & DOORWAY OPENINGS TO 0.05M GRID");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var envRoot = GameObject.Find("Environment");
            if (envRoot == null) return;

            // 1. Right Wall Group: Door 214 at Z=10m (opening 9.5-10.5m), Door 216 at Z=18m (opening 17.5-18.5m)
            var rwGroup = envRoot.transform.Find("Corridor_RightWall_Group");
            if (rwGroup != null)
            {
                // RightWall_Front: 0.0 to 9.5m (center 4.75, len 9.5)
                SetTransform(rwGroup.Find("RightWall_Front"), new Vector3(1.25f, 1.40f, 4.75f), new Vector3(0.10f, 2.80f, 9.50f), sb);

                // RightWall_Lintel_214: 9.5 to 10.5m (center 10.0, len 1.0, Y 2.475)
                SetTransform(rwGroup.Find("RightWall_Lintel_214"), new Vector3(1.25f, 2.475f, 10.00f), new Vector3(0.10f, 0.65f, 1.00f), sb);

                // RightWall_Mid: 10.5 to 17.5m (center 14.0, len 7.0)
                SetTransform(rwGroup.Find("RightWall_Mid"), new Vector3(1.25f, 1.40f, 14.00f), new Vector3(0.10f, 2.80f, 7.00f), sb);

                // RightWall_Lintel_216: 17.5 to 18.5m (center 18.0, len 1.0, Y 2.475)
                SetTransform(rwGroup.Find("RightWall_Lintel_216"), new Vector3(1.25f, 2.475f, 18.00f), new Vector3(0.10f, 0.65f, 1.00f), sb);

                // RightWall_Rear: 18.5 to 24.0m (center 21.25, len 5.5)
                SetTransform(rwGroup.Find("RightWall_Rear"), new Vector3(1.25f, 1.40f, 21.25f), new Vector3(0.10f, 2.80f, 5.50f), sb);
            }

            // 2. Left Wall Group: Door 210 at Z=6m (opening 5.5-6.5m), Door 212 at Z=14m (opening 13.5-14.5m)
            var lwGroup = envRoot.transform.Find("Corridor_LeftWall_Group");
            if (lwGroup != null)
            {
                // LeftWall_Seg1: 0 to 5.5m (center 2.75, len 5.5)
                SetTransform(lwGroup.Find("LeftWall_Seg1"), new Vector3(-1.25f, 1.40f, 2.75f), new Vector3(0.10f, 2.80f, 5.50f), sb);

                // LeftWall_Lintel_210: 5.5 to 6.5m (center 6.0, len 1.0, Y 2.475)
                SetTransform(lwGroup.Find("LeftWall_Lintel_210"), new Vector3(-1.25f, 2.475f, 6.00f), new Vector3(0.10f, 0.65f, 1.00f), sb);

                // LeftWall_Seg2: 6.5 to 13.5m (center 10.0, len 7.0)
                SetTransform(lwGroup.Find("LeftWall_Seg2"), new Vector3(-1.25f, 1.40f, 10.00f), new Vector3(0.10f, 2.80f, 7.00f), sb);

                // LeftWall_Lintel_212: 13.5 to 14.5m (center 14.0, len 1.0, Y 2.475)
                SetTransform(lwGroup.Find("LeftWall_Lintel_212"), new Vector3(-1.25f, 2.475f, 14.00f), new Vector3(0.10f, 0.65f, 1.00f), sb);

                // LeftWall_Seg3: 14.5 to 24.0m (center 19.25, len 9.5)
                SetTransform(lwGroup.Find("LeftWall_Seg3"), new Vector3(-1.25f, 1.40f, 19.25f), new Vector3(0.10f, 2.80f, 9.50f), sb);
            }

            // 3. End Wall at Z = 24m
            var endGroup = envRoot.transform.Find("Corridor_EndWall_Group");
            if (endGroup != null)
            {
                SetTransform(endGroup.Find("EndWall_Left"), new Vector3(-0.90f, 1.40f, 24.05f), new Vector3(0.60f, 2.80f, 0.10f), sb);
                SetTransform(endGroup.Find("EndWall_Lintel"), new Vector3(0.00f, 2.475f, 24.05f), new Vector3(1.20f, 0.65f, 0.10f), sb);
                SetTransform(endGroup.Find("EndWall_Right"), new Vector3(0.90f, 1.40f, 24.05f), new Vector3(0.60f, 2.80f, 0.10f), sb);
            }

            sb.AppendLine();
        }

        // ==============================================================================================
        // STEP 3: Fix Swing Doors (Open 3-piece archway, 2.5cm door leaf gaps)
        // ==============================================================================================
        private static void FixSwingDoors(Material trimMat, Material doorMat, Material door214Mat, Material woodMat, StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("STEP 3: REBUILDING DOOR FRAMES & RESIZING DOOR SLABS (2 TO 3 CM GAP STANDARD)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            string[] doorNames = { "Door_210", "Door_212", "Door_214", "Door_216" };
            float[] doorZPositions = { 6.00f, 14.00f, 10.00f, 18.00f };
            bool[] isLeftWall = { true, true, false, false };

            for (int i = 0; i < doorNames.Length; i++)
            {
                string doorName = doorNames[i];
                float zPos = doorZPositions[i];
                bool onLeft = isLeftWall[i];
                bool is214 = doorName == "Door_214";

                var doorObj = GameObject.Find(doorName);
                if (doorObj == null) continue;

                sb.AppendLine($"=== Updating {doorName} ===");

                // Snap Door root position
                float wallX = onLeft ? -1.20f : 1.20f;
                Vector3 oldDoorPos = doorObj.transform.position;
                doorObj.transform.position = new Vector3(wallX, 0f, zPos);
                sb.AppendLine($"  Position: {oldDoorPos} -> {doorObj.transform.position}");

                // 1. Remove old solid blocker Frame if present
                var oldFrame = doorObj.transform.Find("Frame");
                if (oldFrame != null)
                {
                    sb.AppendLine($"  Removed solid blocker frame cube: {oldFrame.name} (Scale: {oldFrame.localScale})");
                    UnityEngine.Object.DestroyImmediate(oldFrame.gameObject);
                }

                // 2. Create open 3-piece archway Frame
                var frameGroup = new GameObject("Frame");
                frameGroup.transform.SetParent(doorObj.transform, false);
                frameGroup.transform.localPosition = Vector3.zero;
                frameGroup.transform.localRotation = Quaternion.identity;

                // Left Jamb (Hinge side): Z = -0.48, H = 2.15, W = 0.04
                CreateJamb(frameGroup.transform, "Jamb_Left", new Vector3(0f, 1.075f, -0.48f), new Vector3(0.12f, 2.15f, 0.04f), trimMat);

                // Right Jamb (Strike side): Z = +0.48, H = 2.15, W = 0.04
                CreateJamb(frameGroup.transform, "Jamb_Right", new Vector3(0f, 1.075f, 0.48f), new Vector3(0.12f, 2.15f, 0.04f), trimMat);

                // Head Jamb (Header): Y = 2.13, H = 0.04, Span = 0.92
                CreateJamb(frameGroup.transform, "Jamb_Head", new Vector3(0f, 2.13f, 0f), new Vector3(0.12f, 0.04f, 0.92f), trimMat);
                sb.AppendLine("  Built open 3-piece archway frame: Clear Opening = 0.92m W x 2.11m H (Passable doorway)");

                // 3. Align Pivot & Resize DoorSlab for 2.5cm gap
                var pivot = doorObj.transform.Find("Pivot");
                if (pivot == null)
                {
                    pivot = new GameObject("Pivot").transform;
                    pivot.SetParent(doorObj.transform, false);
                }

                // Pivot located at Z = -0.44m (hinge line, 2cm clearance from left jamb at -0.46m)
                pivot.localPosition = new Vector3(0f, 0f, -0.44f);
                pivot.localRotation = Quaternion.identity;

                // DoorSlab
                var slab = pivot.Find("DoorSlab");
                if (slab == null)
                {
                    var slabGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    slabGo.name = "DoorSlab";
                    slabGo.transform.SetParent(pivot, false);
                    slab = slabGo.transform;
                }

                // Slab dimensions: Width = 0.87m, Height = 2.08m, Thickness = 0.05m
                // Slab local Z is 0.435m (spans from Z = 0.0 to 0.87m relative to pivot at -0.44m, reaching -0.44 + 0.87 = +0.43m)
                // Clearance to right jamb at +0.46m is 0.46 - 0.43 = 0.03m = 3.0cm!
                slab.localPosition = new Vector3(0f, 1.05f, 0.435f);
                slab.localScale = new Vector3(0.05f, 2.08f, 0.87f);
                slab.localRotation = Quaternion.identity;
                slab.GetComponent<Renderer>().sharedMaterial = is214 ? door214Mat : doorMat;

                // Ensure slab has BoxCollider
                var slabCol = slab.GetComponent<BoxCollider>();
                if (slabCol == null) slabCol = slab.gameObject.AddComponent<BoxCollider>();
                slabCol.size = Vector3.one;
                slabCol.center = Vector3.zero;

                sb.AppendLine($"  Resized DoorSlab: ({slab.localScale.x:F3}m W x {slab.localScale.y:F3}m H x {slab.localScale.z:F3}m Span)");
                sb.AppendLine("  Clearance: Hinge Gap = 2.0 cm, Strike Gap = 3.0 cm, Top Gap = 2.0 cm, Sill Gap = 1.0 cm. (Perfect 2-3cm standard)");

                // Door Knob adjustment
                var knob = slab.Find("Knob");
                if (knob != null)
                {
                    float angleSign = onLeft ? 1f : -1f;
                    knob.localPosition = new Vector3(-angleSign * 0.6f, -0.05f, 0.35f);
                }

                // Room Plate adjustment
                var plate = slab.Find($"RoomPlate_{doorName.Replace("Door_", "")}");
                if (plate != null)
                {
                    float angleSign = onLeft ? 1f : -1f;
                    plate.localPosition = new Vector3(-angleSign * 0.55f, 0.45f, 0f);
                }

                // Re-bind DoorController pivotTransform
                var dc = doorObj.GetComponent<DoorController>();
                if (dc != null)
                {
                    var pivotField = typeof(DoorController).GetField("pivotTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (pivotField != null) pivotField.SetValue(dc, pivot);
                    EditorUtility.SetDirty(dc);
                }
            }

            sb.AppendLine();
        }

        private static GameObject CreateJamb(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var jamb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jamb.name = name;
            jamb.transform.SetParent(parent, false);
            jamb.transform.localPosition = pos;
            jamb.transform.localScale = scale;
            if (mat != null) jamb.GetComponent<Renderer>().sharedMaterial = mat;
            return jamb;
        }

        // ==============================================================================================
        // STEP 4: Close Gaps with Threshold Plates
        // ==============================================================================================
        private static void AddThresholdPlates(Material metalMat, Material woodMat, StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("STEP 4: CLOSING SEAMS & VOIDS WITH PRECISION THRESHOLD PLATES");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var envRoot = GameObject.Find("Environment");
            if (envRoot == null) return;

            // 1. Elevator Threshold Plate (Seals 10cm void between Z=0.00 and Z=-0.10)
            var elevRoot = envRoot.transform.Find("Elevator");
            if (elevRoot != null)
            {
                var oldThresh = elevRoot.Find("Threshold_Elevator");
                if (oldThresh != null) UnityEngine.Object.DestroyImmediate(oldThresh.gameObject);

                var threshGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                threshGo.name = "Threshold_Elevator";
                threshGo.transform.SetParent(elevRoot, false);
                threshGo.transform.position = new Vector3(0.00f, -0.005f, -0.05f);
                threshGo.transform.localScale = new Vector3(2.40f, 0.03f, 0.16f);
                threshGo.GetComponent<Renderer>().sharedMaterial = metalMat;

                sb.AppendLine("  Added [Threshold_Elevator]: Bridges Z = -0.13 to +0.03m (10cm void permanently sealed).");
            }

            // 2. Room 214 Doorway Threshold (Seals 5cm seam between corridor and Room 214)
            var door214 = GameObject.Find("Door_214");
            if (door214 != null)
            {
                var oldSill = door214.transform.Find("Threshold_Sill");
                if (oldSill != null) UnityEngine.Object.DestroyImmediate(oldSill.gameObject);

                var sillGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sillGo.name = "Threshold_Sill";
                sillGo.transform.SetParent(door214.transform, false);
                sillGo.transform.localPosition = new Vector3(0.05f, 0.005f, 0.00f);
                sillGo.transform.localScale = new Vector3(0.18f, 0.015f, 1.00f);
                sillGo.GetComponent<Renderer>().sharedMaterial = woodMat;

                sb.AppendLine("  Added [Door_214/Threshold_Sill]: Bridges X = 1.15 to 1.33m (5cm seam permanently sealed).");
            }

            // 3. Room 210, 212 & 216 Doorway Thresholds
            string[] otherDoors = { "Door_210", "Door_212", "Door_216" };
            float[] sillOffsets = { -0.05f, -0.05f, 0.05f };

            for (int i = 0; i < otherDoors.Length; i++)
            {
                var dObj = GameObject.Find(otherDoors[i]);
                if (dObj == null) continue;

                var oldSill = dObj.transform.Find("Threshold_Sill");
                if (oldSill != null) UnityEngine.Object.DestroyImmediate(oldSill.gameObject);

                var sillGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sillGo.name = "Threshold_Sill";
                sillGo.transform.SetParent(dObj.transform, false);
                sillGo.transform.localPosition = new Vector3(sillOffsets[i], 0.005f, 0.00f);
                sillGo.transform.localScale = new Vector3(0.18f, 0.015f, 1.00f);
                sillGo.GetComponent<Renderer>().sharedMaterial = woodMat;

                sb.AppendLine($"  Added [{otherDoors[i]}/Threshold_Sill]: Door threshold saddle installed.");
            }

            sb.AppendLine();
        }

        // ==============================================================================================
        // STEP 5: Fix Coplanar Overlays & Z-Fighting
        // ==============================================================================================
        private static void FixCoplanarOverlays(StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("STEP 5: FIXING COPLANAR OVERLAYS (OFFSET BY +3MM)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            // Newspaper clue on coffee table in Room 212
            var news = GameObject.Find("Environment/Room_212/Clue_Newspaper");
            if (news != null)
            {
                Vector3 p = news.transform.position;
                news.transform.position = new Vector3(p.x, 0.485f, p.z);
                sb.AppendLine($"  Offset Clue_Newspaper: Y={p.y:F4} -> {news.transform.position.y:F4} (prevents coffee table z-fight)");
            }

            // Voicemail clue on corkboard
            var vm = GameObject.Find("Props/CorkNoticeboard/Clue_Voicemail");
            if (vm != null)
            {
                Vector3 p = vm.transform.position;
                vm.transform.position = new Vector3(1.192f, p.y, p.z);
                sb.AppendLine($"  Offset Clue_Voicemail: X={p.x:F4} -> {vm.transform.position.x:F4} (prevents noticeboard z-fight)");
            }

            sb.AppendLine();
        }

        // ==============================================================================================
        // STEP 6: Apply Static Flags
        // ==============================================================================================
        private static void ApplyStaticFlags(StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("STEP 6: APPLYING STATIC FLAGS (CONTRIBUTE GI, BATCHING, OCCLUSION, NAVIGATION)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var envRoot = GameObject.Find("Environment");
            if (envRoot == null) return;

            var allTransforms = envRoot.GetComponentsInChildren<Transform>(true);
            int staticMarked = 0;
            int dynamicSkipped = 0;

            const StaticEditorFlags targetFlags = StaticEditorFlags.ContributeGI |
                                                  StaticEditorFlags.BatchingStatic |
                                                  StaticEditorFlags.OccluderStatic |
                                                  StaticEditorFlags.OccludeeStatic;

            foreach (var t in allTransforms)
            {
                var go = t.gameObject;
                string path = GetPath(go);

                // Exclude moving / dynamic objects
                bool isDynamic = path.Contains("/Pivot") || path.Contains("/DoorSlab") ||
                                 path.Contains("ElevatorDoor") || path.Contains("BareBulb") ||
                                 go.GetComponent<Light>() != null || go.GetComponent<AudioSource>() != null ||
                                 go.GetComponent<AnomalyTarget>() != null || go.GetComponent<IInteractable>() != null;

                if (isDynamic)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, 0);
                    dynamicSkipped++;
                }
                else
                {
                    GameObjectUtility.SetStaticEditorFlags(go, targetFlags);
                    staticMarked++;
                }
            }

            sb.AppendLine($"Applied Static Flags to {staticMarked} environment GameObjects.");
            sb.AppendLine($"Preserved dynamic non-static state on {dynamicSkipped} interactive/moving objects.");
            sb.AppendLine();
        }

        private static void SetTransform(Transform t, Vector3 targetPos, Vector3 targetScale, StringBuilder sb)
        {
            if (t == null) return;
            Vector3 oldPos = t.position;
            Vector3 oldScale = t.localScale;

            t.position = targetPos;
            t.localScale = targetScale;

            sb.AppendLine($"  - [{t.name}] Pos: ({oldPos.x:F3}, {oldPos.y:F3}, {oldPos.z:F3}) -> ({targetPos.x:F3}, {targetPos.y:F3}, {targetPos.z:F3}) | Scale: ({oldScale.x:F3}, {oldScale.y:F3}, {oldScale.z:F3}) -> ({targetScale.x:F3}, {targetScale.y:F3}, {targetScale.z:F3})");
        }

        private static string GetPath(GameObject go)
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
    }
}

