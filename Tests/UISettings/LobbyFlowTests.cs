using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PSH;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class LobbyFlowTests
{
    GameObject root, events, loaderObject, result;
    EventSystem eventSystem;
    PSH_Script_SceneLoader loader;
    [UnitySetUp] public IEnumerator Setup()
    {
#if BALLARETRO_STEAM
        Steamworks.Fixture.Reset();Steamworks.LeaderboardFixture.Reset();
        typeof(SteamManager).GetMethod("InitOnPlayMode",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
#endif
        LocaleTestContext.Select("ko");yield return LocalizationSettings.InitializationOperation;
        Time.timeScale=1;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
        events=new GameObject("Events",typeof(EventSystem));eventSystem=events.GetComponent<EventSystem>();
        loaderObject=new GameObject("Scene Loader",typeof(PSH_Script_SceneLoader));loader=loaderObject.GetComponent<PSH_Script_SceneLoader>();
        root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Public/Main_Lobby/UI_Lobby/Prefabs/Canvas_Lobby.prefab"));
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        yield return null;yield return null;
    }
    [TearDown] public void Cleanup()
    {
        if(result)Object.DestroyImmediate(result);
        Object.DestroyImmediate(root);Object.DestroyImmediate(loaderObject);Object.DestroyImmediate(events);
        foreach(var cursor in Object.FindObjectsByType<PSH_Script_CursorManager>(FindObjectsSortMode.None))Object.DestroyImmediate(cursor.gameObject);
        foreach(var service in Object.FindObjectsByType<CJS_Script_PinballRankingService>(FindObjectsSortMode.None))Object.DestroyImmediate(service.gameObject);
        foreach(var steam in Object.FindObjectsByType<SteamManager>(FindObjectsSortMode.None))Object.DestroyImmediate(steam.gameObject);
        LocaleTestContext.Select("ko");Time.timeScale=1;
    }
    void Click(Component control)
    {
        Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)control.transform;
        var canvas=root.GetComponent<Canvas>();
        var data=new PointerEventData(eventSystem){position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();eventSystem.RaycastAll(data,hits);
        Assert.That(hits,Is.Not.Empty,$"{control.name} at {data.position}; screen={Screen.width}x{Screen.height}; scale={rect.lossyScale}");
        Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(control.gameObject),control.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    PSH_Script_StartButton Tile(string name)=>root.GetComponentsInChildren<PSH_Script_StartButton>().Single(b=>b.name==name);
    [Test] public void GameTileLoadsDirectlyWithoutNickname_AndDoubleClickLoadsOnce()
    {
        Click(Tile("Button_Start"));
        Assert.That(loader.Destination,Is.EqualTo("Main_Scene_Game"));
        var nickname=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Panel_InputEmail");
        Assert.That(nickname.gameObject.activeSelf,Is.False);
        Click(Tile("Button_Start"));Assert.That(loader.Requests,Is.EqualTo(1));
    }
    [Test] public void TutorialTileUsesItsOwnDestination()
    {Click(Tile("Button_tutorial"));Assert.That(loader.Destination,Is.EqualTo("Main_Scene_Tutorial"));}
    [UnityTest] public IEnumerator RankingRemainsClickableAfterGameOverOnBothPlatforms()
    {
        Assert.That(root.GetComponentInChildren<PSH_Script_WorldRankingUI>(),Is.Not.Null);
        var icon=root.GetComponentsInChildren<Button>().Single(b=>b.name=="OpenWorldRanking").GetComponent<Image>();
        Assert.That(icon.sprite.name,Is.EqualTo("RankBtn_98-104"));
        root.SetActive(false);
        result=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Public/Main_Game/UI_Game/Prefabs/Canvas_GameScene.prefab"));
        result.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        // Match Main_Scene_Game's serialized overrides of the shared canvas prefab.
        var scaler=result.GetComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=0;
        var settings=result.GetComponent<PSH_Script_SettingController>();
        settings.panelSetting=result.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Panel_Setting").gameObject;
        settings.panelWarning=settings.panelSetting.GetComponentsInChildren<Transform>(true).Single(t=>string.Equals(t.name,"Image_Warning",System.StringComparison.OrdinalIgnoreCase)).gameObject;
        result.SetActive(true);
        var ending=result.GetComponentInChildren<CJS_Script_GameOverUI>(true);
        Assert.That(ending,Is.Not.Null);
        for(var t=ending.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);
        ending.panelGameEnd.SetActive(true);yield return null;
        var entry=result.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Image_Rank_bg");
        if (BallARetroPlatform.IsDemoBuild)
        {
            Assert.That(entry.gameObject.activeSelf, Is.False);
            Assert.That(ending.GetComponentsInChildren<BallARetroDemoPresentation>(true).Any(t => t.name == "DemoResult"), Is.True);
        }
        else Assert.That(entry.gameObject.activeInHierarchy,Is.True);
        Time.timeScale=0;
        Click(result.GetComponentsInChildren<Button>().Single(b=>b.name=="Image_RankOpen"));
        yield return null; // The dynamically created Canvas registers its graphics for this frame.
        var ui=result.GetComponentInChildren<PSH_Script_WorldRankingUI>();
        Assert.That(ui,Is.Not.Null);
        Assert.That(ui.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Title").text,
            Is.EqualTo(BallARetroPlatform.IsDemoBuild ? PSH_Script_Localization.Text("Demo_RankingTitle") : "SCORE  RANKING  v2"));
        if(!BallARetroPlatform.IsSteamBuild)
            Assert.That(ui.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Status").text,Is.EqualTo(PSH_Script_Localization.Text("Rank_SteamOnly")));
        Click(ui.GetComponentsInChildren<Button>().Single(b=>b.name=="Close"));
        Assert.That(ui.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="Title"),Is.False);
        Assert.That(ending.panelGameEnd.activeInHierarchy,Is.True);
        Assert.That(Time.timeScale,Is.Zero);
    }
#if BALLARETRO_STEAM && !BALLARETRO_DEMO
    [UnityTest] public IEnumerator WorldRankingRendersInThreeLanguages_AndClosesWithTheMouse()
    {
        Steamworks.LeaderboardFixture.Rows=Enumerable.Range(1,10).Select(i=>new Steamworks.LeaderboardEntry_t{
            m_steamIDUser=new Steamworks.CSteamID{Value=(ulong)i},m_nGlobalRank=i,m_nScore=(11-i)*1500}).ToArray();
        Steamworks.LeaderboardFixture.Steps=Enumerable.Range(1,10).Select(i=>i<4?3:2).ToArray();
        Steamworks.LeaderboardFixture.PersonalRows=new[]{new Steamworks.LeaderboardEntry_t{
            m_steamIDUser=new Steamworks.CSteamID{Value=Steamworks.Fixture.UserId},m_nGlobalRank=137,m_nScore=1250}};
        Steamworks.LeaderboardFixture.PersonalSteps=new[]{2};
        var ui=root.GetComponentInChildren<PSH_Script_WorldRankingUI>();
        var open=ui.GetComponentsInChildren<Button>().Single(b=>b.name=="OpenWorldRanking");
        var anchor=(RectTransform)open.transform;
        Assert.That(anchor.anchorMax,Is.EqualTo(new Vector2(1,0)));
        Click(open);
        float deadline=Time.realtimeSinceStartup+2;
        while(ui.GetComponentsInChildren<TMP_Text>().Count(t=>t.name=="Player")<10&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(ui.GetComponentsInChildren<TMP_Text>().Count(t=>t.name=="Player"),Is.EqualTo(10));
        var canvas=root.GetComponent<Canvas>();
        var cameraObject=new GameObject("Ranking capture",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-100);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=100;
        try
        {
            foreach(var code in new[]{"ko","en","ja"})
            {
                LocaleTestContext.Select(code);yield return LocalizationSettings.InitializationOperation;yield return null;
                Assert.That(ui.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Title").text,Is.EqualTo("SCORE  RANKING  v2"));
                Assert.That(ui.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="Position").text,Is.EqualTo("1ST"));
                Assert.That(ui.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="Player").color,Is.EqualTo(Color.red));
                Assert.That(ui.GetComponentsInChildren<TMP_Text>().Count(t=>t.name=="Step"&&t.text=="3"),Is.EqualTo(3));
                Assert.That(ui.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Status").text,Is.Empty);
                Assert.That(ui.GetComponentsInChildren<TMP_Text>().Last(t=>t.name=="Position").text,Is.EqualTo("137TH"));
                Assert.That(ui.GetComponentsInChildren<TMP_Text>().Last(t=>t.name=="Player").color,Is.EqualTo((Color)new Color32(255,171,0,255)));
                foreach(int width in new[]{1920,2560})
                {
                    var target=RenderTexture.GetTemporary(width,width*9/16,24);camera.targetTexture=target;
                    yield return null;Canvas.ForceUpdateCanvases();
                    foreach(var text in ui.GetComponentsInChildren<TMP_Text>())
                    {text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,code+"/"+text.name);}
                    camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
                    var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"..",$"world-ranking-{code}-{width}.png"),pixels.EncodeToPNG());
                    Object.DestroyImmediate(pixels);RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
                }
            }
            Click(ui.GetComponentsInChildren<Button>().Single(b=>b.name=="Close"));
            Assert.That(ui.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="Title"),Is.False);
        }
        finally{canvas.worldCamera=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;Object.DestroyImmediate(cameraObject);}
    }
    [UnityTest] public IEnumerator ClosingDuringLoadDoesNotReopenOrPopulateAClosedPanel()
    {
        Steamworks.LeaderboardFixture.Hold=true;
        var ui=root.GetComponentInChildren<PSH_Script_WorldRankingUI>();
        ui.Show();ui.Close();Steamworks.LeaderboardFixture.Hold=false;
        yield return null;yield return null;yield return null;
        Assert.That(ui.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="Title"),Is.False);
    }
    void SetTopTen(int personalRank)
    {
        Steamworks.LeaderboardFixture.Rows=Enumerable.Range(1,10).Select(i=>new Steamworks.LeaderboardEntry_t{
            m_steamIDUser=new Steamworks.CSteamID{Value=i==personalRank?Steamworks.Fixture.UserId:(ulong)i},
            m_nGlobalRank=i,m_nScore=(11-i)*1500}).ToArray();
        Steamworks.LeaderboardFixture.Steps=Enumerable.Repeat(3,10).ToArray();
        Steamworks.LeaderboardFixture.PersonalRows=personalRank>10?new[]{new Steamworks.LeaderboardEntry_t{
            m_steamIDUser=new Steamworks.CSteamID{Value=Steamworks.Fixture.UserId},m_nGlobalRank=personalRank,m_nScore=1000}}:System.Array.Empty<Steamworks.LeaderboardEntry_t>();
    }
    IEnumerator Loaded(PSH_Script_WorldRankingUI ui)
    {
        var refresh=ui.GetComponentsInChildren<Button>().Single(b=>b.name=="Refresh");
        float deadline=Time.realtimeSinceStartup+2;
        while(!refresh.interactable&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(refresh.interactable,Is.True);
        yield return null;
    }
    [UnityTest] public IEnumerator OnlyTheSteamAccountIsHighlighted_AndTopTenPlayersKeepTenthPlaceVisible()
    {
        Steamworks.Fixture.Name="World Player 1"; // Same nickname as first place, different SteamID.
        var ui=root.GetComponentInChildren<PSH_Script_WorldRankingUI>();
        foreach(int ownRank in new[]{1,5,9,10,11,89,137})
        {
            SetTopTen(ownRank);ui.Show();yield return Loaded(ui);
            var names=ui.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="Player").ToArray();
            var positions=ui.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="Position").ToArray();
            Assert.That(names.Length,Is.EqualTo(10));
            var orange=(Color)new Color32(255,171,0,255);
            Assert.That(names.Count(t=>t.color==orange),Is.EqualTo(1));
            int ownIndex=ownRank<=10?ownRank-1:9;
            Assert.That(names[ownIndex].text,Is.EqualTo(Steamworks.Fixture.Name));
            Assert.That(names[ownIndex].color,Is.EqualTo(orange));
            Assert.That(positions[ownIndex].text,Does.StartWith(ownRank.ToString()));
            Assert.That(positions.Last().text,Is.EqualTo(ownRank<=10?"10TH":ownRank+"TH"));
            if(ownRank>10)Assert.That(positions.Take(9).Select(t=>t.text),Is.EqualTo(new[]{"1ST","2ND","3RD","4TH","5TH","6TH","7TH","8TH","9TH"}));
            Assert.That(ui.GetComponentsInChildren<Transform>().Count(t=>t.name=="PersonalRow"),Is.EqualTo(1));
        }
    }
    [UnityTest] public IEnumerator AnUnrankedPlayerHasTheirSteamNameWithoutInventedRankOrScore()
    {
        SetTopTen(0);
        var ui=root.GetComponentInChildren<PSH_Script_WorldRankingUI>();ui.Show();yield return Loaded(ui);
        var own=ui.GetComponentsInChildren<Transform>().Single(t=>t.name=="PersonalRow");
        Assert.That(own.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Player").text,Is.EqualTo(Steamworks.Fixture.Name));
        foreach(var text in own.GetComponentsInChildren<TMP_Text>().Where(t=>t.name!="Player"))Assert.That(text.text,Is.EqualTo("—"));
        Assert.That(ui.GetComponentsInChildren<TMP_Text>().Count(t=>t.name=="Player"),Is.EqualTo(10));
    }
    [UnityTest] public IEnumerator PersonalRowPulsesWhileGameIsPaused_AndStopsWithScreenEffectsDisabled()
    {
        var keys=System.Enum.GetValues(typeof(PSH_Script_EffectSettings.Effect)).Cast<PSH_Script_EffectSettings.Effect>()
            .Select(PSH_Script_EffectSettings.KeyFor).Concat(new[]{PSH_Script_EffectSettings.PreferenceKey});
        var preferences=keys.ToDictionary(k=>k,k=>PlayerPrefs.HasKey(k)?(int?)PlayerPrefs.GetInt(k):null);
        try
        {
            PSH_Script_EffectSettings.SetEnabled(true);SetTopTen(137);Time.timeScale=0;
            var ui=root.GetComponentInChildren<PSH_Script_WorldRankingUI>();ui.Show();yield return Loaded(ui);
            var own=ui.GetComponentsInChildren<Transform>().Single(t=>t.name=="PersonalRow");
            float minimum=2,maximum=0,end=Time.realtimeSinceStartup+2.1f;
            while(Time.realtimeSinceStartup<end)
            {minimum=Mathf.Min(minimum,own.localScale.x);maximum=Mathf.Max(maximum,own.localScale.x);yield return null;}
            Assert.That(maximum-minimum,Is.GreaterThan(.025f));
            Assert.That(minimum,Is.GreaterThanOrEqualTo(1));Assert.That(maximum,Is.LessThanOrEqualTo(1.031f));
            PSH_Script_EffectSettings.Set(PSH_Script_EffectSettings.Effect.ScreenEffects,false);yield return null;yield return null;
            Assert.That(own.localScale,Is.EqualTo(Vector3.one));
            Assert.That(own.GetComponentsInChildren<TMP_Text>().All(t=>t.color==(Color)new Color32(255,171,0,255)),Is.True);
        }
        finally
        {
            foreach(var pair in preferences)if(pair.Value.HasValue)PlayerPrefs.SetInt(pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
            typeof(PSH_Script_EffectSettings).GetMethod("ResetStatics",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            _=PSH_Script_EffectSettings.IsEnabled;
        }
    }
    [UnityTest] public IEnumerator LoggedOutEmptyAndFailedStatesStayLocalizedAndReadable()
    {
        var ui=root.GetComponentInChildren<PSH_Script_WorldRankingUI>();
        Steamworks.Fixture.LoggedOn=false;
        ui.Show();
        foreach(var code in new[]{"ko","en","ja"})
        {
            LocaleTestContext.Select(code);yield return LocalizationSettings.InitializationOperation;yield return null;
            var status=ui.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Status");
            status.ForceMeshUpdate();
            Assert.That(status.text,Is.EqualTo(PSH_Script_Localization.Text("Nickname_Steam")));
            Assert.That(status.isTextOverflowing,Is.False,code+"/login message");
        }
        Steamworks.Fixture.LoggedOn=true;
        foreach(bool fail in new[]{false,true})
        {
            Steamworks.LeaderboardFixture.DownloadFails=fail;
            Click(ui.GetComponentsInChildren<Button>().Single(b=>b.name=="Refresh"));
            var refresh=ui.GetComponentsInChildren<Button>().Single(b=>b.name=="Refresh");
            float deadline=Time.realtimeSinceStartup+2;
            while(!refresh.interactable&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(refresh.interactable,Is.True);
            var status=ui.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Status");
            status.ForceMeshUpdate();
            Assert.That(status.text,Is.EqualTo(PSH_Script_Localization.Text(fail?"Rank_LoadFailed":"Rank_Empty")));
            Assert.That(status.isTextOverflowing,Is.False);
        }
    }
#endif
}
