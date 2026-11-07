using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SettingsInteractionTests
{
    GameObject root, events;
    EventSystem eventSystem;
    bool oldCursorVisible;
    CursorLockMode oldCursorLock;
    float oldTime;
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
    PSH_Script_SettingController Create(bool lobby)
    {
        string path=lobby ? "Assets/Public/Main_Lobby/UI_Lobby/Prefabs/Canvas_Lobby.prefab"
            : "Assets/Public/Main_Game/UI_Game/Prefabs/Canvas_GameScene.prefab";
        root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
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
                controller.OnClickGoToLobby();yield return null;
                var button=controller.panelWarning.GetComponentsInChildren<Button>().Single(b=>b.name==name);
                Click(button);yield return null;
                Assert.That(controller.panelWarning.activeSelf,Is.False,name);
                Assert.That(controller.panelSetting.activeSelf,Is.True);Assert.That(Time.timeScale,Is.Zero);
            }
            Click(CloseButton(controller));Assert.That(Time.timeScale,Is.EqualTo(.75f));
            Object.DestroyImmediate(root);root=null;
        }
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
