using UnityEngine;
using EndlessHallway.Interaction;
using EndlessHallway.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessHallway.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private float interactDistance = 2.5f;
        [SerializeField] private LayerMask interactableLayers = ~0; // All layers by default
        [SerializeField] private Transform rayOrigin;

        private IInteractable currentInteractable;
        private Camera playerCamera;
        private bool canInteract = true;

        public bool CanInteract
        {
            get => canInteract;
            set
            {
                canInteract = value;
                if (InteractionManager.Instance != null)
                {
                    InteractionManager.Instance.IsInteractionEnabled = value;
                }
                if (!canInteract)
                {
                    if (PromptUI.Instance != null) PromptUI.Instance.HidePrompt();
                    if (InteractionPrompt.Instance != null) InteractionPrompt.Instance.Hide();
                }
            }
        }

        private void Start()
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (rayOrigin == null && playerCamera != null)
            {
                rayOrigin = playerCamera.transform;
            }
            if (rayOrigin == null)
            {
                rayOrigin = transform;
            }
        }

        private void Update()
        {
            // If centralized InteractionManager is active, it handles raycasting and input
            if (InteractionManager.Instance != null && InteractionManager.Instance.enabled)
            {
                return;
            }

            if (!canInteract) return;

            PerformInteractionRaycast();
            HandleInteractionInput();
        }

        private void PerformInteractionRaycast()
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayers))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract())
                {
                    if (currentInteractable != interactable)
                    {
                        currentInteractable = interactable;
                        if (PromptUI.Instance != null)
                        {
                            PromptUI.Instance.ShowPrompt(interactable.GetInteractionPrompt());
                        }
                    }
                    return;
                }
            }

            if (currentInteractable != null)
            {
                currentInteractable = null;
                if (PromptUI.Instance != null)
                {
                    PromptUI.Instance.HidePrompt();
                }
            }
        }

        private void HandleInteractionInput()
        {
            if (currentInteractable == null) return;

            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
            }
            if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                interactPressed = true;
            }
#endif

            if (interactPressed)
            {
                currentInteractable.Interact(this);
                // Prompt will update on next raycast or interactable state change
                if (currentInteractable != null && !currentInteractable.CanInteract() && PromptUI.Instance != null)
                {
                    PromptUI.Instance.HidePrompt();
                }
            }
        }
    }
}
