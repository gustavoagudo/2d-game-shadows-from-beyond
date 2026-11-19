using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    Rigidbody2D rb;
    Animator animator;

    [Header("--- Movement ---")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float moveSpeedOnAir = 4f;
    float moveX;
    bool isFacingRight = true;

    [Header("--- Bottom Check ---")]
    [SerializeField] Transform bottom;
    [SerializeField] Vector2 bottomSize = new Vector2(2f, 2f);
    [SerializeField] LayerMask bottomLayer;
    [SerializeField] bool isJumping = false;
    [SerializeField] float fallGravity = 2.0f;
    float gravityBase;

    [Header("--- Side Check ---")]
    [SerializeField] Transform side;
    [SerializeField] Vector2 sideSize = new Vector2(2f, 2f);
    [SerializeField] LayerMask sideLayer;
    [SerializeField] float slideSpeed = -1.5f;
    bool isOnWall;

    [Header("--- Jump ---")]
    [SerializeField] float jumpForce = 8f;
    [SerializeField] float jumpMax = 3.0f;
    [SerializeField] float jumpStart = 8f;

    [SerializeField] ParticleSystem dustFX;

    [Header("--- Sword Attack ---")]
    [SerializeField] Vector2 attackOffset = new Vector2(0.85f, 0.15f);
    [SerializeField] Vector2 attackSize = new Vector2(1.7f, 1.8f);
    [SerializeField] float attackCooldown = 0.1f;
    // Must match the Attack / AirAttack clips: hit window opens on the slash frame
    const float groundAttackDuration = 4f / 12f;   // 4 frames @ 12 fps
    const float groundAttackHitStart = 2f / 12f;   // frame 2 = slash
    const float airAttackDuration = 2f / 9f;       // 2 frames @ 9 fps
    const float airAttackHitStart = 1f / 9f;       // frame 1 = slash
    InputAction attackAction;
    int enemyMask;
    bool isAttacking;
    float nextAttackTime;
    Coroutine attackRoutine;

    [Header("--- Parry ---")]
    [Tooltip("Seconds from the start of a sword swing during which an enemy hit is parried")]
    [SerializeField] float parryWindow = 0.3f;
    float attackStartTime = -10f;

    [Header("--- Dodge ---")]
    [SerializeField] float dodgeSpeed = 12f;
    [SerializeField] float dodgeDuration = 3f / 11f;   // Dodge clip: 3 roll frames @ 11 fps
    [SerializeField] float dodgeCooldown = 0.45f;
    InputAction dodgeAction;
    bool isDodging;
    bool airDodgeUsed;
    float nextDodgeTime;
    bool ignoringEnemies;

    // read by PlayerHealth
    public bool IsDodging => isDodging;
    public bool IsParryWindow => isAttacking && Time.time - attackStartTime <= parryWindow;

    bool gameBeginning = true;

    bool isGoingUp {
        get {
            return rb.linearVelocityY > 0;
        }
    }
    bool isGoingDown {
        get {
            return rb.linearVelocityY < 0;
        }
    }


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gravityBase = rb.gravityScale;
        animator = GetComponentInChildren<Animator>();
        animator.SetTrigger("isAppearing");
        Door.OnGoingNextLevel += GoNextLevel;

        enemyMask = LayerMask.GetMask("Enemy", "Enemy(Temporary)");   // temporary = just bumped/parried
        PlayerInput playerInput = GetComponent<PlayerInput>();
        attackAction = playerInput != null ? playerInput.actions.FindAction("Attack") : null;
        if (attackAction != null)
        {
            attackAction.performed += OnAttack;
        }
        else
        {
            Debug.LogWarning("PlayerControl: 'Attack' input action not found.");
        }

        dodgeAction = playerInput != null ? playerInput.actions.FindAction("Dodge") : null;
        if (dodgeAction != null)
        {
            dodgeAction.performed += OnDodge;
        }
        else
        {
            Debug.LogWarning("PlayerControl: 'Dodge' input action not found.");
        }
    }

    void OnDisable()
    {
        Door.OnGoingNextLevel -= GoNextLevel;
        if (attackAction != null)
        {
            attackAction.performed -= OnAttack;
        }
        if (dodgeAction != null)
        {
            dodgeAction.performed -= OnDodge;
        }
        SetIgnoreEnemies(false);
    }

    void OnAttack(InputAction.CallbackContext context)
    {
        if (gameBeginning || isAttacking || isDodging || !GameManager.isGameOn
            || Time.timeScale == 0f || Time.time < nextAttackTime)
        {
            return;
        }
        attackRoutine = StartCoroutine(Attack(!isGrounded));
    }

    IEnumerator Attack(bool inAir)
    {
        float duration = inAir ? airAttackDuration : groundAttackDuration;
        float hitStart = inAir ? airAttackHitStart : groundAttackHitStart;

        isAttacking = true;
        attackStartTime = Time.time;
        GameAudio.Sfx(inAir ? "SFX_Kael_Espada_AtaqueAereo" : "SFX_Kael_Espada_Golpe", 0.9f);
        animator.SetBool("isAttacking", true);
        animator.SetTrigger(inAir ? "airAttack" : "attack");

        // each enemy is hit at most once per swing
        HashSet<IHittable> alreadyHit = new HashSet<IHittable>();
        float t = 0f;
        while (t < duration)
        {
            if (t >= hitStart && GameManager.isGameOn)
            {
                foreach (Collider2D col in Physics2D.OverlapBoxAll(AttackCenter, attackSize, 0f, enemyMask))
                {
                    IHittable target = col.GetComponentInParent<IHittable>();
                    if (target != null && alreadyHit.Add(target))
                    {
                        target.TakeSwordHit();
                    }
                }
            }
            yield return null;
            t += Time.deltaTime;
        }

        animator.SetBool("isAttacking", false);
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
        attackRoutine = null;
    }

    // ------------------------------------------------------------------ dodge
    void OnDodge(InputAction.CallbackContext context)
    {
        if (gameBeginning || isDodging || !GameManager.isGameOn
            || Time.timeScale == 0f || Time.time < nextDodgeTime)
        {
            return;
        }
        if (!isGrounded)
        {
            if (airDodgeUsed) return;   // one dodge per jump
            airDodgeUsed = true;
        }
        StartCoroutine(Dodge());
    }

    IEnumerator Dodge()
    {
        // a dodge cancels a swing in progress
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        if (isAttacking)
        {
            isAttacking = false;
            animator.SetBool("isAttacking", false);
            nextAttackTime = Time.time + attackCooldown;
        }

        // dodge toward the held direction, otherwise forward
        float dir = Mathf.Abs(moveX) > 0.1f ? Mathf.Sign(moveX) : (isFacingRight ? 1f : -1f);
        if ((dir > 0f) != isFacingRight) DirectionChange();

        isDodging = true;
        animator.SetBool("isDodging", true);
        animator.SetTrigger("dodge");
        bool groundedDodge = isGrounded;
        if (groundedDodge) dustFX.Play();
        GameAudio.Sfx(groundedDodge ? "SFX_Kael_Rolamento" : "SFX_Kael_Esquiva_Ar", 0.85f);
        SetIgnoreEnemies(true);          // roll through enemies instead of bumping into them

        float t = 0f;
        while (t < dodgeDuration)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dir * dodgeSpeed, 0f);
            t += Time.deltaTime;
            yield return null;
        }

        SetIgnoreEnemies(false);
        rb.gravityScale = gravityBase;
        rb.linearVelocity = new Vector2(moveX * moveSpeed, 0f);
        isDodging = false;
        animator.SetBool("isDodging", false);
        nextDodgeTime = Time.time + dodgeCooldown;
    }

    void SetIgnoreEnemies(bool ignore)
    {
        if (ignoringEnemies == ignore) return;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0) return;
        Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, ignore);
        ignoringEnemies = ignore;
    }

    Vector3 AttackCenter {
        get {
            float dir = isFacingRight ? 1f : -1f;
            return transform.position + new Vector3(attackOffset.x * dir, attackOffset.y, 0f);
        }
    }

    void GoNextLevel()
    {
        animator.SetTrigger("isNextLevel");
    }

    float t = 0;
    void Update()
    {
        //delay at the game start for the chracter creation effect
        if (gameBeginning)
        {
            t += Time.deltaTime;
            if (t > 0.5f)
            {
                gameBeginning = false;
            }
            return;
        }

        if (isDodging)
        {
            // the Dodge coroutine drives the velocity
            AnimationHandle();
            return;
        }
        bool grounded = isGrounded;
        if (grounded)
        {
            airDodgeUsed = false;
        }
        FootstepsAndLanding(grounded);

        if (!isJumping)
        {
            rb.linearVelocityX = moveX * moveSpeed;
        }
        else
        {
            rb.linearVelocityX = moveX * moveSpeedOnAir;
        }

        DirectionCheck();
        BottomCheck();
        SideCheck();
        FallingAccel();
        AnimationHandle();

        if (transform.position.y - jumpStart > jumpMax)
        {
            jumpMax = transform.position.y - jumpStart;
        }
    }

    // ------------------------------------------------------------------ footsteps / landing sounds
    bool wasGrounded = true;
    float airTime, lowestVelocityY, nextStepTime;

    void FootstepsAndLanding(bool grounded)
    {
        if (!grounded)
        {
            airTime += Time.deltaTime;
            lowestVelocityY = Mathf.Min(lowestVelocityY, rb.linearVelocityY);
        }
        else
        {
            if (!wasGrounded && airTime > 0.15f)
            {
                GameAudio.Sfx(lowestVelocityY < -14f ? "SFX_Kael_Aterrissagem_Pesada" : "SFX_Kael_Aterrissagem_Leve", 0.7f);
                nextStepTime = Time.time + 0.2f;
            }
            airTime = 0f;
            lowestVelocityY = 0f;
            // running footsteps (Kael runs at full speed: ~4 steps per second)
            if (Mathf.Abs(moveX) > 0.1f && Time.time >= nextStepTime)
            {
                GameAudio.Sfx("SFX_Kael_Passo_Corrida", 0.55f, 0.08f);
                nextStepTime = Time.time + 0.26f;
            }
        }
        wasGrounded = grounded;
    }

    void AnimationHandle()
    {

        animator.SetBool("isWalking", false);
        if (!isJumping && Mathf.Abs(moveX)>0.1f)
        {
            animator.SetBool("isWalking", true);
        }
        animator.SetBool("isOnWall", isOnWall);
        animator.SetFloat("velocityY", rb.linearVelocityY);
    }

    void FallingAccel()
    {
        if (isJumping)
        {
            if (transform.position.y - jumpStart > jumpMax)
            {
                rb.linearVelocityY = 0;
            }
        }
        if (!isJumping && rb.linearVelocityY > 0)
        {
            rb.linearVelocityY = 0;
        }

            if (rb.linearVelocityY < 0)
        {
            rb.gravityScale = gravityBase * fallGravity;
        }
        else
        {
            rb.gravityScale = gravityBase;
        }
    }

    bool IsTouchingSide()
    {
        return Physics2D.OverlapBox(side.position, sideSize, 0, sideLayer);
    }    

    void SideCheck()
    {
        if (IsTouchingSide() && isJumping)
        {
            isOnWall = true;
            rb.linearVelocityY = Mathf.Max(rb.linearVelocityY, slideSpeed);
            
        }
        else
        {
            isOnWall = false;
            
        }
    }

    void BottomCheck()
    {
        if (isGoingUp)
        {
            return;
        }
        if (Physics2D.OverlapBox(bottom.position, bottomSize, 0, bottomLayer))
        {
            isJumping = false;
        }
    }

    void DirectionChange()
    {
        if (isGrounded)
        {
            dustFX.Play();
        }
        isFacingRight = !isFacingRight;
        Vector3 ls = rb.transform.localScale;
        ls.x *= -1f;
        rb.transform.localScale = ls;
    }

    void DirectionCheck()
    {
        if (isFacingRight && moveX < 0 || !isFacingRight && moveX > 0)
        {
            DirectionChange();
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveX = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (isDodging)
        {
            return;
        }
        if (context.performed && !isJumping && isGrounded)
        {
            isJumping = true;
            dustFX.Play();
            SoundFXManager.Play("Jump");
            rb.linearVelocityY = jumpForce;
            jumpStart = transform.position.y;
        }
        else if (context.canceled && isGoingUp)
        {
            rb.linearVelocityY *= 0.5f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(bottom.transform.position, bottomSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(side.transform.position, sideSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(AttackCenter, attackSize);
    }

    bool isGrounded {
        get { return Physics2D.OverlapBox(bottom.position, bottomSize, 0, bottomLayer); }
    }

}
