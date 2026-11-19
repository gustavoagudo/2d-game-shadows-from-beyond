using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Data-driven boss. Sprites are loaded from Resources/Bosses/<id>/<animation>/ (frames sorted by name).
// The boss stands on a flat arena floor (floorY), wakes up when the player passes activateX,
// locks the arena, hides the level door until it is defeated, and shows a health bar.
[DefaultExecutionOrder(20000)]
public class BossController : MonoBehaviour, IHittable
{
    public enum BossType { GuardiaoFloresta, CapitaoEspectral, GuardiaoCaido }

    [SerializeField] BossType bossType;
    [SerializeField] string displayName = "Chefe";
    [SerializeField] int maxHealth = 20;
    [SerializeField] int score = 50;
    [SerializeField] float arenaMinX = 0f;
    [SerializeField] float arenaMaxX = 20f;
    [SerializeField] float floorY = 0f;          // world Y of the floor surface (top of the ground tiles)
    [SerializeField] float activateX = 2f;       // player X that starts the fight
    [SerializeField] float moveSpeed = 2.5f;

    const float PPU = 40f;                       // boss/fx sprites: same pixel density as Kael
    string id;
    readonly Dictionary<string, Sprite[]> anims = new Dictionary<string, Sprite[]>();

    SpriteRenderer sr;
    BoxCollider2D hurtCollider;
    Transform player;
    Door door;
    GameObject arenaWall;
    int playerMask;

    int health;
    int facing = -1;                 // art faces right; flipX when facing left
    bool active, dead, invulnerable, attacking;
    float hitFlash;

    // animation state
    Sprite[] curFrames;
    float curFps = 10f, curTime;
    bool curLoop = true;

    // per-boss body box (units, relative to feet/pivot, for facing right)
    Vector2 bodyOffset, bodySize;

    // UI
    GameObject barRoot;
    RectTransform barFill;

    // ------------------------------------------------------------------ setup
    void Awake()
    {
        id = bossType == BossType.GuardiaoFloresta ? "guardiao_floresta"
           : bossType == BossType.CapitaoEspectral ? "capitao_espectral" : "guardiao_caido";

        switch (bossType)
        {
            case BossType.GuardiaoFloresta: bodyOffset = new Vector2(0.3f, 1.9f); bodySize = new Vector2(4.0f, 3.4f); break;
            case BossType.CapitaoEspectral: bodyOffset = new Vector2(0.1f, 2.0f); bodySize = new Vector2(2.0f, 3.9f); break;
            default:                        bodyOffset = new Vector2(0.0f, 1.6f); bodySize = new Vector2(1.8f, 3.2f); break;
        }

        GameObject spriteGo = new GameObject("Sprite");
        spriteGo.transform.SetParent(transform, false);
        sr = spriteGo.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -1;            // behind the player so Kael stays readable

        gameObject.layer = LayerMask.NameToLayer("Enemy");
        Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        hurtCollider = gameObject.AddComponent<BoxCollider2D>();
        hurtCollider.isTrigger = true;
        hurtCollider.offset = bodyOffset;
        hurtCollider.size = bodySize;

        playerMask = LayerMask.GetMask("Player");
        health = maxHealth;
        transform.position = new Vector3(transform.position.x, floorY, transform.position.z);
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        door = FindFirstObjectByType<Door>();
        if (door != null) door.gameObject.SetActive(false);   // the exit only appears after the boss

        Play("idle", 8f, true);
    }

    Sprite[] Frames(string anim)
    {
        if (anims.TryGetValue(anim, out Sprite[] f)) return f;
        f = Resources.LoadAll<Sprite>($"Bosses/{id}/{anim}");
        System.Array.Sort(f, (a, b) => string.CompareOrdinal(a.name, b.name));
        if (f.Length == 0) Debug.LogWarning($"Boss {id}: no frames for '{anim}'");
        anims[anim] = f;
        return f;
    }

    // ------------------------------------------------------------------ animation
    void Play(string anim, float fps, bool loop)
    {
        curFrames = Frames(anim);
        curFps = fps; curLoop = loop; curTime = 0f;
        ApplyFrame();
    }

    float Length(string anim, float fps) => Frames(anim).Length / fps;

    IEnumerator PlayOnce(string anim, float fps = 12f)
    {
        Play(anim, fps, false);
        yield return new WaitForSeconds(Length(anim, fps));
    }

    void ApplyFrame()
    {
        if (curFrames == null || curFrames.Length == 0) return;
        int i = Mathf.FloorToInt(curTime * curFps);
        i = curLoop ? i % curFrames.Length : Mathf.Min(i, curFrames.Length - 1);
        sr.sprite = curFrames[i];
    }

    void Update()
    {
        curTime += Time.deltaTime;
        ApplyFrame();
        sr.flipX = facing < 0;
        hurtCollider.offset = new Vector2(bodyOffset.x * facing, bodyOffset.y);

        if (hitFlash > 0f)
        {
            hitFlash -= Time.deltaTime;
            sr.color = hitFlash > 0f ? new Color(1f, 0.55f, 0.65f) : Color.white;
        }

        if (!active && !dead && player != null && GameManager.isGameOn && player.position.x >= activateX)
        {
            StartCoroutine(Fight());
        }

        // touching the boss hurts (player has its own invincibility window); a staggered boss is harmless
        if (active && !dead && !staggered && GameManager.isGameOn &&
            Physics2D.OverlapBox(BodyCenter, bodySize * 0.85f, 0f, playerMask))
        {
            HurtPlayer(BodyCenter, bodySize * 0.85f);
        }
    }

    // Damage goes through the enemy channel so the player can parry it.
    // The reported position is the point of the hitbox closest to the player (used for the parry range).
    void HurtPlayer(Vector2 boxCenter, Vector2 boxSize)
    {
        Vector2 p = player != null ? (Vector2)player.position : boxCenter;
        Vector2 half = boxSize * 0.5f;
        Vector2 closest = new Vector2(Mathf.Clamp(p.x, boxCenter.x - half.x, boxCenter.x + half.x),
                                      Mathf.Clamp(p.y, boxCenter.y - half.y, boxCenter.y + half.y));
        Enemy.OnPlayerHit?.Invoke(1, closest, false);
    }

    // ------------------------------------------------------------------ parry → stagger
    [Header("--- Parry ---")]
    [SerializeField] float staggerTime = 1.1f;
    bool staggered;
    int attackIndex;

    void OnEnable() { PlayerHealth.OnParry += OnPlayerParry; }
    void OnDisable() { PlayerHealth.OnParry -= OnPlayerParry; }

    void OnPlayerParry(Vector3 hitPosition)
    {
        if (!active || dead || staggered) return;
        // only parries of this boss's own body/weapon (not a far projectile) interrupt it
        Vector2 half = bodySize * 0.5f + new Vector2(3f, 1.5f);
        Vector2 d = (Vector2)hitPosition - BodyCenter;
        if (Mathf.Abs(d.x) > half.x || Mathf.Abs(d.y) > half.y) return;
        StopAllCoroutines();
        attacking = false;
        invulnerable = false;
        attackIndex++;                       // the interrupted attack does not repeat right away
        StartCoroutine(Stagger());
    }

    IEnumerator Stagger()
    {
        staggered = true;
        transform.position = new Vector3(transform.position.x, floorY, transform.position.z);  // e.g. parried mid-jump
        hitFlash = 0.2f;
        CameraShake(0.2f);
        GameAudio.Sfx("SFX_Chefe_Atordoado");
        // knocked back a little, away from the player
        float push = 0f;
        Play("hit", 8f, false);
        while (push < 0.25f)
        {
            MoveX(-facing * 6f * Time.deltaTime);
            push += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(staggerTime - 0.25f);
        staggered = false;
        Play("idle", 8f, true);
        yield return new WaitForSeconds(0.35f);
        yield return FightLoop();
    }

    Vector2 BodyCenter => (Vector2)transform.position + new Vector2(bodyOffset.x * facing, bodyOffset.y);
    float PlayerX => player != null ? player.position.x : transform.position.x;
    void FacePlayer() { facing = PlayerX < transform.position.x ? -1 : 1; }

    // ------------------------------------------------------------------ fight loop
    IEnumerator Fight()
    {
        active = true;
        LockArena(true);
        GameAudio.BossStart((int)bossType);
        GameAudio.Sfx("SFX_Arena_Fechar");
        Snd(bossType == BossType.GuardiaoFloresta ? "SFX_Floresta_Rugido"
          : bossType == BossType.CapitaoEspectral ? "SFX_Capitao_Aparicao" : "SFX_Caido_Aparicao");
        BuildHealthBar();
        FacePlayer();
        yield return PlayOnce(TelegraphName(0), 6f);   // short "wake up" pose
        Play("idle", 8f, true);
        yield return new WaitForSeconds(0.6f);
        yield return FightLoop();
    }

    IEnumerator FightLoop()
    {
        while (!dead)
        {
            if (!GameManager.isGameOn) { Play("idle", 8f, true); yield return null; continue; }

            attacking = true;
            switch (bossType)
            {
                case BossType.GuardiaoFloresta:
                    yield return attackIndex % 2 == 0 ? Investida() : Raizes(); break;
                case BossType.CapitaoEspectral:
                    yield return attackIndex % 3 == 0 ? Estocada() : attackIndex % 3 == 1 ? SaltoPancada() : OndaSombra(); break;
                default:
                    yield return attackIndex % 3 == 0 ? Combo() : attackIndex % 3 == 1 ? Projeteis() : Teleporte(); break;
            }
            attacking = false;
            if (dead) yield break;

            // recovery: the window to punish the boss
            Play("idle", 8f, true);
            float rest = Mathf.Lerp(1.3f, 0.7f, 1f - (float)health / maxHealth);
            yield return new WaitForSeconds(rest);
            attackIndex++;
        }
    }

    string TelegraphName(int i)
    {
        switch (bossType)
        {
            case BossType.GuardiaoFloresta: return "telegraph_investida";
            case BossType.CapitaoEspectral: return "telegraph_estocada";
            default: return "telegraph_combo";
        }
    }

    // walk toward the player until within `range`, max `maxTime` seconds
    IEnumerator Approach(float range, float maxTime)
    {
        Play("move", 12f, true);
        float t = 0f;
        while (!dead && t < maxTime && Mathf.Abs(PlayerX - transform.position.x) > range)
        {
            FacePlayer();
            MoveX(facing * moveSpeed * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }
    }

    void MoveX(float dx)
    {
        float half = bodySize.x * 0.5f;
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x + dx, arenaMinX + half, arenaMaxX - half);
        transform.position = p;
    }

    IEnumerator Telegraph(string anim, float extraHold)
    {
        FacePlayer();
        switch (anim)
        {
            case "telegraph_investida": Snd("SFX_Floresta_Investida_Aviso"); break;
            case "telegraph_raizes": Snd("SFX_Floresta_Raizes_Aviso"); break;
            case "telegraph_estocada": Snd("SFX_Capitao_Estocada_Aviso"); break;
            case "telegraph_projeteis": Snd("SFX_Caido_Projeteis_Carga"); break;
        }
        yield return PlayOnce(anim, 10f);
        yield return new WaitForSeconds(extraHold);
    }

    // melee hitbox active between t0..t1 (seconds into the attack animation)
    IEnumerator MeleeWindow(float t0, float t1, Vector2 offset, Vector2 size)
    {
        yield return new WaitForSeconds(t0);
        float t = t0;
        while (t < t1 && !dead)
        {
            Vector2 c = (Vector2)transform.position + new Vector2(offset.x * facing, offset.y);
            if (Physics2D.OverlapBox(c, size, 0f, playerMask))
            {
                HurtPlayer(c, size);
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }
    }

    string SoundPrefix => bossType == BossType.GuardiaoFloresta ? "SFX_Floresta"
                        : bossType == BossType.CapitaoEspectral ? "SFX_Capitao" : "SFX_Caido";
    void Snd(string name, float volume = 1f) => GameAudio.SfxAt(name, transform.position + Vector3.up * 1.5f, volume, 22f);

    BossFx Fx(string name, Vector3 pos, float fps, bool loop, float lifetime = 0f, bool flip = false)
        => BossFx.Spawn(Frames("fx/" + name), fps, loop, pos, flip, lifetime);

    // ---------------- Guardião da Floresta
    IEnumerator Investida()
    {
        yield return Telegraph("telegraph_investida", 0.35f);
        Play("attack_investida", 12f, true);
        Snd("SFX_Floresta_Investida");
        float t = 0f;
        float speed = 11f;
        while (!dead && t < 1.6f)
        {
            float before = transform.position.x;
            MoveX(facing * speed * Time.deltaTime);
            if (Physics2D.OverlapBox(BodyCenter, bodySize, 0f, playerMask))
                HurtPlayer(BodyCenter, bodySize);
            if (dead || staggered) yield break;
            if (Mathf.Approximately(before, transform.position.x)) break;   // hit the arena wall
            t += Time.deltaTime;
            yield return null;
        }
        Fx("impacto", new Vector3(transform.position.x + facing * bodySize.x * 0.5f, floorY), 12f, false);
        Snd("SFX_Floresta_Impacto_Parede");
        CameraShake(0.25f);
    }

    IEnumerator Raizes()
    {
        yield return Telegraph("telegraph_raizes", 0.2f);
        Play("attack_raizes", 12f, false);
        yield return new WaitForSeconds(2f / 12f);
        CameraShake(0.2f);
        // roots erupt under the player and to both sides; the first frame is the warning
        float px = PlayerX;
        float[] xs = { px, px - 2.6f, px + 2.6f };
        foreach (float x in xs)
        {
            if (x < arenaMinX + 0.5f || x > arenaMaxX - 0.5f) continue;
            StartCoroutine(Root(x));
        }
        yield return new WaitForSeconds(Length("attack_raizes", 12f) - 2f / 12f);
    }

    IEnumerator Root(float x)
    {
        Sprite[] f = Frames("fx/raiz");
        // warning: tip only, no damage
        BossFx.Spawn(new[] { f[0] }, 1f, true, new Vector3(x, floorY), false, 0.55f);
        yield return new WaitForSeconds(0.55f);
        GameAudio.SfxAt("SFX_Floresta_Raizes", new Vector3(x, floorY), 0.9f, 22f);
        Fx("raiz", new Vector3(x, floorY), 12f, false).Damage(new Vector2(0, 0.8f), new Vector2(0.8f, 1.5f), 1f / 12f, 4f / 12f);
    }

    // ---------------- Capitão Espectral
    IEnumerator Estocada()
    {
        yield return Approach(3.5f, 2.5f);
        yield return Telegraph("telegraph_estocada", 0.35f);
        Play("attack_estocada", 12f, false);
        Snd("SFX_Capitao_Estocada");
        StartCoroutine(MeleeWindow(1f / 12f, 4f / 12f, new Vector2(2.2f, 2.4f), new Vector2(2.2f, 1.0f)));
        // short lunge forward
        float t = 0f;
        while (t < 3f / 12f) { MoveX(facing * 6f * Time.deltaTime); t += Time.deltaTime; yield return null; }
        yield return new WaitForSeconds(Length("attack_estocada", 12f) - t);
    }

    IEnumerator SaltoPancada()
    {
        yield return Telegraph("telegraph_salto", 0.25f);
        float startX = transform.position.x;
        float targetX = Mathf.Clamp(PlayerX, arenaMinX + bodySize.x * 0.5f, arenaMaxX - bodySize.x * 0.5f);
        FacePlayer();
        Play("attack_salto_pancada", 12f, false);
        Snd("SFX_Capitao_Salto");
        float dur = 0.6f, t = 0f, height = 4f;
        while (t < dur && !dead)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            curTime = Mathf.Min(curTime, 1.5f / 12f);            // hold the airborne frames
            transform.position = new Vector3(Mathf.Lerp(startX, targetX, k), floorY + 4f * height * k * (1f - k), 0f);
            yield return null;
        }
        transform.position = new Vector3(targetX, floorY, 0f);
        curTime = 2f / 12f;                                       // slam frame
        Snd("SFX_Capitao_Pancada");
        CameraShake(0.35f);
        Fx("impacto", new Vector3(targetX - 1.2f, floorY), 12f, false);
        Fx("impacto", new Vector3(targetX + 1.2f, floorY), 12f, false);
        StartCoroutine(MeleeWindow(0f, 0.15f, new Vector2(0f, 0.6f), new Vector2(4.5f, 1.2f)));
        yield return new WaitForSeconds(5f / 12f);
    }

    IEnumerator OndaSombra()
    {
        yield return Telegraph("telegraph_onda", 0.3f);
        Play("attack_onda_sombra", 12f, false);
        yield return new WaitForSeconds(3f / 12f);
        Vector3 start = new Vector3(transform.position.x + facing * 1.6f, floorY);
        Snd("SFX_Capitao_Onda_Lancar");
        BossFx wave = Fx("onda_sombra", start, 12f, true, 6f);
        LoopEmitter.Attach(wave.gameObject, "SFX_Capitao_Onda_loop", 0.5f, 12f);
        wave
            .Damage(new Vector2(0f, 0.75f), new Vector2(0.6f, 1.4f), 0f, 6f)
            .Move(new Vector2(facing * 6.5f, 0f), arenaMinX, arenaMaxX);
        yield return new WaitForSeconds(Length("attack_onda_sombra", 12f) - 3f / 12f);
    }

    // ---------------- Guardião Caído
    IEnumerator Combo()
    {
        yield return Approach(2.6f, 2.5f);
        yield return Telegraph("telegraph_combo", 0.25f);
        Play("attack_combo_espada", 12f, false);
        Vector2 off = new Vector2(1.8f, 1.6f), size = new Vector2(2.6f, 3.0f);
        StartCoroutine(MeleeWindow(1f / 12f, 3f / 12f, off, size));
        Snd("SFX_Caido_Combo");
        Fx("corte", transform.position + new Vector3(facing * 1.9f, 1.6f), 12f, false, 0f, facing < 0);
        yield return new WaitForSeconds(5f / 12f);
        StartCoroutine(MeleeWindow(0f, 2f / 12f, off, size));
        Snd("SFX_Caido_Combo");
        Fx("corte", transform.position + new Vector3(facing * 2.1f, 1.3f), 12f, false, 0f, facing < 0);
        yield return new WaitForSeconds(Length("attack_combo_espada", 12f) - 5f / 12f);
    }

    IEnumerator Projeteis()
    {
        yield return Telegraph("telegraph_projeteis", 0.3f);
        Play("attack_projeteis_lunares", 12f, false);
        yield return new WaitForSeconds(2f / 12f);
        Vector3 origin = transform.position + new Vector3(facing * 2.0f, 2.4f);
        Vector2 toPlayer = player != null ? ((Vector2)player.position + Vector2.up * 0.3f - (Vector2)origin).normalized
                                          : new Vector2(facing, 0);
        Snd("SFX_Caido_Projetil_Disparo");
        for (int i = -1; i <= 1; i++)
        {
            Vector2 dir = Quaternion.Euler(0, 0, i * 14f) * toPlayer;
            Fx("projetil_lunar", origin, 12f, true, 5f)
                .Damage(Vector2.zero, new Vector2(0.6f, 0.45f), 0f, 5f)
                .Move(dir * 6f, arenaMinX - 1f, arenaMaxX + 1f);
        }
        yield return new WaitForSeconds(Length("attack_projeteis_lunares", 12f) - 2f / 12f);
    }

    IEnumerator Teleporte()
    {
        yield return Telegraph("telegraph_teleporte", 0.2f);
        Snd("SFX_Caido_Teleporte_Sumir");
        Fx("teleporte", transform.position + Vector3.up * 1.6f, 12f, false);
        yield return PlayOnce("attack_teleporte", 12f);
        // reappear behind the player
        float side = PlayerX < (arenaMinX + arenaMaxX) * 0.5f ? 1f : -1f;
        float x = Mathf.Clamp(PlayerX + side * 2.4f, arenaMinX + 1f, arenaMaxX - 1f);
        transform.position = new Vector3(x, floorY, 0f);
        Snd("SFX_Caido_Teleporte_Surgir");
        Fx("teleporte", transform.position + Vector3.up * 1.6f, 12f, false);
        FacePlayer();
        // fade back in by playing the dissolve backwards
        Sprite[] f = Frames("attack_teleporte");
        for (int i = f.Length - 1; i >= 0; i--) { curFrames = new[] { f[i] }; curTime = 0; ApplyFrame(); yield return new WaitForSeconds(1f / 14f); }
        yield return Combo();
    }

    // ------------------------------------------------------------------ damage
    public bool TakeSwordHit()
    {
        if (!active || dead || invulnerable) return false;
        health--;
        hitFlash = 0.12f;
        GameAudio.Sfx("SFX_Espada_Impacto_Inimigo");
        if (health > 0) Snd(SoundPrefix + "_Dano");   // the lethal hit plays the death sound instead
        UpdateHealthBar();
        if (health <= 0)
        {
            // stop the fight loop and any running attack before the death sequence starts
            StopAllCoroutines();
            attacking = false;
            invulnerable = false;
            StartCoroutine(Die());
            return true;
        }
        StartCoroutine(Invulnerable(0.25f));
        if (!attacking) StartCoroutine(HitReaction());
        return true;
    }

    IEnumerator Invulnerable(float t) { invulnerable = true; yield return new WaitForSeconds(t); invulnerable = false; }

    IEnumerator HitReaction()
    {
        if (staggered) yield break;          // the stagger already shows the hit pose
        yield return PlayOnce("hit", 12f);
        if (!attacking && !dead && !staggered) Play("idle", 8f, true);
    }

    IEnumerator Die()
    {
        dead = true;
        hurtCollider.enabled = false;
        hitFlash = 0f; sr.color = Color.white;
        GameManager.ScoreAdd(score);
        Enemy.OnGettingPoint?.Invoke(transform.position + Vector3.up * 2f, score);
        CameraShake(0.4f);
        Snd(SoundPrefix + "_Morte");
        GameAudio.BossDefeated();
        PlayerHealth.BossRewards();          // +1 max heart for this run and full health
        yield return PlayOnce("death", 10f);
        for (float t = 0; t < 0.8f; t += Time.deltaTime)
        {
            sr.color = new Color(1f, 1f, 1f, 1f - t / 0.8f);
            yield return null;
        }
        if (barRoot != null) Destroy(barRoot);
        LockArena(false);
        sr.enabled = false;
        if (bossType == BossType.GuardiaoCaido)
        {
            // final boss: no exit door — the seal is restored, the end credits roll
            yield return new WaitForSeconds(2.5f);
            GameManager.FinishGame();
            yield break;
        }
        if (door != null)
        {
            door.gameObject.SetActive(true);
            GameAudio.Sfx("SFX_Selo_Portal_Abrir", 0.8f);
            // every boss has its own fx set; the portal glow uses the Guardião Caído's teleport fx
            Sprite[] glow = Resources.LoadAll<Sprite>("Bosses/guardiao_caido/fx/teleporte");
            System.Array.Sort(glow, (x, y) => string.CompareOrdinal(x.name, y.name));
            if (glow.Length > 0) BossFx.Spawn(glow, 10f, false, door.transform.position + Vector3.up * 0.8f, false, 0f);
        }
    }

    // ------------------------------------------------------------------ arena / ui / feel
    void LockArena(bool locked)
    {
        if (locked && arenaWall == null)
        {
            arenaWall = new GameObject("BossArenaWall");
            arenaWall.layer = LayerMask.NameToLayer("Platform");
            arenaWall.transform.position = new Vector3(arenaMinX - 0.5f, floorY + 6f);
            BoxCollider2D c = arenaWall.AddComponent<BoxCollider2D>();
            c.size = new Vector2(1f, 12f);
        }
        else if (!locked && arenaWall != null)
        {
            Destroy(arenaWall);
        }
    }

    float shakeTime, shakeStrength;
    const float ShakeDuration = 0.25f;

    void CameraShake(float strength)
    {
        shakeStrength = strength;
        shakeTime = ShakeDuration;
    }

    // Runs after Cinemachine has positioned the camera this frame (see DefaultExecutionOrder),
    // so the offset is visible; Cinemachine rewrites the position again next frame.
    void LateUpdate()
    {
        if (shakeTime <= 0f) return;
        shakeTime -= Time.deltaTime;
        Camera cam = Camera.main;
        if (cam != null)
            cam.transform.position += (Vector3)(Random.insideUnitCircle * shakeStrength * Mathf.Clamp01(shakeTime / ShakeDuration));
    }

    void BuildHealthBar()
    {
        barRoot = new GameObject("BossHealthBar");
        Canvas canvas = barRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        CanvasScaler scaler = barRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        RectTransform frame = NewRect("Frame", barRoot.transform, new Color(0.05f, 0.04f, 0.09f, 0.9f));
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 1f);
        frame.pivot = new Vector2(0.5f, 1f);
        frame.sizeDelta = new Vector2(820, 34);
        frame.anchoredPosition = new Vector2(0, -60);

        RectTransform back = NewRect("Back", frame, new Color(0.22f, 0.12f, 0.28f, 1f));
        back.anchorMin = Vector2.zero; back.anchorMax = Vector2.one;
        back.offsetMin = new Vector2(5, 5); back.offsetMax = new Vector2(-5, -5);

        barFill = NewRect("Fill", back, new Color(0.83f, 0.25f, 0.62f, 1f));
        barFill.anchorMin = Vector2.zero; barFill.anchorMax = Vector2.one;
        barFill.offsetMin = barFill.offsetMax = Vector2.zero;
        barFill.pivot = new Vector2(0f, 0.5f);

        GameObject label = new GameObject("Name", typeof(RectTransform));
        label.transform.SetParent(frame, false);
        Text text = label.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = displayName;
        text.fontSize = 30;
        text.alignment = TextAnchor.LowerCenter;
        text.color = new Color(0.92f, 0.9f, 1f);
        RectTransform lr = (RectTransform)label.transform;
        lr.anchorMin = new Vector2(0f, 1f); lr.anchorMax = new Vector2(1f, 1f);
        lr.pivot = new Vector2(0.5f, 0f);
        lr.sizeDelta = new Vector2(0, 40);
        lr.anchoredPosition = new Vector2(0, 4);
        UpdateHealthBar();
    }

    RectTransform NewRect(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    void UpdateHealthBar()
    {
        if (barFill == null) return;
        barFill.anchorMax = new Vector2(Mathf.Clamp01((float)health / maxHealth), 1f);
    }

    void OnDestroy()
    {
        if (barRoot != null) Destroy(barRoot);
        if (arenaWall != null) Destroy(arenaWall);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(arenaMinX, floorY), new Vector3(arenaMaxX, floorY));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(activateX, floorY), new Vector3(activateX, floorY + 4f));
    }
}
