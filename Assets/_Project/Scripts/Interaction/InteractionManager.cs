using System;
using UnityEngine;
using EndlessHallway.Player;
using EndlessHallway.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessHallway.Interaction
{
    /// <summary>
    /// Centralized manager coordinating player interaction raycasting, focus detection,
    /// distance validation, and execution.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        [Header("Raycast Settings")]
        [SerializeField] private float defaultInteractDistance = 2.5f;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private Transform rayOrigin;

        [Header("State")]
        [SerializeField] private bool isInteractionEnabled = true;

        // Events
        public event Action<IInteractable> OnInteractableFocused;
        public event Action<IInteractable> OnInteractableUnfocused;
        public event Action<IInteractable> OnInteractionExecuted;

        private IInteractable currentInteractable;
        private PlayerInteraction playerInteraction;
        private Camera playerCamera;

        public IInteractable CurrentInteractable => currentInteractable;
        public float DefaultInteractDistance => defaultInteractDistance;
        public bool IsInteractionEnabled
        {
            get => isInteractionEnabled;
            set
            {
                isInteractionEnabled = value;
                if (!isInteractionEnabled && currentInteractable != null)
                {
                    ClearFocus();
                }
            }
        }

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
            FindPlayerReferences();
        }

        public void FindPlayerReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
            if (rayOrigin == null && playerCamera != null)
            {
                rayOrigin = playerCamera.transform;
            }
            if (playerInteraction == null)
            {
                playerInteraction = FindAnyObjectByType<PlayerInteraction>();
            }
        }

        private void Update()
        {
            if (!isInteractionEnabled) return;

            if (rayOrigin == null)
            {
                FindPlayerReferences();
                if (rayOrigin == null) return;
            }

            PerformRaycast();
            HandleInput();
        }

        private void PerformRaycast()
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            float maxCheckDistance = defaultInteractDistance;

            if (currentInteractable != null && currentInteractable.CustomInteractionDistance > 0f)
            {
                maxCheckDistance = Mathf.Max(defaultInteractDistance, currentInteractable.CustomInteractionDistance);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, maxCheckDistance, interactableLayers))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                float allowedDistance = (interactable != null && interactable.CustomInteractionDistance > 0f)
                    ? interactable.CustomInteractionDistance
                    : defaultInteractDistance;

                if (interactable != null && hit.distance <= allowedDistance && interactable.CanInteract())
                {
                    if (currentInteractable != interactable)
                    {
                        SetFocus(interactable);
                    }
                    return;
                }
            }

            if (currentInteractable != null)
            {
                ClearFocus();
            }
        }

        private void SetFocus(IInteractable interactable)
        {
            if (currentInteractable != null)
            {
                currentInteractable.OnFocusExit();
                OnInteractableUnfocused?.Invoke(currentInteractable);
            }

            currentInteractable = interactable;
            currentInteractable.OnFocusEnter();
            OnInteractableFocused?.Invoke(currentInteractable);

            if (PromptUI.Instance != null)
            {
                PromptUI.Instance.ShowPrompt(currentInteractable.GetInteractionPrompt());
            }
        }

        private void ClearFocus()
        {
            if (currentInteractable != null)
            {
                var prev = currentInteractable;
                currentInteractable = null;
                prev.OnFocusExit();
                OnInteractableUnfocused?.Invoke(prev);

                if (PromptUI.Instance != null)
                {
                    PromptUI.Instance.HidePrompt();
                }
            }
        }

        private void HandleInput()
        {
            if (currentInteractable == null) return;

            if (!currentInteractable.CanInteract())
            {
                ClearFocus();
                return;
            }

            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }
            if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                interactPressed = true;
            }
#endif

            if (interactPressed)
            {
                ExecuteInteraction(currentInteractable);
            }
        }

        public void ExecuteInteraction(IInteractable target)
        {
            if (target == null || !target.CanInteract()) return;

            if (playerInteraction == null)
            {
                playerInteraction = FindAnyObjectByType<PlayerInteraction>();
            }

            target.Interact(playerInteraction);
            OnInteractionExecuted?.Invoke(target);

            if (currentInteractable != null && !currentInteractable.CanInteract())
            {
                ClearFocus();
            }
            else if (currentInteractable != null && PromptUI.Instance != null)
            {
                PromptUI.Instance.ShowPrompt(currentInteractable.GetInteractionPrompt());
            }
        }
    }
}
