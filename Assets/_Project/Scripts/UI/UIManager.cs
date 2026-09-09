using System;
using System.Collections.Generic;
using UnityEngine;
using EndlessHallway.Core;

namespace EndlessHallway.UI
{
    public enum UIScreen
    {
        None,
        MainMenu,
        PauseMenu,
        GameOver,
        Settings,
        Credits
    }

    /// <summary>
    /// Centralized UI orchestrator that manages panel states, layering, ESC routing,
    /// and cursor control across all menus and HUD elements.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set { instance = value; }
        }

        [Header("Screens")]
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private PauseMenuUI pauseMenuUI;
        [SerializeField] private GameOverUI gameOverUI;
        [SerializeField] private SettingsUI settingsUI;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject hudLayer;

        [Header("State")]
        [SerializeField] private UIScreen currentScreen = UIScreen.None;
        public UIScreen CurrentScreen => currentScreen;
        public bool IsAnyModalOpen => currentScreen != UIScreen.None;

        private UIScreen previousScreen = UIScreen.None;
        private Action settingsCloseCallback;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            FindReferencesIfNull();
        }

        private void Start()
        {
            FindReferencesIfNull();
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.OnPauseToggled += HandlePauseToggled;
            }
        }

        private void FindReferencesIfNull()
        {
            if (mainMenuUI == null) mainMenuUI = FindAnyObjectByType<MainMenuUI>(FindObjectsInactive.Include);
            if (pauseMenuUI == null) pauseMenuUI = FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
            if (gameOverUI == null) gameOverUI = FindAnyObjectByType<GameOverUI>(FindObjectsInactive.Include);
            if (settingsUI == null) settingsUI = FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);
            if (creditsPanel == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    var cp = canvas.transform.Find("CreditsPanel");
                    if (cp != null) creditsPanel = cp.gameObject;
                }
            }
        }

        public void HandleEscapeInput()
        {
            // 1. If Settings is open, close it and return to caller
            if (currentScreen == UIScreen.Settings)
            {
                CloseSettings();
                return;
            }

            // 2. If Credits is open, close it and return to caller
            if (currentScreen == UIScreen.Credits)
            {
                CloseCredits();
                return;
            }

            // 3. If GameOver or MainMenu is open, let them handle or do nothing
            if (currentScreen == UIScreen.GameOver || currentScreen == UIScreen.MainMenu)
            {
                return;
            }

            // 4. In gameplay or paused: Toggle pause
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.TogglePause();
            }
            else
            {
                TogglePause();
            }
        }

        public void ShowMainMenu()
        {
            currentScreen = UIScreen.MainMenu;
            previousScreen = UIScreen.None;

            if (mainMenuUI != null) mainMenuUI.ShowMenu();
            if (pauseMenuUI != null) pauseMenuUI.gameObject.SetActive(false);
            if (gameOverUI != null) gameOverUI.HideGameOver();
            if (settingsUI != null) settingsUI.Close();
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (hudLayer != null) hudLayer.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }
            else
            {
                SetCursorState(true);
                SetPlayerControls(false);
            }
            Time.timeScale = 1f;
        }

        public void HideMainMenu()
        {
            if (currentScreen == UIScreen.MainMenu)
            {
                currentScreen = UIScreen.None;
            }

            if (mainMenuUI != null) mainMenuUI.HideMenu();
            if (hudLayer != null) hudLayer.SetActive(true);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Exploring);
            }
            else
            {
                SetCursorState(false);
                SetPlayerControls(true);
            }
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.OnPauseToggled -= HandlePauseToggled;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;

            if (currentScreen != UIScreen.None)
            {
                SetCursorState(true);
                SetPlayerControls(false);
            }
            else if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Exploring)
            {
                SetCursorState(false);
                SetPlayerControls(true);
            }
        }

        private void HandlePauseToggled(bool isPaused)
        {
            if (isPaused && currentScreen == UIScreen.None)
            {
                currentScreen = UIScreen.PauseMenu;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetState(GameState.Paused);
                }
            }
            else if (!isPaused && currentScreen == UIScreen.PauseMenu)
            {
                currentScreen = UIScreen.None;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetState(GameState.Exploring);
                }
            }
        }

        public void TogglePause()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.TogglePause();
            }
            else
            {
                if (currentScreen == UIScreen.PauseMenu) ResumeFromPause();
                else if (currentScreen == UIScreen.None) ShowPauseMenu();
            }
        }

        public void ShowPauseMenu()
        {
            if (currentScreen == UIScreen.MainMenu || currentScreen == UIScreen.GameOver) return;

            currentScreen = UIScreen.PauseMenu;
            if (PauseManager.Instance != null && !PauseManager.Instance.IsPaused)
            {
                PauseManager.Instance.SetPaused(true);
            }

            if (pauseMenuUI != null)
            {
                pauseMenuUI.gameObject.SetActive(true);
            }
            if (settingsUI != null) settingsUI.Close();
            if (creditsPanel != null) creditsPanel.SetActive(false);

            Time.timeScale = 0f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Paused);
            }
            else
            {
                SetCursorState(true);
                SetPlayerControls(false);
            }
        }

        public void ResumeFromPause()
        {
            if (currentScreen == UIScreen.PauseMenu)
            {
                currentScreen = UIScreen.None;
            }

            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            {
                PauseManager.Instance.SetPaused(false);
            }

            if (pauseMenuUI != null)
            {
                pauseMenuUI.gameObject.SetActive(false);
            }
            if (settingsUI != null) settingsUI.Close();

            Time.timeScale = 1f;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Exploring);
            }
            else
            {
                SetCursorState(false);
                SetPlayerControls(true);
            }
        }

        public void ShowGameOver(Action onRetry = null, Action onExit = null)
        {
            currentScreen = UIScreen.GameOver;
            if (pauseMenuUI != null) pauseMenuUI.gameObject.SetActive(false);
            if (settingsUI != null) settingsUI.Close();
            if (creditsPanel != null) creditsPanel.SetActive(false);

            if (gameOverUI != null)
            {
                gameOverUI.ShowGameOver(onRetry, onExit);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.GameOver);
            }
            else
            {
                SetCursorState(true);
                SetPlayerControls(false);
            }
        }

        public void HideGameOver()
        {
            if (currentScreen == UIScreen.GameOver)
            {
                currentScreen = UIScreen.None;
            }

            if (gameOverUI != null)
            {
                gameOverUI.HideGameOver();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Exploring);
            }
            else
            {
                SetCursorState(false);
                SetPlayerControls(true);
            }
        }

        public void OpenSettings(Action onClosed = null)
        {
            previousScreen = currentScreen;
            currentScreen = UIScreen.Settings;
            settingsCloseCallback = onClosed;

            if (settingsUI != null)
            {
                settingsUI.Open(OnSettingsClosedInternal);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Settings);
            }
            else
            {
                SetCursorState(true);
            }
        }

        private void OnSettingsClosedInternal()
        {
            currentScreen = previousScreen;
            settingsCloseCallback?.Invoke();
            settingsCloseCallback = null;

            if (GameManager.Instance != null)
            {
                if (currentScreen == UIScreen.MainMenu) GameManager.Instance.SetState(GameState.MainMenu);
                else if (currentScreen == UIScreen.PauseMenu) GameManager.Instance.SetState(GameState.Paused);
                else GameManager.Instance.SetState(GameState.Exploring);
            }
            else
            {
                SetCursorState(currentScreen != UIScreen.None);
            }
        }

        public void CloseSettings()
        {
            if (settingsUI != null)
            {
                settingsUI.Close();
            }
        }

        public void OpenCredits()
        {
            previousScreen = currentScreen;
            currentScreen = UIScreen.Credits;
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(true);
            }
        }

        public void CloseCredits()
        {
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }
            currentScreen = previousScreen;
        }

        public void SetCursorState(bool visible)
        {
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }

        public void SetPlayerControls(bool enabled)
        {
            var pCtrl = FindAnyObjectByType<Player.PlayerController>();
            var pLook = FindAnyObjectByType<Player.PlayerCameraLook>();
            if (pCtrl != null) pCtrl.CanMove = enabled;
            if (pLook != null) pLook.CanLook = enabled;
        }
    }
}
