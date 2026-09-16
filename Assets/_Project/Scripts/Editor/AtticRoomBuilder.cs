using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EndlessHallway.Editor
{
    public static class AtticRoomBuilder
    {
        private const string MAT_DIR = "Assets/_Project/Materials/Attic";
        private const string TEX_DIR = "Assets/_Project/Art/Textures/Attic";

        [MenuItem("Tools/Endless Hallway/Build Complete Dark Attic Room", false, 28)]
        public static void BuildCompleteAtticRoom()
        {
            Debug.Log("[AtticRoomBuilder] Starting central staircase dark attic room construction...");

            var envRoot = GameObject.Find("Environment");
            if (envRoot == null)
            {
                Debug.LogError("[AtticRoomBuilder] 'Environment' root GameObject not found in scene!");
                return;
            }

            // Ensure procedural normal and mask textures exist
            EnsureProceduralTextures();

            // 1. Load Materials
            var floorMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/M_Attic_Floor_Wood.mat");
            var rafterMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/M_Attic_Rafter_Wood.mat");
            var wallMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/M_Attic_Wood_Panels.mat");
            var stoneMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/M_Chimney_Stone.mat");
            var rugMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/M_Attic_Rug.mat");
            var crateMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/M_Attic_Crate.mat");
            var lanternMetal = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/M_Attic_Lantern_Metal.mat");
            var lanternGlass = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/M_Attic_Lantern_Glass.mat");
            var portLeftMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/M_Painting_Attic_Left.mat");
            var portRightMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_DIR + "/M_Painting_Attic_Right.mat");
            var trimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Trim.mat");
            var corridorFloorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Floor.mat");

            // Setup Material properties
            ConfigureMaterials(floorMat, lanternGlass, trimMat);

            // 2. Clean old Attic_Room and Mezzanine structures
            var landing = GameObject.Find("StairwellLanding");
            if (landing != null)
            {
                var oldAttic = landing.transform.Find("Attic_Room");
                if (oldAttic != null) UnityEngine.Object.DestroyImmediate(oldAttic.gameObject);

                var oldMez = landing.transform.Find("UpperMezzanine");
                if (oldMez != null) UnityEngine.Object.DestroyImmediate(oldMez.gameObject);

                var oldCeil = landing.transform.Find("Ceiling");
                if (oldCeil != null) UnityEngine.Object.DestroyImmediate(oldCeil.gameObject);

                var oldStairs = landing.transform.Find("Stairs");
                if (oldStairs != null) UnityEngine.Object.DestroyImmediate(oldStairs.gameObject);

                var oldFloor = landing.transform.Find("Floor");
                if (oldFloor != null) UnityEngine.Object.DestroyImmediate(oldFloor.gameObject);

                var oldLowerSeal = landing.transform.Find("Stairwell_SouthSealWall_Lower");
                if (oldLowerSeal != null) UnityEngine.Object.DestroyImmediate(oldLowerSeal.gameObject);

                var oldWallLW = landing.transform.Find("Wall_Left_West");
                if (oldWallLW != null) UnityEngine.Object.DestroyImmediate(oldWallLW.gameObject);

                var oldWallRE = landing.transform.Find("Wall_Right_East");
                if (oldWallRE != null) UnityEngine.Object.DestroyImmediate(oldWallRE.gameObject);

                var oldWallFN = landing.transform.Find("Wall_Far_North");
                if (oldWallFN != null) UnityEngine.Object.DestroyImmediate(oldWallFN.gameObject);

                var oldWallSU = landing.transform.Find("Wall_South_Upper");
                if (oldWallSU != null) UnityEngine.Object.DestroyImmediate(oldWallSU.gameObject);

                var oldDado = landing.transform.Find("Dado_Rail_Left");
                if (oldDado != null) UnityEngine.Object.DestroyImmediate(oldDado.gameObject);

                var oldSconceMez = landing.transform.Find("StairwellSconce_Mezzanine");
                if (oldSconceMez != null) UnityEngine.Object.DestroyImmediate(oldSconceMez.gameObject);

                var oldSconceGnd = landing.transform.Find("StairwellSconce_Ground");
                if (oldSconceGnd != null) UnityEngine.Object.DestroyImmediate(oldSconceGnd.gameObject);
            }
            else
            {
                landing = new GameObject("StairwellLanding");
                landing.transform.SetParent(envRoot.transform, false);
            }

            // 3. Create Root Attic_Room with zeroed local transform
            GameObject atticRoot = new GameObject("Attic_Room");
            atticRoot.transform.SetParent(landing.transform, false);
            atticRoot.transform.localPosition = Vector3.zero;
            atticRoot.transform.localRotation = Quaternion.identity;
            atticRoot.transform.localScale = Vector3.one;

            // Room Footprint Constants
            // Room width: 6.40m (X = -3.20m to +3.20m, centered at X = 0.0m)
            // Room length: 7.70m (Z = 24.00m to 31.70m, midZ = 27.85m)
            // Landing floor elevation: Y = 2.80m (matches 16 steps of rise 0.175m)
            // Floor slab thickness: 0.14m -> slabMidY = 2.73m
            float floorY = 2.80f;
            float slabH = 0.14f;
            float slabMidY = floorY - (slabH / 2f); // 2.73m
            float roomHalfW = 3.20f;
            float roomMinZ = 24.00f;
            float roomMaxZ = 31.70f;
            float roomLen = roomMaxZ - roomMinZ; // 7.70m
            float roomMidZ = (roomMinZ + roomMaxZ) / 2f; // 27.85m

            // Central Stair opening in floor:
            // X: -0.70m to +0.70m (width 1.40m)
            // Z: 24.65m to 29.28m (length 4.63m)
            float stairHoleMinX = -0.70f;
            float stairHoleMaxX = 0.70f;
            float stairHoleMinZ = 24.65f;
            float stairHoleMaxZ = 29.28f;

            // 4. Floors: Bilateral Paths + Front Fireplace Landing + South Cross-Path
            GameObject floorGroup = new GameObject("Floor_Structure");
            floorGroup.transform.SetParent(atticRoot.transform, false);

            // 4a. Left Path (West): X = -3.20 to -0.70 (width 2.50m), Z = 24.00 to 31.70 (len 7.70m)
            CreateStaticBox(floorGroup.transform, "AtticFloor_LeftPath",
                new Vector3(-1.95f, slabMidY, roomMidZ),
                new Vector3(2.50f, slabH, roomLen), floorMat);

            // 4b. Right Path (East): X = +0.70 to +3.20 (width 2.50m), Z = 24.00 to 31.70 (len 7.70m)
            CreateStaticBox(floorGroup.transform, "AtticFloor_RightPath",
                new Vector3(1.95f, slabMidY, roomMidZ),
                new Vector3(2.50f, slabH, roomLen), floorMat);

            // 4c. North Landing (In front of Fireplace): X = -0.70 to +0.70 (width 1.40m), Z = 29.28 to 31.70 (len 2.42m)
            // Front edge aligns flush with top step 15 at Z = 29.28m!
            float northLandMidZ = (stairHoleMaxZ + roomMaxZ) / 2f; // 30.49m
            float northLandLen = roomMaxZ - stairHoleMaxZ; // 2.42m
            CreateStaticBox(floorGroup.transform, "AtticFloor_NorthLanding",
                new Vector3(0.0f, slabMidY, northLandMidZ),
                new Vector3(1.40f, slabH, northLandLen), floorMat);

            // Note: South cross-slab is omitted over the stair entrance (X = -0.70 to +0.70)
            // to provide 100% open, unconstrained vertical cathedral headroom above the player.

            // 5. Central Grand Staircase (Centered at X = 0.0m)
            GameObject stairsGroup = new GameObject("Stairs");
            stairsGroup.transform.SetParent(landing.transform, false);

            int numSteps = 16;
            float stepRise = 0.175f; // 16 * 0.175 = 2.80m exact
            float stepRun = 0.28f;   // 28cm deep treads
            float stairWidth = 1.30f;
            float startZ = 24.80f;
            float flightRun = numSteps * stepRun; // 4.48m
            float flightRise = numSteps * stepRise; // 2.80m

            // Bottom starter bullnose step (visual only)
            CreateStaticBox(stairsGroup.transform, "Step_Plinth",
                new Vector3(0.0f, 0.04f, startZ - 0.12f),
                new Vector3(stairWidth + 0.10f, 0.08f, 0.28f), rafterMat, false);

            // Treads, Risers, and Under-Support (visual only - movement handled by smooth Stair_MovementRamp)
            for (int i = 0; i < numSteps; i++)
            {
                float treadY = (i + 1) * stepRise;
                float treadZ = startZ + (i + 0.5f) * stepRun;
                float riserZ = startZ + (i * stepRun);

                // Tread slab with nosing (visual only)
                CreateStaticBox(stairsGroup.transform, $"Step_Tread_{i}",
                    new Vector3(0.0f, treadY - 0.02f, treadZ),
                    new Vector3(stairWidth, 0.04f, stepRun + 0.02f), rafterMat, false);

                // Closed vertical riser (visual only)
                CreateStaticBox(stairsGroup.transform, $"Step_Riser_{i}",
                    new Vector3(0.0f, treadY - (stepRise / 2f), riserZ),
                    new Vector3(stairWidth, stepRise, 0.03f), rafterMat, false);

                // Solid foundation under-support down to ground floor (visual only)
                if (i > 0)
                {
                    float underHeight = i * stepRise;
                    CreateStaticBox(stairsGroup.transform, $"Step_UnderSupport_{i}",
                        new Vector3(0.0f, underHeight / 2f, treadZ),
                        new Vector3(stairWidth - 0.02f, underHeight, stepRun), wallMat, false);
                }
            }

            // Dual Carriage Stringers (Left and Right)
            float stringerLen = Mathf.Sqrt(flightRun * flightRun + flightRise * flightRise);
            float stairAngle = Mathf.Atan2(flightRise, flightRun) * Mathf.Rad2Deg; // 32.01 deg
            float stringerMidZ = startZ + (flightRun / 2f); // 27.04m
            float stringerMidY = flightRise / 2f; // 1.40m

            var leftStringer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftStringer.name = "Stringer_Left";
            leftStringer.transform.SetParent(stairsGroup.transform, false);
            leftStringer.transform.position = new Vector3(-stairWidth / 2f, stringerMidY, stringerMidZ);
            leftStringer.transform.rotation = Quaternion.Euler(-stairAngle, 0f, 0f);
            leftStringer.transform.localScale = new Vector3(0.06f, 0.28f, stringerLen);
            leftStringer.GetComponent<Renderer>().sharedMaterial = rafterMat;
            UnityEngine.Object.DestroyImmediate(leftStringer.GetComponent<Collider>());
            SetStaticFlags(leftStringer);

            var rightStringer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightStringer.name = "Stringer_Right";
            rightStringer.transform.SetParent(stairsGroup.transform, false);
            rightStringer.transform.position = new Vector3(stairWidth / 2f, stringerMidY, stringerMidZ);
            rightStringer.transform.rotation = Quaternion.Euler(-stairAngle, 0f, 0f);
            rightStringer.transform.localScale = new Vector3(0.06f, 0.28f, stringerLen);
            rightStringer.GetComponent<Renderer>().sharedMaterial = rafterMat;
            UnityEngine.Object.DestroyImmediate(rightStringer.GetComponent<Collider>());
            SetStaticFlags(rightStringer);

            // Solid Enclosed Bulkhead Walls along both stringers down to ground floor (visual only)
            GameObject underStairBulkhead = new GameObject("UnderStair_Bulkheads");
            underStairBulkhead.transform.SetParent(stairsGroup.transform, false);
            for (int u = 1; u < numSteps; u++)
            {
                float uHeight = u * stepRise;
                float uZ = startZ + (u + 0.5f) * stepRun;
                CreateStaticBox(underStairBulkhead.transform, $"Bulkhead_Left_{u}",
                    new Vector3(-stairWidth / 2f - 0.01f, uHeight / 2f, uZ),
                    new Vector3(0.04f, uHeight, stepRun), wallMat, false);

                CreateStaticBox(underStairBulkhead.transform, $"Bulkhead_Right_{u}",
                    new Vector3(stairWidth / 2f + 0.01f, uHeight / 2f, uZ),
                    new Vector3(0.04f, uHeight, stepRun), wallMat, false);
            }

            // Smooth Movement Incline Ramp Collider for seamless character movement
            // Sloping upwards smoothly from submerged ground (Z = 24.70m, Y = -0.05m) to landing (Z = 29.28m, Y = 2.80m)
            float rampRun = 4.58f;
            float rampRise = 2.85f;
            float rampMidZ = (24.70f + 29.28f) / 2f; // 26.99f
            float rampMidY = (-0.05f + 2.80f) / 2f;  // 1.375f
            float rampAngle = Mathf.Atan2(rampRise, rampRun) * Mathf.Rad2Deg; // 31.89°
            float rampSlopeLen = Mathf.Sqrt(rampRun * rampRun + rampRise * rampRise); // 5.394m

            var stairRamp = new GameObject("Stair_MovementRamp");
            stairRamp.transform.SetParent(stairsGroup.transform, false);
            stairRamp.transform.position = new Vector3(0.0f, rampMidY, rampMidZ);
            stairRamp.transform.rotation = Quaternion.Euler(-rampAngle, 0f, 0f);
            var rampCol = stairRamp.AddComponent<BoxCollider>();
            rampCol.size = new Vector3(stairWidth, 0.04f, rampSlopeLen);
            rampCol.center = Vector3.zero;

            // Transition Landing Pad to eliminate any lip at the landing threshold
            var landingPad = new GameObject("Stair_LandingPad");
            landingPad.transform.SetParent(stairsGroup.transform, false);
            landingPad.transform.position = new Vector3(0.0f, 2.78f, 29.35f);
            var padCol = landingPad.AddComponent<BoxCollider>();
            padCol.size = new Vector3(stairWidth, 0.04f, 0.30f);
            padCol.center = Vector3.zero;

            // Ground Floor under attic (Y = 0)
            CreateStaticBox(landing.transform, "Floor_Ground",
                new Vector3(0.0f, -0.05f, roomMidZ),
                new Vector3(roomHalfW * 2f, 0.10f, roomLen), corridorFloorMat);

            // Lower Seal Walls below Y = 2.80m
            GameObject lowerWalls = new GameObject("Lower_Walls");
            lowerWalls.transform.SetParent(landing.transform, false);

            // West lower wall
            CreateStaticBox(lowerWalls.transform, "LowerWall_West",
                new Vector3(-roomHalfW, 1.40f, roomMidZ),
                new Vector3(0.12f, 2.80f, roomLen), wallMat);

            // East lower wall
            CreateStaticBox(lowerWalls.transform, "LowerWall_East",
                new Vector3(roomHalfW, 1.40f, roomMidZ),
                new Vector3(0.12f, 2.80f, roomLen), wallMat);

            // North lower back wall
            CreateStaticBox(lowerWalls.transform, "LowerWall_North",
                new Vector3(0.0f, 1.40f, roomMaxZ),
                new Vector3(roomHalfW * 2f, 2.80f, 0.12f), wallMat);

            // South lower wall flanking doorway (doorway is X = -0.60 to +0.60, height 2.20)
            CreateStaticBox(lowerWalls.transform, "LowerWall_South_Left",
                new Vector3(-1.90f, 1.40f, roomMinZ),
                new Vector3(2.60f, 2.80f, 0.12f), wallMat);

            CreateStaticBox(lowerWalls.transform, "LowerWall_South_Right",
                new Vector3(1.90f, 1.40f, roomMinZ),
                new Vector3(2.60f, 2.80f, 0.12f), wallMat);

            CreateStaticBox(lowerWalls.transform, "LowerWall_South_Header",
                new Vector3(0.0f, 2.50f, roomMinZ),
                new Vector3(1.20f, 0.60f, 0.12f), wallMat);

            // 6. Perimeter Knee-Walls (from floorY 2.80m up to 4.10m, height 1.30m)
            GameObject kneeGroup = new GameObject("Walls_Knee");
            kneeGroup.transform.SetParent(atticRoot.transform, false);

            float apexY = 8.00f;
            float kneeY = 4.10f;
            float kneeH = kneeY - floorY; // 1.30m
            float kneeMidY = (floorY + kneeY) / 2f; // 3.45m

            CreateStaticBox(kneeGroup.transform, "KneeWall_West",
                new Vector3(-roomHalfW, kneeMidY, roomMidZ),
                new Vector3(0.12f, kneeH, roomLen), wallMat);

            CreateStaticBox(kneeGroup.transform, "KneeWall_East",
                new Vector3(roomHalfW, kneeMidY, roomMidZ),
                new Vector3(0.12f, kneeH, roomLen), wallMat);

            // 7. Pitched Beam Ceiling (Cathedral A-Frame Architecture)
            // Apex at X = 0.0m, Y = 8.00m. Knee at X = ±3.20m, Y = 4.10m.
            // Rise = 3.90m, Run = 3.20m, Angle ≈ 50.6°
            GameObject roofGroup = new GameObject("Pitched_Roof");
            roofGroup.transform.SetParent(atticRoot.transform, false);

            float rise = apexY - kneeY; // 3.90m
            float run = roomHalfW;      // 3.20m
            float roofSlopeLen = Mathf.Sqrt(rise * rise + run * run) + 0.20f; // ~5.25m
            float pitchAngle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg; // 50.63°
            float midSlopeY = (apexY + kneeY) / 2f; // 6.05m

            // 7a. Roof Ceiling Deck Planks
            var roofLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofLeft.name = "RoofDeck_Left";
            roofLeft.transform.SetParent(roofGroup.transform, false);
            roofLeft.transform.position = new Vector3(-run / 2f, midSlopeY, roomMidZ);
            roofLeft.transform.rotation = Quaternion.Euler(0f, 0f, pitchAngle);
            roofLeft.transform.localScale = new Vector3(roofSlopeLen, 0.12f, roomLen + 0.04f);
            roofLeft.GetComponent<Renderer>().sharedMaterial = wallMat;
            SetStaticFlags(roofLeft);

            var roofRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofRight.name = "RoofDeck_Right";
            roofRight.transform.SetParent(roofGroup.transform, false);
            roofRight.transform.position = new Vector3(run / 2f, midSlopeY, roomMidZ);
            roofRight.transform.rotation = Quaternion.Euler(0f, 0f, -pitchAngle);
            roofRight.transform.localScale = new Vector3(roofSlopeLen, 0.12f, roomLen + 0.04f);
            roofRight.GetComponent<Renderer>().sharedMaterial = wallMat;
            SetStaticFlags(roofRight);

            // 7b. Central Longitudinal Ridge Beam at Apex
            CreateStaticBox(roofGroup.transform, "Ridge_Beam",
                new Vector3(0.0f, apexY - 0.12f, roomMidZ),
                new Vector3(0.24f, 0.24f, roomLen + 0.04f), rafterMat);

            // 7c. Structural Timber Rafters (7 bays along Z)
            float[] rafterZs = new float[] { 24.20f, 25.40f, 26.65f, 27.85f, 29.10f, 30.35f, 31.55f };
            GameObject raftersGroup = new GameObject("Rafter_Beams");
            raftersGroup.transform.SetParent(roofGroup.transform, false);

            for (int b = 0; b < rafterZs.Length; b++)
            {
                float z = rafterZs[b];

                // Left Rafter
                var rL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rL.name = $"Rafter_Left_{b}";
                rL.transform.SetParent(raftersGroup.transform, false);
                rL.transform.position = new Vector3(-run / 2f, midSlopeY - 0.04f, z);
                rL.transform.rotation = Quaternion.Euler(0f, 0f, pitchAngle);
                rL.transform.localScale = new Vector3(roofSlopeLen - 0.06f, 0.18f, 0.16f);
                rL.GetComponent<Renderer>().sharedMaterial = rafterMat;
                SetStaticFlags(rL);

                // Right Rafter
                var rR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rR.name = $"Rafter_Right_{b}";
                rR.transform.SetParent(raftersGroup.transform, false);
                rR.transform.position = new Vector3(run / 2f, midSlopeY - 0.04f, z);
                rR.transform.rotation = Quaternion.Euler(0f, 0f, -pitchAngle);
                rR.transform.localScale = new Vector3(roofSlopeLen - 0.06f, 0.18f, 0.16f);
                rR.GetComponent<Renderer>().sharedMaterial = rafterMat;
                SetStaticFlags(rR);

                // Horizontal Collar Tie Beam (Connecting left and right rafters high up at Y = 7.10m for ample headroom)
                CreateStaticBox(raftersGroup.transform, $"CollarTie_{b}",
                    new Vector3(0f, 7.10f, z),
                    new Vector3(1.55f, 0.14f, 0.12f), rafterMat);
            }

            // 7d. Longitudinal Purlin Beams
            CreateStaticBox(roofGroup.transform, "Purlin_Left",
                new Vector3(-1.60f, midSlopeY, roomMidZ),
                new Vector3(0.14f, 0.14f, roomLen), rafterMat);

            CreateStaticBox(roofGroup.transform, "Purlin_Right",
                new Vector3(1.60f, midSlopeY, roomMidZ),
                new Vector3(0.14f, 0.14f, roomLen), rafterMat);

            // 8. Watertight Gable Walls (North Z = 31.70m, South Z = 24.00m)
            GameObject gableGroup = new GameObject("Gable_Walls");
            gableGroup.transform.SetParent(atticRoot.transform, false);

            CreateWatertightGableWall(gableGroup.transform, "GableWall_North", roomMaxZ, 0.12f, wallMat, true, roomHalfW, floorY, kneeY, apexY);
            CreateWatertightGableWall(gableGroup.transform, "GableWall_South", roomMinZ, 0.12f, wallMat, false, roomHalfW, floorY, kneeY, apexY);

            // 9. Stone Fireplace & Chimney (Centered on Back Wall at X = 0.0m, Z = 31.35m)
            // Directly across from the top of the stairs!
            GameObject chimneyGroup = new GameObject("Chimney_Fireplace");
            chimneyGroup.transform.SetParent(atticRoot.transform, false);

            float chimX = 0.0f;
            float chimZ = 31.35f;

            // 9a. Chimney Breast Shaft (Starting above mantel at Y = 4.00m rising to apex 8.10m)
            CreateStaticBox(chimneyGroup.transform, "Chimney_Shaft",
                new Vector3(chimX, 6.05f, chimZ),
                new Vector3(1.80f, 4.10f, 0.60f), stoneMat);

            // 9b. Fireplace Hearth Surround Pillars
            CreateStaticBox(chimneyGroup.transform, "Hearth_Pillar_Left",
                new Vector3(chimX - 0.65f, 3.375f, chimZ - 0.08f),
                new Vector3(0.50f, 1.15f, 0.76f), stoneMat);

            CreateStaticBox(chimneyGroup.transform, "Hearth_Pillar_Right",
                new Vector3(chimX + 0.65f, 3.375f, chimZ - 0.08f),
                new Vector3(0.50f, 1.15f, 0.76f), stoneMat);

            // Heavy Stone Lintel across top of firebox opening
            CreateStaticBox(chimneyGroup.transform, "Hearth_Lintel",
                new Vector3(chimX, 3.82f, chimZ - 0.08f),
                new Vector3(1.80f, 0.20f, 0.76f), stoneMat);

            // 9c. Deep Hollow Charred Firebox Interior
            // Back wall of cavity
            CreateStaticBox(chimneyGroup.transform, "Firebox_Back",
                new Vector3(chimX, 3.375f, chimZ + 0.26f),
                new Vector3(0.80f, 1.15f, 0.08f), lanternMetal);

            // Left inner cheek of firebox cavity
            CreateStaticBox(chimneyGroup.transform, "Firebox_Cheek_Left",
                new Vector3(chimX - 0.40f, 3.375f, chimZ + 0.05f),
                new Vector3(0.06f, 1.15f, 0.45f), lanternMetal);

            // Right inner cheek of firebox cavity
            CreateStaticBox(chimneyGroup.transform, "Firebox_Cheek_Right",
                new Vector3(chimX + 0.40f, 3.375f, chimZ + 0.05f),
                new Vector3(0.06f, 1.15f, 0.45f), lanternMetal);

            // Cavity ceiling soffit
            CreateStaticBox(chimneyGroup.transform, "Firebox_Ceiling",
                new Vector3(chimX, 3.82f, chimZ + 0.05f),
                new Vector3(0.80f, 0.06f, 0.45f), lanternMetal);

            // Hearth Base Floor Slab
            CreateStaticBox(chimneyGroup.transform, "Hearth_Base_Slab",
                new Vector3(chimX, 2.83f, chimZ - 0.16f),
                new Vector3(1.90f, 0.06f, 0.95f), stoneMat);

            // Timber Mantel Shelf
            CreateStaticBox(chimneyGroup.transform, "Chimney_Mantel",
                new Vector3(chimX, 3.95f, chimZ - 0.14f),
                new Vector3(1.95f, 0.10f, 0.85f), rafterMat);

            // 10. Framed Silhouette Portraits Flanking Chimney
            GameObject portraitsGroup = new GameObject("Portraits");
            portraitsGroup.transform.SetParent(atticRoot.transform, false);

            CreateFramedPortrait(portraitsGroup.transform, "Portrait_Silhouette_Left",
                new Vector3(-1.65f, 4.15f, 31.62f),
                new Vector3(0.85f, 1.05f, 0.04f), portLeftMat, trimMat);

            CreateFramedPortrait(portraitsGroup.transform, "Portrait_Silhouette_Right",
                new Vector3(1.65f, 4.15f, 31.62f),
                new Vector3(0.85f, 1.05f, 0.04f), portRightMat, trimMat);

            // 11. Symmetrical Balustrades & Railings
            // Opening: X from -0.70m to +0.70m (width 1.40m), Z from 24.65m to 29.28m (length 4.63m).
            // Left Railing at X = -0.70m.
            // Right Railing at X = +0.70m.
            // South Cross-Railing at Z = 24.65m (safeguards South cross-path).
            // North exit at Z = 29.28m is 100% OPEN so player steps cleanly onto AtticFloor_NorthLanding!
            GameObject railingGroup = new GameObject("Stairwell_Railing");
            railingGroup.transform.SetParent(atticRoot.transform, false);

            float railLeftX = stairHoleMinX;  // -0.70m
            float railRightX = stairHoleMaxX; // +0.70m
            float railFrontZ = stairHoleMinZ; // 24.65m
            float railBackZ = stairHoleMaxZ;  // 29.28m
            float longRailLen = railBackZ - railFrontZ; // 4.63m
            float longRailMidZ = (railBackZ + railFrontZ) / 2f; // 26.965m
            float crossRailW = railRightX - railLeftX; // 1.40m

            float postH = 1.05f;
            float postMidY = floorY + (postH / 2f); // 3.325m
            float railH = 0.82f;
            float railY = floorY + railH; // 3.62m
            float shoeY = floorY + 0.03f;

            // 11a. Four Corner Newel Posts
            CreateNewelPost(railingGroup.transform, "NewelPost_SouthLeft",
                new Vector3(railLeftX, postMidY, railFrontZ), rafterMat);

            CreateNewelPost(railingGroup.transform, "NewelPost_SouthRight",
                new Vector3(railRightX, postMidY, railFrontZ), rafterMat);

            CreateNewelPost(railingGroup.transform, "NewelPost_NorthLeft",
                new Vector3(railLeftX, postMidY, railBackZ), rafterMat);

            CreateNewelPost(railingGroup.transform, "NewelPost_NorthRight",
                new Vector3(railRightX, postMidY, railBackZ), rafterMat);

            // 11b. Left Balustrade (along X = -0.70m)
            BuildLongRailing(railingGroup.transform, "Railing_Left", railLeftX, railFrontZ, railBackZ, floorY, railH, shoeY, rafterMat);

            // 11c. Right Balustrade (along X = +0.70m)
            BuildLongRailing(railingGroup.transform, "Railing_Right", railRightX, railFrontZ, railBackZ, floorY, railH, shoeY, rafterMat);

            // 11d. Foreground Stair Entrance
            // Open between SouthLeft and SouthRight newel posts as depicted in the concept art,
            // providing an unobstructed entrance and infinite vertical headroom.
            // Fall-prevention Box Colliders on the left and right side walkways:
            var leftBarrier = railingGroup.AddComponent<BoxCollider>();
            leftBarrier.center = new Vector3(railLeftX, floorY + 0.60f, longRailMidZ);
            leftBarrier.size = new Vector3(0.15f, 1.20f, longRailLen);

            var rightBarrier = railingGroup.AddComponent<BoxCollider>();
            rightBarrier.center = new Vector3(railRightX, floorY + 0.60f, longRailMidZ);
            rightBarrier.size = new Vector3(0.15f, 1.20f, longRailLen);

            // 11e. Inner Stairwell Handrails (attached along the stringers inside the stair opening)
            var handrailInL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handrailInL.name = "InnerHandrail_Left";
            handrailInL.transform.SetParent(stairsGroup.transform, false);
            handrailInL.transform.position = new Vector3(-stairWidth / 2f + 0.04f, stringerMidY + 0.70f, stringerMidZ);
            handrailInL.transform.rotation = Quaternion.Euler(-stairAngle, 0f, 0f);
            handrailInL.transform.localScale = new Vector3(0.05f, 0.07f, stringerLen);
            handrailInL.GetComponent<Renderer>().sharedMaterial = rafterMat;
            UnityEngine.Object.DestroyImmediate(handrailInL.GetComponent<Collider>());
            SetStaticFlags(handrailInL);

            var handrailInR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handrailInR.name = "InnerHandrail_Right";
            handrailInR.transform.SetParent(stairsGroup.transform, false);
            handrailInR.transform.position = new Vector3(stairWidth / 2f - 0.04f, stringerMidY + 0.70f, stringerMidZ);
            handrailInR.transform.rotation = Quaternion.Euler(-stairAngle, 0f, 0f);
            handrailInR.transform.localScale = new Vector3(0.05f, 0.07f, stringerLen);
            handrailInR.GetComponent<Renderer>().sharedMaterial = rafterMat;
            UnityEngine.Object.DestroyImmediate(handrailInR.GetComponent<Collider>());
            SetStaticFlags(handrailInR);

            // 12. Set Dressing Props (Shelves, Desk, Chair on Left Path; Crate, Rug on Right Path)
            GameObject dressingGroup = new GameObject("Set_Dressing");
            dressingGroup.transform.SetParent(atticRoot.transform, false);

            // 12a. Left Metal/Wood Shelving Unit with books against West knee-wall
            BuildShelvingUnit(dressingGroup.transform, new Vector3(-2.60f, floorY, 28.50f), rafterMat, trimMat);

            // 12b. Wooden Study Desk with papers and clutter
            BuildStudyDesk(dressingGroup.transform, new Vector3(-2.40f, floorY, 26.00f), rafterMat, trimMat);

            // 12c. Wooden Spindle Chair
            BuildChair(dressingGroup.transform, new Vector3(-1.85f, floorY, 26.00f), rafterMat);

            // 12d. Wooden Cargo Crate on Right Path
            BuildCrate(dressingGroup.transform, new Vector3(2.40f, floorY + 0.31f, 28.20f), crateMat);

            // 12e. Discolored Stained Floor Rug on Right Path
            var rugObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            rugObj.name = "Stained_Floor_Rug";
            rugObj.transform.SetParent(dressingGroup.transform, false);
            rugObj.transform.position = new Vector3(1.90f, floorY + 0.005f, 29.80f);
            rugObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            rugObj.transform.localScale = new Vector3(1.40f, 1.80f, 1f);
            rugObj.GetComponent<Renderer>().sharedMaterial = rugMat;
            UnityEngine.Object.DestroyImmediate(rugObj.GetComponent<Collider>());
            SetStaticFlags(rugObj);

            // 13. Atmospheric Lighting: Central Lantern + Distant Lower Beacon
            GameObject lightingGroup = new GameObject("Attic_Lighting");
            lightingGroup.transform.SetParent(atticRoot.transform, false);

            // 13a. Central Hanging Lantern directly above the head of the stairwell/North landing
            Vector3 lanternPos = new Vector3(0.0f, 6.05f, 28.50f);

            var cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cord.name = "Lantern_Cord";
            cord.transform.SetParent(lightingGroup.transform, false);
            cord.transform.position = new Vector3(0.0f, 7.00f, 28.50f);
            cord.transform.localScale = new Vector3(0.03f, 1.60f, 0.03f);
            var cordRend = cord.GetComponent<Renderer>();
            cordRend.sharedMaterial = lanternMetal;
            cordRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(cord.GetComponent<Collider>());
            SetStaticFlags(cord);

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Lantern_Cap";
            cap.transform.SetParent(lightingGroup.transform, false);
            cap.transform.position = new Vector3(0.0f, 6.36f, 28.50f);
            cap.transform.localScale = new Vector3(0.32f, 0.08f, 0.32f);
            var capRend = cap.GetComponent<Renderer>();
            capRend.sharedMaterial = lanternMetal;
            capRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
            SetStaticFlags(cap);

            var glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Lantern_Glass";
            glass.transform.SetParent(lightingGroup.transform, false);
            glass.transform.position = new Vector3(0.0f, 6.20f, 28.50f);
            glass.transform.localScale = new Vector3(0.24f, 0.26f, 0.24f);
            var glassRend = glass.GetComponent<Renderer>();
            glassRend.sharedMaterial = lanternGlass;
            glassRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(glass.GetComponent<Collider>());
            SetStaticFlags(glass);

            var lgtObj = new GameObject("Hanging_Lantern_Light");
            lgtObj.transform.SetParent(lightingGroup.transform, false);
            lgtObj.transform.position = lanternPos;

            var pointLgt = lgtObj.AddComponent<Light>();
            pointLgt.type = LightType.Point;
            pointLgt.color = new Color(1.0f, 0.80f, 0.50f); // 2700K warm
            pointLgt.intensity = 6.0f;
            pointLgt.range = 15.0f;
            pointLgt.shadows = LightShadows.Soft;

            // 13b. Distant Hallway Beacon Light (Far down at the bottom of stairs, Z = 2.5m)
            var beaconObj = GameObject.Find("Stairwell_Distant_Beacon");
            if (beaconObj != null) UnityEngine.Object.DestroyImmediate(beaconObj);

            beaconObj = new GameObject("Stairwell_Distant_Beacon");
            beaconObj.transform.SetParent(landing.transform, false);
            beaconObj.transform.position = new Vector3(0.0f, 2.10f, 2.50f);

            var beaconBulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beaconBulb.name = "Beacon_Bulb";
            beaconBulb.transform.SetParent(beaconObj.transform, false);
            beaconBulb.transform.localPosition = Vector3.zero;
            beaconBulb.transform.localScale = new Vector3(0.12f, 0.16f, 0.12f);
            var bulbRend = beaconBulb.GetComponent<Renderer>();
            bulbRend.sharedMaterial = lanternGlass;
            bulbRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(beaconBulb.GetComponent<Collider>());
            SetStaticFlags(beaconBulb);

            var beaconLight = beaconObj.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = new Color(1.0f, 0.85f, 0.60f);
            beaconLight.intensity = 1.2f;
            beaconLight.range = 3.5f;
            beaconLight.shadows = LightShadows.None;

            // 13c. Dim Corridor Light Bleed onto Stairs (CeilingLight_5)
            var allLights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in allLights)
            {
                if (l.name == "PointLight" && Mathf.Abs(l.transform.position.z - 22.0f) < 0.5f)
                {
                    l.range = 2.4f;
                    l.intensity = 0.8f;
                }
            }

            // 13d. Set Deep Dark Ambient Lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.012f, 0.012f, 0.016f);
            RenderSettings.ambientIntensity = 0.15f;

            // 14. Save Scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[AtticRoomBuilder] Central staircase dark attic room successfully constructed, optimized, and saved!");
        }

        private static void BuildLongRailing(Transform parent, string name, float railX, float frontZ, float backZ, float floorY, float railH, float shoeY, Material mat)
        {
            float len = backZ - frontZ;
            float midZ = (backZ + frontZ) / 2f;
            float railY = floorY + railH;

            CreateStaticBox(parent, $"{name}_Shoe",
                new Vector3(railX, shoeY, midZ),
                new Vector3(0.06f, 0.06f, len - 0.12f), mat);

            CreateStaticBox(parent, $"{name}_Handrail",
                new Vector3(railX, railY, midZ),
                new Vector3(0.08f, 0.08f, len - 0.12f), mat);

            int numBalusters = 24;
            float balStep = (len - 0.24f) / (numBalusters + 1);
            for (int i = 1; i <= numBalusters; i++)
            {
                float bZ = frontZ + 0.12f + (i * balStep);
                CreateStaticBox(parent, $"{name}_Baluster_{i}",
                    new Vector3(railX, floorY + (railH / 2f), bZ),
                    new Vector3(0.038f, railH + 0.04f, 0.038f), mat);
            }
        }

        private static void CreateWatertightGableWall(Transform parent, string name, float zPos, float thickness, Material mat, bool facingInward, float halfW, float floorY, float kneeY, float apexY)
        {
            var wallObj = new GameObject(name);
            wallObj.transform.SetParent(parent, false);
            wallObj.transform.position = new Vector3(0f, 0f, zPos);

            var mf = wallObj.AddComponent<MeshFilter>();
            var mr = wallObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";

            float halfT = thickness / 2f;
            float zF = facingInward ? -halfT : halfT;
            float zB = facingInward ? halfT : -halfT;

            // Pentagon 2D vertices: (X, Y)
            // 0: Bottom-Left  (-halfW, floorY)
            // 1: Bottom-Right ( halfW, floorY)
            // 2: Knee-Right   ( halfW, kneeY)
            // 3: Apex         ( 0.00,  apexY)
            // 4: Knee-Left    (-halfW, kneeY)

            Vector3[] verts = new Vector3[]
            {
                // Front face
                new Vector3(-halfW, floorY, zF),
                new Vector3( halfW, floorY, zF),
                new Vector3( halfW, kneeY,  zF),
                new Vector3( 0.00f, apexY,  zF),
                new Vector3(-halfW, kneeY,  zF),

                // Back face
                new Vector3(-halfW, floorY, zB),
                new Vector3( halfW, floorY, zB),
                new Vector3( halfW, kneeY,  zB),
                new Vector3( 0.00f, apexY,  zB),
                new Vector3(-halfW, kneeY,  zB),
            };

            Vector2[] uvs = new Vector2[10];
            for (int i = 0; i < 5; i++)
            {
                uvs[i] = new Vector2((verts[i].x + halfW) / (halfW * 2f) * 4f, (verts[i].y - floorY) / (apexY - floorY) * 3f);
                uvs[i + 5] = uvs[i];
            }

            // Triangles
            int[] tris = new int[]
            {
                // Front face
                0, 2, 1,
                0, 4, 2,
                4, 3, 2,

                // Back face
                5, 6, 7,
                5, 7, 9,
                9, 7, 8
            };

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;

            var col = wallObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, (floorY + apexY) / 2f, 0f);
            col.size = new Vector3(halfW * 2f, apexY - floorY, thickness);

            SetStaticFlags(wallObj);
        }

        private static void EnsureProceduralTextures()
        {
            string normPath = TEX_DIR + "/T_Attic_Floor_Normal.png";
            string maskPath = TEX_DIR + "/T_Attic_Floor_Mask.png";

            if (System.IO.File.Exists(normPath) && System.IO.File.Exists(maskPath)) return;

            int w = 512; int h = 512;
            Texture2D normTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Texture2D maskTex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            int numPlanks = 8;
            int plankW = w / numPlanks;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int xInPlank = x % plankW;
                    bool isSeam = (xInPlank == 0 || xInPlank == plankW - 1);
                    bool isNearSeam = (xInPlank <= 2 || xInPlank >= plankW - 3);

                    float grain = Mathf.PerlinNoise(x * 0.15f, y * 0.02f) * 0.15f;
                    float fineGrain = Mathf.PerlinNoise(x * 0.4f, y * 0.1f) * 0.08f;

                    float nx = 0f;
                    if (xInPlank < 4) nx = -0.5f * (1f - (xInPlank / 4f));
                    else if (xInPlank > plankW - 5) nx = 0.5f * ((xInPlank - (plankW - 5)) / 4f);
                    nx += (grain - 0.075f) * 0.5f;

                    float ny = (fineGrain - 0.04f) * 0.3f;
                    Vector3 n = new Vector3(nx, ny, 1f).normalized;

                    Color nCol = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                    normTex.SetPixel(x, y, nCol);

                    float metallic = 0.02f;
                    float ao = isSeam ? 0.2f : (isNearSeam ? 0.6f : 0.95f);
                    float smoothness = isSeam ? 0.30f : (isNearSeam ? 0.55f : (0.84f + grain * 0.6f));
                    smoothness = Mathf.Clamp(smoothness, 0.2f, 0.95f);

                    Color mCol = new Color(metallic, ao, 0f, smoothness);
                    maskTex.SetPixel(x, y, mCol);
                }
            }

            normTex.Apply();
            maskTex.Apply();

            System.IO.File.WriteAllBytes(normPath, normTex.EncodeToPNG());
            System.IO.File.WriteAllBytes(maskPath, maskTex.EncodeToPNG());

            AssetDatabase.ImportAsset(normPath);
            AssetDatabase.ImportAsset(maskPath);

            var normImporter = AssetImporter.GetAtPath(normPath) as TextureImporter;
            if (normImporter != null)
            {
                normImporter.textureType = TextureImporterType.NormalMap;
                normImporter.SaveAndReimport();
            }

            var maskImporter = AssetImporter.GetAtPath(maskPath) as TextureImporter;
            if (maskImporter != null)
            {
                maskImporter.sRGBTexture = false;
                maskImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                maskImporter.SaveAndReimport();
            }

            UnityEngine.Object.DestroyImmediate(normTex);
            UnityEngine.Object.DestroyImmediate(maskTex);
        }

        private static void ConfigureMaterials(Material floorMat, Material lanternGlass, Material trimMat)
        {
            if (floorMat != null)
            {
                var norm = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "/T_Attic_Floor_Normal.png");
                var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_DIR + "/T_Attic_Floor_Mask.png");
                if (norm != null)
                {
                    floorMat.SetTexture("_BumpMap", norm);
                    floorMat.SetFloat("_BumpScale", 0.65f);
                    floorMat.EnableKeyword("_NORMALMAP");
                }
                if (mask != null)
                {
                    floorMat.SetTexture("_MetallicGlossMap", mask);
                    floorMat.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                floorMat.SetFloat("_Smoothness", 0.92f);
                floorMat.SetFloat("_SmoothnessTextureChannel", 0f);
                floorMat.mainTextureScale = new Vector2(4.0f, 6.0f);
                floorMat.color = new Color(0.28f, 0.20f, 0.14f);
                EditorUtility.SetDirty(floorMat);
            }

            if (lanternGlass != null)
            {
                lanternGlass.EnableKeyword("_EMISSION");
                lanternGlass.SetColor("_EmissionColor", new Color(1.0f, 0.85f, 0.5f) * 2.5f);
                EditorUtility.SetDirty(lanternGlass);
            }

            if (trimMat != null)
            {
                trimMat.EnableKeyword("_EMISSION");
                trimMat.SetColor("_EmissionColor", new Color(0.18f, 0.13f, 0.08f));
                EditorUtility.SetDirty(trimMat);
            }
        }

        private static GameObject CreateStaticBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool withCollider = true)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = pos;
            box.transform.localScale = scale;
            if (mat != null) box.GetComponent<Renderer>().sharedMaterial = mat;
            if (!withCollider)
            {
                UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            }
            SetStaticFlags(box);
            return box;
        }

        private static void SetStaticFlags(GameObject go)
        {
            var flags = StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.ContributeGI |
                        StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.OccludeeStatic;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
        }

        private static void CreateNewelPost(Transform parent, string name, Vector3 pos, Material mat)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = name;
            post.transform.SetParent(parent, false);
            post.transform.position = pos;
            post.transform.localScale = new Vector3(0.12f, 1.05f, 0.12f);
            post.GetComponent<Renderer>().sharedMaterial = mat;
            SetStaticFlags(post);

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Finial";
            sphere.transform.SetParent(post.transform, false);
            sphere.transform.localPosition = new Vector3(0f, 0.56f, 0f);
            sphere.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
            sphere.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
            SetStaticFlags(sphere);
        }

        private static void CreateFramedPortrait(Transform parent, string name, Vector3 pos, Vector3 scale, Material canvasMat, Material frameMat)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.position = pos;

            // Frame
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(group.transform, false);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = scale;
            frame.GetComponent<Renderer>().sharedMaterial = frameMat;
            SetStaticFlags(frame);

            // Canvas
            var canvas = GameObject.CreatePrimitive(PrimitiveType.Quad);
            canvas.name = "Canvas";
            canvas.transform.SetParent(group.transform, false);
            canvas.transform.localPosition = new Vector3(0f, 0f, -scale.z / 2f - 0.005f);
            canvas.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvas.transform.localScale = new Vector3(scale.x * 0.86f, scale.y * 0.86f, 1f);
            canvas.GetComponent<Renderer>().sharedMaterial = canvasMat;
            UnityEngine.Object.DestroyImmediate(canvas.GetComponent<Collider>());
            SetStaticFlags(canvas);
        }

        private static void BuildShelvingUnit(Transform parent, Vector3 pos, Material woodMat, Material metalMat)
        {
            var unit = new GameObject("Metal_Shelving_Unit");
            unit.transform.SetParent(parent, false);
            unit.transform.position = pos;

            float w = 0.50f; float h = 1.75f; float l = 1.60f;
            Vector3[] postLocs = new Vector3[] {
                new Vector3(-w/2, h/2, -l/2), new Vector3(w/2, h/2, -l/2),
                new Vector3(-w/2, h/2, l/2), new Vector3(w/2, h/2, l/2)
            };
            foreach (var p in postLocs)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.transform.SetParent(unit.transform, false);
                post.transform.localPosition = p;
                post.transform.localScale = new Vector3(0.04f, h, 0.04f);
                post.GetComponent<Renderer>().sharedMaterial = metalMat;
                SetStaticFlags(post);
            }

            for (int s = 0; s < 4; s++)
            {
                float sy = 0.15f + (s * (h - 0.25f) / 3f);
                var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelf.transform.SetParent(unit.transform, false);
                shelf.transform.localPosition = new Vector3(0f, sy, 0f);
                shelf.transform.localScale = new Vector3(w, 0.035f, l);
                shelf.GetComponent<Renderer>().sharedMaterial = woodMat;
                SetStaticFlags(shelf);

                if (s < 2)
                {
                    var books = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    books.transform.SetParent(unit.transform, false);
                    books.transform.localPosition = new Vector3(0f, sy + 0.14f, (s == 0 ? -0.35f : 0.25f));
                    books.transform.localScale = new Vector3(0.25f, 0.24f, 0.45f);
                    books.GetComponent<Renderer>().sharedMaterial = woodMat;
                    UnityEngine.Object.DestroyImmediate(books.GetComponent<Collider>());
                    SetStaticFlags(books);
                }
            }

            var col = unit.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, h/2, 0f);
            col.size = new Vector3(w, h, l);
        }

        private static void BuildStudyDesk(Transform parent, Vector3 pos, Material woodMat, Material trimMat)
        {
            var desk = new GameObject("Study_Desk");
            desk.transform.SetParent(parent, false);
            desk.transform.position = pos;

            float w = 0.70f; float h = 0.76f; float l = 1.30f;
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(desk.transform, false);
            top.transform.localPosition = new Vector3(0f, h - 0.025f, 0f);
            top.transform.localScale = new Vector3(w, 0.05f, l);
            top.GetComponent<Renderer>().sharedMaterial = woodMat;
            SetStaticFlags(top);

            Vector3[] legLocs = new Vector3[] {
                new Vector3(-w/2 + 0.05f, (h-0.05f)/2, -l/2 + 0.05f),
                new Vector3(w/2 - 0.05f, (h-0.05f)/2, -l/2 + 0.05f),
                new Vector3(-w/2 + 0.05f, (h-0.05f)/2, l/2 - 0.05f),
                new Vector3(w/2 - 0.05f, (h-0.05f)/2, l/2 - 0.05f)
            };
            foreach (var lp in legLocs)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.transform.SetParent(desk.transform, false);
                leg.transform.localPosition = lp;
                leg.transform.localScale = new Vector3(0.06f, h - 0.05f, 0.06f);
                leg.GetComponent<Renderer>().sharedMaterial = woodMat;
                SetStaticFlags(leg);
            }

            var col = desk.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, h/2, 0f);
            col.size = new Vector3(w, h, l);
        }

        private static void BuildChair(Transform parent, Vector3 pos, Material woodMat)
        {
            var chair = new GameObject("Spindle_Chair");
            chair.transform.SetParent(parent, false);
            chair.transform.position = pos;

            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.transform.SetParent(chair.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            seat.transform.localScale = new Vector3(0.42f, 0.04f, 0.42f);
            seat.GetComponent<Renderer>().sharedMaterial = woodMat;
            SetStaticFlags(seat);

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.transform.SetParent(chair.transform, false);
            back.transform.localPosition = new Vector3(-0.18f, 0.68f, 0f);
            back.transform.localScale = new Vector3(0.04f, 0.48f, 0.40f);
            back.GetComponent<Renderer>().sharedMaterial = woodMat;
            SetStaticFlags(back);

            var col = chair.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(0.45f, 0.90f, 0.45f);
        }

        private static void BuildCrate(Transform parent, Vector3 pos, Material crateMat)
        {
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Shipping_Crate";
            crate.transform.SetParent(parent, false);
            crate.transform.position = pos;
            crate.transform.localScale = new Vector3(0.68f, 0.62f, 1.25f);
            crate.GetComponent<Renderer>().sharedMaterial = crateMat;
            SetStaticFlags(crate);
        }
    }
}
