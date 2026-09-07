using UnityEngine;

namespace EndlessHallway.Anomaly
{
    public enum AnomalyType
    {
        TransformShift = 0,
        SetActiveState = 1,
        MaterialSwap = 2,
        LightFlicker = 3,
        DoorLockState = 4,
        AudioTrigger = 5,
        LightingChange = 6,
        ObserverSpawn = 7
    }

    /// <summary>
    /// Data-driven definition of an anomaly that can be authored in the inspector without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAnomaly", menuName = "Endless Hallway/Anomaly Definition")]
    public class AnomalyDefinitionSO : ScriptableObject
    {
        [Header("Identification")]
        public string anomalyId = "Anomaly_UniqueId";
        [TextArea(2, 4)]
        public string description;

        [Header("Targeting")]
        [Tooltip("Matches the targetId on an AnomalyTarget or doorId on a DoorController.")]
        public string targetObjectId;

        [Header("Anomaly Type")]
        public AnomalyType anomalyType;

        [Header("Transform Shift")]
        public Vector3 positionOffset;
        public Vector3 rotationOffset;

        [Header("Active State")]
        public bool activeState = true;

        [Header("Material Swap")]
        public Material targetMaterial;

        [Header("Light Flicker")]
        public bool isLightFlickering = true;
        public float flickerIntervalMin = 0.05f;
        public float flickerIntervalMax = 0.25f;
        public Color lightColor = Color.white;

        [Header("Door State")]
        public bool setDoorLocked = true;
        public bool setDoorOpen = false;

        [Header("Audio")]
        public AudioClip audioClip;
        public bool loopAudio = false;
        [Range(0f, 1f)] public float audioVolume = 1f;

        [Header("Volume Profile (LightingChange)")]
        public UnityEngine.Rendering.VolumeProfile volumeProfile;

        [Header("Observer Spawn")]
        public string observerSpawnPointId = "FarHallway";
        public EndlessHallway.Entity.ObserverState observerState = EndlessHallway.Entity.ObserverState.GlimpseFar;
    }
}
