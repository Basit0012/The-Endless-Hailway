using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EndlessHallway.Core;

namespace EndlessHallway.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        private static PauseMenuUI instance;
        public static PauseMenuUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<PauseMenuUI>();
                    if (instance == null)
                    {
                        var canvas = FindAnyObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            instance = canvas.gameObject.AddComponent<PauseMenuUI>();
                        }
                    }
                }
                return instance;
            }
        }

        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private SettingsUI settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartLoopButton;
        [SerializeField] private Button quitMainMenuButton;
        [SerializeField] private Button quitGameButton;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI loopInfoText;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            EnsureUI();
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void EnsureUI()
        {
            if (pausePanel != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("PauseMenuPanel");
            if (existing != null)
            {
                pausePanel = existing.gameObject;
                var btns = pausePanel.GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    string n = b.name.ToLower();
                    if (n.Contains("resume")) resumeButton = b;
                    else if (n.Contains("restart")) restartLoopButton = b;
                    else if (n.Contains("setting") || n.Contains("option")) settingsButton = b;
                    else if (n.Contains("mainmenu")) quitMainMenuButton = b;
                    else if (n.Contains("quit")) quitGameButton = b;
                }
                var tmps = pausePanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    if (t.name.ToLower().Contains("loop")) loopInfoText = t;
                }
            }
            else
            {
                var pObj = new GameObject("PauseMenuPanel");
                pObj.transform.SetParent(canvas.transform, false);
                var pRect = pObj.AddComponent<RectTransform>();
                pRect.anchorMin = Vector2.zero;
                pRect.anchorMax = Vector2.one;
                pRect.offsetMin = Vector2.zero;
                pRect.offsetMax = Vector2.zero;
                var img = pObj.AddComponent<Image>();
                img.color = new Color(0.04f, 0.05f, 0.07f, 0.90f);

                var tObj = new GameObject("Title");
                tObj.transform.SetParent(pObj.transform, false);
                var tRect = tObj.AddComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0.1f, 0.72f);
                tRect.anchorMax = new Vector2(0.9f, 0.84f);
                tRect.offsetMin = Vector2.zero;
                tRect.offsetMax = Vector2.zero;
                var titleTmp = tObj.AddComponent<TextMeshProUGUI>();
                titleTmp.text = "PAUSED // MARROW POINT RESIDENCES";
                titleTmp.fontSize = 32f;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.color = new Color(0.96f, 0.92f, 0.78f, 1f);
                titleTmp.alignment = TextAlignmentOptions.Center;

                var liObj = new GameObject("LoopInfo");
                liObj.transform.SetParent(pObj.transform, false);
                var liRect = liObj.AddComponent<RectTransform>();
                liRect.anchorMin = new Vector2(0.1f, 0.65f);
                liRect.anchorMax = new Vector2(0.9f, 0.72f);
                liRect.offsetMin = Vector2.zero;
                liRect.offsetMax = Vector2.zero;
                loopInfoText = liObj.AddComponent<TextMeshProUGUI>();
                loopInfoText.text = "CORRIDOR // LOOP 0";
                loopInfoText.fontSize = 17f;
                loopInfoText.color = new Color(0.55f, 0.68f, 0.70f, 1f);
                loopInfoText.alignment = TextAlignmentOptions.Center;

                var bGrp = new GameObject("ButtonsGroup");
                bGrp.transform.SetParent(pObj.transform, false);
                var bRect = bGrp.AddComponent<RectTransform>();
                bRect.anchorMin = new Vector2(0.35f, 0.12f);
                bRect.anchorMax = new Vector2(0.65f, 0.64f);
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;
                var layout = bGrp.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 14f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                resumeButton = CreateSimpleButton(bGrp.transform, "ResumeButton", "RESUME");
                settingsButton = CreateSimpleButton(bGrp.transform, "SettingsButton", "SETTINGS & ACCESSIBILITY");
                restartLoopButton = CreateSimpleButton(bGrp.transform, "RestartLoopButton", "RESTART CURRENT LOOP");
                quitMainMenuButton = CreateSimpleButton(bGrp.transform, "QuitMainMenuButton", "QUIT TO MAIN MENU");
                quitGameButton = CreateSimpleButton(bGrp.transform, "QuitGameButton", "QUIT TO DESKTOP");

                pausePanel = pObj;
            }

            if (settingsPanel == null)
            {
                settingsPanel = FindAnyObjectByType<SettingsUI>();
            }

            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (restartLoopButton != null) restartLoopButton.onClick.AddListener(OnRestartLoopClicked);
            if (quitMainMenuButton != null) quitMainMenuButton.onClick.AddListener(OnQuitMainMenuClicked);
            if (quitGameButton != null) quitGameButton.onClick.AddListener(OnQuitClicked);
        }

        private Button CreateSimpleButton(Transform parent, string name, string text)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320, 48);

            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.14f, 0.18f, 0.22f, 1f);

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.14f, 0.18f, 0.22f, 1f);
            colors.highlightedColor = new Color(0.28f, 0.35f, 0.40f, 1f);
            colors.pressedColor = new Color(0.08f, 0.10f, 0.12f, 1f);
            btn.colors = colors;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.92f, 0.90f, 0.84f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private void Start()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.OnPauseToggled += HandlePauseToggled;
            }
        }

        private void OnDestroy()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.OnPauseToggled -= HandlePauseToggled;
            }
        }

        private void HandlePauseToggled(bool isPaused)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(isPaused);
            }

            if (isPaused)
            {
                if (settingsPanel != null && settingsPanel.gameObject.activeSelf)
                {
                    settingsPanel.Close();
                }

                if (loopInfoText != null && LoopManager.Instance != null)
                {
                    loopInfoText.text = $"CORRIDOR // LOOP {LoopManager.Instance.CurrentLoop}";
                }

                if (resumeButton != null)
                {
                    resumeButton.Select();
                }
            }
            else
            {
                if (settingsPanel != null)
                {
                    settingsPanel.Close();
                }
            }
        }

        public void OnResumeClicked()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.SetPaused(false);
            }
        }

        public void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                if (pausePanel != null) pausePanel.SetActive(false);
                settingsPanel.Open(OnSettingsClosed);
            }
        }

        private void OnSettingsClosed()
        {
            if (pausePanel != null && PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            {
                pausePanel.SetActive(true);
            }
        }

        public void OnRestartLoopClicked()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.RestartCurrentLoop();
            }
        }

        public void OnQuitMainMenuClicked()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.SetPaused(false);
            }

            if (MainMenuUI.Instance != null)
            {
                MainMenuUI.Instance.ShowMenu();
            }
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
