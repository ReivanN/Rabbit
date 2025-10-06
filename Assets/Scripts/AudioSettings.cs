using System;

[Serializable]
public class AudioSettings
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public bool musicEnabled = true;
    public bool sfxEnabled = true;
}