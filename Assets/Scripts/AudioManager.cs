using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class AudioManager : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioSource musicSource;
    
    [Header("Settings")]
    [SerializeField] private AudioSettings currentSettings = new AudioSettings();

    public static AudioManager Instance { get; private set; }
    public event Action<AudioSettings> OnSettingsChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        PlayMusic();
        UpdateAudioSources();
    }

    #region Public Settings API
    
    public void SetMasterVolume(float volume)
    {
        currentSettings.masterVolume = Mathf.Clamp01(volume);
        UpdateAudioSources();
        SaveSettings();
        OnSettingsChanged?.Invoke(currentSettings);
    }

    public void SetMusicVolume(float volume)
    {
        currentSettings.musicVolume = Mathf.Clamp01(volume);
        UpdateAudioSources();
        SaveSettings();
        OnSettingsChanged?.Invoke(currentSettings);
    }

    public void SetSFXVolume(float volume)
    {
        currentSettings.sfxVolume = Mathf.Clamp01(volume);
        UpdateAudioSources();
        SaveSettings();
        OnSettingsChanged?.Invoke(currentSettings);
    }

    public void SetMusicEnabled(bool enabled)
    {
        currentSettings.musicEnabled = enabled;
        UpdateAudioSources();
        SaveSettings();
        OnSettingsChanged?.Invoke(currentSettings);
    }

    public void SetSFXEnabled(bool enabled)
    {
        currentSettings.sfxEnabled = enabled;
        UpdateAudioSources();
        SaveSettings();
        OnSettingsChanged?.Invoke(currentSettings);
    }

    public AudioSettings GetCurrentSettings() => currentSettings;

    #endregion

    #region Audio Control
    
    public void PlayMusic()
    {
        if (currentSettings.musicEnabled && musicSource.clip != null)
        {
            musicSource.volume = currentSettings.masterVolume * currentSettings.musicVolume;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip, AudioSource source, float volumeScale = 1f)
    {
        if (!currentSettings.sfxEnabled || clip == null || source == null) 
            return;

        float volume = currentSettings.masterVolume * currentSettings.sfxVolume * volumeScale;
        source.PlayOneShot(clip, volume);
    }

    public async UniTask PlaySFXAsync(AudioClip clip, AudioSource source, float volumeScale = 1f)
    {
        if (!currentSettings.sfxEnabled || clip == null || source == null) 
            return;

        float volume = currentSettings.masterVolume * currentSettings.sfxVolume * volumeScale;
        source.PlayOneShot(clip, volume);
        
        // Ждем завершения звука если нужно
        await UniTask.Delay((int)(clip.length * 1000));
    }

    #endregion

    private void UpdateAudioSources()
    {
        // Обновляем музыку
        if (musicSource != null)
        {
            musicSource.volume = currentSettings.musicEnabled ? 
                currentSettings.masterVolume * currentSettings.musicVolume : 0f;
        }
    }

    #region Save/Load
    
    private void SaveSettings()
    {
        string json = JsonUtility.ToJson(currentSettings);
        PlayerPrefs.SetString("AudioSettings", json);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("AudioSettings"))
        {
            string json = PlayerPrefs.GetString("AudioSettings");
            currentSettings = JsonUtility.FromJson<AudioSettings>(json);
        }
        UpdateAudioSources();
    }

    #endregion
}

