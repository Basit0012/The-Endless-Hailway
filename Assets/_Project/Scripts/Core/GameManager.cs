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

        private void HandleLoopChanged(int loopIndex)
        {
            Debug.Log($"[GameManager] Loop advanced to {loopIndex}");
        }

        public void TriggerEnding(bool accepted)
        {
            SetState(GameState.Ending);
            Debug.Log($"[GameManager] Ending triggered. Acceptance: {accepted}");
        }
    }
}
