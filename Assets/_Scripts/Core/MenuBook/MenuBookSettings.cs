
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;


public class MenuBookSettings : MonoBehaviour
{
    [Header("Controls:")]
    [SerializeField] private Slider sensitivitySlider = null;

    [Header("Audio:")]
    [SerializeField] private Slider musicVolumeSlider = null;
    [SerializeField] private Slider sfxVolumeSlider = null;
    [SerializeField] private Slider ambianceVolumeSlider = null;
    [SerializeField] private AudioMixer musicMixer = null;
    [SerializeField] private AudioMixer sfxMixer = null;
    [SerializeField] private AudioMixer ambianceMixer = null;

    [Header("Display:")]
    [SerializeField] private Slider frameRateSlider = null;
    [SerializeField] private Toggle vSyncToggle = null;
    [SerializeField] private Toggle fullScreenToggle = null;
    [SerializeField] private TMP_Dropdown screenSizeDropdown = null;

    private TextMeshProUGUI sensitivityValueText = null;
    private TextMeshProUGUI musicVolumeValueText = null;
    private TextMeshProUGUI sfxVolumeValueText = null;
    private TextMeshProUGUI ambianceVolumeValueText = null;
    private TextMeshProUGUI frameRateValueText = null;

    private Action onDestroy = null;


    private void Awake()
    {
        #region Controls ========================================================================== Controls

        sensitivityValueText = sensitivitySlider.transform.Find("Value").GetComponent<TextMeshProUGUI>();
        

        sensitivitySlider.minValue = 1;
        sensitivitySlider.maxValue = 100;
        sensitivitySlider.wholeNumbers = true;
        GameSettings.Controls.Sensitivity.SubscribeUntilDestroyed(sensitivity =>
        {
            int sensitivityPercent = Mathf.RoundToInt(sensitivity * sensitivitySlider.maxValue);
            sensitivitySlider.SetValueWithoutNotify(sensitivityPercent);
            sensitivityValueText.text = $"{sensitivityPercent}";
        }, ref onDestroy, initialize: true);

        void OnSensitivitySliderChanged(float sensitivityPercent)
            => GameSettings.Controls.Sensitivity.Set(sensitivityPercent / sensitivitySlider.maxValue);
        sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);

        #endregion Controls


        #region Audio ============================================================================= Audio

        musicVolumeValueText = musicVolumeSlider.transform.Find("Value").GetComponent<TextMeshProUGUI>();
        sfxVolumeValueText = sfxVolumeSlider.transform.Find("Value").GetComponent<TextMeshProUGUI>();
        ambianceVolumeValueText = ambianceVolumeSlider.transform.Find("Value").GetComponent<TextMeshProUGUI>();


        musicVolumeSlider.minValue = 0;
        musicVolumeSlider.maxValue = 100;
        musicVolumeSlider.wholeNumbers = true;
        GameSettings.Audio.MusicVolume.SubscribeUntilDestroyed(volume =>
        {
            int musicVolumePercent = Mathf.RoundToInt(volume * musicVolumeSlider.maxValue);
            musicVolumeSlider.SetValueWithoutNotify(musicVolumePercent);
            musicVolumeValueText.text = $"{musicVolumePercent}";
            float level = volume <= 0f ? -80f : Mathf.Log10(volume) * 20f;
            musicMixer.SetFloat("MasterVolume", level);
        }, ref onDestroy, initialize: true);

        void OnMusicVolumeSliderChanged(float volumePercent)
            => GameSettings.Audio.MusicVolume.Set(volumePercent / musicVolumeSlider.maxValue);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);


        sfxVolumeSlider.minValue = 0;
        sfxVolumeSlider.maxValue = 100;
        sfxVolumeSlider.wholeNumbers = true;
        GameSettings.Audio.SFXVolume.SubscribeUntilDestroyed(volume =>
        {
            int sfxVolumePercent = Mathf.RoundToInt(volume * sfxVolumeSlider.maxValue);
            sfxVolumeSlider.SetValueWithoutNotify(sfxVolumePercent);
            sfxVolumeValueText.text = $"{sfxVolumePercent}";
            float level = volume <= 0f ? -80f : Mathf.Log10(volume) * 20f;
            sfxMixer.SetFloat("MasterVolume", level);
        }, ref onDestroy, initialize: true);

        void OnSFXVolumeSliderChanged(float volumePercent)
            => GameSettings.Audio.SFXVolume.Set(volumePercent / sfxVolumeSlider.maxValue);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeSliderChanged);


        ambianceVolumeSlider.minValue = 0;
        ambianceVolumeSlider.maxValue = 100;
        ambianceVolumeSlider.wholeNumbers = true;
        GameSettings.Audio.AmbianceVolume.SubscribeUntilDestroyed(volume =>
        {
            int ambianceVolumePercent = Mathf.RoundToInt(volume * ambianceVolumeSlider.maxValue);
            ambianceVolumeSlider.SetValueWithoutNotify(ambianceVolumePercent);
            ambianceVolumeValueText.text = $"{ambianceVolumePercent}";
            float level = volume <= 0f ? -80f : Mathf.Log10(volume) * 20f;
            ambianceMixer.SetFloat("MasterVolume", level);
        }, ref onDestroy, initialize: true);

        void OnAmbianceVolumeSliderChanged(float volumePercent)
            => GameSettings.Audio.AmbianceVolume.Set(volumePercent / ambianceVolumeSlider.maxValue);
        ambianceVolumeSlider.onValueChanged.AddListener(OnAmbianceVolumeSliderChanged);

        #endregion Audio


        #region Display =========================================================================== Display

        frameRateValueText = frameRateSlider.transform.Find("Value").GetComponent<TextMeshProUGUI>();


        frameRateSlider.minValue = GameSettings.Display.FrameRate.Min;
        frameRateSlider.maxValue = GameSettings.Display.FrameRate.Max;
        frameRateSlider.wholeNumbers = true;
        GameSettings.Display.FrameRate.SubscribeUntilDestroyed(frameRate =>
        {
            frameRateSlider.SetValueWithoutNotify(frameRate);
            frameRateValueText.text = $"{frameRate}";
        }, ref onDestroy, initialize: true);

        void OnFrameRateSliderChanged(float frameRate) =>
            GameSettings.Display.FrameRate.Set(Mathf.RoundToInt(frameRate));
        frameRateSlider.onValueChanged.AddListener(OnFrameRateSliderChanged);


        GameSettings.Display.VSync.SubscribeUntilDestroyed(vSyncOn =>
        vSyncToggle.SetIsOnWithoutNotify(vSyncOn), ref onDestroy, initialize: true);

        void OnVSyncToggleChanged(bool vSyncOn) =>
            GameSettings.Display.VSync.Set(vSyncOn);
        vSyncToggle.onValueChanged.AddListener(OnVSyncToggleChanged);


        GameSettings.Display.DisplayMode.SubscribeUntilDestroyed(modeName =>
        {
            bool isFullScreen = modeName == "Borderless windowed";
            fullScreenToggle.SetIsOnWithoutNotify(isFullScreen);
        }, ref onDestroy, initialize: true);

        void OnFullScreenToggleChanged(bool isFullScreen) =>
            GameSettings.Display.DisplayMode.SetByIndex(isFullScreen ? 0 : 1);
        fullScreenToggle.onValueChanged.AddListener(OnFullScreenToggleChanged);


        GameSettings.Display.Resolution.SubscribeUntilDestroyed(resolution =>
        OnResolutionsChanged(GameSettings.Display.Resolution.AllowedValues), ref onDestroy, initialize: true);
        GameSettings.Display.Resolution.OnAllowedValuesChanged.Always += OnResolutionsChanged;

        void OnScreenSizeDropdownChanged(int index) =>
            GameSettings.Display.Resolution.SetByIndex(index);
        screenSizeDropdown.onValueChanged.AddListener(OnScreenSizeDropdownChanged);

        #endregion Display
    }

    protected virtual void OnDestroy()
    {
        GameSettings.Display.Resolution.OnAllowedValuesChanged.Always -= OnResolutionsChanged;

        onDestroy?.Invoke();
    }

    private void OnResolutionsChanged(List<Vector2Int> resolutions)
    {
        Vector2Int currentResolution = GameSettings.Display.Resolution.Get();
        int optionIndex = 0;
        bool optionFound = false;
        List<TMP_Dropdown.OptionData> options = new();

        for (int i = 0; i < resolutions.Count; i++)
        {
            Vector2Int resolution = resolutions[i];

            if (resolution == currentResolution)
            {
                optionIndex = i;
                optionFound = true;
            }

            options.Add(new($"{resolution.x} x {resolution.y}"));
        }

        if (!optionFound)
        { options.Prepend(new($"{currentResolution.x} x {currentResolution.y}")); }

        screenSizeDropdown.ClearOptions();
        screenSizeDropdown.options = options;
        screenSizeDropdown.SetValueWithoutNotify(optionFound ? optionIndex : 0);
    }
}
