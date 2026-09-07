using System;
using UnityEngine;
using EndlessHallway.Anomaly;

namespace EndlessHallway.Core
{
    /// <summary>
    /// Coordinates loop progression and notifies systems to reset or apply anomalies.
    /// </summary>
    public class LoopManager : MonoBehaviour
    {
        public static LoopManager Instance { get; private set; }

        [Header("Loop Tracking")]
        [SerializeField] private int currentLoop = 0;
        public int CurrentLoop => currentLoop;

        public event Action<int> OnLoopChanged;
        public event Action OnLoopReset;

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
            // Initialize Loop 0 at game start
            ApplyCurrentLoopState();
        }

        /// <summary>
        /// Increments the loop and refreshes the world. Called by ElevatorController upon elevator ride completion.
        /// </summary>
        public void AdvanceLoop()
        {
            currentLoop++;
            Debug.Log($"[LoopManager] Advancing to Loop {currentLoop}");

            ApplyCurrentLoopState();
        }

        /// <summary>
        /// Resets the loop to 0 or a target loop (e.g. for denial ending or debug testing).
        /// </summary>
        public void ResetLoop(int targetLoop = 0)
        {
            currentLoop = targetLoop;
            Debug.Log($"[LoopManager] Resetting to Loop {currentLoop}");
            OnLoopReset?.Invoke();
            ApplyCurrentLoopState();
        }

        private void ApplyCurrentLoopState()
        {
            // 1. Reset dynamic world elements (doors closed, interactive objects returned to base state)
            if (WorldStateResetter.Instance != null)
            {
                WorldStateResetter.Instance.ResetWorldState();
            }

            // 2. Apply anomalies specific to this loop
            if (AnomalyManager.Instance != null)
            {
                AnomalyManager.Instance.ApplyLoop(currentLoop);
            }

            // 3. Notify listeners
            OnLoopChanged?.Invoke(currentLoop);
        }
    }
}
