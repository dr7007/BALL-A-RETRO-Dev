using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent, RequireComponent(typeof(Volume))]
public class PSH_Script_EffectVolume : MonoBehaviour
{
    private readonly Dictionary<VolumeComponent, bool> originalStates = new Dictionary<VolumeComponent, bool>();
    private void OnEnable()
    {
        var volume = GetComponent<Volume>();
        if (volume.sharedProfile != null)
            foreach (var effect in volume.profile.components)
                if (effect is Bloom || effect is ChromaticAberration || effect is LensDistortion ||
                    effect is MotionBlur || effect is FilmGrain)
                    originalStates[effect] = effect.active;
        PSH_Script_EffectSettings.Changed += OnSettingsChanged;
        OnSettingsChanged(false);
    }
    private void OnDisable()
    {
        PSH_Script_EffectSettings.Changed -= OnSettingsChanged;
        Apply(true);
        originalStates.Clear();
    }
    private void OnSettingsChanged(bool _) => Apply(PSH_Script_EffectSettings.PostProcessingEnabled);
    private void Apply(bool enabled)
    {
        foreach (var pair in originalStates)
            if (pair.Key != null) pair.Key.active = enabled && pair.Value;
    }
}
