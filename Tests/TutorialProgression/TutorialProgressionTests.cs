using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PSH;
using TMPro;
using UnityEngine.Localization.Settings;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class TutorialProgressionTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private LocalizationSettings localeSettings;
    private CJS_TutorialDirector tutorial;
    private KHS_Script_ScoreManager score;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1;
        localeSettings = LocaleTestContext.Create();
        yield return LocalizationSettings.InitializationOperation;
        var scoreGO = New("Score"); scoreGO.SetActive(false);
        score = scoreGO.AddComponent<KHS_Script_ScoreManager>();
        var canvas = New("TutorialCanvas", typeof(RectTransform), typeof(Canvas));
        var go = New("Tutorial"); go.SetActive(false);
        tutorial = go.AddComponent<CJS_TutorialDirector>();
        tutorial.scoreManager = score;
        tutorial.plunger = New("Plunger").AddComponent<KHS_Script_PlungerController>();
        tutorial.resultPanel = New("Result", typeof(RectTransform), typeof(Image));
        tutorial.resultPanel.transform.SetParent(canvas.transform, false);
        tutorial.tutorialText = Text("Hint", canvas.transform);
        tutorial.resultTitleText = Text("Title", tutorial.resultPanel.transform);
        tutorial.resultDescText = Text("Description", tutorial.resultPanel.transform);
        go.SetActive(true);
        yield return null;
        yield return null;
        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear(); Object.DestroyImmediate(localeSettings); LocalizationSettings.Instance = null; Time.timeScale = 1; Cursor.lockState = CursorLockMode.None;
    }

    [Test]
    public void ReachingScoreWithoutPostLaunchPracticeKeepsTheTutorialRunning()
    {
        Launch(); score.curScore = 1050; Tick();
        Assert.That(tutorial.resultPanel.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1));
        Assert.That(tutorial.tutorialText.text, Does.Contain("목표 점수 달성"));
        Assert.That(tutorial.tutorialText.text, Does.Contain("Z: 눌러보세요"));
    }

    [UnityTest]
    public IEnumerator BothFlippersAndScoreOpenTheClickableCompletionPanel()
    {
        Launch(); score.curScore = 1050;
        Press(KeyCode.Z);
        Assert.That(tutorial.resultPanel.activeSelf, Is.False);
        Press(KeyCode.Slash);
        Assert.That(Time.timeScale, Is.EqualTo(1), "The final flipper press must be allowed to animate.");
        yield return new WaitForSecondsRealtime(0.3f);
        Tick();
        Assert.That(tutorial.resultPanel.activeSelf, Is.True);
        Assert.That(tutorial.resultTitleText.text, Is.EqualTo("튜토리얼 완료!"));
        Assert.That(tutorial.resultPanel.GetComponent<GraphicRaycaster>().enabled, Is.True);
        Assert.That(tutorial.resultPanel.GetComponent<Canvas>().sortingOrder, Is.EqualTo(50));
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(Cursor.visible, Is.True);
    }

    [Test]
    public void DemonstrationBeforeLaunchDoesNotCompleteThePracticeGoal()
    {
        Press(KeyCode.Z); Press(KeyCode.Slash);
        Launch(); score.curScore = 1050; Tick();
        Assert.That(tutorial.resultPanel.activeSelf, Is.False);
    }

    [Test]
    public void RepeatedLeftFlipperDoesNotCountAsRightFlipperPractice()
    {
        Launch(); score.curScore = 1050;
        Press(KeyCode.Z); Press(KeyCode.Z); Tick();
        Assert.That(tutorial.resultPanel.activeSelf, Is.False);
        Assert.That(tutorial.tutorialText.text, Does.Contain("/: 눌러보세요"));
    }

    [Test]
    public void PausedInputsDoNotCountAsGameplayPractice()
    {
        Launch(); Time.timeScale = 0;
        Press(KeyCode.Z); Press(KeyCode.Slash);
        Time.timeScale = 1; score.curScore = 1050; Tick();
        Assert.That(tutorial.resultPanel.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator PracticingBeforeTheScoreGoalWaitsForTheScore()
    {
        Launch(); Press(KeyCode.Z); Press(KeyCode.Slash);
        Assert.That(tutorial.resultPanel.activeSelf, Is.False);
        yield return new WaitForSecondsRealtime(0.3f);
        score.curScore = 1000; Tick();
        Assert.That(tutorial.resultPanel.activeSelf, Is.True);
    }

    [TestCase("OnGameOverWithScore")]
    [TestCase("OnGameClearWithScore")]
    public void EndEventsDoNotBypassUnfinishedPractice(string method)
    {
        Launch(); score.curScore = 1100; Call(method, 1100);
        Assert.That(tutorial.resultPanel.activeSelf, Is.True);
        Assert.That(tutorial.resultTitleText.text, Is.EqualTo("다시 해볼까요?"));
        Assert.That(tutorial.resultDescText.text, Does.Contain("플리퍼"));
    }

    [UnityTest]
    public IEnumerator LanguageChangePreservesPracticeAndTranslatesTheOpenResult()
    {
        Launch(); Press(KeyCode.Z);
        LocaleTestContext.Select("en"); yield return LocalizationSettings.InitializationOperation; yield return null;
        Assert.That(tutorial.tutorialText.text, Does.Contain("Z: Done"));
        Assert.That(tutorial.tutorialText.text, Does.Contain("/: Press it"));
        score.curScore = 1000; Press(KeyCode.Slash);
        yield return new WaitForSecondsRealtime(.3f); Tick();
        Assert.That(tutorial.resultTitleText.text, Is.EqualTo("Tutorial Complete!"));
        LocaleTestContext.Select("ja"); yield return LocalizationSettings.InitializationOperation; yield return null;
        Assert.That(tutorial.resultTitleText.text, Is.EqualTo("チュートリアル完了！"));
        Assert.That(tutorial.resultDescText.text, Does.Contain("1000"));
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(tutorial.resultPanel.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator LanguageChangeDoesNotReplayTheCameraStepOrResetInputProgress()
    {
        Call("GoToStep", CJS_TutorialDirector.Step.Flippers);
        typeof(CJS_TutorialDirector).GetField("_leftDone", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tutorial, true);
        Time.timeScale = 0;
        LocaleTestContext.Select("en"); yield return LocalizationSettings.InitializationOperation; yield return null;
        Assert.That(tutorial.tutorialText.text, Is.EqualTo("Press both Z and /!"));
        Assert.That(typeof(CJS_TutorialDirector).GetField("_leftDone", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tutorial), Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        LocaleTestContext.Select("ja"); yield return LocalizationSettings.InitializationOperation; yield return null;
        Assert.That(tutorial.tutorialText.text, Does.Contain("それぞれ"));
    }

    [Test]
    public void MissionHintUsesASmallNonblockingCornerAndTheTutorialScoreTarget()
    {
        Launch();
        Assert.That(score.targetScore, Is.EqualTo(1000));
        Assert.That(tutorial.tutorialText.rectTransform.anchorMax, Is.EqualTo(new Vector2(0,1)));
        Assert.That(tutorial.tutorialText.fontSizeMax, Is.EqualTo(24));
        Assert.That(tutorial.tutorialText.raycastTarget, Is.False);
        Assert.That(tutorial.tutorialText.color, Is.Not.EqualTo(Color.yellow));
    }

    [UnityTest]
    public IEnumerator TutorialEndingNeverEmitsMainGameEndingOrSubmitsARanking()
    {
        var ranking = New("Ranking").AddComponent<CJS_Script_PinballRankingService>();
        typeof(KHS_Script_ScoreManager).GetField("rankingService", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(score, ranking);
        int endings=0, rounds=0;
        Action ended=()=>endings++; Action round=()=>rounds++;
        KHS_Script_ScoreManager.OnGameOver += ended;
        KHS_Script_ScoreManager.OnGameClear += ended;
        KHS_Script_ScoreManager.Round_Clear += round;
        try
        {
            Launch();
            typeof(KHS_Script_ScoreManager).GetField("currentState", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(score, KHS_Script_ScoreManager.RoundState.Playing);
            score.AddScore(2200);
            Assert.That(rounds,Is.Zero,"Tutorial practice must not trigger round-choice/camera sequences.");
            score.CompleteTutorial(false);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(endings,Is.Zero);
            Assert.That(ranking.Submissions,Is.Zero);
            Assert.That(tutorial.resultPanel.activeSelf,Is.True);
            Assert.That(tutorial.tutorialText.text,Is.Empty);
            Assert.That(Time.timeScale,Is.Zero);
        }
        finally
        {
            KHS_Script_ScoreManager.OnGameOver -= ended;
            KHS_Script_ScoreManager.OnGameClear -= ended;
            KHS_Script_ScoreManager.Round_Clear -= round;
        }
    }

    [UnityTest]
    public IEnumerator AuthoredTutorialDialogueCompletesBeforeControlPractice()
    {
        var go=New("Dialogue");go.SetActive(false);
        var dialogue=go.AddComponent<PSH_Script_DialogueUI>();
        var panel=New("IntroPanel",typeof(RectTransform));
        var text=Text("IntroText",panel.transform);
        var fields=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(PSH_Script_DialogueUI).GetField("introPanelRoot",fields).SetValue(dialogue,panel);
        typeof(PSH_Script_DialogueUI).GetField("introText",fields).SetValue(dialogue,text);
        go.SetActive(true);
        var opening=New("Opening").AddComponent<PSH_Script_GameSceneDirector>();
        // Exercise the production main-game director too; it must defer to tutorial ownership.
        yield return null;
        tutorial.StartCoroutine((IEnumerator)typeof(CJS_TutorialDirector).GetMethod("PlayIntroduction",fields).Invoke(tutorial,null));
        yield return null;yield return null;
        Assert.That(typeof(PSH_Script_DialogueUI).GetField("currentCutsceneId",fields).GetValue(dialogue),Is.EqualTo("Tutorial"));
        Assert.That(tutorial.tutorialText.text,Is.Empty);
        Assert.That(tutorial.resultPanel.activeSelf,Is.False);
        var lines=(List<string>)typeof(PSH_Script_DialogueUI).GetField("currentLines",fields).GetValue(dialogue);
        Assert.That(lines.Count,Is.EqualTo(7));
        Assert.That(lines[0],Does.Contain("처음인 모양"));
        for(int i=0;i<14;i++)typeof(PSH_Script_DialogueUI).GetMethod("AdvanceDialogue",fields).Invoke(dialogue,null);
        Assert.That(panel.activeSelf,Is.False);
        Assert.That(tutorial.tutorialText.text,Does.Contain("각각 눌러보세요"));
        Assert.That(Time.timeScale,Is.EqualTo(1));
        Assert.That(tutorial.resultPanel.activeSelf,Is.False);
    }

    private void Launch() { Call("GoToStep", CJS_TutorialDirector.Step.Plunger); KHS_Script_PlungerController.Launch(); }
    private void Tick() => Call("CheckScoreMission");
    private void Press(KeyCode key) => ((Action<KeyCode>)typeof(KHS_Script_FliperController)
        .GetField("OnFlipperPressed", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))?.Invoke(key);
    private void Call(string method, params object[] args) => typeof(CJS_TutorialDirector)
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tutorial, args);
    private GameObject New(string name, params Type[] components) { var go = new GameObject(name, components); objects.Add(go); return go; }
    private TMP_Text Text(string name, Transform parent) { var go = New(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false); return go.GetComponent<TMP_Text>(); }
}
