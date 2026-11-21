using UnityEngine;

/// <summary>
/// Turns the fountain water (and its sound) on when the player comes close and
/// off again when they walk away. When it switches off, the water already in the
/// air finishes falling and the sound fades out, so it feels natural.
///
/// Put this on the object with the water ParticleSystem. If the object also has
/// an AudioSource, it loops while the water is on.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class FountainProximity : MonoBehaviour
{
    [Tooltip("Water starts when the player is closer than this (metres, measured on the ground).")]
    public float startDistance = 1.3f;

    [Tooltip("Water stops when the player is further than this. A bit larger than the start distance so it doesn't flicker at the edge.")]
    public float stopDistance = 1.6f;

    [Header("Sound")]
    [Range(0f, 1f)] public float soundVolume = 0.8f;
    public float fadeInSeconds = 1f;
    public float fadeOutSeconds = 1.5f;

    ParticleSystem water;
    AudioSource sound;
    bool on;

    void Awake()
    {
        water = GetComponent<ParticleSystem>();
        var main = water.main;
        main.playOnAwake = false;
        water.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        sound = GetComponent<AudioSource>();
        if (sound != null)
        {
            sound.playOnAwake = false;
            sound.loop = true;
            sound.volume = 0f;
            sound.Stop();
        }
    }

    void Update()
    {
        var player = FirstPersonController.Instance;
        if (player != null)
        {
            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;

            if (!on && distance < startDistance)
            {
                water.Play(true);
                if (sound != null && !sound.isPlaying) sound.Play();
                on = true;
            }
            else if (on && distance > stopDistance)
            {
                water.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                on = false;
            }
        }

        // Fade the sound towards on/off; stop it completely once silent.
        if (sound != null)
        {
            float target = on ? soundVolume : 0f;
            float seconds = on ? fadeInSeconds : fadeOutSeconds;
            sound.volume = Mathf.MoveTowards(sound.volume, target, soundVolume * Time.deltaTime / Mathf.Max(0.01f, seconds));
            if (!on && sound.isPlaying && sound.volume <= 0f) sound.Stop();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, startDistance);
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
