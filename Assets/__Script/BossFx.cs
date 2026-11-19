using UnityEngine;

// Short-lived boss effect / projectile: plays a sprite sequence, optionally moves and damages the player.
public class BossFx : MonoBehaviour
{
    Sprite[] frames;
    float fps;
    bool loop;
    float lifetime;
    float time;
    SpriteRenderer sr;

    // damage
    Vector2 hitSize;
    Vector2 hitOffset;
    float hitStart, hitEnd;   // seconds since spawn
    bool hasHitbox;
    bool hitDone;
    int playerMask;

    // movement
    Vector2 velocity;
    float minX = float.NegativeInfinity, maxX = float.PositiveInfinity;

    public static BossFx Spawn(Sprite[] frames, float fps, bool loop, Vector3 position, bool flipX,
                               float lifetime, int sortingOrder = 5)
    {
        GameObject go = new GameObject("BossFx");
        go.transform.position = position;
        BossFx fx = go.AddComponent<BossFx>();
        fx.sr = go.AddComponent<SpriteRenderer>();
        fx.sr.flipX = flipX;
        fx.sr.sortingOrder = sortingOrder;
        fx.frames = frames;
        fx.fps = fps;
        fx.loop = loop;
        fx.lifetime = lifetime > 0 ? lifetime : (frames.Length > 0 ? frames.Length / fps : 0.1f);
        fx.playerMask = LayerMask.GetMask("Player");
        if (frames.Length > 0) fx.sr.sprite = frames[0];
        return fx;
    }

    public BossFx Damage(Vector2 offset, Vector2 size, float start, float end)
    {
        hasHitbox = true; hitOffset = offset; hitSize = size; hitStart = start; hitEnd = end;
        return this;
    }

    public BossFx Move(Vector2 vel, float arenaMinX, float arenaMaxX)
    {
        velocity = vel; minX = arenaMinX; maxX = arenaMaxX;
        if (vel.sqrMagnitude > 0.001f)
        {
            // sprites point right: rotate toward the travel direction (keep upright when going left)
            float angle = Mathf.Atan2(vel.y, Mathf.Abs(vel.x)) * Mathf.Rad2Deg;
            sr.flipX = vel.x < 0;
            transform.rotation = Quaternion.Euler(0, 0, vel.x < 0 ? -angle : angle);
        }
        return this;
    }

    void Update()
    {
        time += Time.deltaTime;
        if (frames != null && frames.Length > 0)
        {
            int i = Mathf.FloorToInt(time * fps);
            i = loop ? i % frames.Length : Mathf.Min(i, frames.Length - 1);
            sr.sprite = frames[i];
        }

        transform.position += (Vector3)(velocity * Time.deltaTime);
        if (transform.position.x < minX || transform.position.x > maxX)
        {
            Destroy(gameObject);
            return;
        }

        if (hasHitbox && !hitDone && time >= hitStart && time <= hitEnd && GameManager.isGameOn)
        {
            Vector2 off = new Vector2(sr.flipX ? -hitOffset.x : hitOffset.x, hitOffset.y);
            if (Physics2D.OverlapBox((Vector2)transform.position + off, hitSize, 0f, playerMask))
            {
                hitDone = true;
                // enemy channel: the player can parry it; a parried projectile is deflected (destroyed)
                Enemy.OnPlayerHit?.Invoke(1, transform.position, false);
                if (PlayerHealth.LastHitParried && velocity.sqrMagnitude > 0.01f)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        if (time >= lifetime) Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (!hasHitbox || sr == null) return;
        Gizmos.color = Color.magenta;
        Vector2 off = new Vector2(sr.flipX ? -hitOffset.x : hitOffset.x, hitOffset.y);
        Gizmos.DrawWireCube((Vector2)transform.position + off, hitSize);
    }
}
