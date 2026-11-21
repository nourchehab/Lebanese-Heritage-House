using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Always-visible item bar at the bottom-centre of the screen.
/// Shows a "?" box for every item the player hasn't found yet, and the item's
/// picture once it is found. The last (5th) box is the key and always shows
/// the key picture.
///
/// You don't need to add this to any scene: it creates itself when the game
/// starts and stays on screen when you load other scenes (courtyard, connecting
/// room, main room).
///
/// From other scripts:
///   ItemBar.Instance.Reveal(2);                 // slot 3 shows the stone picture
///   ItemBar.Instance.Reveal(0, keySprite);      // slot 1 shows a custom picture
///   ItemBar.Instance.RevealNext(someSprite);    // fills the first empty slot
///   ItemBar.Instance.AllFound                   // true once all 5 are found
///   ItemBar.Instance.ItemFound += slot => {...} // react when something is found
/// </summary>
public class ItemBar : MonoBehaviour
{
    public const int SlotCount = 5;

    public static ItemBar Instance { get; private set; }

    /// <summary>Raised with the slot index (0-4) each time an item is found.</summary>
    public event Action<int> ItemFound;

    /// <summary>Raised once, when the last item is found.</summary>
    public event Action AllItemsFound;

    // Sizes are in "reference pixels" for a 1920x1080 screen; the bar scales
    // automatically with the actual resolution.
    const float SlotSize = 96f;
    const float Spacing = 14f;
    const float BottomMargin = 28f;
    const float Padding = 12f;

    Image[] slotImages;
    bool[] found;
    Sprite unknownSprite;
    Sprite defaultFoundSprite;
    Sprite keySprite;

    /// <summary>The last box is the key: it always shows the key picture instead of a "?".</summary>
    public const int KeySlot = SlotCount - 1;

    Sprite EmptySpriteFor(int slot) => slot == KeySlot ? keySprite : unknownSprite;

    public int FoundCount
    {
        get
        {
            int n = 0;
            foreach (bool f in found) if (f) n++;
            return n;
        }
    }

    public bool AllFound => FoundCount == SlotCount;

    public bool IsFound(int slot) => slot >= 0 && slot < SlotCount && found[slot];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateOnGameStart()
    {
        if (Instance == null)
            new GameObject("ItemBar").AddComponent<ItemBar>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
        unknownSprite = Resources.Load<Sprite>("ItemBoxes/unknown_itembox");
        defaultFoundSprite = Resources.Load<Sprite>("ItemBoxes/stone_itembox");
        keySprite = Resources.Load<Sprite>("ItemBoxes/key_itembox");
        if (unknownSprite == null)
            Debug.LogWarning("ItemBar: Resources/ItemBoxes/unknown_itembox is missing or not imported as a Sprite.");

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // draw on top of other UI

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Bar background: a soft dark strip behind the boxes.
        var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(transform, false);
        var barRect = (RectTransform)bar.transform;
        barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        float barWidth = SlotCount * SlotSize + (SlotCount - 1) * Spacing + Padding * 2;
        barRect.sizeDelta = new Vector2(barWidth, SlotSize + Padding * 2);
        barRect.anchoredPosition = new Vector2(0f, BottomMargin);
        var barImage = bar.GetComponent<Image>();
        barImage.color = new Color(0f, 0f, 0f, 0.35f);
        barImage.raycastTarget = false;

        slotImages = new Image[SlotCount];
        found = new bool[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            var slot = new GameObject($"Slot{i + 1}", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(bar.transform, false);
            var r = (RectTransform)slot.transform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.sizeDelta = new Vector2(SlotSize, SlotSize);
            r.anchoredPosition = new Vector2(Padding + i * (SlotSize + Spacing), 0f);

            var img = slot.GetComponent<Image>();
            img.sprite = EmptySpriteFor(i);
            img.preserveAspect = true;
            img.raycastTarget = false;
            slotImages[i] = img;
        }
    }

    /// <summary>Marks slot (0-4) as found and shows its picture (stone by default).</summary>
    public void Reveal(int slot, Sprite itemSprite = null)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            Debug.LogWarning($"ItemBar: slot {slot} is out of range (0-{SlotCount - 1}).");
            return;
        }
        if (found[slot]) return;

        found[slot] = true;
        slotImages[slot].sprite = itemSprite != null ? itemSprite
            : slot == KeySlot ? keySprite
            : defaultFoundSprite;
        ItemFound?.Invoke(slot);
        if (AllFound) AllItemsFound?.Invoke();
    }

    /// <summary>Fills the first slot that is still a "?". Returns the slot used, or -1 if full.</summary>
    public int RevealNext(Sprite itemSprite = null)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (!found[i])
            {
                Reveal(i, itemSprite);
                return i;
            }
        }
        return -1;
    }

    /// <summary>Turns every slot back into a "?" (e.g. for a new game).</summary>
    public void ResetAll()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            found[i] = false;
            slotImages[i].sprite = EmptySpriteFor(i);
        }
    }
}
