using UnityEngine;
using UnityEngine.UI;

// Original vector particles: no downloaded textures, materials or effect prefabs.
[DisallowMultipleComponent]
public sealed class BallARetroLoadingVfx : MaskableGraphic
{
    float phase;
    static readonly Color Amber = new Color32(255, 180, 58, 255);
    static readonly Color Cyan = new Color32(76, 226, 235, 255);

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    void Update()
    {
        if (!PSH_Script_EffectSettings.ParticlesEnabled) return;
        phase += Time.unscaledDeltaTime;
        SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        phase = 0;
        PSH_Script_EffectSettings.Changed += EffectsChanged;
    }

    protected override void OnDisable()
    {
        PSH_Script_EffectSettings.Changed -= EffectsChanged;
        base.OnDisable();
    }

    void EffectsChanged(bool _) => SetVerticesDirty();

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var center = rectTransform.rect.center;
        float scale = Mathf.Min(rectTransform.rect.width / 640f, rectTransform.rect.height / 360f);
        if (scale <= 0) return;
        // A static pinball and orbit remain readable when particles are disabled.
        float time = PSH_Script_EffectSettings.ParticlesEnabled ? phase : 0;
        for (int ring = 0; ring < 2; ring++)
        {
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2 / 32 + time * (ring == 0 ? .75f : -.45f);
                float radius = ring == 0 ? 66 : 94;
                var point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var tint = ring == 0 ? Amber : Cyan;
                tint.a = i % 4 == 0 ? .95f : .35f;
                Quad(mesh, center + point * scale, (i % 4 == 0 ? 5 : 3) * scale, tint);
            }
        }
        // Sparse converging motes, in the same pixel geometry as the portal effect.
        if (PSH_Script_EffectSettings.ParticlesEnabled)
            for (int i = 0; i < 28; i++)
            {
                float progress = Mathf.Repeat(time * .35f + i * .61803399f, 1f);
                float angle = i * 2.399963f + time * .14f;
                float radius = Mathf.Lerp(158, 104, progress);
                var tint = i % 2 == 0 ? Amber : Cyan;
                tint.a = Mathf.Sin(progress * Mathf.PI) * .6f;
                Quad(mesh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * scale, 2.5f * scale, tint);
            }
        Quad(mesh, center, 15 * scale, Amber);
        Quad(mesh, center + new Vector2(-4, 4) * scale, 5 * scale, Color.white);
    }

    static void Quad(VertexHelper mesh, Vector2 center, float halfSize, Color tint)
    {
        int first = mesh.currentVertCount;
        mesh.AddVert(center + new Vector2(-halfSize, -halfSize), tint, Vector2.zero);
        mesh.AddVert(center + new Vector2(-halfSize, halfSize), tint, Vector2.zero);
        mesh.AddVert(center + new Vector2(halfSize, halfSize), tint, Vector2.zero);
        mesh.AddVert(center + new Vector2(halfSize, -halfSize), tint, Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }
}
