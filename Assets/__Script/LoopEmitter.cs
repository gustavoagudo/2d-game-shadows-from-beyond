using UnityEngine;

// A looping world sound (saw, moving platform, enemy wings/steps...). Volume falls off with the distance
// to the player, follows the "SoundVolume" setting, and pauses with the game.
public class LoopEmitter : MonoBehaviour
{
    AudioSource source;
    float baseVolume = 1f;
    float range = 10f;

    public static LoopEmitter Attach(GameObject target, string clipName, float volume, float range)
    {
        if (GameAudio.I == null || target == null) return null;
        AudioClip[] clips = GameAudio.I.Clips(clipName);
        if (clips.Length == 0) return null;
        LoopEmitter e = target.AddComponent<LoopEmitter>();
        e.baseVolume = volume;
        e.range = range;
        e.source = target.AddComponent<AudioSource>();
        e.source.clip = clips[0];
        e.source.loop = true;
        e.source.playOnAwake = false;
        e.source.spatialBlend = 0f;
        e.source.volume = 0f;
        e.source.time = Random.Range(0f, clips[0].length * 0.9f);   // de-sync identical emitters
        e.source.Play();
        return e;
    }

    void Update()
    {
        if (source == null || GameAudio.I == null) return;
        bool paused = Time.timeScale == 0f;
        if (paused && source.isPlaying) source.Pause();
        else if (!paused && !source.isPlaying) source.UnPause();
        source.volume = baseVolume * GameAudio.I.SoundVolume * GameAudio.I.Proximity(transform.position, range);
    }

    public void Stop()
    {
        if (source != null) source.Stop();
        enabled = false;
    }
}
