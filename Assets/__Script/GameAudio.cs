using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Central audio for "Kael — Sombras de Além". Created automatically before the first scene, survives scene loads.
// - Region music: exploration + combat layer started together (PlayScheduled) so they stay in sync;
//   the combat layer fades in while enemies are near the player.
// - Boss music, stingers (boss defeated, level complete, final victory, game over) and region ambience.
// - Sound effects by name with random variations: "SFX_Kael_Pulo" plays SFX_Kael_Pulo_01/_02 at random.
// Clips are loaded from Resources/Audio/Music and Resources/Audio/SFX.
// Volumes come from PlayerPrefs "MusicVolume" / "SoundVolume" (the options sliders).
public class GameAudio : MonoBehaviour
{
    public static GameAudio I { get; private set; }

    public const float DefaultMusicVolume = 0.7f;
    public const float DefaultSoundVolume = 0.8f;

    struct Region { public string explore, combat, ambience; }
    static readonly Dictionary<string, Region> Regions = new Dictionary<string, Region>
    {
        { "__Title",   new Region { explore = "MUS_TemaKael_Menu_loop", combat = null, ambience = "AMB_Templo_Ruinas_loop" } },
        { "__Main",    new Region { explore = "MUS_Templo_Exploracao_loop", combat = "MUS_Templo_CamadaCombate_loop", ambience = "AMB_Templo_Ruinas_loop" } },
        { "__Level01", new Region { explore = "MUS_Floresta_Exploracao_loop", combat = "MUS_Floresta_CamadaCombate_loop", ambience = "AMB_Floresta_loop" } },
        { "__Level02", new Region { explore = "MUS_Ruinas_Exploracao_loop", combat = "MUS_Ruinas_CamadaCombate_loop", ambience = "AMB_Ruinas_loop" } },
        { "__Level03", new Region { explore = "MUS_Trono_Exploracao_loop", combat = "MUS_Trono_CamadaCombate_loop", ambience = "AMB_Trono_loop" } },
    };
    static readonly string[] BossMusic = { "MUS_Chefe_GuardiaoFloresta_loop", "MUS_Chefe_CapitaoEspectral_loop", "MUS_Chefe_GuardiaoCaido_loop" };

    // old SoundFXManager names -> new effects
    static readonly Dictionary<string, string> Legacy = new Dictionary<string, string>
    {
        { "Jump", "SFX_Kael_Pulo" }, { "Hit", "SFX_Kael_Dano" }, { "Death", "SFX_Kael_Morte" },
        { "StepOn", "SFX_Espada_Impacto_Inimigo" }, { "Gem", "SFX_Coletar_Gema" },
        { "Falling", "SFX_Plataforma_Tremer" }, { "LevelProgress", "SFX_Porta_Entrar" },
    };

    AudioSource explore, combat, boss, ambience, stinger;
    readonly List<AudioSource> sfx = new List<AudioSource>();
    readonly Dictionary<string, AudioClip[]> cache = new Dictionary<string, AudioClip[]>();

    float musicVolume = DefaultMusicVolume, soundVolume = DefaultSoundVolume;
    float combatMix, combatTarget;      // 0..1
    float regionMix = 1f, bossMix;      // crossfade region <-> boss music
    bool bossActive, musicPaused;
    string currentRegion;
    float nextScan, nextPrefs;
    Transform player;

    public float SoundVolume => soundVolume;
    public float MusicVolume => musicVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (I != null) return;
        GameObject go = new GameObject("GameAudio");
        DontDestroyOnLoad(go);
        I = go.AddComponent<GameAudio>();
    }

    void Awake()
    {
        explore = NewSource("Music_Explore", true);
        combat = NewSource("Music_Combat", true);
        boss = NewSource("Music_Boss", true);
        ambience = NewSource("Ambience", true);
        stinger = NewSource("Stinger", false);
        for (int i = 0; i < 12; i++) sfx.Add(NewSource("Sfx" + i, false));
        ReadPrefs();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    AudioSource NewSource(string name, bool loop)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f;
        return s;
    }

    // ------------------------------------------------------------------ clips
    public AudioClip[] Clips(string name)
    {
        if (cache.TryGetValue(name, out AudioClip[] c)) return c;
        List<AudioClip> list = new List<AudioClip>();
        string folder = name.StartsWith("SFX_") ? "Audio/SFX/" : "Audio/Music/";
        AudioClip one = Resources.Load<AudioClip>(folder + name);
        if (one != null) list.Add(one);
        for (int i = 1; i <= 9; i++)
        {
            AudioClip v = Resources.Load<AudioClip>($"{folder}{name}_{i:00}");
            if (v == null) break;
            list.Add(v);
        }
        if (list.Count == 0) Debug.LogWarning($"GameAudio: clip '{name}' not found in Resources/{folder}");
        c = list.ToArray();
        cache[name] = c;
        return c;
    }

    AudioClip Pick(string name)
    {
        AudioClip[] c = Clips(name);
        return c.Length == 0 ? null : c[Random.Range(0, c.Length)];
    }

    // ------------------------------------------------------------------ effects (static helpers: safe to call anywhere)
    public static void Sfx(string name, float volume = 1f, float pitchVariation = 0.05f)
    {
        if (I != null) I.PlaySfx(name, volume, pitchVariation);
    }

    // positional-ish: louder when the source is near the player (2D game: simple distance falloff)
    public static void SfxAt(string name, Vector3 position, float volume = 1f, float range = 12f)
    {
        if (I == null) return;
        float k = I.Proximity(position, range);
        if (k > 0.02f) I.PlaySfx(name, volume * k, 0.05f);
    }

    public static void PlayLegacy(string oldName)
    {
        if (string.IsNullOrEmpty(oldName)) return;
        Sfx(Legacy.TryGetValue(oldName, out string n) ? n : oldName);
    }

    void PlaySfx(string name, float volume, float pitchVariation)
    {
        AudioClip clip = Pick(name);
        if (clip == null) return;
        AudioSource s = sfx[0];
        foreach (AudioSource a in sfx) { if (!a.isPlaying) { s = a; break; } }
        if (s.isPlaying) s = sfx[Random.Range(0, sfx.Count)];   // all busy: steal one
        s.clip = clip;
        s.volume = Mathf.Clamp01(volume * soundVolume * Random.Range(0.92f, 1f));
        s.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        s.Play();
    }

    public float Proximity(Vector3 position, float range)
    {
        Vector3 listener = player != null ? player.position
                         : Camera.main != null ? Camera.main.transform.position : position;
        float d = Vector2.Distance(position, listener);
        return Mathf.Clamp01(1f - d / range);
    }

    // ------------------------------------------------------------------ music flow
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = null;
        bossActive = false; bossMix = 0f; regionMix = 1f;
        combatMix = combatTarget = 0f;
        boss.Stop();
        musicPaused = false;
        if (Regions.TryGetValue(scene.name, out Region r)) StartCoroutine(StartRegion(scene.name, r));
    }

    IEnumerator StartRegion(string sceneName, Region r)
    {
        // a stinger crossing the scene change (e.g. final victory -> title) plays to the end first
        while (stinger.isPlaying && stinger.clip != null && stinger.clip.name == "MUS_Final_Vitoria") yield return null;

        if (currentRegion == sceneName && explore.isPlaying) yield break;   // same region already playing
        currentRegion = sceneName;
        explore.Stop(); combat.Stop(); ambience.Stop();
        explore.clip = Pick(r.explore);
        combat.clip = r.combat != null ? Pick(r.combat) : null;
        ambience.clip = r.ambience != null ? Pick(r.ambience) : null;
        double start = AudioSettings.dspTime + 0.15;
        if (explore.clip != null) explore.PlayScheduled(start);
        if (combat.clip != null) combat.PlayScheduled(start);          // same length -> stays in sync
        if (ambience.clip != null) ambience.Play();
    }

    public static void BossStart(int bossIndex)
    {
        if (I == null) return;
        I.bossActive = true;
        I.boss.clip = I.Pick(BossMusic[Mathf.Clamp(bossIndex, 0, BossMusic.Length - 1)]);
        if (I.boss.clip != null) I.boss.Play();
    }

    public static void BossDefeated()
    {
        if (I == null) return;
        I.bossActive = false;
        I.boss.Stop(); I.bossMix = 0f;
        I.PlayStinger("MUS_Chefe_Derrotado", true);
    }

    public static void LevelComplete(bool finalLevel)
    {
        if (I == null) return;
        I.PlayStinger(finalLevel ? "MUS_Final_Vitoria" : "MUS_Fase_Concluida", false);
        if (finalLevel) { I.explore.Stop(); I.combat.Stop(); I.boss.Stop(); I.currentRegion = null; }
    }

    // short musical cue over the music (music ducks while it plays)
    public static void Jingle(string name)
    {
        if (I != null) I.PlayStinger(name, true);
    }

    public static void GameOver()
    {
        if (I == null) return;
        I.explore.Stop(); I.combat.Stop(); I.boss.Stop(); I.ambience.Stop();
        I.currentRegion = null;
        I.PlayStinger("MUS_GameOver", false);
    }

    public static void PauseMusic(bool pause)
    {
        if (I == null) return;
        I.musicPaused = pause;
        foreach (AudioSource s in new[] { I.explore, I.combat, I.boss, I.ambience })
        {
            if (pause) s.Pause(); else s.UnPause();
        }
    }

    void PlayStinger(string name, bool duckMusic)
    {
        stinger.clip = Pick(name);
        if (stinger.clip == null) return;
        stinger.volume = musicVolume;
        stinger.Play();
        if (duckMusic) StartCoroutine(Duck(stinger.clip.length));
    }

    float duck = 1f;
    IEnumerator Duck(float seconds)
    {
        duck = 0.15f;
        yield return new WaitForSecondsRealtime(seconds * 0.85f);
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime) { duck = Mathf.Lerp(0.15f, 1f, t); yield return null; }
        duck = 1f;
    }

    public static void SetMusicVolume(float v) { if (I != null) I.musicVolume = Mathf.Clamp01(v); }
    public static void SetSoundVolume(float v) { if (I != null) I.soundVolume = Mathf.Clamp01(v); }

    void ReadPrefs()
    {
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", DefaultMusicVolume);
        soundVolume = PlayerPrefs.GetFloat("SoundVolume", DefaultSoundVolume);
    }

    // ------------------------------------------------------------------ per frame
    void Update()
    {
        if (Time.unscaledTime >= nextPrefs) { ReadPrefs(); nextPrefs = Time.unscaledTime + 0.25f; }

        if (player == null && Time.unscaledTime >= nextScan)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.3f;
            combatTarget = EnemiesNear(9f) ? 1f : 0f;
        }

        float dt = Time.unscaledDeltaTime;
        combatMix = Mathf.MoveTowards(combatMix, combatTarget, dt / (combatTarget > combatMix ? 1.0f : 2.5f));
        regionMix = Mathf.MoveTowards(regionMix, bossActive ? 0f : 1f, dt / 1.5f);
        bossMix = Mathf.MoveTowards(bossMix, bossActive ? 1f : 0f, dt / 1.0f);

        float m = musicVolume * duck;
        explore.volume = m * regionMix;
        combat.volume = m * regionMix * combatMix;
        boss.volume = m * bossMix;
        ambience.volume = musicVolume * 0.6f * regionMix;
        if (stinger.isPlaying) stinger.volume = musicVolume;
    }

    bool EnemiesNear(float range)
    {
        if (player == null) return false;
        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e.IsAlive && Vector2.Distance(e.transform.position, player.position) < range) return true;
        }
        return false;
    }
}
