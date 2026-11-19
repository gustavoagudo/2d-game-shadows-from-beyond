using System.Collections;
using UnityEngine;

// Shakes when the player steps on it, falls, then comes back at its original place
// (players respawn at checkpoints, so the platforms must be there again).
public class FallingPlatform : MonoBehaviour
{
    Rigidbody2D rb;
    SpriteRenderer sr;
    [SerializeField] float delay = 0.5f;
    [SerializeField] int voidLayer = 31;
    [SerializeField] float fallTime = 1f;
    [SerializeField] float respawnDelay = 2f;

    Vector3 startPosition;
    Quaternion startRotation;
    int startLayer;
    RigidbodyType2D startBodyType;
    Collider2D[] colliders;
    SpriteRenderer[] renderers;
    Bounds startBounds;
    bool busy;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        startLayer = gameObject.layer;
        startBodyType = rb.bodyType;
        colliders = GetComponentsInChildren<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        startBounds = new Bounds(startPosition, Vector3.zero);
        foreach (Collider2D c in colliders) startBounds.Encapsulate(c.bounds);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // the player has two colliders: only the first contact starts the fall
        if (!busy && collision.CompareTag("Player"))
        {
            StartCoroutine(Fall());
        }
    }

    IEnumerator Fall()
    {
        busy = true;
        SoundFXManager.Play("Falling");
        for (int i = 0; i < 3; i++)
        {
            SetAlpha(1.0f);
            yield return new WaitForSeconds(0.1f);
            SetAlpha(0.5f);
            yield return new WaitForSeconds(0.1f);
        }
        SetAlpha(1.0f);
        yield return new WaitForSeconds(delay);
        GameAudio.SfxAt("SFX_Plataforma_Cair", transform.position, 0.9f, 12f);
        rb.bodyType = RigidbodyType2D.Dynamic;
        gameObject.layer = voidLayer;
        yield return new WaitForSeconds(fallTime);

        // hidden: back to the start
        SetVisible(false);
        rb.bodyType = startBodyType;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.SetPositionAndRotation(startPosition, startRotation);
        gameObject.layer = startLayer;
        yield return new WaitForSeconds(respawnDelay);

        // don't pop in on top of the player
        int playerMask = LayerMask.GetMask("Player");
        while (Physics2D.OverlapBox(startBounds.center, startBounds.size, 0f, playerMask))
        {
            yield return null;
        }

        SetVisible(true);
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            SetAlpha(t / 0.35f);
            yield return null;
        }
        SetAlpha(1f);
        busy = false;
    }

    void SetVisible(bool visible)
    {
        foreach (Collider2D c in colliders) if (c) c.enabled = visible;
        foreach (SpriteRenderer r in renderers) if (r) r.enabled = visible;
    }

    void SetAlpha(float alpha)
    {
        Color color = sr.color;
        color.a = alpha;
        sr.color = color;
    }
}
