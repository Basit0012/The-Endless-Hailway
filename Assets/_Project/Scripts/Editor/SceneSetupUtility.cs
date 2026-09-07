using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using EndlessHallway.Core;
using EndlessHallway.Player;
using EndlessHallway.Interaction;
using EndlessHallway.Anomaly;
using EndlessHallway.Audio;
using EndlessHallway.UI;
using EndlessHallway.Entity;

namespace EndlessHallway.Editor
{
    public static class SceneSetupUtility
    {
        [MenuItem("Tools/Endless Hallway/1. Generate Audio Clips")]
        public static void GenerateAudioClips()
        {
            string audioPath = Application.dataPath + "/_Project/Audio";
            if (!Directory.Exists(audioPath))
            {
                Directory.CreateDirectory(audioPath);
            }

            int sr = 44100;

            // 1. Footstep
            int footLen = (int)(sr * 0.15f);
            float[] footSamples = new float[footLen];
            for (int i = 0; i < footLen; i++)
            {
                float t = (float)i / footLen;
                float env = Mathf.Exp(-t * 25f);
                float noise = (float)(new System.Random(i).NextDouble() * 2.0 - 1.0);
                float tone = Mathf.Sin(2f * Mathf.PI * 80f * ((float)i / sr));
                footSamples[i] = (tone * 0.7f + noise * 0.3f) * env * 0.5f;
            }
            WriteWavFile(audioPath + "/sfx_footstep.wav", footSamples, sr);

            // 2. Door Open
            int openLen = (int)(sr * 0.4f);
            float[] openSamples = new float[openLen];
            for (int i = 0; i < openLen; i++)
            {
                float t = (float)i / openLen;
                float env = Mathf.Sin(t * Mathf.PI);
                float creak = Mathf.Sin(2f * Mathf.PI * (180f + t * 60f) * ((float)i / sr));
                float click = (i < sr * 0.05f) ? Mathf.Sin(2f * Mathf.PI * 600f * ((float)i / sr)) * Mathf.Exp(-((float)i / sr) * 80f) : 0f;
                openSamples[i] = (creak * 0.4f * env + click * 0.6f) * 0.6f;
            }
            WriteWavFile(audioPath + "/sfx_door_open.wav", openSamples, sr);

            // 3. Door Close
            int closeLen = (int)(sr * 0.35f);
            float[] closeSamples = new float[closeLen];
            for (int i = 0; i < closeLen; i++)
            {
                float t = (float)i / closeLen;
                float env = Mathf.Exp(-t * 16f);
                float thud = Mathf.Sin(2f * Mathf.PI * 95f * ((float)i / sr));
                float click = Mathf.Sin(2f * Mathf.PI * 750f * ((float)i / sr)) * Mathf.Exp(-t * 40f);
                closeSamples[i] = (thud * 0.7f + click * 0.3f) * env * 0.7f;
            }
            WriteWavFile(audioPath + "/sfx_door_close.wav", closeSamples, sr);

            // 4. Door Rattle
            int rattleLen = (int)(sr * 0.25f);
            float[] rattleSamples = new float[rattleLen];
            for (int i = 0; i < rattleLen; i++)
            {
                float t = (float)i / rattleLen;
                float env = Mathf.Sin(t * Mathf.PI);
                float jitter = Mathf.Sin(2f * Mathf.PI * 320f * ((float)i / sr)) * Mathf.Sin(2f * Mathf.PI * 35f * ((float)i / sr));
                rattleSamples[i] = jitter * env * 0.6f;
            }
            WriteWavFile(audioPath + "/sfx_door_rattle.wav", rattleSamples, sr);

            // 5. Elevator Bell
            int bellLen = (int)(sr * 0.8f);
            float[] bellSamples = new float[bellLen];
            for (int i = 0; i < bellLen; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 4.5f);
                float chime = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1567.98f * t) * 0.3f;
                bellSamples[i] = chime * env * 0.7f;
            }
            WriteWavFile(audioPath + "/sfx_elevator_bell.wav", bellSamples, sr);

            // 6. Elevator Hum
            int humLen = (int)(sr * 1.5f);
            float[] humSamples = new float[humLen];
            for (int i = 0; i < humLen; i++)
            {
                float t = (float)i / sr;
                float motor = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 130f * t) * 0.25f;
                humSamples[i] = motor * 0.4f;
            }
            WriteWavFile(audioPath + "/sfx_elevator_hum.wav", humSamples, sr);

            // 7. Ambient Hallway Drone
            int ambLen = (int)(sr * 3.0f);
            float[] ambSamples = new float[ambLen];
            for (int i = 0; i < ambLen; i++)
            {
                float t = (float)i / sr;
                float drone = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.2f + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.05f;
                ambSamples[i] = drone * 0.35f;
            }
            WriteWavFile(audioPath + "/amb_hallway_drone.wav", ambSamples, sr);

            AssetDatabase.Refresh();
            Debug.Log("[SceneSetupUtility] Audio clips generated successfully.");
        }

        private static void WriteWavFile(string filePath, float[] samples, int sampleRate)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var writer = new BinaryWriter(fileStream))
            {
                int byteRate = sampleRate * 2;
                int dataSize = samples.Length * 2;

                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write((short)1); // mono
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);

                for (int i = 0; i < samples.Length; i++)
                {
                    float s = Mathf.Clamp(samples[i], -1f, 1f);
                    short val = (short)(s * 32767);
                    writer.Write(val);
                }
            }
        }

        [MenuItem("Tools/Endless Hallway/2. Build Main Scene")]
        public static void BuildMainScene()
        {
            string scenePath = "Assets/_Project/Scenes/Main.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load materials
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Wall.mat");
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Floor.mat");
            Material ceilingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Ceiling.mat");
            Material trimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Trim.mat");
            Material doorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door.mat");
            Material door214Mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door_214.mat");
            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Elevator_Metal.mat");
            Material interiorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Elevator_Interior.mat");
            Material emissiveMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Light_Emissive.mat");
            Material matMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_WelcomeMat.mat");
            Material corkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Noticeboard_Cork.mat");
            Material paperMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Paper_Notice.mat");
            Material plateMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door_Plate.mat");

            // Load audio clips
            AudioClip footstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/sfx_footstep.wav");
            AudioClip doorOpenClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/sfx_door_open.wav");
            AudioClip doorCloseClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/sfx_door_close.wav");
            AudioClip doorRattleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/sfx_door_rattle.wav");
            AudioClip bellClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/sfx_elevator_bell.wav");
            AudioClip elevatorHumClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/sfx_elevator_hum.wav");
            AudioClip droneClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/amb_hallway_drone.wav");

            // Root objects
            GameObject envRoot = new GameObject("Environment");
            GameObject propsRoot = new GameObject("Props");
            GameObject managersRoot = new GameObject("Managers");

            // 1. Hallway Geometry
            float hallWidth = 2.4f;
            float hallHeight = 2.8f;
            float hallLength = 24.0f;
            float startZ = 0f;
            float centerZ = startZ + hallLength / 2f;

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Corridor_Floor";
            floor.transform.SetParent(envRoot.transform);
            floor.transform.position = new Vector3(0f, -0.05f, centerZ);
            floor.transform.localScale = new Vector3(hallWidth, 0.1f, hallLength);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Corridor_Ceiling";
            ceiling.transform.SetParent(envRoot.transform);
            ceiling.transform.position = new Vector3(0f, hallHeight + 0.05f, centerZ);
            ceiling.transform.localScale = new Vector3(hallWidth, 0.1f, hallLength);
            ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMat;

            // Left Wall (X = -hallWidth/2)
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "Corridor_LeftWall";
            leftWall.transform.SetParent(envRoot.transform);
            leftWall.transform.position = new Vector3(-hallWidth / 2f - 0.05f, hallHeight / 2f, centerZ);
            leftWall.transform.localScale = new Vector3(0.1f, hallHeight, hallLength);
            leftWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Right Wall (X = hallWidth/2)
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "Corridor_RightWall";
            rightWall.transform.SetParent(envRoot.transform);
            rightWall.transform.position = new Vector3(hallWidth / 2f + 0.05f, hallHeight / 2f, centerZ);
            rightWall.transform.localScale = new Vector3(0.1f, hallHeight, hallLength);
            rightWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // End Wall (Z = hallLength)
            GameObject endWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            endWall.name = "Corridor_EndWall";
            endWall.transform.SetParent(envRoot.transform);
            endWall.transform.position = new Vector3(0f, hallHeight / 2f, hallLength + 0.05f);
            endWall.transform.localScale = new Vector3(hallWidth + 0.2f, hallHeight, 0.1f);
            endWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Baseboard trims
            GameObject trimLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trimLeft.name = "Trim_Left";
            trimLeft.transform.SetParent(envRoot.transform);
            trimLeft.transform.position = new Vector3(-hallWidth / 2f + 0.02f, 0.06f, centerZ);
            trimLeft.transform.localScale = new Vector3(0.04f, 0.12f, hallLength);
            trimLeft.GetComponent<Renderer>().sharedMaterial = trimMat;

            GameObject trimRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trimRight.name = "Trim_Right";
            trimRight.transform.SetParent(envRoot.transform);
            trimRight.transform.position = new Vector3(hallWidth / 2f - 0.02f, 0.06f, centerZ);
            trimRight.transform.localScale = new Vector3(0.04f, 0.12f, hallLength);
            trimRight.GetComponent<Renderer>().sharedMaterial = trimMat;

            // 2. Ceiling Lights
            float[] lightZ = new float[] { 2.5f, 6.5f, 10.5f, 14.5f, 18.5f, 22.0f };
            for (int i = 0; i < lightZ.Length; i++)
            {
                float lz = lightZ[i];
                GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fixture.name = (lz == 10.5f) ? "CeilingLight_214" : $"CeilingLight_{i}";
                fixture.transform.SetParent(envRoot.transform);
                fixture.transform.position = new Vector3(0f, hallHeight - 0.04f, lz);
                fixture.transform.localScale = new Vector3(0.35f, 0.06f, 0.9f);
                fixture.GetComponent<Renderer>().sharedMaterial = emissiveMat;
                Object.DestroyImmediate(fixture.GetComponent<Collider>());

                GameObject pLightObj = new GameObject("PointLight");
                pLightObj.transform.SetParent(fixture.transform);
                pLightObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                Light pl = pLightObj.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.range = 6.5f;
                pl.intensity = 1.2f;
                pl.color = new Color(1.0f, 0.93f, 0.82f);

                if (lz == 10.5f)
                {
                    // Light above Room 214 has AnomalyTarget
                    var target = fixture.AddComponent<AnomalyTarget>();
                    var field = typeof(AnomalyTarget).GetField("targetId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) field.SetValue(target, "Light_214");
                }
            }

            // 3. Doors
            void CreateDoor(string id, string roomNum, float zPos, bool onLeft, bool is214 = false)
            {
                float xWall = onLeft ? -hallWidth / 2f : hallWidth / 2f;
                float angleSign = onLeft ? 1f : -1f;

                GameObject doorGroup = new GameObject(id);
                doorGroup.transform.SetParent(envRoot.transform);
                doorGroup.transform.position = new Vector3(xWall, 0f, zPos);

                // Door Frame
                GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frame.name = "Frame";
                frame.transform.SetParent(doorGroup.transform);
                frame.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                frame.transform.localScale = new Vector3(0.12f, 2.15f, 1.05f);
                frame.GetComponent<Renderer>().sharedMaterial = trimMat;

                // Door Pivot
                GameObject pivot = new GameObject("Pivot");
                pivot.transform.SetParent(doorGroup.transform);
                pivot.transform.localPosition = new Vector3(0f, 0f, -0.45f);

                // Door Slab
                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "DoorSlab";
                slab.transform.SetParent(pivot.transform);
                slab.transform.localPosition = new Vector3(0f, 1.02f, 0.45f);
                slab.transform.localScale = new Vector3(0.06f, 2.05f, 0.9f);
                slab.GetComponent<Renderer>().sharedMaterial = is214 ? door214Mat : doorMat;

                // Door Knob
                GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                knob.name = "Knob";
                knob.transform.SetParent(slab.transform);
                knob.transform.localPosition = new Vector3(-angleSign * 0.6f, -0.05f, 0.35f);
                knob.transform.localScale = new Vector3(0.12f, 0.08f, 0.08f);
                knob.GetComponent<Renderer>().sharedMaterial = metalMat;
                Object.DestroyImmediate(knob.GetComponent<Collider>());

                // Room Number Plate
                GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name = "RoomPlate_" + roomNum;
                plate.transform.SetParent(slab.transform);
                plate.transform.localPosition = new Vector3(-angleSign * 0.55f, 0.45f, 0f);
                plate.transform.localScale = new Vector3(0.02f, 0.12f, 0.22f);
                plate.GetComponent<Renderer>().sharedMaterial = plateMat;
                Object.DestroyImmediate(plate.GetComponent<Collider>());

                // DoorController
                DoorController dc = doorGroup.AddComponent<DoorController>();
                var idField = typeof(DoorController).GetField("doorId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (idField != null) idField.SetValue(dc, id);

                var pivotField = typeof(DoorController).GetField("pivotTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pivotField != null) pivotField.SetValue(dc, pivot.transform);

                var openAngleField = typeof(DoorController).GetField("openAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (openAngleField != null) openAngleField.SetValue(dc, onLeft ? 90f : -90f);

                var openClipField = typeof(DoorController).GetField("openClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (openClipField != null) openClipField.SetValue(dc, doorOpenClip);

                var closeClipField = typeof(DoorController).GetField("closeClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (closeClipField != null) closeClipField.SetValue(dc, doorCloseClip);

                var rattleClipField = typeof(DoorController).GetField("lockedRattleClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (rattleClipField != null) rattleClipField.SetValue(dc, doorRattleClip);

                // Add BoxCollider on slab for raycast interaction
                var slabCol = slab.GetComponent<BoxCollider>();
                if (slabCol == null) slab.AddComponent<BoxCollider>();
            }

            CreateDoor("Door_210", "210", 6.0f, true);
            CreateDoor("Door_212", "212", 14.0f, true);
            CreateDoor("Door_214", "214", 10.0f, false, true);
            CreateDoor("Door_216", "216", 18.0f, false);

            // 4. Welcome Mat outside Room 214
            GameObject matObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            matObj.name = "Mat_214";
            matObj.transform.SetParent(propsRoot.transform);
            matObj.transform.position = new Vector3(hallWidth / 2f - 0.45f, 0.015f, 10.0f);
            matObj.transform.localScale = new Vector3(0.55f, 0.02f, 0.85f);
            matObj.GetComponent<Renderer>().sharedMaterial = matMat;
            Object.DestroyImmediate(matObj.GetComponent<Collider>());

            var matTarget = matObj.AddComponent<AnomalyTarget>();
            var matIdField = typeof(AnomalyTarget).GetField("targetId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (matIdField != null) matIdField.SetValue(matTarget, "Mat_214");

            // 5. Cork Noticeboard near elevator (Z = 2.0m on right wall)
            GameObject boardObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardObj.name = "CorkNoticeboard";
            boardObj.transform.SetParent(propsRoot.transform);
            boardObj.transform.position = new Vector3(hallWidth / 2f - 0.02f, 1.45f, 2.2f);
            boardObj.transform.localScale = new Vector3(0.04f, 0.8f, 1.2f);
            boardObj.GetComponent<Renderer>().sharedMaterial = corkMat;

            // Fire Drill Notice pinned on board
            GameObject noticePaper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            noticePaper.name = "FireDrillNotice";
            noticePaper.transform.SetParent(boardObj.transform);
            noticePaper.transform.localPosition = new Vector3(-0.55f, 0.05f, 0.1f);
            noticePaper.transform.localScale = new Vector3(0.2f, 0.55f, 0.35f);
            noticePaper.GetComponent<Renderer>().sharedMaterial = paperMat;

            Examinable examinable = boardObj.AddComponent<Examinable>();

            // 6. Elevator
            GameObject elevatorRoot = new GameObject("Elevator");
            elevatorRoot.transform.SetParent(envRoot.transform);
            elevatorRoot.transform.position = new Vector3(0f, 0f, 0f);

            // Elevator Cabin
            GameObject cabinFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinFloor.name = "Cabin_Floor";
            cabinFloor.transform.SetParent(elevatorRoot.transform);
            cabinFloor.transform.position = new Vector3(0f, -0.05f, -1.3f);
            cabinFloor.transform.localScale = new Vector3(2.2f, 0.1f, 2.4f);
            cabinFloor.GetComponent<Renderer>().sharedMaterial = interiorMat;

            GameObject cabinCeiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinCeiling.name = "Cabin_Ceiling";
            cabinCeiling.transform.SetParent(elevatorRoot.transform);
            cabinCeiling.transform.position = new Vector3(0f, hallHeight + 0.05f, -1.3f);
            cabinCeiling.transform.localScale = new Vector3(2.2f, 0.1f, 2.4f);
            cabinCeiling.GetComponent<Renderer>().sharedMaterial = interiorMat;

            GameObject cabinBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinBack.name = "Cabin_BackWall";
            cabinBack.transform.SetParent(elevatorRoot.transform);
            cabinBack.transform.position = new Vector3(0f, hallHeight / 2f, -2.55f);
            cabinBack.transform.localScale = new Vector3(2.2f, hallHeight, 0.1f);
            cabinBack.GetComponent<Renderer>().sharedMaterial = interiorMat;

            GameObject cabinLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinLeft.name = "Cabin_LeftWall";
            cabinLeft.transform.SetParent(elevatorRoot.transform);
            cabinLeft.transform.position = new Vector3(-1.15f, hallHeight / 2f, -1.3f);
            cabinLeft.transform.localScale = new Vector3(0.1f, hallHeight, 2.4f);
            cabinLeft.GetComponent<Renderer>().sharedMaterial = interiorMat;

            GameObject cabinRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinRight.name = "Cabin_RightWall";
            cabinRight.transform.SetParent(elevatorRoot.transform);
            cabinRight.transform.position = new Vector3(1.15f, hallHeight / 2f, -1.3f);
            cabinRight.transform.localScale = new Vector3(0.1f, hallHeight, 2.4f);
            cabinRight.GetComponent<Renderer>().sharedMaterial = interiorMat;

            // Elevator light
            GameObject elevLightObj = new GameObject("ElevatorLight");
            elevLightObj.transform.SetParent(elevatorRoot.transform);
            elevLightObj.transform.position = new Vector3(0f, hallHeight - 0.2f, -1.3f);
            Light elevLight = elevLightObj.AddComponent<Light>();
            elevLight.type = LightType.Point;
            elevLight.range = 5f;
            elevLight.intensity = 1.0f;
            elevLight.color = new Color(0.9f, 0.95f, 1.0f);

            // Sliding Doors (Z = 0)
            GameObject leftDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftDoor.name = "ElevatorDoor_Left";
            leftDoor.transform.SetParent(elevatorRoot.transform);
            leftDoor.transform.position = new Vector3(-1.45f, 1.15f, 0.05f);
            leftDoor.transform.localScale = new Vector3(1.1f, 2.3f, 0.06f);
            leftDoor.GetComponent<Renderer>().sharedMaterial = metalMat;

            GameObject rightDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightDoor.name = "ElevatorDoor_Right";
            rightDoor.transform.SetParent(elevatorRoot.transform);
            rightDoor.transform.position = new Vector3(1.45f, 1.15f, 0.05f);
            rightDoor.transform.localScale = new Vector3(1.1f, 2.3f, 0.06f);
            rightDoor.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Elevator Button / Control Panel
            GameObject buttonPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonPanel.name = "ElevatorButtonPanel";
            buttonPanel.transform.SetParent(elevatorRoot.transform);
            buttonPanel.transform.position = new Vector3(0.95f, 1.2f, -0.6f);
            buttonPanel.transform.localScale = new Vector3(0.06f, 0.35f, 0.2f);
            buttonPanel.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Spawn point inside elevator
            GameObject spawnPtObj = new GameObject("PlayerSpawnPoint_Elevator");
            spawnPtObj.transform.SetParent(elevatorRoot.transform);
            spawnPtObj.transform.position = new Vector3(0f, 1.0f, -1.4f);
            spawnPtObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            // ElevatorController
            ElevatorController elevCtrl = buttonPanel.AddComponent<ElevatorController>();

            var lDoorField = typeof(ElevatorController).GetField("leftDoor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lDoorField != null) lDoorField.SetValue(elevCtrl, leftDoor.transform);

            var rDoorField = typeof(ElevatorController).GetField("rightDoor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (rDoorField != null) rDoorField.SetValue(elevCtrl, rightDoor.transform);

            var spawnField = typeof(ElevatorController).GetField("playerSpawnPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (spawnField != null) spawnField.SetValue(elevCtrl, spawnPtObj.transform);

            var bellField = typeof(ElevatorController).GetField("bellDingClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bellField != null) bellField.SetValue(elevCtrl, bellClip);

            var humField = typeof(ElevatorController).GetField("elevatorRideHumClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (humField != null) humField.SetValue(elevCtrl, elevatorHumClip);

            // 7. Observer Spawn Points
            GameObject obsPointFar = new GameObject("ObserverSpawn_FarHallway");
            obsPointFar.transform.SetParent(propsRoot.transform);
            obsPointFar.transform.position = new Vector3(0f, 0f, 22.5f);
            obsPointFar.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var spFar = obsPointFar.AddComponent<ObserverSpawnPoint>();

            // Observer Controller
            GameObject observerObj = new GameObject("ObserverEntity");
            observerObj.transform.SetParent(propsRoot.transform);
            observerObj.transform.position = new Vector3(0f, 0f, 22.5f);
            GameObject obsVis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obsVis.name = "ObserverSilhouette";
            obsVis.transform.SetParent(observerObj.transform);
            obsVis.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            obsVis.transform.localScale = new Vector3(0.55f, 0.95f, 0.4f);
            obsVis.GetComponent<Renderer>().sharedMaterial = trimMat; // Shadowy dark silhouette
            var obsCtrl = observerObj.AddComponent<ObserverController>();
            var obsVisField = typeof(ObserverController).GetField("visualRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (obsVisField != null) obsVisField.SetValue(obsCtrl, obsVis);
            var obsColField = typeof(ObserverController).GetField("observerCollider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (obsColField != null) obsColField.SetValue(obsCtrl, obsVis.GetComponent<Collider>());

            // 8. Player Setup
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(0f, 1.0f, 1.2f);
            playerObj.transform.rotation = Quaternion.identity;

            CharacterController cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0f, 0f);

            PlayerController pc = playerObj.AddComponent<PlayerController>();
            if (footstepClip != null)
            {
                var stepField = typeof(PlayerController).GetField("footstepClips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (stepField != null) stepField.SetValue(pc, new AudioClip[] { footstepClip });
            }

            GameObject camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(playerObj.transform);
            camHolder.transform.localPosition = new Vector3(0f, 0.7f, 0f);

            var camHolderField = typeof(PlayerController).GetField("cameraHolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (camHolderField != null) camHolderField.SetValue(pc, camHolder.transform);

            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(camHolder.transform);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;
            Camera cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            camObj.AddComponent<AudioListener>();

            PlayerCameraLook look = playerObj.AddComponent<PlayerCameraLook>();
            var bodyField = typeof(PlayerCameraLook).GetField("playerBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bodyField != null) bodyField.SetValue(look, playerObj.transform);
            var camField = typeof(PlayerCameraLook).GetField("cameraTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (camField != null) camField.SetValue(look, camObj.transform);

            PlayerInteraction interaction = playerObj.AddComponent<PlayerInteraction>();
            var rayOriginField = typeof(PlayerInteraction).GetField("rayOrigin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (rayOriginField != null) rayOriginField.SetValue(interaction, camObj.transform);

            // 9. UI Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Reticle Dot
            GameObject reticleObj = new GameObject("Reticle");
            reticleObj.transform.SetParent(canvasObj.transform, false);
            Image reticleImg = reticleObj.AddComponent<Image>();
            reticleImg.color = new Color(1f, 1f, 1f, 0.65f);
            reticleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 5f);

            // Prompt Container
            GameObject promptContainer = new GameObject("PromptContainer");
            promptContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform promptRect = promptContainer.AddComponent<RectTransform>();
            promptRect.anchoredPosition = new Vector2(0f, -40f);
            promptRect.sizeDelta = new Vector2(400f, 40f);

            GameObject promptTextObj = new GameObject("PromptText");
            promptTextObj.transform.SetParent(promptContainer.transform, false);
            TextMeshProUGUI promptTmp = promptTextObj.AddComponent<TextMeshProUGUI>();
            promptTmp.fontSize = 20;
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.text = "";
            promptTmp.color = Color.white;
            promptTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 40f);

            PromptUI promptUI = canvasObj.AddComponent<PromptUI>();
            var reticleField = typeof(PromptUI).GetField("reticleObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (reticleField != null) reticleField.SetValue(promptUI, reticleObj);
            var pTextField = typeof(PromptUI).GetField("promptText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pTextField != null) pTextField.SetValue(promptUI, promptTmp);
            var pContainerField = typeof(PromptUI).GetField("promptContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pContainerField != null) pContainerField.SetValue(promptUI, promptContainer);

            // Screen Fade Panel
            GameObject fadeObj = new GameObject("FadePanel");
            fadeObj.transform.SetParent(canvasObj.transform, false);
            RectTransform fadeRect = fadeObj.AddComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            Image fadeImg = fadeObj.AddComponent<Image>();
            fadeImg.color = Color.black;
            fadeObj.AddComponent<CanvasGroup>();
            fadeObj.AddComponent<FadeController>();

            // Examine Modal Panel
            GameObject examinePanel = new GameObject("ExaminePanel");
            examinePanel.transform.SetParent(canvasObj.transform, false);
            RectTransform exRect = examinePanel.AddComponent<RectTransform>();
            exRect.anchorMin = new Vector2(0.2f, 0.15f);
            exRect.anchorMax = new Vector2(0.8f, 0.85f);
            exRect.offsetMin = Vector2.zero;
            exRect.offsetMax = Vector2.zero;
            Image exBg = examinePanel.AddComponent<Image>();
            exBg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

            GameObject exTitleObj = new GameObject("Title");
            exTitleObj.transform.SetParent(examinePanel.transform, false);
            TextMeshProUGUI exTitle = exTitleObj.AddComponent<TextMeshProUGUI>();
            exTitle.fontSize = 24;
            exTitle.fontStyle = FontStyles.Bold;
            exTitle.alignment = TextAlignmentOptions.Center;
            exTitle.color = new Color(0.95f, 0.85f, 0.6f);
            RectTransform exTitleRect = exTitleObj.GetComponent<RectTransform>();
            exTitleRect.anchorMin = new Vector2(0.05f, 0.85f);
            exTitleRect.anchorMax = new Vector2(0.95f, 0.98f);
            exTitleRect.offsetMin = Vector2.zero;
            exTitleRect.offsetMax = Vector2.zero;

            GameObject exBodyObj = new GameObject("Body");
            exBodyObj.transform.SetParent(examinePanel.transform, false);
            TextMeshProUGUI exBody = exBodyObj.AddComponent<TextMeshProUGUI>();
            exBody.fontSize = 18;
            exBody.color = new Color(0.9f, 0.9f, 0.9f);
            exBody.alignment = TextAlignmentOptions.TopLeft;
            RectTransform exBodyRect = exBodyObj.GetComponent<RectTransform>();
            exBodyRect.anchorMin = new Vector2(0.08f, 0.15f);
            exBodyRect.anchorMax = new Vector2(0.92f, 0.82f);
            exBodyRect.offsetMin = Vector2.zero;
            exBodyRect.offsetMax = Vector2.zero;

            GameObject exCloseHint = new GameObject("CloseHint");
            exCloseHint.transform.SetParent(examinePanel.transform, false);
            TextMeshProUGUI exHint = exCloseHint.AddComponent<TextMeshProUGUI>();
            exHint.fontSize = 14;
            exHint.color = new Color(0.6f, 0.6f, 0.6f);
            exHint.alignment = TextAlignmentOptions.Center;
            exHint.text = "Press [E] or [Esc] to Close";
            RectTransform exHintRect = exCloseHint.GetComponent<RectTransform>();
            exHintRect.anchorMin = new Vector2(0.1f, 0.02f);
            exHintRect.anchorMax = new Vector2(0.9f, 0.12f);
            exHintRect.offsetMin = Vector2.zero;
            exHintRect.offsetMax = Vector2.zero;

            ExamineUI exUI = canvasObj.AddComponent<ExamineUI>();
            var pRootField = typeof(ExamineUI).GetField("panelRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pRootField != null) pRootField.SetValue(exUI, examinePanel);
            var titleField = typeof(ExamineUI).GetField("titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (titleField != null) titleField.SetValue(exUI, exTitle);
            var bodyField2 = typeof(ExamineUI).GetField("bodyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bodyField2 != null) bodyField2.SetValue(exUI, exBody);

            examinePanel.SetActive(false);

            // 10. Core Managers Setup
            managersRoot.AddComponent<GameManager>();
            managersRoot.AddComponent<LoopManager>();
            managersRoot.AddComponent<WorldStateResetter>();

            AnomalyManager am = managersRoot.AddComponent<AnomalyManager>();
            // Load loop configs into AnomalyManager
            var l0 = AssetDatabase.LoadAssetAtPath<LoopConfigSO>("Assets/_Project/ScriptableObjects/Loops/Loop_00_Baseline.asset");
            var l1 = AssetDatabase.LoadAssetAtPath<LoopConfigSO>("Assets/_Project/ScriptableObjects/Loops/Loop_01_FirstWrongness.asset");
            var l2 = AssetDatabase.LoadAssetAtPath<LoopConfigSO>("Assets/_Project/ScriptableObjects/Loops/Loop_02_DoorAndLight.asset");
            var configsList = new System.Collections.Generic.List<LoopConfigSO> { l0, l1, l2 };
            var configsField = typeof(AnomalyManager).GetField("loopConfigs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configsField != null) configsField.SetValue(am, configsList);

            AudioManager audioMgr = managersRoot.AddComponent<AudioManager>();
            if (droneClip != null)
            {
                var baseClipField = typeof(AudioManager).GetField("defaultBaseClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (baseClipField != null) baseClipField.SetValue(audioMgr, droneClip);
            }

            managersRoot.AddComponent<OneShotPool>();

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SceneSetupUtility] Main scene built and saved to " + scenePath);
        }
    }
}
