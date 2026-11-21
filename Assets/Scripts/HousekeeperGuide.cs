using UnityEngine;

/// <summary>
/// The gatekeeper who meets the player in the courtyard. Walk up to him and
/// press E: he and the player have a short exchange, then he walks away
/// towards <see cref="exitPoint"/> and disappears.
///
/// The lines are editable in the Inspector. Leave a line's speaker empty to use
/// <see cref="speakerName"/>.
/// </summary>
public class HousekeeperGuide : Interactable
{
    public string speakerName = "Gatekeeper";

    [Tooltip("Turn to face the player while talking.")]
    public bool facePlayer = true;

    [Tooltip("Show the speaker's name floating above his head.")]
    public bool showNameLabel = true;
    public float labelHeight = 2.05f;

    public GameHUD.Line[] lines =
    {
        new GameHUD.Line("", "Welcome! Come in, quickly. Listen: you only have one hour to find the key and leave. And no one should know about this."),
        new GameHUD.Line("You", "Okay, don't worry. But please, I need some information so I know what I'm supposed to look for."),
        new GameHUD.Line("", "All I can tell you is that the owner of this house was Assem Salam. That's all I know. Be quick."),
    };

    [Header("Leaving")]
    [Tooltip("Where he walks to after talking. He disappears when he gets there.")]
    public Transform exitPoint;
    public float walkSpeed = 1.3f;

    TextMesh nameLabel;
    bool talked, leaving;

    public override string Prompt => $"Talk to the {speakerName}";

    public override bool CanInteract(Vector3 playerPosition) => !talked;

    void Start()
    {
        if (showNameLabel) CreateNameLabel();
    }

    void CreateNameLabel()
    {
        var go = new GameObject("NameLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, labelHeight, 0f);
        nameLabel = go.AddComponent<TextMesh>();
        nameLabel.text = speakerName;
        nameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameLabel.GetComponent<MeshRenderer>().sharedMaterial = nameLabel.font.material;
        nameLabel.fontSize = 48;
        nameLabel.characterSize = 0.008f;
        nameLabel.anchor = TextAnchor.MiddleCenter;
        nameLabel.alignment = TextAlignment.Center;
        nameLabel.color = new Color(1f, 0.9f, 0.65f);
    }

    void Update()
    {
        if (!leaving) return;
        if (exitPoint == null) { gameObject.SetActive(false); return; }

        Vector3 target = exitPoint.position;
        target.y = transform.position.y;
        Vector3 to = target - transform.position;
        if (to.magnitude < 0.1f) { gameObject.SetActive(false); return; }

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 8f * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (nameLabel != null && cam != null)
            nameLabel.transform.rotation = Quaternion.LookRotation(nameLabel.transform.position - cam.transform.position);
    }

    public override void Interact()
    {
        var hud = GameHUD.Instance;
        if (hud == null || talked) return;

        if (facePlayer && FirstPersonController.Instance != null)
        {
            Vector3 to = FirstPersonController.Instance.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
        }

        var shown = new GameHUD.Line[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string speaker = string.IsNullOrEmpty(lines[i].speaker) ? speakerName : lines[i].speaker;
            shown[i] = new GameHUD.Line(speaker, lines[i].text);
        }

        hud.ShowDialogue(shown, () =>
        {
            talked = true;
            leaving = true;
            // Let the player walk through him while he leaves.
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        });
    }
}
