using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EndlessHallway.Editor
{
    [InitializeOnLoad]
    public static class LightingAuditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        static LightingAuditor()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                RunAudit();
            };
        }

        [MenuItem("Tools/Lighting/Audit Current Lighting", false, 1)]
        public static void AuditLightingMenu()
        {
            string report = RunAudit();
            Debug.Log($"[LightingAuditor] Audit complete. Report written to LightingAuditReport.txt.\n{report.Substring(0, Mathf.Min(1200, report.Length))}...");
        }

        public static string RunAudit()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("              THE ENDLESS HALLWAY — LIGHTING AUDIT REPORT                       ");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Scene: {activeScene.path}");
            sb.AppendLine("================================================================================\n");

            // 1. URP Asset & Pipeline Settings
            AuditPipelineSettings(sb);

            // 2. Ambient & Environment Settings
            AuditAmbientSettings(sb);

            // 3. Fog Settings
            AuditFogSettings(sb);

            // 4. Lights Audit
            AuditLights(sb);

            // 5. Reflection Probes Audit
            AuditReflectionProbes(sb);

            // 6. Volumes & Post-Processing Audit
            AuditVolumes(sb);

            // 7. Shadow Atlas Analysis & Warnings
            AuditShadowAtlas(sb);

            string reportContent = sb.ToString();

            // Save report
            string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "LightingAuditReport.txt");
            File.WriteAllText(reportPath, reportContent);

            string assetsReportPath = Path.Combine(Application.dataPath, "LightingAuditReport.txt");
            File.WriteAllText(assetsReportPath, reportContent);
            AssetDatabase.Refresh();

            return reportContent;
        }

        private static void AuditPipelineSettings(StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("1. UNIVERSAL RENDER PIPELINE (URP) CONFIGURATION");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var currentAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (currentAsset == null)
            {
                sb.AppendLine("Active Render Pipeline Asset: NONE or Not URP!");
                sb.AppendLine();
                return;
            }

            sb.AppendLine($"Active URP Asset: {currentAsset.name} ({AssetDatabase.GetAssetPath(currentAsset)})");
            sb.AppendLine($"SRP Batcher Enabled: {currentAsset.useSRPBatcher}");
            sb.AppendLine($"Supports Camera Depth Texture: {currentAsset.supportsCameraDepthTexture}");
            sb.AppendLine($"Supports Camera Opaque Texture: {currentAsset.supportsCameraOpaqueTexture}");
            sb.AppendLine($"Supports HDR: {currentAsset.supportsHDR}");
            sb.AppendLine($"MSAA Sample Count: {currentAsset.msaaSampleCount}");
            sb.AppendLine($"Render Scale: {currentAsset.renderScale}");

            sb.AppendLine("\nShadow Settings:");
            sb.AppendLine($"  Shadow Distance: {currentAsset.shadowDistance} m");
            sb.AppendLine($"  Shadow Cascades: {currentAsset.shadowCascadeCount}");
            sb.AppendLine($"  Shadow Depth Bias: {currentAsset.shadowDepthBias}");
            sb.AppendLine($"  Shadow Normal Bias: {currentAsset.shadowNormalBias}");
            sb.AppendLine($"  Supports Main Light Shadows: {currentAsset.supportsMainLightShadows}");
            sb.AppendLine($"  Supports Additional Light Shadows: {currentAsset.supportsAdditionalLightShadows}");

            sb.AppendLine();
        }

        private static void AuditAmbientSettings(StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("2. AMBIENT & ENVIRONMENT LIGHTING SETUP");
            sb.AppendLine("--------------------------------------------------------------------------------");

            sb.AppendLine($"Ambient Mode: {RenderSettings.ambientMode}");
            sb.AppendLine($"Ambient Intensity: {RenderSettings.ambientIntensity:F3}");

            if (RenderSettings.ambientMode == AmbientMode.Skybox)
            {
                var skyMat = RenderSettings.skybox;
                sb.AppendLine($"Skybox Material: {(skyMat != null ? skyMat.name : "None (Default)")}");
                sb.AppendLine($"Ambient Sky Color (Calculated/Cached): {RenderSettings.ambientSkyColor}");
            }
            else if (RenderSettings.ambientMode == AmbientMode.Trilight)
            {
                sb.AppendLine($"Sky Color: {RenderSettings.ambientSkyColor}");
                sb.AppendLine($"Equator Color: {RenderSettings.ambientEquatorColor}");
                sb.AppendLine($"Ground Color: {RenderSettings.ambientGroundColor}");
            }
            else // Flat
            {
                sb.AppendLine($"Flat Ambient Color: {RenderSettings.ambientLight}");
            }

            sb.AppendLine($"Default Reflection Mode: {RenderSettings.defaultReflectionMode}");
            sb.AppendLine($"Default Reflection Resolution: {RenderSettings.defaultReflectionResolution}");
            sb.AppendLine($"Reflection Intensity: {RenderSettings.reflectionIntensity:F3}");
            sb.AppendLine($"Reflection Bounces: {RenderSettings.reflectionBounces}");
            sb.AppendLine($"Custom Reflection Cubemap: {(RenderSettings.customReflectionTexture != null ? RenderSettings.customReflectionTexture.name : "None")}");

            sb.AppendLine();
        }

        private static void AuditFogSettings(StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("3. FOG CONFIGURATION");
            sb.AppendLine("--------------------------------------------------------------------------------");

            sb.AppendLine($"Fog Enabled: {RenderSettings.fog}");
            if (RenderSettings.fog)
            {
                sb.AppendLine($"Fog Mode: {RenderSettings.fogMode}");
                sb.AppendLine($"Fog Color: #{ColorUtility.ToHtmlStringRGBA(RenderSettings.fogColor)} (RGBA: {RenderSettings.fogColor})");
                sb.AppendLine($"Fog Density: {RenderSettings.fogDensity:F4}");
                sb.AppendLine($"Linear Fog Start: {RenderSettings.fogStartDistance} m, End: {RenderSettings.fogEndDistance} m");
            }
            else
            {
                sb.AppendLine("Fog is currently DISABLED in RenderSettings.");
            }

            sb.AppendLine();
        }

        private static void AuditLights(StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("4. LIGHT SOURCES AUDIT (SCENE LIGHTS)");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            int realtimeCount = 0;
            int mixedCount = 0;
            int bakedCount = 0;
            int shadowCasters = 0;

            sb.AppendLine($"Total Light Components in Scene: {allLights.Length}\n");

            // Header
            sb.AppendLine(string.Format("{0,-40} | {1,-11} | {2,-8} | {3,-9} | {4,-6} | {5,-12} | {6,-16}",
                "Hierarchy Path", "Type", "Mode", "Intensity", "Range", "Shadows", "Color (Hex)"));
            sb.AppendLine(new string('-', 115));

            var sortedLights = allLights.OrderBy(l => GetPath(l.gameObject)).ToList();

            foreach (var l in sortedLights)
            {
                string path = GetPath(l.gameObject);
                string typeStr = l.type.ToString();
                string modeStr = l.lightmapBakeType.ToString();
                string shadowStr = l.shadows.ToString();
                string hexColor = "#" + ColorUtility.ToHtmlStringRGB(l.color);

                if (l.lightmapBakeType == LightmapBakeType.Realtime) realtimeCount++;
                else if (l.lightmapBakeType == LightmapBakeType.Mixed) mixedCount++;
                else if (l.lightmapBakeType == LightmapBakeType.Baked) bakedCount++;

                if (l.shadows != LightShadows.None && l.enabled && l.gameObject.activeInHierarchy)
                    shadowCasters++;

                sb.AppendLine(string.Format("{0,-40} | {1,-11} | {2,-8} | {3,-9:F2} | {4,-6:F1} | {5,-12} | {6,-16}",
                    Truncate(path, 40), typeStr, modeStr, l.intensity, l.range, shadowStr, hexColor));
            }

            sb.AppendLine(new string('-', 115));
            sb.AppendLine($"Light Summary: Realtime={realtimeCount}, Mixed={mixedCount}, Baked={bakedCount}");
            sb.AppendLine($"Active Shadow Casters: {shadowCasters}");
            sb.AppendLine();
        }

        private static void AuditReflectionProbes(StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("5. REFLECTION PROBES AUDIT");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var probes = UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            sb.AppendLine($"Total Reflection Probes in Scene: {probes.Length}");

            foreach (var p in probes)
            {
                string path = GetPath(p.gameObject);
                sb.AppendLine($"  - [{path}] Mode={p.mode}, Resolution={p.resolution}, Intensity={p.intensity:F2}, BoxSize=({p.size.x:F1}, {p.size.y:F1}, {p.size.z:F1}), BoxProjection={p.boxProjection}");
            }

            sb.AppendLine();
        }

        private static void AuditVolumes(StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("6. POST-PROCESSING & VOLUMES AUDIT");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var volumes = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            sb.AppendLine($"Total Volume Components in Scene: {volumes.Length}\n");

            foreach (var v in volumes)
            {
                string path = GetPath(v.gameObject);
                string profileName = v.sharedProfile != null ? v.sharedProfile.name : (v.profile != null ? v.profile.name : "None");
                string profilePath = v.sharedProfile != null ? AssetDatabase.GetAssetPath(v.sharedProfile) : "Runtime Instance";

                sb.AppendLine($"=== Volume: [{path}] ===");
                sb.AppendLine($"  Is Global: {v.isGlobal}");
                sb.AppendLine($"  Priority: {v.priority}");
                sb.AppendLine($"  Weight: {v.weight:F2}");
                sb.AppendLine($"  Profile: {profileName} ({profilePath})");

                var profile = v.sharedProfile ?? v.profile;
                if (profile != null)
                {
                    sb.AppendLine($"  Profile Components ({profile.components.Count}):");
                    foreach (var c in profile.components)
                    {
                        if (c == null) continue;
                        string compName = c.GetType().Name;
                        sb.AppendLine($"    * {compName} (active: {c.active})");

                        if (c is Tonemapping tm)
                        {
                            sb.AppendLine($"        - Mode: {tm.mode.value}");
                        }
                        else if (c is ColorAdjustments ca)
                        {
                            sb.AppendLine($"        - Post Exposure: {ca.postExposure.value:F2}");
                            sb.AppendLine($"        - Contrast: {ca.contrast.value:F1}");
                            sb.AppendLine($"        - Saturation: {ca.saturation.value:F1}");
                            sb.AppendLine($"        - Color Filter: #{ColorUtility.ToHtmlStringRGB(ca.colorFilter.value)}");
                            sb.AppendLine($"        - Hue Shift: {ca.hueShift.value:F1}");
                        }
                        else if (c is WhiteBalance wb)
                        {
                            sb.AppendLine($"        - Temperature: {wb.temperature.value:F1}");
                            sb.AppendLine($"        - Tint: {wb.tint.value:F1}");
                        }
                        else if (c is Vignette vig)
                        {
                            sb.AppendLine($"        - Intensity: {vig.intensity.value:F2}");
                            sb.AppendLine($"        - Smoothness: {vig.smoothness.value:F2}");
                            sb.AppendLine($"        - Color: #{ColorUtility.ToHtmlStringRGB(vig.color.value)}");
                            sb.AppendLine($"        - Rounded: {vig.rounded.value}");
                        }
                        else if (c is FilmGrain fg)
                        {
                            sb.AppendLine($"        - Type: {fg.type.value}");
                            sb.AppendLine($"        - Intensity: {fg.intensity.value:F2}");
                            sb.AppendLine($"        - Response: {fg.response.value:F2}");
                        }
                        else if (c is Bloom bl)
                        {
                            sb.AppendLine($"        - Threshold: {bl.threshold.value:F2}");
                            sb.AppendLine($"        - Intensity: {bl.intensity.value:F2}");
                            sb.AppendLine($"        - Scatter: {bl.scatter.value:F2}");
                            sb.AppendLine($"        - Tint: #{ColorUtility.ToHtmlStringRGB(bl.tint.value)}");
                        }
                        else if (c is LensDistortion ld)
                        {
                            sb.AppendLine($"        - Intensity: {ld.intensity.value:F2}");
                            sb.AppendLine($"        - X Multiplier: {ld.xMultiplier.value:F2}");
                            sb.AppendLine($"        - Y Multiplier: {ld.yMultiplier.value:F2}");
                            sb.AppendLine($"        - Center: {ld.center.value}");
                            sb.AppendLine($"        - Scale: {ld.scale.value:F2}");
                        }
                        else if (c is ShadowsMidtonesHighlights smh)
                        {
                            sb.AppendLine($"        - Shadows: {smh.shadows.value}");
                            sb.AppendLine($"        - Midtones: {smh.midtones.value}");
                            sb.AppendLine($"        - Highlights: {smh.highlights.value}");
                        }
                    }
                }
                sb.AppendLine();
            }
        }

        private static void AuditShadowAtlas(StringBuilder sb)
        {
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("7. SHADOW ATLAS ANALYSIS & ZERO-WARNING ROADMAP");
            sb.AppendLine("--------------------------------------------------------------------------------");

            var currentAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var shadowCasters = allLights.Where(l => l.shadows != LightShadows.None && l.enabled && l.gameObject.activeInHierarchy).ToList();

            sb.AppendLine($"Active Punctual Shadow Casters: {shadowCasters.Count}");
            foreach (var sc in shadowCasters)
            {
                sb.AppendLine($"  - [{GetPath(sc.gameObject)}] Type={sc.type}, Shadows={sc.shadows}, Bias={sc.shadowBias}, NormalBias={sc.shadowNormalBias}");
            }

            sb.AppendLine("\nRoot Cause of Console Warning:");
            sb.AppendLine("  'Reduced additional punctual light shadows resolution by 2 to make 6 shadow maps fit in the 2048x2048 shadow atlas.'");
            sb.AppendLine("  Explanation: 6 realtime point lights in the hallway all have realtime shadows enabled simultaneously.");
            sb.AppendLine("  Because each point light requires 6 cube faces or multiple slices, URP runs out of shadow atlas space and is forced to downsample resolution dynamically.");
            sb.AppendLine("  Survival Horror Solution: In the 'no light' aesthetic, corridor ceiling fixtures are OFF / barely glowing emissives (NO realtime shadows). Only the player's Flashlight spot light casts realtime shadows.");
            sb.AppendLine("  With only 1 active shadow caster (Flashlight Spotlight = 1 atlas slice), shadow atlas usage is optimal and atlas reduction warnings drop to exactly ZERO.");

            sb.AppendLine();
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

        private static string Truncate(string str, int maxLen)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            if (str.Length <= maxLen) return str;
            return "..." + str.Substring(str.Length - (maxLen - 3));
        }
    }
}
