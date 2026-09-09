using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EndlessHallway.Core;

namespace EndlessHallway.UI
{
    public class GameOverUI : MonoBehaviour
    {
        private static GameOverUI instance;
        public static GameOverUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GameOverUI>(FindObjectsInactive.Include);
                }
                return instance;
            }
            private set { instance = value; }
        }

        [Header("Root Panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visuals")]
        [SerializeField] private Image horrorImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI subText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button exitButton;

        [Header("Audio")]
        [SerializeField] private AudioClip gameOverAmbience;

        private Action onRetryAction;
        private Action onExitAction;
        private Coroutine fadeCoroutine;

        public bool IsOpen => gameOverPanel != null && gameOverPanel.activeSelf && (canvasGroup == null || canvasGroup.alpha > 0.05f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (canvasGroup == null && gameOverPanel != null)
            {
                canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
            }

            HookButtons();
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        private void HookButtons()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        }

        public void ShowGameOver(Action onRetry = null, Action onExit = null)
        {
            onRetryAction = onRetry;
            onExitAction = onExit;

            if (gameOverPanel == null) return;
            gameOverPanel.SetActive(true);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.GameOver);
            }
            else
            {
                // Freeze player
                var pCtrl = FindAnyObjectByType<Player.PlayerController>();
                var pLook = FindAnyObjectByType<Player.PlayerCameraLook>();
                if (pCtrl != null) pCtrl.CanMove = false;
                if (pLook != null) pLook.CanLook = false;

                // Cursor setup
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeInRoutine(1.2f));

            if (retryButton != null)
            {
                retryButton.Select();
            }

            if (gameOverAmbience != null && Audio.OneShotPool.Instance != null)
            {
                Audio.OneShotPool.Instance.Play2D(gameOverAmbience, 0.6f, 0.9f);
            }
        }

        public void HideGameOver()
        {
            if (UIManager.Instance != null && UIManager.Instance.CurrentScreen == UIScreen.GameOver)
            {
                UIManager.Instance.HideGameOver();
                return;
            }

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private IEnumerator FadeInRoutine(float duration)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = true;

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                    yield return null;
                }

                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
            }
        }

        public void OnRetryClicked()
        {
            HideGameOver();

            if (onRetryAction != null)
            {
                onRetryAction.Invoke();
                return;
            }

            // Default retry behavior: Resume from checkpoint or restart current loop
            int targetLoop = 0;
            if (SaveManager.Instance != null && SaveManager.Instance.HasCheckpoint())
            {
                targetLoop = SaveManager.Instance.GetSavedLoop();
            }
            else if (LoopManager.Instance != null)
            {
                targetLoop = LoopManager.Instance.CurrentLoop;
            }

            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.ResetLoop(targetLoop);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Exploring);
            }
            else
            {
                // Restore controls
                var pCtrl = FindAnyObjectByType<Player.PlayerController>();
                var pLook = FindAnyObjectByType<Player.PlayerCameraLook>();
                if (pCtrl != null) pCtrl.CanMove = true;
                if (pLook != null) pLook.CanLook = true;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void OnExitClicked()
        {
            HideGameOver();

            if (onExitAction != null)
            {
                onExitAction.Invoke();
                return;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMainMenu();
            }
            else if (MainMenuUI.Instance != null)
            {
                MainMenuUI.Instance.ShowMenu();
            }
        }
    }
}
