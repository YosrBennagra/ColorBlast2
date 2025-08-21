using UnityEngine;
using System;

/// <summary>
/// Lightweight audio helper that doesn't require compile-time dependency on AudioManager.
/// Uses PlayerPrefs for mute toggles and reflection to call AudioManager methods when present.
/// Falls back to AudioSource.PlayClipAtPoint for SFX.
/// </summary>
public static class AudioShim
{
    private const string MusicMuteKey = "Audio.MusicMuted";
    private const string SfxMuteKey = "Audio.SfxMuted";

    public static bool IsMusicMuted() => PlayerPrefs.GetInt(MusicMuteKey, 0) == 1;
    public static bool IsSfxMuted() => PlayerPrefs.GetInt(SfxMuteKey, 0) == 1;

    public static void ToggleMusicMuted()
    {
        SetMusicMuted(!IsMusicMuted());
        // Try to propagate to AudioManager if present
        InvokeOnAudioManager("SetMusicMuted", new object[] { IsMusicMuted() });
    }

    public static void ToggleSfxMuted()
    {
        SetSfxMuted(!IsSfxMuted());
        InvokeOnAudioManager("SetSfxMuted", new object[] { IsSfxMuted() });
    }

    public static void SetMusicMuted(bool muted)
    {
        PlayerPrefs.SetInt(MusicMuteKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetSfxMuted(bool muted)
    {
        PlayerPrefs.SetInt(SfxMuteKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void PlaySfxAt(Vector3 worldPos, AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || IsSfxMuted()) return;
        // If AudioManager exists, try to use its method
        if (!InvokeOnAudioManager("PlaySfxAt", new object[] { worldPos, clip, volumeScale }))
        {
            AudioSource.PlayClipAtPoint(clip, worldPos, Mathf.Clamp01(volumeScale));
        }
    }

    private static bool InvokeOnAudioManager(string method, object[] args)
    {
    var allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            var mb = allBehaviours[i];
            if (mb == null) continue;
            var t = mb.GetType();
            if (t.Name == "AudioManager")
            {
                var mi = t.GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (mi != null)
                {
                    mi.Invoke(mb, args);
                    return true;
                }
            }
        }
        return false;
    }
}
