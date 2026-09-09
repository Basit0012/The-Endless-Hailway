using System;
using UnityEngine;

namespace EndlessHallway.Core
{
    /// <summary>
    /// Central manager for audio, display, accessibility, and control settings.
    /// Persists settings to PlayerPrefs and dispatches real-time change events.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        private const string KEY_MASTER_VOL = "Setting_MasterVolume";
        private const string KEY_MUSIC_VOL = "Setting_MusicVolume";
        private const string KEY_SFX_VOL = "Setting_SFXVolume";
        private const string KEY_BRIGHTNESS = "Setting_Brightness";
        private const string KEY_FOV = "Setting_FOV";
        private const string KEY_SHAKE = "Setting_CameraShake";
        private const string KEY_SUBTITLES = "Setting_Subtitles";
        private const string KEY_INVERT_Y = "Setting_InvertY";
        private const string KEY_COLORBLIND = "Setting_Colorblind";
        private const string KEY_PHOTOSENSITIVE = "Setting_Photosensitive";

        [Header("Audio")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;

        [Header("Display")]
        [Range(-1f, 1f)] [SerializeField] private float brightness = 0.0f;
        [Range(60f, 100f)] [SerializeField] private float fieldOfView = 75f;

        [Header("Gameplay & Controls")]
        [Range(0f, 1f)] [SerializeField] private float cameraShake = 1.0f;
        [SerializeField] private bool invertY = false;

        [Header("Accessibility")]
        [SerializeField] private bool subtitlesEnabled = true;
        [SerializeField] private bool colorblindAssistance = false;
        [SerializeField] private bool photosensitivitySoftening = false;

        public event Action OnSettingsChanged;

        public float MasterVolume
        {
            get => masterVolume;
            set { masterVolume = Mathf.Clamp01(value); TriggerChange(); }
        }

        public float MusicVolume
        {
            get => musicVolume;
            set { musicVolume = Mathf.Clamp01(value); TriggerChange(); }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set { sfxVolume = Mathf.Clamp01(value); TriggerChange(); }
        }

        public float Brightness
        {
            get => brightness;
            set { brightness = Mathf.Clamp(value, -1f, 1f); TriggerChange(); }
        }

        public float FieldOfView
        {
            get => fieldOfView;
            set { fieldOfView = Mathf.Clamp(value, 60f, 100f); TriggerChange(); }
        }

        public float CameraShake
        {
            get => cameraShake;
            set { cameraShake = Mathf.Clamp01(value); TriggerChange(); }
        }

        public bool InvertY
        {
            get => invertY;
            set { invertY = value; TriggerChange(); }
        }

        public bool SubtitlesEnabled
        {
            get => subtitlesEnabled;
            set { subtitlesEnabled = value; TriggerChange(); }
        }

        public bool ColorblindAssistance
        {
            get => colorblindAssistance;
            set { colorblindAssistance = value; TriggerChange(); }
        }

        public bool PhotosensitivitySoftening
        {
            get => photosensitivitySoftening;
            set { photosensitivitySoftening = value; TriggerChange(); }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
        }

        public void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1.0f);
            musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.8f);
            sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 0.85f);
            brightness = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, 0.0f);
            fieldOfView = PlayerPrefs.GetFloat(KEY_FOV, 75f);
            cameraShake = PlayerPrefs.GetFloat(KEY_SHAKE, 1.0f);
            subtitlesEnabled = PlayerPrefs.GetInt(KEY_SUBTITLES, 1) == 1;
            invertY = PlayerPrefs.GetInt(KEY_INVERT_Y, 0) == 1;
            colorblindAssistance = PlayerPrefs.GetInt(KEY_COLORBLIND, 0) == 1;
            photosensitivitySoftening = PlayerPrefs.GetInt(KEY_PHOTOSENSITIVE, 0) == 1;

            TriggerChange();
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(KEY_MASTER_VOL, masterVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOL, musicVolume);
            PlayerPrefs.SetFloat(KEY_SFX_VOL, sfxVolume);
            PlayerPrefs.SetFloat(KEY_BRIGHTNESS, brightness);
            PlayerPrefs.SetFloat(KEY_FOV, fieldOfView);
            PlayerPrefs.SetFloat(KEY_SHAKE, cameraShake);
            PlayerPrefs.SetInt(KEY_SUBTITLES, subtitlesEnabled ? 1 : 0);
            PlayerPrefs.SetInt(KEY_INVERT_Y, invertY ? 1 : 0);
            PlayerPrefs.SetInt(KEY_COLORBLIND, colorblindAssistance ? 1 : 0);
            PlayerPrefs.SetInt(KEY_PHOTOSENSITIVE, photosensitivitySoftening ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ResetToDefaults()
        {
            masterVolume = 1.0f;
            musicVolume = 0.8f;
            sfxVolume = 0.85f;
            brightness = 0.0f;
            fieldOfView = 75f;
            cameraShake = 1.0f;
            subtitlesEnabled = true;
            invertY = false;
            colorblindAssistance = false;
            photosensitivitySoftening = false;

            SaveSettings();
            TriggerChange();
        }

        private void TriggerChange()
        {
            OnSettingsChanged?.Invoke();
        }

        private void OnApplicationQuit()
        {
            SaveSettings();
        }
    }
}
