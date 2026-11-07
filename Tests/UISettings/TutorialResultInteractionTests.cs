using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class TutorialResultInteractionTests
{
    Scene scene;
    GameObject hud, events;
    GameObject result;
    EventSystem eventSystem;
    PSH_Script_SettingController settings;

    [UnitySetUp] public IEnumerator Setup()
    {
        LocaleTestContext.Select("ko"); yield return LocalizationSettings.InitializationOperation;
        scene = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            "Assets/TutorialUI.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        hud = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Public/Main_Game/UI_Game/Prefabs/Canvas_GameScene.prefab"));
        hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        // The tutorial director owns its result; the main game's ending panel stays closed.
        foreach (var node in hud.GetComponentsInChildren<Transform>(true))
            if (node.name == "Panel_GameEnd" || node.name == "Panel_GameOver_Bg" || node.name == "Panel_GameClear_Bg")
                node.gameObject.SetActive(false);
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0;
        settings = hud.GetComponent<PSH_Script_SettingController>();
        settings.panelSetting = hud.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Panel_Setting").gameObject;
        settings.panelWarning = settings.panelSetting.GetComponentsInChildren<Transform>(true)
            .Single(t => string.Equals(t.name, "Image_Warning", System.StringComparison.OrdinalIgnoreCase)).gameObject;
        events = new GameObject("Tutorial result events", typeof(EventSystem)); eventSystem = events.GetComponent<EventSystem>();
        result = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true))
            .Single(t => t.name == "TutorialResultPanel").gameObject;
        result.SetActive(false); result.AddComponent<PSH_Script_ModalCanvas>().SetSortingOrder(50); result.SetActive(true);
        Time.timeScale = 0; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        yield return null; yield return null;
    }

    [UnityTearDown] public IEnumerator Cleanup()
    {
        Object.DestroyImmediate(hud); Object.DestroyImmediate(events);
        if (scene.IsValid() && scene.isLoaded)
            foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
        foreach (var cursor in Object.FindObjectsByType<PSH_Script_CursorManager>(FindObjectsSortMode.None)) Object.DestroyImmediate(cursor.gameObject);
        Time.timeScale = 1;
        yield return null;
    }

    void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var pointer = new PointerEventData(eventSystem) {
            position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>(); eventSystem.RaycastAll(pointer, hits);
        Assert.That(hits, Is.Not.Empty, button.name);
        Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(button.gameObject),
            button.name + " intercepted by " + string.Join(", ", hits.Take(5).Select(h => h.gameObject.name + "/" + h.module.name)));
        bool clicked = false;
        UnityEngine.Events.UnityAction listener = () => clicked = true;
        button.onClick.AddListener(listener);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        if (button.GetComponent<PSH_Script_BaseButtonHover>() != null)
            Assert.That(button.GetComponentInChildren<TMP_Text>().color, Is.EqualTo(Color.cyan), "The button must visibly respond to hover.");
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        button.onClick.RemoveListener(listener);
        Assert.That(clicked, Is.True);
    }

    [UnityTest] public IEnumerator ResultButtonsReceiveClicksAboveTheActualGameplayHud()
    {
        foreach (var code in new[] { "ko", "en", "ja" })
        {
            LocaleTestContext.Select(code); yield return LocalizationSettings.InitializationOperation; yield return null;
            foreach (bool success in new[] { false, true })
            {
                result.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "ResultTitleText").text = PSH_Script_Localization.Text(success ? "Tut_SuccessTitle" : "Tut_FailTitle");
                result.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "ResultDescText").text = success
                    ? PSH_Script_Localization.Text("Tut_SuccessBody", 1000) : PSH_Script_Localization.Text("Tut_FailPractice", "Z", "/");
                Canvas.ForceUpdateCanvases();
                foreach (var text in result.GetComponentsInChildren<TMP_Text>())
                { text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, code + "/" + text.name); }
            }
            foreach (var button in result.GetComponentsInChildren<Button>()) Click(button);
            Assert.That(Time.timeScale, Is.Zero);
        }
    }

    [UnityTest] public IEnumerator SettingsStayAboveTheResult_AndClosingThemRestoresResultClicks()
    {
        settings.ToggleMenu(); yield return null;
        var close = settings.panelSetting.GetComponentsInChildren<Button>()
            .Single(b => b.name == "Button_Close" && !b.transform.IsChildOf(settings.panelWarning.transform));
        Click(close); yield return null;
        Assert.That(settings.panelSetting.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.Zero); Assert.That(Cursor.visible, Is.True);
        foreach (var button in result.GetComponentsInChildren<Button>()) Click(button);
    }
}
