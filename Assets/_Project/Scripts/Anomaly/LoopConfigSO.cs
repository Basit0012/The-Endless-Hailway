using System.Collections.Generic;
using UnityEngine;

namespace EndlessHallway.Anomaly
{
    [CreateAssetMenu(fileName = "Loop_Config", menuName = "Endless Hallway/Loop Configuration")]
    public class LoopConfigSO : ScriptableObject
    {
        [Header("Loop Identification")]
        public int loopIndex = 0;
        public string loopTitle = "Loop 0 - Baseline";
        [TextArea(2, 5)]
        public string narrativeNote;

        [Header("Ambience Overlay")]
        public AudioClip ambientOverlayClip;
        [Range(0f, 1f)]
        public float ambientOverlayVolume = 0.5f;

        [Header("Active Anomalies")]
        public List<AnomalyDefinitionSO> activeAnomalies = new List<AnomalyDefinitionSO>();
    }
}
