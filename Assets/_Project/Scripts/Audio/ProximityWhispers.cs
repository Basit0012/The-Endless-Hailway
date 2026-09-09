using UnityEngine;
using EndlessHallway.Core;

namespace EndlessHallway.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class ProximityWhispers : MonoBehaviour
    {
        [Header("Distance Settings")]
        [SerializeField] private float maxWhisperDistance = 7.5f;
        [SerializeField] private float minWhisperDistance = 1.5f;
        [SerializeField] private float maxVolume = 0.55f;

        private AudioSource audioSource;
        private Transform playerTransform;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = minWhisperDistance;
            audioSource.maxDistance = maxWhisperDistance;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }

        private void Start()
        {
            FindPlayer();
            if (audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        private void FindPlayer()
        {
            if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            float master = SettingsManager.Instance != null ? SettingsManager.Instance.MasterVolume : 1.0f;
            float sfx = SettingsManager.Instance != null ? SettingsManager.Instance.SFXVolume : 0.85f;

            if (dist < maxWhisperDistance)
            {
                if (!audioSource.isPlaying) audioSource.Play();
                float factor = 1f - Mathf.Clamp01((dist - minWhisperDistance) / (maxWhisperDistance - minWhisperDistance));
                audioSource.volume = factor * maxVolume * master * sfx;
            }
            else
            {
                audioSource.volume = 0f;
            }
        }
    }
}
