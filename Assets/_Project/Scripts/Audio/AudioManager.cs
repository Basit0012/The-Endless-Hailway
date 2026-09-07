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
            StartBaseAmbience();
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
                baseAmbienceSource.volume = baseVolume;
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
            // Fade out existing overlay
            if (overlayAmbienceSource.isPlaying)
            {
                float startVol = overlayAmbienceSource.volume;
                float elapsed = 0f;
                while (elapsed < crossfadeDuration * 0.5f)
                {
                    elapsed += Time.deltaTime;
                    overlayAmbienceSource.volume = Mathf.Lerp(startVol, 0f, elapsed / (crossfadeDuration * 0.5f));
                    yield return null;
                }
                overlayAmbienceSource.Stop();
            }

            if (newClip == null) yield break;

            // Start new clip and fade in
            overlayAmbienceSource.clip = newClip;
            overlayAmbienceSource.volume = 0f;
            overlayAmbienceSource.Play();

            float inElapsed = 0f;
            while (inElapsed < crossfadeDuration * 0.5f)
            {
                inElapsed += Time.deltaTime;
                overlayAmbienceSource.volume = Mathf.Lerp(0f, targetVolume, inElapsed / (crossfadeDuration * 0.5f));
                yield return null;
            }
            overlayAmbienceSource.volume = targetVolume;
        }
    }
}
