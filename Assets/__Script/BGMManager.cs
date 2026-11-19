using UnityEngine;

// Legacy entry point kept so existing scenes/scripts keep working.
// Music is now handled by GameAudio (region music + combat layer + bosses); this only forwards calls.
public class BGMManager : MonoBehaviour
{
    static BGMManager instance;
    public AudioClip clip;   // old track, no longer played

    void Awake()
    {
        if (!instance)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            AudioSource old = GetComponent<AudioSource>();
            if (old != null) { old.Stop(); old.enabled = false; }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void PlayBGM(bool reset, AudioClip clip = null)
    {
        GameAudio.PauseMusic(false);
    }

    public static void SetVolume(float volume)
    {
        GameAudio.SetMusicVolume(volume);
    }

    public static void PauseBGM()
    {
        GameAudio.PauseMusic(true);
    }
}
