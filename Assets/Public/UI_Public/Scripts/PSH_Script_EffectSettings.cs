using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Persistent visual-effect preference shared by menus and gameplay.</summary>
public static class PSH_Script_EffectSettings
{
    public const string PreferenceKey = "Settings_VFX_On";
    private static bool loaded;
    private static bool enabled = true;
    public static event Action<bool> Changed;
    public static bool IsEnabled
    {
        get { Load(); return enabled; }
    }

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
        enabled = PlayerPrefs.GetInt(PreferenceKey, 1) == 1;
        Shader.SetGlobalFloat("_BallARetroReduceEffects", enabled ? 0f : 1f);
        ApplyLibrarySettings(enabled);
    }

    public static void SetEnabled(bool value)
    {
        Load();
        PlayerPrefs.SetInt(PreferenceKey, value ? 1 : 0);
        PlayerPrefs.Save();
        Shader.SetGlobalFloat("_BallARetroReduceEffects", value ? 0f : 1f);
        ApplyLibrarySettings(value);
        if (enabled == value) return;
        enabled = value;
        Changed?.Invoke(value);
    }

    private static void ApplyLibrarySettings(bool value)
    {
        CartoonFX.CFXR_Effect.GlobalDisableCameraShake = !value;
        CartoonFX.CFXR_Effect.GlobalDisableLights = !value;
        if (!value)
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
