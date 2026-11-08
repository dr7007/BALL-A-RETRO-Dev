using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PSH;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class DemoGameplayTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private KHS_Script_ScoreManager score;
    private YJ_Script_BallController ball;
    private Rigidbody body;
    private BallARetroDemoRun Demo => BallARetroDemoRun.Current;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1;
        var ballObject = New("Ball", typeof(Rigidbody), typeof(SphereCollider));
        ballObject.tag = "Ball";
        body = ballObject.GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezePositionY;
        ball = ballObject.AddComponent<YJ_Script_BallController>();
        ball.SetBallCount(1);
        New("Ball HUD", typeof(RectTransform), typeof(TextMeshProUGUI)).AddComponent<KHS_Script_UIToBall>();
        var camera = New("CameraHolder").AddComponent<KHS_Script_CameraManager>();
        camera.cameraGos = new[] { New("Camera") };
        var scoreObject = New("Score");
        scoreObject.SetActive(false);
        score = scoreObject.AddComponent<KHS_Script_ScoreManager>();
        score.targetScores = new[] { 2000, 4000, 6000 };
        Set(score, "goalRound", 3);
        Set(score, "currentgameScores", new int[3]);
        Set(score, "camHolder", camera);
        scoreObject.SetActive(true);
        yield return null;
        yield return null;
        yield return null;
        Assert.That(Demo.IsEnabled, Is.True);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Time.timeScale = 1;
    }

    [Test]
    public void DemoStartsWithTwoCreditsAndRestoresTwoAtRoundBoundary()
    {
        Assert.That(ball.GetBallCount(), Is.EqualTo(2));
        Assert.That(Object.FindFirstObjectByType<KHS_Script_UIToBall>().GetComponent<TMP_Text>().text, Is.EqualTo("2"));
        score.HandleBallOut(ball);
        Assert.That(ball.GetBallCount(), Is.EqualTo(1));
        Assert.That(ball.BallCountInitResponse(), Is.EqualTo(2));
        Assert.That(score.HasEnded, Is.False);
    }

    [Test]
    public void ExactlyThreePicksAreIndependentOfCreditsAndCannotBeRepeated()
    {
        ball.AddBallFunc(20);
        Assert.That(Demo.TryBeginPick(1), Is.True);
        Assert.That(Demo.TryBeginPick(1), Is.False);
        Assert.That(Demo.TryBeginPick(3), Is.False);
        Set(score, "currentRound", 2);
        Assert.That(Demo.TryBeginPick(2), Is.True);
        Set(score, "currentRound", 3);
        Assert.That(Demo.TryBeginPick(3), Is.True);
        Assert.That(Demo.TryBeginPick(3), Is.False);
        Assert.That(Demo.TryBeginPick(4), Is.False);
        Assert.That(Demo.PicksTaken, Is.EqualTo(3));
    }

    [Test]
    public void StarterOffersBothStrategiesAndSupportThenRemovesMaxedUpgrades()
    {
        var roller = MakeRoller();
        CollectionAssert.AreEquivalent(new[] { 9, 10, 6 }, roller.Roll3(out _).Select(x => x.funcIdx));
        roller.PushPicked(new CJS_ChoiceData { funcIdx = 9 });
        Assert.That(roller.Roll3(out _).First(x => x.funcIdx == 9).localizationKey, Is.EqualTo("Demo_CapacitorUpgrade"));
        roller.PushPicked(new CJS_ChoiceData { funcIdx = 9 });
        CollectionAssert.AreEquivalent(new[] { 10, 6, 8 }, roller.Roll3(out _).Select(x => x.funcIdx));
        Assert.That(roller.Roll3(out _).First(x => x.funcIdx == 8).localizationKey, Is.EqualTo("Demo_ExtraBall"));
        Assert.That(Demo.ApplyUpgrade(9), Is.False);
        Assert.That(Demo.CapacitorLevel, Is.EqualTo(2));
    }

    [Test]
    public void AcceptedBumperTriggerChargesOnceAndOrdinarySwitchDoesNotCharge()
    {
        Demo.ApplyUpgrade(9);
        var bumper = New("Bumper", typeof(KHS_Script_BumperEffect)).AddComponent<KHS_Script_DumpManager>();
        bumper.bumpScore = 50;
        var collider = ball.GetComponent<Collider>();
        Invoke(bumper, "OnTriggerEnter", collider);
        Invoke(bumper, "OnTriggerEnter", collider);
        Assert.That(Demo.Charge, Is.EqualTo(100));
        Assert.That(score.curScore, Is.EqualTo(50));
        var normal = New("Switch").AddComponent<KHS_Script_DumpManager>();
        normal.bumpScore = 50;
        Invoke(normal, "OnTriggerEnter", collider);
        Assert.That(Demo.Charge, Is.EqualTo(100));
        Assert.That(score.curScore, Is.EqualTo(100));
    }

    [Test]
    public void ChargeCapsAndPausedHitsCannotAddCharge()
    {
        Demo.ApplyUpgrade(9);
        Charge(20);
        Assert.That(Demo.Charge, Is.EqualTo(600));
        Demo.ApplyUpgrade(9);
        Time.timeScale = 0;
        Charge(20);
        Assert.That(Demo.Charge, Is.EqualTo(600));
        Time.timeScale = 1;
        Charge(20);
        Assert.That(Demo.Charge, Is.EqualTo(1000));
    }

    [UnityTest]
    public IEnumerator BlackholePaysBaseAndFlatChargeAtomicallyAcrossRoundTarget()
    {
        Demo.ApplyUpgrade(9);
        Charge(6);
        score.curScore = 1800;
        score.AddBlackholeScoreAt(3000, Vector3.zero);
        Assert.That(score.curScore, Is.EqualTo(2900));
        Assert.That(Demo.ChargePaidTotal, Is.EqualTo(600));
        Assert.That(Demo.Charge, Is.Zero);
        yield return null;
        yield return null;
        Assert.That(score.RoundRespone(), Is.EqualTo(2));
        Assert.That(score.targetScore, Is.EqualTo(4000));
    }

    [Test]
    public void LastBallBonusUsesAwardedBaseButNeverMultipliesCapacitorPayout()
    {
        Demo.ApplyUpgrade(9);
        Demo.ApplyUpgrade(10);
        ball.SetBallCount(1);
        score.multiplier = 2;
        Charge(6);
        score.AddBlackholeScoreAt(3000, Vector3.zero);
        Assert.That(score.curScore, Is.EqualTo(2100)); // 500*2 + 50% of1000 + flat600.
        Assert.That(Demo.LastBallBonusTotal, Is.EqualTo(500));
        Assert.That(Demo.ChargePaidTotal, Is.EqualTo(600));
        Assert.That(Demo.Charge, Is.Zero);
    }

    [Test]
    public void LastBallTracksCreditsAndRemainsSeparateFromComboMultiplier()
    {
        Demo.ApplyUpgrade(10);
        Assert.That(Demo.LastBallActive, Is.False);
        score.AddScore(100);
        ball.SetBallCount(1);
        Assert.That(Demo.LastBallActive, Is.True);
        score.multiplier = 2;
        score.AddScore(100);
        score.multiplier = 1;
        score.AddScore(100);
        Assert.That(score.curScore, Is.EqualTo(550));
        Assert.That(Demo.LastBallBonusTotal, Is.EqualTo(150));
        Demo.ApplyUpgrade(10);
        Assert.That(Demo.LastBallBonusPercent, Is.EqualTo(75));
        score.AddScore(100);
        Assert.That(score.curScore, Is.EqualTo(725));
        ball.AddBallFunc(1);
        Assert.That(Demo.LastBallActive, Is.False);
    }

    [Test]
    public void MultiplyingExistingScoreDoesNotRecursivelyChargeOrAwardLastBallBonus()
    {
        Demo.ApplyUpgrade(9);
        Demo.ApplyUpgrade(10);
        ball.SetBallCount(1);
        Charge(1);
        score.curScore = 100;
        score.MultiplyScore(3);
        Assert.That(score.curScore, Is.EqualTo(300));
        Assert.That(Demo.Charge, Is.EqualTo(100));
        Assert.That(Demo.LastBallBonusTotal, Is.Zero);
    }

    [Test]
    public void EndedRunRejectsChargesLatePayoutAndExtraChoices()
    {
        Demo.ApplyUpgrade(9);
        Demo.ApplyUpgrade(10);
        Charge(1);
        ball.SetBallCount(1);
        score.HandleBallOut(ball);
        int finalScore = score.FinalUserScore;
        Charge(5);
        score.AddBlackholeScoreAt(3000, Vector3.zero);
        Assert.That(score.curScore, Is.EqualTo(finalScore));
        Assert.That(Demo.ChargePaidTotal + Demo.LastBallBonusTotal, Is.Zero);
        Assert.That(Demo.Charge, Is.EqualTo(100));
        Assert.That(Demo.LastBallActive, Is.False);
        Assert.That(Demo.TryBeginPick(1), Is.False);
        Assert.That(Demo.IsEnabled, Is.True, "The result screen still reads the final contribution model.");
    }

    [UnityTest]
    public IEnumerator TutorialInitializationRetainsAuthoredSingleBallAndHasNoDemoEffects()
    {
        Object.DestroyImmediate(Demo);
        ball.ConfigureRunBallCredits(1);
        score.ConfigureTutorial(1000);
        score.gameObject.AddComponent<BallARetroDemoRun>();
        yield return null;
        yield return null;
        Assert.That(Demo.IsEnabled, Is.False);
        Assert.That(ball.GetBallCount(), Is.EqualTo(1));
        Assert.That(BallARetroDemoRun.BlocksLaunch, Is.False);
        Assert.That(Demo.ApplyUpgrade(9), Is.False);
        score.AddBlackholeScoreAt(3000, Vector3.zero);
        Assert.That(score.curScore, Is.EqualTo(3000));
    }

    [Test]
    public void ResetClearsTheWholeRunModel()
    {
        Demo.TryBeginPick(1);
        Demo.ApplyUpgrade(9);
        Demo.ApplyUpgrade(10);
        ball.SetBallCount(1);
        Charge(1);
        score.AddBlackholeScoreAt(3000, Vector3.zero);
        KHS_Script_ResetController.ResetRun();
        Assert.That(Demo.IsEnabled, Is.False);
        Assert.That(Demo.PicksTaken + Demo.CapacitorLevel + Demo.LastBallLevel + Demo.Charge
            + Demo.ChargePaidTotal + Demo.LastBallBonusTotal, Is.Zero);
    }

    [UnityTest]
    public IEnumerator CapturePaysOnceAndCancelsItsPendingLaunchOnDrain()
    {
        Demo.ApplyUpgrade(9);
        Charge(1);
        var blackhole = New("Blackhole").AddComponent<YJ_Script_BlackholeController>();
        Set(blackhole, "holdTime", .04f);
        yield return null;
        Invoke(blackhole, "OnTriggerStay", ball.GetComponent<Collider>());
        Assert.That(body.isKinematic, Is.True);
        KHS_Script_BallOutController.Drain();
        yield return new WaitForSecondsRealtime(.1f);
        Assert.That(score.curScore, Is.Zero);
        Assert.That(Demo.ChargePaidTotal, Is.Zero);
        Assert.That(Demo.Charge, Is.EqualTo(100));
        Assert.That(body.isKinematic, Is.False);
    }

    [UnityTest]
    public IEnumerator DisablingCapturedBlackholeCannotPayLater()
    {
        Demo.ApplyUpgrade(9);
        Charge(1);
        var blackhole = New("Blackhole").AddComponent<YJ_Script_BlackholeController>();
        Set(blackhole, "holdTime", .04f);
        yield return null;
        Invoke(blackhole, "OnTriggerStay", ball.GetComponent<Collider>());
        blackhole.enabled = false;
        yield return new WaitForSecondsRealtime(.1f);
        Assert.That(score.curScore, Is.Zero);
        Assert.That(Demo.ChargePaidTotal, Is.Zero);
        Assert.That(body.isKinematic, Is.False);
    }

    [UnityTest]
    public IEnumerator RealCaptureConsumesChargeOnce()
    {
        Demo.ApplyUpgrade(9);
        Charge(1);
        var blackhole = New("Blackhole").AddComponent<YJ_Script_BlackholeController>();
        Set(blackhole, "holdTime", .02f);
        yield return null;
        Invoke(blackhole, "OnTriggerStay", ball.GetComponent<Collider>());
        Invoke(blackhole, "OnTriggerStay", ball.GetComponent<Collider>());
        yield return new WaitForSecondsRealtime(.08f);
        Assert.That(score.curScore, Is.EqualTo(600));
        Assert.That(Demo.ChargePaidTotal, Is.EqualTo(100));
        Assert.That(Demo.Charge, Is.Zero);
        Assert.That(blackhole.isActivated, Is.False);
    }

    [UnityTest]
    public IEnumerator SevenRetrySignalsOpenOneStarterAndDoubleClickAppliesOneUpgrade()
    {
        var ui = MakeChoiceUI(out var content);
        for (int i = 0; i < 7; i++) PSH_Script_GameSceneDirector.RetrySignal();
        yield return null;
        yield return null;
        yield return null;
        Assert.That(content.childCount, Is.EqualTo(3));
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(BallARetroDemoRun.BlocksLaunch, Is.True);
        var button = content.GetChild(0).GetComponent<Button>();
        button.onClick.Invoke();
        button.onClick.Invoke();
        Assert.That(Demo.CapacitorLevel, Is.EqualTo(1));
        Assert.That(Demo.PicksTaken, Is.EqualTo(1));
        Assert.That(CJS_Script_ChoiceState.I.Picked.Count, Is.EqualTo(1));
        ui.ShowChoices();
        yield return null;
        Assert.That(content.childCount, Is.Zero);
        Assert.That(BallARetroDemoRun.BlocksLaunch, Is.False);
    }

    [UnityTest]
    public IEnumerator FirstBannerCompletionOpensStarterAndLaterBannersDoNotRepeatIt()
    {
        var ui = MakeChoiceUI(out var content);
        KHS_Script_UIImgFunc.SetOpen(true);
        yield return null;
        Assert.That(content.childCount, Is.Zero);
        KHS_Script_UIImgFunc.SetOpen(false);
        yield return null;
        yield return null;
        yield return null;
        Assert.That(content.childCount, Is.EqualTo(3));
        content.GetChild(1).GetComponent<Button>().onClick.Invoke();
        KHS_Script_UIImgFunc.SetOpen(false);
        yield return null;
        yield return null;
        Assert.That(Demo.LastBallLevel, Is.EqualTo(1));
        Assert.That(content.childCount, Is.Zero);
        Set(score, "currentRound", 2);
        ui.ShowChoices();
        content.GetChild(1).GetComponent<Button>().onClick.Invoke();
        Assert.That(Demo.LastBallLevel, Is.EqualTo(2));
        Assert.That(Demo.PicksTaken, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator PlungerCannotLaunchBeforeStarterEvenWhenRetryUnlockedInput()
    {
        var plunger = New("Plunger").AddComponent<KHS_Script_PlungerController>();
        yield return null;
        plunger.isBallReady = true;
        Set(plunger, "ballRigidbody", body);
        Set(plunger, "isLock", false);
        int launches = 0;
        Action launched = () => launches++;
        KHS_Script_PlungerController.OnBallLaunched += launched;
        try
        {
            Invoke(plunger, "Launch");
            Assert.That(launches, Is.Zero);
            Demo.TryBeginPick(1);
            body.isKinematic = false;
            KHS_Script_CameraManager.SetMonitor(true);
            PSH_Script_GameSceneDirector.RetrySignal();
            Invoke(plunger, "Launch");
            Assert.That(launches, Is.Zero, "Repeated retry unlocks cannot launch while monitor is returning.");
            KHS_Script_CameraManager.SetMonitor(false);
            yield return new WaitForSecondsRealtime(.75f);
            Invoke(plunger, "Launch");
            Assert.That(launches, Is.EqualTo(1));
        }
        finally { KHS_Script_PlungerController.OnBallLaunched -= launched; }
    }

    private CJS_Script_ChoiceRoller MakeRoller(GameObject target = null)
    {
        target = target ? target : New("Roller");
        var roller = target.AddComponent<CJS_Script_ChoiceRoller>();
        roller.allChoices = new List<CJS_ChoiceData> {
            new CJS_ChoiceData { funcIdx = 6, localizationKey = "Choice_6" },
            new CJS_ChoiceData { funcIdx = 8, localizationKey = "Choice_8" }
        };
        return roller;
    }

    private CJS_Script_ChoiceUIController MakeChoiceUI(out Transform content)
    {
        New("Choice history").AddComponent<CJS_Script_ChoiceState>();
        var uiObject = New("Choice UI");
        uiObject.SetActive(false);
        var roller = MakeRoller(uiObject);
        var panel = New("Panel", typeof(RectTransform));
        content = New("Content", typeof(RectTransform)).transform;
        content.SetParent(panel.transform, false);
        var prefab = New("Card", typeof(RectTransform), typeof(Button));
        prefab.SetActive(false);
        var card = prefab.AddComponent<CJS_Script_ChoiceCard>();
        Set(card, "btn", prefab.GetComponent<Button>());
        prefab.SetActive(true);
        var ui = uiObject.AddComponent<CJS_Script_ChoiceUIController>();
        Set(ui, "rollerBehaviour", roller);
        Set(ui, "panelRoot", panel);
        Set(ui, "content", content);
        Set(ui, "cardPrefab", card);
        Set(ui, "ballCon", ball);
        uiObject.SetActive(true);
        return ui;
    }

    private void Charge(int count) { for (int i = 0; i < count; i++) Demo.RecordBumperHit(); }
    private GameObject New(string name, params Type[] components)
    { var go = new GameObject(name, components); objects.Add(go); return go; }
    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Invoke(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
}
