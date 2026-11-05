using UnityEngine;

// Suppress rendering only: gameplay colliders, portal activation and scoring stay active.
[DisallowMultipleComponent, DefaultExecutionOrder(30000)]
public class PSH_Script_EffectVisibility : MonoBehaviour
{
    private Renderer targetRenderer;
    private Light targetLight;
    private bool wasHidden, wasLit;
    private bool suppressed;

    private void OnEnable()
    {
        targetRenderer = GetComponent<Renderer>();
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
                wasHidden = targetRenderer != null && targetRenderer.forceRenderingOff;
                wasLit = targetLight != null && targetLight.enabled;
                suppressed = true;
            }
            if (targetRenderer != null) targetRenderer.forceRenderingOff = true;
            if (targetLight != null) targetLight.enabled = false;
        }
        else if (suppressed)
        {
            if (targetRenderer != null) targetRenderer.forceRenderingOff = wasHidden;
            if (targetLight != null) targetLight.enabled = wasLit;
            suppressed = false;
        }
    }
}
