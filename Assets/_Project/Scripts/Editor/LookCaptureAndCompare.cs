using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EndlessHallway.Editor
{
    [InitializeOnLoad]
    public static class LookCaptureAndCompare
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string ReferenceImagePath = "Assets/_Project/References/corridor_reference.png";
        private const int IterationIndex = 0; // Baseline iteration

        public struct CaptureSpot
        {
            public string Name;
            public Vector3 Position;
            public Quaternion Rotation;

            public CaptureSpot(string name, Vector3 pos, Quaternion rot)
            {
                Name = name;
                Position = pos;
                Rotation = rot;
            }
        }

        public class ShotMetrics
        {
            public float MeanLuminance;
            public float PctBelow10;
            public float PctBelow20;
            public float P95Luminance;
            public float P99Luminance;
            public float MedianHue;
            public float MedianSaturation;
            public float MeanR;
            public float MeanG;
            public float MeanB;
            public float FloorLuminance;
            public float FarEndLuminance;
            public float LeftWallLuminance;
            public float RightWallLuminance;
            public float CenterLuminance;
            public float CornerLuminance;
            public float GrainStd;
        }

        public class MetricTarget
        {
            public string Name;
            public float Target;
            public float Min;
            public float Max;
            public string Unit;
            public Func<ShotMetrics, float> Getter;

            public MetricTarget(string name, float target, float min, float max, string unit, Func<ShotMetrics, float> getter)
            {
                Name = name;
                Target = target;
                Min = min;
                Max = max;
                Unit = unit;
                Getter = getter;
            }

            public bool IsPass(float val) => val >= Min && val <= Max;
        }

        private static readonly List<MetricTarget> TargetDefinitions = new List<MetricTarget>()
        {
            new MetricTarget("Mean Luminance", 0.375f, 0.30f, 0.45f, "", m => m.MeanLuminance),
            new MetricTarget("Pixels < 0.10", 7.0f, 4.0f, 14.0f, "%", m => m.PctBelow10),
            new MetricTarget("Pixels < 0.20", 27.0f, 20.0f, 36.0f, "%", m => m.PctBelow20),
            new MetricTarget("95th percentile", 0.73f, 0.62f, 0.82f, "", m => m.P95Luminance),
            new MetricTarget("99th percentile", 0.85f, 0.00f, 0.90f, "(<0.90)", m => m.P99Luminance),
            new MetricTarget("Median Hue (lit)", 189.0f, 180.0f, 200.0f, "°", m => m.MedianHue),
            new MetricTarget("Median Saturation", 0.54f, 0.45f, 0.62f, "", m => m.MedianSaturation),
            new MetricTarget("Mean R", 0.235f, 0.18f, 0.29f, "", m => m.MeanR),
            new MetricTarget("Mean G", 0.410f, 0.35f, 0.47f, "", m => m.MeanG),
            new MetricTarget("Mean B", 0.440f, 0.38f, 0.50f, "", m => m.MeanB),
            new MetricTarget("Floor Luminance", 0.15f, 0.10f, 0.20f, "", m => m.FloorLuminance),
            new MetricTarget("Far Corridor End", 0.12f, 0.07f, 0.16f, "", m => m.FarEndLuminance),
            new MetricTarget("Right Wall", 0.54f, 0.45f, 0.63f, "", m => m.RightWallLuminance),
            new MetricTarget("Left Wall", 0.39f, 0.30f, 0.48f, "", m => m.LeftWallLuminance),
            new MetricTarget("Center Luminance", 0.20f, 0.10f, 0.28f, "", m => m.CenterLuminance),
            new MetricTarget("Corner Luminance", 0.36f, 0.28f, 0.45f, "", m => m.CornerLuminance),
            new MetricTarget("Film Grain Std", 0.023f, 0.015f, 0.030f, "", m => m.GrainStd)
        };

        static LookCaptureAndCompare()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                string reportPath = Path.Combine(Application.dataPath, "..", "LookReports", $"iteration_{IterationIndex}", "report.txt");
                if (!File.Exists(reportPath))
                {
                    ExecuteCaptureAndCompare(IterationIndex);
                }
            };
        }

        [MenuItem("Tools/Look/Capture And Compare", false, 1)]
        public static void RunFromMenu()
        {
            ExecuteCaptureAndCompare(IterationIndex);
        }

        public static void ExecuteCaptureAndCompare(int iteration)
        {
            Debug.Log($"[LookCaptureAndCompare] Starting capture and compare for iteration {iteration}...");

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "LookReports", $"iteration_{iteration}");
            Directory.CreateDirectory(outputDir);

            // Load reference texture
            Texture2D refTex = null;
            if (File.Exists(ReferenceImagePath))
            {
                byte[] refBytes = File.ReadAllBytes(ReferenceImagePath);
                refTex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                refTex.LoadImage(refBytes);
            }
            else
            {
                Debug.LogWarning($"[LookCaptureAndCompare] Reference image not found at {ReferenceImagePath}");
            }

            // Define the 4 fixed capture spots
            // Door 214 is at (1.2, 0, 10), eye height 1.6
            Vector3 door214Center = new Vector3(1.2f, 1.6f, 10f);
            Vector3 shot3Pos = new Vector3(0f, 1.6f, 15f);
            Quaternion shot3Rot = Quaternion.LookRotation(door214Center - shot3Pos, Vector3.up);

            var spots = new List<CaptureSpot>()
            {
                new CaptureSpot("shot1_elevator", new Vector3(0f, 1.6f, 0.5f), Quaternion.Euler(0f, 0f, 0f)),
                new CaptureSpot("shot2_z8", new Vector3(0f, 1.6f, 8.0f), Quaternion.Euler(0f, 0f, 0f)),
                new CaptureSpot("shot3_door214", shot3Pos, shot3Rot),
                new CaptureSpot("shot4_looking_back", new Vector3(0f, 1.6f, 22.0f), Quaternion.Euler(0f, 180f, 0f))
            };

            // Setup camera
            Camera cam = SetupCaptureCamera();
            if (cam == null)
            {
                Debug.LogError("[LookCaptureAndCompare] Failed to locate or create capture camera.");
                return;
            }

            int width = 1920;
            int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            cam.targetTexture = rt;

            var metricsPerShot = new Dictionary<string, ShotMetrics>();

            try
            {
                for (int i = 0; i < spots.Count; i++)
                {
                    var spot = spots[i];
                    cam.transform.position = spot.Position;
                    cam.transform.rotation = spot.Rotation;

                    cam.Render();

                    RenderTexture.active = rt;
                    Texture2D shotTex = new Texture2D(width, height, TextureFormat.RGB24, false);
                    shotTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    shotTex.Apply();
                    RenderTexture.active = null;

                    // Compute metrics
                    ShotMetrics metrics = ComputeMetrics(shotTex);
                    metricsPerShot[spot.Name] = metrics;

                    // Save raw screenshot
                    byte[] rawPng = shotTex.EncodeToPNG();
                    File.WriteAllBytes(Path.Combine(outputDir, $"raw_{spot.Name}.png"), rawPng);

                    // Build and save side-by-side composite
                    if (refTex != null)
                    {
                        Texture2D composite = BuildSideBySide(refTex, shotTex);
                        byte[] compositePng = composite.EncodeToPNG();
                        File.WriteAllBytes(Path.Combine(outputDir, $"{spot.Name}.png"), compositePng);
                        UnityEngine.Object.DestroyImmediate(composite);
                    }

                    UnityEngine.Object.DestroyImmediate(shotTex);
                }
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                if (refTex != null) UnityEngine.Object.DestroyImmediate(refTex);
                TearDownCaptureCamera(cam);
            }

            // Generate report
            GenerateReport(iteration, spots, metricsPerShot, outputDir);
            Debug.Log($"[LookCaptureAndCompare] Report written to: {Path.Combine(outputDir, "report.txt")}");
        }

        private static Camera SetupCaptureCamera()
        {
            // Prefer existing player camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (var c in cams)
                {
                    if (c.gameObject.name.Contains("Main Camera") || c.CompareTag("MainCamera"))
                    {
                        mainCam = c;
                        break;
                    }
                }
            }

            if (mainCam != null)
            {
                var camData = mainCam.GetUniversalAdditionalCameraData();
                if (camData != null) camData.renderPostProcessing = true;
                return mainCam;
            }

            GameObject camGo = new GameObject("__LookCaptureCamera");
            Camera newCam = camGo.AddComponent<Camera>();
            newCam.fieldOfView = 65f;
            newCam.nearClipPlane = 0.05f;
            newCam.farClipPlane = 1000f;
            var data = newCam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
            return newCam;
        }

        private static void TearDownCaptureCamera(Camera cam)
        {
            if (cam != null && cam.gameObject.name == "__LookCaptureCamera")
            {
                UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }
        }

        private static ShotMetrics ComputeMetrics(Texture2D tex)
        {
            int w = tex.width;
            int h = tex.height;
            Color32[] pixels = tex.GetPixels32();
            int total = pixels.Length;

            float[] lumArray = new float[total];
            float rSum = 0f, gSum = 0f, bSum = 0f;
            int below10Count = 0;
            int below20Count = 0;

            List<float> litHues = new List<float>(total / 2);
            List<float> litSats = new List<float>(total / 2);

            float[,] grid = new float[w, h];

            for (int i = 0; i < total; i++)
            {
                Color32 c = pixels[i];
                float r = c.r / 255f;
                float g = c.g / 255f;
                float b = c.b / 255f;

                rSum += r;
                gSum += g;
                bSum += b;

                // Relative luminance (sRGB gamma space)
                float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                lumArray[i] = lum;

                int x = i % w;
                int y = i / w;
                grid[x, y] = lum;

                if (lum < 0.10f) below10Count++;
                if (lum < 0.20f) below20Count++;

                if (lum > 0.05f)
                {
                    Color.RGBToHSV(new Color(r, g, b), out float H, out float S, out float V);
                    if (S > 0.05f)
                    {
                        litHues.Add(H * 360f);
                        litSats.Add(S);
                    }
                }
            }

            Array.Sort(lumArray);
            litHues.Sort();
            litSats.Sort();

            float meanLum = 0f;
            for (int i = 0; i < total; i++) meanLum += lumArray[i];
            meanLum /= total;

            ShotMetrics m = new ShotMetrics();
            m.MeanLuminance = meanLum;
            m.PctBelow10 = (below10Count / (float)total) * 100f;
            m.PctBelow20 = (below20Count / (float)total) * 100f;
            m.P95Luminance = lumArray[(int)(total * 0.95f)];
            m.P99Luminance = lumArray[(int)(total * 0.99f)];
            m.MedianHue = litHues.Count > 0 ? litHues[litHues.Count / 2] : 0f;
            m.MedianSaturation = litSats.Count > 0 ? litSats[litSats.Count / 2] : 0f;
            m.MeanR = rSum / total;
            m.MeanG = gSum / total;
            m.MeanB = bSum / total;

            // Regional Luminance (Unity Texture2D: y=0 is bottom, y=h is top)
            m.FloorLuminance = SampleRegionMean(grid, (int)(w * 0.35f), 0, (int)(w * 0.65f), (int)(h * 0.25f));
            m.FarEndLuminance = SampleRegionMean(grid, (int)(w * 0.44f), (int)(h * 0.44f), (int)(w * 0.56f), (int)(h * 0.56f));
            m.LeftWallLuminance = SampleRegionMean(grid, (int)(w * 0.05f), (int)(h * 0.35f), (int)(w * 0.25f), (int)(h * 0.70f));
            m.RightWallLuminance = SampleRegionMean(grid, (int)(w * 0.75f), (int)(h * 0.35f), (int)(w * 0.95f), (int)(h * 0.70f));
            m.CenterLuminance = m.FarEndLuminance;

            // 4 corners: 5% box
            float c1 = SampleRegionMean(grid, 0, 0, (int)(w * 0.05f), (int)(h * 0.05f));
            float c2 = SampleRegionMean(grid, (int)(w * 0.95f), 0, w, (int)(h * 0.05f));
            float c3 = SampleRegionMean(grid, 0, (int)(h * 0.95f), (int)(w * 0.05f), h);
            float c4 = SampleRegionMean(grid, (int)(w * 0.95f), (int)(h * 0.95f), w, h);
            m.CornerLuminance = (c1 + c2 + c3 + c4) * 0.25f;

            // High-frequency grain standard deviation
            double diffSum = 0, diffSqSum = 0;
            int diffCount = 0;
            for (int y = 2; y < h - 2; y += 2)
            {
                for (int x = 2; x < w - 2; x += 2)
                {
                    float lap = grid[x, y] - 0.25f * (grid[x - 1, y] + grid[x + 1, y] + grid[x, y - 1] + grid[x, y + 1]);
                    diffSum += lap;
                    diffSqSum += lap * lap;
                    diffCount++;
                }
            }
            double dMean = diffSum / diffCount;
            double dVar = (diffSqSum / diffCount) - (dMean * dMean);
            m.GrainStd = (float)Math.Sqrt(Math.Max(0, dVar * 0.8));

            return m;
        }

        private static float SampleRegionMean(float[,] grid, int xMin, int yMin, int xMax, int yMax)
        {
            float sum = 0f;
            int count = 0;
            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    sum += grid[x, y];
                    count++;
                }
            }
            return count > 0 ? sum / count : 0f;
        }

        private static Texture2D BuildSideBySide(Texture2D leftRef, Texture2D rightShot)
        {
            int h = 1080;
            int halfW = 1920;
            int fullW = halfW * 2;

            Texture2D composite = new Texture2D(fullW, h, TextureFormat.RGB24, false);

            Color[] leftColors = leftRef.width == halfW && leftRef.height == h ? leftRef.GetPixels() : ScalePixels(leftRef, halfW, h);
            Color[] rightColors = rightShot.width == halfW && rightShot.height == h ? rightShot.GetPixels() : ScalePixels(rightShot, halfW, h);

            Color[] compColors = new Color[fullW * h];

            for (int y = 0; y < h; y++)
            {
                int srcRow = y * halfW;
                int dstRow = y * fullW;

                Array.Copy(leftColors, srcRow, compColors, dstRow, halfW);
                Array.Copy(rightColors, srcRow, compColors, dstRow + halfW, halfW);
            }

            composite.SetPixels(compColors);
            composite.Apply();
            return composite;
        }

        private static Color[] ScalePixels(Texture2D src, int targetW, int targetH)
        {
            Color[] scaled = new Color[targetW * targetH];
            for (int y = 0; y < targetH; y++)
            {
                float v = (float)y / targetH;
                for (int x = 0; x < targetW; x++)
                {
                    float u = (float)x / targetW;
                    scaled[y * targetW + x] = src.GetPixelBilinear(u, v);
                }
            }
            return scaled;
        }

        private static void GenerateReport(int iteration, List<CaptureSpot> spots, Dictionary<string, ShotMetrics> metrics, string outputDir)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=========================================================================================================");
            sb.AppendLine($"               LOOK CAPTURE AND COMPARE REPORT — ITERATION {iteration} (BASELINE)                        ");
            sb.AppendLine("=========================================================================================================");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Reference Target: {ReferenceImagePath} (1920x1080 sRGB)");
            sb.AppendLine("=========================================================================================================\n");

            sb.AppendLine(string.Format("{0,-20} | {1,-8} | {2,-12} | {3,-10} | {4,-10} | {5,-10} | {6,-10} | {7,-6}",
                "Metric", "Target", "Acceptable", "Shot 1", "Shot 2", "Shot 3", "Shot 4", "Pass"));
            sb.AppendLine(new string('-', 105));

            int totalChecks = 0;
            int passChecks = 0;

            foreach (var t in TargetDefinitions)
            {
                float s1 = t.Getter(metrics[spots[0].Name]);
                float s2 = t.Getter(metrics[spots[1].Name]);
                float s3 = t.Getter(metrics[spots[2].Name]);
                float s4 = t.Getter(metrics[spots[3].Name]);

                bool p1 = t.IsPass(s1);
                bool p2 = t.IsPass(s2);
                bool p3 = t.IsPass(s3);
                bool p4 = t.IsPass(s4);

                bool allPass = p1 && p2 && p3 && p4;
                if (p1) passChecks++;
                if (p2) passChecks++;
                if (p3) passChecks++;
                if (p4) passChecks++;
                totalChecks += 4;

                string acceptStr = t.Unit == "%" ? $"{t.Min:0}-{t.Max:0}%" :
                                   t.Unit == "°" ? $"{t.Min:0}-{t.Max:0}°" :
                                   $"{t.Min:0.00}-{t.Max:0.00}";
                string targetStr = t.Unit == "%" ? $"{t.Target:0}%" :
                                   t.Unit == "°" ? $"{t.Target:0}°" :
                                   $"{t.Target:0.00}";

                sb.AppendLine(string.Format("{0,-20} | {1,-8} | {2,-12} | {3,-10} | {4,-10} | {5,-10} | {6,-10} | {7,-6}",
                    t.Name, targetStr, acceptStr,
                    FormatValue(s1, p1, t.Unit),
                    FormatValue(s2, p2, t.Unit),
                    FormatValue(s3, p3, t.Unit),
                    FormatValue(s4, p4, t.Unit),
                    allPass ? "PASS" : "FAIL"));
            }

            sb.AppendLine(new string('-', 105));
            float passRate = (passChecks / (float)totalChecks) * 100f;
            sb.AppendLine($"SUMMARY: {passChecks} / {totalChecks} checks passed ({passRate:F1}% compliance rate)");
            sb.AppendLine($"Target Threshold for Step 2 completion: >= 90.0% PASS\n");

            sb.AppendLine("Generated Artifacts:");
            foreach (var s in spots)
            {
                sb.AppendLine($"  - Side-by-side composite: {outputDir}/{s.Name}.png");
                sb.AppendLine($"  - Captured raw view:      {outputDir}/raw_{s.Name}.png");
            }

            File.WriteAllText(Path.Combine(outputDir, "report.txt"), sb.ToString());

            // Also output Markdown version
            GenerateMarkdownReport(iteration, spots, metrics, outputDir, passChecks, totalChecks, passRate);
        }

        private static void GenerateMarkdownReport(int iteration, List<CaptureSpot> spots, Dictionary<string, ShotMetrics> metrics, string outputDir, int passChecks, int totalChecks, float passRate)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Look Capture & Compare Report — Iteration {iteration} (Baseline)\n");
            sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}  ");
            sb.AppendLine($"**Reference:** `{ReferenceImagePath}`  \n");

            sb.AppendLine("| Metric | Target | Acceptable Range | Shot 1 (Elevator) | Shot 2 (Z=8) | Shot 3 (Door 214) | Shot 4 (Look Back) | Status |");
            sb.AppendLine("| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |");

            foreach (var t in TargetDefinitions)
            {
                float s1 = t.Getter(metrics[spots[0].Name]);
                float s2 = t.Getter(metrics[spots[1].Name]);
                float s3 = t.Getter(metrics[spots[2].Name]);
                float s4 = t.Getter(metrics[spots[3].Name]);

                bool p1 = t.IsPass(s1);
                bool p2 = t.IsPass(s2);
                bool p3 = t.IsPass(s3);
                bool p4 = t.IsPass(s4);

                bool allPass = p1 && p2 && p3 && p4;

                string acceptStr = t.Unit == "%" ? $"{t.Min:0}–{t.Max:0}%" :
                                   t.Unit == "°" ? $"{t.Min:0}–{t.Max:0}°" :
                                   $"{t.Min:0.00}–{t.Max:0.00}";
                string targetStr = t.Unit == "%" ? $"{t.Target:0}%" :
                                   t.Unit == "°" ? $"{t.Target:0}°" :
                                   $"{t.Target:0.00}";

                sb.AppendLine($"| **{t.Name}** | {targetStr} | {acceptStr} | {FormatMdValue(s1, p1, t.Unit)} | {FormatMdValue(s2, p2, t.Unit)} | {FormatMdValue(s3, p3, t.Unit)} | {FormatMdValue(s4, p4, t.Unit)} | {(allPass ? "✅ PASS" : "❌ FAIL")} |");
            }

            sb.AppendLine($"\n**Overall Compliance:** {passChecks}/{totalChecks} checks ({passRate:F1}% passed)\n");
            sb.AppendLine("### Side-by-Side Captures (Reference Left | Render Right)\n");
            foreach (var s in spots)
            {
                sb.AppendLine($"- **{s.Name}:** `LookReports/iteration_{iteration}/{s.Name}.png`");
            }

            File.WriteAllText(Path.Combine(outputDir, "report.md"), sb.ToString());
        }

        private static string FormatValue(float val, bool pass, string unit)
        {
            string s = unit == "%" ? $"{val:F1}%" :
                       unit == "°" ? $"{val:F0}°" :
                       $"{val:F3}";
            return pass ? $"{s} (OK)" : $"{s} (!)";
        }

        private static string FormatMdValue(float val, bool pass, string unit)
        {
            string s = unit == "%" ? $"{val:F1}%" :
                       unit == "°" ? $"{val:F0}°" :
                       $"{val:F3}";
            return pass ? s : $"**{s}**";
        }
    }
}
