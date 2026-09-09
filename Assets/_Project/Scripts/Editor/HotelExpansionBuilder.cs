using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EndlessHallway.Interaction;
using EndlessHallway.Anomaly;
using EndlessHallway.Entity;

namespace EndlessHallway.Editor
{
    public static class HotelExpansionBuilder
    {
        [MenuItem("Tools/Endless Hallway/Build Complete Hotel Floor Expansion", false, 25)]
        public static void BuildHotelExpansion()
        {
            var envRoot = GameObject.Find("Environment");
            if (envRoot == null) envRoot = new GameObject("Environment");

            var propsRoot = GameObject.Find("Props");
            if (propsRoot == null) propsRoot = new GameObject("Props");

            // 1. Materials
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Wall.mat");
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Floor.mat");
            Material ceilingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Ceiling.mat");
            Material trimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Trim.mat");
            Material doorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door.mat");
            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door_Metal.mat");
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Chair_Wood.mat");
            Material paperMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Paper_Notice.mat");

            // Create or load expansion materials
            Material suiteWallMat = GetOrCreateMat("Assets/_Project/Materials/M_Suite_Wallpaper.mat", new Color(0.24f, 0.32f, 0.42f), 0.15f);
            Material redCarpetMat = GetOrCreateMat("Assets/_Project/Materials/M_Suite_Carpet.mat", new Color(0.48f, 0.12f, 0.14f), 0.05f);
            Material navyCouchMat = GetOrCreateMat("Assets/_Project/Materials/M_Couch_Navy.mat", new Color(0.10f, 0.16f, 0.28f), 0.18f);
            Material quiltMat = GetOrCreateMat("Assets/_Project/Materials/M_Bed_Quilt.mat", new Color(0.35f, 0.28f, 0.22f), 0.10f);
            Material utilityWallMat = GetOrCreateMat("Assets/_Project/Materials/M_Utility_Wall.mat", new Color(0.32f, 0.35f, 0.38f), 0.12f);
            Material shelvingMat = GetOrCreateMat("Assets/_Project/Materials/M_Metal_Shelving.mat", new Color(0.20f, 0.22f, 0.25f), 0.45f);
            Material stairWoodMat = GetOrCreateMat("Assets/_Project/Materials/M_Staircase_Wood.mat", new Color(0.22f, 0.15f, 0.09f), 0.65f);

            // 2. Re-segment Corridor Walls to create Doorways
            ReconstructCorridorWalls(envRoot, wallMat);

            // 3. Build Room 210: Standard Hotel Room
            BuildRoom210(envRoot, wallMat, floorMat, ceilingMat, woodMat, quiltMat, paperMat);

            // 4. Build Room 212: Domestic Living Room Suite
            BuildRoom212(envRoot, suiteWallMat, redCarpetMat, ceilingMat, navyCouchMat, woodMat, paperMat);

            // 5. Build Room 216: Maintenance / Utility Room with Scripted Jump Scare
            BuildRoom216(envRoot, utilityWallMat, floorMat, ceilingMat, shelvingMat, metalMat);

            // 6. Build Stairwell Landing at Z = 24m (Reference Image 0)
            BuildStairwellLanding(envRoot, wallMat, floorMat, ceilingMat, trimMat, stairWoodMat);

            EditorUtility.SetDirty(envRoot);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[HotelExpansionBuilder] Complete Hotel Floor Expansion successfully built and saved!");
        }

        private static Material GetOrCreateMat(string path, Color baseColor, float smoothness)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.SetColor("_BaseColor", baseColor);
                mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void ReconstructCorridorWalls(GameObject envRoot, Material wallMat)
        {
            // Remove old monolithic left wall if present
            var oldLw = GameObject.Find("Corridor_LeftWall");
            if (oldLw != null) Object.DestroyImmediate(oldLw);

            var oldLwGroup = GameObject.Find("Corridor_LeftWall_Group");
            if (oldLwGroup != null) Object.DestroyImmediate(oldLwGroup);

            GameObject lwGroup = new GameObject("Corridor_LeftWall_Group");
            lwGroup.transform.SetParent(envRoot.transform);

            // Left Wall Door 210 is at Z = 6.0m, Door 212 is at Z = 14.0m
            // Seg 1: 0 to 5.5 (center 2.75, len 5.5)
            CreateWallSeg(lwGroup.transform, "LeftWall_Seg1", new Vector3(-1.25f, 1.4f, 2.75f), new Vector3(0.1f, 2.8f, 5.5f), wallMat);
            // Lintel 210: 5.5 to 6.5 (center 6.0, Y 2.48, len 1.0)
            CreateWallSeg(lwGroup.transform, "LeftWall_Lintel_210", new Vector3(-1.25f, 2.48f, 6.0f), new Vector3(0.1f, 0.65f, 1.0f), wallMat);
            // Seg 2: 6.5 to 13.5 (center 10.0, len 7.0)
            CreateWallSeg(lwGroup.transform, "LeftWall_Seg2", new Vector3(-1.25f, 1.4f, 10.0f), new Vector3(0.1f, 2.8f, 7.0f), wallMat);
            // Lintel 212: 13.5 to 14.5 (center 14.0, Y 2.48, len 1.0)
            CreateWallSeg(lwGroup.transform, "LeftWall_Lintel_212", new Vector3(-1.25f, 2.48f, 14.0f), new Vector3(0.1f, 0.65f, 1.0f), wallMat);
            // Seg 3: 14.5 to 24.0 (center 19.25, len 9.5)
            CreateWallSeg(lwGroup.transform, "LeftWall_Seg3", new Vector3(-1.25f, 1.4f, 19.25f), new Vector3(0.1f, 2.8f, 9.5f), wallMat);

            // Reconstruct Right Wall for Door 214 (Z = 10m) and Door 216 (Z = 18m)
            var oldRw = GameObject.Find("Corridor_RightWall");
            if (oldRw != null) Object.DestroyImmediate(oldRw);

            var oldRwGroup = GameObject.Find("Corridor_RightWall_Group");
            if (oldRwGroup != null) Object.DestroyImmediate(oldRwGroup);

            GameObject rwGroup = new GameObject("Corridor_RightWall_Group");
            rwGroup.transform.SetParent(envRoot.transform);

            // Seg 1: 0 to 9.45 (center 4.725, len 9.45)
            CreateWallSeg(rwGroup.transform, "RightWall_Front", new Vector3(1.25f, 1.4f, 4.725f), new Vector3(0.1f, 2.8f, 9.45f), wallMat);
            // Lintel 214: 9.45 to 10.55 (center 10.0, Y 2.48, len 1.1)
            CreateWallSeg(rwGroup.transform, "RightWall_Lintel_214", new Vector3(1.25f, 2.48f, 10.0f), new Vector3(0.1f, 0.65f, 1.1f), wallMat);
            // Seg 2: 10.55 to 17.5 (center 14.025, len 6.95)
            CreateWallSeg(rwGroup.transform, "RightWall_Mid", new Vector3(1.25f, 1.4f, 14.025f), new Vector3(0.1f, 2.8f, 6.95f), wallMat);
            // Lintel 216: 17.5 to 18.5 (center 18.0, Y 2.48, len 1.0)
            CreateWallSeg(rwGroup.transform, "RightWall_Lintel_216", new Vector3(1.25f, 2.48f, 18.0f), new Vector3(0.1f, 0.65f, 1.0f), wallMat);
            // Seg 3: 18.5 to 24.0 (center 21.25, len 5.5)
            CreateWallSeg(rwGroup.transform, "RightWall_Rear", new Vector3(1.25f, 1.4f, 21.25f), new Vector3(0.1f, 2.8f, 5.5f), wallMat);

            // End Wall doorway leading into Stairwell Landing
            var oldEndWall = GameObject.Find("Corridor_EndWall");
            if (oldEndWall != null) Object.DestroyImmediate(oldEndWall);

            var oldEndGroup = GameObject.Find("Corridor_EndWall_Group");
            if (oldEndGroup != null) Object.DestroyImmediate(oldEndGroup);

            GameObject endGroup = new GameObject("Corridor_EndWall_Group");
            endGroup.transform.SetParent(envRoot.transform);

            // Left pillar: X = -1.2 to -0.6 (center -0.9, width 0.6)
            CreateWallSeg(endGroup.transform, "EndWall_Left", new Vector3(-0.9f, 1.4f, 24.05f), new Vector3(0.6f, 2.8f, 0.1f), wallMat);
            // Lintel: X = -0.6 to 0.6 (center 0.0, Y 2.48, width 1.2)
            CreateWallSeg(endGroup.transform, "EndWall_Lintel", new Vector3(0f, 2.48f, 24.05f), new Vector3(1.2f, 0.65f, 0.1f), wallMat);
            // Right pillar: X = 0.6 to 1.2 (center 0.9, width 0.6)
            CreateWallSeg(endGroup.transform, "EndWall_Right", new Vector3(0.9f, 1.4f, 24.05f), new Vector3(0.6f, 2.8f, 0.1f), wallMat);
        }

        private static GameObject CreateWallSeg(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = pos;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = mat;
            return cube;
        }

        private static void BuildRoom210(GameObject envRoot, Material wallMat, Material floorMat, Material ceilingMat, Material woodMat, Material quiltMat, Material paperMat)
        {
            var oldRoom = GameObject.Find("Room_210");
            if (oldRoom != null) Object.DestroyImmediate(oldRoom);

            GameObject room = new GameObject("Room_210");
            room.transform.SetParent(envRoot.transform);

            float cx = -3.5f; float cz = 6.0f; float rw = 4.6f; float rl = 4.4f; float rh = 2.8f;

            // Floor & Ceiling
            CreateBox(room.transform, "Floor", new Vector3(cx, -0.05f, cz), new Vector3(rw, 0.1f, rl), floorMat);
            CreateBox(room.transform, "Ceiling", new Vector3(cx, rh + 0.05f, cz), new Vector3(rw, 0.1f, rl), ceilingMat);

            // Outer Walls
            CreateBox(room.transform, "Wall_West", new Vector3(cx - rw/2f, rh/2f, cz), new Vector3(0.1f, rh, rl), wallMat);
            CreateBox(room.transform, "Wall_North", new Vector3(cx, rh/2f, cz + rl/2f), new Vector3(rw, rh, 0.1f), wallMat);
            CreateBox(room.transform, "Wall_South", new Vector3(cx, rh/2f, cz - rl/2f), new Vector3(rw, rh, 0.1f), wallMat);

            // Furniture: Queen Bed
            GameObject bed = new GameObject("HotelBed");
            bed.transform.SetParent(room.transform);
            bed.transform.position = new Vector3(-4.5f, 0f, 7.0f);
            CreateBox(bed.transform, "Frame", new Vector3(-4.5f, 0.2f, 7.0f), new Vector3(1.8f, 0.4f, 2.2f), woodMat);
            CreateBox(bed.transform, "Mattress", new Vector3(-4.5f, 0.5f, 7.0f), new Vector3(1.7f, 0.3f, 2.1f), quiltMat);
            CreateBox(bed.transform, "Headboard", new Vector3(-5.4f, 0.7f, 7.0f), new Vector3(0.15f, 1.0f, 2.0f), woodMat);
            CreateBox(bed.transform, "Pillow_1", new Vector3(-5.0f, 0.72f, 6.5f), new Vector3(0.45f, 0.15f, 0.65f), quiltMat);
            CreateBox(bed.transform, "Pillow_2", new Vector3(-5.0f, 0.72f, 7.5f), new Vector3(0.45f, 0.15f, 0.65f), quiltMat);

            // Nightstand & Lamp
            GameObject nightstand = CreateBox(room.transform, "Nightstand", new Vector3(-5.2f, 0.35f, 5.2f), new Vector3(0.65f, 0.7f, 0.65f), woodMat);
            GameObject lamp = CreateBox(room.transform, "BedsideLamp", new Vector3(-5.2f, 0.85f, 5.2f), new Vector3(0.25f, 0.35f, 0.25f), woodMat);

            GameObject lampLightObj = new GameObject("LampLight");
            lampLightObj.transform.SetParent(lamp.transform);
            lampLightObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            var lgt = lampLightObj.AddComponent<Light>();
            lgt.type = LightType.Point;
            lgt.range = 5.0f;
            lgt.intensity = 1.6f;
            lgt.color = new Color(1.0f, 0.88f, 0.72f);

            // Armchair & Luggage
            CreateBox(room.transform, "Armchair", new Vector3(-2.2f, 0.45f, 4.6f), new Vector3(0.85f, 0.9f, 0.85f), woodMat);
            CreateBox(room.transform, "Suitcase", new Vector3(-2.2f, 0.25f, 7.4f), new Vector3(0.55f, 0.35f, 0.75f), woodMat);

            // Clue: Room Service Breakfast Order Slip
            GameObject slip = GameObject.CreatePrimitive(PrimitiveType.Quad);
            slip.name = "Clue_RoomService";
            slip.transform.SetParent(room.transform);
            slip.transform.position = new Vector3(-5.2f, 0.71f, 5.35f);
            slip.transform.rotation = Quaternion.Euler(90f, 15f, 0f);
            slip.transform.localScale = new Vector3(0.22f, 0.3f, 1f);
            slip.GetComponent<Renderer>().sharedMaterial = paperMat;
            var ex = slip.AddComponent<Examinable>();

            var sprCal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Clues/T_Clue_Calendar.jpg");
            SetExData(ex, "Clue_RoomService", "ROOM SERVICE ORDER - UNIT 210",
                "MARROW POINT RESIDENCES - ROOM SERVICE LOG\n\nUNIT: 210\nREQUEST: Continental breakfast at 7:00 AM.\n\nSPECIAL INSTRUCTION: 'Do not knock before dawn. Elias down the hall was screaming about the heat again. Please keep the corridor quiet.'\n\n[VOIDED - OCT 14, 1978]",
                sprCal, false, "[E] Inspect Room Service Slip");
        }

        private static void BuildRoom212(GameObject envRoot, Material suiteWallMat, Material redCarpetMat, Material ceilingMat, Material navyCouchMat, Material woodMat, Material paperMat)
        {
            var oldRoom = GameObject.Find("Room_212");
            if (oldRoom != null) Object.DestroyImmediate(oldRoom);

            GameObject room = new GameObject("Room_212");
            room.transform.SetParent(envRoot.transform);

            float cx = -3.7f; float cz = 14.0f; float rw = 5.0f; float rl = 5.0f; float rh = 2.8f;

            // Red Carpet Floor & Ceiling
            CreateBox(room.transform, "Floor", new Vector3(cx, -0.05f, cz), new Vector3(rw, 0.1f, rl), redCarpetMat);
            CreateBox(room.transform, "Ceiling", new Vector3(cx, rh + 0.05f, cz), new Vector3(rw, 0.1f, rl), ceilingMat);

            // Domestic Suite Walls
            CreateBox(room.transform, "Wall_West", new Vector3(cx - rw/2f, rh/2f, cz), new Vector3(0.1f, rh, rl), suiteWallMat);
            CreateBox(room.transform, "Wall_North", new Vector3(cx, rh/2f, cz + rl/2f), new Vector3(rw, rh, 0.1f), suiteWallMat);
            CreateBox(room.transform, "Wall_South", new Vector3(cx, rh/2f, cz - rl/2f), new Vector3(rw, rh, 0.1f), suiteWallMat);

            // Navy Blue Vintage Couch (Matching Image 1 reference brief)
            GameObject couch = new GameObject("NavyCouch");
            couch.transform.SetParent(room.transform);
            couch.transform.position = new Vector3(-5.0f, 0f, 14.0f);
            CreateBox(couch.transform, "Base", new Vector3(-5.0f, 0.28f, 14.0f), new Vector3(1.1f, 0.45f, 2.3f), navyCouchMat);
            CreateBox(couch.transform, "Backrest", new Vector3(-5.45f, 0.7f, 14.0f), new Vector3(0.3f, 0.85f, 2.3f), navyCouchMat);
            CreateBox(couch.transform, "Arm_L", new Vector3(-4.95f, 0.5f, 12.9f), new Vector3(1.0f, 0.55f, 0.25f), navyCouchMat);
            CreateBox(couch.transform, "Arm_R", new Vector3(-4.95f, 0.5f, 15.1f), new Vector3(1.0f, 0.55f, 0.25f), navyCouchMat);

            // Wooden Coffee Table
            CreateBox(room.transform, "CoffeeTable", new Vector3(-3.4f, 0.25f, 14.0f), new Vector3(0.75f, 0.45f, 1.3f), woodMat);

            // Floor Lamp with soft safe glow
            GameObject floorLamp = CreateBox(room.transform, "FloorLamp", new Vector3(-5.5f, 1.1f, 15.8f), new Vector3(0.35f, 2.1f, 0.35f), woodMat);
            GameObject flLight = new GameObject("LampLight");
            flLight.transform.SetParent(floorLamp.transform);
            flLight.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            var fl = flLight.AddComponent<Light>();
            fl.type = LightType.Point;
            fl.range = 6.0f;
            fl.intensity = 1.8f;
            fl.color = new Color(1.0f, 0.91f, 0.76f);

            // Clue: Folded Newspaper on coffee table
            GameObject news = GameObject.CreatePrimitive(PrimitiveType.Quad);
            news.name = "Clue_Newspaper";
            news.transform.SetParent(room.transform);
            news.transform.position = new Vector3(-3.4f, 0.48f, 14.0f);
            news.transform.rotation = Quaternion.Euler(90f, -10f, 0f);
            news.transform.localScale = new Vector3(0.35f, 0.45f, 1f);
            news.GetComponent<Renderer>().sharedMaterial = paperMat;
            var ex = news.AddComponent<Examinable>();

            var sprInc = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Clues/T_Clue_IncidentReport1.jpg");
            SetExData(ex, "Clue_Newspaper", "THE MARROW COURIER - OCT 1978",
                "THE MARROW COURIER - OCTOBER 12, 1978\n\n'TENANT PETITION DEMANDS ELEVATOR & ALARM OVERHAUL'\n\nResidents across floors 2 and 3 have formally submitted grievances regarding the erratic elevator descent patterns and fire alarm testing scheduled late past midnight.\n\nSuperintendent Vance commented: 'All automated protocols are designed for resident security during smoke damper maintenance.'",
                sprInc, false, "[E] Read Newspaper");
        }

        private static void BuildRoom216(GameObject envRoot, Material utilityWallMat, Material floorMat, Material ceilingMat, Material shelvingMat, Material metalMat)
        {
            var oldRoom = GameObject.Find("Room_216");
            if (oldRoom != null) Object.DestroyImmediate(oldRoom);

            GameObject room = new GameObject("Room_216");
            room.transform.SetParent(envRoot.transform);

            float cx = 3.5f; float cz = 18.0f; float rw = 4.6f; float rl = 4.4f; float rh = 2.8f;

            // Concrete Floor & Ceiling
            CreateBox(room.transform, "Floor", new Vector3(cx, -0.05f, cz), new Vector3(rw, 0.1f, rl), floorMat);
            CreateBox(room.transform, "Ceiling", new Vector3(cx, rh + 0.05f, cz), new Vector3(rw, 0.1f, rl), ceilingMat);

            // Cinderblock Walls
            CreateBox(room.transform, "Wall_East", new Vector3(cx + rw/2f, rh/2f, cz), new Vector3(0.1f, rh, rl), utilityWallMat);
            CreateBox(room.transform, "Wall_North", new Vector3(cx, rh/2f, cz + rl/2f), new Vector3(rw, rh, 0.1f), utilityWallMat);
            CreateBox(room.transform, "Wall_South", new Vector3(cx, rh/2f, cz - rl/2f), new Vector3(rw, rh, 0.1f), utilityWallMat);

            // Overhead Steam Pipes running along ceiling
            for (int p = 0; p < 3; p++)
            {
                var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pipe.name = $"OverheadPipe_{p}";
                pipe.transform.SetParent(room.transform);
                pipe.transform.position = new Vector3(cx, rh - 0.2f - (p * 0.18f), cz - 1.2f + (p * 1.2f));
                pipe.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                pipe.transform.localScale = new Vector3(0.12f, rw / 2f, 0.12f);
                pipe.GetComponent<Renderer>().sharedMaterial = metalMat;
                Object.DestroyImmediate(pipe.GetComponent<Collider>());
            }

            // Metal Utility Shelving Units
            CreateBox(room.transform, "Shelving_North", new Vector3(3.2f, 1.1f, cz + 1.85f), new Vector3(2.4f, 2.2f, 0.55f), shelvingMat);
            CreateBox(room.transform, "Shelving_South", new Vector3(3.2f, 1.1f, cz - 1.85f), new Vector3(2.4f, 2.2f, 0.55f), shelvingMat);

            // High-Voltage Breaker Panel on back wall (X = cx + rw/2f - 0.05)
            GameObject breaker = CreateBox(room.transform, "CircuitBreakerPanel", new Vector3(5.65f, 1.45f, cz), new Vector3(0.12f, 1.2f, 0.85f), metalMat);

            // Single Bare Hanging Bulb
            GameObject bulbWire = CreateBox(room.transform, "BulbWire", new Vector3(cx, rh - 0.35f, cz), new Vector3(0.02f, 0.7f, 0.02f), metalMat);
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "BareBulb";
            bulb.transform.SetParent(room.transform);
            bulb.transform.position = new Vector3(cx, rh - 0.72f, cz);
            bulb.transform.localScale = new Vector3(0.14f, 0.18f, 0.14f);
            bulb.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Light_Emissive.mat");
            Object.DestroyImmediate(bulb.GetComponent<Collider>());

            GameObject bulbLightObj = new GameObject("BulbLight");
            bulbLightObj.transform.SetParent(bulb.transform);
            bulbLightObj.transform.localPosition = Vector3.zero;
            var bl = bulbLightObj.AddComponent<Light>();
            bl.type = LightType.Point;
            bl.range = 6.0f;
            bl.intensity = 1.4f;
            bl.color = new Color(1.0f, 0.85f, 0.60f);

            // Observer Doorway Jump Scare Spot
            GameObject obsSpot = new GameObject("ObserverJumpScareSpot_216");
            obsSpot.transform.SetParent(room.transform);
            obsSpot.transform.position = new Vector3(1.65f, 0f, cz);
            obsSpot.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Facing into Room 216

            // Trigger Volume for Jump Scare
            GameObject trigObj = new GameObject("UtilityRoom_JumpScareTrigger");
            trigObj.transform.SetParent(room.transform);
            trigObj.transform.position = new Vector3(4.2f, 1.0f, cz);
            var col = trigObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.8f, 2.0f, 2.5f);

            var js = trigObj.AddComponent<UtilityRoomJumpScareTrigger>();
            var fBulb = typeof(UtilityRoomJumpScareTrigger).GetField("utilityBulbLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fBulb != null) fBulb.SetValue(js, bl);
            var fSpot = typeof(UtilityRoomJumpScareTrigger).GetField("observerDoorwaySpot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fSpot != null) fSpot.SetValue(js, obsSpot.transform);
            var fClip = typeof(UtilityRoomJumpScareTrigger).GetField("jumpScareClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var jsClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/sfx_jumpscare.wav");
            if (fClip != null && jsClip != null) fClip.SetValue(js, jsClip);
        }

        private static void BuildStairwellLanding(GameObject envRoot, Material wallMat, Material floorMat, Material ceilingMat, Material trimMat, Material stairWoodMat)
        {
            var oldLanding = GameObject.Find("StairwellLanding");
            if (oldLanding != null) Object.DestroyImmediate(oldLanding);

            GameObject landing = new GameObject("StairwellLanding");
            landing.transform.SetParent(envRoot.transform);

            float cx = 0.0f; float cz = 27.5f; float rw = 5.2f; float rl = 7.0f; float rh = 5.5f;

            // Floor & Ceiling (High ceiling disappearing into darkness)
            CreateBox(landing.transform, "Floor", new Vector3(cx, -0.05f, cz), new Vector3(rw, 0.1f, rl), floorMat);
            CreateBox(landing.transform, "Ceiling", new Vector3(cx, rh + 0.05f, cz), new Vector3(rw, 0.1f, rl), ceilingMat);

            // Walls (Two-tone mustard & teal matching Reference Image 0)
            CreateBox(landing.transform, "Wall_Left_West", new Vector3(cx - rw/2f, rh/2f, cz), new Vector3(0.1f, rh, rl), wallMat);
            CreateBox(landing.transform, "Wall_Right_East", new Vector3(cx + rw/2f, rh/2f, cz), new Vector3(0.1f, rh, rl), wallMat);
            CreateBox(landing.transform, "Wall_Far_North", new Vector3(cx, rh/2f, cz + rl/2f), new Vector3(rw, rh, 0.1f), wallMat);

            // Waist-height dark wood trim strip along left wall (matching photo)
            CreateBox(landing.transform, "Dado_Rail_Left", new Vector3(cx - rw/2f + 0.03f, 1.15f, cz), new Vector3(0.05f, 0.08f, rl), trimMat);

            // The Wooden Staircase ascending along East Wall (Z = 25.5 to 30.5, Y = 0 to 4.2)
            GameObject stairsGroup = new GameObject("Stairs");
            stairsGroup.transform.SetParent(landing.transform);

            int numSteps = 16;
            float stepRise = 0.22f;
            float stepRun = 0.30f;
            float stairWidth = 1.35f;
            float startZ = 25.2f;
            float stairX = cx + rw/2f - (stairWidth / 2f);

            for (int i = 0; i < numSteps; i++)
            {
                float stepY = (i + 0.5f) * stepRise;
                float stepZ = startZ + (i * stepRun);
                var tread = CreateBox(stairsGroup.transform, $"Step_{i}", new Vector3(stairX, stepY, stepZ), new Vector3(stairWidth, stepRise, stepRun), stairWoodMat);
            }

            // Newel Post at base of stairs (with sphere cap matching photo)
            float newelX = stairX - (stairWidth / 2f) + 0.05f;
            float newelZ = startZ;
            GameObject post = CreateBox(stairsGroup.transform, "NewelPost", new Vector3(newelX, 0.55f, newelZ), new Vector3(0.12f, 1.1f, 0.12f), stairWoodMat);

            GameObject sphereCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereCap.name = "SphereCap";
            sphereCap.transform.SetParent(post.transform);
            sphereCap.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            sphereCap.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            sphereCap.GetComponent<Renderer>().sharedMaterial = stairWoodMat;
            Object.DestroyImmediate(sphereCap.GetComponent<Collider>());

            // Banister Handrail ascending with stairs
            GameObject handrail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handrail.name = "Handrail";
            handrail.transform.SetParent(stairsGroup.transform);
            float railLen = Mathf.Sqrt(Mathf.Pow(numSteps * stepRun, 2) + Mathf.Pow(numSteps * stepRise, 2));
            float railAngle = Mathf.Atan2(numSteps * stepRise, numSteps * stepRun) * Mathf.Rad2Deg;
            handrail.transform.position = new Vector3(newelX, (numSteps * stepRise) / 2f + 0.9f, startZ + (numSteps * stepRun) / 2f);
            handrail.transform.rotation = Quaternion.Euler(railAngle, 0f, 0f);
            handrail.transform.localScale = new Vector3(0.08f, 0.08f, railLen);
            handrail.GetComponent<Renderer>().sharedMaterial = stairWoodMat;

            // Spindles / Balusters
            for (int b = 1; b < numSteps; b += 2)
            {
                float bY = (b * stepRise) + 0.45f;
                float bZ = startZ + (b * stepRun);
                CreateBox(stairsGroup.transform, $"Baluster_{b}", new Vector3(newelX, bY, bZ), new Vector3(0.04f, 0.9f, 0.04f), stairWoodMat);
            }

            // Paintings on Left Wall (Matching Reference Image 0):
            // 1. The Cubist Wheel-Ear Clown portrait
            Material matClown = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Paintings/M_Painting_ClownTires.mat");
            if (matClown == null) matClown = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Paintings/M_Painting_Stage0.mat");
            CreateFramedPainting(landing.transform, "Painting_Clown_Image0", new Vector3(cx - rw/2f + 0.04f, 1.9f, 26.0f), new Vector3(1.1f, 1.1f, 0.04f), matClown, trimMat);

            // 2. The Minimalist Penguin portrait hung slightly askew
            Material matPenguin = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Paintings/M_Painting_Penguin.mat");
            if (matPenguin != null)
            {
                var pObj = CreateFramedPainting(landing.transform, "Painting_Penguin_Image0", new Vector3(cx - rw/2f + 0.04f, 2.1f, 28.5f), new Vector3(0.7f, 0.85f, 0.04f), matPenguin, trimMat);
                pObj.transform.localRotation = Quaternion.Euler(0f, 90f, -3.5f);
            }

            // Landing Light (Warm point light angled to create dramatic falloff like Image 0)
            GameObject landingLightObj = new GameObject("LandingLight");
            landingLightObj.transform.SetParent(landing.transform);
            landingLightObj.transform.position = new Vector3(cx - 0.8f, 3.2f, 26.5f);
            var ll = landingLightObj.AddComponent<Light>();
            ll.type = LightType.Point;
            ll.range = 8.5f;
            ll.intensity = 1.8f;
            ll.color = new Color(1.0f, 0.88f, 0.65f); // warm 3200K
            ll.shadows = LightShadows.Soft;

            // Far hallway observer spawn at base of stairs
            var spawnRoot = GameObject.Find("SpawnPoints");
            if (spawnRoot != null)
            {
                var oldFar = GameObject.Find("ObserverSpawn_FarHallway");
                if (oldFar != null)
                {
                    oldFar.transform.position = new Vector3(cx, 0f, 25.5f);
                }
            }
        }

        private static GameObject CreateFramedPainting(Transform parent, string name, Vector3 pos, Vector3 scale, Material canvasMat, Material frameMat)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.position = pos;
            group.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // Frame
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(group.transform, false);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = scale;
            frame.GetComponent<Renderer>().sharedMaterial = frameMat;

            // Canvas
            var canvas = GameObject.CreatePrimitive(PrimitiveType.Quad);
            canvas.name = "Canvas";
            canvas.transform.SetParent(group.transform, false);
            canvas.transform.localPosition = new Vector3(0f, 0f, -scale.z / 2f - 0.005f);
            canvas.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvas.transform.localScale = new Vector3(scale.x * 0.88f, scale.y * 0.88f, 1f);
            canvas.GetComponent<Renderer>().sharedMaterial = canvasMat;
            Object.DestroyImmediate(canvas.GetComponent<Collider>());

            return group;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = pos;
            box.transform.localScale = scale;
            if (mat != null) box.GetComponent<Renderer>().sharedMaterial = mat;
            return box;
        }

        private static void SetExData(Examinable ex, string id, string title, string body, Sprite sprite, bool warm, string prompt)
        {
            var fId = typeof(Examinable).GetField("clueId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fTitle = typeof(Examinable).GetField("title", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fBody = typeof(Examinable).GetField("documentText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fSprite = typeof(Examinable).GetField("documentSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fWarm = typeof(Examinable).GetField("isWarmToTouch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fPrompt = typeof(Examinable).GetField("promptText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fId != null) fId.SetValue(ex, id);
            if (fTitle != null) fTitle.SetValue(ex, title);
            if (fBody != null) fBody.SetValue(ex, body);
            if (fSprite != null && sprite != null) fSprite.SetValue(ex, sprite);
            if (fWarm != null) fWarm.SetValue(ex, warm);
            if (fPrompt != null) fPrompt.SetValue(ex, prompt);

            EditorUtility.SetDirty(ex);
        }
    }
}
