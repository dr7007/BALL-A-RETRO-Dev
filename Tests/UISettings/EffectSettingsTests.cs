using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class EffectSettingsTests
{
    GameObject root;
    bool saved;
    [SetUp] public void Setup()
    {
        saved = PSH_Script_EffectSettings.IsEnabled;
        PSH_Script_EffectSettings.SetEnabled(true);
        root = new GameObject("Fixture");
    }
    [TearDown] public void Cleanup()
    {
        Object.DestroyImmediate(root);
        PSH_Script_EffectSettings.SetEnabled(saved);
        Time.timeScale = 1;
    }
    GameObject Child(string name, params System.Type[] types)
    {
        var go = new GameObject(name, types);
        go.transform.SetParent(root.transform);
        return go;
    }
    Toggle Toggle()
    {
        var go = Child("Toggle", typeof(RectTransform), typeof(Toggle), typeof(PSH_Script_ToggleUIController));
        return go.GetComponent<Toggle>();
    }
    [Test] public void ToggleWorksWithoutSoundBridge_AndSynchronizesOtherMenus()
    {
        var first = Toggle(); var second = Toggle();
        first.isOn = false;
        Assert.That(PSH_Script_EffectSettings.IsEnabled, Is.False);
        Assert.That(PlayerPrefs.GetInt(PSH_Script_EffectSettings.PreferenceKey), Is.Zero);
        Assert.That(second.isOn, Is.False);
        second.gameObject.SetActive(false);
        first.isOn = true;
        second.gameObject.SetActive(true);
        Assert.That(second.isOn, Is.True);
    }
    [Test] public void ExistingParticlesStopRenderingImmediately_WithoutDisablingGameplay()
    {
        var go = Child("Effect", typeof(ParticleSystem), typeof(BoxCollider));
        PSH_Script_EffectSettings.ApplyToHierarchy(root);
        PSH_Script_EffectSettings.SetEnabled(false);
        Assert.That(go.GetComponent<ParticleSystemRenderer>().forceRenderingOff, Is.True);
        Assert.That(go.activeInHierarchy, Is.True);
        Assert.That(go.GetComponent<BoxCollider>().enabled, Is.True);
        PSH_Script_EffectSettings.SetEnabled(true);
        Assert.That(go.GetComponent<ParticleSystemRenderer>().forceRenderingOff, Is.False);
    }
    [Test] public void NewAndInitiallyInactiveEffects_RespectDisabledPreference()
    {
        PSH_Script_EffectSettings.SetEnabled(false);
        var active = Child("New Effect", typeof(ParticleSystem));
        var inactive = Child("Inactive Effect", typeof(ParticleSystem));
        inactive.SetActive(false);
        PSH_Script_EffectSettings.ApplyToHierarchy(root);
        Assert.That(active.GetComponent<Renderer>().forceRenderingOff, Is.True);
        inactive.SetActive(true);
        Assert.That(inactive.GetComponent<Renderer>().forceRenderingOff, Is.True);
    }
    [Test] public void RestoringEffects_PreservesPreviouslyHiddenRendererAndLight()
    {
        var go = Child("Hidden", typeof(ParticleSystem), typeof(Light));
        go.GetComponent<Renderer>().forceRenderingOff = true;
        go.GetComponent<Light>().enabled = false;
        PSH_Script_EffectSettings.ApplyToHierarchy(root);
        PSH_Script_EffectSettings.SetEnabled(false);
        PSH_Script_EffectSettings.SetEnabled(true);
        Assert.That(go.GetComponent<Renderer>().forceRenderingOff, Is.True);
        Assert.That(go.GetComponent<Light>().enabled, Is.False);
    }
    [Test] public void BloomIsDisabledOnRuntimeCopy_AssetRemainsIntact()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.Add<Bloom>().active = true;
        profile.Add<FilmGrain>().active = false;
        var volume = Child("Volume", typeof(Volume)).GetComponent<Volume>();
        volume.sharedProfile = profile;
        PSH_Script_EffectSettings.ApplyToHierarchy(root);
        PSH_Script_EffectSettings.SetEnabled(false);
        Assert.That(volume.profile.TryGet<Bloom>(out var bloom), Is.True);
        Assert.That(bloom.active, Is.False);
        Assert.That(profile.components[0].active, Is.True);
        PSH_Script_EffectSettings.SetEnabled(true);
        Assert.That(bloom.active, Is.True);
        volume.profile.TryGet<FilmGrain>(out var grain);
        Assert.That(grain.active, Is.False);
        Object.DestroyImmediate(profile);
    }
    [UnityTest] public IEnumerator DisablingDuringCameraShake_RestoresPositionWhilePaused()
    {
        var go = Child("Camera"); go.transform.localPosition = new Vector3(2, 3, 4);
        var shaker = go.AddComponent<CJS_Script_CameraShaker>();
        shaker.useUnscaledTime = true;
        shaker.Shake(1, 2);
        yield return null;
        Time.timeScale = 0;
        PSH_Script_EffectSettings.SetEnabled(false);
        Assert.That(go.transform.localPosition, Is.EqualTo(new Vector3(2, 3, 4)));
        shaker.Shake(1, 2);
        yield return null;
        Assert.That(go.transform.localPosition, Is.EqualTo(new Vector3(2, 3, 4)));
    }
    [Test] public void AnimatedShaderSetting_UpdatesImmediately()
    {
        PSH_Script_EffectSettings.SetEnabled(false);
        Assert.That(Shader.GetGlobalFloat("_BallARetroReduceEffects"), Is.EqualTo(1));
        Assert.That(CartoonFX.CFXR_Effect.GlobalDisableCameraShake, Is.True);
        Assert.That(CartoonFX.CFXR_Effect.GlobalDisableLights, Is.True);
        PSH_Script_EffectSettings.SetEnabled(true);
        Assert.That(Shader.GetGlobalFloat("_BallARetroReduceEffects"), Is.Zero);
        Assert.That(CartoonFX.CFXR_Effect.GlobalDisableCameraShake, Is.False);
    }
    [Test] public void ModifiedShaderGraphs_CompileWithoutErrors()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Shader", new[] { "Assets/EffectShaders" });
        Assert.That(guids.Length, Is.EqualTo(4));
        foreach (var guid in guids)
        {
            var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            var material = new Material(shader);
            UnityEditor.ShaderUtil.CompilePass(material, 0, true);
            Assert.That(UnityEditor.ShaderUtil.ShaderHasError(shader), Is.False, shader.name);
            Object.DestroyImmediate(material);
        }
    }
}
