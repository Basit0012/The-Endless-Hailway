using System.Collections.Generic;
using UnityEngine;
using EndlessHallway.Interaction;
using EndlessHallway.Audio;

namespace EndlessHallway.Anomaly
{
    /// <summary>
    /// Master registry and applier for loop anomalies based on ScriptableObject configurations.
    /// </summary>
    public class AnomalyManager : MonoBehaviour
    {
        public static AnomalyManager Instance { get; private set; }

        [Header("Configurations")]
        [SerializeField] private List<LoopConfigSO> loopConfigs = new List<LoopConfigSO>();

        [Header("Volume Management")]
        [SerializeField] private UnityEngine.Rendering.Volume sceneVolume;
        [SerializeField] private UnityEngine.Rendering.VolumeProfile baselineVolumeProfile;

        private readonly Dictionary<string, AnomalyTarget> targetRegistry = new Dictionary<string, AnomalyTarget>();
        private readonly Dictionary<string, DoorController> doorRegistry = new Dictionary<string, DoorController>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (sceneVolume == null)
            {
                sceneVolume = FindAnyObjectByType<UnityEngine.Rendering.Volume>();
            }
            if (sceneVolume != null && baselineVolumeProfile == null)
            {
                baselineVolumeProfile = sceneVolume.sharedProfile;
            }

            // Discover and register all targets and doors in the scene (including currently inactive ones)
            AnomalyTarget[] targets = FindObjectsByType<AnomalyTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in targets)
            {
                if (t != null)
                {
                    t.InitializeBaseline();
                    RegisterTarget(t);
                }
            }

            DoorController[] doors = FindObjectsByType<DoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var d in doors)
            {
                if (d != null)
                {
                    RegisterDoor(d);
                }
            }
        }

        public void RegisterTarget(AnomalyTarget target)
        {
            if (target != null && !string.IsNullOrEmpty(target.TargetId))
            {
                targetRegistry[target.TargetId] = target;
            }
        }

        public void UnregisterTarget(AnomalyTarget target)
        {
            if (target != null && !string.IsNullOrEmpty(target.TargetId) && targetRegistry.ContainsKey(target.TargetId))
            {
                targetRegistry.Remove(target.TargetId);
            }
        }

        public void RegisterDoor(DoorController door)
        {
            if (door != null && !string.IsNullOrEmpty(door.DoorId))
            {
                doorRegistry[door.DoorId] = door;
            }
        }

        public void UnregisterDoor(DoorController door)
        {
            if (door != null && !string.IsNullOrEmpty(door.DoorId) && doorRegistry.ContainsKey(door.DoorId))
            {
                doorRegistry.Remove(door.DoorId);
            }
        }

        public void SetSceneVolumeProfile(UnityEngine.Rendering.VolumeProfile profile)
        {
            if (sceneVolume != null && profile != null)
            {
                sceneVolume.profile = profile;
            }
        }

        /// <summary>
        /// Resets the hallway to baseline, then applies anomalies configured for the specified loop.
        /// </summary>
        public void ApplyLoop(int loopIndex)
        {
            // 1. Reset all registered targets to baseline
            foreach (var target in targetRegistry.Values)
            {
                if (target != null)
                {
                    target.ResetToBaseline();
                }
            }

            // Reset all registered doors to baseline
            foreach (var door in doorRegistry.Values)
            {
                if (door != null)
                {
                    door.ResetToDefaultState();
                }
            }

            // Reset Volume to baseline
            if (sceneVolume != null && baselineVolumeProfile != null)
            {
                sceneVolume.profile = baselineVolumeProfile;
            }

            // Reset Observer to hidden
            if (Entity.ObserverController.Instance != null)
            {
                Entity.ObserverController.Instance.ResetToDefaultState();
            }

            // 2. Locate configuration for this loop
            LoopConfigSO config = GetConfigForLoop(loopIndex);
            if (config == null)
            {
                Debug.Log($"[AnomalyManager] No specific config found for Loop {loopIndex}. Hallway remains at baseline.");
                return;
            }

            Debug.Log($"[AnomalyManager] Applying Loop {config.loopIndex}: '{config.loopTitle}' ({config.activeAnomalies.Count} anomalies)");

            // 3. Apply configured anomalies
            foreach (var anomaly in config.activeAnomalies)
            {
                if (anomaly == null) continue;

                if (anomaly.anomalyType == AnomalyType.LightingChange)
                {
                    if (sceneVolume != null && anomaly.volumeProfile != null)
                    {
                        sceneVolume.profile = anomaly.volumeProfile;
                    }
                }
                else if (anomaly.anomalyType == AnomalyType.ObserverSpawn)
                {
                    if (Entity.ObserverController.Instance != null)
                    {
                        Entity.ObserverController.Instance.SpawnAtPoint(anomaly.observerSpawnPointId, anomaly.observerState);
                    }
                }
                else if (anomaly.anomalyType == AnomalyType.DoorLockState)
                {
                    // Check door registry or find in scene
                    if (doorRegistry.TryGetValue(anomaly.targetObjectId, out DoorController door))
                    {
                        door.SetLocked(anomaly.setDoorLocked);
                        if (anomaly.setDoorOpen) door.SetOpen(true);
                    }
                    else
                    {
                        // Fallback search
                        DoorController[] allDoors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
                        foreach (var d in allDoors)
                        {
                            if (d.DoorId == anomaly.targetObjectId)
                            {
                                doorRegistry[d.DoorId] = d;
                                d.SetLocked(anomaly.setDoorLocked);
                                if (anomaly.setDoorOpen) d.SetOpen(true);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (targetRegistry.TryGetValue(anomaly.targetObjectId, out AnomalyTarget target))
                    {
                        target.ApplyAnomaly(anomaly);
                    }
                    else
                    {
                        Debug.LogWarning($"[AnomalyManager] Anomaly target '{anomaly.targetObjectId}' not found in scene.");
                    }
                }
            }

            // 4. Update audio ambience
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetOverlayAmbience(config.ambientOverlayClip, config.ambientOverlayVolume);
            }
        }

        private LoopConfigSO GetConfigForLoop(int loopIndex)
        {
            foreach (var cfg in loopConfigs)
            {
                if (cfg != null && cfg.loopIndex == loopIndex)
                {
                    return cfg;
                }
            }
            return null;
        }
    }
}
