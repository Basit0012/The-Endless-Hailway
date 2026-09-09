using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace EndlessHallway.UI
{
    [RequireComponent(typeof(Button))]
    public class RetroButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private string originalText;
        [SerializeField] private bool useBrackets = true;

        [Header("Audio")]
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;

        private Button button;
        private bool isHovered = false;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (buttonText == null)
            {
                buttonText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (buttonText != null && string.IsNullOrEmpty(originalText))
            {
                originalText = buttonText.text.Trim();
            }

            // Ensure text does not block clicks
            if (buttonText != null)
            {
                buttonText.raycastTarget = false;
            }

            // Ensure vertical navigation by default for clean keyboard/controller support
            if (button != null)
            {
                var nav = button.navigation;
                nav.mode = Navigation.Mode.Vertical;
                button.navigation = nav;
            }
        }

        private void OnEnable()
        {
            ResetVisualState();
        }

        public void SetOriginalText(string text)
        {
            originalText = text.Trim();
            ResetVisualState();
        }

        public void SetAudioClips(AudioClip hover, AudioClip click)
        {
            hoverClip = hover;
            clickClip = click;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            ApplyHoverState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetVisualState();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (button != null && !button.interactable) return;
            ApplyHoverState();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            ResetVisualState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            PlayClickAudio();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (button != null && !button.interactable) return;
            PlayClickAudio();
        }

        private void ApplyHoverState()
        {
            if (isHovered) return;
            isHovered = true;

            if (buttonText != null && !string.IsNullOrEmpty(originalText))
            {
                string clean = originalText;
                if (clean.StartsWith(">") && clean.EndsWith("<"))
                {
                    // Already has carets
                    buttonText.text = clean;
                }
                else if (clean.StartsWith("[") && clean.EndsWith("]"))
                {
                    buttonText.text = $"> {clean} <";
                }
                else
                {
                    buttonText.text = $"> [ {clean} ] <";
                }
            }

            PlayHoverAudio();
        }

        private void ResetVisualState()
        {
            isHovered = false;
            if (buttonText != null && !string.IsNullOrEmpty(originalText))
            {
                if (useBrackets && !originalText.StartsWith("["))
                {
                    buttonText.text = $"[ {originalText} ]";
                }
                else
                {
                    buttonText.text = originalText;
                }
            }
        }

        private void PlayHoverAudio()
        {
            if (hoverClip != null && Audio.OneShotPool.Instance != null)
            {
                Audio.OneShotPool.Instance.Play2D(hoverClip, 0.45f, UnityEngine.Random.Range(0.98f, 1.02f));
            }
        }

        private void PlayClickAudio()
        {
            if (clickClip != null && Audio.OneShotPool.Instance != null)
            {
                Audio.OneShotPool.Instance.Play2D(clickClip, 0.75f, 1.0f);
            }
        }
    }
}
