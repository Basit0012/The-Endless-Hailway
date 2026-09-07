using UnityEngine;
using EndlessHallway.Player;

namespace EndlessHallway.Interaction
{
    /// <summary>
    /// Generic contract for all interactive objects (doors, elevator, noticeboard, phone, clues, switches, etc.).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Prompt displayed in the reticle/UI (e.g., "[E] Open Door", "[E] Read Notice", "[E] Answer Phone").
        /// </summary>
        string GetInteractionPrompt();

        /// <summary>
        /// Whether the object is currently capable of being interacted with.
        /// </summary>
        bool CanInteract();

        /// <summary>
        /// Called when the player presses the interact key while focusing on this object.
        /// </summary>
        void Interact(PlayerInteraction source);

        /// <summary>
        /// Optional custom interaction distance. Return <= 0 to use the global default distance.
        /// </summary>
        float CustomInteractionDistance => 0f;

        /// <summary>
        /// Optional callback when player begins looking at this interactable.
        /// </summary>
        void OnFocusEnter() { }

        /// <summary>
        /// Optional callback when player looks away or moves out of range.
        /// </summary>
        void OnFocusExit() { }
    }
}
