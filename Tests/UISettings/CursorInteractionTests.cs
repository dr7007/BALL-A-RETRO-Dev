using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Uses actual GraphicRaycaster hit testing, including runtime-created dropdown items.
public class CursorInteractionTests
{
    GameObject root, events, cursorObject, effects;
    PSH_Script_CursorManager cursor;
    float previousTime;
    bool previousVisible;
    CursorLockMode previousLock;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp] public void Setup()
    {
        previousTime = Time.timeScale;
        previousVisible = Cursor.visible;
        previousLock = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0;
        events = new GameObject("Cursor events", typeof(EventSystem));
        cursorObject = new GameObject("Cursor", typeof(PSH_Script_CursorManager));
        cursor = cursorObject.GetComponent<PSH_Script_CursorManager>();
    }

    [TearDown] public void Cleanup()
    {
        if (effects) Object.DestroyImmediate(effects);
        if (root) Object.DestroyImmediate(root);
        Object.DestroyImmediate(cursorObject);
        Object.DestroyImmediate(events);
        Time.timeScale = previousTime;
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisible;
    }

    Object Owner => (Object)typeof(PSH_Script_CursorManager).GetField("handOwner", Private).GetValue(cursor);
    bool Hand => (bool)typeof(PSH_Script_CursorManager).GetField("showingHand", Private).GetValue(cursor);
    void Point(Vector2 position)
    {
        Canvas.ForceUpdateCanvases();
        typeof(PSH_Script_CursorManager).GetMethod("UpdateCursorAt", Private).Invoke(cursor, new object[] { position });
    }
    static Vector2 Center(Component component)
    {
        var rect = (RectTransform)component.transform;
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }
    void ExpectHand(Selectable control)
    {
        Point(Center(control));
        Assert.That(Hand, Is.True, control.name);
        Assert.That(Owner, Is.EqualTo(control), control.name);
    }
    void EmptyCanvas()
    {
        root = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
    }
    GameObject Graphic(string name, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent != null ? parent : root.transform, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(120, 80);
        return go;
    }
    PSH_Script_SettingController Lobby()
    {
        root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Public/Main_Lobby/UI_Lobby/Prefabs/Canvas_Lobby.prefab"));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        return root.GetComponent<PSH_Script_SettingController>();
    }

    [UnityTest] public IEnumerator SettingsButtonsSlidersAndDropdownsShowHandWhilePaused()
    {
        var settings = Lobby();
        yield return null;
        settings.ToggleMenu();
        yield return null;
        var controls = settings.panelSetting.GetComponentsInChildren<Selectable>()
            .Where(s => s.IsActive() && s.IsInteractable()).ToArray();
        Assert.That(controls.OfType<Button>().Any(), Is.True);
        Assert.That(controls.OfType<Slider>().Any(), Is.True);
        Assert.That(controls.OfType<TMP_Dropdown>().Any(), Is.True);
        foreach (var control in controls) ExpectHand(control);
        Assert.That(Time.timeScale, Is.Zero);
        var close = controls.OfType<Button>().Single(b => b.name == "Button_Close");
        var position = Center(close);
        close.onClick.Invoke();
        yield return null;
        Point(position);
        Assert.That(Owner, Is.Not.EqualTo(close), "Closing a menu must release its cursor without a mouse move.");
    }

    [UnityTest] public IEnumerator DropdownItemsAndDynamicEffectSwitchesShowHand()
    {
        var settings = Lobby();
        yield return null;
        settings.ToggleMenu();
        yield return null;
        var dropdown = settings.panelSetting.GetComponentsInChildren<TMP_Dropdown>()
            .First(d => d.options.Count == 3);
        dropdown.Show();
        yield return null;
        var items = dropdown.GetComponentsInChildren<Toggle>();
        Assert.That(items.Length, Is.EqualTo(3));
        foreach (var item in items) ExpectHand(item);
        dropdown.Hide();

        var font = settings.panelSetting.GetComponentInChildren<TMP_Text>().font;
        effects = PSH_Script_EffectOptionsPanel.Create(font, 200).gameObject;
        effects.SetActive(true);
        yield return null;
        var controls = effects.GetComponentsInChildren<Selectable>();
        Assert.That(controls.OfType<Toggle>().Count(), Is.EqualTo(6));
        foreach (var control in controls) ExpectHand(control);
        Point(Center(effects.transform.Find("Panel/Title")));
        Assert.That(Hand, Is.False, "Noninteractive text and panels must keep the normal cursor.");
    }

    [UnityTest] public IEnumerator DisabledControlsAndBlockingPanelsUpdateWithoutPointerMovement()
    {
        EmptyCanvas();
        var button = Graphic("Button without hover script").AddComponent<Button>();
        var label = Graphic("Raycastable child", button.transform);
        yield return null;
        var position = Center(label.transform);
        Point(position);
        Assert.That(Owner, Is.EqualTo(button));
        button.interactable = false;
        Point(position); Assert.That(Hand, Is.False);
        button.interactable = true;
        var group = button.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        Point(position); Assert.That(Hand, Is.False);
        group.interactable = true;
        Point(position); Assert.That(Owner, Is.EqualTo(button));
        var blocker = Graphic("Noninteractive modal blocker");
        yield return null;
        Point(position); Assert.That(Hand, Is.False);
        blocker.SetActive(false);
        Point(position); Assert.That(Owner, Is.EqualTo(button));
        button.enabled = false;
        Point(position); Assert.That(Hand, Is.False);
        button.gameObject.SetActive(false);
        Point(position); Assert.That(Hand, Is.False);
    }

    [UnityTest] public IEnumerator CustomClickableTilesRespectCanvasGroups()
    {
        EmptyCanvas();
        var tile = Graphic("Lobby tile").AddComponent<PSH_Script_InputFieldHover>();
        yield return null;
        var position = Center(tile);
        Point(position); Assert.That(Owner, Is.EqualTo(tile));
        var group = tile.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        Point(position); Assert.That(Hand, Is.False);
        group.interactable = true;
        tile.enabled = false;
        Point(position); Assert.That(Hand, Is.False);
        var trigger = tile.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Add(new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter });
        Point(position); Assert.That(Hand, Is.False);
        trigger.triggers.Add(new EventTrigger.Entry { eventID = EventTriggerType.PointerClick });
        Point(position); Assert.That(Owner, Is.EqualTo(trigger));
    }

    [UnityTest] public IEnumerator NameInputFieldShowsHand()
    {
        EmptyCanvas();
        var panel = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Public/Main_Lobby/UI_Lobby/Prefabs/Panel_InputEmail.prefab"), root.transform);
        panel.SetActive(true);
        // Fit the authored 1920x1080 layout to the isolated test viewport.
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        yield return null;
        var inputs = panel.GetComponentsInChildren<TMP_InputField>();
        Assert.That(inputs, Is.Not.Empty);
        foreach (var input in inputs) ExpectHand(input);
    }

    [UnityTest] public IEnumerator HiddenCursorAndDestroyedControlsReleaseHand()
    {
        EmptyCanvas();
        var button = Graphic("Button").AddComponent<Button>();
        yield return null;
        ExpectHand(button);
        Cursor.visible = false;
        cursor.SendMessage("LateUpdate");
        Assert.That(Hand, Is.False);
        Cursor.visible = true;
        ExpectHand(button);
        var position = Center(button);
        Object.DestroyImmediate(button.gameObject);
        Point(position);
        Assert.That(Hand, Is.False);
    }
}
