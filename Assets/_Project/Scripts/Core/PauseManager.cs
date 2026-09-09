using System;
using UnityEngine;
using EndlessHallway.Player;
using EndlessHallway.Audio;
using EndlessHallway.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessHallway.Core
{
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        [SerializeField] private bool isPaused = false;
        public bool IsPaused => isPaused;

        public event Action<bool> OnPauseToggled;

        private PlayerController cachedPlayerController;
        private PlayerCameraLook cachedPlayerCameraLook;
        private PlayerInteraction cachedPlayerInteraction;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (FindAnyObjectByType<SettingsManager>() == null)
            {
                gameObject.AddComponent<SettingsManager>();
            }
            if (FindAnyObjectByType<SaveManager>() == null)
            {
                gameObject.AddComponent<SaveManager>();
            }
        }

        private void Start()
        {
            FindPlayerReferences();

            // Ensure an EventSystem exists in the scene so UI buttons can receive click events
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            if (PauseMenuUI.Instance == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null && canvas.GetComponent<PauseMenuUI>() == null)
                {
                    canvas.gameObject.AddComponent<PauseMenuUI>();
                }
            }
        }

        public void FindPlayerReferences()
        {
            if (cachedPlayerController == null)
            {
                cachedPlayerController = FindAnyObjectByType<PlayerController>();
            }
            if (cachedPlayerCameraLook == null)
            {
                cachedPlayerCameraLook = FindAnyObjectByType<PlayerCameraLook>();
            }
            if (cachedPlayerInteraction == null)
            {
                cachedPlayerInteraction = FindAnyObjectByType<PlayerInteraction>();
            }
        }

        private void Update()
        {
            // Do not intercept if player is currently examining a document; ExamineUI consumes ESC
            if (ExamineUI.Instance != null && ExamineUI.Instance.IsOpen)
            {
                return;
            }

            // Do not pause during ending cutscene
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Ending)
            {
                return;
            }

            bool escPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escPressed = true;
            }
            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            {
                escPressed = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!escPressed && Input.GetKeyDown(KeyCode.Escape))
            {
                escPressed = true;
            }
#endif

            if (escPressed)
            {
                if (UIManager.Instance != null && UIManager.Instance.IsAnyModalOpen)
                {
                    UIManager.Instance.HandleEscapeInput();
                }
                else
                {
                    TogglePause();
                }
            }
        }

        public void TogglePause()
        {
            SetPaused(!isPaused);
        }

        public void SetPaused(bool paused)
        {
            if (isPaused == paused) return;
            isPaused = paused;

            Time.timeScale = isPaused ? 0f : 1f;

            if (cachedPlayerController == null || cachedPlayerCameraLook == null)
            {
                FindPlayerReferences();
            }

            if (cachedPlayerController != null)
            {
                cachedPlayerController.CanMove = !isPaused;
            }

            if (cachedPlayerCameraLook != null)
            {
                cachedPlayerCameraLook.CanLook = !isPaused;
                if (isPaused)
                {
                    cachedPlayerCameraLook.UnlockCursor();
                }
                else
                {
                    cachedPlayerCameraLook.LockCursor();
                }
            }
            else
            {
                Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = isPaused;
            }

            if (cachedPlayerInteraction != null)
            {
                cachedPlayerInteraction.CanInteract = !isPaused;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.DuckAudio(isPaused);
            }

            OnPauseToggled?.Invoke(isPaused);
        }

        public void RestartCurrentLoop()
        {
            SetPaused(false);

            if (LoopManager.Instance != null)
            {
                int current = LoopManager.Instance.CurrentLoop;
                LoopManager.Instance.ResetLoop(current);

                var elevator = FindAnyObjectByType<Interaction.ElevatorController>();
                var spawnField = typeof(Interaction.ElevatorController).GetField("playerSpawnPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Transform spawnPt = null;
                if (spawnField != null && elevator != null)
                {
                    spawnPt = spawnField.GetValue(elevator) as Transform;
                }

                if (spawnPt != null && cachedPlayerController != null)
                {
                    cachedPlayerController.Teleport(spawnPt.position, spawnPt.rotation);
                }
                else if (cachedPlayerController != null)
                {
                    cachedPlayerController.Teleport(new Vector3(0f, 1.0f, -1.4f), Quaternion.identity);
                }
            }
        }
    }
}
