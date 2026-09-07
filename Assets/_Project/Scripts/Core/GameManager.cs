using System;
using UnityEngine;

namespace EndlessHallway.Core
{
    public enum GameState
    {
        Exploring,
        InElevator,
        Examining,
        Ending
    }

    /// <summary>
    /// Master coordinator for game state, loop progression, and endings.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Exploring;
        public GameState CurrentState => currentState;

        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetState(GameState.Exploring);

            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.OnLoopChanged += HandleLoopChanged;
            }
        }

        private void OnDestroy()
        {
            if (LoopManager.Instance != null)
            {
                LoopManager.Instance.OnLoopChanged -= HandleLoopChanged;
            }
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnStateChanged?.Invoke(currentState);

            switch (currentState)
            {
                case GameState.Exploring:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                case GameState.InElevator:
                    // Controls can be partially limited
                    break;
                case GameState.Examining:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                case GameState.Ending:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }
        }

        [Header("Ending Volume Profiles")]
        [SerializeField] private UnityEngine.Rendering.VolumeProfile acceptanceProfile;
        [SerializeField] private UnityEngine.Rendering.VolumeProfile erosionProfile;

        private void HandleLoopChanged(int loopIndex)
        {
            Debug.Log($"[GameManager] Loop advanced to {loopIndex}");
        }

        public void TriggerEnding(bool accepted)
        {
            if (currentState == GameState.Ending) return;
            SetState(GameState.Ending);
            Debug.Log($"[GameManager] Ending triggered. Acceptance: {accepted}");
            StartCoroutine(EndingSequenceRoutine(accepted));
        }

        private System.Collections.IEnumerator EndingSequenceRoutine(bool accepted)
        {
            // 1. Freeze player controls
            var player = FindAnyObjectByType<Player.PlayerController>();
            if (player != null) player.CanMove = false;
            var look = FindAnyObjectByType<Player.PlayerCameraLook>();
            if (look != null) look.CanLook = false;

            // 2. Ending presentation
            if (accepted)
            {
                // Stop audio ambience and telephone
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.SetOverlayAmbience(null, 0f);
                }

                var phoneObj = GameObject.Find("Room214_Telephone");
                if (phoneObj != null)
                {
                    var aSrc = phoneObj.GetComponent<AudioSource>();
                    if (aSrc != null) aSrc.Stop();
                }

                // Hide Observer
                if (Entity.ObserverController.Instance != null)
                {
                    Entity.ObserverController.Instance.SetVisible(false);
                }

                // Switch Volume to warm golden daylight
                if (acceptanceProfile != null && Anomaly.AnomalyManager.Instance != null)
                {
                    Anomaly.AnomalyManager.Instance.SetSceneVolumeProfile(acceptanceProfile);
                }

                // Softly close Door 214 behind player
                var door214 = GameObject.Find("Door_214");
                if (door214 != null)
                {
                    var dc = door214.GetComponent<Interaction.DoorController>();
                    if (dc != null && dc.IsOpen) dc.SetOpen(false);
                }

                // Acceptance ending: Fade to pure white
                if (UI.FadeController.Instance != null)
                {
                    UI.FadeController.Instance.SetColor(Color.white);
                    yield return StartCoroutine(UI.FadeController.Instance.FadeOutRoutine(4.0f));
                }
                else
                {
                    yield return new WaitForSeconds(4.0f);
                }

                yield return new WaitForSeconds(2.0f);
                Debug.Log("[GameManager] ACCEPTANCE ENDING REACHED. Aiden faced the truth of October 14th. The loop is broken.");
            }
            else
            {
                // Erosion ending: Fade to pure black
                if (UI.FadeController.Instance != null)
                {
                    UI.FadeController.Instance.SetColor(Color.black);
                    yield return StartCoroutine(UI.FadeController.Instance.FadeOutRoutine(3.0f));
                }
                else
                {
                    yield return new WaitForSeconds(3.0f);
                }

                // Silence audio
                if (Audio.AudioManager.Instance != null)
                {
                    Audio.AudioManager.Instance.SetOverlayAmbience(null, 0f);
                }

                yield return new WaitForSeconds(2.0f);
                Debug.Log("[GameManager] Erosion — Some choose not to remember.");

                // Silently reset back to Loop 0
                if (LoopManager.Instance != null)
                {
                    LoopManager.Instance.ResetLoop(0);
                }

                // Apply washed-out erosion profile
                if (erosionProfile != null && Anomaly.AnomalyManager.Instance != null)
                {
                    Anomaly.AnomalyManager.Instance.SetSceneVolumeProfile(erosionProfile);
                }

                // Permanently remove the fire drill notice from the noticeboard
                var board = GameObject.Find("CorkNoticeboard");
                if (board != null)
                {
                    var noticePaper = board.transform.Find("NoticePaper");
                    if (noticePaper != null) noticePaper.gameObject.SetActive(false);
                    var examinable = board.GetComponent<Interaction.Examinable>();
                    if (examinable != null) examinable.enabled = false;
                }

                if (UI.FadeController.Instance != null)
                {
                    yield return StartCoroutine(UI.FadeController.Instance.FadeInRoutine(2.5f));
                }

                if (player != null) player.CanMove = true;
                if (look != null) look.CanLook = true;
                SetState(GameState.Exploring);
            }
        }
    }
}
