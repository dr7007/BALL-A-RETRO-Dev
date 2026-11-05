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
        PSH_Script_EffectSettings.Changed += Apply;
        Apply(PSH_Script_EffectSettings.IsEnabled);
    }
    private void OnDisable()
    {
        PSH_Script_EffectSettings.Changed -= Apply;
        Apply(true);
        originalStates.Clear();
    }
    private void Apply(bool enabled)
    {
        foreach (var pair in originalStates)
            if (pair.Key != null) pair.Key.active = enabled && pair.Value;
    }
}
