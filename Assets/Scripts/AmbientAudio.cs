using UnityEngine;

/// <summary>
/// Background ambience that loops forever without a gap: shortly before the
/// track ends, a second copy starts and the two crossfade (one fades out while
/// the other fades in), so the loop point can't be heard.
///
/// The volume follows where the player is: full in the courtyard, and
/// <see cref="quietZoneVolume"/> (30%) while the player is inside
/// <see cref="quietZone"/> (the connecting room), however they got there.
/// Changes are eased so they never jump.
/// </summary>
public class AmbientAudio : MonoBehaviour
{
    public static AmbientAudio Instance { get; private set; }

    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.8f;

    [Tooltip("Length of the crossfade at the loop point (seconds).")]
    public float crossfadeSeconds = 6f;

    [Tooltip("Fade-in when the game starts (seconds).")]
    public float startFadeSeconds = 3f;

    [Tooltip("How long volume changes between areas take (seconds).")]
    public float areaFadeSeconds = 1.5f;

    [Header("Quieter area")]
    [Tooltip("While the player is inside this box (the connecting room), the ambience is turned down.")]
    public Collider quietZone;
    [Range(0f, 1f)] public float quietZoneVolume = 0.3f;

    AudioSource a, b;       // the two players that take turns
    AudioSource current;    // the one carrying the track right now
    AudioSource next;       // fades in near the end of `current`
    float areaVolume = 1f, areaTarget = 1f;
    float startFade;        // 0..1 overall fade-in at game start
    bool crossfading;

    void Awake()
    {
        Instance = this;
        a = CreateSource("Ambience A");
        b = CreateSource("Ambience B");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    AudioSource CreateSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = false;
        s.playOnAwake = false;
        s.spatialBlend = 0f; // 2D: heard the same everywhere
        s.volume = 0f;
        return s;
    }

    void Start()
    {
        if (clip == null) { Debug.LogWarning("AmbientAudio: no clip assigned."); enabled = false; return; }
        current = a; next = b;
        current.time = 0f;
        current.Play();
    }

    /// <summary>Ease the ambience to this fraction of its volume (1 = full, 0.3 = 30%).</summary>
    public void SetAreaVolume(float fraction) => areaTarget = Mathf.Clamp01(fraction);

    void Update()
    {
        var player = FirstPersonController.Instance;
        if (quietZone != null && player != null)
            areaTarget = quietZone.bounds.Contains(player.transform.position + Vector3.up * 0.5f) ? quietZoneVolume : 1f;

        float dt = Time.unscaledDeltaTime;
        startFade = Mathf.MoveTowards(startFade, 1f, dt / Mathf.Max(0.01f, startFadeSeconds));
        areaVolume = Mathf.MoveTowards(areaVolume, areaTarget, dt / Mathf.Max(0.01f, areaFadeSeconds));
        float master = volume * startFade * areaVolume;

        float length = clip.length;
        float fade = Mathf.Min(crossfadeSeconds, length * 0.5f);
        float remaining = length - current.time;

        // Start the next copy when the current one gets close to the end.
        if (!crossfading && current.isPlaying && remaining <= fade)
        {
            next.time = 0f;
            next.Play();
            crossfading = true;
        }

        if (crossfading)
        {
            // t goes 0 -> 1 over the crossfade; equal-power curves keep loudness steady.
            float t = Mathf.Clamp01(next.time / fade);
            current.volume = master * Mathf.Cos(t * Mathf.PI * 0.5f);
            next.volume = master * Mathf.Sin(t * Mathf.PI * 0.5f);

            if (t >= 1f || !current.isPlaying)
            {
                current.Stop();
                (current, next) = (next, current);
                crossfading = false;
            }
        }
        else
        {
            current.volume = master;
            if (!current.isPlaying) { current.time = 0f; current.Play(); } // safety net
        }
    }
}
