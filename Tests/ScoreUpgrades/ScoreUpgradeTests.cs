using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class ScoreUpgradeTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private KHS_Script_ScoreManager score;
    private KHS_Script_RogueLikeManager upgrades;
    private YJ_Script_BumperComboManager combo;
    private YJ_Script_BallController ball;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1;
        ball = New("Ball").AddComponent<YJ_Script_BallController>();
        New("Flipper").AddComponent<KHS_Script_FliperController>();
        var camera = New("Camera holder").AddComponent<KHS_Script_CameraManager>();
        camera.cameraGos = new[] { New("Camera") };
        var scoreObject = New("Score");
        scoreObject.SetActive(false);
        score = scoreObject.AddComponent<KHS_Script_ScoreManager>();
        score.targetScores = new[] { 100000, 200000, 300000 };
        Set(score, "goalRound", 3);
        Set(score, "currentgameScores", new int[3]);
        Set(score, "camHolder", camera);
        scoreObject.SetActive(true);

        combo = New("Actual bumper combo").AddComponent<YJ_Script_BumperComboManager>();
        combo.bumperA = New("Bumper A").AddComponent<KHS_Script_BumperEffect>();
        combo.bumperB = New("Bumper B").AddComponent<KHS_Script_BumperEffect>();
        combo.bumperC = New("Bumper C").AddComponent<KHS_Script_BumperEffect>();
        var upgradeObject = New("Actual upgrade dispatcher");
        upgradeObject.SetActive(false);
        upgrades = upgradeObject.AddComponent<KHS_Script_RogueLikeManager>();
        Set(upgrades, "blockerHolderGo", New("Bonus chips"));
        Set(upgrades, "plincoColliderGo", New("Bonus exits"));
        upgradeObject.SetActive(true);
        yield return null;
        yield return null;
        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Time.timeScale = 1;
    }

    [Test]
    public void UnpickedNormalScoreAndAllComboLevelsRemainUnchanged()
    {
        AssertAward(100);
        CompleteCombo(); AssertAward(200);
        CompleteCombo(); AssertAward(400);
        CompleteCombo(); AssertAward(600);
        KHS_Script_BallOutController.Drain(); AssertAward(100);
    }

    [Test]
    public void PickBeforeComboSurvivesComboChangesAndActualBallOutCallback()
    {
        upgrades.MatchingFunc(4); AssertAward(120);
        CompleteCombo(); AssertAward(240);
        CompleteCombo(); AssertAward(480);
        KHS_Script_BallOutController.Drain(); AssertAward(120);
        CompleteCombo(); AssertAward(240);
    }

    [Test]
    public void PickingDuringComboAddsTwentyPercentToTheRunRatherThanComboState()
    {
        CompleteCombo(); AssertAward(200);
        upgrades.MatchingFunc(4); AssertAward(240);
        Assert.That(score.multiplier, Is.EqualTo(2f));
        CompleteCombo(); AssertAward(480);
    }

    [Test]
    public void RepeatedPicksAreAdditiveAndPreserveAuthoredBaseMultiplier()
    {
        // This is the same authored base the actual combo records in Start.
        score.multiplier = 1.5f;
        Invoke(combo, "Start");
        upgrades.MatchingFunc(4);
        upgrades.MatchingFunc(4);
        AssertAward(210);
        CompleteCombo(); AssertAward(420);
        CompleteCombo(); AssertAward(840);
        CompleteCombo(); AssertAward(1260);
        KHS_Script_BallOutController.Drain(); AssertAward(210);
    }

    [UnityTest]
    public IEnumerator RealRoundTransitionRetainsTheRunUpgrade()
    {
        upgrades.MatchingFunc(4);
        score.targetScore = 100;
        score.AddScore(100);
        yield return null;
        yield return null;
        Assert.That(score.RoundRespone(), Is.EqualTo(2));
        Assert.That(score.IsPlaying, Is.True);
        AssertAward(120);
    }

    [Test]
    public void RunResetClearsTheChoiceWithoutTreatingBallOutAsRunReset()
    {
        upgrades.MatchingFunc(4);
        KHS_Script_BallOutController.Drain(); AssertAward(120);
        KHS_Script_ResetController.ResetRun();
        Assert.That(score.curScore, Is.Zero);
        AssertAward(100);
    }

    [Test]
    public void TutorialHasNoRunUpgradeAndRetainsNormalComboScoring()
    {
        score.ConfigureTutorial(100000);
        upgrades.MatchingFunc(4); AssertAward(100);
        CompleteCombo(); AssertAward(200);
        KHS_Script_BallOutController.Drain(); AssertAward(100);
    }

    [Test]
    public void MultiplyingAlreadyAwardedScoreDoesNotApplyUpgradeAgain()
    {
        upgrades.MatchingFunc(4);
        score.AddScore(100);
        score.MultiplyScore(3);
        Assert.That(score.curScore, Is.EqualTo(360));
    }

    [Test]
    public void AwardEventsUseTheSameBoostedValueAsTheScore()
    {
        int emitted = -1;
        System.Action<int> handler = value => emitted = value;
        KHS_Script_ScoreManager.OnScoreGained += handler;
        try
        {
            upgrades.MatchingFunc(4);
            CompleteCombo();
            AssertAward(240);
            Assert.That(emitted, Is.EqualTo(240));
        }
        finally { KHS_Script_ScoreManager.OnScoreGained -= handler; }
    }

#if !BALLARETRO_DEMO
    [Test]
    public void OrdinaryBlackholeKeepsItsExistingBaseAward()
    {
        upgrades.MatchingFunc(4);
        score.AddBlackholeScoreAt(3000, Vector3.zero);
        Assert.That(score.curScore, Is.EqualTo(3600));
    }

#else
    [Test]
    public void DemoContributionIsCombinedOnceAndChargeRemainsFlat()
    {
        var demo = BallARetroDemoRun.Current;
        Assert.That(demo.IsEnabled, Is.True);
        upgrades.MatchingFunc(4);
        upgrades.MatchingFunc(4);
        upgrades.MatchingFunc(9);
        upgrades.MatchingFunc(10);
        ball.SetBallCount(1);
        for (int i = 0; i < 6; i++) demo.RecordBumperHit();
        CompleteCombo();
        score.AddBlackholeScoreAt(3000, Vector3.zero);
        Assert.That(score.curScore, Is.EqualTo(2700)); // 500 * 2 * 1.4 + 700 last-ball + 600 charge.
        Assert.That(demo.LastBallBonusTotal, Is.EqualTo(700));
        Assert.That(demo.ChargePaidTotal, Is.EqualTo(600));
        Assert.That(demo.Charge, Is.Zero);
    }
#endif

    private void CompleteCombo()
    {
        combo.bumperA.lightenable = combo.bumperB.lightenable = combo.bumperC.lightenable = true;
        combo.CheckCombo();
        Assert.That(combo.bumperA.lightenable || combo.bumperB.lightenable || combo.bumperC.lightenable, Is.False);
    }
    private void AssertAward(int expected)
    {
        int before = score.curScore;
        score.AddScore(100);
        Assert.That(score.curScore - before, Is.EqualTo(expected));
    }
    private GameObject New(string name) { var go = new GameObject(name); objects.Add(go); return go; }
    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void Invoke(object target, string method)
        => target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
}
