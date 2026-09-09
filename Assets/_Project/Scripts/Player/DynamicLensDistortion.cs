using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using EndlessHallway.Core;

namespace EndlessHallway.Player
{
    /// <summary>
    /// Escalates URP Lens Distortion (barrel/fisheye warp) automatically as loops progress
    /// and pulses under physiological stress to sell the surreal "dollhouse diorama" claustrophobia.
    /// </summary>
    public class DynamicLensDistortion : MonoBehaviour
    {
        public static DynamicLensDistortion Instance { get; private set; }

        [Header("Volume Reference")]
        [SerializeField] private Volume targetVolume;
        private LensDistortion lensDistortion;

        [Header("Distortion Curves")]
        [Tooltip("Barrel distortion intensity per loop (0 to 7)")]
        [SerializeField] private float[] loopDistortionStrengths = new float[]
        {
            -0.05f, // Loop 0: Baseline, virtually flat
            -0.10f, // Loop 1: Subtle edge curvature
            -0.16f, // Loop 2: Minor barrel warp
            -0.24f, // Loop 3: Distinct doorway bowing
            -0.34f, // Loop 4: Pronounced fisheye
            -0.44f, // Loop 5: Deepening wrongness
            -0.54f, // Loop 6: Heavy dollhouse warp
            -0.62f  // Loop 7: Unhinged reality warp
        };

        [SerializeField] private float maxStressExtraWarp = -0.14f;
        [SerializeField] private float lerpSpeed = 3.5f;

        private float currentIntensity = -0.05f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (targetVolume == null)
            {
                targetVolume = FindAnyObjectByType<Volume>();
            }
            EnsureLensDistortion();
        }

        private void Start()
        {
            EnsureLensDistortion();
        }

        private void EnsureLensDistortion()
        {
            if (targetVolume != null && targetVolume.profile != null)
            {
                if (!targetVolume.profile.TryGet(out lensDistortion))
                {
                    lensDistortion = targetVolume.profile.Add<LensDistortion>(true);
                    lensDistortion.intensity.overrideState = true;
                    lensDistortion.scale.overrideState = true;
                    lensDistortion.scale.value = 1.0f;
                }
                else
                {
                    lensDistortion.intensity.overrideState = true;
                }
            }
        }

        private void Update()
        {
            if (lensDistortion == null)
            {
                EnsureLensDistortion();
                if (lensDistortion == null) return;
            }

            int curLoop = LoopManager.Instance != null ? LoopManager.Instance.CurrentLoop : 0;
            int idx = Mathf.Clamp(curLoop, 0, loopDistortionStrengths.Length - 1);
            float baseWarp = loopDistortionStrengths[idx];

            float stress = PlayerStressSystem.Instance != null ? PlayerStressSystem.Instance.StressLevel : 0f;
            float targetWarp = baseWarp + (stress * maxStressExtraWarp);

            currentIntensity = Mathf.Lerp(currentIntensity, targetWarp, Time.deltaTime * lerpSpeed);
            lensDistortion.intensity.value = currentIntensity;
        }

        public void ForceDistortion(float intensity)
        {
            currentIntensity = intensity;
            if (lensDistortion != null)
            {
                lensDistortion.intensity.value = intensity;
            }
        }
    }
}
