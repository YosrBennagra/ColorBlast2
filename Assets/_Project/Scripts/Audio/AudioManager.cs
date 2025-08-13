using UnityEngine;

/// <summary>
/// Simple AudioManager.
/// Rules:
///  - First ever launch: music OFF (muted persisted in PlayerPrefs)
///  - When player turns music ON: start a RANDOM track (not always first)
///  - When music already ON and player toggles OFF: stop music
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")] 
    [SerializeField] private AudioClip[] backgroundMusicPlaylist; // assign in Inspector
    [Range(0f,1f)][SerializeField] private float musicVolume = 0.6f;

    [Header("SFX")] 
    [Range(0f,1f)][SerializeField] private float sfxVolume = 1f;

    private const string MusicMuteKey = "Audio.MusicMuted"; // 1 = muted, 0 = unmuted

    private AudioSource musicSource;
    private bool musicMuted;
    private System.Random rng = new System.Random();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = false; // we handle advancing manually
        musicSource.spatialBlend = 0f;

        // First launch default: muted (OFF)
        if (!PlayerPrefs.HasKey(MusicMuteKey))
        {
            PlayerPrefs.SetInt(MusicMuteKey, 1); // 1 = muted
            PlayerPrefs.Save();
        }
        musicMuted = PlayerPrefs.GetInt(MusicMuteKey, 1) == 1;
        ApplyMusicVolume();
    }

    private void Start()
    {
        // If user previously enabled music, resume with a random track
        if (!musicMuted)
        {
            PlayRandomTrack();
        }
    }

    private void Update()
    {
        // Simple loop: when a track finishes and music still ON, start another random one
        if (!musicMuted && backgroundMusicPlaylist != null && backgroundMusicPlaylist.Length > 0)
        {
            if (!musicSource.isPlaying && musicSource.clip != null) // finished current
            {
                PlayRandomTrack();
            }
        }
    }

    // UI toggle hook
    public void ToggleMusic()
    {
        SetMusicMuted(!musicMuted);
        if (!musicMuted)
        {
            PlayRandomTrack();
        }
        else
        {
            StopMusic();
        }
    }

    public void SetMusicMuted(bool muted)
    {
        musicMuted = muted;
        PlayerPrefs.SetInt(MusicMuteKey, musicMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    public bool IsMusicMuted() => musicMuted;

    private void ApplyMusicVolume()
    {
        if (musicSource != null)
            musicSource.volume = musicMuted ? 0f : musicVolume;
    }

    private void PlayRandomTrack()
    {
        if (backgroundMusicPlaylist == null || backgroundMusicPlaylist.Length == 0) return;
        int idx = rng.Next(backgroundMusicPlaylist.Length);
        var clip = backgroundMusicPlaylist[idx];
        if (clip == null) return;
        musicSource.Stop();
        musicSource.clip = clip;
        ApplyMusicVolume();
        if (!musicMuted) musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
}
