using UnityEngine;
using UnityEngine.Rendering;

// Endless horizontal parallax background, attached to the camera.
// Each layer is a horizontally-seamless sprite drawn in Tiled mode as a child of the camera:
//   parallax 0 = infinitely far (moves with the camera), 1 = moves like the level itself.
// The layers are rebuilt at runtime and in the editor; they are never saved in the scene.
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public Sprite sprite;
        [Range(0f, 1f)] public float parallax;
        public int sortingOrder = -100;
    }

    [SerializeField] Layer[] layers = new Layer[0];
    [SerializeField] float depth = 20f;

    Camera cam;
    Transform[] built;
    SpriteRenderer[] renderers;
    bool dirty = true;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        dirty = true;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        Clear();
    }

    void OnValidate()
    {
        dirty = true;
    }

    void Build()
    {
        Clear();
        built = new Transform[layers.Length];
        renderers = new SpriteRenderer[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null || layers[i].sprite == null) continue;
            GameObject go = new GameObject("Parallax_" + layers[i].sprite.name);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = layers[i].sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.sortingOrder = layers[i].sortingOrder;
            built[i] = go.transform;
            renderers[i] = sr;
        }
        dirty = false;
    }

    void Clear()
    {
        if (built == null) return;
        foreach (Transform t in built)
        {
            if (t == null) continue;
            if (Application.isPlaying) Destroy(t.gameObject);
            else DestroyImmediate(t.gameObject);
        }
        built = null;
        renderers = null;
    }

    void LateUpdate()
    {
        UpdateLayers();
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera c)
    {
        // runs after Cinemachine moved the camera: no jitter
        // never create/destroy objects from inside the render callback
        if (c == cam && !dirty && built != null) UpdateLayers();
    }

    void UpdateLayers()
    {
        if (cam == null) return;
        if (dirty || built == null) Build();
        if (built == null) return;

        float viewH = cam.orthographicSize * 2f;
        float viewW = viewH * cam.aspect;
        float camX = transform.position.x;

        for (int i = 0; i < built.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null || sr.sprite == null) continue;

            Vector2 size = sr.sprite.bounds.size;
            // never leave empty bands if the view is taller than the art (other resolutions)
            float scale = Mathf.Max(1f, viewH / size.y);
            float w = size.x * scale;
            int copies = Mathf.CeilToInt(viewW / w) + 2;
            if (copies % 2 == 0) copies++;          // odd count keeps one copy centered
            sr.size = new Vector2(size.x * copies, size.y);

            float offset = Mathf.Repeat(camX * layers[i].parallax, w);
            built[i].localScale = new Vector3(scale, scale, 1f);
            built[i].localPosition = new Vector3(-offset, 0f, depth);
        }
    }
}
