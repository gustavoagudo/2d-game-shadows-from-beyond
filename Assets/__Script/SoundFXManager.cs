using UnityEngine;
using UnityEngine.UI;

// Legacy entry point kept so existing scenes/scripts keep working.
// Effects are now played by GameAudio; old names ("Jump", "Hit", "Gem"...) are mapped to the new sounds.
public class SoundFXManager : MonoBehaviour
{
    static SoundFXManager instance;
    public Slider volumeSlider;

    void Awake()
    {
        if (!instance)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void Play(string clipname)
    {
        GameAudio.PlayLegacy(clipname);
    }

    public static void SetVolume(float volume)
    {
        GameAudio.SetSoundVolume(volume);
    }
}
