using UnityEngine;

// Lunar rune checkpoint. Touching it makes it the respawn point for falls, heals a heart (first time only)
// and lights it up in cyan. Place it with its transform 0.5 units above the floor (sprite pivot is 16 px up).
public class Checkpoint : MonoBehaviour
{
    [SerializeField] Sprite sprite;
    [SerializeField] int healOnActivate = 1;

    static readonly Color Dormant = new Color(0.42f, 0.42f, 0.58f, 1f);
    static readonly Color Glow = new Color(0.37f, 0.89f, 0.94f, 1f);

    SpriteRenderer sr;
    bool active;
    float flash;

    void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;
        sr.color = Dormant;
        BoxCollider2D c = gameObject.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.offset = new Vector2(0f, 0.5f);
        c.size = new Vector2(1.4f, 2.4f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (active || !other.CompareTag("Player") || !GameManager.isGameOn) return;
        active = true;
        flash = 1f;
        if (PlayerHealth.Instance != null) PlayerHealth.Instance.SetCheckpoint(transform.position + Vector3.up * 0.8f);
        PlayerHealth.Heal(healOnActivate);
        GameAudio.Sfx("SFX_Checkpoint_Ativar");
        GameAudio.Jingle("MUS_Checkpoint");
    }

    void Update()
    {
        if (!active) return;
        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 1.2f);
        float breathe = 0.25f + 0.15f * Mathf.Sin(Time.time * 2.5f);          // soft lunar glow while active
        sr.color = Color.Lerp(Color.white, Glow, Mathf.Max(flash, breathe));
        transform.localScale = Vector3.one * (1f + 0.18f * flash * flash);
    }
}
