using UnityEngine;
using EndlessHallway.Player;
using EndlessHallway.Audio;

namespace EndlessHallway.Anomaly
{
    [RequireComponent(typeof(Collider))]
    public class AnomalyTriggerVolume : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string triggerId = "Trigger_HallwayMid";
        [SerializeField] private int requiredLoop = 3;
        [SerializeField] private bool triggerOncePerLoop = true;

        [Header("Audio")]
        [SerializeField] private AudioClip soundClip;
        [SerializeField] private Transform soundOrigin;
        [Range(0f, 1f)] [SerializeField] private float volume = 1f;

        private bool hasTriggeredThisLoop = false;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Start()
        {
            if (Core.LoopManager.Instance != null)
            {
                Core.LoopManager.Instance.OnLoopChanged += HandleLoopChanged;
            }
        }

        private void OnDestroy()
        {
            if (Core.LoopManager.Instance != null)
            {
                Core.LoopManager.Instance.OnLoopChanged -= HandleLoopChanged;
            }
        }

        private void HandleLoopChanged(int currentLoop)
        {
            hasTriggeredThisLoop = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggeredThisLoop && triggerOncePerLoop) return;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                int currentLoop = Core.LoopManager.Instance != null ? Core.LoopManager.Instance.CurrentLoop : 0;
                if (currentLoop == requiredLoop)
                {
                    ExecuteTrigger();
                }
            }
        }

        private void ExecuteTrigger()
        {
            hasTriggeredThisLoop = true;
            Debug.Log($"[AnomalyTriggerVolume] Trigger '{triggerId}' fired.");

            if (soundClip != null && OneShotPool.Instance != null)
            {
                Vector3 pos = soundOrigin != null ? soundOrigin.position : transform.position;
                OneShotPool.Instance.PlayOneShot(soundClip, pos, volume);
            }
        }
    }
}
