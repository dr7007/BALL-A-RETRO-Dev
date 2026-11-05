using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class LocalizationUITests
{
    GameObject root;
    Canvas canvas;
    Camera camera;
    RenderPipelineAsset oldPipeline;
    UniversalRendererData renderer;
    UniversalRenderPipelineAsset pipeline;
    Dictionary<string,int?> preferences=new Dictionary<string,int?>();
    [UnitySetUp] public IEnumerator Setup()
    {
        LocaleTestContext.Select("ko");yield return LocalizationSettings.InitializationOperation;
        root=new GameObject("Localized UI fixture");
        foreach(var effect in (PSH_Script_EffectSettings.Effect[])Enum.GetValues(typeof(PSH_Script_EffectSettings.Effect)))
        { string k=PSH_Script_EffectSettings.KeyFor(effect);preferences[k]=PlayerPrefs.HasKey(k)?PlayerPrefs.GetInt(k):(int?)null; }
        preferences[PSH_Script_EffectSettings.PreferenceKey]=PlayerPrefs.HasKey(PSH_Script_EffectSettings.PreferenceKey)?PlayerPrefs.GetInt(PSH_Script_EffectSettings.PreferenceKey):(int?)null;
        PSH_Script_EffectSettings.SetEnabled(true);
        canvas=Child("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.gameObject.layer=5;canvas.renderMode=RenderMode.ScreenSpaceCamera;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
        camera=Child("Camera",typeof(Camera)).GetComponent<Camera>();camera.transform.position=new Vector3(0,0,-100);camera.cullingMask=1<<5;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;canvas.worldCamera=camera;canvas.planeDistance=100;
        oldPipeline=GraphicsSettings.defaultRenderPipeline;renderer=ScriptableObject.CreateInstance<UniversalRendererData>();pipeline=UniversalRenderPipelineAsset.Create(renderer);GraphicsSettings.defaultRenderPipeline=pipeline;
        if(EventSystem.current==null)Child("Events",typeof(EventSystem));
        Time.timeScale=0;yield return null;
    }
    [TearDown] public void Cleanup()
    {
        foreach(var p in Object.FindObjectsByType<PSH_Script_EffectOptionsPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(p.gameObject);
        Object.DestroyImmediate(root);GraphicsSettings.defaultRenderPipeline=oldPipeline;Object.DestroyImmediate(pipeline);Object.DestroyImmediate(renderer);
        foreach(var p in preferences)if(p.Value.HasValue)PlayerPrefs.SetInt(p.Key,p.Value.Value);else PlayerPrefs.DeleteKey(p.Key);
        PlayerPrefs.Save();typeof(PSH_Script_EffectSettings).GetMethod("ResetStatics",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        LocaleTestContext.Select("ko");Time.timeScale=1;
    }
    GameObject Child(string name,params Type[] types){var go=new GameObject(name,types);go.transform.SetParent(root.transform,false);return go;}
    GameObject Prefab(string path){var p=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.That(p,Is.Not.Null,path);return Object.Instantiate(p,canvas.transform);}
    static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(target);
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(target,value);
    static IEnumerable<string> Texts(GameObject go)=>go.GetComponentsInChildren<TMP_Text>().Select(t=>t.text);
    GameObject SettingsMenu(out GameObject warning)
    {
        var menu=Prefab("Assets/Public/Main_Game/UI_Game/Prefabs/Panel_Setting.prefab");
        warning=menu.GetComponentsInChildren<PSH_Script_LocalizedText>(true)
            .Single(t=>Field<string>(t,"key")=="Warning_Quit").transform.parent.gameObject;
        // The real SettingController hides this until Quit is pressed; the fixture omits that controller.
        warning.SetActive(false);
        return menu;
    }

    [UnityTest] public IEnumerator SystemDropdownUpdatesLabelsAndOpenEffectsWithoutChangingOptions()
    {
        var menu=SettingsMenu(out _);
        yield return null;yield return null;
        var language=menu.GetComponentInChildren<PSH_Script_LanguageManager>();
        var dropdown=Field<TMP_Dropdown>(language,"languageDropdown");
        var display=menu.GetComponentInChildren<PSH_Script_ResolutionSettings>();
        int screenMode=display.screenModeDropdown.value;
        var master=menu.GetComponentInChildren<PSH_Script_ToggleUIController>();
        PSH_Script_EffectSettings.Set(PSH_Script_EffectSettings.Effect.Particles,false);
        var sliders=menu.GetComponentsInChildren<Slider>();var values=sliders.Select(s=>s.value).ToArray();
        dropdown.value=2;yield return LocalizationSettings.InitializationOperation;yield return null;yield return null;
        Assert.That(Texts(menu),Does.Contain("視覚効果"));
        Assert.That(Texts(menu),Does.Contain("効果音の音量"));
        Assert.That(Texts(menu),Does.Contain("解像度"));
        Assert.That(Texts(menu),Does.Contain("詳細設定"));
        Assert.That(master.label.text,Is.EqualTo("一部"));
        Assert.That(display.screenModeDropdown.value,Is.EqualTo(screenMode));
        Assert.That(sliders.Select(s=>s.value),Is.EqualTo(values));
        master.OpenDetails();yield return null;
        var panel=Object.FindFirstObjectByType<PSH_Script_EffectOptionsPanel>();Assert.That(Texts(panel.gameObject),Does.Contain("カメラの揺れ"));
        LocaleTestContext.Select("en");yield return LocalizationSettings.InitializationOperation;yield return null;
        Assert.That(Texts(panel.gameObject),Does.Contain("Camera Shake"));
        Assert.That(Texts(menu),Does.Contain("Visual Effects"));
        Assert.That(master.label.text,Is.EqualTo("Some"));
        Assert.That(PSH_Script_EffectSettings.ParticlesEnabled,Is.False);
        Assert.That(Time.timeScale,Is.Zero);
    }

    [UnityTest] public IEnumerator CardsAndInventoryKeepTheSameUpgradeWhenLanguageChanges()
    {
        var card=Prefab("Assets/Public/Main_Game/UI_Game/Prefabs/Button_Choice.prefab").GetComponent<CJS_Script_ChoiceCard>();
        int picked=0;var data=new CJS_ChoiceData{localizationKey="Choice_6",name="보충의 시간",description="플리퍼의 사용횟수를 5 늘립니다.",rarity="Common",funcIdx=6};
        card.Bind(data,d=>{Assert.That(d,Is.SameAs(data));picked++;});
        var inventory=Child("Inventory");var slot=inventory.AddComponent<CJS_Script_ChoiceSlot>();
        var name=Child("Inventory name",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        Set(slot,"txtName",name);slot.Bind(CJS_ChoiceSnapshot.From(data));
        foreach(var code in new[]{"en","ja","ko"})
        {
            LocaleTestContext.Select(code);yield return LocalizationSettings.InitializationOperation;yield return null;
            Assert.That(Field<TMP_Text>(card,"txtName").text,Is.EqualTo(PSH_Script_Localization.Text("Choice_6_Name")));
            Assert.That(Field<TMP_Text>(card,"txtDesc").text,Is.EqualTo(PSH_Script_Localization.Text("Choice_6_Description")));
            Assert.That(name.text,Is.EqualTo(PSH_Script_Localization.Text("Choice_6_Name")));
            Assert.That(picked,Is.Zero);
        }
        card.GetComponent<Button>().onClick.Invoke();Assert.That(picked,Is.EqualTo(1));
    }

    [UnityTest] public IEnumerator AllThreeLanguagesFitSettingsAndEffectsAtFhdAndQhd()
    {
        var menu=SettingsMenu(out var warning);yield return null;yield return null;
        foreach(var code in new[]{"ko","en","ja"})
        {
            LocaleTestContext.Select(code);yield return LocalizationSettings.InitializationOperation;yield return null;yield return null;
            foreach(int width in new[]{1920,2560}) yield return Capture(menu,code+"-settings",width);
            warning.SetActive(true);yield return null;
            yield return Capture(warning,code+"-quit-warning",1920);
            warning.SetActive(false);
            menu.GetComponentInChildren<PSH_Script_ToggleUIController>().OpenDetails();yield return null;
            var panel=Object.FindFirstObjectByType<PSH_Script_EffectOptionsPanel>();
            var overlay=panel.GetComponent<Canvas>();overlay.renderMode=RenderMode.ScreenSpaceCamera;overlay.worldCamera=camera;overlay.planeDistance=90;
            foreach(int width in new[]{1920,2560}) yield return Capture(panel.gameObject,code+"-effects",width);
            panel.gameObject.SetActive(false);
        }
    }
    [UnityTest] public IEnumerator EveryUpgradeDescriptionFitsTheRealCardInEveryLanguage()
    {
        var card=Prefab("Assets/Public/Main_Game/UI_Game/Prefabs/Button_Choice.prefab").GetComponent<CJS_Script_ChoiceCard>();
        var rect=(RectTransform)card.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.localScale=Vector3.one;
        foreach(var code in new[]{"ko","en","ja"})
        {
            LocaleTestContext.Select(code);yield return LocalizationSettings.InitializationOperation;
            for(int i=0;i<9;i++)
            {
                card.Bind(new CJS_ChoiceData{localizationKey="Choice_"+i,rarity="Legendary"},null);yield return null;
                CheckFits(card.gameObject);
            }
            yield return Capture(card.gameObject,code+"-upgrade",1920);
        }
    }
    [UnityTest] public IEnumerator LobbyNicknameRecordsAndResultTranslateWhenOpened()
    {
        var paths=new[]{
            "Assets/Public/Main_Lobby/UI_Lobby/Prefabs/Canvas_Lobby.prefab",
            "Assets/Public/Main_Lobby/UI_Lobby/Prefabs/Panel_InputEmail.prefab",
            "Assets/Public/UI_Public/Prefabs/Panel_Rank.prefab",
            "Assets/Public/Main_Game/UI_Game/Prefabs/Panel_GameEnd.prefab"};
        foreach(var path in paths)
        {
            var screen=Prefab(path);screen.SetActive(false);
            foreach(var code in new[]{"ko","en","ja"})
            {
                LocaleTestContext.Select(code);yield return LocalizationSettings.InitializationOperation;
                screen.SetActive(true);
                foreach(var label in screen.GetComponentsInChildren<PSH_Script_LocalizedText>(true))
                    for(var t=label.transform;t!=screen.transform;t=t.parent)t.gameObject.SetActive(true);
                yield return null;Canvas.ForceUpdateCanvases();
                foreach(var label in screen.GetComponentsInChildren<PSH_Script_LocalizedText>())
                {
                    string key=Field<string>(label,"key");if(string.IsNullOrEmpty(key))continue;
                    var tmp=label.GetComponent<TMP_Text>();Assert.That(tmp.text,Is.EqualTo(PSH_Script_Localization.Text(key)),path+" / "+key);
                    tmp.ForceMeshUpdate();Assert.That(tmp.isTextOverflowing,Is.False,path+" / "+code+" / "+key);
                }
                screen.SetActive(false);
            }
            Object.DestroyImmediate(screen);
        }
    }
    static void CheckFits(GameObject go)
    {
        Canvas.ForceUpdateCanvases();
        foreach(var label in go.GetComponentsInChildren<TMP_Text>())
        {label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.name+": "+label.text);}
    }
    IEnumerator Capture(GameObject subject,string name,int width)
    {
        var target=RenderTexture.GetTemporary(width,width*9/16,24);camera.targetTexture=target;yield return null;
        try
        {
            CheckFits(subject);
            RenderPipelineManager.beginCameraRendering+=Emit;
            try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});}
            finally{RenderPipelineManager.beginCameraRendering-=Emit;}
            var old=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();RenderTexture.active=old;
            Assert.That(pixels.GetPixels32().Count(c=>c.r>150&&c.g>150&&c.b>150),Is.GreaterThan(300),"Text must actually render");
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"..",name+"-"+width+".png"),pixels.EncodeToPNG());Object.Destroy(pixels);
        }
        finally{camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);}
    }
    static void Emit(ScriptableRenderContext context,Camera cam)=>ScriptableRenderContext.EmitGeometryForCamera(cam);
}
