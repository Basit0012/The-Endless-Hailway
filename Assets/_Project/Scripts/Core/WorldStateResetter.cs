using System.Collections.Generic;
using UnityEngine;

namespace EndlessHallway.Core
{
    /// <summary>
    /// Interface for any object that needs to return to default state when a loop resets.
    /// </summary>
    public interface IResettable
    {
        void ResetToDefaultState();
    }

    /// <summary>
    /// Registry and trigger for resetting non-anomalous scene elements across loops.
    /// </summary>
    public class WorldStateResetter : MonoBehaviour
    {
        public static WorldStateResetter Instance { get; private set; }

        private readonly List<IResettable> resettables = new List<IResettable>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Register(IResettable item)
        {
            if (!resettables.Contains(item))
            {
                resettables.Add(item);
            }
        }

        public void Unregister(IResettable item)
        {
            if (resettables.Contains(item))
            {
                resettables.Remove(item);
            }
        }

        /// <summary>
        /// Resets all registered items back to their baseline state.
        /// </summary>
        public void ResetWorldState()
        {
            for (int i = resettables.Count - 1; i >= 0; i--)
            {
                if (resettables[i] != null)
                {
                    resettables[i].ResetToDefaultState();
                }
                else
                {
                    resettables.RemoveAt(i);
                }
            }
        }
    }
}
