using UnityEngine;

namespace EndlessHallway.Audio
{
    public class OneShotPool : MonoBehaviour
    {
        public static OneShotPool Instance { get; private set; }

        [SerializeField] private int poolSize = 8;
        private AudioSource[] sources;
        private int currentIndex = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePool();
        }

        private void InitializePool()
        {
            sources = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                GameObject child = new GameObject($"AudioSource_Pooled_{i}");
                child.transform.SetParent(transform);
                AudioSource src = child.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f; // 3D by default
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 1f;
                src.maxDistance = 20f;
                sources[i] = src;
            }
        }

        public void PlayOneShot(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || sources == null || sources.Length == 0) return;

            AudioSource src = sources[currentIndex];
            currentIndex = (currentIndex + 1) % poolSize;

            src.transform.position = position;
            src.spatialBlend = 1f;
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.loop = false;
            src.Play();
        }

        public void Play2D(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || sources == null || sources.Length == 0) return;

            AudioSource src = sources[currentIndex];
            currentIndex = (currentIndex + 1) % poolSize;

            src.spatialBlend = 0f; // 2D stereo
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.loop = false;
            src.Play();
        }
    }
}
