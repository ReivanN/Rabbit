using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Toggles")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;

    private void Start()
    {
        LoadCurrentSettings();
        SetupCallbacks();
    }

    private void LoadCurrentSettings()
    {
        var settings = AudioManager.Instance.GetCurrentSettings();
        
        musicToggle.isOn = settings.musicEnabled;
        sfxToggle.isOn = settings.sfxEnabled;
        
        UpdateTexts();
    }

    private void SetupCallbacks()
    {
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);

        AudioManager.Instance.OnSettingsChanged += OnSettingsChanged;
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance.SetMasterVolume(value);
        UpdateTexts();
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
        UpdateTexts();
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
        UpdateTexts();
    }

    private void OnMusicToggleChanged(bool enabled)
    {
        AudioManager.Instance.SetMusicEnabled(enabled);
    }

    private void OnSFXToggleChanged(bool enabled)
    {
        AudioManager.Instance.SetSFXEnabled(enabled);
    }

    private void OnSettingsChanged(AudioSettings settings)
    {
        UpdateTexts();
    }

    private void UpdateTexts()
    {
        var settings = AudioManager.Instance.GetCurrentSettings();
    }

    private void OnDestroy()
    {
        AudioManager.Instance.OnSettingsChanged -= OnSettingsChanged;
    }
}