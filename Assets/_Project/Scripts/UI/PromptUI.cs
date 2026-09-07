using UnityEngine;
using TMPro;

namespace EndlessHallway.UI
{
    public class PromptUI : MonoBehaviour
    {
        public static PromptUI Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private GameObject reticleObject;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private GameObject promptContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            HidePrompt();
        }

        public void ShowPrompt(string message)
        {
            if (promptText != null)
            {
                promptText.text = message;
            }

            if (promptContainer != null)
            {
                promptContainer.SetActive(true);
            }
        }

        public void HidePrompt()
        {
            if (promptText != null)
            {
                promptText.text = "";
            }

            if (promptContainer != null)
            {
                promptContainer.SetActive(false);
            }
        }

        public void SetReticleVisible(bool visible)
        {
            if (reticleObject != null)
            {
                reticleObject.SetActive(visible);
            }
        }
    }
}
