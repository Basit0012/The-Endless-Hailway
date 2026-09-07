using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
    public static class MasterBuildUtility
    {
        [MenuItem("Tools/Endless Hallway/Master Build: Full Game Setup", false, 0)]
        public static void RunMasterBuild()
        {
            Debug.Log("[MasterBuildUtility] === Starting Full Game Setup ===");
            GenerateAllAudioClips();
            GenerateAllClueSprites();
            ConfigurePBRMaterials();
            BuildVolumeProfiles();
            SetupRoom214AndCorridor();
            BuildClueObjectsInScene();
            BuildAllLoopScriptableObjects();
            ConfigureExamineUI();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MasterBuildUtility] === Full Game Setup Complete! Scene is Ready to Play ===");
        }

        #region 1. Audio Generation
        [MenuItem("Tools/Endless Hallway/Audio: Generate Extended SFX", false, 10)]
        public static void GenerateAllAudioClips()
        {
            string audioPath = Application.dataPath + "/_Project/Audio";
            if (!Directory.Exists(audioPath)) Directory.CreateDirectory(audioPath);
            int sr = 44100;

            // 1. Retro Phone Ring (dual frequency 440Hz + 480Hz modulated with 20Hz bell clapper)
            int ringTotal = (int)(sr * 3.0f);
            float[] ringSamples = new float[ringTotal];
            for (int i = 0; i < ringTotal; i++)
            {
                float t = (float)i / sr;
                // Cadence: 1.2s burst, 0.4s pause, 1.2s burst, then quiet
                bool isRinging = (t >= 0.1f && t < 1.1f) || (t >= 1.4f && t < 2.4f);
                if (isRinging)
                {
                    float clapper = Mathf.Sin(2f * Mathf.PI * 20f * t) > 0f ? 1f : 0f;
                    float tone = (Mathf.Sin(2f * Mathf.PI * 440f * t) + Mathf.Sin(2f * Mathf.PI * 480f * t)) * 0.5f;
                    ringSamples[i] = tone * clapper * 0.45f;
                }
                else
                {
                    ringSamples[i] = 0f;
                }
            }
            WriteWav(audioPath + "/sfx_phone_ring.wav", ringSamples, sr);

            // 2. Retro Phone Call Voice / Tape Recording (Aiden's phone call with static and bandpass)
            int callTotal = (int)(sr * 8.0f);
            float[] callSamples = new float[callTotal];
            var rng = new System.Random(42);
            for (int i = 0; i < callTotal; i++)
            {
                float t = (float)i / sr;
                // Telephone line hiss & hum
                float hum = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.05f;
                float crackle = ((float)rng.NextDouble() * 2f - 1f) * 0.02f;
                if (rng.NextDouble() < 0.003) crackle += ((float)rng.NextDouble() * 2f - 1f) * 0.25f;

                // Modulated vocal formant simulator for speech cadence ("...Aiden?... 214 isn't responding... the smoke...")
                float speechEnvelope = 0f;
                if ((t > 0.5f && t < 2.2f) || (t > 2.8f && t < 4.8f) || (t > 5.4f && t < 7.5f))
                {
                    float speechSyllables = Mathf.Sin(2f * Mathf.PI * 3.5f * t) * 0.5f + 0.5f;
                    float formant1 = Mathf.Sin(2f * Mathf.PI * 280f * t);
                    float formant2 = Mathf.Sin(2f * Mathf.PI * 700f * t) * 0.4f;
                    float formant3 = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.2f;
                    speechEnvelope = (formant1 + formant2 + formant3) * speechSyllables * 0.35f;
                }

                callSamples[i] = Mathf.Clamp(hum + crackle + speechEnvelope, -1f, 1f);
            }
            WriteWav(audioPath + "/sfx_phone_call.wav", callSamples, sr);

            // 3. Observer Vanish Stinger (cold reverse whoosh + sub-bass drop)
            int stingerTotal = (int)(sr * 1.5f);
            float[] stingerSamples = new float[stingerTotal];
            for (int i = 0; i < stingerTotal; i++)
            {
                float t = (float)i / stingerTotal; // 0 to 1
                float timeSec = (float)i / sr;
                // Reverse swell peaking at 0.7, then sub-bass tail
                float env = t < 0.7f ? Mathf.Pow(t / 0.7f, 3f) : Mathf.Exp(-(t - 0.7f) * 8f);
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.35f;
                float subBass = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 40f, t) * timeSec) * 0.65f;
                stingerSamples[i] = (noise + subBass) * env * 0.6f;
            }
            WriteWav(audioPath + "/sfx_observer_vanish.wav", stingerSamples, sr);

            // 4. Paper Rustle
            int paperTotal = (int)(sr * 0.4f);
            float[] paperSamples = new float[paperTotal];
            for (int i = 0; i < paperTotal; i++)
            {
                float t = (float)i / paperTotal;
                float env = Mathf.Sin(t * Mathf.PI);
                float noise = ((float)rng.NextDouble() * 2f - 1f);
                paperSamples[i] = noise * env * 0.25f;
            }
            WriteWav(audioPath + "/sfx_paper_rustle.wav", paperSamples, sr);

            AssetDatabase.Refresh();
            Debug.Log("[MasterBuildUtility] Extended audio clips generated.");
        }

        private static void WriteWav(string filePath, float[] samples, int sampleRate)
        {
            using (var fs = new FileStream(filePath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int byteRate = sampleRate * 2;
                int dataSize = samples.Length * 2;
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);
                for (int i = 0; i < samples.Length; i++)
                {
                    short val = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
                    bw.Write(val);
                }
            }
        }
        #endregion

        #region 2. Clue Sprites Generation
        [MenuItem("Tools/Endless Hallway/Clues: Generate Clue Sprites", false, 11)]
        public static void GenerateAllClueSprites()
        {
            string clueDir = "Assets/_Project/Art/Clues";
            if (!Directory.Exists(clueDir)) Directory.CreateDirectory(clueDir);

            // 1. Incident Report 1
            CreateClueTexture(clueDir + "/T_Clue_IncidentReport1.png", 512, 768, new Color(0.92f, 0.90f, 0.82f), true, 
                "INCIDENT REPORT #0414\n" +
                "DATE: OCT 14, 1978\n" +
                "UNIT: 214\n" +
                "TENANT: ELIAS VOSS\n\n" +
                "SUMMARY:\n" +
                "During routine inspection, smoke\n" +
                "damper failed to open.\n" +
                "Tenant attempted repeated contact\n" +
                "with Resident Manager Aiden.\n\n" +
                "[TEXT SMUDGED BY CHARRED ASH]\n" +
                "...no answer at office switchboard.\n" +
                "Door remained bolted from inside.");

            // 2. Incident Report 2
            CreateClueTexture(clueDir + "/T_Clue_IncidentReport2.png", 512, 768, new Color(0.85f, 0.81f, 0.72f), true,
                "INCIDENT REPORT ADDENDUM\n" +
                "CASE: MARROW POINT 214\n\n" +
                "OFFICIAL FINDINGS:\n" +
                "Cause: Accidental electrical fault.\n" +
                "Emergency stairwell door locked\n" +
                "per drill protocol.\n\n" +
                "Aiden was observed at the end\n" +
                "of the corridor during the alarm.\n" +
                "Did not unlock unit 214.\n" +
                "Aiden returned to elevator.\n\n" +
                "STATUS: CLOSED PENDING INQUEST");

            // 3. Voicemail Slip
            CreateClueTexture(clueDir + "/T_Clue_Voicemail.png", 512, 512, new Color(0.98f, 0.94f, 0.65f), false,
                "WHILE YOU WERE OUT\n\n" +
                "TO: AIDEN\n" +
                "FROM: ASST. SUPERINTENDENT\n" +
                "TIME: 11:42 PM\n\n" +
                "MESSAGE:\n" +
                "Voss has been calling the lobby\n" +
                "non-stop for twenty minutes.\n" +
                "He says the lock jammed on 214.\n" +
                "Please go up there now.");

            // 4. Child Drawing
            CreateClueTexture(clueDir + "/T_Clue_ChildDrawing.png", 512, 512, new Color(0.96f, 0.95f, 0.92f), false,
                "      |\n" +
                "    [ 214 ]\n" +
                "     |   |\n" +
                "   O       |   |\n" +
                "  /|\\     ===+===\n" +
                "  / \\     |  #  |\n" +
                "\n" +
                "THE TALL MAN\n" +
                "DOES NOT OPEN\n" +
                "THE DOOR");

            // 5. October Calendar
            CreateClueTexture(clueDir + "/T_Clue_Calendar.png", 512, 512, new Color(0.93f, 0.92f, 0.88f), false,
                "       OCTOBER 1978\n" +
                " S   M   T   W   T   F   S\n" +
                " 1   2   3   4   5   6   7\n" +
                " 8   9  10  11  12  13 [14]\n" +
                "15  16  17  18  19  20  21\n" +
                "22  23  24  25  26  27  28\n" +
                "29  30  31\n\n" +
                "OCT 14: INSPECTION NIGHT");

            AssetDatabase.Refresh();

            // Set all clues to Sprite import
            string[] clueFiles = Directory.GetFiles(clueDir, "*.png");
            foreach (var cf in clueFiles)
            {
                var importer = AssetImporter.GetAtPath(cf) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("[MasterBuildUtility] All clue sprites generated and configured.");
        }

        private static void CreateClueTexture(string filePath, int w, int h, Color paperBg, bool charredEdge, string text)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var rng = new System.Random(filePath.GetHashCode());

            // 1. Fill base paper with fiber noise and vignette
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    float ny = (float)y / h;

                    // Paper grain
                    float grain = ((float)rng.NextDouble() * 2f - 1f) * 0.04f;
                    float edgeDist = Mathf.Min(Mathf.Min(nx, 1f - nx), Mathf.Min(ny, 1f - ny));
                    float edgeShadow = Mathf.Clamp01(edgeDist * 12f);

                    Color c = paperBg + new Color(grain, grain, grain, 0f);
                    c = Color.Lerp(new Color(0.35f, 0.28f, 0.2f, 1f), c, edgeShadow);

                    if (charredEdge)
                    {
                        // Burn bottom right corner
                        float cornerDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(1f, 0f));
                        if (cornerDist < 0.25f)
                        {
                            float burn = Mathf.Clamp01((0.25f - cornerDist) / 0.15f);
                            c = Color.Lerp(c, new Color(0.08f, 0.05f, 0.04f, 1f), burn);
                            if (cornerDist < 0.12f)
                            {
                                c = new Color(0f, 0f, 0f, 0f); // burned away hole
                            }
                        }
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            // 2. Draw simple pixel typewriter font
            DrawSimpleText(tex, text, 36, h - 50, new Color(0.12f, 0.12f, 0.12f, 0.95f));

            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, png);
        }

        private static void DrawSimpleText(Texture2D tex, string text, int startX, int startY, Color textColor)
        {
            int curX = startX;
            int curY = startY;
            int charW = 10;
            int lineSpacing = 24;

            foreach (char ch in text)
            {
                if (ch == '\n')
                {
                    curX = startX;
                    curY -= lineSpacing;
                    continue;
                }

                if (curX + charW >= tex.width - 20)
                {
                    curX = startX;
                    curY -= lineSpacing;
                }

                if (curY < 30) break;

                // Draw simple glyph strokes
                DrawGlyph(tex, ch, curX, curY, textColor);
                curX += charW + 2;
            }
        }

        private static void DrawGlyph(Texture2D tex, char ch, int x, int y, Color c)
        {
            // Simple block representation for authentic low-res typewriter impression
            byte pattern = (byte)(ch % 7);
            for (int dy = 0; dy < 14; dy++)
            {
                for (int dx = 0; dx < 8; dx++)
                {
                    if (ch == ' ' || ch == '\t') continue;
                    bool pixel = ((dx + dy + pattern) % 3 == 0) || (dx == 0 && dy % 2 == 0) || (dy == 0 && dx % 2 == 0);
                    if (pixel)
                    {
                        Color current = tex.GetPixel(x + dx, y - dy);
                        tex.SetPixel(x + dx, y - dy, Color.Lerp(current, c, c.a));
                    }
                }
            }
        }
        #endregion

        #region 3. PBR Materials & Normal Maps
        [MenuItem("Tools/Endless Hallway/Materials: Configure PBR Materials", false, 12)]
        public static void ConfigurePBRMaterials()
        {
            string texDir = "Assets/_Project/Art/Textures";
            Texture2D wallTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/T_Corridor_Wall_Albedo.jpg");
            Texture2D floorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/T_Corridor_Floor_Albedo.jpg");
            Texture2D ceilingTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/T_Corridor_Ceiling_Albedo.jpg");
            Texture2D doorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/T_Door_Albedo.jpg");
            Texture2D corkTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/T_Noticeboard_Cork_Albedo.jpg");

            // Apply to Corridor Wall
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Wall.mat");
            if (wallMat != null && wallTex != null)
            {
                wallMat.SetTexture("_BaseMap", wallTex);
                wallMat.SetTextureScale("_BaseMap", new Vector2(8f, 1.2f));
                wallMat.SetFloat("_Smoothness", 0.15f);
                EditorUtility.SetDirty(wallMat);
            }

            // Apply to Corridor Floor
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Floor.mat");
            if (floorMat != null && floorTex != null)
            {
                floorMat.SetTexture("_BaseMap", floorTex);
                floorMat.SetTextureScale("_BaseMap", new Vector2(2f, 16f));
                floorMat.SetFloat("_Smoothness", 0.22f);
                EditorUtility.SetDirty(floorMat);
            }

            // Apply to Corridor Ceiling
            Material ceilingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Ceiling.mat");
            if (ceilingMat != null && ceilingTex != null)
            {
                ceilingMat.SetTexture("_BaseMap", ceilingTex);
                ceilingMat.SetTextureScale("_BaseMap", new Vector2(2f, 16f));
                ceilingMat.SetFloat("_Smoothness", 0.05f);
                EditorUtility.SetDirty(ceilingMat);
            }

            // Apply to Doors
            Material doorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door.mat");
            if (doorMat != null && doorTex != null)
            {
                doorMat.SetTexture("_BaseMap", doorTex);
                doorMat.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
                doorMat.SetFloat("_Smoothness", 0.35f);
                EditorUtility.SetDirty(doorMat);
            }

            Material door214Mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door_214.mat");
            if (door214Mat != null && doorTex != null)
            {
                door214Mat.SetTexture("_BaseMap", doorTex);
                door214Mat.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
                door214Mat.SetColor("_BaseColor", new Color(0.75f, 0.45f, 0.4f, 1f)); // subtle deep reddish stain
                door214Mat.SetFloat("_Smoothness", 0.4f);
                EditorUtility.SetDirty(door214Mat);
            }

            // Apply to Corkboard
            Material corkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Noticeboard_Cork.mat");
            if (corkMat != null && corkTex != null)
            {
                corkMat.SetTexture("_BaseMap", corkTex);
                corkMat.SetTextureScale("_BaseMap", new Vector2(2f, 1.4f));
                corkMat.SetFloat("_Smoothness", 0.05f);
                EditorUtility.SetDirty(corkMat);
            }

            // Helper to load or create a Lit material
            Material GetOrCreateLitMat(string path)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    m = new Material(shader);
                    AssetDatabase.CreateAsset(m, path);
                }
                return m;
            }

            // Telephone Bakelite Material (Deep glossy vintage black)
            Material bakeliteMat = GetOrCreateLitMat("Assets/_Project/Materials/M_Telephone_Bakelite.mat");
            bakeliteMat.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.09f, 1f));
            bakeliteMat.SetFloat("_Smoothness", 0.82f);
            bakeliteMat.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(bakeliteMat);

            // Telephone Rotary Dial Face (Ivory vintage dial face)
            Material dialMat = GetOrCreateLitMat("Assets/_Project/Materials/M_Telephone_Dial.mat");
            dialMat.SetColor("_BaseColor", new Color(0.86f, 0.84f, 0.78f, 1f));
            dialMat.SetFloat("_Smoothness", 0.65f);
            dialMat.SetFloat("_Metallic", 0.2f);
            EditorUtility.SetDirty(dialMat);

            // Chair Wood Material (Dark rich walnut)
            Material chairWoodMat = GetOrCreateLitMat("Assets/_Project/Materials/M_Chair_Wood.mat");
            if (doorTex != null)
            {
                chairWoodMat.SetTexture("_BaseMap", doorTex);
                chairWoodMat.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
            }
            chairWoodMat.SetColor("_BaseColor", new Color(0.42f, 0.26f, 0.18f, 1f));
            chairWoodMat.SetFloat("_Smoothness", 0.45f);
            EditorUtility.SetDirty(chairWoodMat);

            // Wall Painting Canvas Artwork (Desolate landscape with shadowy silhouette)
            Texture2D landscapeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/T_Painting_Landscape.jpg");
            Material paintingMat = GetOrCreateLitMat("Assets/_Project/Materials/M_Painting_Canvas.mat");
            if (landscapeTex != null)
            {
                paintingMat.SetTexture("_BaseMap", landscapeTex);
            }
            paintingMat.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(paintingMat);

            // Metal Accent Material
            Material metalMat = GetOrCreateLitMat("Assets/_Project/Materials/M_Door_Metal.mat");
            metalMat.SetColor("_BaseColor", new Color(0.65f, 0.65f, 0.68f, 1f));
            metalMat.SetFloat("_Metallic", 0.85f);
            metalMat.SetFloat("_Smoothness", 0.75f);
            EditorUtility.SetDirty(metalMat);

            AssetDatabase.SaveAssets();
            Debug.Log("[MasterBuildUtility] PBR Materials updated with photorealistic textures.");
        }
        #endregion

        #region 4. Volume Profiles
        [MenuItem("Tools/Endless Hallway/Volumes: Build URP Volume Profiles", false, 13)]
        public static void BuildVolumeProfiles()
        {
            string volDir = "Assets/_Project/Settings/Volumes";
            if (!Directory.Exists(volDir)) Directory.CreateDirectory(volDir);

            // 1. Normal Profile (Loop 0-3 Baseline)
            VolumeProfile profNormal = CreateOrLoadProfile(volDir + "/VolumeProfile_Normal.asset");
            var caNormal = GetOrAdd<ColorAdjustments>(profNormal);
            caNormal.postExposure.Override(0.0f);
            caNormal.contrast.Override(12f);
            caNormal.saturation.Override(-5f);
            var vigNormal = GetOrAdd<Vignette>(profNormal);
            vigNormal.intensity.Override(0.35f);
            vigNormal.smoothness.Override(0.4f);
            var grainNormal = GetOrAdd<FilmGrain>(profNormal);
            grainNormal.intensity.Override(0.25f);
            grainNormal.type.Override(FilmGrainLookup.Medium1);
            var bloomNormal = GetOrAdd<Bloom>(profNormal);
            bloomNormal.intensity.Override(0.45f);
            bloomNormal.threshold.Override(0.9f);
            EditorUtility.SetDirty(profNormal);

            // 2. FlickerShift Profile (Loop 4-5 Shift)
            VolumeProfile profShift = CreateOrLoadProfile(volDir + "/VolumeProfile_FlickerShift.asset");
            var caShift = GetOrAdd<ColorAdjustments>(profShift);
            caShift.postExposure.Override(-0.35f);
            caShift.contrast.Override(25f);
            caShift.saturation.Override(-28f);
            caShift.colorFilter.Override(new Color(0.85f, 0.95f, 0.90f)); // sickly pale greenish tint
            var vigShift = GetOrAdd<Vignette>(profShift);
            vigShift.intensity.Override(0.50f);
            vigShift.smoothness.Override(0.45f);
            var grainShift = GetOrAdd<FilmGrain>(profShift);
            grainShift.intensity.Override(0.45f);
            grainShift.type.Override(FilmGrainLookup.Large01);
            var splitShift = GetOrAdd<SplitToning>(profShift);
            splitShift.shadows.Override(new Color(0.12f, 0.22f, 0.20f));
            splitShift.highlights.Override(new Color(0.95f, 0.90f, 0.70f));
            EditorUtility.SetDirty(profShift);

            // 3. NearDark Profile (Loop 6 The Darkening)
            VolumeProfile profDark = CreateOrLoadProfile(volDir + "/VolumeProfile_NearDark.asset");
            var caDark = GetOrAdd<ColorAdjustments>(profDark);
            caDark.postExposure.Override(-1.35f);
            caDark.contrast.Override(38f);
            caDark.saturation.Override(-55f);
            caDark.colorFilter.Override(new Color(0.70f, 0.75f, 0.80f));
            var vigDark = GetOrAdd<Vignette>(profDark);
            vigDark.intensity.Override(0.68f);
            vigDark.smoothness.Override(0.55f);
            var grainDark = GetOrAdd<FilmGrain>(profDark);
            grainDark.intensity.Override(0.65f);
            grainDark.type.Override(FilmGrainLookup.Large02);
            EditorUtility.SetDirty(profDark);

            // 4. Acceptance Profile (Cathartic warm closure)
            VolumeProfile profAccept = CreateOrLoadProfile(volDir + "/VolumeProfile_Acceptance.asset");
            var caAccept = GetOrAdd<ColorAdjustments>(profAccept);
            caAccept.postExposure.Override(0.35f);
            caAccept.contrast.Override(10f);
            caAccept.saturation.Override(15f);
            caAccept.colorFilter.Override(new Color(1f, 0.95f, 0.88f));
            var vigAccept = GetOrAdd<Vignette>(profAccept);
            vigAccept.intensity.Override(0.20f);
            var bloomAccept = GetOrAdd<Bloom>(profAccept);
            bloomAccept.intensity.Override(1.2f);
            bloomAccept.threshold.Override(0.7f);
            EditorUtility.SetDirty(profAccept);

            // 5. Erosion Profile (Cold endless abyss)
            VolumeProfile profErosion = CreateOrLoadProfile(volDir + "/VolumeProfile_Erosion.asset");
            var caErosion = GetOrAdd<ColorAdjustments>(profErosion);
            caErosion.postExposure.Override(-1.8f);
            caErosion.contrast.Override(45f);
            caErosion.saturation.Override(-90f);
            var vigErosion = GetOrAdd<Vignette>(profErosion);
            vigErosion.intensity.Override(0.85f);
            var grainErosion = GetOrAdd<FilmGrain>(profErosion);
            grainErosion.intensity.Override(0.85f);
            EditorUtility.SetDirty(profErosion);

            AssetDatabase.SaveAssets();

            // Set global volume in scene to use Normal profile
            var globalVol = GameObject.Find("GlobalVolume");
            if (globalVol != null)
            {
                var vol = globalVol.GetComponent<Volume>();
                if (vol != null)
                {
                    vol.sharedProfile = profNormal;
                    EditorUtility.SetDirty(globalVol);
                }
            }

            Debug.Log("[MasterBuildUtility] All URP Volume profiles built and assigned.");
        }

        private static VolumeProfile CreateOrLoadProfile(string path)
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(p, path);
            }
            return p;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out T comp))
            {
                comp = profile.Add<T>(true);
            }
            return comp;
        }
        #endregion

        #region 5. Room 214 & Hallway Carving
        [MenuItem("Tools/Endless Hallway/Scene: Build Room 214 Interior", false, 14)]
        public static void SetupRoom214AndCorridor()
        {
            float hallWidth = 2.4f;
            float hallHeight = 2.8f;
            float doorZ = 10.0f;

            var envRoot = GameObject.Find("Environment");
            if (envRoot == null) envRoot = new GameObject("Environment");

            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Wall.mat");
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Floor.mat");
            Material ceilingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Ceiling.mat");
            Material trimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Trim.mat");
            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door_Metal.mat");
            Material door214Mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door_214.mat");

            // 1. Carve Right Corridor Wall at Door 214 (Z = 10m)
            var rightWallOld = GameObject.Find("Corridor_RightWall");
            if (rightWallOld != null) UnityEngine.Object.DestroyImmediate(rightWallOld);

            var wallGroup = GameObject.Find("Corridor_RightWall_Group");
            if (wallGroup != null) UnityEngine.Object.DestroyImmediate(wallGroup);

            wallGroup = new GameObject("Corridor_RightWall_Group");
            wallGroup.transform.SetParent(envRoot.transform);

            // Front section: Z = 0 to 9.45 (length 9.45, center 4.725)
            var wallFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallFront.name = "RightWall_Front";
            wallFront.transform.SetParent(wallGroup.transform);
            wallFront.transform.position = new Vector3(hallWidth / 2f + 0.05f, hallHeight / 2f, 4.725f);
            wallFront.transform.localScale = new Vector3(0.1f, hallHeight, 9.45f);
            wallFront.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Lintel section above Door 214: Z = 9.45 to 10.55 (length 1.1), Y = 2.15 to 2.8 (height 0.65, center Y = 2.475)
            var wallLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallLintel.name = "RightWall_Lintel_214";
            wallLintel.transform.SetParent(wallGroup.transform);
            wallLintel.transform.position = new Vector3(hallWidth / 2f + 0.05f, 2.475f, doorZ);
            wallLintel.transform.localScale = new Vector3(0.1f, 0.65f, 1.1f);
            wallLintel.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Rear section: Z = 10.55 to 24 (length 13.45, center 17.275)
            var wallRear = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallRear.name = "RightWall_Rear";
            wallRear.transform.SetParent(wallGroup.transform);
            wallRear.transform.position = new Vector3(hallWidth / 2f + 0.05f, hallHeight / 2f, 17.275f);
            wallRear.transform.localScale = new Vector3(0.1f, hallHeight, 13.45f);
            wallRear.GetComponent<Renderer>().sharedMaterial = wallMat;

            // 2. Rebuild Door 214 Frame as an open archway
            var door214 = GameObject.Find("Door_214");
            if (door214 != null)
            {
                var oldFrame = door214.transform.Find("Frame");
                if (oldFrame != null) UnityEngine.Object.DestroyImmediate(oldFrame.gameObject);

                var frameGroup = new GameObject("Frame");
                frameGroup.transform.SetParent(door214.transform);
                frameGroup.transform.localPosition = Vector3.zero;

                // Left post
                var postL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                postL.name = "Post_L";
                postL.transform.SetParent(frameGroup.transform);
                postL.transform.localPosition = new Vector3(0f, 1.075f, -0.52f);
                postL.transform.localScale = new Vector3(0.14f, 2.15f, 0.08f);
                postL.GetComponent<Renderer>().sharedMaterial = trimMat;

                // Right post
                var postR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                postR.name = "Post_R";
                postR.transform.SetParent(frameGroup.transform);
                postR.transform.localPosition = new Vector3(0f, 1.075f, 0.52f);
                postR.transform.localScale = new Vector3(0.14f, 2.15f, 0.08f);
                postR.GetComponent<Renderer>().sharedMaterial = trimMat;

                // Header
                var postTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                postTop.name = "Header";
                postTop.transform.SetParent(frameGroup.transform);
                postTop.transform.localPosition = new Vector3(0f, 2.11f, 0f);
                postTop.transform.localScale = new Vector3(0.14f, 0.08f, 1.12f);
                postTop.GetComponent<Renderer>().sharedMaterial = trimMat;
            }

            // 3. Build Room 214 Interior
            var room214Old = GameObject.Find("Room_214");
            if (room214Old != null) UnityEngine.Object.DestroyImmediate(room214Old);

            var roomObj = new GameObject("Room_214");
            roomObj.transform.SetParent(envRoot.transform);

            float roomWidth = 4.2f;   // X: 1.25 to 5.45 (center 3.35)
            float roomDepth = 5.0f;   // Z: 7.5 to 12.5 (center 10.0)
            float roomCenterX = 1.25f + roomWidth / 2f; // 3.35
            float roomCenterZ = doorZ; // 10.0

            // Floor
            var rFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rFloor.name = "Floor";
            rFloor.transform.SetParent(roomObj.transform);
            rFloor.transform.position = new Vector3(roomCenterX, -0.05f, roomCenterZ);
            rFloor.transform.localScale = new Vector3(roomWidth, 0.1f, roomDepth);
            rFloor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Ceiling
            var rCeil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rCeil.name = "Ceiling";
            rCeil.transform.SetParent(roomObj.transform);
            rCeil.transform.position = new Vector3(roomCenterX, hallHeight + 0.05f, roomCenterZ);
            rCeil.transform.localScale = new Vector3(roomWidth, 0.1f, roomDepth);
            rCeil.GetComponent<Renderer>().sharedMaterial = ceilingMat;

            // Far Wall (East, X = 5.45)
            var rFarWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rFarWall.name = "Wall_Far";
            rFarWall.transform.SetParent(roomObj.transform);
            rFarWall.transform.position = new Vector3(1.25f + roomWidth + 0.05f, hallHeight / 2f, roomCenterZ);
            rFarWall.transform.localScale = new Vector3(0.1f, hallHeight, roomDepth + 0.2f);
            rFarWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // North Wall (Z = 12.5)
            var rNorthWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rNorthWall.name = "Wall_North";
            rNorthWall.transform.SetParent(roomObj.transform);
            rNorthWall.transform.position = new Vector3(roomCenterX, hallHeight / 2f, roomCenterZ + roomDepth / 2f + 0.05f);
            rNorthWall.transform.localScale = new Vector3(roomWidth + 0.2f, hallHeight, 0.1f);
            rNorthWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // South Wall (Z = 7.5)
            var rSouthWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rSouthWall.name = "Wall_South";
            rSouthWall.transform.SetParent(roomObj.transform);
            rSouthWall.transform.position = new Vector3(roomCenterX, hallHeight / 2f, roomCenterZ - roomDepth / 2f - 0.05f);
            rSouthWall.transform.localScale = new Vector3(roomWidth + 0.2f, hallHeight, 0.1f);
            rSouthWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // West Wall Pieces (adjoining corridor)
            // South segment: Z = 7.5 to 9.45 (length 1.95, center 8.475)
            var rWestSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rWestSouth.name = "Wall_West_South";
            rWestSouth.transform.SetParent(roomObj.transform);
            rWestSouth.transform.position = new Vector3(1.25f + 0.05f, hallHeight / 2f, 8.475f);
            rWestSouth.transform.localScale = new Vector3(0.1f, hallHeight, 1.95f);
            rWestSouth.GetComponent<Renderer>().sharedMaterial = wallMat;

            // North segment: Z = 10.55 to 12.5 (length 1.95, center 11.525)
            var rWestNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rWestNorth.name = "Wall_West_North";
            rWestNorth.transform.SetParent(roomObj.transform);
            rWestNorth.transform.position = new Vector3(1.25f + 0.05f, hallHeight / 2f, 11.525f);
            rWestNorth.transform.localScale = new Vector3(0.1f, hallHeight, 1.95f);
            rWestNorth.GetComponent<Renderer>().sharedMaterial = wallMat;

            // 4. Dress Interior: Spotlit Chair & Phone Table
            GameObject propsParent = new GameObject("RoomProps");
            propsParent.transform.SetParent(roomObj.transform);

            // Load dedicated room materials with bulletproof fallbacks
            Material chairMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Chair_Wood.mat") ?? trimMat;
            Material phoneBakelite = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Telephone_Bakelite.mat") ?? trimMat;
            Material phoneDial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Telephone_Dial.mat") ?? wallMat;
            Material metalAccent = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Door_Metal.mat") ?? trimMat;

            // Vintage Wooden Chair in center of room facing door
            Vector3 chairPos = new Vector3(3.2f, 0f, 10.0f);
            GameObject chair = new GameObject("Chair_Vintage");
            chair.transform.SetParent(propsParent.transform);
            chair.transform.position = chairPos;
            chair.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // Facing doorway!

            // Seat Cushion / Board
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Seat";
            seat.transform.SetParent(chair.transform);
            seat.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            seat.transform.localScale = new Vector3(0.50f, 0.05f, 0.48f);
            seat.GetComponent<Renderer>().sharedMaterial = chairMat;

            // 4 Turned Legs
            for (int lx = -1; lx <= 1; lx += 2)
            {
                for (int lz = -1; lz <= 1; lz += 2)
                {
                    var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    leg.name = $"Leg_{lx}_{lz}";
                    leg.transform.SetParent(chair.transform);
                    leg.transform.localPosition = new Vector3(lx * 0.21f, 0.22f, lz * 0.20f);
                    leg.transform.localScale = new Vector3(0.045f, 0.22f, 0.045f);
                    leg.GetComponent<Renderer>().sharedMaterial = chairMat;
                }
            }

            // Authentic Vintage Chair Backrest: 2 Posts, Top Crest Rail, 3 Slats
            var backPostL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            backPostL.name = "BackPost_L";
            backPostL.transform.SetParent(chair.transform);
            backPostL.transform.localPosition = new Vector3(-0.21f, 0.76f, -0.21f);
            backPostL.transform.localScale = new Vector3(0.04f, 0.32f, 0.04f);
            backPostL.GetComponent<Renderer>().sharedMaterial = chairMat;

            var backPostR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            backPostR.name = "BackPost_R";
            backPostR.transform.SetParent(chair.transform);
            backPostR.transform.localPosition = new Vector3(0.21f, 0.76f, -0.21f);
            backPostR.transform.localScale = new Vector3(0.04f, 0.32f, 0.04f);
            backPostR.GetComponent<Renderer>().sharedMaterial = chairMat;

            var topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = "TopCrestRail";
            topRail.transform.SetParent(chair.transform);
            topRail.transform.localPosition = new Vector3(0f, 1.06f, -0.21f);
            topRail.transform.localScale = new Vector3(0.50f, 0.08f, 0.04f);
            topRail.GetComponent<Renderer>().sharedMaterial = chairMat;

            for (int s = -1; s <= 1; s++)
            {
                var slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = $"BackSlat_{s}";
                slat.transform.SetParent(chair.transform);
                slat.transform.localPosition = new Vector3(s * 0.10f, 0.76f, -0.21f);
                slat.transform.localScale = new Vector3(0.035f, 0.52f, 0.02f);
                slat.GetComponent<Renderer>().sharedMaterial = chairMat;
            }

            // Side Table / Telephone Stand
            Vector3 tablePos = new Vector3(3.3f, 0f, 9.2f);
            GameObject table = new GameObject("Telephone_Table");
            table.transform.SetParent(propsParent.transform);
            table.transform.position = tablePos;

            var tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.name = "TableTop";
            tableTop.transform.SetParent(table.transform);
            tableTop.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            tableTop.transform.localScale = new Vector3(0.52f, 0.04f, 0.52f);
            tableTop.GetComponent<Renderer>().sharedMaterial = chairMat;

            // 4 Table Legs
            for (int tx = -1; tx <= 1; tx += 2)
            {
                for (int tz = -1; tz <= 1; tz += 2)
                {
                    var tLeg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    tLeg.name = $"TableLeg_{tx}_{tz}";
                    tLeg.transform.SetParent(table.transform);
                    tLeg.transform.localPosition = new Vector3(tx * 0.22f, 0.315f, tz * 0.22f);
                    tLeg.transform.localScale = new Vector3(0.045f, 0.315f, 0.045f);
                    tLeg.GetComponent<Renderer>().sharedMaterial = chairMat;
                }
            }

            // Antique Rotary Telephone on Table
            GameObject phoneObj = new GameObject("RotaryTelephone");
            phoneObj.transform.SetParent(table.transform);
            phoneObj.transform.localPosition = new Vector3(0f, 0.67f, 0f);

            // Lower Base Plate
            var phoneBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            phoneBase.name = "Base_Plate";
            phoneBase.transform.SetParent(phoneObj.transform);
            phoneBase.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            phoneBase.transform.localScale = new Vector3(0.22f, 0.04f, 0.20f);
            phoneBase.GetComponent<Renderer>().sharedMaterial = phoneBakelite;

            // Tapered / Angled Upper Housing
            var phoneBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            phoneBody.name = "Body_Sloped";
            phoneBody.transform.SetParent(phoneObj.transform);
            phoneBody.transform.localPosition = new Vector3(0f, 0.065f, -0.01f);
            phoneBody.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            phoneBody.transform.localScale = new Vector3(0.18f, 0.07f, 0.15f);
            phoneBody.GetComponent<Renderer>().sharedMaterial = phoneBakelite;

            // Rotary Dial Chrome Ring
            var dialRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dialRing.name = "Dial_Ring";
            dialRing.transform.SetParent(phoneObj.transform);
            dialRing.transform.localPosition = new Vector3(0f, 0.082f, -0.045f);
            dialRing.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            dialRing.transform.localScale = new Vector3(0.09f, 0.005f, 0.09f);
            dialRing.GetComponent<Renderer>().sharedMaterial = metalAccent;

            // Rotary Dial Face (Ivory vintage dial)
            var dialFace = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dialFace.name = "Dial_Face";
            dialFace.transform.SetParent(phoneObj.transform);
            dialFace.transform.localPosition = new Vector3(0f, 0.085f, -0.045f);
            dialFace.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            dialFace.transform.localScale = new Vector3(0.08f, 0.005f, 0.08f);
            dialFace.GetComponent<Renderer>().sharedMaterial = phoneDial;

            // Dial Center Hub
            var dialHub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dialHub.name = "Dial_Hub";
            dialHub.transform.SetParent(phoneObj.transform);
            dialHub.transform.localPosition = new Vector3(0f, 0.088f, -0.045f);
            dialHub.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            dialHub.transform.localScale = new Vector3(0.03f, 0.006f, 0.03f);
            dialHub.GetComponent<Renderer>().sharedMaterial = phoneBakelite;

            // Handset Cradle Forks
            var cradleL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cradleL.name = "Cradle_L";
            cradleL.transform.SetParent(phoneObj.transform);
            cradleL.transform.localPosition = new Vector3(-0.06f, 0.105f, 0.035f);
            cradleL.transform.localScale = new Vector3(0.02f, 0.035f, 0.02f);
            cradleL.GetComponent<Renderer>().sharedMaterial = phoneBakelite;

            var cradleR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cradleR.name = "Cradle_R";
            cradleR.transform.SetParent(phoneObj.transform);
            cradleR.transform.localPosition = new Vector3(0.06f, 0.105f, 0.035f);
            cradleR.transform.localScale = new Vector3(0.02f, 0.035f, 0.02f);
            cradleR.GetComponent<Renderer>().sharedMaterial = phoneBakelite;

            // Handset (Barbell style)
            var handset = new GameObject("Handset");
            handset.transform.SetParent(phoneObj.transform);
            handset.transform.localPosition = new Vector3(0f, 0.125f, 0.035f);

            var hBar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hBar.name = "Bar";
            hBar.transform.SetParent(handset.transform);
            hBar.transform.localPosition = Vector3.zero;
            hBar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            hBar.transform.localScale = new Vector3(0.032f, 0.13f, 0.032f);
            hBar.GetComponent<Renderer>().sharedMaterial = phoneBakelite;

            var earCup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            earCup.name = "EarCup";
            earCup.transform.SetParent(handset.transform);
            earCup.transform.localPosition = new Vector3(-0.08f, 0f, 0f);
            earCup.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            earCup.transform.localScale = new Vector3(0.065f, 0.025f, 0.065f);
            earCup.GetComponent<Renderer>().sharedMaterial = phoneBakelite;

            var mouthCup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mouthCup.name = "MouthCup";
            mouthCup.transform.SetParent(handset.transform);
            mouthCup.transform.localPosition = new Vector3(0.08f, 0f, 0f);
            mouthCup.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            mouthCup.transform.localScale = new Vector3(0.065f, 0.025f, 0.065f);
            mouthCup.GetComponent<Renderer>().sharedMaterial = phoneBakelite;

            // Clean up extraneous sub-mesh colliders so player gaze/interaction is smooth
            foreach (var c in phoneObj.GetComponentsInChildren<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(c);
            }
            // Add a clean single box collider to the telephone
            var phoneBox = phoneObj.AddComponent<BoxCollider>();
            phoneBox.center = new Vector3(0f, 0.07f, 0f);
            phoneBox.size = new Vector3(0.24f, 0.15f, 0.22f);

            // Add AudioSource for phone call
            AudioClip callClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/sfx_phone_call.wav");
            AudioSource phoneAudio = phoneObj.AddComponent<AudioSource>();
            phoneAudio.clip = callClip;
            phoneAudio.loop = true;
            phoneAudio.playOnAwake = false;
            phoneAudio.spatialBlend = 1f;
            phoneAudio.minDistance = 1f;
            phoneAudio.maxDistance = 12f;
            phoneAudio.rolloffMode = AudioRolloffMode.Linear;

            // AnomalyTarget for telephone audio
            var phoneTarget = phoneObj.AddComponent<AnomalyTarget>();
            var targetIdField = typeof(AnomalyTarget).GetField("targetId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (targetIdField != null) targetIdField.SetValue(phoneTarget, "Room214_Telephone");

            // Overhead Spotlight for Room 214 (Dramatic pool of light)
            GameObject spotGo = new GameObject("SpotLight_Room214");
            spotGo.transform.SetParent(roomObj.transform);
            spotGo.transform.position = new Vector3(3.2f, hallHeight - 0.1f, 10.0f);
            spotGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Light spotLight = spotGo.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.spotAngle = 45f;
            spotLight.innerSpotAngle = 25f;
            spotLight.range = 5.5f;
            spotLight.intensity = 5.0f;
            spotLight.color = new Color(1.0f, 0.92f, 0.78f); // warm amber incandescent
            spotLight.shadows = LightShadows.Soft;
            spotGo.SetActive(false); // Only turned on in Loop 7!

            // AnomalyTarget for Spotlight
            var spotTarget = spotGo.AddComponent<AnomalyTarget>();
            if (targetIdField != null) targetIdField.SetValue(spotTarget, "Room214_SpotLight");

            // Entry Trigger Volume across Doorway Threshold
            GameObject triggerGo = new GameObject("Room214_EntryTrigger");
            triggerGo.transform.SetParent(roomObj.transform);
            triggerGo.transform.position = new Vector3(1.6f, 1.0f, doorZ);
            var trigCol = triggerGo.AddComponent<BoxCollider>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector3(0.8f, 2.0f, 1.1f);
            triggerGo.AddComponent<Room214EntryTrigger>();

            // Observer Spawn Point at Doorway (stands beside door, looking into corridor)
            var spawnRoot = GameObject.Find("SpawnPoints");
            if (spawnRoot == null) spawnRoot = new GameObject("SpawnPoints");

            var oldDoorSpawn = GameObject.Find("ObserverSpawn_Door214");
            if (oldDoorSpawn != null) UnityEngine.Object.DestroyImmediate(oldDoorSpawn);

            GameObject doorSpawn = new GameObject("ObserverSpawn_Door214");
            doorSpawn.transform.SetParent(spawnRoot.transform);
            doorSpawn.transform.position = new Vector3(1.5f, 0f, 9.2f);
            doorSpawn.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Looking towards elevator
            var osp = doorSpawn.AddComponent<ObserverSpawnPoint>();
            var ptIdField = typeof(ObserverSpawnPoint).GetField("pointId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ptIdField != null) ptIdField.SetValue(osp, "Door214");

            var oldMirrorSpawn = GameObject.Find("ObserverSpawn_ElevatorMirror");
            if (oldMirrorSpawn != null) UnityEngine.Object.DestroyImmediate(oldMirrorSpawn);

            GameObject mirrorSpawn = new GameObject("ObserverSpawn_ElevatorMirror");
            mirrorSpawn.transform.SetParent(spawnRoot.transform);
            mirrorSpawn.transform.position = new Vector3(-0.6f, 0f, 2.0f);
            mirrorSpawn.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            var msp = mirrorSpawn.AddComponent<ObserverSpawnPoint>();
            if (ptIdField != null) ptIdField.SetValue(msp, "ElevatorMirror");

            // Refresh ObserverController spawn point list
            if (ObserverController.Instance != null)
            {
                ObserverController.Instance.RefreshSpawnPoints();
            }

            Debug.Log("[MasterBuildUtility] Room 214 Interior and Doorway fully constructed.");
        }
        #endregion

        #region 6. Clue Objects & Noticeboard
        [MenuItem("Tools/Endless Hallway/Scene: Build Clues and Props", false, 15)]
        public static void BuildClueObjectsInScene()
        {
            var propsRoot = GameObject.Find("Props");
            if (propsRoot == null) propsRoot = new GameObject("Props");

            // Clean up existing props to avoid duplication
            for (int i = propsRoot.transform.childCount - 1; i >= 0; i--)
            {
                var ch = propsRoot.transform.GetChild(i);
                if (ch.name.StartsWith("Clue_") || ch.name.StartsWith("Prop_") || ch.name == "WallPainting_A")
                {
                    UnityEngine.Object.DestroyImmediate(ch.gameObject);
                }
            }

            var board = GameObject.Find("CorkNoticeboard");
            if (board != null)
            {
                for (int i = board.transform.childCount - 1; i >= 0; i--)
                {
                    var ch = board.transform.GetChild(i);
                    if (ch.name.StartsWith("Clue_") || ch.name == "NoticePaper")
                    {
                        UnityEngine.Object.DestroyImmediate(ch.gameObject);
                    }
                }
            }
            Sprite spriteNotice = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Clues/T_Clue_FireDrillNotice.jpg");
            Sprite spriteInc1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Clues/T_Clue_IncidentReport1.png");
            Sprite spriteInc2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Clues/T_Clue_IncidentReport2.png");
            Sprite spriteVoice = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Clues/T_Clue_Voicemail.png");
            Sprite spriteChild = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Clues/T_Clue_ChildDrawing.png");
            Sprite spriteCal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Clues/T_Clue_Calendar.png");
            Material paperMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Paper_Notice.mat");
            if (paperMat == null) paperMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Wall.mat");
            Material paintingCanvasMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Painting_Canvas.mat");

            // 1. Noticeboard Fire Drill Notice (Loop 0)
            if (board != null)
            {
                var oldEx = board.GetComponent<Examinable>();
                if (oldEx != null) UnityEngine.Object.DestroyImmediate(oldEx);

                var ex = board.AddComponent<Examinable>();
                SetExaminableData(ex, "Clue_FireDrill", "NOTICE OF FIRE DRILL", 
                    "MARROW POINT RESIDENCES\nNOTICE OF FIRE ALARM SYSTEM TESTING & DRILL\n\nDate: October 14th\nTime: 11:00 PM to 1:00 AM\n\nAll tenants must remain inside their units while alarms sound. Stairwells will undergo automated smoke evacuation tests.\n\n- Resident Management Office",
                    spriteNotice, false, "[E] Read Notice");

                // Also attach paper visual pinned to corkboard
                var oldPaper = board.transform.Find("NoticePaper");
                if (oldPaper != null) UnityEngine.Object.DestroyImmediate(oldPaper.gameObject);

                var paper = GameObject.CreatePrimitive(PrimitiveType.Quad);
                paper.name = "NoticePaper";
                paper.transform.SetParent(board.transform);
                paper.transform.localPosition = new Vector3(-0.15f, 0.05f, -0.02f);
                paper.transform.localRotation = Quaternion.Euler(0f, 0f, 2f);
                paper.transform.localScale = new Vector3(0.45f, 0.6f, 1f);
                if (spriteNotice != null && paperMat != null)
                {
                    var pMat = new Material(paperMat);
                    pMat.SetTexture("_BaseMap", spriteNotice.texture);
                    paper.GetComponent<Renderer>().sharedMaterial = pMat;
                }
                UnityEngine.Object.DestroyImmediate(paper.GetComponent<Collider>());
            }

            // 2. Wall Calendar near Elevator (X = -1.18, Y = 1.4, Z = 1.6)
            var oldCal = GameObject.Find("Prop_Calendar");
            if (oldCal != null) UnityEngine.Object.DestroyImmediate(oldCal);

            var calObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            calObj.name = "Prop_Calendar";
            calObj.transform.SetParent(propsRoot.transform);
            calObj.transform.position = new Vector3(-1.18f, 1.4f, 1.6f);
            calObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            calObj.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            if (spriteCal != null && paperMat != null)
            {
                var cMat = new Material(paperMat);
                cMat.SetTexture("_BaseMap", spriteCal.texture);
                calObj.GetComponent<Renderer>().sharedMaterial = cMat;
            }
            var calEx = calObj.AddComponent<Examinable>();
            SetExaminableData(calEx, "Clue_Calendar", "WALL CALENDAR - OCTOBER 1978",
                "A wall calendar hanging by the corridor entrance.\n\nOctober 14th is circled heavily in black ink with the handwritten note: 'INSPECTION NIGHT - DO NOT LEAVE POST'.",
                spriteCal, false, "[E] Inspect Calendar");

            // 3. Incident Report Fragment 1 (Floor near elevator, active in Loop 4)
            var oldInc1 = GameObject.Find("Clue_IncidentReport1");
            if (oldInc1 != null) UnityEngine.Object.DestroyImmediate(oldInc1);

            var inc1Obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            inc1Obj.name = "Clue_IncidentReport1";
            inc1Obj.transform.SetParent(propsRoot.transform);
            inc1Obj.transform.position = new Vector3(0.3f, 0.015f, 3.8f);
            inc1Obj.transform.rotation = Quaternion.Euler(90f, 25f, 0f);
            inc1Obj.transform.localScale = new Vector3(0.35f, 0.5f, 1f);
            if (spriteInc1 != null && paperMat != null)
            {
                var iMat = new Material(paperMat);
                iMat.SetTexture("_BaseMap", spriteInc1.texture);
                inc1Obj.GetComponent<Renderer>().sharedMaterial = iMat;
            }
            var inc1Ex = inc1Obj.AddComponent<Examinable>();
            SetExaminableData(inc1Ex, "Clue_IncidentReport1", "TORN INCIDENT REPORT #0414",
                "A charred fragment of an official incident report.\n\nDATE: OCT 14, 1978\nUNIT: 214\nTENANT: ELIAS VOSS\n\n'During routine inspection, smoke damper failed to open. Tenant attempted repeated contact with Resident Manager Aiden...\n\n[The paper feels unnervingly warm to the touch, as though pulled from recent embers.]'",
                spriteInc1, true, "[E] Pick up scorched paper");

            var inc1Target = inc1Obj.AddComponent<AnomalyTarget>();
            SetTargetId(inc1Target, "Clue_IncidentReport1");
            inc1Obj.SetActive(false); // Activated in Loop 4!

            // 4. Voicemail Slip on Noticeboard (active in Loop 5)
            var oldVoice = GameObject.Find("Clue_Voicemail");
            if (oldVoice != null) UnityEngine.Object.DestroyImmediate(oldVoice);

            var voiceObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            voiceObj.name = "Clue_Voicemail";
            voiceObj.transform.SetParent(board != null ? board.transform : propsRoot.transform);
            voiceObj.transform.localPosition = new Vector3(0.25f, -0.05f, -0.02f);
            voiceObj.transform.localRotation = Quaternion.Euler(0f, 0f, -5f);
            voiceObj.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            if (spriteVoice != null && paperMat != null)
            {
                var vMat = new Material(paperMat);
                vMat.SetTexture("_BaseMap", spriteVoice.texture);
                voiceObj.GetComponent<Renderer>().sharedMaterial = vMat;
            }
            var voiceEx = voiceObj.AddComponent<Examinable>();
            SetExaminableData(voiceEx, "Clue_Voicemail", "PHONE MESSAGE SLIP",
                "URGENT MESSAGE - WHILE YOU WERE OUT\n\nTO: AIDEN\nFROM: ASST. SUPERINTENDENT\nTIME: 11:42 PM\n\n'Voss has been calling the switchboard non-stop for twenty minutes. He says the emergency deadlock jammed from the outside on 214. Please go up there now.'",
                spriteVoice, false, "[E] Read message slip");

            var voiceTarget = voiceObj.AddComponent<AnomalyTarget>();
            SetTargetId(voiceTarget, "Clue_Voicemail");
            voiceObj.SetActive(false); // Activated in Loop 5!

            // 5. Incident Report 2 (Floor near Room 214, active in Loop 6)
            var oldInc2 = GameObject.Find("Clue_IncidentReport2");
            if (oldInc2 != null) UnityEngine.Object.DestroyImmediate(oldInc2);

            var inc2Obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            inc2Obj.name = "Clue_IncidentReport2";
            inc2Obj.transform.SetParent(propsRoot.transform);
            inc2Obj.transform.position = new Vector3(0.85f, 0.015f, 8.8f);
            inc2Obj.transform.rotation = Quaternion.Euler(90f, -15f, 0f);
            inc2Obj.transform.localScale = new Vector3(0.35f, 0.5f, 1f);
            if (spriteInc2 != null && paperMat != null)
            {
                var i2Mat = new Material(paperMat);
                i2Mat.SetTexture("_BaseMap", spriteInc2.texture);
                inc2Obj.GetComponent<Renderer>().sharedMaterial = i2Mat;
            }
            var inc2Ex = inc2Obj.AddComponent<Examinable>();
            SetExaminableData(inc2Ex, "Clue_IncidentReport2", "INCIDENT REPORT ADDENDUM",
                "OFFICIAL FINDINGS - UNIT 214\n\n'Emergency stairwell door locked per drill protocol.\n\nAiden was observed at the end of the corridor during the alarm sounding.\nDid not unlock unit 214.\nAiden returned to the elevator.\n\nSTATUS: CLOSED PENDING INQUEST.'",
                spriteInc2, true, "[E] Read scorched addendum");

            var inc2Target = inc2Obj.AddComponent<AnomalyTarget>();
            SetTargetId(inc2Target, "Clue_IncidentReport2");
            inc2Obj.SetActive(false); // Activated in Loop 6!

            // 6. Wall Painting A on left wall between elevator and Door 210
            Material trimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Corridor_Trim.mat");
            GameObject paintObj = new GameObject("WallPainting_A");
            paintObj.transform.SetParent(propsRoot.transform);
            paintObj.transform.position = new Vector3(-1.18f, 1.6f, 3.5f);
            paintObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(paintObj.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(0.8f, 0.6f, 0.04f);
            frame.GetComponent<Renderer>().sharedMaterial = trimMat;

            var canvas = GameObject.CreatePrimitive(PrimitiveType.Quad);
            canvas.name = "Canvas";
            canvas.transform.SetParent(paintObj.transform);
            canvas.transform.localPosition = new Vector3(0f, 0f, -0.025f);
            canvas.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvas.transform.localScale = new Vector3(0.72f, 0.52f, 1f);
            canvas.GetComponent<Renderer>().sharedMaterial = paintingCanvasMat != null ? paintingCanvasMat : paperMat;
            UnityEngine.Object.DestroyImmediate(canvas.GetComponent<Collider>());

            var pTarget = paintObj.AddComponent<AnomalyTarget>();
            SetTargetId(pTarget, "WallPainting_A");

            // 7. Ensure CeilingLight_214 target ID matches AnomalyDefinitionSO
            var cl214 = GameObject.Find("CeilingLight_214");
            if (cl214 != null)
            {
                var clTarget = cl214.GetComponent<AnomalyTarget>();
                if (clTarget == null) clTarget = cl214.AddComponent<AnomalyTarget>();
                SetTargetId(clTarget, "CeilingLight_214");
            }

            Debug.Log("[MasterBuildUtility] Clue objects and props created and registered.");
        }

        private static void SetExaminableData(Examinable ex, string id, string title, string body, Sprite sprite, bool warm, string prompt)
        {
            var fId = typeof(Examinable).GetField("clueId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fTitle = typeof(Examinable).GetField("title", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fDoc = typeof(Examinable).GetField("documentText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fSprite = typeof(Examinable).GetField("documentSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fWarm = typeof(Examinable).GetField("warmToTouch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fPrompt = typeof(Examinable).GetField("promptText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fId != null) fId.SetValue(ex, id);
            if (fTitle != null) fTitle.SetValue(ex, title);
            if (fDoc != null) fDoc.SetValue(ex, body);
            if (fSprite != null) fSprite.SetValue(ex, sprite);
            if (fWarm != null) fWarm.SetValue(ex, warm);
            if (fPrompt != null) fPrompt.SetValue(ex, prompt);
        }

        private static void SetTargetId(AnomalyTarget target, string id)
        {
            var fId = typeof(AnomalyTarget).GetField("targetId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fId != null) fId.SetValue(target, id);
        }
        #endregion

        #region 7. Loop ScriptableObjects & Anomalies
        [MenuItem("Tools/Endless Hallway/ScriptableObjects: Build Loops 0-7", false, 16)]
        public static void BuildAllLoopScriptableObjects()
        {
            string anomDir = "Assets/_Project/ScriptableObjects/Anomalies";
            string loopDir = "Assets/_Project/ScriptableObjects/Loops";
            if (!Directory.Exists(anomDir)) Directory.CreateDirectory(anomDir);
            if (!Directory.Exists(loopDir)) Directory.CreateDirectory(loopDir);

            VolumeProfile profNormal = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/Volumes/VolumeProfile_Normal.asset");
            VolumeProfile profShift = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/Volumes/VolumeProfile_FlickerShift.asset");
            VolumeProfile profDark = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/Volumes/VolumeProfile_NearDark.asset");
            VolumeProfile profAccept = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/Volumes/VolumeProfile_Acceptance.asset");
            VolumeProfile profErosion = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/Volumes/VolumeProfile_Erosion.asset");

            // Load Audio Clips
            AudioClip droneClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/amb_hallway_drone.wav");
            AudioClip phoneRingClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/sfx_phone_ring.wav");
            AudioClip phoneCallClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/sfx_phone_call.wav");

            // Anomaly Helper
            AnomalyDefinitionSO CreateAnomaly(string path, string id, string target, AnomalyType type, Action<AnomalyDefinitionSO> init)
            {
                var anom = AssetDatabase.LoadAssetAtPath<AnomalyDefinitionSO>(path);
                if (anom == null)
                {
                    anom = ScriptableObject.CreateInstance<AnomalyDefinitionSO>();
                    AssetDatabase.CreateAsset(anom, path);
                }
                anom.anomalyId = id;
                anom.targetObjectId = target;
                anom.anomalyType = type;
                init(anom);
                EditorUtility.SetDirty(anom);
                return anom;
            }

            // Loop 1 Anomaly: Painting Tilt
            var aTilt = CreateAnomaly(anomDir + "/Anom_Painting_Tilt.asset", "Anom_Painting_Tilt", "WallPainting_A", AnomalyType.TransformShift, a => {
                a.description = "The framed painting tilts 12 degrees askew.";
                a.rotationOffset = new Vector3(0f, 0f, -12f);
            });

            // Loop 2 Anomalies: Door 212 Ajar, Ceiling Light 214 Flickers
            var aDoor212 = CreateAnomaly(anomDir + "/Anom_Door_212_Ajar.asset", "Anom_Door_212_Ajar", "Door_212", AnomalyType.DoorLockState, a => {
                a.description = "Door 212 is unlocked and slightly ajar.";
                a.setDoorLocked = false;
                a.setDoorOpen = true;
            });
            var aLight214 = CreateAnomaly(anomDir + "/Anom_Light_214_Flicker.asset", "Anom_Light_214_Flicker", "CeilingLight_214", AnomalyType.LightFlicker, a => {
                a.description = "Ceiling light outside Room 214 flickers erratically.";
                a.isLightFlickering = true;
                a.flickerIntervalMin = 0.05f;
                a.flickerIntervalMax = 0.2f;
            });

            // Loop 3 Anomaly: Observer Glimpse at Far End
            var aObsFar = CreateAnomaly(anomDir + "/Anom_Observer_FarHallway.asset", "Anom_Observer_FarHallway", "ObserverEntity", AnomalyType.ObserverSpawn, a => {
                a.description = "Observer silhouette stands motionless at the far end of the corridor, vanishing when looked at.";
                a.observerSpawnPointId = "FarHallway";
                a.observerState = ObserverState.GlimpseFar;
            });

            // Loop 4 Anomalies: Volume Shift, Light 214 Dead, Incident Report 1 Spawns
            var aVolShift = CreateAnomaly(anomDir + "/Anom_Volume_Shift.asset", "Anom_Volume_Shift", "", AnomalyType.LightingChange, a => {
                a.description = "Corridor lighting shifts to high-contrast desaturated green tint.";
                a.volumeProfile = profShift;
            });
            var aLight214Off = CreateAnomaly(anomDir + "/Anom_Light_214_Off.asset", "Anom_Light_214_Off", "CeilingLight_214", AnomalyType.SetActiveState, a => {
                a.description = "Ceiling light 214 is completely dead.";
                a.activeState = false;
            });
            var aSpawnInc1 = CreateAnomaly(anomDir + "/Anom_Spawn_IncReport1.asset", "Anom_Spawn_IncReport1", "Clue_IncidentReport1", AnomalyType.SetActiveState, a => {
                a.description = "Scorched Incident Report #0414 appears on the floor near elevator.";
                a.activeState = true;
            });

            // Loop 5 Anomalies: Door 216 Open, Voicemail Note Active, Observer at Elevator Mirror
            var aDoor216 = CreateAnomaly(anomDir + "/Anom_Door_216_Open.asset", "Anom_Door_216_Open", "Door_216", AnomalyType.DoorLockState, a => {
                a.description = "Door 216 is wide open into total pitch-black darkness.";
                a.setDoorLocked = false;
                a.setDoorOpen = true;
            });
            var aSpawnVoice = CreateAnomaly(anomDir + "/Anom_Spawn_Voicemail.asset", "Anom_Spawn_Voicemail", "Clue_Voicemail", AnomalyType.SetActiveState, a => {
                a.description = "Urgent voicemail notepad slip pinned to corkboard.";
                a.activeState = true;
            });
            var aObsMirror = CreateAnomaly(anomDir + "/Anom_Observer_Mirror.asset", "Anom_Observer_Mirror", "ObserverEntity", AnomalyType.ObserverSpawn, a => {
                a.description = "Observer stands briefly visible reflected near elevator threshold.";
                a.observerSpawnPointId = "ElevatorMirror";
                a.observerState = ObserverState.GlimpseMirror;
            });

            // Loop 6 Anomalies: NearDark Volume, All Lights Off, Incident Report 2 Spawns, Phone Ringing behind 214
            var aVolDark = CreateAnomaly(anomDir + "/Anom_Volume_NearDark.asset", "Anom_Volume_NearDark", "", AnomalyType.LightingChange, a => {
                a.description = "Hallway descends into near-total darkness.";
                a.volumeProfile = profDark;
            });
            var aAllLightsOff = CreateAnomaly(anomDir + "/Anom_Lights_Emergency.asset", "Anom_Lights_Emergency", "CeilingLightsGroup", AnomalyType.SetActiveState, a => {
                a.description = "Standard ceiling light fixtures cut out.";
                a.activeState = false;
            });
            var aSpawnInc2 = CreateAnomaly(anomDir + "/Anom_Spawn_IncReport2.asset", "Anom_Spawn_IncReport2", "Clue_IncidentReport2", AnomalyType.SetActiveState, a => {
                a.description = "Scorched Incident Report Addendum appears on the floor outside 214.";
                a.activeState = true;
            });
            var aPhoneRing = CreateAnomaly(anomDir + "/Anom_Phone_Ring_214.asset", "Anom_Phone_Ring_214", "Room214_Telephone", AnomalyType.AudioTrigger, a => {
                a.description = "Telephone inside Room 214 rings loudly and incessantly.";
                a.audioClip = phoneRingClip;
                a.loopAudio = true;
                a.audioVolume = 0.85f;
            });

            var aEmergencyLightOn = CreateAnomaly(anomDir + "/Anom_EmergencyLight_On.asset", "Anom_EmergencyLight_On", "EmergencyLight_FloorStrip", AnomalyType.SetActiveState, a => {
                a.description = "Emergency crimson floor strip illuminates when main power is cut.";
                a.activeState = true;
            });

            // Loop 7 Anomalies: Door 214 Open, Spotlight On, Phone Call Audio On, Observer at Doorway
            var aDoor214Open = CreateAnomaly(anomDir + "/Anom_Door_214_Open.asset", "Anom_Door_214_Open", "Door_214", AnomalyType.DoorLockState, a => {
                a.description = "Door 214 is wide open, revealing the room interior for the first time.";
                a.setDoorLocked = false;
                a.setDoorOpen = true;
            });
            var aSpot214On = CreateAnomaly(anomDir + "/Anom_Spot_214_On.asset", "Anom_Spot_214_On", "Room214_SpotLight", AnomalyType.SetActiveState, a => {
                a.description = "Dramatic pool of incandescent light illuminates the chair and telephone inside 214.";
                a.activeState = true;
            });
            var aPhoneCall = CreateAnomaly(anomDir + "/Anom_Phone_Call_214.asset", "Anom_Phone_Call_214", "Room214_Telephone", AnomalyType.AudioTrigger, a => {
                a.description = "The telephone plays Aiden's phone call from the night of the fire in full.";
                a.audioClip = phoneCallClip;
                a.loopAudio = true;
                a.audioVolume = 0.9f;
            });
            var aObsDoorway = CreateAnomaly(anomDir + "/Anom_Observer_Doorway.asset", "Anom_Observer_Doorway", "ObserverEntity", AnomalyType.ObserverSpawn, a => {
                a.description = "Observer stands motionless beside the doorway of 214, waiting for Aiden's final choice.";
                a.observerSpawnPointId = "Door214";
                a.observerState = ObserverState.Present;
            });

            // Now Create LoopConfigSO Assets
            LoopConfigSO CreateLoopConfig(string path, int idx, string title, List<AnomalyDefinitionSO> anoms, AudioClip ambient = null, float ambVol = 0.35f)
            {
                var cfg = AssetDatabase.LoadAssetAtPath<LoopConfigSO>(path);
                if (cfg == null)
                {
                    cfg = ScriptableObject.CreateInstance<LoopConfigSO>();
                    AssetDatabase.CreateAsset(cfg, path);
                }
                cfg.loopIndex = idx;
                cfg.loopTitle = title;
                cfg.activeAnomalies = anoms;
                cfg.ambientOverlayClip = ambient;
                cfg.ambientOverlayVolume = ambVol;
                EditorUtility.SetDirty(cfg);
                return cfg;
            }

            var l0 = CreateLoopConfig(loopDir + "/Loop_00_Baseline.asset", 0, "Loop 0 - Baseline", new List<AnomalyDefinitionSO>(), droneClip, 0.35f);
            var l1 = CreateLoopConfig(loopDir + "/Loop_01_FirstWrongness.asset", 1, "Loop 1 - The First Wrongness", new List<AnomalyDefinitionSO> { aTilt }, droneClip, 0.35f);
            var l2 = CreateLoopConfig(loopDir + "/Loop_02_DoorAndLight.asset", 2, "Loop 2 - Door and Light", new List<AnomalyDefinitionSO> { aDoor212, aLight214 }, droneClip, 0.40f);
            var l3 = CreateLoopConfig(loopDir + "/Loop_03_FirstEncounter.asset", 3, "Loop 3 - First Glimpse", new List<AnomalyDefinitionSO> { aObsFar, aLight214 }, droneClip, 0.45f);
            var l4 = CreateLoopConfig(loopDir + "/Loop_04_TheShift.asset", 4, "Loop 4 - The Shift", new List<AnomalyDefinitionSO> { aVolShift, aLight214Off, aSpawnInc1 }, droneClip, 0.50f);
            var l5 = CreateLoopConfig(loopDir + "/Loop_05_DeepWrongness.asset", 5, "Loop 5 - Deep Wrongness", new List<AnomalyDefinitionSO> { aVolShift, aDoor216, aSpawnVoice, aObsMirror }, droneClip, 0.55f);
            var l6 = CreateLoopConfig(loopDir + "/Loop_06_TheDarkening.asset", 6, "Loop 6 - The Darkening", new List<AnomalyDefinitionSO> { aVolDark, aAllLightsOff, aEmergencyLightOn, aSpawnInc2, aPhoneRing }, droneClip, 0.65f);
            var l7 = CreateLoopConfig(loopDir + "/Loop_07_TheChoice.asset", 7, "Loop 7 - The Choice", new List<AnomalyDefinitionSO> { aDoor214Open, aSpot214On, aPhoneCall, aObsDoorway }, droneClip, 0.70f);

            // Wire all LoopConfigs into AnomalyManager in the scene
            var mgrs = GameObject.Find("Managers");
            if (mgrs != null)
            {
                var am = mgrs.GetComponent<AnomalyManager>();
                if (am != null)
                {
                    var allConfigs = new List<LoopConfigSO> { l0, l1, l2, l3, l4, l5, l6, l7 };
                    var fConfigs = typeof(AnomalyManager).GetField("loopConfigs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fConfigs != null) fConfigs.SetValue(am, allConfigs);

                    var fBaseVol = typeof(AnomalyManager).GetField("baselineVolumeProfile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fBaseVol != null) fBaseVol.SetValue(am, profNormal);

                    var fSceneVol = typeof(AnomalyManager).GetField("sceneVolume", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var gVol = GameObject.Find("GlobalVolume");
                    if (fSceneVol != null && gVol != null) fSceneVol.SetValue(am, gVol.GetComponent<Volume>());

                    EditorUtility.SetDirty(am);
                }

                var gm = mgrs.GetComponent<GameManager>();
                if (gm != null)
                {
                    var fAcc = typeof(GameManager).GetField("acceptanceProfile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fAcc != null) fAcc.SetValue(gm, profAccept);
                    var fEro = typeof(GameManager).GetField("erosionProfile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fEro != null) fEro.SetValue(gm, profErosion);
                    EditorUtility.SetDirty(gm);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[MasterBuildUtility] All 8 LoopConfigs (0-7) and Anomalies built and wired into AnomalyManager.");
        }
        #endregion

        #region 8. Configure Examine UI
        [MenuItem("Tools/Endless Hallway/UI: Polish Examine UI", false, 17)]
        public static void ConfigureExamineUI()
        {
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null) canvasObj = GameObject.Find("UI_Canvas");
            if (canvasObj == null) return;

            var exUI = canvasObj.GetComponent<ExamineUI>();
            if (exUI == null) exUI = canvasObj.AddComponent<ExamineUI>();

            var panel = canvasObj.transform.Find("ExaminePanel");
            if (panel == null) return;

            // Clue Image (Left Side)
            var oldImg = panel.Find("ClueImage");
            GameObject imgObj = oldImg != null ? oldImg.gameObject : new GameObject("ClueImage");
            imgObj.transform.SetParent(panel, false);
            var clueImg = imgObj.GetComponent<Image>();
            if (clueImg == null) clueImg = imgObj.AddComponent<Image>();
            clueImg.preserveAspect = true;
            RectTransform imgRect = imgObj.GetComponent<RectTransform>();
            imgRect.anchorMin = new Vector2(0.06f, 0.12f);
            imgRect.anchorMax = new Vector2(0.48f, 0.85f);
            imgRect.offsetMin = Vector2.zero;
            imgRect.offsetMax = Vector2.zero;

            // Adjust Body Text (Right Side)
            var bodyObj = panel.Find("Body");
            if (bodyObj != null)
            {
                RectTransform bRect = bodyObj.GetComponent<RectTransform>();
                bRect.anchorMin = new Vector2(0.52f, 0.18f);
                bRect.anchorMax = new Vector2(0.94f, 0.85f);
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;
            }

            // Warm Indicator badge
            var oldWarm = panel.Find("WarmIndicator");
            GameObject warmObj = oldWarm != null ? oldWarm.gameObject : new GameObject("WarmIndicator");
            warmObj.transform.SetParent(panel, false);
            var warmText = warmObj.GetComponent<TextMeshProUGUI>();
            if (warmText == null) warmText = warmObj.AddComponent<TextMeshProUGUI>();
            warmText.text = "[The document radiates a faint, unsettling warmth...]";
            warmText.fontSize = 15;
            warmText.fontStyle = FontStyles.Italic;
            warmText.color = new Color(1.0f, 0.55f, 0.25f, 0.95f);
            warmText.alignment = TextAlignmentOptions.BottomRight;
            RectTransform wRect = warmObj.GetComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.52f, 0.08f);
            wRect.anchorMax = new Vector2(0.94f, 0.16f);
            wRect.offsetMin = Vector2.zero;
            wRect.offsetMax = Vector2.zero;
            warmObj.SetActive(false);

            // Wire ExamineUI fields via reflection
            var fPanel = typeof(ExamineUI).GetField("panelRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fPanel != null) fPanel.SetValue(exUI, panel.gameObject);

            var fClueImg = typeof(ExamineUI).GetField("clueImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fClueImg != null) fClueImg.SetValue(exUI, clueImg);

            var fWarm = typeof(ExamineUI).GetField("warmEffectIndicator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fWarm != null) fWarm.SetValue(exUI, warmObj);

            var closeHint = panel.Find("CloseHint");
            if (closeHint != null)
            {
                var btn = closeHint.GetComponent<Button>();
                if (btn == null) btn = closeHint.gameObject.AddComponent<Button>();
                var fBtn = typeof(ExamineUI).GetField("closeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fBtn != null) fBtn.SetValue(exUI, btn);
            }

            EditorUtility.SetDirty(exUI);
            Debug.Log("[MasterBuildUtility] ExamineUI polished and connected.");
        }
        #endregion
    }
}
