using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Effect = PSH_Script_EffectSettings.Effect;

public class EffectOptionsTests
{
    GameObject root;
    readonly Dictionary<string, int?> preferences = new Dictionary<string, int?>();
    static readonly Effect[] effects = (Effect[])Enum.GetValues(typeof(Effect));

    [SetUp] public void Setup()
    {
        preferences.Clear();
        foreach (var key in effects.Select(PSH_Script_EffectSettings.KeyFor).Concat(new[] { PSH_Script_EffectSettings.PreferenceKey }))
            preferences[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
        PSH_Script_EffectSettings.SetEnabled(true);
        root = new GameObject("Effect options fixture");
    }
    [TearDown] public void Cleanup()
    {
        Object.DestroyImmediate(root);
        foreach (var pair in preferences)
            if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
            else PlayerPrefs.DeleteKey(pair.Key);
        PlayerPrefs.Save();
        Reload();
        Time.timeScale = 1;
    }
    static void Reload()
    {
        typeof(PSH_Script_EffectSettings).GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        _ = PSH_Script_EffectSettings.IsEnabled;
    }
    void ClearPreferences()
    {
        foreach (var key in preferences.Keys) PlayerPrefs.DeleteKey(key);
    }
    GameObject Child(string name, params Type[] types)
    {
        var go = new GameObject(name, types);
        go.transform.SetParent(root.transform);
        return go;
    }

    [Test] public void NewProfile_DefaultsToEveryEffectOn()
    {
        ClearPreferences(); Reload();
        Assert.That(PSH_Script_EffectSettings.AllEnabled, Is.True);
        foreach (var effect in effects) Assert.That(PSH_Script_EffectSettings.Get(effect), Is.True, effect.ToString());
    }
    [Test] public void OldAllOffPreference_IsPreservedOnUpgrade()
    {
        ClearPreferences();
        PlayerPrefs.SetInt(PSH_Script_EffectSettings.PreferenceKey, 0);
        Reload();
        Assert.That(PSH_Script_EffectSettings.IsEnabled, Is.False);
        foreach (var effect in effects) Assert.That(PSH_Script_EffectSettings.Get(effect), Is.False);
    }
    [Test] public void IndividualChoicesSurviveReload_AndPresetsUpdateEverySwitch()
    {
        PSH_Script_EffectSettings.SetEnabled(false);
        PSH_Script_EffectSettings.Set(Effect.CameraShake, true);
        PSH_Script_EffectSettings.Set(Effect.Trails, true);
        Reload();
        foreach (var effect in effects)
            Assert.That(PSH_Script_EffectSettings.Get(effect), Is.EqualTo(effect == Effect.CameraShake || effect == Effect.Trails));
        Assert.That(PSH_Script_EffectSettings.IsEnabled, Is.True);
        Assert.That(PSH_Script_EffectSettings.AllEnabled, Is.False);
        PSH_Script_EffectSettings.SetEnabled(true);
        Reload();
        Assert.That(PSH_Script_EffectSettings.AllEnabled, Is.True);
        PSH_Script_EffectSettings.SetEnabled(false);
        Reload();
        Assert.That(PSH_Script_EffectSettings.IsEnabled, Is.False);
    }
    [Test] public void TrailParticlesAndFlashLights_AreIndependent_AndBallMeshIsUntouched()
    {
        var ball = Child("Ball", typeof(MeshRenderer), typeof(TrailRenderer));
        var particles = Child("Particles", typeof(ParticleSystem), typeof(Light));
        PSH_Script_EffectSettings.ApplyToHierarchy(root);
        PSH_Script_EffectSettings.Set(Effect.Trails, false);
        Assert.That(ball.GetComponent<MeshRenderer>().forceRenderingOff, Is.False);
        Assert.That(ball.GetComponent<TrailRenderer>().forceRenderingOff, Is.True);
        Assert.That(particles.GetComponent<ParticleSystemRenderer>().forceRenderingOff, Is.False);
        PSH_Script_EffectSettings.Set(Effect.Particles, false);
        Assert.That(particles.GetComponent<ParticleSystemRenderer>().forceRenderingOff, Is.True);
        Assert.That(particles.GetComponent<Light>().enabled, Is.True);
        PSH_Script_EffectSettings.Set(Effect.Flashes, false);
        Assert.That(particles.GetComponent<Light>().enabled, Is.False);
        PSH_Script_EffectSettings.Set(Effect.Trails, true);
        Assert.That(ball.GetComponent<TrailRenderer>().forceRenderingOff, Is.False);
        Assert.That(particles.GetComponent<ParticleSystemRenderer>().forceRenderingOff, Is.True);
        Assert.That(ball.GetComponent<MeshRenderer>().forceRenderingOff, Is.False);
    }
    [Test] public void ShakeFlashAndScreenAnimationFlags_AreIndependent()
    {
        PSH_Script_EffectSettings.Set(Effect.CameraShake, false);
        Assert.That(CartoonFX.CFXR_Effect.GlobalDisableCameraShake, Is.True);
        Assert.That(CartoonFX.CFXR_Effect.GlobalDisableLights, Is.False);
        Assert.That(Shader.GetGlobalFloat("_BallARetroReduceEffects"), Is.Zero);
        PSH_Script_EffectSettings.Set(Effect.Flashes, false);
        PSH_Script_EffectSettings.Set(Effect.CameraShake, true);
        Assert.That(CartoonFX.CFXR_Effect.GlobalDisableCameraShake, Is.False);
        Assert.That(CartoonFX.CFXR_Effect.GlobalDisableLights, Is.True);
        PSH_Script_EffectSettings.Set(Effect.ScreenEffects, false);
        Assert.That(Shader.GetGlobalFloat("_BallARetroReduceEffects"), Is.EqualTo(1));
    }
    [Test] public void PostProcessingDoesNotFollowParticleSwitch()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.Add<Bloom>().active = true;
        var volume = Child("Volume", typeof(Volume)).GetComponent<Volume>();
        volume.sharedProfile = profile;
        PSH_Script_EffectSettings.ApplyToHierarchy(root);
        try
        {
            PSH_Script_EffectSettings.Set(Effect.Particles, false);
            volume.profile.TryGet<Bloom>(out var bloom);
            Assert.That(bloom.active, Is.True);
            PSH_Script_EffectSettings.Set(Effect.PostProcessing, false);
            Assert.That(bloom.active, Is.False);
            Assert.That(profile.components[0].active, Is.True);
        }
        finally { Object.DestroyImmediate(profile); }
    }
    [UnityTest] public IEnumerator CameraShakeWorksWithOtherEffectsOff_AndStopsImmediately()
    {
        PSH_Script_EffectSettings.SetEnabled(false);
        PSH_Script_EffectSettings.Set(Effect.CameraShake, true);
        var go = Child("Camera");
        go.transform.localPosition = new Vector3(1, 2, 3);
        var shaker = go.AddComponent<CJS_Script_CameraShaker>();
        shaker.useUnscaledTime = true;
        Time.timeScale = 0;
        shaker.Shake(2, 1);
        yield return null;
        Assert.That(Vector3.Distance(go.transform.localPosition, new Vector3(1, 2, 3)), Is.GreaterThan(.001f));
        PSH_Script_EffectSettings.Set(Effect.CameraShake, false);
        Assert.That(go.transform.localPosition, Is.EqualTo(new Vector3(1, 2, 3)));
    }
    [UnityTest] public IEnumerator RealSettingsRow_OpensDetailsAndClosesThemWithTheMenu()
    {
        var canvas = Child("Settings", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EffectSettingsRow.prefab");
        Assert.That(prefab, Is.Not.Null);
        var row = Object.Instantiate(prefab, canvas.transform);
        var rect = (RectTransform)row.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1162, 70);
        var controller = row.GetComponentInChildren<PSH_Script_ToggleUIController>();
        var master = controller.GetComponent<Toggle>();
        var details = row.GetComponentInChildren<Button>();
        Assert.That(details.name, Is.EqualTo("Button_EffectDetails"));
        var es = EventSystem.current != null ? EventSystem.current : Child("Events", typeof(EventSystem)).GetComponent<EventSystem>();
        yield return null;
        Canvas.ForceUpdateCanvases();
        Click(es, null, details.gameObject);
        var panel = Object.FindFirstObjectByType<PSH_Script_EffectOptionsPanel>();
        Assert.That(panel, Is.Not.Null, "The prefab's details button must be wired to the controller.");
        try
        {
            PSH_Script_EffectSettings.Set(Effect.Particles, false);
            Assert.That(master.isOn, Is.True, "The master stays ON while some effects are enabled.");
            Assert.That(controller.label.text, Is.EqualTo("일부"));
            master.isOn = false;
            Assert.That(PSH_Script_EffectSettings.IsEnabled, Is.False);
            Assert.That(panel.GetComponentsInChildren<Toggle>().All(t => !t.isOn), Is.True);
            master.isOn = true;
            Assert.That(PSH_Script_EffectSettings.AllEnabled, Is.True);
            canvas.gameObject.SetActive(false);
            Assert.That(panel.gameObject.activeSelf, Is.False, "Closing the system menu must hide its detached dialog.");
            canvas.gameObject.SetActive(true);
            details.onClick.Invoke();
            Assert.That(panel.gameObject.activeSelf, Is.True);
        }
        finally { Object.DestroyImmediate(panel.gameObject); }
    }
    [UnityTest] public IEnumerator DialogControls_RenderAndReceiveClicksAtFhdAndQhd_WhilePaused()
    {
        Time.timeScale = 0;
        var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Galmuri11-Bold SDF.asset");
        Assert.That(font, Is.Not.Null);
        var panel = PSH_Script_EffectOptionsPanel.Create(font, 150);
        panel.transform.SetParent(root.transform, false);
        panel.gameObject.SetActive(true);
        var es = EventSystem.current != null ? EventSystem.current : Child("Events", typeof(EventSystem)).GetComponent<EventSystem>();
        var camera = Child("UI Camera", typeof(Camera)).GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -100);
        camera.cullingMask = 1 << 5;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        var canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 100;
        var previous = GraphicsSettings.defaultRenderPipeline;
        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        var pipeline = UniversalRenderPipelineAsset.Create(renderer);
        GraphicsSettings.defaultRenderPipeline = pipeline;
        yield return null;
        try
        {
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                var target = RenderTexture.GetTemporary(size.x, size.y, 24);
                camera.targetTexture = target;
                yield return null;
                try
                {
                    Canvas.ForceUpdateCanvases();
                    var toggles = panel.GetComponentsInChildren<Toggle>();
                    Assert.That(toggles.Length, Is.EqualTo(6));
                    Click(es, camera, panel.transform.Find("Panel/DefaultMode").gameObject);
                    Assert.That(toggles.All(t => t.isOn), Is.True);
                    foreach (var toggle in toggles)
                    {
                        Click(es, camera, toggle.gameObject);
                        Assert.That(toggle.isOn, Is.False);
                        Assert.That(PSH_Script_EffectSettings.Get((Effect)Enum.Parse(typeof(Effect), toggle.name)), Is.False);
                        Click(es, camera, toggle.gameObject);
                    }
                    Click(es, camera, panel.transform.Find("Panel/ReduceEffects").gameObject);
                    Assert.That(toggles.All(t => !t.isOn), Is.True);
                    Assert.That(PSH_Script_EffectSettings.IsEnabled, Is.False);
                    Click(es, camera, panel.transform.Find("Panel/CameraShake").gameObject);
                    Assert.That(PSH_Script_EffectSettings.CameraShakeEnabled, Is.True);
                    Assert.That(panel.transform.Find("Panel/Status").GetComponent<TMP_Text>().text, Does.StartWith("사용자 설정"));
                    foreach (var label in panel.GetComponentsInChildren<TMP_Text>())
                    {
                        label.ForceMeshUpdate();
                        Assert.That(label.isTextOverflowing, Is.False, label.name + ": " + label.text);
                    }
                    RenderPipelineManager.beginCameraRendering += EmitUI;
                    try { RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); }
                    finally { RenderPipelineManager.beginCameraRendering -= EmitUI; }
                    var old = RenderTexture.active;
                    RenderTexture.active = target;
                    var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); pixels.Apply();
                    RenderTexture.active = old;
                    Assert.That(pixels.GetPixels32().Count(c => c.r > 150 && c.g > 150 && c.b > 150), Is.GreaterThan(1000));
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "..", $"effects-options-{size.x}.png"), pixels.EncodeToPNG());
                    Object.Destroy(pixels);
                }
                finally { camera.targetTexture = null; RenderTexture.ReleaseTemporary(target); }
            }
        }
        finally
        {
            GraphicsSettings.defaultRenderPipeline = previous;
            Object.Destroy(pipeline); Object.Destroy(renderer);
        }
        panel.transform.Find("Panel/Close").GetComponent<Button>().onClick.Invoke();
        Assert.That(panel.gameObject.activeSelf, Is.False);
    }
    static void EmitUI(ScriptableRenderContext context, Camera camera) => ScriptableRenderContext.EmitGeometryForCamera(camera);

    static void Click(EventSystem es, Camera camera, GameObject control)
    {
        var rect = (RectTransform)control.transform;
        var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        var data = new PointerEventData(es) { position = point, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>();
        es.RaycastAll(data, hits);
        Assert.That(hits.Count, Is.GreaterThan(0), control.name);
        Assert.That(hits[0].gameObject.GetComponentInParent<Selectable>().gameObject, Is.EqualTo(control));
        ExecuteEvents.Execute(control, data, ExecuteEvents.pointerClickHandler);
    }
}
