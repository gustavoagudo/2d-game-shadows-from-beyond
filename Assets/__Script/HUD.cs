using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.PlayerLoop.EarlyUpdate;

public class HUD : MonoBehaviour
{
    [SerializeField] Image heartPrefab;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] GameObject pointText;
    [SerializeField] RectTransform uiCanvas;
    List<Image> hearts = new List<Image>();
    RectTransform rt;
    int healthPrevValue = 0;


    void Awake()
    {
        rt = GetComponentInParent<RectTransform>();
        PlayerHealth.OnHealthChanged += HealthUpdate;
        GameManager.OnScoreChanged += ScoreUpdate;
        Enemy.OnGettingPoint += PointShow;
        Item.OnGettingPoint += PointShow;
    }

    void Start()
    {
        ScoreUpdate();
    }

    void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= HealthUpdate;
        GameManager.OnScoreChanged -= ScoreUpdate;
        Enemy.OnGettingPoint -= PointShow;
        Item.OnGettingPoint -= PointShow;
    }

    void PointShow(Vector3 pos, int point)
    {

        GameObject floatingText = Instantiate(pointText);
        floatingText.transform.position = pos;
        PointAnimation pointAnimation = floatingText.GetComponent<PointAnimation>();
        pointAnimation.point = point;
    }

    void ScoreUpdate()
    {
        scoreText.text = GameManager.score.ToString("N0");
    }

    // One slot per max heart (max grows +1 per boss defeated): full hearts are lit, lost ones stay as dark slots.
    static readonly Color EmptyHeart = new Color(0.18f, 0.18f, 0.3f, 0.6f);
    bool firstUpdate = true;

    void HealthUpdate(int health)
    {
        int slots = Mathf.Max(GameManager.healthMax, health);
        while (hearts.Count < slots)
        {
            Image h = Instantiate(heartPrefab, transform);
            hearts.Add(h);
            if (!firstUpdate) StartCoroutine(Pop(h, Color.white));     // new max heart
        }
        while (hearts.Count > slots)
        {
            Destroy(hearts[hearts.Count - 1].gameObject);
            hearts.RemoveAt(hearts.Count - 1);
        }
        for (int i = 0; i < hearts.Count; i++)
        {
            bool full = i < health;
            bool wasFull = i < healthPrevValue;
            if (firstUpdate || full == wasFull) hearts[i].color = full ? Color.white : EmptyHeart;
            else StartCoroutine(Pop(hearts[i], full ? Color.white : EmptyHeart));   // lost or healed: pulse
        }
        healthPrevValue = health;
        firstUpdate = false;
    }

    IEnumerator Pop(Image heart, Color target)
    {
        RectTransform r = heart.rectTransform;
        Color from = target == EmptyHeart ? new Color(1f, 0.45f, 0.5f, 1f) : new Color(0.6f, 1f, 1f, 1f);
        for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            if (heart == null) yield break;
            float k = t / 0.35f;
            r.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(k * Mathf.PI));
            heart.color = Color.Lerp(from, target, k);
            yield return null;
        }
        if (heart == null) yield break;
        r.localScale = Vector3.one;
        heart.color = target;
    }
}
