using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EndlessHallway.Interaction
{
    /// <summary>
    /// UI presentation component for subtle, atmospheric interaction prompts.
    /// Listens to InteractionManager focus events and provides smooth alpha fading and reticle feedback.
    /// </summary>
    public class InteractionPrompt : MonoBehaviour
    {
        public static InteractionPrompt Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private CanvasGroup promptCanvasGroup;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private Image reticleDot;

        [Header("Animation Settings")]
        [SerializeField] private float fadeSpeed = 8.0f;
        [SerializeField] private Vector3 reticleNormalScale = Vector3.one;
        [SerializeField] private Vector3 reticleFocusScale = new Vector3(1.35f, 1.35f, 1f);
        [SerializeField] private Color reticleNormalColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private Color reticleFocusColor = new Color(1f, 0.95f, 0.8f, 0.9f);

        private float targetAlpha = 0f;
        private Vector3 targetReticleScale = Vector3.one;
        private Color targetReticleColor = Color.white;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (promptCanvasGroup == null) promptCanvasGroup = GetComponent<CanvasGroup>();
            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = 0f;
            }
            targetReticleScale = reticleNormalScale;
            targetReticleColor = reticleNormalColor;
        }

        private void Start()
        {
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.OnInteractableFocused += HandleInteractableFocused;
                InteractionManager.Instance.OnInteractableUnfocused += HandleInteractableUnfocused;
                InteractionManager.Instance.OnInteractionExecuted += HandleInteractionExecuted;
            }
        }

        private void OnDestroy()
        {
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.OnInteractableFocused -= HandleInteractableFocused;
                InteractionManager.Instance.OnInteractableUnfocused -= HandleInteractableUnfocused;
                InteractionManager.Instance.OnInteractionExecuted -= HandleInteractionExecuted;
            }
        }

        private void Update()
        {
            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = Mathf.MoveTowards(promptCanvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            }

            if (reticleDot != null)
            {
                reticleDot.transform.localScale = Vector3.Lerp(reticleDot.transform.localScale, targetReticleScale, Time.deltaTime * fadeSpeed);
                reticleDot.color = Color.Lerp(reticleDot.color, targetReticleColor, Time.deltaTime * fadeSpeed);
            }
        }

        private void HandleInteractableFocused(IInteractable interactable)
        {
            if (interactable == null) return;

            if (promptText != null)
            {
                promptText.text = interactable.GetInteractionPrompt();
            }

            targetAlpha = 1f;
            targetReticleScale = reticleFocusScale;
            targetReticleColor = reticleFocusColor;
        }

        private void HandleInteractableUnfocused(IInteractable interactable)
        {
            targetAlpha = 0f;
            targetReticleScale = reticleNormalScale;
            targetReticleColor = reticleNormalColor;
        }

        private void HandleInteractionExecuted(IInteractable interactable)
        {
            if (reticleDot != null)
            {
                reticleDot.transform.localScale = reticleFocusScale * 1.25f;
            }
            if (interactable != null && promptText != null && interactable.CanInteract())
            {
                promptText.text = interactable.GetInteractionPrompt();
            }
        }

        public void SetPromptText(string text)
        {
            if (promptText != null) promptText.text = text;
            targetAlpha = string.IsNullOrEmpty(text) ? 0f : 1f;
        }

        public void Hide()
        {
            targetAlpha = 0f;
            targetReticleScale = reticleNormalScale;
            targetReticleColor = reticleNormalColor;
        }
    }
}
