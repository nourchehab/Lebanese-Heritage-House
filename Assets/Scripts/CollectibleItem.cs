using UnityEngine;

/// <summary>
/// Put this on a hidden object in a room (a stone, a key...). When the player
/// walks into it, it disappears and its slot in the item bar is revealed.
///
/// Setup: the object needs a Collider (this script ticks "Is Trigger" for you),
/// and your player object must have the "Player" tag plus a Rigidbody or
/// CharacterController so trigger events fire. Other scripts (e.g. a click or
/// interact system) can also call Collect() directly.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    [Tooltip("Which box in the bar this item fills: 0 = first (left) ... 4 = last (right).")]
    [Range(0, ItemBar.SlotCount - 1)]
    public int slot;

    [Tooltip("Picture shown in the bar once found. Leave empty to use the stone picture.")]
    public Sprite icon;

    [Tooltip("Tag of the player object.")]
    public string playerTag = "Player";

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            Collect();
    }

    public void Collect()
    {
        if (ItemBar.Instance != null)
            ItemBar.Instance.Reveal(slot, icon);
        gameObject.SetActive(false);
    }
}
