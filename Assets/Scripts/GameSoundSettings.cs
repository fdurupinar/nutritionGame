using UnityEngine;

// Kept separate from game progress so starting a new game preserves sound preferences.
public static class GameSoundSettings
{
    private const string VolumeKey = "Settings_MasterVolume";
    private const string MutedKey = "Settings_Muted";

    public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
    public static bool Muted => PlayerPrefs.GetInt(MutedKey, 0) != 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Apply()
    {
        AudioListener.volume = Muted ? 0f : Volume;
    }

    public static void SetVolume(float value)
    {
        PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
        Apply();
    }

    public static void SetMuted(bool muted)
    {
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        Apply();
    }

    public static void ResetDefaults()
    {
        SetVolume(1f);
        SetMuted(false);
    }
}
