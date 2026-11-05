using UnityEngine;

// Suppress rendering only: gameplay colliders, portal activation and scoring stay active.
[DisallowMultipleComponent, DefaultExecutionOrder(30000)]
public class PSH_Script_EffectVisibility : MonoBehaviour
{
    private Renderer[] targetRenderers;
    private Light targetLight;
    private bool[] wasHidden;
    private bool wasLit;
    private bool[] suppressed;
    private bool lightSuppressed;

    internal static bool IsEffectRenderer(Renderer renderer)
    {
        return renderer != null && (renderer is ParticleSystemRenderer ||
            renderer is TrailRenderer || renderer.GetType().Name == "VFXRenderer");
    }

    private void OnEnable()
    {
        // A ball's MeshRenderer and TrailRenderer share one GameObject. Only
        // suppress effect renderers, never whichever Renderer happens to be first.
        targetRenderers = System.Array.FindAll(GetComponents<Renderer>(), IsEffectRenderer);
        wasHidden = new bool[targetRenderers.Length];
        suppressed = new bool[targetRenderers.Length];
        targetLight = GetComponent<Light>();
        PSH_Script_EffectSettings.Changed += Apply;
        Apply(PSH_Script_EffectSettings.IsEnabled);
    }
    private void OnDisable()
    {
        PSH_Script_EffectSettings.Changed -= Apply;
        Refresh(true);
    }
    private void LateUpdate()
    {
        Refresh(false);
    }
    private void Apply(bool _) => Refresh(false);

    private void Refresh(bool restore)
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var renderer = targetRenderers[i];
            if (renderer == null) continue;
            bool enabled = restore || (renderer is TrailRenderer
                ? PSH_Script_EffectSettings.TrailsEnabled : PSH_Script_EffectSettings.ParticlesEnabled);
            if (!enabled)
            {
                if (!suppressed[i]) wasHidden[i] = renderer.forceRenderingOff;
                suppressed[i] = true;
                renderer.forceRenderingOff = true;
            }
            else if (suppressed[i])
            {
                renderer.forceRenderingOff = wasHidden[i];
                suppressed[i] = false;
            }
        }
        if (targetLight == null) return;
        if (!restore && !PSH_Script_EffectSettings.FlashesEnabled)
        {
            if (!lightSuppressed) wasLit = targetLight.enabled;
            lightSuppressed = true;
            targetLight.enabled = false;
        }
        else if (lightSuppressed)
        {
            targetLight.enabled = wasLit;
            lightSuppressed = false;
        }
    }
}
