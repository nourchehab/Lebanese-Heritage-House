using UnityEngine;

/// <summary>
/// A door the player uses with E. The scanned doors are solid, so instead of
/// opening, it moves the player to <see cref="destination"/> (an empty object
/// placed on the other side, facing the way the player should look).
/// </summary>
public class DoorPortal : Interactable
{
    [Tooltip("Shown after [E], e.g. \"Go inside\".")]
    public string promptText = "Go through the door";

    [Tooltip("Where the player arrives. Its blue (forward) arrow is the direction they will face.")]
    public Transform destination;

    [Tooltip("Background ambience volume after going through (1 = full, 0.3 = 30%). -1 leaves it unchanged.")]
    public float ambienceVolume = -1f;

    [Tooltip("Optional: lock the door until all items are found.")]
    public bool requiresAllItems;
    public string lockedMessage = "The door is locked. Maybe the key will open it.";

    [Tooltip("Only usable from the side this object's blue (forward) arrow points to, so the door can't be used through the wall.")]
    public bool oneSided = true;

    public override string Prompt => promptText;

    public override bool CanInteract(Vector3 playerPosition)
    {
        if (!oneSided) return true;
        Vector3 offset = playerPosition - transform.position;
        offset.y = 0f;
        return Vector3.Dot(offset, transform.forward) > 0f;
    }

    public override void Interact()
    {
        if (destination == null || FirstPersonController.Instance == null) return;

        if (requiresAllItems && (ItemBar.Instance == null || !ItemBar.Instance.AllFound))
        {
            GameHUD.Instance?.ShowDialogue(new[] { new GameHUD.Line("", lockedMessage) });
            return;
        }

        FirstPersonController.Instance.TeleportTo(destination.position, destination.eulerAngles.y);
        if (ambienceVolume >= 0f && AmbientAudio.Instance != null)
            AmbientAudio.Instance.SetAreaVolume(ambienceVolume);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (destination == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(FocusPosition, destination.position);
        Gizmos.DrawRay(destination.position + Vector3.up, destination.forward * 0.6f);
    }
}
