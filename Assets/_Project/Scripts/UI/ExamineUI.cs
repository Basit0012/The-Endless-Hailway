using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EndlessHallway.Player;
using EndlessHallway.Interaction;
using EndlessHallway.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessHallway.UI
{
    public class ExamineUI : MonoBehaviour
    {
        public static ExamineUI Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Image clueImage;
        [SerializeField] private GameObject warmEffectIndicator;
        [SerializeField] private Button closeButton;

        private PlayerInteraction activePlayer;
        private PlayerController activePlayerController;
        private PlayerCameraLook activePlayerLook;
        private bool isOpen = false;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (panelRoot != null) panelRoot.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            if (!isOpen) return;

            bool closePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                closePressed = true;
            }
#endif

            if (closePressed)
            {
                Close();
            }
        }

        public void Show(Examinable examinable, PlayerInteraction player)
        {
            if (examinable == null) return;

            isOpen = true;
            activePlayer = player;

            if (player != null)
            {
                activePlayerController = player.GetComponent<PlayerController>();
                activePlayerLook = player.GetComponent<PlayerCameraLook>();

                if (activePlayerController != null) activePlayerController.CanMove = false;
                if (activePlayerLook != null)
                {
                    activePlayerLook.CanLook = false;
                    activePlayerLook.UnlockCursor();
                }
                player.CanInteract = false;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Examining);
            }

            if (titleText != null) titleText.text = SanitizeText(examinable.Title);
            if (bodyText != null) bodyText.text = SanitizeText(examinable.DocumentText);

            if (SubtitleUI.Instance != null && SettingsManager.Instance != null && SettingsManager.Instance.SubtitlesEnabled)
            {
                SubtitleUI.Instance.ShowSubtitle($"[Reading: {examinable.Title}]", 2.5f);
            }

            if (clueImage != null)
            {
                if (examinable.DocumentSprite != null)
                {
                    clueImage.gameObject.SetActive(true);
                    clueImage.sprite = examinable.DocumentSprite;
                }
                else
                {
                    clueImage.gameObject.SetActive(false);
                }
            }

            if (warmEffectIndicator != null)
            {
                warmEffectIndicator.SetActive(examinable.IsWarmToTouch);
                if (examinable.IsWarmToTouch)
                {
                    var tmp = warmEffectIndicator.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        bool cb = Core.SettingsManager.Instance != null && Core.SettingsManager.Instance.ColorblindAssistance;
                        tmp.text = cb 
                            ? "[THERMAL ARTIFACT // RADIAL HEAT DETECTED]" 
                            : "[The document radiates a faint, unsettling warmth...]";
                    }
                }
            }

            if (Core.SaveManager.Instance != null && !string.IsNullOrEmpty(examinable.ClueId))
            {
                Core.SaveManager.Instance.RecordClue(examinable.ClueId);
            }

            if (panelRoot != null) panelRoot.SetActive(true);
            if (PromptUI.Instance != null) PromptUI.Instance.HidePrompt();
        }

        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            if (panelRoot != null) panelRoot.SetActive(false);

            if (activePlayer != null)
            {
                if (activePlayerController != null) activePlayerController.CanMove = true;
                if (activePlayerLook != null)
                {
                    activePlayerLook.CanLook = true;
                    activePlayerLook.LockCursor();
                }
                activePlayer.CanInteract = true;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Exploring);
            }
        }

        private string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input
                .Replace('\u2018', '\'')  // Left single quotation mark
                .Replace('\u2019', '\'')  // Right single quotation mark
                .Replace('\u201C', '\"')  // Left double quotation mark
                .Replace('\u201D', '\"')  // Right double quotation mark
                .Replace('\u2013', '-')   // En dash
                .Replace('\u2014', '-')   // Em dash
                .Replace('\u2026', '.')   // Horizontal ellipsis
                .Replace('\u00A0', ' ');  // Non-breaking space
        }
    }
}
