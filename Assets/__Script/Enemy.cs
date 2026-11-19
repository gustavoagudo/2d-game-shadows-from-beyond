using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class Enemy : MonoBehaviour, IHittable
{
    [SerializeField] protected int health;
    protected int currentHealth;
    [SerializeField] protected int damage;
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected bool isFacingRight;
    [SerializeField] protected GameObject head;
    [SerializeField] protected float headHeight;
    [SerializeField] protected float headWidth;
    [SerializeField] protected LayerMask playerMask;
    [SerializeField] protected LayerMask playerWeaponMask;
    [SerializeField] protected int enemyLayer = 11;
    [SerializeField] protected int enemyDeadLayer = 12;
    [SerializeField] protected int enemyTmpLayer = 13;
    [SerializeField] protected ParticleSystem crashFX;

    public static UnityAction<int, Vector3, bool> OnPlayerHit;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected Vector3 headSize;
    protected Vector3 headPosition;
    protected bool isAlive = true;
    [SerializeField] protected int score;

    public static UnityAction<Vector3, int> OnGettingPoint;


    protected virtual void Start()
    {
        isAlive = true;
        currentHealth = health;
        rb = GetComponent<Rigidbody2D>();
        animator = rb.GetComponentInChildren<Animator>();
        crashFX = rb.GetComponentInChildren<ParticleSystem>();

        headSize = new Vector3(headWidth, headHeight, 0);

        // ambient loop: wings for flyers, little legs for crawlers
        loop = LoopEmitter.Attach(gameObject, IsFlyer ? "SFX_Mariposa_Asas_loop" : "SFX_Rastejante_Andar_loop",
                                  IsFlyer ? 0.3f : 0.28f, 6f);
    }

    LoopEmitter loop;
    float swordImmuneUntil;
    public bool IsAlive => isAlive;
    bool IsFlyer => this is EnemyBird;

    void OnCollisionEnter2D(Collision2D collision)
    {
        //enemy got hit (on the head)
        headPosition = head.transform.position;
        if (Physics2D.OverlapBox(headPosition, headSize, 0, playerWeaponMask))
        {
            MakeUninteractable(2f);
            // bounce the player up (damage 0)
            OnPlayerHit?.Invoke(0, transform.position, false);
            GameAudio.Sfx("SFX_Kael_Pisao_Inimigo");
            ReceiveHit();
        }

        //player got hit
        else if (collision.collider.CompareTag("Player"))
        {
            MakeUninteractable(1f);
            Vector3 position = transform.position;
            OnPlayerHit?.Invoke(damage, position, false);
        }
    }

    // Called by the player's sword (PlayerControl attack hitbox).
    // Returns true if the hit landed.
    // The sword also hits an enemy in its temporary layer (right after it bumped into the player
    // or was parried), otherwise a swing right after a parry would pass through it.
    public bool TakeSwordHit()
    {
        if (!isAlive || gameObject.layer == enemyDeadLayer || Time.time < swordImmuneUntil)
        {
            return false;
        }
        swordImmuneUntil = Time.time + 0.25f;
        CancelInvoke("MakeInteractable");
        MakeUninteractable(0.4f);
        GameAudio.Sfx("SFX_Espada_Impacto_Inimigo");
        ReceiveHit();
        return true;
    }

    void ReceiveHit()
    {
        animator.SetBool("isHit", true);
        string kind = IsFlyer ? "SFX_Mariposa" : "SFX_Rastejante";

        if (--currentHealth >= 1)
        {
            GameAudio.Sfx(kind + "_Dano");
        }
        else
        {
            GameAudio.Sfx(kind + "_Morte");
            if (loop != null) loop.Stop();
            GameManager.ScoreAdd(score);
            OnGettingPoint?.Invoke(transform.position, score);

            crashFX.Play();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(Vector3.down * 2f, ForceMode2D.Impulse);
            rb.constraints = RigidbodyConstraints2D.None;
            rb.AddTorque(90);
            CancelInvoke("MakeInteractable");
            gameObject.layer = enemyDeadLayer;

            EnemyDead();
        }
    }

    void MakeUninteractable(float duration)
    {
        gameObject.layer = enemyTmpLayer;
        Invoke("MakeInteractable", duration);
    }
    void MakeInteractable()
    {
        gameObject.layer = enemyLayer;
    }

    void EnemyDead()
    {
        isAlive = false;
        Destroy(this.gameObject, 0.8f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = UnityEngine.Color.red;
        headPosition = head.transform.position;
        Gizmos.DrawWireCube(headPosition, headSize);
    }
}
