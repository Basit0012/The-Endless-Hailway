using System.Collections;
using UnityEngine;

namespace EndlessHallway.Anomaly
{
    public class AnomalyTarget : MonoBehaviour
    {
        [Header("Identifier")]
        [Tooltip("Unique ID matching AnomalyDefinitionSO.targetObjectId")]
        [SerializeField] private string targetId;
        public string TargetId => targetId;

        private Vector3 baselinePosition;
        private Quaternion baselineRotation;
        private Vector3 baselineScale;
        private bool baselineActive;

        // Cached components
        private Renderer cachedRenderer;
        private Material baselineMaterial;
        private Light cachedLight;
        private bool baselineLightEnabled;
        private Color baselineLightColor;
        private float baselineLightIntensity;
        private AudioSource cachedAudioSource;
        private Coroutine flickerCoroutine;
        private bool isBaselineInitialized = false;

        private void Awake()
        {
            InitializeBaseline();
        }

        public void InitializeBaseline()
        {
            if (isBaselineInitialized) return;
            isBaselineInitialized = true;

            // Cache baseline transforms
            baselinePosition = transform.localPosition;
            baselineRotation = transform.localRotation;
            baselineScale = transform.localScale;
            baselineActive = gameObject.activeSelf;

            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer == null)
            {
                var quadChild = transform.Find("PaintingQuad");
                if (quadChild == null) quadChild = transform.Find("Canvas");
                if (quadChild != null) cachedRenderer = quadChild.GetComponent<Renderer>();
                else cachedRenderer = GetComponentInChildren<Renderer>();
            }

            if (cachedRenderer != null)
            {
                baselineMaterial = cachedRenderer.sharedMaterial;
            }

            cachedLight = GetComponent<Light>();
            if (cachedLight == null)
            {
                cachedLight = GetComponentInChildren<Light>();
            }

            if (cachedLight != null)
            {
                baselineLightEnabled = cachedLight.enabled;
                baselineLightColor = cachedLight.color;
                baselineLightIntensity = cachedLight.intensity;
            }

            cachedAudioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (AnomalyManager.Instance != null)
            {
                AnomalyManager.Instance.RegisterTarget(this);
            }
        }

        private void OnDestroy()
        {
            if (AnomalyManager.Instance != null)
            {
                AnomalyManager.Instance.UnregisterTarget(this);
            }
        }

        public void ApplyAnomaly(AnomalyDefinitionSO anomaly)
        {
            if (anomaly == null) return;

            switch (anomaly.anomalyType)
            {
                case AnomalyType.TransformShift:
                    transform.localPosition = baselinePosition + anomaly.positionOffset;
                    transform.localRotation = baselineRotation * Quaternion.Euler(anomaly.rotationOffset);
                    break;

                case AnomalyType.SetActiveState:
                    gameObject.SetActive(anomaly.activeState);
                    break;

                case AnomalyType.MaterialSwap:
                    if (cachedRenderer != null && anomaly.targetMaterial != null)
                    {
                        cachedRenderer.sharedMaterial = anomaly.targetMaterial;
                    }
                    break;

                case AnomalyType.LightFlicker:
                    if (cachedLight != null)
                    {
                        cachedLight.color = anomaly.lightColor;
                        if (anomaly.isLightFlickering)
                        {
                            if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
                            flickerCoroutine = StartCoroutine(FlickerLightRoutine(anomaly.flickerIntervalMin, anomaly.flickerIntervalMax));
                        }
                    }
                    break;

                case AnomalyType.AudioTrigger:
                    if (anomaly.audioClip != null)
                    {
                        if (cachedAudioSource == null)
                        {
                            cachedAudioSource = gameObject.AddComponent<AudioSource>();
                            cachedAudioSource.spatialBlend = 1f; // 3D sound
                            cachedAudioSource.maxDistance = 15f;
                            cachedAudioSource.rolloffMode = AudioRolloffMode.Linear;
                        }
                        cachedAudioSource.clip = anomaly.audioClip;
                        cachedAudioSource.loop = anomaly.loopAudio;
                        cachedAudioSource.volume = anomaly.audioVolume;
                        cachedAudioSource.Play();

                        if (UI.SubtitleUI.Instance != null && anomaly.audioClip != null)
                        {
                            string clipName = anomaly.audioClip.name.ToLower();
                            if (clipName.Contains("ring"))
                            {
                                UI.SubtitleUI.Instance.ShowSubtitle("[Telephone rings continuously inside Room 214]", 4.0f);
                            }
                            else if (clipName.Contains("call"))
                            {
                                UI.SubtitleUI.Instance.ShowSubtitle("[Distorted telephone voice] \"...Aiden?... 214 isn't responding... the smoke dampers are jammed...\"", 7.0f);
                            }
                            else
                            {
                                UI.SubtitleUI.Instance.ShowSubtitle($"[{anomaly.description}]", 3.5f);
                            }
                        }
                    }
                    break;
            }
        }

        private IEnumerator FlickerLightRoutine(float minInterval, float maxInterval)
        {
            while (true)
            {
                cachedLight.enabled = !cachedLight.enabled;
                float wait = Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(wait);
            }
        }

        public void ResetToBaseline()
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }

            transform.localPosition = baselinePosition;
            transform.localRotation = baselineRotation;
            transform.localScale = baselineScale;
            gameObject.SetActive(baselineActive);

            if (cachedRenderer != null && baselineMaterial != null)
            {
                cachedRenderer.sharedMaterial = baselineMaterial;
            }

            if (cachedLight != null)
            {
                cachedLight.enabled = baselineLightEnabled;
                cachedLight.color = baselineLightColor;
                cachedLight.intensity = baselineLightIntensity;
            }

            if (cachedAudioSource != null && cachedAudioSource.isPlaying)
            {
                cachedAudioSource.Stop();
            }
        }
    }
}
