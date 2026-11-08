using UnityEngine;

// Authored for BALL-A-RETRO. Geometry, timing and colors are generated locally.
[DisallowMultipleComponent, RequireComponent(typeof(ParticleSystem))]
public sealed class BallARetroPortalVfx : MonoBehaviour
{
    public enum PortalStyle { ColoredHalo, FireVortex }

    [SerializeField] PortalStyle style = PortalStyle.ColoredHalo;
    [SerializeField, Min(.01f)] float radius = 1f;
    [SerializeField] Color tint = new Color32(75, 225, 235, 255);
    public const int ParticleBudget = 2048;
    public const int HaloParticleCount = 384;
    public int ActiveParticleCount => style == PortalStyle.FireVortex ? ParticleBudget : HaloParticleCount;
    const float Tau = Mathf.PI * 2;
    readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ParticleBudget];
    readonly Seed[] seeds = new Seed[ParticleBudget];
    ParticleSystem system;
    float elapsed;

    struct Seed
    {
        public float angle, phase, variation, drift;
    }

    void Awake()
    {
        system = GetComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.simulationSpeed = 0;
        main.maxParticles = ParticleBudget;
        main.startSize3D = true;
        var emission = system.emission;
        emission.enabled = false;
        var shape = system.shape;
        shape.enabled = false;
        for (int i = 0; i < ParticleBudget; i++)
            seeds[i] = new Seed {
                angle = Sample(i, 0) * Tau,
                phase = Sample(i, 1),
                variation = Sample(i, 2),
                drift = Sample(i, 3)
            };
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
        if (style == PortalStyle.FireVortex) RefreshFireVortex();
        else RefreshColoredHalo();
        system.SetParticles(particles, ActiveParticleCount);
    }

    void RefreshColoredHalo()
    {
        for (int i = 0; i < HaloParticleCount; i++)
        {
            var seed = seeds[i];
            float angle, distance, depth, width, height, alpha;
            Color color;
            if (i < 96)
            {
                // Overlapping soft strokes form a continuous glow, rather than a dotted ring.
                angle = seed.angle - elapsed * (.38f + seed.drift * .18f);
                distance = .96f + Mathf.Sin(elapsed * .7f + seed.phase * Tau) * .035f;
                depth = .012f;
                width = .48f + seed.variation * .18f;
                height = .18f + seed.drift * .10f;
                alpha = .09f + seed.variation * .03f;
                color = Color.Lerp(tint, Color.white, .12f);
            }
            else if (i < 288)
            {
                // One-second wisps curl along the rim and slowly expand as they fade.
                float travel = Mathf.Repeat(elapsed / (1f + seed.variation * .35f) + seed.phase, 1);
                angle = seed.angle - travel * (1.1f + seed.drift * .8f);
                distance = .91f + travel * .12f + Mathf.Sin(seed.phase * Tau + travel * 4) * .08f;
                depth = -.03f - seed.variation * .07f;
                width = Mathf.Lerp(.30f, .66f, travel);
                height = width * (.75f + seed.drift * .35f);
                alpha = Envelope(travel) * (.065f + seed.variation * .035f);
                color = Color.Lerp(tint, Color.white, .08f + seed.variation * .2f);
            }
            else
            {
                // Bright elongated sparks skim the rim; some peel away along an outer arc.
                float travel = Mathf.Repeat(elapsed / (.45f + seed.variation * .35f) + seed.phase, 1);
                angle = seed.angle - elapsed * 3 + travel * seed.drift * .35f;
                distance = .97f + (seed.drift - .5f) * .09f
                    + travel * travel * (i % 4 == 0 ? .68f : .16f);
                depth = .025f + seed.variation * .025f;
                width = .19f + seed.variation * .23f;
                height = .014f + seed.drift * .014f;
                alpha = Envelope(travel) * (.75f + seed.drift * .25f);
                color = Color.Lerp(tint, Color.white, .84f + seed.variation * .14f);
            }
            Place(i, angle, distance, depth, 1.3f, width, height, alpha, color);
        }
    }

    void RefreshFireVortex()
    {
        for (int i = 0; i < ParticleBudget; i++)
        {
            var seed = seeds[i];
            float travel = Mathf.Repeat(elapsed / (.8f + seed.variation * .7f) + seed.phase, 1);
            float emissionAngle = seed.angle - elapsed * .65f;
            // Start on the circle and leave along its tangent. This makes a dense
            // golden rim with a widening spiral plume, never smoke filling the hole.
            float tangentTravel = travel * (1.4f + seed.drift * 1.5f);
            float ringRadius = 1 + (seed.variation - .5f) * .025f;
            float angle = emissionAngle + Mathf.Atan2(tangentTravel, ringRadius);
            float distance = Mathf.Sqrt(ringRadius * ringRadius + tangentTravel * tangentTravel);
            float depth = (seed.drift - .5f) * .035f;
            float width = .065f + seed.variation * .075f;
            float height = .006f + seed.drift * .008f;
            float alpha = Mathf.SmoothStep(0, 1, travel * 32)
                * Mathf.SmoothStep(0, 1, (1 - travel) * 2.5f) * (.7f + seed.drift * .3f);
            var color = new Color(1, Mathf.Lerp(.96f, .53f, travel) + seed.variation * .035f,
                .015f + seed.variation * .035f, tint.a);
            Place(i, angle, distance, depth, 1, width, height, alpha, color);
            particles[i].rotation = -(emissionAngle * Mathf.Rad2Deg + 90);
        }
    }

    void Place(int index, float angle, float distance, float depth, float aspect,
        float width, float height, float alpha, Color color)
    {
        float safeRadius = Mathf.Max(.01f, radius);
        color.a *= alpha;
        particles[index].position = new Vector3(Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance * aspect, depth) * safeRadius;
        particles[index].startSize3D = new Vector3(width, height, height) * safeRadius;
        particles[index].startColor = color;
        particles[index].startLifetime = 10;
        particles[index].remainingLifetime = 10;
        particles[index].velocity = Vector3.zero;
        particles[index].rotation = -(angle * Mathf.Rad2Deg + 90);
    }

    static float Envelope(float phase)
    {
        return Mathf.SmoothStep(0, 1, phase * 7) * Mathf.SmoothStep(0, 1, (1 - phase) * 3);
    }

    static float Sample(int index, int channel)
    {
        // Stable local variation without affecting gameplay's UnityEngine.Random state.
        uint value = unchecked((uint)(index * 4 + channel + 1) * 747796405u + 2891336453u);
        value = unchecked(((value >> (int)((value >> 28) + 4)) ^ value) * 277803737u);
        value = (value >> 22) ^ value;
        return (value & 0x00ffffffu) / 16777216f;
    }
}
