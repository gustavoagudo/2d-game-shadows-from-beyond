using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    public static UnityAction<int> OnHealthChanged;
    public static UnityAction OnDead;
    // Raised when the player parries an enemy hit (position of the parried hit). Bosses use it to stagger.
    public static UnityAction<Vector3> OnParry;
    // True right after a damage event was parried (lets projectiles know they were deflected).
    public static bool LastHitParried { get; private set; }

    [Header("--- Parry ---")]
    [Tooltip("Max distance between the player and the enemy hit for a parry to count")]
    [SerializeField] float parryRange = 3f;
    [Tooltip("Invulnerability after a successful parry: time to dodge and reposition")]
    [SerializeField] float parryInvulnerability = 0.6f;
    [SerializeField] Color parryFlash = new Color(0.37f, 0.89f, 0.94f, 1f);

    [Header("--- Falls / checkpoints ---")]
    [Tooltip("Falling below this height counts as falling off the map (safety net besides the DeathZone)")]
    [SerializeField] float fallKillY = -16f;
    [Tooltip("Invulnerability after respawning at a checkpoint")]
    [SerializeField] float respawnInvulnerability = 1.5f;

    public static PlayerHealth Instance { get; private set; }

    bool isInvincible;
    bool respawning;
    Vector3 respawnPoint;

    Rigidbody2D rb;
    Animator animator;
    PlayerControl control;
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(GameManager.health);
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        control = GetComponent<PlayerControl>();
        spriteRenderer = animator != null ? animator.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        respawnPoint = transform.position;          // until a checkpoint is touched, falls return to the level start
        Trap.OnPlayerHit += TakeDamage;
        Enemy.OnPlayerHit += TakeEnemyDamage;
        DeathZone.OnPlayerHit += TakeFall;
    }

    void OnDisable()
    {
        Trap.OnPlayerHit -= TakeDamage;
        Enemy.OnPlayerHit -= TakeEnemyDamage;
        DeathZone.OnPlayerHit -= TakeFall;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!respawning && GameManager.isGameOn && transform.position.y < fallKillY)
        {
            Fell();
        }
    }

    // ------------------------------------------------------------------ checkpoints, healing, boss rewards
    public void SetCheckpoint(Vector3 position)
    {
        respawnPoint = position;
    }

    public static void Heal(int amount)
    {
        if (amount <= 0) return;
        GameManager.health = Mathf.Min(GameManager.healthMax, GameManager.health + amount);
        OnHealthChanged?.Invoke(GameManager.health);
    }

    // Defeating a boss: +1 max heart for the rest of this run, and full health.
    public static void BossRewards()
    {
        GameManager.healthMax++;
        GameManager.health = GameManager.healthMax;
        OnHealthChanged?.Invoke(GameManager.health);
        GameAudio.Sfx("SFX_Coletar_Vida");
    }

    // ------------------------------------------------------------------ falling off the map
    void TakeFall(int damage, Vector3 hitPosition, bool instantDeath)
    {
        if (!respawning && GameManager.isGameOn) Fell();
    }

    // costs one heart and returns to the last checkpoint; the last heart ends the run
    void Fell()
    {
        GameManager.health = Mathf.Max(0, GameManager.health - 1);
        OnHealthChanged?.Invoke(GameManager.health);
        if (GameManager.health <= 0)
        {
            PlayerDie(true);
            return;
        }
        GameAudio.Sfx("SFX_Kael_Queda_Abismo");
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        respawning = true;
        Renderer r = spriteRenderer != null ? spriteRenderer : GetComponentInChildren<Renderer>();
        r.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;                       // stop falling / triggering while off screen
        yield return new WaitForSeconds(0.7f);

        transform.SetParent(null);                  // in case it was riding a moving platform
        transform.position = respawnPoint;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        r.enabled = true;
        animator.SetTrigger("isAppearing");
        respawning = false;
        StartCoroutine(MakeInvincible(respawnInvulnerability));
    }

    // Hits coming from enemies and bosses can be parried; traps and pits cannot.
    void TakeEnemyDamage(int damage, Vector3 hitPosition, bool instantDeath)
    {
        LastHitParried = false;
        if (damage > 0 && !instantDeath && GameManager.isGameOn && control != null && control.IsParryWindow
            && Vector2.Distance(hitPosition, transform.position) <= parryRange)
        {
            Parry(hitPosition);
            return;
        }
        TakeDamage(damage, hitPosition, instantDeath);
    }

    void Parry(Vector3 hitPosition)
    {
        LastHitParried = true;
        GameAudio.Sfx("SFX_Parry");
        OnParry?.Invoke(hitPosition);
        if (!isInvincible)
        {
            StartCoroutine(ParryInvulnerable());
        }
        StartCoroutine(HitStop(0.07f));
    }

    IEnumerator ParryInvulnerable()
    {
        isInvincible = true;
        float t = 0f;
        while (t < parryInvulnerability)
        {
            // cyan glow that fades out: "protected" feedback, no blinking (it is not damage)
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(parryFlash, Color.white, t / parryInvulnerability);
            t += Time.deltaTime;
            yield return null;
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        isInvincible = false;
    }

    IEnumerator HitStop(float seconds)
    {
        // brief freeze frame to sell the parry; never fights with the pause menu
        if (Time.timeScale != 1f) yield break;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(seconds);
        if (Mathf.Approximately(Time.timeScale, 0.05f)) Time.timeScale = 1f;
    }

    void TakeDamage(int damage, Vector3 hitPosition, bool instantDeath)
    {
        float force = 5f;

        // rolling (dodge) makes the player untouchable, except for pits / instant death
        if (control != null && control.IsDodging && !instantDeath)
        {
            return;
        }
        if (respawning)
        {
            return;
        }
        if (isInvincible && !instantDeath)
        {
            return;
        }
        if (!GameManager.isGameOn)
        {
            return;
        }

        GameManager.health -= damage;
        if (GameManager.health < 0)
        {
            GameManager.health = 0;
        }

        if (damage > 0)
        {
            SoundFXManager.Play("Hit");
            OnHealthChanged?.Invoke(GameManager.health);
            animator.SetTrigger("isHit");
        }
        rb.linearVelocity = Vector3.zero;
        //rb.linearVelocityY = 1.0f * force;
        rb.AddForceY(force, ForceMode2D.Impulse);

        if (GameManager.health <= 0 || instantDeath)
        {
            PlayerDie(instantDeath);
        }
        else if (damage > 0)
        {
            StartCoroutine(MakeInvincible(1));
        }
    }

    IEnumerator MakeInvincible(float timer)
    {
        isInvincible = true;
        Renderer renderer = GetComponentInChildren<Renderer>();

        float blinkDuration = 0.1f;
        float elapsedTime = 0f;

        while (elapsedTime < timer)
        {
            renderer.enabled = !renderer.enabled;
            yield return new WaitForSeconds(blinkDuration);
            elapsedTime += blinkDuration;
        }

        renderer.enabled = true;
        isInvincible = false;
    }

    void PlayerDie(bool fell = false)
    {
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        GameManager.isGameOn = false;
        GameAudio.Sfx(fell ? "SFX_Kael_Queda_Abismo" : "SFX_Kael_Morte");
        animator.SetTrigger("isDead");
        StartCoroutine(Delay(1.3f));
    }

    IEnumerator Delay(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnDead!.Invoke();
    }
}
