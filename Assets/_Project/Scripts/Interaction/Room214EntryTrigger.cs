using UnityEngine;
using EndlessHallway.Core;
using EndlessHallway.Entity;
using EndlessHallway.Player;

namespace EndlessHallway.Interaction
{
    /// <summary>
    /// Trigger volume placed at the entrance of Room 214.
    /// In the final loop, walking inside triggers the Acceptance Ending.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Room214EntryTrigger : MonoBehaviour
    {
        private bool triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;

            // Check if player entered
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
            {
                // Verify if we are on the final loop (Loop 7 or loop >= 7)
                int currentLoop = LoopManager.Instance != null ? LoopManager.Instance.CurrentLoop : 0;
                if (currentLoop >= 7)
                {
                    triggered = true;
                    Debug.Log("[Room214EntryTrigger] Player entered Room 214 in final loop! Triggering Acceptance Ending.");
                    if (ObserverController.Instance != null)
                    {
                        ObserverController.Instance.TriggerFinalChoice(true);
                    }
                    else if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TriggerEnding(true);
                    }
                }
            }
        }

        public void ResetTrigger()
        {
            triggered = false;
        }
    }
}
