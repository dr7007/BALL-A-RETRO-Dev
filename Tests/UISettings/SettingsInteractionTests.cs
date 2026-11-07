using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using TMPro;
using Object = UnityEngine.Object;

public class SettingsInteractionTests
{
    GameObject root, events;
    EventSystem eventSystem;
    bool oldCursorVisible;
    CursorLockMode oldCursorLock;
    float oldTime;
    readonly List<Scene> scenes = new List<Scene>();
    [SetUp] public void Setup()
    {
        oldTime=Time.timeScale;oldCursorVisible=Cursor.visible;oldCursorLock=Cursor.lockState;
        events=new GameObject("Settings input events",typeof(EventSystem));eventSystem=events.GetComponent<EventSystem>();
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Time.timeScale=1;
    }
    [TearDown] public void Cleanup()
    {
        if(root)Object.DestroyImmediate(root);Object.DestroyImmediate(events);
        Time.timeScale=oldTime;Cursor.lockState=oldCursorLock;Cursor.visible=oldCursorVisible;
    }
    [UnityTearDown] public IEnumerator UnloadScenes()
    {
        foreach(var scene in scenes) if(scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        scenes.Clear();
        LocaleTestContext.Select("ko");
    }
    PSH_Script_SettingController Create(bool lobby, string sceneName=null)
    {
        string path=lobby ? "Assets/Public/Main_Lobby/UI_Lobby/Prefabs/Canvas_Lobby.prefab"
            : "Assets/Public/Main_Game/UI_Game/Prefabs/Canvas_GameScene.prefab";
        root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        var scene=SceneManager.CreateScene(sceneName ?? (lobby ? "Main_Scene_Lobby" : "Main_Scene_Game"));
        scenes.Add(scene);SceneManager.MoveGameObjectToScene(root,scene);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var controller=root.GetComponent<PSH_Script_SettingController>();
        Assert.That(controller,Is.Not.Null);
        // Game and tutorial scenes supply these three object references as prefab overrides.
        if(!lobby)
        {
            // Match the scale overrides shared by Main_Scene_Game and Main_Scene_Tutorial.
            var scaler=root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);
            scaler.matchWidthOrHeight=0;
            var transforms=root.GetComponentsInChildren<Transform>(true);
            controller.panelSetting=transforms.Single(t=>t.name=="Panel_Setting").gameObject;
            controller.panelWarning=controller.panelSetting.GetComponentsInChildren<Transform>(true)
                .Single(t=>string.Equals(t.name,"Image_Warning",System.StringComparison.OrdinalIgnoreCase)).gameObject;
            controller.goToLobbyButton=controller.panelSetting.GetComponentsInChildren<Transform>(true)
                .Single(t=>t.name=="Button_GoToLobby").gameObject;
        }
        return controller;
    }
    Button CloseButton(PSH_Script_SettingController controller)
        => controller.panelSetting.GetComponentsInChildren<Button>(true)
            .Single(b=>b.name=="Button_Close"&&!b.transform.IsChildOf(controller.panelWarning.transform));
    void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)button.transform;
        var data=new PointerEventData(eventSystem){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();eventSystem.RaycastAll(data,hits);
        Assert.That(hits,Is.Not.Empty,$"No hit on {root.name}/{button.name} at {data.position}; screen={Screen.width}x{Screen.height}, scale={root.transform.lossyScale}");
        Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(button.gameObject),"Another graphic intercepted the close button.");
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    [UnityTest] public IEnumerator LobbyCloseButtonClosesTheRealSettingsPanel()
    {yield return VerifyClose(true);}
    [UnityTest] public IEnumerator GameplayAndTutorialCloseButtonClosesTheSharedSettingsPanel()
    {yield return VerifyClose(false);}
    [UnityTest] public IEnumerator WarningCloseAndCancelKeepTheSystemMenuPaused()
    {
        foreach(bool lobby in new[]{true,false})
        {
            var controller=Create(lobby);yield return null;Time.timeScale=.75f;
            controller.ToggleMenu();yield return null;
            foreach(string name in new[]{"Button_Close (1)","Button_no"})
            {
                Click(ExitButton(controller,lobby));yield return null;
                var button=controller.panelWarning.GetComponentsInChildren<Button>().Single(b=>b.name==name);
                Click(button);yield return null;
                Assert.That(controller.panelWarning.activeSelf,Is.False,name);
                Assert.That(controller.panelSetting.activeSelf,Is.True);Assert.That(Time.timeScale,Is.Zero);
            }
            Click(CloseButton(controller));Assert.That(Time.timeScale,Is.EqualTo(.75f));
            Object.DestroyImmediate(root);root=null;
        }
    }
    Button ExitButton(PSH_Script_SettingController controller,bool lobby)
        => controller.panelSetting.GetComponentsInChildren<Button>()
            .Single(b=>b.name==(lobby ? "Button_CloseGame" : "Button_GoToLobby"));
    Button ConfirmButton(PSH_Script_SettingController controller)
        => controller.panelWarning.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Button_yes");
    void ExpectLabel(GameObject scope,string key)
    {
        Assert.That(scope.GetComponentsInChildren<TMP_Text>().Select(t=>t.text),Does.Contain(PSH_Script_Localization.Text(key)));
        foreach(var text in scope.GetComponentsInChildren<TMP_Text>())
        {
            text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.name+": "+text.text);
        }
    }
    [UnityTest] public IEnumerator BothPlayScenesOfferLocalizedReturnAndCancelPreservesTheRun()
    {
        foreach(string sceneName in new[]{"Main_Scene_Game","Main_Scene_Tutorial"})
        {
            var controller=Create(false,sceneName);
            var reset=root.AddComponent<KHS_Script_ResetController>();
            yield return null;Time.timeScale=.75f;controller.ToggleMenu();yield return null;
            Assert.That(controller.goToLobbyButton.activeInHierarchy,Is.True);
            Assert.That(controller.panelSetting.GetComponentsInChildren<Button>().Any(b=>b.name=="Button_CloseGame"),Is.False);
            foreach(string locale in new[]{"ko","en","ja"})
            {
                LocaleTestContext.Select(locale);yield return LocalizationSettings.InitializationOperation;yield return null;
                ExpectLabel(ExitButton(controller,false).gameObject,"Button_ReturnToLobby");
                Click(ExitButton(controller,false));yield return null;
                ExpectLabel(controller.panelWarning,"Warning_ReturnToLobby");
                ExpectLabel(ConfirmButton(controller).gameObject,"Button_ReturnToLobby");
                var cancel=controller.panelWarning.GetComponentsInChildren<Button>().Single(b=>b.name=="Button_no");
                ExpectLabel(cancel.gameObject,"Button_No");
                Assert.That(reset.LobbyRequests,Is.Zero);Assert.That(Time.timeScale,Is.Zero);
                // Locale changes also update an already open confirmation.
                LocaleTestContext.Select(locale=="ko" ? "en" : "ko");yield return null;
                ExpectLabel(controller.panelWarning,"Warning_ReturnToLobby");
                Click(cancel);yield return null;
                controller.ConfirmExit(); // stale confirmation callbacks after cancellation are ignored
                Assert.That(reset.LobbyRequests,Is.Zero);Assert.That(controller.panelSetting.activeSelf,Is.True);
                Assert.That(Time.timeScale,Is.Zero);
            }
            Click(ExitButton(controller,false));yield return null;
            controller.ToggleMenu(); // Escape cancels the confirmation before closing settings.
            Assert.That(controller.panelWarning.activeSelf,Is.False);
            Assert.That(controller.panelSetting.activeSelf,Is.True);
            Click(CloseButton(controller));Assert.That(Time.timeScale,Is.EqualTo(.75f));
            Object.DestroyImmediate(root);root=null;
        }
    }
    [UnityTest] public IEnumerator ConfirmingReturnDispatchesOnceAndRestoresTimeEvenAfterGameOver()
    {
        foreach(string sceneName in new[]{"Main_Scene_Game","Main_Scene_Tutorial"})
        {
            var controller=Create(false,sceneName);var reset=root.AddComponent<KHS_Script_ResetController>();
            yield return null;
            KHS_Script_ScoreManager.EndGame("Ending2");Time.timeScale=0;controller.ToggleMenu();yield return null;
            Click(ExitButton(controller,false));yield return null;
            Assert.That(controller.panelWarning.activeSelf,Is.True);Assert.That(reset.LobbyRequests,Is.Zero);
            Click(ConfirmButton(controller));controller.ConfirmExit();controller.ToggleMenu();
            Assert.That(reset.LobbyRequests,Is.EqualTo(1));Assert.That(Time.timeScale,Is.EqualTo(1));
            Assert.That(controller.panelSetting.activeSelf,Is.False);Assert.That(PSH_Script_SettingController.IsMenuOpen,Is.False);
            Assert.That(Cursor.visible,Is.True);
            Object.DestroyImmediate(root);root=null;
        }
    }
    [UnityTest] public IEnumerator LobbyOnlyOffersQuitAndNeverRoutesToLobbyReset()
    {
        var controller=Create(true);var reset=root.AddComponent<KHS_Script_ResetController>();
        yield return null;controller.ToggleMenu();yield return null;
        Assert.That(controller.goToLobbyButton.activeSelf,Is.False);
        foreach(string locale in new[]{"ko","en","ja"})
        {
            LocaleTestContext.Select(locale);yield return LocalizationSettings.InitializationOperation;yield return null;
            ExpectLabel(ExitButton(controller,true).gameObject,"Button_Quit");
            Click(ExitButton(controller,true));yield return null;
            ExpectLabel(controller.panelWarning,"Warning_Quit");
            ExpectLabel(ConfirmButton(controller).gameObject,"Button_Quit");
            Click(controller.panelWarning.GetComponentsInChildren<Button>().Single(b=>b.name=="Button_no"));
            Assert.That(reset.LobbyRequests,Is.Zero);
        }
        controller.OnClickGoToLobby();Assert.That(controller.panelWarning.activeSelf,Is.False);
        Click(ExitButton(controller,true));yield return null;Click(ConfirmButton(controller));
        Assert.That(reset.LobbyRequests,Is.Zero);Assert.That(controller.panelSetting.activeSelf,Is.False);
    }
    IEnumerator VerifyClose(bool lobby)
    {
        var controller=Create(lobby);yield return null;
        Time.timeScale=.75f;Cursor.visible=false;
        controller.ToggleMenu();yield return null;
        Assert.That(controller.panelSetting.activeInHierarchy,Is.True);Assert.That(Time.timeScale,Is.Zero);
        var close=CloseButton(controller);Click(close);yield return null;
        Assert.That(controller.panelSetting.activeSelf,Is.False);
        Assert.That(PSH_Script_SettingController.IsMenuOpen,Is.False);
        Assert.That(Time.timeScale,Is.EqualTo(.75f));Assert.That(Cursor.visible,Is.False);
        // Repeated close callbacks must never reopen the panel or overwrite the captured state.
        close.onClick.Invoke();Assert.That(controller.panelSetting.activeSelf,Is.False);
        Assert.That(Time.timeScale,Is.EqualTo(.75f));
        Time.timeScale=0;Cursor.visible=true;controller.ToggleMenu();yield return null;
        Click(close);Assert.That(Time.timeScale,Is.Zero);Assert.That(Cursor.visible,Is.True);
    }
}
