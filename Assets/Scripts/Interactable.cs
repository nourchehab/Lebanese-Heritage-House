using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for things the player can use with E (the housekeeper, doors...).
/// The player can interact when within <see cref="radius"/> and roughly facing it.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> All = new List<Interactable>();

    [Tooltip("How close the player must be (metres).")]
    public float radius = 1.8f;

    [Tooltip("Optional: point the player must face. Defaults to this object.")]
    public Transform focusPoint;

    /// <summary>Text after "[E]" in the on-screen prompt.</summary>
    public abstract string Prompt { get; }

    public abstract void Interact();

    /// <summary>Extra condition for using it (e.g. which side of a wall the player is on).</summary>
    public virtual bool CanInteract(Vector3 playerPosition) => true;

    public Vector3 FocusPosition => focusPoint != null ? focusPoint.position : transform.position;

    protected virtual void OnEnable() => All.Add(this);
    protected virtual void OnDisable() => All.Remove(this);

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(FocusPosition, radius);
    }
}
