using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// On-screen HUD: crosshair, "[E] ..." prompt, controls hint and the dialogue box.
/// Creates itself when the game starts (like the item bar), so it needs no setup.
///
///   GameHUD.Instance.ShowDialogue(lines, onFinished);
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Serializable]
    public struct Line
    {
        public string speaker;
        [TextArea(2, 5)] public string text;
        public Line(string speaker, string text) { this.speaker = speaker; this.text = text; }
    }

    public static GameHUD Instance { get; private set; }

    public bool DialogueOpen => dialoguePanel != null && dialoguePanel.activeSelf;

    Font font;
    Text promptText, hintText, speakerText, bodyText, continueText;
    GameObject dialoguePanel, promptPanel;
    readonly List<Line> lines = new List<Line>();
    int lineIndex;
    Action onFinished;
    float hintHideTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateOnGameStart()
    {
        if (Instance == null) new GameObject("GameHUD").AddComponent<GameHUD>();
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

    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // under the item bar (100)
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Crosshair dot
        var dot = Panel("Crosshair", transform, new Vector2(0.5f, 0.5f), new Vector2(6, 6), Vector2.zero, new Color(1, 1, 1, 0.8f));

        // Interaction prompt, just above the item bar
        promptPanel = Panel("Prompt", transform, new Vector2(0.5f, 0f), new Vector2(520, 48), new Vector2(0, 190), new Color(0, 0, 0, 0.55f));
        promptText = Label("Text", promptPanel.transform, 26, TextAnchor.MiddleCenter, Color.white);
        promptPanel.SetActive(false);

        // Controls hint, top-left, fades after a while
        var hintPanel = Panel("ControlsHint", transform, new Vector2(0f, 1f), new Vector2(560, 44), new Vector2(300, -40), new Color(0, 0, 0, 0.45f));
        hintText = Label("Text", hintPanel.transform, 20, TextAnchor.MiddleCenter, Color.white);
        hintText.text = "WASD walk  ·  Mouse look  ·  Shift faster  ·  E interact  ·  Esc free mouse";
        hintHideTime = Time.time + 12f;

        // Dialogue box, above the item bar
        dialoguePanel = Panel("Dialogue", transform, new Vector2(0.5f, 0f), new Vector2(1100, 230), new Vector2(0, 175), new Color(0.08f, 0.06f, 0.05f, 0.88f));
        speakerText = Label("Speaker", dialoguePanel.transform, 28, TextAnchor.UpperLeft, new Color(0.95f, 0.78f, 0.45f));
        Inset(speakerText.rectTransform, 30, 18, 30, 180);
        speakerText.fontStyle = FontStyle.Bold;
        bodyText = Label("Body", dialoguePanel.transform, 27, TextAnchor.UpperLeft, Color.white);
        Inset(bodyText.rectTransform, 30, 62, 30, 44);
        continueText = Label("Continue", dialoguePanel.transform, 20, TextAnchor.LowerRight, new Color(1, 1, 1, 0.6f));
        Inset(continueText.rectTransform, 30, 180, 26, 14);
        dialoguePanel.SetActive(false);
    }

    GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, anchor.y == 0f ? 0f : anchor.y == 1f ? 1f : 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    Text Label(string name, Transform parent, int size, TextAnchor align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var t = go.GetComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = align; t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Inset(RectTransform r, float left, float top, float right, float bottom)
    {
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(-right, -top);
    }

    public void SetPrompt(string text)
    {
        bool show = !string.IsNullOrEmpty(text);
        if (promptPanel.activeSelf != show) promptPanel.SetActive(show);
        if (show) promptText.text = "[E]  " + text;
    }

    public void ShowDialogue(IList<Line> newLines, Action finished = null)
    {
        if (newLines == null || newLines.Count == 0) { finished?.Invoke(); return; }
        lines.Clear();
        lines.AddRange(newLines);
        lineIndex = 0;
        onFinished = finished;
        openedFrame = Time.frameCount;
        dialoguePanel.SetActive(true);
        SetPrompt(null);
        if (FirstPersonController.Instance != null) FirstPersonController.Instance.InputLocked = true;
        ShowLine();
    }

    void ShowLine()
    {
        var line = lines[lineIndex];
        speakerText.text = line.speaker;
        bodyText.text = line.text;
        bool last = lineIndex == lines.Count - 1;
        continueText.text = $"{lineIndex + 1}/{lines.Count}    [E] / click  {(last ? "close" : "next")}";
    }

    void Update()
    {
        if (hintText != null && hintText.transform.parent.gameObject.activeSelf && Time.time > hintHideTime)
            hintText.transform.parent.gameObject.SetActive(false);

        if (!DialogueOpen) return;
        var kb = Keyboard.current; var mouse = Mouse.current;
        bool next = (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                 || (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked);
        // Ignore the same E press that opened the dialogue.
        if (next && Time.frameCount > openedFrame) Advance();
    }

    int openedFrame;

    void Advance()
    {
        lineIndex++;
        if (lineIndex < lines.Count) { ShowLine(); return; }
        dialoguePanel.SetActive(false);
        if (FirstPersonController.Instance != null) FirstPersonController.Instance.InputLocked = false;
        var done = onFinished; onFinished = null;
        done?.Invoke();
    }
}
