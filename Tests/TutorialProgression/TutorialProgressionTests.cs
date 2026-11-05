using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class TutorialProgressionTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private CJS_TutorialDirector tutorial;
    private KHS_Script_ScoreManager score;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1;
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
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear(); Time.timeScale = 1; Cursor.lockState = CursorLockMode.None;
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
        Assert.That(tutorial.tutorialText.text, Does.Contain("Slash: 눌러보세요"));
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
        Assert.That(tutorial.resultTitleText.text, Is.EqualTo("다시 해볼까?"));
        Assert.That(tutorial.resultDescText.text, Does.Contain("플리퍼"));
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
