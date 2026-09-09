using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EndlessHallway.Core;

namespace EndlessHallway.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private static MainMenuUI instance;
        public static MainMenuUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set { instance = value; }
        }

        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private SettingsUI settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button closeCreditsButton;
        [SerializeField] private Button quitButton;

        [Header("Audio")]
        [SerializeField] private AudioClip menuMusicClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            HookButtons();
        }

        private void Start()
        {
            RefreshContinueButton();
            if (menuPanel != null && menuPanel.activeSelf)
            {
                ShowMenu();
            }
        }

        private void HookButtons()
        {
            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsClicked);
            if (closeCreditsButton != null) closeCreditsButton.onClick.AddListener(OnCloseCreditsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        public void ShowMenu()
        {
            if (UIManager.Instance != null && UIManager.Instance.CurrentScreen != UIScreen.MainMenu)
            {
                UIManager.Instance.ShowMainMenu();
                return;
            }

            if (menuPanel != null) menuPanel.SetActive(true);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.Close();

            RefreshContinueButton();

            if (continueButton != null && continueButton.interactable)
            {
                continueButton.Select();
            }
            else if (newGameButton != null)
            {
                newGameButton.Select();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                var pCtrl = FindAnyObjectByType<Player.PlayerController>();
                var pLook = FindAnyObjectByType<Player.PlayerCameraLook>();
                if (pCtrl != null) pCtrl.CanMove = false;
                if (pLook != null) pLook.CanLook = false;
            }
        }

        public void HideMenu()
        {
            if (UIManager.Instance != null && UIManager.Instance.CurrentScreen == UIScreen.MainMenu)
            {
                UIManager.Instance.HideMainMenu();
                return;
            }

            if (menuPanel != null) menuPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.Close();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Exploring);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                var pCtrl = FindAnyObjectByType<Player.PlayerController>();
                var pLook = FindAnyObjectByType<Player.PlayerCameraLook>();
                if (pCtrl != null) pCtrl.CanMove = true;
                if (pLook != null) pLook.CanLook = true;
            }
        }

        public void RefreshContinueButton()
        {
            if (continueButton != null)
            {
                bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasCheckpoint();
                continueButton.interactable = hasSave;

                string label = hasSave && SaveManager.Instance != null
                    ? $"[ CONTINUE (LOOP {SaveManager.Instance.GetSavedLoop()}) ]"
                    : "[ CONTINUE ]";

                var retroEffect = continueButton.GetComponent<RetroButtonEffect>();
                if (retroEffect != null)
                {
                    retroEffect.SetOriginalText(label);
                }
                else
                {
                    var txt = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = label;
                }
            }
        }

        public void OnContinueClicked()
        {
            int loop = SaveManager.Instance != null ? SaveManager.Instance.GetSavedLoop() : 0;
            HideMenu();

            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.ResetLoop(loop);
            }
        }

        public void OnNewGameClicked()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.ClearCheckpoint();
            }

            HideMenu();

            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.ResetLoop(0);
            }
        }

        public void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                if (menuPanel != null) menuPanel.SetActive(false);
                settingsPanel.Open(OnSettingsClosed);
            }
        }

        private void OnSettingsClosed()
        {
            if (menuPanel != null) menuPanel.SetActive(true);
        }

        public void OnCreditsClicked()
        {
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        public void OnCloseCreditsClicked()
        {
            if (creditsPanel != null) creditsPanel.SetActive(false);
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
