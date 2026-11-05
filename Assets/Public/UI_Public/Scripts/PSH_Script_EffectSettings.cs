using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Persistent visual-effect preference shared by menus and gameplay.</summary>
public static class PSH_Script_EffectSettings
{
    public const string PreferenceKey = "Settings_VFX_On";
    public enum Effect { CameraShake, Particles, Trails, Flashes, ScreenEffects, PostProcessing }
    private static readonly bool[] states = new bool[6];
    private static bool loaded;
    // Raised for any category change; the argument reports whether any effect is ON.
    public static event Action<bool> Changed;
    public static bool IsEnabled
    {
        get { Load(); return Array.Exists(states, value => value); }
    }
    public static bool AllEnabled { get { Load(); return Array.TrueForAll(states, value => value); } }
    public static bool CameraShakeEnabled => Get(Effect.CameraShake);
    public static bool ParticlesEnabled => Get(Effect.Particles);
    public static bool TrailsEnabled => Get(Effect.Trails);
    public static bool FlashesEnabled => Get(Effect.Flashes);
    public static bool ScreenEffectsEnabled => Get(Effect.ScreenEffects);
    public static bool PostProcessingEnabled => Get(Effect.PostProcessing);
    public static string KeyFor(Effect effect) => PreferenceKey + "_" + effect;
    public static bool Get(Effect effect) { Load(); return states[(int)effect]; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = false;
        Changed = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Load();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void Load()
    {
        if (loaded) return;
        loaded = true;
        // New profiles default to all ON. Preserve an existing all-OFF preference
        // when upgrading from the old single switch.
        int legacyDefault = PlayerPrefs.GetInt(PreferenceKey, 1);
        foreach (Effect effect in Enum.GetValues(typeof(Effect)))
            states[(int)effect] = PlayerPrefs.GetInt(KeyFor(effect), legacyDefault) == 1;
        ApplyRuntimeSettings();
    }

    public static void SetEnabled(bool value)
    {
        Load();
        for (int i = 0; i < states.Length; i++) states[i] = value;
        SaveAndApply();
    }

    public static void Set(Effect effect, bool value)
    {
        Load();
        if (states[(int)effect] == value) return;
        states[(int)effect] = value;
        SaveAndApply();
    }

    private static void SaveAndApply()
    {
        // Write a complete snapshot so partially migrated profiles stay consistent.
        foreach (Effect effect in Enum.GetValues(typeof(Effect)))
            PlayerPrefs.SetInt(KeyFor(effect), states[(int)effect] ? 1 : 0);
        PlayerPrefs.SetInt(PreferenceKey, IsEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyRuntimeSettings();
        Changed?.Invoke(IsEnabled);
    }

    private static void ApplyRuntimeSettings()
    {
        Shader.SetGlobalFloat("_BallARetroReduceEffects", ScreenEffectsEnabled ? 0f : 1f);
        CartoonFX.CFXR_Effect.GlobalDisableCameraShake = !CameraShakeEnabled;
        CartoonFX.CFXR_Effect.GlobalDisableLights = !FlashesEnabled;
        if (!CameraShakeEnabled)
            foreach (var effect in UnityEngine.Object.FindObjectsByType<CartoonFX.CFXR_Effect>(FindObjectsSortMode.None))
                effect.cameraShake?.StopShake();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects()) ApplyToHierarchy(root);
    }

    // Also called immediately after runtime VFX instantiation, before its first frame.
    public static void ApplyToHierarchy(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!PSH_Script_EffectVisibility.IsEffectRenderer(renderer)) continue;
            if (!renderer.TryGetComponent<PSH_Script_EffectVisibility>(out _))
                renderer.gameObject.AddComponent<PSH_Script_EffectVisibility>();
            foreach (var light in renderer.GetComponentsInChildren<Light>(true))
                if (!light.TryGetComponent<PSH_Script_EffectVisibility>(out _))
                    light.gameObject.AddComponent<PSH_Script_EffectVisibility>();
        }
        foreach (var volume in root.GetComponentsInChildren<Volume>(true))
            if (!volume.TryGetComponent<PSH_Script_EffectVolume>(out _))
                volume.gameObject.AddComponent<PSH_Script_EffectVolume>();
    }
}
