using UnityEngine;
using EndlessHallway.Player;
using EndlessHallway.UI;

namespace EndlessHallway.Interaction
{
    public class Examinable : MonoBehaviour, IInteractable
    {
        [Header("Clue Details")]
        [SerializeField] private string clueId = "Noticeboard_FireDrill";
        [SerializeField] private string title = "FIRE DRILL NOTICE";
        [SerializeField] [TextArea(4, 12)] private string documentText = "ATTENTION RESIDENTS\n\nA building-wide fire alarm test will take place on October 14th between 11:00 PM and 1:00 AM.\n\nPlease remain in your units unless instructed by building staff.\n\n- Marrow Point Management";
        [SerializeField] private Sprite documentSprite;
        [SerializeField] private bool warmToTouch = false;

        [Header("Prompt")]
        [SerializeField] private string promptText = "[E] Inspect Notice";

        public string ClueId => clueId;
        public string Title => title;
        public string DocumentText => documentText;
        public Sprite DocumentSprite => documentSprite;
        public bool IsWarmToTouch => warmToTouch;

        public bool CanInteract()
        {
            return ExamineUI.Instance == null || !ExamineUI.Instance.IsOpen;
        }

        public string GetInteractionPrompt()
        {
            return promptText;
        }

        public void Interact(PlayerInteraction source)
        {
            if (ExamineUI.Instance != null)
            {
                ExamineUI.Instance.Show(this, source);
            }
        }
    }
}
