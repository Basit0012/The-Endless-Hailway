using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using EndlessHallway.Core;
using EndlessHallway.UI;

namespace EndlessHallway.Editor
{
    public static class RetroUIBuilder
    {
        private const string ART_UI_DIR = "Assets/_Project/Art/UI";
        private const string AUDIO_DIR = "Assets/_Project/Audio";

        [MenuItem("Tools/Endless Hallway/UI: Rebuild Complete 8-Bit UI System", false, 1)]
        public static void BuildCompleteRetroUISystem()
        {
            Debug.Log("[RetroUIBuilder] Starting complete 8-bit UI system rebuild...");

            EnsureDirectories();
            GenerateRetroSprites();
            GenerateRetroAudio();
            AssetDatabase.Refresh();

            SetupCanvasAndManagers();
            BuildMainMenuPanel();
            BuildPauseMenuPanel();
            BuildGameOverPanel();
            BuildSettingsPanel();
            BuildCreditsPanel();
            BuildCRTScanlineOverlay();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RetroUIBuilder] Complete 8-bit UI System successfully built and saved!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(ART_UI_DIR)) Directory.CreateDirectory(ART_UI_DIR);
            if (!Directory.Exists(AUDIO_DIR)) Directory.CreateDirectory(AUDIO_DIR);
        }

        #region 1. Procedural Sprite Generation
        public static void GenerateRetroSprites()
        {
            EnsureDirectories();

            // 1. Panel Retro Frame (64x64, 3px solid border, 1px dark inset gap, dark void fill)
            GenerateSlicedSprite(ART_UI_DIR + "/spr_ui_panel_retro.png", 64, 64, (x, y, w, h) =>
            {
                int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (edge < 3) return new Color(0.78f, 0.78f, 0.82f, 1f); // Crisp outer border
                if (edge < 4) return new Color(0.08f, 0.08f, 0.10f, 1f); // Dark inset line
                return new Color(0.05f, 0.06f, 0.08f, 0.96f);             // Dark card body
            }, new Vector4(8, 8, 8, 8));

            // 2. Button Normal (64x64, 2px border, dark zinc fill)
            GenerateSlicedSprite(ART_UI_DIR + "/spr_ui_button_normal.png", 64, 64, (x, y, w, h) =>
            {
                int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (edge < 2) return new Color(0.35f, 0.36f, 0.42f, 1f); // Crisp border (#595c6b)
                return new Color(0.08f, 0.09f, 0.11f, 1f);               // Dark zinc fill
            }, new Vector4(6, 6, 6, 6));

            // 3. Button Hover (64x64, 2px brilliant white/amber border, elevated fill, corner pixel notches)
            GenerateSlicedSprite(ART_UI_DIR + "/spr_ui_button_hover.png", 64, 64, (x, y, w, h) =>
            {
                int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                bool isCorner = (x < 2 || x >= w - 2) && (y < 2 || y >= h - 2);
                if (isCorner) return new Color(0.98f, 0.88f, 0.35f, 1f); // Corner pixel accent
                if (edge < 2) return new Color(0.96f, 0.94f, 0.90f, 1f); // Brilliant white border
                return new Color(0.16f, 0.18f, 0.22f, 1f);               // Elevated grey fill
            }, new Vector4(6, 6, 6, 6));

            // 4. Button Pressed (64x64, 2px dark border, recessed deep black fill)
            GenerateSlicedSprite(ART_UI_DIR + "/spr_ui_button_pressed.png", 64, 64, (x, y, w, h) =>
            {
                int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (edge < 2) return new Color(0.25f, 0.26f, 0.30f, 1f);
                return new Color(0.04f, 0.04f, 0.05f, 1f);
            }, new Vector4(6, 6, 6, 6));

            // 5. Button Disabled (64x64, 2px dim border, muted fill)
            GenerateSlicedSprite(ART_UI_DIR + "/spr_ui_button_disabled.png", 64, 64, (x, y, w, h) =>
            {
                int edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (edge < 2) return new Color(0.18f, 0.19f, 0.22f, 0.8f);
                return new Color(0.06f, 0.06f, 0.07f, 0.8f);
            }, new Vector4(6, 6, 6, 6));

            // 6. Scanlines pattern (16x16, repeating horizontal lines)
            GenerateScanlinesTexture(ART_UI_DIR + "/spr_ui_scanlines.png", 16, 16);

            AssetDatabase.Refresh();
        }

        private static void GenerateSlicedSprite(string path, int w, int h, Func<int, int, int, int, Color> colorFunc, Vector4 border)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, colorFunc(x, y, w, h));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void GenerateScanlinesTexture(string path, int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                bool line = (y % 4 < 2);
                Color c = line ? new Color(0f, 0f, 0f, 0.22f) : new Color(0f, 0f, 0f, 0f);
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
        #endregion

        #region 2. Procedural Audio Generation
        public static void GenerateRetroAudio()
        {
            EnsureDirectories();
            int sr = 44100;

            // 1. UI Hover tick (20ms soft sine burst at 880Hz)
            int hoverTotal = (int)(sr * 0.020f);
            float[] hoverSamples = new float[hoverTotal];
            for (int i = 0; i < hoverTotal; i++)
            {
                float t = (float)i / hoverTotal;
                float env = Mathf.Sin(t * Mathf.PI);
                hoverSamples[i] = Mathf.Sin(2f * Mathf.PI * 880f * ((float)i / sr)) * env * 0.35f;
            }
            WriteWav(AUDIO_DIR + "/ui_hover.wav", hoverSamples, sr);

            // 2. UI Click mechanical snap (35ms crisp typewriter click)
            int clickTotal = (int)(sr * 0.035f);
            float[] clickSamples = new float[clickTotal];
            var rng = new System.Random(42);
            for (int i = 0; i < clickTotal; i++)
            {
                float t = (float)i / clickTotal;
                float env = Mathf.Exp(-t * 8f);
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.4f;
                float pop = Mathf.Sin(2f * Mathf.PI * 180f * ((float)i / sr)) * 0.6f;
                clickSamples[i] = (noise + pop) * env * 0.75f;
            }
            WriteWav(AUDIO_DIR + "/ui_click.wav", clickSamples, sr);

            AssetDatabase.Refresh();
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

        #region 3. Canvas & Managers Setup
        private static GameObject GetRootCanvas()
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.transform.parent == null)
                {
                    ConfigureCanvasScaler(c);
                    return c.gameObject;
                }
            }
            var root = GameObject.Find("Canvas");
            if (root != null && root.transform.parent == null)
            {
                var c = root.GetComponent<Canvas>();
                if (c != null) ConfigureCanvasScaler(c);
                return root;
            }

            var newCanvas = new GameObject("Canvas");
            var canvasComp = newCanvas.AddComponent<Canvas>();
            ConfigureCanvasScaler(canvasComp);
            newCanvas.AddComponent<GraphicRaycaster>();
            return newCanvas;
        }

        private static void ConfigureCanvasScaler(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        private static void SetupCanvasAndManagers()
        {
            var canvasObj = GetRootCanvas();

            // Ensure EventSystem
            var eventSystem = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Ensure UIManager on Managers
            var mgrs = GameObject.Find("Managers");
            if (mgrs == null) mgrs = new GameObject("Managers");
            var uiMgr = mgrs.GetComponent<UIManager>();
            if (uiMgr == null) uiMgr = mgrs.AddComponent<UIManager>();

            // Ensure HUD Layer container
            var hudLayer = canvasObj.transform.Find("HUD_Layer");
            if (hudLayer == null)
            {
                var hudObj = new GameObject("HUD_Layer");
                hudObj.transform.SetParent(canvasObj.transform, false);
                var hRect = hudObj.AddComponent<RectTransform>();
                hRect.anchorMin = Vector2.zero;
                hRect.anchorMax = Vector2.one;
                hRect.offsetMin = Vector2.zero;
                hRect.offsetMax = Vector2.zero;
                hudLayer = hudObj.transform;
            }

            // Move Reticle, PromptContainer, SubtitlePanel into HUD_Layer
            var reticle = canvasObj.transform.Find("Reticle");
            if (reticle != null) reticle.SetParent(hudLayer, false);

            var promptContainer = canvasObj.transform.Find("PromptContainer");
            if (promptContainer != null) promptContainer.SetParent(hudLayer, false);

            var subPanel = canvasObj.transform.Find("SubtitlePanel");
            if (subPanel != null) subPanel.SetParent(hudLayer, false);

            SetField(uiMgr, "hudLayer", hudLayer.gameObject);
        }
        #endregion

        #region 4. Main Menu Panel Rebuild
        private static void BuildMainMenuPanel()
        {
            var canvasObj = GetRootCanvas();
            var panelRetroSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_panel_retro.png");
            var btnNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_normal.png");
            var btnHoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_hover.png");
            var btnPressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_pressed.png");
            var btnDisabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_disabled.png");
            var hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_DIR + "/ui_hover.wav");
            var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_DIR + "/ui_click.wav");

            var oldMenu = canvasObj.transform.Find("MainMenuPanel");
            if (oldMenu != null) UnityEngine.Object.DestroyImmediate(oldMenu.gameObject);

            var menuPanel = new GameObject("MainMenuPanel");
            menuPanel.transform.SetParent(canvasObj.transform, false);
            var mRect = menuPanel.AddComponent<RectTransform>();
            mRect.anchorMin = Vector2.zero;
            mRect.anchorMax = Vector2.one;
            mRect.offsetMin = Vector2.zero;
            mRect.offsetMax = Vector2.zero;

            var bgImg = menuPanel.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.03f, 0.04f, 0.95f);

            var cg = menuPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            // Modal Card (520x660)
            var cardObj = new GameObject("Card");
            cardObj.transform.SetParent(menuPanel.transform, false);
            var cRect = cardObj.AddComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(520, 660);
            var cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = panelRetroSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;

            // Title Header
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.80f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "THE ENDLESS HALLWAY";
            tTmp.fontSize = 32f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.characterSpacing = 15f;
            tTmp.color = new Color(0.96f, 0.92f, 0.78f, 1f);
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.raycastTarget = false;

            // Subtitle
            var subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(cardObj.transform, false);
            var sRect = subObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.05f, 0.73f);
            sRect.anchorMax = new Vector2(0.95f, 0.81f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;
            var sTmp = subObj.AddComponent<TextMeshProUGUI>();
            sTmp.text = "MARROW POINT RESIDENCES // APARTMENT 214";
            sTmp.fontSize = 13f;
            sTmp.characterSpacing = 12f;
            sTmp.color = new Color(0.55f, 0.68f, 0.70f, 1f);
            sTmp.alignment = TextAlignmentOptions.Center;
            sTmp.raycastTarget = false;

            // Buttons Container
            var btnGroup = new GameObject("ButtonsGroup");
            btnGroup.transform.SetParent(cardObj.transform, false);
            var bgRect = btnGroup.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.08f, 0.08f);
            bgRect.anchorMax = new Vector2(0.92f, 0.70f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var vlg = btnGroup.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 5 Required Buttons
            var startBtn = Create8BitButton(btnGroup.transform, "StartGameButton", "[ START GAME ]", 420, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var contBtn = Create8BitButton(btnGroup.transform, "ContinueButton", "[ CONTINUE ]", 420, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var optBtn = Create8BitButton(btnGroup.transform, "OptionsButton", "[ OPTIONS ]", 420, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var credBtn = Create8BitButton(btnGroup.transform, "CreditsButton", "[ CREDITS ]", 420, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var quitBtn = Create8BitButton(btnGroup.transform, "QuitButton", "[ QUIT ]", 420, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);

            // Wire MainMenuUI
            var mainMenuUI = canvasObj.GetComponent<MainMenuUI>();
            if (mainMenuUI == null) mainMenuUI = canvasObj.AddComponent<MainMenuUI>();

            SetField(mainMenuUI, "menuPanel", menuPanel);
            SetField(mainMenuUI, "continueButton", contBtn);
            SetField(mainMenuUI, "newGameButton", startBtn);
            SetField(mainMenuUI, "settingsButton", optBtn);
            SetField(mainMenuUI, "creditsButton", credBtn);
            SetField(mainMenuUI, "quitButton", quitBtn);

            var uiMgr = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (uiMgr != null) SetField(uiMgr, "mainMenuUI", mainMenuUI);

            menuPanel.SetActive(true);
        }
        #endregion

        #region 5. Pause Menu Panel Rebuild
        private static void BuildPauseMenuPanel()
        {
            var canvasObj = GetRootCanvas();
            var panelRetroSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_panel_retro.png");
            var btnNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_normal.png");
            var btnHoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_hover.png");
            var btnPressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_pressed.png");
            var btnDisabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_disabled.png");
            var hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_DIR + "/ui_hover.wav");
            var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_DIR + "/ui_click.wav");

            var oldPause = canvasObj.transform.Find("PauseMenuPanel");
            if (oldPause != null) UnityEngine.Object.DestroyImmediate(oldPause.gameObject);

            var pausePanel = new GameObject("PauseMenuPanel");
            pausePanel.transform.SetParent(canvasObj.transform, false);
            var pRect = pausePanel.AddComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;

            var bgImg = pausePanel.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.03f, 0.04f, 0.90f);

            var cg = pausePanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            // Modal Card (500x640)
            var cardObj = new GameObject("Card");
            cardObj.transform.SetParent(pausePanel.transform, false);
            var cRect = cardObj.AddComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(500, 640);
            var cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = panelRetroSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.81f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "PAUSED";
            tTmp.fontSize = 36f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.characterSpacing = 25f;
            tTmp.color = new Color(0.96f, 0.92f, 0.78f, 1f);
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.raycastTarget = false;

            // Loop Info
            var liObj = new GameObject("LoopInfo");
            liObj.transform.SetParent(cardObj.transform, false);
            var liRect = liObj.AddComponent<RectTransform>();
            liRect.anchorMin = new Vector2(0.05f, 0.74f);
            liRect.anchorMax = new Vector2(0.95f, 0.82f);
            liRect.offsetMin = Vector2.zero;
            liRect.offsetMax = Vector2.zero;
            var liTmp = liObj.AddComponent<TextMeshProUGUI>();
            liTmp.text = "CORRIDOR // LOOP 0";
            liTmp.fontSize = 13f;
            liTmp.characterSpacing = 12f;
            liTmp.color = new Color(0.55f, 0.68f, 0.70f, 1f);
            liTmp.alignment = TextAlignmentOptions.Center;
            liTmp.raycastTarget = false;

            // Buttons Container
            var btnGroup = new GameObject("ButtonsGroup");
            btnGroup.transform.SetParent(cardObj.transform, false);
            var bgRect = btnGroup.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.08f, 0.08f);
            bgRect.anchorMax = new Vector2(0.92f, 0.71f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var vlg = btnGroup.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var resumeBtn = Create8BitButton(btnGroup.transform, "ResumeButton", "[ RESUME ]", 400, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var restartBtn = Create8BitButton(btnGroup.transform, "RestartButton", "[ RESTART ]", 400, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var optBtn = Create8BitButton(btnGroup.transform, "OptionsButton", "[ OPTIONS ]", 400, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var mmBtn = Create8BitButton(btnGroup.transform, "MainMenuButton", "[ MAIN MENU ]", 400, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var quitBtn = Create8BitButton(btnGroup.transform, "QuitButton", "[ QUIT ]", 400, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);

            var pauseUI = pausePanel.AddComponent<PauseMenuUI>();
            SetField(pauseUI, "pausePanel", pausePanel);
            SetField(pauseUI, "resumeButton", resumeBtn);
            SetField(pauseUI, "restartLoopButton", restartBtn);
            SetField(pauseUI, "settingsButton", optBtn);
            SetField(pauseUI, "quitMainMenuButton", mmBtn);
            SetField(pauseUI, "quitGameButton", quitBtn);
            SetField(pauseUI, "loopInfoText", liTmp);

            var uiMgr = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (uiMgr != null) SetField(uiMgr, "pauseMenuUI", pauseUI);

            pausePanel.SetActive(false);
        }
        #endregion

        #region 6. Game Over Panel Rebuild
        private static void BuildGameOverPanel()
        {
            var canvasObj = GetRootCanvas();
            var panelRetroSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_panel_retro.png");
            var btnNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_normal.png");
            var btnHoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_hover.png");
            var btnPressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_pressed.png");
            var btnDisabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_disabled.png");
            var hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_DIR + "/ui_hover.wav");
            var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_DIR + "/ui_click.wav");
            var horrorTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/Paintings/T_Painting_Stage4.png");

            var oldOver = canvasObj.transform.Find("GameOverPanel");
            if (oldOver != null) UnityEngine.Object.DestroyImmediate(oldOver.gameObject);

            var goPanel = new GameObject("GameOverPanel");
            goPanel.transform.SetParent(canvasObj.transform, false);
            var goRect = goPanel.AddComponent<RectTransform>();
            goRect.anchorMin = Vector2.zero;
            goRect.anchorMax = Vector2.one;
            goRect.offsetMin = Vector2.zero;
            goRect.offsetMax = Vector2.zero;

            var bgImg = goPanel.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.02f, 0.02f, 0.96f);

            var cg = goPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // Modal Card (580x720)
            var cardObj = new GameObject("Card");
            cardObj.transform.SetParent(goPanel.transform, false);
            var cRect = cardObj.AddComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(580, 720);
            var cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = panelRetroSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.85f);
            tRect.anchorMax = new Vector2(0.95f, 0.96f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "GAME OVER";
            tTmp.fontSize = 40f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.characterSpacing = 25f;
            tTmp.color = new Color(0.92f, 0.25f, 0.25f, 1f); // Horror red
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.raycastTarget = false;

            // Subtitle
            var subObj = new GameObject("Prompt");
            subObj.transform.SetParent(cardObj.transform, false);
            var sRect = subObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.05f, 0.78f);
            sRect.anchorMax = new Vector2(0.95f, 0.85f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;
            var sTmp = subObj.AddComponent<TextMeshProUGUI>();
            sTmp.text = "Continue?";
            sTmp.fontSize = 20f;
            sTmp.fontStyle = FontStyles.Bold;
            sTmp.characterSpacing = 15f;
            sTmp.color = new Color(0.92f, 0.90f, 0.84f, 1f);
            sTmp.alignment = TextAlignmentOptions.Center;
            sTmp.raycastTarget = false;

            // Atmospheric Horror Image Frame (440x240)
            var imgFrameObj = new GameObject("HorrorImageFrame");
            imgFrameObj.transform.SetParent(cardObj.transform, false);
            var ifRect = imgFrameObj.AddComponent<RectTransform>();
            ifRect.anchorMin = new Vector2(0.5f, 0.56f);
            ifRect.anchorMax = new Vector2(0.5f, 0.56f);
            ifRect.sizeDelta = new Vector2(440, 240);
            var ifImg = imgFrameObj.AddComponent<Image>();
            ifImg.sprite = btnNormalSprite;
            ifImg.type = Image.Type.Sliced;
            ifImg.color = new Color(0.6f, 0.2f, 0.2f, 1f); // Reddish border frame

            var imgContentObj = new GameObject("ImageContent");
            imgContentObj.transform.SetParent(imgFrameObj.transform, false);
            var icRect = imgContentObj.AddComponent<RectTransform>();
            icRect.anchorMin = Vector2.zero;
            icRect.anchorMax = Vector2.one;
            icRect.offsetMin = new Vector2(4, 4);
            icRect.offsetMax = new Vector2(-4, -4);
            var icImg = imgContentObj.AddComponent<Image>();
            if (horrorTex != null)
            {
                var sp = Sprite.Create(horrorTex, new Rect(0, 0, horrorTex.width, horrorTex.height), new Vector2(0.5f, 0.5f));
                icImg.sprite = sp;
            }
            icImg.color = new Color(0.85f, 0.70f, 0.70f, 1f);
            icImg.preserveAspect = true;
            icImg.raycastTarget = false;

            // Secondary Lore Note
            var loreObj = new GameObject("LoreNote");
            loreObj.transform.SetParent(cardObj.transform, false);
            var lRect = loreObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.05f, 0.33f);
            lRect.anchorMax = new Vector2(0.95f, 0.39f);
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;
            var lTmp = loreObj.AddComponent<TextMeshProUGUI>();
            lTmp.text = "The corridor remains unfinished.";
            lTmp.fontSize = 13f;
            lTmp.fontStyle = FontStyles.Italic;
            lTmp.characterSpacing = 8f;
            lTmp.color = new Color(0.60f, 0.65f, 0.70f, 1f);
            lTmp.alignment = TextAlignmentOptions.Center;
            lTmp.raycastTarget = false;

            // Buttons Container
            var btnGroup = new GameObject("ButtonsGroup");
            btnGroup.transform.SetParent(cardObj.transform, false);
            var bgRect = btnGroup.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.08f, 0.08f);
            bgRect.anchorMax = new Vector2(0.92f, 0.31f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var vlg = btnGroup.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var retryBtn = Create8BitButton(btnGroup.transform, "RetryButton", "[ ↻ RETRY ]", 420, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var exitBtn = Create8BitButton(btnGroup.transform, "ExitButton", "[ ✕ EXIT ]", 420, 58, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);

            var gameOverUI = goPanel.AddComponent<GameOverUI>();
            SetField(gameOverUI, "gameOverPanel", goPanel);
            SetField(gameOverUI, "canvasGroup", cg);
            SetField(gameOverUI, "horrorImage", icImg);
            SetField(gameOverUI, "titleText", tTmp);
            SetField(gameOverUI, "promptText", sTmp);
            SetField(gameOverUI, "subText", lTmp);
            SetField(gameOverUI, "retryButton", retryBtn);
            SetField(gameOverUI, "exitButton", exitBtn);

            var uiMgr = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (uiMgr != null) SetField(uiMgr, "gameOverUI", gameOverUI);

            goPanel.SetActive(false);
        }
        #endregion

        #region 7. Settings Panel Rebuild
        private static void BuildSettingsPanel()
        {
            var canvasObj = GetRootCanvas();
            var panelRetroSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_panel_retro.png");
            var btnNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_normal.png");
            var btnHoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_hover.png");
            var btnPressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_pressed.png");
            var btnDisabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_disabled.png");
            var hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_DIR + "/ui_hover.wav");
            var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_DIR + "/ui_click.wav");

            var oldSet = canvasObj.transform.Find("SettingsPanel");
            if (oldSet != null) UnityEngine.Object.DestroyImmediate(oldSet.gameObject);

            var setPanel = new GameObject("SettingsPanel");
            setPanel.transform.SetParent(canvasObj.transform, false);
            var setRect = setPanel.AddComponent<RectTransform>();
            setRect.anchorMin = new Vector2(0.12f, 0.05f);
            setRect.anchorMax = new Vector2(0.88f, 0.95f);
            setRect.offsetMin = Vector2.zero;
            setRect.offsetMax = Vector2.zero;

            var bgImg = setPanel.AddComponent<Image>();
            bgImg.sprite = panelRetroSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            var cg = setPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(setPanel.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.88f);
            tRect.anchorMax = new Vector2(0.95f, 0.98f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "SETTINGS & ACCESSIBILITY";
            tTmp.fontSize = 28f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.characterSpacing = 18f;
            tTmp.color = new Color(0.95f, 0.92f, 0.78f, 1f);
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.raycastTarget = false;

            // Left Column (Audio & Display)
            var leftCol = new GameObject("LeftColumn");
            leftCol.transform.SetParent(setPanel.transform, false);
            var lcRect = leftCol.AddComponent<RectTransform>();
            lcRect.anchorMin = new Vector2(0.05f, 0.15f);
            lcRect.anchorMax = new Vector2(0.48f, 0.86f);
            lcRect.offsetMin = Vector2.zero;
            lcRect.offsetMax = Vector2.zero;
            var lcLayout = leftCol.AddComponent<VerticalLayoutGroup>();
            lcLayout.spacing = 10f;
            lcLayout.childAlignment = TextAnchor.UpperCenter;
            lcLayout.childControlWidth = true;
            lcLayout.childControlHeight = false;

            Create8BitHeader(leftCol.transform, "AUDIO CONTROLS");
            var masterSlider = Create8BitSlider(leftCol.transform, "MasterSlider", "MASTER VOLUME", 0f, 1f, 1.0f, out var masterVal);
            var musicSlider = Create8BitSlider(leftCol.transform, "MusicSlider", "MUSIC / AMBIENCE", 0f, 1f, 0.8f, out var musicVal);
            var sfxSlider = Create8BitSlider(leftCol.transform, "SFXSlider", "SOUND EFFECTS", 0f, 1f, 1.0f, out var sfxVal);

            Create8BitHeader(leftCol.transform, "DISPLAY & FIELD OF VIEW");
            var brightSlider = Create8BitSlider(leftCol.transform, "BrightnessSlider", "BRIGHTNESS", 0.2f, 2.5f, 1.0f, out var brightVal);
            var fovSlider = Create8BitSlider(leftCol.transform, "FOVSlider", "FIELD OF VIEW", 60f, 105f, 75f, out var fovVal);

            // Right Column (Motion & Accessibility)
            var rightCol = new GameObject("RightColumn");
            rightCol.transform.SetParent(setPanel.transform, false);
            var rcRect = rightCol.AddComponent<RectTransform>();
            rcRect.anchorMin = new Vector2(0.52f, 0.15f);
            rcRect.anchorMax = new Vector2(0.95f, 0.86f);
            rcRect.offsetMin = Vector2.zero;
            rcRect.offsetMax = Vector2.zero;
            var rcLayout = rightCol.AddComponent<VerticalLayoutGroup>();
            rcLayout.spacing = 10f;
            rcLayout.childAlignment = TextAnchor.UpperCenter;
            rcLayout.childControlWidth = true;
            rcLayout.childControlHeight = false;

            Create8BitHeader(rightCol.transform, "GAMEPLAY & MOTION");
            var shakeSlider = Create8BitSlider(rightCol.transform, "ShakeSlider", "CAMERA SHAKE", 0f, 1.5f, 1.0f, out var shakeVal);
            var invertToggle = Create8BitToggle(rightCol.transform, "InvertYToggle", "INVERT Y-AXIS LOOK", false);

            Create8BitHeader(rightCol.transform, "ACCESSIBILITY AID");
            var subToggle = Create8BitToggle(rightCol.transform, "SubtitlesToggle", "SUBTITLES & CAPTIONS", true);
            var photoToggle = Create8BitToggle(rightCol.transform, "PhotoToggle", "SOFTEN JUMP SCARE FLASHES", false);
            var colorblindToggle = Create8BitToggle(rightCol.transform, "ColorblindToggle", "HIGH-CONTRAST ACCESSIBILITY", false);

            // Footer Buttons
            var footObj = new GameObject("FooterButtons");
            footObj.transform.SetParent(setPanel.transform, false);
            var footRect = footObj.AddComponent<RectTransform>();
            footRect.anchorMin = new Vector2(0.05f, 0.03f);
            footRect.anchorMax = new Vector2(0.95f, 0.13f);
            footRect.offsetMin = Vector2.zero;
            footRect.offsetMax = Vector2.zero;
            var footLayout = footObj.AddComponent<HorizontalLayoutGroup>();
            footLayout.spacing = 30f;
            footLayout.childAlignment = TextAnchor.MiddleCenter;
            footLayout.childControlWidth = false;
            footLayout.childControlHeight = true;

            var resetBtn = Create8BitButton(footObj.transform, "ResetDefaultsButton", "[ RESET DEFAULTS ]", 280, 56, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var backBtn = Create8BitButton(footObj.transform, "BackButton", "[ BACK / APPLY ]", 280, 56, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);

            var settingsUI = setPanel.AddComponent<SettingsUI>();
            SetField(settingsUI, "panelRoot", setPanel);
            SetField(settingsUI, "masterSlider", masterSlider);
            SetField(settingsUI, "musicSlider", musicSlider);
            SetField(settingsUI, "sfxSlider", sfxSlider);
            SetField(settingsUI, "masterValText", masterVal);
            SetField(settingsUI, "musicValText", musicVal);
            SetField(settingsUI, "sfxValText", sfxVal);
            SetField(settingsUI, "brightnessSlider", brightSlider);
            SetField(settingsUI, "fovSlider", fovSlider);
            SetField(settingsUI, "brightnessValText", brightVal);
            SetField(settingsUI, "fovValText", fovVal);
            SetField(settingsUI, "shakeSlider", shakeSlider);
            SetField(settingsUI, "shakeValText", shakeVal);
            SetField(settingsUI, "invertYToggle", invertToggle);
            SetField(settingsUI, "subtitlesToggle", subToggle);
            SetField(settingsUI, "photosensitiveToggle", photoToggle);
            SetField(settingsUI, "colorblindToggle", colorblindToggle);
            SetField(settingsUI, "resetDefaultsButton", resetBtn);
            SetField(settingsUI, "backButton", backBtn);

            var uiMgr = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (uiMgr != null) SetField(uiMgr, "settingsUI", settingsUI);

            setPanel.SetActive(false);
        }
        #endregion

        #region 8. Credits Panel Rebuild
        private static void BuildCreditsPanel()
        {
            var canvasObj = GetRootCanvas();
            var panelRetroSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_panel_retro.png");
            var btnNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_normal.png");
            var btnHoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_hover.png");
            var btnPressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_pressed.png");
            var btnDisabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_disabled.png");
            var hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_DIR + "/ui_hover.wav");
            var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_DIR + "/ui_click.wav");

            var oldCred = canvasObj.transform.Find("CreditsPanel");
            if (oldCred != null) UnityEngine.Object.DestroyImmediate(oldCred.gameObject);

            var credPanel = new GameObject("CreditsPanel");
            credPanel.transform.SetParent(canvasObj.transform, false);
            var cpRect = credPanel.AddComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0.2f, 0.15f);
            cpRect.anchorMax = new Vector2(0.8f, 0.85f);
            cpRect.offsetMin = Vector2.zero;
            cpRect.offsetMax = Vector2.zero;

            var bgImg = credPanel.AddComponent<Image>();
            bgImg.sprite = panelRetroSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            var cg = credPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(credPanel.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.85f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "CREDITS & PRODUCTION LOG";
            tTmp.fontSize = 24f;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.characterSpacing = 16f;
            tTmp.color = new Color(0.96f, 0.92f, 0.78f, 1f);
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.raycastTarget = false;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(credPanel.transform, false);
            var cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.1f, 0.22f);
            cRect.anchorMax = new Vector2(0.9f, 0.82f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;
            var cTmp = contentObj.AddComponent<TextMeshProUGUI>();
            cTmp.text = "<b>EXPERIENCE DESIGN & NARRATIVE ARCHITECTURE</b>\nLiminal Psychological Horror Build v2.0\n\n<b>ENVIRONMENT & HOTEL EXPANSION</b>\nMarrow Point 2nd Floor Corridor & Ground Zero Suite 214\nRooms 210, 212, 216 & Stairwell Landing\n\n<b>AUDIO DIRECTION & TACTILE INTERFACE</b>\nProcedural Telephony, 8-Bit Tactile UI & Physiological Dread Engine\n\n<b>ENGINE</b>\nUnity 6 & Universal Render Pipeline (URP)";
            cTmp.fontSize = 16f;
            cTmp.lineSpacing = 18f;
            cTmp.color = new Color(0.85f, 0.85f, 0.82f, 1f);
            cTmp.alignment = TextAlignmentOptions.Center;
            cTmp.raycastTarget = false;

            var closeBtn = Create8BitButton(credPanel.transform, "CloseCreditsButton", "[ CLOSE ]", 260, 56, btnNormalSprite, btnHoverSprite, btnPressedSprite, btnDisabledSprite, hoverClip, clickClip);
            var cbRect = closeBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.35f, 0.05f);
            cbRect.anchorMax = new Vector2(0.65f, 0.16f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;

            var uiMgr = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (uiMgr != null) SetField(uiMgr, "creditsPanel", credPanel);

            credPanel.SetActive(false);
        }
        #endregion

        #region 9. Atmospheric CRT Scanlines Overlay
        private static void BuildCRTScanlineOverlay()
        {
            var canvasObj = GetRootCanvas();
            var scanlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_scanlines.png");

            var oldScan = canvasObj.transform.Find("CRT_Horror_Overlay");
            if (oldScan != null) UnityEngine.Object.DestroyImmediate(oldScan.gameObject);

            var scanObj = new GameObject("CRT_Horror_Overlay");
            scanObj.transform.SetParent(canvasObj.transform, false);
            var sRect = scanObj.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;

            var sImg = scanObj.AddComponent<Image>();
            sImg.sprite = scanlineSprite;
            sImg.type = Image.Type.Tiled;
            sImg.color = new Color(0f, 0f, 0f, 0.045f); // Ultra-restrained 4.5% CRT texture
            sImg.raycastTarget = false;                 // NEVER block raycasts!

            // Position just above HUD and below modals
            scanObj.transform.SetSiblingIndex(2);
        }
        #endregion

        #region 10. 8-Bit Widget Helpers
        private static Button Create8BitButton(Transform parent, string name, string labelText, float w, float h, Sprite normal, Sprite hover, Sprite pressed, Sprite disabled, AudioClip hoverClip, AudioClip clickClip)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(w, h);

            var le = btnObj.AddComponent<LayoutElement>();
            le.minWidth = w * 0.9f;
            le.preferredWidth = w;
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleWidth = 1f;
            le.flexibleHeight = 0f;

            var img = btnObj.AddComponent<Image>();
            img.sprite = normal;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = true; // Full button hitbox!

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Configure SpriteSwap for authentic crisp 8-bit response
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = hover;
            ss.pressedSprite = pressed;
            ss.selectedSprite = hover;
            ss.disabledSprite = disabled;
            btn.spriteState = ss;

            // Text
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 20f;
            tmp.color = new Color(0.94f, 0.94f, 0.92f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // NEVER block button clicks!

            // Attach RetroButtonEffect for carets & audio
            var effect = btnObj.AddComponent<RetroButtonEffect>();
            effect.SetOriginalText(labelText);
            effect.SetAudioClips(hoverClip, clickClip);

            return btn;
        }

        private static void Create8BitHeader(Transform parent, string title)
        {
            var obj = new GameObject("Header_" + title);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480, 30);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = 13f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 16f;
            tmp.color = new Color(0.92f, 0.78f, 0.35f, 1f); // Retro amber
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            tmp.raycastTarget = false;
        }

        private static Slider Create8BitSlider(Transform parent, string name, string labelText, float min, float max, float defVal, out TextMeshProUGUI outValText)
        {
            var btnNormal = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_normal.png");

            var container = new GameObject(name);
            container.transform.SetParent(parent, false);
            var cr = container.AddComponent<RectTransform>();
            cr.sizeDelta = new Vector2(480, 42);

            var le = container.AddComponent<LayoutElement>();
            le.minHeight = 42;
            le.preferredHeight = 42;
            le.flexibleWidth = 1f;

            var lblObj = new GameObject("Label");
            lblObj.transform.SetParent(container.transform, false);
            var lblRect = lblObj.AddComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(0f, 0f);
            lblRect.anchorMax = new Vector2(0.42f, 1f);
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;
            var lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
            lblTmp.text = labelText;
            lblTmp.fontSize = 14f;
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.characterSpacing = 10f;
            lblTmp.color = new Color(0.85f, 0.85f, 0.82f);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lblTmp.raycastTarget = false;

            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(container.transform, false);
            var sRect = sliderObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.44f, 0.20f);
            sRect.anchorMax = new Vector2(0.82f, 0.80f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;

            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = btnNormal;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.14f, 0.16f, 0.20f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var faRect = fillArea.AddComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.offsetMin = new Vector2(2, 2);
            faRect.offsetMax = new Vector2(-2, -2);

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.92f, 0.78f, 0.35f);

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            var haRect = handleArea.AddComponent<RectTransform>();
            haRect.anchorMin = Vector2.zero;
            haRect.anchorMax = Vector2.one;
            haRect.offsetMin = Vector2.zero;
            haRect.offsetMax = Vector2.zero;

            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleArea.transform, false);
            var handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18, 28);
            var handleImg = handleObj.AddComponent<Image>();
            handleImg.color = new Color(0.95f, 0.95f, 0.95f);

            var slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defVal;

            var valObj = new GameObject("ValueText");
            valObj.transform.SetParent(container.transform, false);
            var valRect = valObj.AddComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.84f, 0f);
            valRect.anchorMax = new Vector2(1.0f, 1f);
            valRect.offsetMin = Vector2.zero;
            valRect.offsetMax = Vector2.zero;
            var valTmp = valObj.AddComponent<TextMeshProUGUI>();
            valTmp.fontSize = 14f;
            valTmp.color = new Color(0.90f, 0.85f, 0.65f);
            valTmp.alignment = TextAlignmentOptions.MidlineRight;
            valTmp.text = $"{defVal:F1}";
            valTmp.raycastTarget = false;
            outValText = valTmp;

            return slider;
        }

        private static Toggle Create8BitToggle(Transform parent, string name, string labelText, bool defVal)
        {
            var btnNormal = AssetDatabase.LoadAssetAtPath<Sprite>(ART_UI_DIR + "/spr_ui_button_normal.png");

            var container = new GameObject(name);
            container.transform.SetParent(parent, false);
            var cr = container.AddComponent<RectTransform>();
            cr.sizeDelta = new Vector2(480, 42);

            var le = container.AddComponent<LayoutElement>();
            le.minHeight = 42;
            le.preferredHeight = 42;
            le.flexibleWidth = 1f;

            var lblObj = new GameObject("Label");
            lblObj.transform.SetParent(container.transform, false);
            var lblRect = lblObj.AddComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(0f, 0f);
            lblRect.anchorMax = new Vector2(0.85f, 1f);
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;
            var lblTmp = lblObj.AddComponent<TextMeshProUGUI>();
            lblTmp.text = labelText;
            lblTmp.fontSize = 14f;
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.characterSpacing = 10f;
            lblTmp.color = new Color(0.85f, 0.85f, 0.82f);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lblTmp.raycastTarget = false;

            var boxObj = new GameObject("Background");
            boxObj.transform.SetParent(container.transform, false);
            var boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.90f, 0.15f);
            boxRect.anchorMax = new Vector2(0.98f, 0.85f);
            boxRect.offsetMin = Vector2.zero;
            boxRect.offsetMax = Vector2.zero;
            var boxImg = boxObj.AddComponent<Image>();
            boxImg.sprite = btnNormal;
            boxImg.type = Image.Type.Sliced;
            boxImg.color = Color.white;

            var chkObj = new GameObject("Checkmark");
            chkObj.transform.SetParent(boxObj.transform, false);
            var chkRect = chkObj.AddComponent<RectTransform>();
            chkRect.anchorMin = new Vector2(0.2f, 0.2f);
            chkRect.anchorMax = new Vector2(0.8f, 0.8f);
            chkRect.offsetMin = Vector2.zero;
            chkRect.offsetMax = Vector2.zero;
            var chkImg = chkObj.AddComponent<Image>();
            chkImg.color = new Color(0.92f, 0.78f, 0.35f);

            var toggle = container.AddComponent<Toggle>();
            toggle.targetGraphic = boxImg;
            toggle.graphic = chkImg;
            toggle.isOn = defVal;

            return toggle;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }
        #endregion
    }
}
