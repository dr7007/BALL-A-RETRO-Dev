using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PSH;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// PlayMode tests use Unity's actual event propagation and production dialogue code.
// Only localized text is substituted, so these tests need no Addressables/server.
public class DialogueInputTests
{
    private class TestDatabase : LocalizedStringDatabase
    {
        public StringTable Table;
        public override StringTable GetTable(TableReference reference, Locale locale = null) => Table;
    }

    private readonly List<Object> objects = new List<Object>();
    private PSH_Script_DialogueUI dialogue;
    private GameObject introPanel, endingPanel;
    private TMP_Text introText, endingText;
    private EventSystem eventSystem;
    private int completed;
    private string finishedId;
    private float oldTimeScale;
    private bool oldCursorVisible;
    private CursorLockMode oldCursorLock;

    [SetUp]
    public void SetUp()
    {
        oldTimeScale = Time.timeScale;
        oldCursorVisible = Cursor.visible;
        oldCursorLock = Cursor.lockState;
        Time.timeScale = 0.75f;
        var settings = Keep(ScriptableObject.CreateInstance<LocalizationSettings>());
        LocalizationSettings.Instance = settings;
        var table = Keep(ScriptableObject.CreateInstance<StringTable>());
        table.SharedData = Keep(ScriptableObject.CreateInstance<SharedTableData>());
        table.SharedData.TableCollectionName = "DialogueTable";
        foreach (var id in new[] { "Intro", "Tutorial", "Ending1", "Ending2" })
        {
            table.AddEntry(id + "_01", "First line.");
            table.AddEntry(id + "_02", "Second line.");
        }
        LocalizationSettings.StringDatabase = new TestDatabase { Table = table };
        eventSystem = Keep(new GameObject("EventSystem", typeof(EventSystem))).GetComponent<EventSystem>();

        var controller = Keep(new GameObject("Canvas_Dialogue", typeof(RectTransform), typeof(Canvas)));
        controller.SetActive(false);
        introPanel = MakePanel("Intro", controller.transform);
        introText = MakeText(introPanel.transform);
        // Match Main_Scene_Game: the ending is under Canvas_GameScene, not Canvas_Dialogue.
        var gameCanvas = Keep(new GameObject("Canvas_GameScene", typeof(RectTransform), typeof(Canvas)));
        endingPanel = MakePanel("Panel_GameEnd", gameCanvas.transform);
        endingText = MakeText(endingPanel.transform);
        dialogue = controller.AddComponent<PSH_Script_DialogueUI>();
        SetField("introPanelRoot", introPanel);
        SetField("introText", introText);
        SetField("endingPanelRoot", endingPanel);
        SetField("endingText", endingText);
        SetField("charInterval", 10f);
        controller.SetActive(true);
        completed = 0;
        finishedId = null;
        PSH_Script_DialogueUI.OnDialogueComplete += OnComplete;
    }

    [TearDown]
    public void TearDown()
    {
        PSH_Script_DialogueUI.OnDialogueComplete -= OnComplete;
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] is GameObject) Object.DestroyImmediate(objects[i]);
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear();
        LocalizationSettings.Instance = null;
        Time.timeScale = oldTimeScale;
        Cursor.lockState = oldCursorLock;
        Cursor.visible = oldCursorVisible;
    }

    [TestCase("Ending2")]
    [TestCase("Ending1")]
    public void EndingReceivesClicksAcrossCanvasHierarchyAndCompletesOnce(string id)
    {
        dialogue.Play(id);
        Assert.That(endingPanel.transform.IsChildOf(dialogue.transform), Is.False);
        Click(endingText.gameObject);
        Assert.That(endingText.text, Is.EqualTo("First line."));
        Click(endingPanel);
        Assert.That(endingText.text, Is.EqualTo("S"), "One click must advance exactly one line.");
        Click(endingText.gameObject);
        Assert.That(endingText.text, Is.EqualTo("Second line."));
        Click(endingPanel);
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(finishedId, Is.EqualTo(id));
        Assert.That(endingPanel.activeSelf, Is.True, "Keep the ending/result panel visible.");
        Click(endingPanel);
        Assert.That(completed, Is.EqualTo(1));
    }

    [TestCase("Intro")]
    [TestCase("Tutorial")]
    public void IntroAndTutorialStillReceiveClicksWhilePaused(string id)
    {
        dialogue.Play(id);
        Assert.That(Time.timeScale, Is.Zero);
        Click(introText.gameObject);
        Assert.That(introText.text, Is.EqualTo("First line."));
        Click(introPanel);
        Assert.That(introText.text, Is.EqualTo("S"));
        Click(introPanel);
        Click(introPanel);
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(introPanel.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(0.75f));
    }

    [Test]
    public void SkipButtonConsumesClickAndCompletesOnlyOnce()
    {
        var button = MakeButton(introPanel.transform);
        button.onClick.AddListener(dialogue.SkipDialogue);
        dialogue.Play("Intro");
        Click(button.gameObject);
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(finishedId, Is.EqualTo("Intro"));
        Assert.That(Time.timeScale, Is.EqualTo(0.75f));
        dialogue.SkipDialogue();
        Assert.That(completed, Is.EqualTo(1));
    }

    [Test]
    public void OtherButtonsDoNotAdvanceDialogue()
    {
        var button = MakeButton(endingPanel.transform);
        int buttonClicks = 0;
        button.onClick.AddListener(() => buttonClicks++);
        dialogue.Play("Ending2");
        Click(button.gameObject);
        Assert.That(buttonClicks, Is.EqualTo(1));
        Assert.That(endingText.text, Is.EqualTo("F"));
    }

    [TestCase(PointerEventData.InputButton.Right)]
    [TestCase(PointerEventData.InputButton.Middle)]
    public void OnlyLeftClickAdvances(PointerEventData.InputButton button)
    {
        dialogue.Play("Intro");
        Click(introPanel, button);
        Assert.That(introText.text, Is.EqualTo("F"));
    }

    [Test]
    public void PreviousEndingPanelCannotAdvanceAnotherDialogue()
    {
        dialogue.Play("Ending2");
        dialogue.SkipDialogue();
        dialogue.Play("Intro");
        Click(endingPanel);
        Assert.That(introText.text, Is.EqualTo("F"));
        Assert.That(completed, Is.EqualTo(1));
    }

    [TestCase("Ending1")]
    [TestCase("Ending2")]
    public void ClosingSettingsKeepsEndingClickable(string id)
    {
        var menu = MakeSettingsMenu();
        KHS_Script_ScoreManager.EndGame(id);
        dialogue.Play(id);
        for (int i = 0; i < 3; ++i)
        {
            menu.ToggleMenu();
            Assert.That(menu.panelSetting.activeSelf, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            menu.ToggleMenu();
            Assert.That(menu.panelSetting.activeSelf, Is.False);
            AssertClickableCursor();
            Assert.That(endingText.text, Is.EqualTo("F"), "Menu toggles must not advance the ending.");
        }
        Click(endingPanel);
        Assert.That(endingText.text, Is.EqualTo("First line."));
        Click(endingPanel);
        Click(endingPanel);
        Click(endingPanel);
        Assert.That(completed, Is.EqualTo(1));
        Assert.That(finishedId, Is.EqualTo(id));
    }

    [TestCase("Ending1")]
    [TestCase("Ending2")]
    public void ClosingSettingsKeepsFinishedResultClickable(string id)
    {
        var menu = MakeSettingsMenu();
        KHS_Script_ScoreManager.EndGame(id);
        dialogue.Play(id);
        dialogue.SkipDialogue();
        var resultButton = MakeButton(endingPanel.transform);
        int resultClicks = 0;
        resultButton.onClick.AddListener(() => resultClicks++);
        menu.ToggleMenu();
        menu.ToggleMenu();
        AssertClickableCursor();
        Click(resultButton.gameObject);
        Assert.That(resultClicks, Is.EqualTo(1));
        Assert.That(completed, Is.EqualTo(1));
    }

    [TestCase("Intro")]
    [TestCase("Tutorial")]
    public void ClosingSettingsPreservesPausedDialogue(string id)
    {
        var menu = MakeSettingsMenu();
        dialogue.Play(id);
        menu.ToggleMenu();
        menu.ToggleMenu();
        Assert.That(Time.timeScale, Is.Zero, "Closing settings must not resume a paused dialogue.");
        AssertClickableCursor();
        Click(introPanel);
        Assert.That(introText.text, Is.EqualTo("First line."));
        dialogue.SkipDialogue();
        Assert.That(Time.timeScale, Is.EqualTo(0.75f));
    }

    [TestCase(1f)]
    [TestCase(0.35f)]
    public void ClosingSettingsRestoresGameplayCursorAndSpeed(float speed)
    {
        var menu = MakeSettingsMenu();
        Time.timeScale = speed;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // Batchmode has no focused Game view and may report None even after
        // requesting Locked. Verify restoration of the actual pre-menu state.
        CursorLockMode gameplayLock = Cursor.lockState;
        menu.ToggleMenu();
        AssertClickableCursor();
        menu.ToggleMenu();
        Assert.That(Cursor.visible, Is.False);
        Assert.That(Cursor.lockState, Is.EqualTo(gameplayLock));
        Assert.That(Time.timeScale, Is.EqualTo(speed));
    }

    [Test]
    public void ClosingSettingsPreservesVisibleCursorWithoutGameEndEvent()
    {
        var menu = MakeSettingsMenu();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        menu.ToggleMenu();
        menu.ToggleMenu();
        AssertClickableCursor(); // Lobby, explanation popup or a choice screen.
    }

    [TestCase("Ending1")]
    [TestCase("Ending2")]
    public void GameEndsWhileSettingsAreOpenKeepsCursorAvailable(string id)
    {
        var menu = MakeSettingsMenu();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        menu.ToggleMenu();
        // Ending sequences use real-time waits and can finish while settings are open.
        KHS_Script_ScoreManager.EndGame(id);
        dialogue.Play(id);
        menu.ToggleMenu();
        AssertClickableCursor();
        Click(endingPanel);
        Assert.That(endingText.text, Is.EqualTo("First line."));
    }

    private PSH_Script_SettingController MakeSettingsMenu()
    {
        var go = Keep(new GameObject("Settings controller"));
        var menu = go.AddComponent<PSH_Script_SettingController>();
        menu.panelSetting = new GameObject("System window");
        menu.panelSetting.transform.SetParent(go.transform, false);
        menu.panelSetting.SetActive(false);
        return menu;
    }

    private static void AssertClickableCursor()
    {
        Assert.That(Cursor.visible, Is.True, "The UI still needs a visible pointer.");
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None), "The pointer must be free to reach UI buttons.");
    }

    private void Click(GameObject target, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
    {
        var data = new PointerEventData(eventSystem) { button = button };
        ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerClickHandler);
    }

    private GameObject MakePanel(string name, Transform parent)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        return panel;
    }

    private TMP_Text MakeText(Transform parent)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.enabled = false; // Text state is tested without depending on a font or GPU.
        return text;
    }

    private Button MakeButton(Transform parent)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Button>();
    }

    private T Keep<T>(T value) where T : Object { objects.Add(value); return value; }
    private void SetField(string name, object value) => typeof(PSH_Script_DialogueUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dialogue, value);
    private void OnComplete(string id) { completed++; finishedId = id; }
}
