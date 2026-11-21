using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person player: WASD to walk, mouse to look, Shift to walk faster.
/// Esc frees the mouse cursor; click in the game view to capture it again.
/// Stops at ledges (e.g. the edge of the courtyard paving) instead of falling off.
/// Needs a CharacterController on the same object and a Camera as a child.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public static FirstPersonController Instance { get; private set; }

    [Header("Movement")]
    public float walkSpeed = 2.2f;
    public float runSpeed = 4.0f;
    public float gravity = 15f;

    [Header("Looking")]
    [Tooltip("Degrees turned per pixel of mouse movement.")]
    public float mouseSensitivity = 0.12f;
    public float maxPitch = 85f;
    public Transform cameraPivot;

    [Header("Ledges")]
    [Tooltip("Stop the player at drops deeper than this (metres).")]
    public float maxDrop = 0.6f;
    public LayerMask groundMask = ~0;

    /// <summary>When true (e.g. during dialogue) the player can't move or look.</summary>
    public bool InputLocked { get; set; }

    CharacterController controller;
    float pitch;
    float verticalVelocity;
    bool wasLocked;
    int ignoreLookFrames;

    void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
        if (cameraPivot == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraPivot = cam.transform;
        }
    }

    void Start() => SetCursorCaptured(true);

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void SetCursorCaptured(bool captured)
    {
        Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !captured;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            SetCursorCaptured(false);
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            SetCursorCaptured(true);

        bool canControl = !InputLocked && Cursor.lockState == CursorLockMode.Locked;

        // Ignore mouse movement for a few frames after the cursor is captured:
        // the cursor jumping to the screen centre would otherwise spin the view.
        bool locked = Cursor.lockState == CursorLockMode.Locked;
        if (locked && !wasLocked) ignoreLookFrames = 3;
        wasLocked = locked;

        // Look
        if (canControl && mouse != null)
        {
            Vector2 raw = mouse.delta.ReadValue();
            if (ignoreLookFrames > 0) { ignoreLookFrames--; raw = Vector2.zero; }
            raw = Vector2.ClampMagnitude(raw, 200f); // drop sudden spikes
            Vector2 delta = raw * mouseSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
            if (cameraPivot != null) cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        // Walk
        Vector3 move = Vector3.zero;
        if (canControl && keyboard != null)
        {
            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            Vector3 input = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
            float speed = keyboard.leftShiftKey.isPressed ? runSpeed : walkSpeed;
            move = transform.TransformDirection(input) * speed * Time.deltaTime;
        }

        // Don't walk off ledges: only allow the step if there is ground below it.
        if (move.sqrMagnitude > 0f && controller.isGrounded && !HasGroundAt(transform.position + move))
        {
            // Try sliding along the ledge one axis at a time.
            Vector3 alongX = new Vector3(move.x, 0f, 0f), alongZ = new Vector3(0f, 0f, move.z);
            if (HasGroundAt(transform.position + alongX)) move = alongX;
            else if (HasGroundAt(transform.position + alongZ)) move = alongZ;
            else move = Vector3.zero;
        }

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity -= gravity * Time.deltaTime;
        move.y = verticalVelocity * Time.deltaTime;

        controller.Move(move);
    }

    bool HasGroundAt(Vector3 position)
    {
        // Feet of the capsule at the candidate position.
        Vector3 feet = position + controller.center - Vector3.up * (controller.height * 0.5f);
        Vector3 origin = feet + Vector3.up * (controller.stepOffset + 0.05f);
        float distance = controller.stepOffset + 0.05f + maxDrop;
        return Physics.Raycast(origin, Vector3.down, distance, groundMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Moves the player instantly (used by doors), facing the given direction.</summary>
    public void TeleportTo(Vector3 position, float yaw)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        verticalVelocity = 0f;
        controller.enabled = true;
    }
}
