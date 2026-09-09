using System.Collections.Generic;
using UnityEngine;

namespace EndlessHallway.Anomaly
{
    [CreateAssetMenu(fileName = "PaintingSet_Default", menuName = "Endless Hallway/Painting Set")]
    public class PaintingSet : ScriptableObject
    {
        [Header("Mutating Painting Pool")]
        [Tooltip("Progressively more grotesque paintings corresponding to loop progression.")]
        public List<Material> stageMaterials = new List<Material>();

        public Material GetMaterialForStage(int stage)
        {
            if (stageMaterials == null || stageMaterials.Count == 0) return null;
            int idx = Mathf.Clamp(stage, 0, stageMaterials.Count - 1);
            return stageMaterials[idx];
        }
    }
}
