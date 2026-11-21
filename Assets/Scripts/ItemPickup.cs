using UnityEngine;

/// <summary>
/// An item the player can pick up with E (e.g. the carved stone panel on the
/// fountain). Picking it up plays the "item found" reveal, fills its slot in the
/// item bar, then shows a short info card about it.
///
/// If <see cref="oneSided"/> is on, it can only be picked up from the side this
/// object's blue (forward) arrow points to.
/// </summary>
public class ItemPickup : Interactable
{
    public string itemName = "Carved Stone Panel";

    [Tooltip("Item bar box this item fills: 0 = first (left) ... 4 = last.")]
    [Range(0, ItemBar.SlotCount - 1)] public int slot;

    [Tooltip("Big picture shown in the middle of the screen when found.")]
    public Sprite revealImage;

    [Tooltip("Picture in the item bar once found. Empty = the stone box.")]
    public Sprite barIcon;

    public AudioClip foundSound;

    [Header("Info card")]
    public string infoTitle = "Carved Stone Panel";
    [TextArea(3, 6)]
    public string infoText =
        "In late-Ottoman Beirut, houses were built from blocks of local sandstone. " +
        "Masons carved the finest blocks with flowers, leaves and scrolls, like this panel, " +
        "and set them above doors, windows and fountains as decoration.";

    [Tooltip("Only usable from the side the forward arrow points to.")]
    public bool oneSided = true;

    bool collected;

    public override string Prompt => $"Pick up the {itemName.ToLower()}";

    public override bool CanInteract(Vector3 playerPosition)
    {
        if (collected || (ItemBar.Instance != null && ItemBar.Instance.IsFound(slot))) return false;
        if (!oneSided) return true;
        Vector3 offset = playerPosition - transform.position;
        offset.y = 0f;
        return Vector3.Dot(offset, transform.forward) > 0f;
    }

    public override void Interact()
    {
        if (collected) return;
        collected = true;

        void Finish()
        {
            ItemBar.Instance?.Reveal(slot, barIcon);
            ItemRevealUI.Instance?.ShowInfo(infoTitle, infoText);
        }

        if (ItemRevealUI.Instance != null)
            ItemRevealUI.Instance.Play(revealImage, $"ITEM {slot + 1} FOUND!", itemName, foundSound, slot, Finish);
        else
            Finish();
    }
}
