using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// "Item found!" moment: the item pops up big in the middle of the screen with a
/// spinning light burst and a sound, then flies down into its slot in the item
/// bar (2 seconds in total). Afterwards a small info card explains the item;
/// the player closes it with E (or it closes by itself).
/// Creates itself when the game starts, like the item bar.
/// </summary>
public class ItemRevealUI : MonoBehaviour
{
    public static ItemRevealUI Instance { get; private set; }

    /// <summary>True while the reveal or the info card is on screen.</summary>
    public bool Busy => revealing || infoOpen;

    const float Duration = 2f;           // whole reveal animation
    const float InfoAutoCloseSeconds = 20f;

    Font font;
    AudioSource audioSource;
    RectTransform root;
    Image dim, rays, glow, item;
    Text title, itemName;
    CanvasGroup group;

    GameObject infoPanel;
    CanvasGroup infoGroup;
    Text infoTitle, infoBody;

    bool revealing, infoOpen;
    int infoOpenedFrame;
    float infoCloseTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateOnGameStart()
    {
        if (Instance == null) new GameObject("ItemRevealUI").AddComponent<ItemRevealUI>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- building the UI ----------

    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120; // above the HUD and the item bar
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root = NewRect("Reveal", transform);
        Stretch(root);
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        dim = NewImage("Dim", root, null, new Color(0, 0, 0, 0.6f));
        Stretch(dim.rectTransform);

        rays = NewImage("Rays", root, MakeSprite(MakeRaysTexture()), new Color(1f, 0.85f, 0.45f, 0.55f));
        rays.rectTransform.sizeDelta = new Vector2(1100, 1100);

        glow = NewImage("Glow", root, MakeSprite(MakeGlowTexture()), new Color(1f, 0.9f, 0.6f, 0.85f));
        glow.rectTransform.sizeDelta = new Vector2(820, 820);

        item = NewImage("Item", root, null, Color.white);
        item.preserveAspect = true;
        item.rectTransform.sizeDelta = new Vector2(600, 450);

        title = NewText("Title", root, 64, new Color(1f, 0.84f, 0.35f), FontStyle.Bold);
        title.rectTransform.sizeDelta = new Vector2(1200, 90);
        title.rectTransform.anchoredPosition = new Vector2(0, 330);
        AddOutline(title.gameObject, 3f);

        itemName = NewText("ItemName", root, 40, Color.white, FontStyle.Bold);
        itemName.rectTransform.sizeDelta = new Vector2(1200, 60);
        itemName.rectTransform.anchoredPosition = new Vector2(0, -290);
        AddOutline(itemName.gameObject, 2f);

        root.gameObject.SetActive(false);

        // Small info card (shown after the reveal), just above the item bar.
        infoPanel = new GameObject("InfoCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        infoPanel.transform.SetParent(transform, false);
        var ir = (RectTransform)infoPanel.transform;
        ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 0f);
        ir.pivot = new Vector2(0.5f, 0f);
        ir.sizeDelta = new Vector2(760, 190);
        ir.anchoredPosition = new Vector2(0, 180);
        var bg = infoPanel.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.05f, 0.9f);
        bg.raycastTarget = false;
        infoGroup = infoPanel.GetComponent<CanvasGroup>();

        infoTitle = NewText("Title", ir, 26, new Color(0.95f, 0.78f, 0.45f), FontStyle.Bold);
        infoTitle.alignment = TextAnchor.UpperLeft;
        Inset(infoTitle.rectTransform, 24, 14, 24, 140);
        infoBody = NewText("Body", ir, 22, Color.white, FontStyle.Normal);
        infoBody.alignment = TextAnchor.UpperLeft;
        Inset(infoBody.rectTransform, 24, 50, 24, 34);
        var close = NewText("Close", ir, 18, new Color(1, 1, 1, 0.6f), FontStyle.Normal);
        close.alignment = TextAnchor.LowerRight;
        close.text = "[E] close";
        Inset(close.rectTransform, 24, 150, 20, 10);
        infoPanel.SetActive(false);
    }

    RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var r = NewRect(name, parent);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Text NewText(string name, Transform parent, int size, Color color, FontStyle style)
    {
        var r = NewRect(name, parent);
        var t = r.gameObject.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void AddOutline(GameObject go, float size)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor = new Color(0, 0, 0, 0.85f);
        o.effectDistance = new Vector2(size, -size);
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static void Inset(RectTransform r, float left, float top, float right, float bottom)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(-right, -top);
    }

    static Sprite MakeSprite(Texture2D tex) =>
        Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

    static Texture2D MakeRaysTexture()
    {
        const int n = 256; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - n / 2f) / (n / 2f), dy = (y - n / 2f) / (n / 2f);
                float r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
                float ray = Mathf.SmoothStep(0.55f, 0.95f, Mathf.Cos(a * 14f) * 0.5f + 0.5f);
                float fade = Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 4f);
                tex.SetPixel(x, y, new Color(1, 1, 1, ray * fade));
            }
        tex.Apply();
        return tex;
    }

    static Texture2D MakeGlowTexture()
    {
        const int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - n / 2f) / (n / 2f), dy = (y - n / 2f) / (n / 2f);
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - r), 2.2f)));
            }
        tex.Apply();
        return tex;
    }

    // ---------- playing ----------

    /// <summary>Plays the reveal; <paramref name="flyToSlot"/> is the item-bar slot it flies into.</summary>
    public void Play(Sprite image, string heading, string name, AudioClip sound, int flyToSlot, Action onFinished)
    {
        if (revealing) return;
        StartCoroutine(Reveal(image, heading, name, sound, flyToSlot, onFinished));
    }

    IEnumerator Reveal(Sprite image, string heading, string name, AudioClip sound, int slot, Action onFinished)
    {
        revealing = true;
        var player = FirstPersonController.Instance;
        if (player != null) player.InputLocked = true;
        GameHUD.Instance?.SetPrompt(null);

        item.sprite = image;
        title.text = heading;
        itemName.text = name;
        root.gameObject.SetActive(true);
        if (sound != null) audioSource.PlayOneShot(sound);

        // Where the slot is on screen (same layout as ItemBar), relative to the centre.
        Vector2 slotPos = new Vector2(-280f + 12f + 48f + slot * (96f + 14f), -540f + 28f + 12f + 48f);

        float t = 0f;
        while (t < Duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / Duration);

            // Background & light burst
            float appear = EaseOutCubic(Mathf.Clamp01(t / 0.3f));
            float leave = Mathf.Clamp01((t - 1.5f) / 0.5f);           // last 0.5 s
            dim.color = new Color(0, 0, 0, 0.6f * appear * (1f - leave));
            rays.rectTransform.localEulerAngles = new Vector3(0, 0, -t * 90f);
            rays.rectTransform.localScale = Vector3.one * (EaseOutBack(Mathf.Clamp01(t / 0.45f)) * (1f + 0.05f * Mathf.Sin(t * 9f)));
            rays.color = new Color(1f, 0.85f, 0.45f, 0.55f * (1f - leave));
            float pulse = 1f + 0.08f * Mathf.Sin(t * 12f);
            glow.rectTransform.localScale = Vector3.one * EaseOutCubic(Mathf.Clamp01(t / 0.25f)) * pulse * (1f - 0.6f * leave);
            glow.color = new Color(1f, 0.9f, 0.6f, 0.85f * (1f - leave));

            // Item: pop in with overshoot and a twist, bob, then fly into the bar slot
            float pop = EaseOutBack(Mathf.Clamp01(t / 0.45f), 2.2f);
            float twist = Mathf.Lerp(-18f, 0f, EaseOutElastic(Mathf.Clamp01(t / 0.7f)));
            Vector2 pos = new Vector2(0, 10f * Mathf.Sin(t * 5f));
            float scale = pop;
            if (leave > 0f)
            {
                float f = EaseInCubic(leave);
                pos = Vector2.Lerp(pos, slotPos, f);
                scale = Mathf.Lerp(pop, 96f / 450f, f);
                twist = Mathf.Lerp(twist, 360f, f);
            }
            item.rectTransform.anchoredPosition = pos;
            item.rectTransform.localScale = Vector3.one * scale;
            item.rectTransform.localEulerAngles = new Vector3(0, 0, twist);

            // Text: punch in, then fade
            float tp = Mathf.Clamp01((t - 0.15f) / 0.35f);
            title.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, EaseOutBack(tp));
            title.color = new Color(1f, 0.84f, 0.35f, tp * (1f - leave));
            float nt = Mathf.Clamp01((t - 0.35f) / 0.3f);
            itemName.rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(-360f, -290f, EaseOutCubic(nt)));
            itemName.color = new Color(1, 1, 1, nt * (1f - leave));

            yield return null;
        }

        root.gameObject.SetActive(false);
        if (player != null) player.InputLocked = false;
        revealing = false;
        onFinished?.Invoke();
    }

    /// <summary>Shows the small info card; the player closes it with E.</summary>
    public void ShowInfo(string heading, string body)
    {
        infoTitle.text = heading;
        infoBody.text = body;
        infoPanel.SetActive(true);
        infoOpen = true;
        infoOpenedFrame = Time.frameCount;
        infoCloseTime = Time.unscaledTime + InfoAutoCloseSeconds;
        StartCoroutine(FadeInfo(0f, 1f, 0.25f));
    }

    IEnumerator FadeInfo(float from, float to, float seconds)
    {
        var r = (RectTransform)infoPanel.transform;
        for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
        {
            float u = EaseOutCubic(t / seconds);
            infoGroup.alpha = Mathf.Lerp(from, to, u);
            r.anchoredPosition = new Vector2(0, Mathf.Lerp(from < to ? 150f : 180f, from < to ? 180f : 150f, u));
            yield return null;
        }
        infoGroup.alpha = to;
        if (to == 0f) infoPanel.SetActive(false);
    }

    void Update()
    {
        if (!infoOpen) return;
        var kb = Keyboard.current;
        bool close = kb != null && kb.eKey.wasPressedThisFrame && Time.frameCount > infoOpenedFrame;
        if (close || Time.unscaledTime > infoCloseTime)
        {
            infoOpen = false;
            StartCoroutine(FadeInfo(1f, 0f, 0.2f));
        }
    }

    // ---------- easing ----------
    static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    static float EaseInCubic(float x) => x * x * x;
    static float EaseOutBack(float x, float s = 1.70158f) { float c3 = s + 1f; return 1f + c3 * Mathf.Pow(x - 1f, 3f) + s * Mathf.Pow(x - 1f, 2f); }
    static float EaseOutElastic(float x)
    {
        if (x <= 0f) return 0f; if (x >= 1f) return 1f;
        return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
    }
}
