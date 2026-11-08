using UnityEngine;

// Authored for BALL-A-RETRO. Geometry, timing and colors are generated locally.
[DisallowMultipleComponent, RequireComponent(typeof(ParticleSystem))]
public sealed class BallARetroPortalVfx : MonoBehaviour
{
    [SerializeField, Min(.01f)] float radius = 1f;
    [SerializeField] Color tint = new Color32(75, 225, 235, 255);
    const int Count = 144;
    readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[Count];
    ParticleSystem system;
    float elapsed;

    void Awake()
    {
        system = GetComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.simulationSpeed = 0;
        main.maxParticles = Count;
        var emission = system.emission;
        emission.enabled = false;
        var shape = system.shape;
        shape.enabled = false;
    }

    void OnEnable()
    {
        elapsed = 0;
        if (!system) return;
        system.Play();
        Refresh();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        Refresh();
    }

    void OnDisable()
    {
        if (system) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Refresh()
    {
        for (int i = 0; i < Count; i++)
        {
            float angle, distance, depth = 0, alpha;
            if (i < 96)
            {
                int ring = i / 48;
                angle = i % 48 * Mathf.PI / 24 + elapsed * (ring == 0 ? .4f : -.65f);
                distance = radius * (ring == 0 ? 1 : .82f);
                alpha = i % 4 == 0 ? 1 : .45f;
            }
            else
            {
                float travel = Mathf.Repeat(elapsed * .4f + (i - 96) / 48f, 1f);
                angle = i * 2.399963f + travel * 2.6f;
                distance = radius * Mathf.Lerp(.78f, .08f, travel);
                depth = -.12f * radius * travel;
                alpha = Mathf.Sin(travel * Mathf.PI) * .75f;
            }
            Color color = i % 7 == 0 ? Color.Lerp(tint, Color.white, .7f) : tint;
            color.a *= alpha;
            particles[i].position = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, depth);
            particles[i].startSize = radius * (i % 4 == 0 ? .075f : .035f);
            particles[i].startColor = color;
            particles[i].startLifetime = 10;
            particles[i].remainingLifetime = 10;
            particles[i].velocity = Vector3.zero;
            particles[i].rotation = 45;
        }
        system.SetParticles(particles, Count);
    }
}
