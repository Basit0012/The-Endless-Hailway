using UnityEngine;

namespace EndlessHallway.Interaction
{
    /// <summary>
    /// Contract for all interactive objects (doors, elevator buttons, noticeboard, clue documents).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Called when the player presses the interact key while focusing on this object.
        /// </summary>
        void Interact(Player.PlayerInteraction source);

        /// <summary>
        /// Prompt displayed in the reticle/UI (e.g., "[E] Open Door", "[E] Read Notice").
        /// </summary>
        string GetInteractionPrompt();

        /// <summary>
        /// Whether the object is currently capable of being interacted with.
        /// </summary>
        bool CanInteract();
    }
}
