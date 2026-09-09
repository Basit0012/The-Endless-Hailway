using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using EndlessHallway.Core;
using EndlessHallway.Entity;

namespace EndlessHallway.Player
{
    /// <summary>
    /// Physiological feedback system tracking proximity to anomalies and the Observer.
    /// Drives dynamic heartbeat audio, heavy breathing, vignette pulsing, and panic FOV creep.
    /// </summary>
    public class PlayerStressSystem : MonoBehaviour
    {
        public static PlayerStressSystem Instance { get; private set; }

        [Header("Stress Parameters")]
        [Range(0f, 1f)] [SerializeField] private float stressLevel = 0f;
        public float StressLevel => stressLevel;

        [SerializeField] private float stressAttackRate = 0.45f;
        [SerializeField] private float stressDecayRate = 0.20f;
        [SerializeField] private float maxFovCreep = 4.5f;

        [Header("Proximity Thresholds")]
        [SerializeField] private float observerStressDistance = 11.0f;
        [SerializeField] private float room214StressDistance = 6.0f;

        [Header("Audio Components")]
        [SerializeField] private AudioSource heartbeatSource;
        [SerializeField] private AudioSource breathingSource;
        [SerializeField] private AudioClip heartbeatClip;
        [SerializeField] private AudioClip breathingClip;

        [Header("Volume Reference")]
        [SerializeField] private Volume globalVolume;
        private Vignette vignetteComponent;
        private float baseVignetteIntensity = 0.35f;

        private Camera playerCamera;
        private PlayerCameraLook playerCameraLook;
        private float heartbeatPhase = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupAudioSources();
        }

        private void Start()
        {
            FindReferences();
        }

        public void FindReferences()
        {
            if (playerCameraLook == null)
            {
                playerCameraLook = GetComponent<PlayerCameraLook>();
                if (playerCameraLook == null) playerCameraLook = FindAnyObjectByType<PlayerCameraLook>();
            }

            if (playerCamera == null && playerCameraLook != null)
            {
                playerCamera = playerCameraLook.TargetCamera;
            }
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (globalVolume == null)
            {
                globalVolume = FindAnyObjectByType<Volume>();
            }
            if (globalVolume != null && globalVolume.profile != null)
            {
                globalVolume.profile.TryGet(out vignetteComponent);
                if (vignetteComponent != null)
                {
                    baseVignetteIntensity = vignetteComponent.intensity.value;
                }
            }
        }

        private void SetupAudioSources()
        {
            if (heartbeatSource == null)
            {
                GameObject hbObj = new GameObject("Audio_Heartbeat");
                hbObj.transform.SetParent(transform);
                heartbeatSource = hbObj.AddComponent<AudioSource>();
                heartbeatSource.spatialBlend = 0f; // 2D
                heartbeatSource.loop = true;
                heartbeatSource.playOnAwake = false;
            }

            if (breathingSource == null)
            {
                GameObject brObj = new GameObject("Audio_Breathing");
                brObj.transform.SetParent(transform);
                breathingSource = brObj.AddComponent<AudioSource>();
                breathingSource.spatialBlend = 0f; // 2D
                breathingSource.loop = true;
                breathingSource.playOnAwake = false;
            }

            if (heartbeatClip == null)
            {
                heartbeatClip = Resources.Load<AudioClip>("sfx_heartbeat");
            }
            if (breathingClip == null)
            {
                breathingClip = Resources.Load<AudioClip>("sfx_breathing");
            }
        }

        private void Update()
        {
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            {
                return;
            }

            float targetStress = CalculateTargetStress();

            if (targetStress > stressLevel)
            {
                stressLevel = Mathf.MoveTowards(stressLevel, targetStress, stressAttackRate * Time.deltaTime);
            }
            else
            {
                stressLevel = Mathf.MoveTowards(stressLevel, targetStress, stressDecayRate * Time.deltaTime);
            }

            ApplyPhysiologicalEffects();
        }

        private float CalculateTargetStress()
        {
            float target = 0f;

            // 1. Observer Proximity & Gaze
            if (ObserverController.Instance != null && ObserverController.Instance.CurrentState != ObserverState.Hidden)
            {
                float dist = Vector3.Distance(transform.position, ObserverController.Instance.transform.position);
                if (dist < observerStressDistance)
                {
                    float proxStress = 1f - (dist / observerStressDistance);
                    target = Mathf.Max(target, proxStress * 0.85f);

                    // If looking toward observer, increase stress
                    if (playerCamera != null)
                    {
                        Vector3 toObs = (ObserverController.Instance.transform.position + Vector3.up * 1.2f) - playerCamera.transform.position;
                        float angle = Vector3.Angle(playerCamera.transform.forward, toObs);
                        if (angle < 45f)
                        {
                            target = Mathf.Max(target, 0.95f);
                        }
                    }
                }
            }

            // 2. Room 214 proximity in late loops (Loops 5, 6, 7)
            if (LoopManager.Instance != null && LoopManager.Instance.CurrentLoop >= 5)
            {
                Vector3 door214Pos = new Vector3(1.2f, 0f, 10.0f);
                float distTo214 = Vector3.Distance(transform.position, door214Pos);
                if (distTo214 < room214StressDistance)
                {
                    float roomStress = 1f - (distTo214 / room214StressDistance);
                    target = Mathf.Max(target, roomStress * 0.70f);
                }
            }

            return Mathf.Clamp01(target);
        }

        private void ApplyPhysiologicalEffects()
        {
            float master = SettingsManager.Instance != null ? SettingsManager.Instance.MasterVolume : 1.0f;
            float sfx = SettingsManager.Instance != null ? SettingsManager.Instance.SFXVolume : 0.85f;
            float netVolume = master * sfx;

            // 1. Dynamic Heartbeat
            if (heartbeatSource != null)
            {
                if (stressLevel > 0.05f)
                {
                    if (!heartbeatSource.isPlaying) heartbeatSource.Play();
                    heartbeatSource.volume = Mathf.Lerp(0f, 0.85f, (stressLevel - 0.05f) / 0.95f) * netVolume;
                    heartbeatSource.pitch = Mathf.Lerp(0.85f, 1.45f, stressLevel);
                }
                else
                {
                    if (heartbeatSource.isPlaying) heartbeatSource.Stop();
                }
            }

            // 2. Heavy Breathing
            if (breathingSource != null)
            {
                if (stressLevel > 0.45f)
                {
                    if (!breathingSource.isPlaying) breathingSource.Play();
                    breathingSource.volume = Mathf.Lerp(0f, 0.65f, (stressLevel - 0.45f) / 0.55f) * netVolume;
                    breathingSource.pitch = Mathf.Lerp(0.9f, 1.2f, stressLevel);
                }
                else
                {
                    if (breathingSource.isPlaying) breathingSource.Stop();
                }
            }

            // 3. Dynamic FOV Creep
            if (playerCamera != null && SettingsManager.Instance != null)
            {
                float baseFov = SettingsManager.Instance.FieldOfView;
                float creep = Mathf.Lerp(0f, maxFovCreep, stressLevel);
                playerCamera.fieldOfView = baseFov + creep;
            }

            // 4. Vignette Heartbeat Pulse
            if (vignetteComponent == null && globalVolume != null && globalVolume.profile != null)
            {
                globalVolume.profile.TryGet(out vignetteComponent);
            }

            if (vignetteComponent != null)
            {
                float bpm = Mathf.Lerp(60f, 135f, stressLevel);
                heartbeatPhase += Time.deltaTime * (bpm / 60f) * Mathf.PI * 2f;
                float pulse = (Mathf.Sin(heartbeatPhase) * 0.5f + 0.5f) * stressLevel * 0.22f;
                vignetteComponent.intensity.value = Mathf.Clamp(baseVignetteIntensity + pulse, 0f, 0.85f);
            }
        }

        public void SetAudioClips(AudioClip hbClip, AudioClip brClip)
        {
            heartbeatClip = hbClip;
            breathingClip = brClip;

            if (heartbeatSource != null && hbClip != null)
            {
                heartbeatSource.clip = hbClip;
            }
            if (breathingSource != null && brClip != null)
            {
                breathingSource.clip = brClip;
            }
        }

        public void AddTrauma(float amount)
        {
            stressLevel = Mathf.Clamp01(stressLevel + amount);
        }
    }
}
