using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EndlessHallway.Core;

namespace EndlessHallway.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Root Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Audio Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI masterValText;
        [SerializeField] private TextMeshProUGUI musicValText;
        [SerializeField] private TextMeshProUGUI sfxValText;

        [Header("Display Sliders")]
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Slider fovSlider;
        [SerializeField] private TextMeshProUGUI brightnessValText;
        [SerializeField] private TextMeshProUGUI fovValText;

        [Header("Gameplay Sliders & Toggles")]
        [SerializeField] private Slider shakeSlider;
        [SerializeField] private TextMeshProUGUI shakeValText;
        [SerializeField] private Toggle invertYToggle;

        [Header("Accessibility Toggles")]
        [SerializeField] private Toggle subtitlesToggle;
        [SerializeField] private Toggle photosensitiveToggle;
        [SerializeField] private Toggle colorblindToggle;

        [Header("Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetDefaultsButton;

        private Action onClosedCallback;
        private bool isUpdatingUI = false;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;

            HookListeners();
        }

        private void HookListeners()
        {
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);

            if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(OnBrightnessSliderChanged);
            if (fovSlider != null) fovSlider.onValueChanged.AddListener(OnFOVSliderChanged);
            if (shakeSlider != null) shakeSlider.onValueChanged.AddListener(OnShakeSliderChanged);

            if (invertYToggle != null) invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
            if (subtitlesToggle != null) subtitlesToggle.onValueChanged.AddListener(OnSubtitlesChanged);
            if (photosensitiveToggle != null) photosensitiveToggle.onValueChanged.AddListener(OnPhotosensitiveChanged);
            if (colorblindToggle != null) colorblindToggle.onValueChanged.AddListener(OnColorblindChanged);

            if (backButton != null) backButton.onClick.AddListener(Close);
            if (resetDefaultsButton != null) resetDefaultsButton.onClick.AddListener(OnResetDefaultsClicked);
        }

        public void Open(Action onClose = null)
        {
            onClosedCallback = onClose;
            panelRoot.SetActive(true);
            RefreshUIValues();
        }

        public void Close()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.SaveSettings();
            }

            panelRoot.SetActive(false);
            var cb = onClosedCallback;
            onClosedCallback = null;
            cb?.Invoke();
        }

        public void RefreshUIValues()
        {
            if (SettingsManager.Instance == null) return;
            isUpdatingUI = true;

            var s = SettingsManager.Instance;

            if (masterSlider != null) { masterSlider.value = s.MasterVolume; UpdateLabel(masterValText, $"{Mathf.RoundToInt(s.MasterVolume * 100)}%"); }
            if (musicSlider != null) { musicSlider.value = s.MusicVolume; UpdateLabel(musicValText, $"{Mathf.RoundToInt(s.MusicVolume * 100)}%"); }
            if (sfxSlider != null) { sfxSlider.value = s.SFXVolume; UpdateLabel(sfxValText, $"{Mathf.RoundToInt(s.SFXVolume * 100)}%"); }

            if (brightnessSlider != null) { brightnessSlider.value = s.Brightness; UpdateLabel(brightnessValText, $"{s.Brightness:F1}"); }
            if (fovSlider != null) { fovSlider.value = s.FieldOfView; UpdateLabel(fovValText, $"{Mathf.RoundToInt(s.FieldOfView)}°"); }
            if (shakeSlider != null) { shakeSlider.value = s.CameraShake; UpdateLabel(shakeValText, s.CameraShake <= 0.001f ? "OFF" : $"{Mathf.RoundToInt(s.CameraShake * 100)}%"); }

            if (invertYToggle != null) invertYToggle.isOn = s.InvertY;
            if (subtitlesToggle != null) subtitlesToggle.isOn = s.SubtitlesEnabled;
            if (photosensitiveToggle != null) photosensitiveToggle.isOn = s.PhotosensitivitySoftening;
            if (colorblindToggle != null) colorblindToggle.isOn = s.ColorblindAssistance;

            isUpdatingUI = false;
        }

        private void UpdateLabel(TextMeshProUGUI label, string text)
        {
            if (label != null) label.text = text;
        }

        private void OnMasterSliderChanged(float val)
        {
            if (isUpdatingUI || SettingsManager.Instance == null) return;
            SettingsManager.Instance.MasterVolume = val;
            UpdateLabel(masterValText, $"{Mathf.RoundToInt(val * 100)}%");
        }

        private void OnMusicSliderChanged(float val)
        {
            if (isUpdatingUI || SettingsManager.Instance == null) return;
            SettingsManager.Instance.MusicVolume = val;
            UpdateLabel(musicValText, $"{Mathf.RoundToInt(val * 100)}%");
        }

        private void OnSFXSliderChanged(float val)
        {
            if (isUpdatingUI || SettingsManager.Instance == null) return;
            SettingsManager.Instance.SFXVolume = val;
            UpdateLabel(sfxValText, $"{Mathf.RoundToInt(val * 100)}%");
        }

        private void OnBrightnessSliderChanged(float val)
        {
            if (isUpdatingUI || SettingsManager.Instance == null) return;
            SettingsManager.Instance.Brightness = val;
            UpdateLabel(brightnessValText, $"{val:F1}");
        }

        private void OnFOVSliderChanged(float val)
        {
            if (isUpdatingUI || SettingsManager.Instance == null) return;
            SettingsManager.Instance.FieldOfView = val;
            UpdateLabel(fovValText, $"{Mathf.RoundToInt(val)}°");
        }

        private void OnShakeSliderChanged(float val)
        {
            if (isUpdatingUI || SettingsManager.Instance == null) return;
            SettingsManager.Instance.CameraShake = val;
            UpdateLabel(shakeValText, val <= 0.001f ? "OFF" : $"{Mathf.RoundToInt(val * 100)}%");
        }

        private void OnInvertYChanged(bool val)
        {
            if (isUpdatingUI || SettingsManager.Instance == null) return;
            SettingsManager.Instance.InvertY = val;
        }

        private void OnSubtitlesChanged(bool val)
        {
            if (isUpdatingUI || SettingsManager.Instance == null) return;
            SettingsManager.Instance.SubtitlesEnabled = val;
        }

        private void OnPhotosensitiveChanged(bool val)
        {
            if (isUpdatingUI || SettingsManager.Instance == null) return;
            SettingsManager.Instance.PhotosensitivitySoftening = val;
        }

        private void OnColorblindChanged(bool val)
        {
            if (isUpdatingUI || SettingsManager.Instance == null) return;
            SettingsManager.Instance.ColorblindAssistance = val;
        }

        private void OnResetDefaultsClicked()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ResetToDefaults();
                RefreshUIValues();
            }
        }
    }
}
