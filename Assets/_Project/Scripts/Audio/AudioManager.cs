using System.Collections;
using UnityEngine;

namespace EndlessHallway.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Ambience - Base Layer")]
        [SerializeField] private AudioSource baseAmbienceSource;
        [SerializeField] private AudioClip defaultBaseClip;
        [Range(0f, 1f)] [SerializeField] private float baseVolume = 0.4f;

        [Header("Ambience - Overlay Layer")]
        [SerializeField] private AudioSource overlayAmbienceSource;
        [Range(0f, 1f)] [SerializeField] private float overlayMaxVolume = 0.6f;

        [Header("Crossfade Speed")]
        [SerializeField] private float crossfadeDuration = 2.0f;

        private Coroutine overlayFadeCoroutine;

        [Header("Pause Ducking")]
        [SerializeField] private float duckFactor = 0.25f; // -12dB
        private bool isDucked = false;
        private Coroutine duckCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupSources();
        }

        private void Start()
        {
            if (Core.SettingsManager.Instance != null)
            {
                Core.SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
                ApplyVolumeSettings();
            }

            StartBaseAmbience();
        }

        private void OnDestroy()
        {
            if (Core.SettingsManager.Instance != null)
            {
                Core.SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;
            }
        }

        private void HandleSettingsChanged()
        {
            ApplyVolumeSettings();
        }

        public void ApplyVolumeSettings()
        {
            float master = Core.SettingsManager.Instance != null ? Core.SettingsManager.Instance.MasterVolume : 1.0f;
            float music = Core.SettingsManager.Instance != null ? Core.SettingsManager.Instance.MusicVolume : 0.8f;
            AudioListener.volume = master;

            float currentDuck = isDucked ? duckFactor : 1.0f;
            if (baseAmbienceSource != null)
            {
                baseAmbienceSource.volume = baseVolume * music * currentDuck;
            }
            if (overlayAmbienceSource != null && overlayFadeCoroutine == null)
            {
                overlayAmbienceSource.volume = overlayMaxVolume * music * currentDuck;
            }
        }

        public void DuckAudio(bool duck)
        {
            isDucked = duck;
            if (duckCoroutine != null) StopCoroutine(duckCoroutine);
            duckCoroutine = StartCoroutine(DuckRoutine(duck ? duckFactor : 1.0f));
        }

        private IEnumerator DuckRoutine(float targetFactor)
        {
            float master = Core.SettingsManager.Instance != null ? Core.SettingsManager.Instance.MasterVolume : 1.0f;
            float music = Core.SettingsManager.Instance != null ? Core.SettingsManager.Instance.MusicVolume : 0.8f;

            float startBase = baseAmbienceSource != null ? baseAmbienceSource.volume : 0f;
            float targetBase = baseVolume * music * targetFactor;

            float elapsed = 0f;
            float duration = 0.15f; // Quick smooth transition in unscaled time

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                if (baseAmbienceSource != null)
                {
                    baseAmbienceSource.volume = Mathf.Lerp(startBase, targetBase, t);
                }
                yield return null;
            }

            if (baseAmbienceSource != null) baseAmbienceSource.volume = targetBase;
        }

        private void SetupSources()
        {
            if (baseAmbienceSource == null)
            {
                GameObject baseObj = new GameObject("BaseAmbienceSource");
                baseObj.transform.SetParent(transform);
                baseAmbienceSource = baseObj.AddComponent<AudioSource>();
                baseAmbienceSource.loop = true;
                baseAmbienceSource.spatialBlend = 0f; // 2D
                baseAmbienceSource.playOnAwake = false;
            }

            if (overlayAmbienceSource == null)
            {
                GameObject overlayObj = new GameObject("OverlayAmbienceSource");
                overlayObj.transform.SetParent(transform);
                overlayAmbienceSource = overlayObj.AddComponent<AudioSource>();
                overlayAmbienceSource.loop = true;
                overlayAmbienceSource.spatialBlend = 0f; // 2D
                overlayAmbienceSource.playOnAwake = false;
            }
        }

        public void StartBaseAmbience()
        {
            if (defaultBaseClip != null && baseAmbienceSource != null)
            {
                baseAmbienceSource.clip = defaultBaseClip;
                float music = Core.SettingsManager.Instance != null ? Core.SettingsManager.Instance.MusicVolume : 0.8f;
                baseAmbienceSource.volume = baseVolume * music;
                baseAmbienceSource.Play();
            }
        }

        public void SetOverlayAmbience(AudioClip clip, float targetVolume = -1f)
        {
            if (targetVolume < 0f) targetVolume = overlayMaxVolume;

            if (overlayFadeCoroutine != null)
            {
                StopCoroutine(overlayFadeCoroutine);
            }

            overlayFadeCoroutine = StartCoroutine(CrossfadeOverlayRoutine(clip, targetVolume));
        }

        private IEnumerator CrossfadeOverlayRoutine(AudioClip newClip, float targetVolume)
        {
            float music = Core.SettingsManager.Instance != null ? Core.SettingsManager.Instance.MusicVolume : 0.8f;
            float duck = isDucked ? duckFactor : 1.0f;
            float scaledTarget = targetVolume * music * duck;

            // Fade out existing overlay
            if (overlayAmbienceSource.isPlaying)
            {
                float startVol = overlayAmbienceSource.volume;
                float elapsed = 0f;
                while (elapsed < crossfadeDuration * 0.5f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    overlayAmbienceSource.volume = Mathf.Lerp(startVol, 0f, elapsed / (crossfadeDuration * 0.5f));
                    yield return null;
                }
                overlayAmbienceSource.Stop();
            }

            if (newClip == null)
            {
                overlayFadeCoroutine = null;
                yield break;
            }

            // Start new clip and fade in
            overlayAmbienceSource.clip = newClip;
            overlayAmbienceSource.volume = 0f;
            overlayAmbienceSource.Play();

            float inElapsed = 0f;
            while (inElapsed < crossfadeDuration * 0.5f)
            {
                inElapsed += Time.unscaledDeltaTime;
                overlayAmbienceSource.volume = Mathf.Lerp(0f, scaledTarget, inElapsed / (crossfadeDuration * 0.5f));
                yield return null;
            }
            overlayAmbienceSource.volume = scaledTarget;
            overlayFadeCoroutine = null;
        }
    }
}
