using UnityEngine;

// Suppress rendering only: gameplay colliders, portal activation and scoring stay active.
[DisallowMultipleComponent, DefaultExecutionOrder(30000)]
public class PSH_Script_EffectVisibility : MonoBehaviour
{
    private Renderer[] targetRenderers;
    private Light targetLight;
    private bool[] wasHidden;
    private bool wasLit;
    private bool suppressed;

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
        targetLight = GetComponent<Light>();
        PSH_Script_EffectSettings.Changed += Apply;
        Apply(PSH_Script_EffectSettings.IsEnabled);
    }
    private void OnDisable()
    {
        PSH_Script_EffectSettings.Changed -= Apply;
        Apply(true);
    }
    private void LateUpdate()
    {
        if (!PSH_Script_EffectSettings.IsEnabled) Apply(false);
    }
    private void Apply(bool enabled)
    {
        if (!enabled)
        {
            if (!suppressed)
            {
                for (int i = 0; i < targetRenderers.Length; i++)
                    wasHidden[i] = targetRenderers[i] != null && targetRenderers[i].forceRenderingOff;
                wasLit = targetLight != null && targetLight.enabled;
                suppressed = true;
            }
            foreach (var renderer in targetRenderers)
                if (renderer != null) renderer.forceRenderingOff = true;
            if (targetLight != null) targetLight.enabled = false;
        }
        else if (suppressed)
        {
            for (int i = 0; i < targetRenderers.Length; i++)
                if (targetRenderers[i] != null) targetRenderers[i].forceRenderingOff = wasHidden[i];
            if (targetLight != null) targetLight.enabled = wasLit;
            suppressed = false;
        }
    }
}
