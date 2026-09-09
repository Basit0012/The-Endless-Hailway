using System.Collections;
using UnityEngine;
using EndlessHallway.Core;
using EndlessHallway.Player;
using EndlessHallway.Audio;

namespace EndlessHallway.Interaction
{
    public class DoorController : MonoBehaviour, IInteractable, IResettable
    {
        [Header("Identity")]
        [SerializeField] private string doorId = "Door_210";
        public string DoorId => doorId;

        [Header("State")]
        [SerializeField] private bool isLocked = false;
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool defaultLockedState = false;
        [SerializeField] private bool defaultOpenState = false;

        [Header("Movement Settings")]
        [SerializeField] private Transform pivotTransform;
        [SerializeField] private float openAngle = -90f;
        [SerializeField] private float swingSpeed = 3.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip closeClip;
        [SerializeField] private AudioClip lockedRattleClip;

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Coroutine swingCoroutine;
        private bool isMoving = false;

        public bool IsLocked => isLocked;
        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (pivotTransform == null)
            {
                pivotTransform = transform;
            }

            closedRotation = pivotTransform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

            defaultLockedState = isLocked;
            defaultOpenState = isOpen;

            if (isOpen)
            {
                pivotTransform.localRotation = openRotation;
            }
        }

        private void Start()
        {
            if (WorldStateResetter.Instance != null)
            {
                WorldStateResetter.Instance.Register(this);
            }
            if (Anomaly.AnomalyManager.Instance != null)
            {
                Anomaly.AnomalyManager.Instance.RegisterDoor(this);
            }
        }

        private void OnDestroy()
        {
            if (WorldStateResetter.Instance != null)
            {
                WorldStateResetter.Instance.Unregister(this);
            }
            if (Anomaly.AnomalyManager.Instance != null)
            {
                Anomaly.AnomalyManager.Instance.UnregisterDoor(this);
            }
        }

        public bool CanInteract()
        {
            return !isMoving;
        }

        public string GetInteractionPrompt()
        {
            if (isLocked)
            {
                return "[E] Locked";
            }
            return isOpen ? "[E] Close Door" : "[E] Open Door";
        }

        public void Interact(PlayerInteraction source)
        {
            if (isMoving) return;

            if (isLocked)
            {
                PlayRattle();
                return;
            }

            ToggleDoor();
        }

        public void ToggleDoor()
        {
            if (isMoving) return;
            SetOpen(!isOpen);
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
            if (swingCoroutine != null) StopCoroutine(swingCoroutine);
            swingCoroutine = StartCoroutine(AnimateSwing(isOpen ? openRotation : closedRotation));

            AudioClip clip = isOpen ? openClip : closeClip;
            if (clip != null && OneShotPool.Instance != null)
            {
                OneShotPool.Instance.PlayOneShot(clip, transform.position, 0.7f);
            }
        }

        public void SetOpenImmediate(bool open)
        {
            if (swingCoroutine != null) StopCoroutine(swingCoroutine);
            isMoving = false;
            isOpen = open;
            pivotTransform.localRotation = isOpen ? openRotation : closedRotation;
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            if (isLocked && isOpen)
            {
                SetOpen(false);
            }
        }

        public void SetLockedImmediate(bool locked)
        {
            isLocked = locked;
            if (isLocked && isOpen)
            {
                SetOpenImmediate(false);
            }
        }

        private void PlayRattle()
        {
            if (lockedRattleClip != null && OneShotPool.Instance != null)
            {
                OneShotPool.Instance.PlayOneShot(lockedRattleClip, transform.position, 0.6f);
            }
            StartCoroutine(RattleAnimation());
        }

        private IEnumerator RattleAnimation()
        {
            Quaternion original = pivotTransform.localRotation;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float jitter = Mathf.Sin(elapsed * 50f) * 1.5f;
                pivotTransform.localRotation = original * Quaternion.Euler(0f, jitter, 0f);
                yield return null;
            }

            pivotTransform.localRotation = original;
        }

        private IEnumerator AnimateSwing(Quaternion targetRotation)
        {
            isMoving = true;
            while (Quaternion.Angle(pivotTransform.localRotation, targetRotation) > 0.5f)
            {
                pivotTransform.localRotation = Quaternion.Slerp(pivotTransform.localRotation, targetRotation, Time.deltaTime * swingSpeed);
                yield return null;
            }
            pivotTransform.localRotation = targetRotation;
            isMoving = false;
        }

        public void ResetToDefaultState()
        {
            if (swingCoroutine != null) StopCoroutine(swingCoroutine);
            isMoving = false;
            isLocked = defaultLockedState;
            isOpen = defaultOpenState;
            pivotTransform.localRotation = isOpen ? openRotation : closedRotation;
        }
    }
}
