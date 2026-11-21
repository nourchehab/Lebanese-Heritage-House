using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the player: finds the nearest <see cref="Interactable"/> in front of the
/// camera, shows "[E] ..." on screen, and calls Interact() when E is pressed.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("How directly the player must look at something (1 = exactly, 0 = 90° off).")]
    [Range(0f, 1f)] public float facingThreshold = 0.6f;

    Camera cam;
    Interactable current;

    void Awake() => cam = GetComponentInChildren<Camera>();

    void Update()
    {
        var hud = GameHUD.Instance;
        if (hud == null) return;

        // While a dialogue, item reveal or info card is open, they handle input.
        if (hud.DialogueOpen || (ItemRevealUI.Instance != null && ItemRevealUI.Instance.Busy))
        {
            hud.SetPrompt(null);
            return;
        }

        current = FindTarget();
        hud.SetPrompt(current != null ? current.Prompt : null);

        var keyboard = Keyboard.current;
        if (current != null && keyboard != null && keyboard.eKey.wasPressedThisFrame)
            current.Interact();
    }

    Interactable FindTarget()
    {
        if (cam == null) return null;
        Vector3 eye = cam.transform.position;
        Vector3 look = cam.transform.forward;
        Interactable best = null;
        float bestScore = float.MinValue;

        foreach (var it in Interactable.All)
        {
            if (!it.CanInteract(transform.position)) continue;
            Vector3 to = it.FocusPosition - eye;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            if (flat.magnitude > it.radius) continue;

            float facing = Vector3.Dot(new Vector3(look.x, 0f, look.z).normalized, flat.normalized);
            if (flat.magnitude > 0.4f && facing < facingThreshold) continue;

            float score = facing - flat.magnitude * 0.2f;
            if (score > bestScore) { bestScore = score; best = it; }
        }
        return best;
    }
}
