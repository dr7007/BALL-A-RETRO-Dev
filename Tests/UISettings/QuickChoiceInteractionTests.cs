using System.Collections;
using System.Linq;
using System.Reflection;
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

public sealed class QuickChoiceInteractionTests
{
    private GameObject root;
    private PSH_Script_QuickChoiceOnOff drawer;
    private KHS_Script_ScoreManager score;
    private float oldTime;
    private bool oldCursor;
    private CursorLockMode oldLock;
    private Scene originalScene, temporaryScene;
    private static void Set(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    [UnitySetUp] public IEnumerator Setup()
    {
        oldTime = Time.timeScale; oldCursor = Cursor.visible; oldLock = Cursor.lockState;
        originalScene = SceneManager.GetActiveScene();
        Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = false;
        LocaleTestContext.Select("en"); yield return LocalizationSettings.InitializationOperation;
        root = new GameObject("QuickChoice fixture", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(EventSystem));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        score = new GameObject("Score", typeof(KHS_Script_ScoreManager)).GetComponent<KHS_Script_ScoreManager>();
        score.transform.SetParent(root.transform);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Public/Main_Game/UI_Game/Prefabs/Canvas_GameScene.prefab");
        drawer = Object.Instantiate(prefab.GetComponentInChildren<PSH_Script_QuickChoiceOnOff>(true), root.transform);
        drawer.duration = 0;
        yield return null;
    }

    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (originalScene.IsValid() && originalScene.isLoaded) SceneManager.SetActiveScene(originalScene);
        if (root) Object.DestroyImmediate(root);
        if (temporaryScene.IsValid() && temporaryScene.isLoaded) yield return SceneManager.UnloadSceneAsync(temporaryScene);
        BallARetroDemoRun.Current = null;
        Time.timeScale = oldTime; Cursor.lockState = oldLock; Cursor.visible = oldCursor;
        LocaleTestContext.Select("ko");
    }

    [UnityTest] public IEnumerator TabToggleStartsClosedStaysOpenPastThreeSecondsAndNeverOwnsGameplayInput()
    {
        var rect = (RectTransform)drawer.transform;
        float hiddenX = rect.anchoredPosition.x;
        Assert.That(drawer.IsOpen, Is.False);
        Assert.That(drawer.GetComponentInChildren<Toggle>().isOn, Is.False);
        Assert.That(root.transform.Find("QuickChoiceTabHint").GetComponent<TMP_Text>().text,
            Is.EqualTo(PSH_Script_Localization.Text("QuickChoice_TabHint")));
        Assert.That(root.transform.Find("QuickChoiceTabHint").GetComponent<TMP_Text>().font,
            Is.SameAs(drawer.GetComponentInChildren<TMP_Text>(true).font), "Hint must inherit the drawer's authored multilingual font.");
        Assert.That(drawer.TryTogglePanel(), Is.True);
        Assert.That(drawer.IsOpen, Is.True);
        Assert.That(rect.anchoredPosition.x, Is.EqualTo(hiddenX + 460));
        yield return new WaitForSecondsRealtime(3.2f);
        Assert.That(drawer.IsOpen, Is.True, "Viewing upgrades has no auto-close timer.");
        Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(Cursor.visible, Is.False);
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
        Assert.That(drawer.TryTogglePanel(), Is.True);
        Assert.That(drawer.IsOpen, Is.False); Assert.That(rect.anchoredPosition.x, Is.EqualTo(hiddenX));
        Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(Cursor.visible, Is.False);
    }

    [UnityTest] public IEnumerator PauseMonitorChoiceResultsAndTextEntryBlockBothInputPaths()
    {
        Time.timeScale = 0;
        Assert.That(drawer.TryTogglePanel(), Is.False);
        drawer.OnToggleValueChanged(true); Assert.That(drawer.IsOpen, Is.False);
        Assert.That(Time.timeScale, Is.Zero);
        Time.timeScale = 1; score.IsPlaying = false;
        Assert.That(drawer.TryTogglePanel(), Is.False);
        score.IsPlaying = true;
        var input = new GameObject("Text input", typeof(RectTransform), typeof(TMP_InputField));
        input.transform.SetParent(root.transform); EventSystem.current.SetSelectedGameObject(input);
        Assert.That(drawer.TryTogglePanel(), Is.False);
        EventSystem.current.SetSelectedGameObject(null);
        Assert.That(drawer.TryTogglePanel(), Is.True);
        KHS_Script_CameraManager.SetMonitor(true);
        Assert.That(drawer.IsOpen, Is.False); Assert.That(drawer.TryTogglePanel(), Is.False);
        KHS_Script_CameraManager.SetMonitor(false); yield return null;
        drawer.OnToggleValueChanged(true); Assert.That(drawer.IsOpen, Is.True);
        Time.timeScale = 0; yield return null;
        Assert.That(drawer.IsOpen, Is.False); Assert.That(Time.timeScale, Is.Zero);
        Time.timeScale = 1; yield return null;
        Assert.That(drawer.TryTogglePanel(), Is.True);
        score.HasEnded = true; KHS_Script_ScoreManager.EndGame("Ending1");
        Assert.That(drawer.IsOpen, Is.False); Assert.That(drawer.TryTogglePanel(), Is.False);
        Assert.That(Cursor.visible, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
    }

    [Test] public void ModifiedTabLeavesSteamOverlayAndOtherShortcutsAlone()
    {
        Assert.That(drawer.TryHandleTabInput(true, true, false, false), Is.False);
        Assert.That(drawer.TryHandleTabInput(true, false, true, false), Is.False);
        Assert.That(drawer.TryHandleTabInput(true, false, false, true), Is.False);
        Assert.That(drawer.TryHandleTabInput(false, false, false, false), Is.False);
        Assert.That(drawer.IsOpen, Is.False);
        Assert.That(drawer.TryHandleTabInput(true, false, false, false), Is.True);
        Assert.That(drawer.IsOpen, Is.True);
        Assert.That(drawer.TryHandleTabInput(true, true, false, false), Is.False);
        Assert.That(drawer.IsOpen, Is.True, "Shift+Tab must not close an already open drawer either.");
    }

    [UnityTest] public IEnumerator ResetDisableSceneChangeAndTutorialAlwaysCloseTheDrawer()
    {
        Assert.That(drawer.TryTogglePanel(), Is.True);
        KHS_Script_ResetController.ResetRun();
        Assert.That(drawer.IsOpen, Is.False); Assert.That(drawer.TryTogglePanel(), Is.False);
        yield return null; Assert.That(drawer.TryTogglePanel(), Is.True);
        drawer.enabled = false; Assert.That(drawer.IsOpen, Is.False);
        drawer.enabled = true; yield return null;
        Assert.That(drawer.TryTogglePanel(), Is.True);
        temporaryScene = SceneManager.CreateScene("DrawerSceneTransitionFixture");
        SceneManager.SetActiveScene(temporaryScene);
        Assert.That(drawer.IsOpen, Is.False);
        SceneManager.SetActiveScene(originalScene); yield return null;
        score.IsTutorialRun = true;
        Assert.That(drawer.TryTogglePanel(), Is.False);
        yield return null;
        Assert.That(root.transform.Find("QuickChoiceTabHint").gameObject.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(Cursor.visible, Is.False);
    }

    [UnityTest] public IEnumerator InventoryRebuildsOnlyWhenChangedAndDoesNotDuplicateSubscriptions()
    {
        var state = new GameObject("Choice state", typeof(CJS_Script_ChoiceState)).GetComponent<CJS_Script_ChoiceState>();
        state.transform.SetParent(root.transform);
        var scroll = drawer.GetComponentInChildren<ScrollRect>();
        var inventory = scroll.gameObject.AddComponent<CJS_Script_ChoiceInventoryUI>();
        var template = new GameObject("Slot template", typeof(RectTransform), typeof(Image), typeof(CJS_Script_ChoiceSlot));
        template.transform.SetParent(root.transform);
        Set(inventory, "grid", scroll.content); Set(inventory, "slotPrefab", template.GetComponent<CJS_Script_ChoiceSlot>());
        state.Add(new CJS_ChoiceData { funcIdx = 9, name = "Capacitor" });
        Assert.That(drawer.TryTogglePanel(), Is.True); yield return null;
        Assert.That(scroll.content.childCount, Is.EqualTo(1));
        var first = scroll.content.GetChild(0).gameObject;
        drawer.TryTogglePanel(); drawer.TryTogglePanel(); yield return null;
        Assert.That(scroll.content.GetChild(0).gameObject, Is.SameAs(first), "Unchanged inventory must not recreate cards.");
        inventory.enabled = false; inventory.enabled = true;
        state.Add(new CJS_ChoiceData { funcIdx = 10, name = "Last ball" }); yield return null;
        Assert.That(scroll.content.childCount, Is.EqualTo(2), "An enable cycle must not retain duplicate event listeners.");
        drawer.TryTogglePanel();
        state.Add(new CJS_ChoiceData { funcIdx = 8, name = "Extra ball" });
        Assert.That(scroll.content.childCount, Is.EqualTo(2));
        drawer.TryTogglePanel(); yield return null;
        Assert.That(scroll.content.childCount, Is.EqualTo(3));
        state.ResetForNewRun(); yield return null;
        Assert.That(scroll.content.childCount, Is.Zero);
    }
}
