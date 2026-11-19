using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Title screen ("Kael — Sombras de Além"). Builds the whole screen at runtime from the UI art
// (layout and behaviour from Assets_GOTY_UI/manifest_ui.json, all positions in 1920x1080 screen pixels).
public class TitleScreen : MonoBehaviour
{
    // Set by GameManager.FinishGame(): the title opens with the end credits rolling.
    public static bool RollCreditsOnStart;

    const string RollText =
        "<size=150%>KAEL</size>\n<size=80%>Sombras de Além</size>\n\n\n" +
        "<color=#5FE3F0>CRIADO POR</color>\n\nGustavo Agudo\nGuilherme Rezende\nJoão Calegari\n\n\n" +
        "<color=#5FE3F0>HISTÓRIA, ARTE, INTERFACE E ÁUDIO</color>\n\nEquipe Kael\n<size=70%>pixel art e trilha sonora sintetizadas por código</size>\n\n\n" +
        "<color=#5FE3F0>ITENS E ARMADILHAS</color>\n\nPixel Frog\n\n" +
        "<color=#5FE3F0>FONTE</color>\n\nPixelify Sans (SIL Open Font License)\n\n\n\n" +
        "O Selo das Duas Luas foi restaurado.\nA lua voltou a brilhar.\n\n\n" +
        "<size=120%>Obrigado por jogar!</size>";

    [Header("Background: Templo da Lua layers (scale 2x, window x 560..1520 of the layer)")]
    [SerializeField] Sprite sky;
    [SerializeField] Sprite far;
    [SerializeField] Sprite mid;
    [SerializeField] Sprite near;

    [Header("Title art")]
    [SerializeField] Sprite eclipse;
    [SerializeField] Sprite eclipseAdvance;
    [SerializeField] Sprite foreground;
    [SerializeField] Sprite vignette;
    [SerializeField] Sprite logo;
    [SerializeField] Sprite pressPlate;
    [SerializeField] Sprite footerPlate;
    [SerializeField] Sprite panel;
    [SerializeField] Sprite[] kaelFrames;
    [SerializeField] Sprite[] lyriaFrames;
    [SerializeField] Sprite[] particleFrames;
    [SerializeField] Sprite[] logoShineFrames;
    [SerializeField] Sprite[] selectorFrames;

    [Header("Buttons: normal, hover, pressed, disabled")]
    [SerializeField] Sprite[] primaryButton = new Sprite[4];
    [SerializeField] Sprite[] button = new Sprite[4];

    [Header("Text")]
    [SerializeField] Font font;
    [SerializeField] string footerText = "v0.1 · Equipe Kael · 2026";
    [TextArea(10, 30)]
    [SerializeField] string creditsText = "";

    static readonly Color TextColor = new Color32(0xE8, 0xE6, 0xFF, 0xFF);
    static readonly Color Cyan = new Color32(0x5F, 0xE3, 0xF0, 0xFF);

    RectTransform canvasRect, stage;
    RawImage skyImg, farImg, midImg, nearImg;
    Image eclipseImg, eclipseAdvImg, kaelImg, lyriaImg, particlesImg, logoShineImg, selectorImg, fadeImg;
    RectTransform lyriaRect, selectorRect;
    Vector2 lyriaBase;
    CanvasGroup logoGroup, pressGroup, creditsGroup, optionsGroup;
    readonly List<Button> menuButtons = new List<Button>();
    TMP_FontAsset fontAsset;
    float time, farScroll, nearScroll, nextShine = 4f;
    bool waitingKey, menuOpen, busy;
    GameObject openPanel;
    Button panelFirst;

    // ------------------------------------------------------------------ build
    void Awake()
    {
        Time.timeScale = 1f;
        GameManager.isGameOn = true;
        if (font != null) fontAsset = TMP_FontAsset.CreateFontAsset(font);

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        GameObject canvasGo = new GameObject("TitleCanvas", typeof(RectTransform));
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasRect = (RectTransform)canvasGo.transform;

        // background layers fill the whole screen; everything else lives on a centered 1920x1080 stage
        skyImg = Layer(sky);
        stage = Rect("Stage", canvasRect);
        stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(0.5f, 0.5f);
        stage.sizeDelta = new Vector2(1920, 1080);
        eclipseImg = Place(NewImage("Eclipse", stage, eclipse), 1190, 140, 422, 422);
        eclipseAdvImg = Place(NewImage("EclipseAdvance", stage, eclipseAdvance), 1190, 140, 422, 422);
        eclipseAdvImg.color = new Color(1, 1, 1, 0);
        // layers drawn over the eclipse: move the stage behind them
        farImg = Layer(far);
        midImg = Layer(mid);
        nearImg = Layer(near);
        RectTransform front = Rect("Front", canvasRect);
        front.anchorMin = front.anchorMax = front.pivot = new Vector2(0.5f, 0.5f);
        front.sizeDelta = new Vector2(1920, 1080);

        Place(NewImage("Foreground", front, foreground), 0, 0, 1920, 1080);
        kaelImg = Place(NewImage("Kael", front, Frame(kaelFrames, 0)), 1200, 512, 192, 288);
        lyriaImg = Place(NewImage("Lyria", front, Frame(lyriaFrames, 0)), 1352, 552, 48, 48);
        lyriaRect = lyriaImg.rectTransform; lyriaBase = lyriaRect.anchoredPosition;
        particlesImg = Place(NewImage("Particles", front, Frame(particleFrames, 0)), 0, 0, 1920, 1080);
        Image vig = NewImage("Vignette", canvasRect, vignette); Stretch(vig.rectTransform);

        RectTransform ui = Rect("UI", canvasRect);
        ui.anchorMin = ui.anchorMax = ui.pivot = new Vector2(0.5f, 0.5f);
        ui.sizeDelta = new Vector2(1920, 1080);

        Image logoImg = Place(NewImage("Logo", ui, logo), 100, 80, 760, 336);
        logoGroup = logoImg.gameObject.AddComponent<CanvasGroup>(); logoGroup.alpha = 0f;
        logoShineImg = Place(NewImage("LogoShine", logoImg.transform, Frame(logoShineFrames, 0)), 0, 0, 760, 336);
        logoShineImg.enabled = false;

        Image press = Place(NewImage("PressAnyKey", ui, pressPlate), 180, 760, 600, 56);
        press.type = Image.Type.Sliced;
        NewText("Text", press.transform, "Pressione qualquer tecla", 30);
        pressGroup = press.gameObject.AddComponent<CanvasGroup>(); pressGroup.alpha = 0f;

        Image foot = Place(NewImage("Footer", ui, footerPlate), 1440, 1016, 440, 40);
        foot.type = Image.Type.Sliced;
        NewText("Text", foot.transform, footerText, 20);

        menuButtons.Add(NewButton("JOGAR", ui, primaryButton, 280, 470, 400, 64, () => StartCoroutine(PlayGame())));
        menuButtons.Add(NewButton("OPÇÕES", ui, button, 280, 558, 400, 52, () => OpenPanel(optionsGroup)));
        menuButtons.Add(NewButton("CRÉDITOS", ui, button, 280, 630, 400, 52, () => OpenPanel(creditsGroup)));
        menuButtons.Add(NewButton("SAIR", ui, button, 280, 702, 400, 52, Quit));
        foreach (Button b in menuButtons) b.gameObject.SetActive(false);

        selectorImg = Place(NewImage("Selector", ui, Frame(selectorFrames, 0)), 228, 482, 36, 36);
        selectorRect = selectorImg.rectTransform;
        selectorImg.enabled = false;

        creditsGroup = BuildCredits(ui);
        optionsGroup = BuildOptions(ui);

        fadeImg = NewImage("Fade", canvasRect, null);
        Stretch(fadeImg.rectTransform);
        fadeImg.color = Color.black;
        fadeImg.raycastTarget = true;

        if (RollCreditsOnStart)
        {
            RollCreditsOnStart = false;
            StartCoroutine(EndCredits());
        }
        else
        {
            StartCoroutine(Intro());
        }
    }

    // ------------------------------------------------------------------ flow
    // End of the game: credits roll over the title background, then the menu appears.
    IEnumerator EndCredits()
    {
        Image dim = NewImage("CreditsDim", canvasRect, null);
        Stretch(dim.rectTransform);
        dim.color = new Color(0.03f, 0.03f, 0.08f, 0.8f);
        dim.transform.SetSiblingIndex(fadeImg.transform.GetSiblingIndex());   // under the fade

        RectTransform roll = Rect("CreditsRoll", dim.transform);
        roll.anchorMin = roll.anchorMax = new Vector2(0.5f, 0f);
        roll.pivot = new Vector2(0.5f, 1f);
        roll.sizeDelta = new Vector2(1200, 100);
        TMP_Text text = NewText("Text", roll, RollText, 46);
        text.alignment = TextAlignmentOptions.Top;
        text.ForceMeshUpdate();
        float height = text.preferredHeight + 40f;
        roll.sizeDelta = new Vector2(1200, height);

        yield return Fade(1f, 0f, 1.2f);
        fadeImg.raycastTarget = false;

        float screenH = canvasRect.rect.height;
        float duration = 34f, t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            roll.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, screenH + height, t / duration));
            if (t > 1.5f && AnyPressed()) break;            // skip
            yield return null;
        }

        for (float k = 0; k < 0.8f; k += Time.deltaTime)
        {
            dim.color = new Color(0.03f, 0.03f, 0.08f, Mathf.Lerp(0.8f, 0f, k / 0.8f));
            text.alpha = 1f - k / 0.8f;
            yield return null;
        }
        Destroy(dim.gameObject);
        logoGroup.alpha = 1f;
        StartCoroutine(ShowMenu());
    }

    IEnumerator Intro()
    {
        yield return Fade(1f, 0f, 0.8f);
        fadeImg.raycastTarget = false;
        for (float t = 0; t < 0.6f; t += Time.deltaTime) { logoGroup.alpha = t / 0.6f; yield return null; }
        logoGroup.alpha = 1f;
        waitingKey = true;
    }

    IEnumerator ShowMenu()
    {
        waitingKey = false;
        GameAudio.Sfx("SFX_UI_Confirmar", 0.8f, 0f);
        for (float t = 0; t < 0.2f; t += Time.deltaTime) { pressGroup.alpha = 1f - t / 0.2f; yield return null; }
        pressGroup.gameObject.SetActive(false);
        foreach (Button b in menuButtons)
        {
            b.gameObject.SetActive(true);
            StartCoroutine(SlideIn(b));
            yield return new WaitForSeconds(0.08f);
        }
        yield return new WaitForSeconds(0.25f);
        menuOpen = true;
        EventSystem.current.SetSelectedGameObject(menuButtons[0].gameObject);
    }

    IEnumerator SlideIn(Button b)
    {
        RectTransform r = (RectTransform)b.transform;
        CanvasGroup g = b.GetComponent<CanvasGroup>();
        Vector2 end = r.anchoredPosition, start = end + Vector2.down * 40f;
        for (float t = 0; t < 0.25f; t += Time.deltaTime)
        {
            float k = 1f - (1f - t / 0.25f) * (1f - t / 0.25f);   // ease out
            r.anchoredPosition = Vector2.Lerp(start, end, k);
            g.alpha = k;
            yield return null;
        }
        r.anchoredPosition = end; g.alpha = 1f;
    }

    IEnumerator PlayGame()
    {
        if (busy || !menuOpen) yield break;
        busy = true;
        menuOpen = false;
        GameAudio.Sfx("SFX_UI_IniciarJogo", 1f, 0f);
        // the eclipse advances: the journey begins
        for (float t = 0; t < 0.6f; t += Time.deltaTime)
        {
            eclipseAdvImg.color = new Color(1, 1, 1, t / 0.6f);
            yield return null;
        }
        eclipseAdvImg.color = Color.white;
        fadeImg.raycastTarget = true;
        yield return Fade(0f, 1f, 0.8f);
        GameManager.StartNewGame();
    }

    void Quit()
    {
        if (busy || !menuOpen) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    void OpenPanel(CanvasGroup g)
    {
        if (busy || !menuOpen) return;
        menuOpen = false;
        GameAudio.Sfx("SFX_UI_Confirmar", 0.8f, 0f);
        openPanel = g.gameObject;
        g.gameObject.SetActive(true);
        Button first = g.GetComponentInChildren<Button>();
        Selectable sel = g.GetComponentInChildren<Slider>();
        EventSystem.current.SetSelectedGameObject(sel != null ? sel.gameObject : first != null ? first.gameObject : null);
        panelFirst = first;
        selectorImg.enabled = false;
    }

    void ClosePanel()
    {
        if (openPanel == null) return;
        GameAudio.Sfx("SFX_UI_Voltar", 0.8f, 0f);
        GameObject closing = openPanel;
        openPanel.SetActive(false);
        openPanel = null;
        menuOpen = true;
        Button back = closing == optionsGroup.gameObject ? menuButtons[1] : menuButtons[2];
        EventSystem.current.SetSelectedGameObject(back.gameObject);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            fadeImg.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        fadeImg.color = new Color(0, 0, 0, to);
    }

    // ------------------------------------------------------------------ per frame
    void Update()
    {
        float dt = Time.deltaTime;
        time += dt;

        // parallax: far ~4 px/s, near ~16 px/s on screen (= 2 and 8 layer px/s at scale 2); mid stays still
        farScroll += 2f / 1920f * dt;
        nearScroll += 8f / 1920f * dt;
        Vector2 size = canvasRect.rect.size;
        float w = size.x / 2f / 1920f, h = size.y / 2f / 540f;
        float x0 = 1040f / 1920f - w / 2f, y0 = 0.5f - h / 2f;
        skyImg.uvRect = new Rect(x0, y0, w, h);
        farImg.uvRect = new Rect(x0 + farScroll, y0, w, h);
        midImg.uvRect = new Rect(x0 + 330f / 1920f, y0, w, h);   // keeps the tall pillar left of the moon (as in the mockup)
        nearImg.uvRect = new Rect(x0 + nearScroll, y0, w, h);

        kaelImg.sprite = Frame(kaelFrames, (int)(time * 6f));
        lyriaImg.sprite = Frame(lyriaFrames, (int)(time * 6f));
        particlesImg.sprite = Frame(particleFrames, (int)(time * 4f));
        lyriaRect.anchoredPosition = lyriaBase + Vector2.up * (Mathf.Sin(time * Mathf.PI) * 4f);

        // occasional shine across the logo (4 frames @ 12 fps every ~7 s)
        if (time >= nextShine)
        {
            float st = time - nextShine;
            int f = (int)(st * 12f);
            if (f < logoShineFrames.Length) { logoShineImg.enabled = true; logoShineImg.sprite = logoShineFrames[f]; }
            else { logoShineImg.enabled = false; nextShine = time + Random.Range(6f, 8f); }
        }

        if (waitingKey)
        {
            pressGroup.alpha = 0.5f + 0.5f * Mathf.Cos(time * Mathf.PI * 2f / 1.6f);
            if (AnyPressed()) StartCoroutine(ShowMenu());
        }

        if (openPanel != null && (Pressed(Keyboard.current?.escapeKey) || Pressed(Gamepad.current?.buttonEast)))
            ClosePanel();

        UpdateSelector();
    }

    GameObject lastSelected;

    void UpdateSelector()
    {
        GameObject sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (sel != lastSelected)
        {
            if (sel != null && lastSelected != null && (menuOpen || openPanel != null)) GameAudio.Sfx("SFX_UI_Navegar", 0.7f, 0f);
            lastSelected = sel;
        }
        Button b = sel != null ? sel.GetComponent<Button>() : null;
        bool show = menuOpen && b != null && menuButtons.Contains(b);
        selectorImg.enabled = show;
        if (!show) return;
        RectTransform r = (RectTransform)b.transform;
        selectorRect.anchoredPosition = new Vector2(r.anchoredPosition.x - 52f, r.anchoredPosition.y - (r.sizeDelta.y - 36f) / 2f);
        selectorImg.sprite = Frame(selectorFrames, (int)(time * 6f));
    }

    static bool Pressed(UnityEngine.InputSystem.Controls.ButtonControl c) => c != null && c.wasPressedThisFrame;

    static bool AnyPressed()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || Pressed(Mouse.current?.leftButton)
            || Pressed(Gamepad.current?.buttonSouth) || Pressed(Gamepad.current?.startButton);
    }

    // ------------------------------------------------------------------ panels
    CanvasGroup BuildCredits(Transform parent)
    {
        Image p = Place(NewImage("Credits", parent, panel), 510, 150, 900, 780);
        p.type = Image.Type.Tiled;
        p.raycastTarget = true;
        TMP_Text title = NewText("Title", p.transform, "CRÉDITOS", 44);
        Top(title.rectTransform, 70, 60);
        TMP_Text body = NewText("Body", p.transform, creditsText, 24);
        body.rectTransform.offsetMin = new Vector2(90, 150);
        body.rectTransform.offsetMax = new Vector2(-90, -140);
        body.alignment = TextAlignmentOptions.Top;
        Button back = NewButton("VOLTAR", p.transform, button, 250, 650, 400, 52, ClosePanel);
        back.GetComponent<CanvasGroup>().alpha = 1f;
        CanvasGroup g = p.gameObject.AddComponent<CanvasGroup>();
        p.gameObject.SetActive(false);
        return g;
    }

    CanvasGroup BuildOptions(Transform parent)
    {
        Image p = Place(NewImage("Options", parent, panel), 610, 260, 700, 520);
        p.type = Image.Type.Tiled;
        p.raycastTarget = true;
        TMP_Text title = NewText("Title", p.transform, "OPÇÕES", 44);
        Top(title.rectTransform, 70, 60);
        VolumeRow(p.transform, "SOM", "SoundVolume", GameAudio.DefaultSoundVolume, 190);
        VolumeRow(p.transform, "MÚSICA", "MusicVolume", GameAudio.DefaultMusicVolume, 280);
        Button back = NewButton("VOLTAR", p.transform, button, 150, 400, 400, 52, ClosePanel);
        back.GetComponent<CanvasGroup>().alpha = 1f;
        CanvasGroup g = p.gameObject.AddComponent<CanvasGroup>();
        p.gameObject.SetActive(false);
        return g;
    }

    // same PlayerPrefs keys as the in-game sliders (SoundVolumeSlider / MusicVolumeSlider)
    void VolumeRow(Transform parent, string label, string key, float def, float y)
    {
        RectTransform row = Rect(label, parent);
        row.anchorMin = row.anchorMax = row.pivot = new Vector2(0, 1);
        row.anchoredPosition = new Vector2(90, -y);
        row.sizeDelta = new Vector2(520, 50);
        TMP_Text t = NewText("Label", row, label, 30);
        t.alignment = TextAlignmentOptions.Left;
        t.rectTransform.offsetMax = new Vector2(-330, 0);

        RectTransform sr = Rect("Slider", row);
        sr.anchorMin = new Vector2(0, 0.5f); sr.anchorMax = new Vector2(1, 0.5f);
        sr.offsetMin = new Vector2(200, -12); sr.offsetMax = new Vector2(0, 12);
        Image track = sr.gameObject.AddComponent<Image>();
        track.color = new Color32(0x1B, 0x1F, 0x3E, 0xFF);
        Outline o = sr.gameObject.AddComponent<Outline>(); o.effectColor = new Color32(0x7C, 0x80, 0xAD, 0xFF); o.effectDistance = new Vector2(4, -4);

        RectTransform fillArea = Rect("Fill Area", sr); Stretch(fillArea);
        RectTransform fill = Rect("Fill", fillArea); Stretch(fill);
        fill.gameObject.AddComponent<Image>().color = new Color32(0x2A, 0x9F, 0xB2, 0xFF);

        RectTransform handleArea = Rect("Handle Area", sr); Stretch(handleArea);
        RectTransform handle = Rect("Handle", handleArea);
        handle.sizeDelta = new Vector2(24, 36);
        Image hi = handle.gameObject.AddComponent<Image>(); hi.color = TextColor;

        Slider s = sr.gameObject.AddComponent<Slider>();
        s.fillRect = fill; s.handleRect = handle; s.targetGraphic = hi;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 1f;
        ColorBlock cb = s.colors; cb.highlightedColor = Cyan; cb.selectedColor = Cyan; s.colors = cb;
        s.value = PlayerPrefs.GetFloat(key, def);
        s.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(key, v));
        sr.gameObject.AddComponent<UIPointerSelect>();
    }

    // ------------------------------------------------------------------ helpers
    Button NewButton(string label, Transform parent, Sprite[] states, float x, float y, float w, float h, System.Action onClick)
    {
        Image img = Place(NewImage(label, parent, states[0]), x, y, w, h);
        img.type = Image.Type.Sliced;
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.SpriteSwap;
        b.spriteState = new SpriteState
        {
            highlightedSprite = states[1], selectedSprite = states[1],
            pressedSprite = states[2], disabledSprite = states[3]
        };
        b.onClick.AddListener(() => onClick());
        NewText("Label", img.transform, label, 34);      // first child: moved by UIButtonPressOffset
        img.gameObject.AddComponent<UIButtonPressOffset>();
        img.gameObject.AddComponent<UIPointerSelect>();
        CanvasGroup g = img.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        return b;
    }

    RawImage Layer(Sprite s)
    {
        RectTransform r = Stretch(Rect(s != null ? s.name : "Layer", canvasRect));
        RawImage img = r.gameObject.AddComponent<RawImage>();
        if (s != null) img.texture = s.texture;
        img.raycastTarget = false;
        return img;
    }

    Image NewImage(string name, Transform parent, Sprite s)
    {
        Image img = Rect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = s;
        img.raycastTarget = false;
        return img;
    }

    TMP_Text NewText(string name, Transform parent, string text, float size)
    {
        RectTransform r = Stretch(Rect(name, parent));
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) t.font = fontAsset;
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = TextColor;
        t.raycastTarget = false;
        return t;
    }

    static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static RectTransform Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }

    static void Top(RectTransform r, float y, float h)
    {
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1);
        r.anchoredPosition = new Vector2(0, -y + h * 0.5f - h * 0.5f);
        r.offsetMin = new Vector2(0, -y - h); r.offsetMax = new Vector2(0, -y);
    }

    // x, y = top-left corner in 1920x1080 stage pixels (as in the manifest)
    static Image Place(Image img, float x, float y, float w, float h)
    {
        RectTransform r = img.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y);
        r.sizeDelta = new Vector2(w, h);
        return img;
    }

    static Sprite Frame(Sprite[] frames, int i) => frames == null || frames.Length == 0 ? null : frames[i % frames.Length];
}
