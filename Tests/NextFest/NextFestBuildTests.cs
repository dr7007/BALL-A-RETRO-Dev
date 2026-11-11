using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class NextFestBuildTests
{
    readonly List<GameObject> objects = new List<GameObject>();
    KHS_Script_ScoreManager score;
    YJ_Script_BallController ball;
    BallARetroRunBuild Run => BallARetroRunBuild.Current;
    [UnitySetUp] public IEnumerator Setup()
    {
        Time.timeScale = 1;
        var ballObject = New("Ball", typeof(Rigidbody), typeof(SphereCollider)); ballObject.tag = "Ball";
        ballObject.GetComponent<Rigidbody>().isKinematic = true;
        ball = ballObject.AddComponent<YJ_Script_BallController>(); ball.SetBallCount(3);
        var camera = New("Cameras").AddComponent<KHS_Script_CameraManager>(); camera.cameraGos = new[] { New("Camera") };
        var go = New("Score"); go.SetActive(false); score = go.AddComponent<KHS_Script_ScoreManager>();
        score.targetScores = new[] { 100000, 200000, 300000 };
        Set(score, "goalRound", 3); Set(score, "currentgameScores", new int[3]); Set(score, "camHolder", camera);
        go.SetActive(true);
        yield return null; yield return null; yield return null;
        Assert.That(Run.IsEnabled, Is.True);
        Assert.That(BallARetroDemoRun.Current, Is.Null);
    }
    [TearDown] public void Cleanup()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear(); Time.timeScale = 1;
    }
    [Test] public void PairChangesChargingTargetWithoutMultiplyingPayout()
    {
        Run.ApplyUpgrade(9); Run.RecordBumperHit(); Assert.That(Run.Charge, Is.EqualTo(100));
        Run.ApplyUpgrade(10); ball.SetBallCount(1);
        Run.RecordBumperHit(); Assert.That(Run.Charge, Is.EqualTo(300));
        score.multiplier = 2; score.AddRunScoreMultiplier(.2f);
        score.AddBlackholeScoreAt(3000, Vector3.zero);
        Assert.That(score.curScore, Is.EqualTo(11100)); // 3000 * 2 * 1.2 * 1.5 + 300.
        Assert.That(Run.ChargePaidTotal, Is.EqualTo(300));
        Assert.That(Run.LastBallBonusTotal, Is.EqualTo(3600));
        Assert.That(Run.Charge, Is.Zero);
    }
    [Test] public void ActualAcceptedBumperChargesOnceAndOrdinarySwitchDoesNot()
    {
        Run.ApplyUpgrade(9); Run.ApplyUpgrade(10); ball.SetBallCount(1);
        var bumper = New("Bumper", typeof(KHS_Script_BumperEffect)).AddComponent<KHS_Script_DumpManager>();
        bumper.bumpScore = 100;
        Invoke(bumper, "OnTriggerEnter", ball.GetComponent<Collider>());
        Invoke(bumper, "OnTriggerEnter", ball.GetComponent<Collider>());
        Assert.That(Run.Charge, Is.EqualTo(200)); Assert.That(score.curScore, Is.EqualTo(150));
        var normal = New("Switch").AddComponent<KHS_Script_DumpManager>();
        Invoke(normal, "OnTriggerEnter", ball.GetComponent<Collider>());
        Assert.That(Run.Charge, Is.EqualTo(200));
    }
    [Test] public void CapsAndExtraBallTradeoffAreExplicit()
    {
        Run.ApplyUpgrade(9); Run.ApplyUpgrade(10); ball.SetBallCount(1);
        Hit(100); Assert.That(Run.Charge, Is.EqualTo(600));
        Assert.That(Run.ApplyUpgrade(9), Is.True); Hit(100); Assert.That(Run.Charge, Is.EqualTo(1000));
        Run.ApplyUpgrade(10); Assert.That(Run.LastBallBonusPercent, Is.EqualTo(75));
        Assert.That(Run.ApplyUpgrade(9) || Run.ApplyUpgrade(10), Is.False);
        ball.AddBallFunc(2); Assert.That(Run.LastBallActive, Is.False); Assert.That(Run.ChargePerHit, Is.EqualTo(100));
    }
    [Test] public void DrainLosesUnpaidChargeButKeepsLevelsAndPaidScore()
    {
        Run.ApplyUpgrade(9); Run.ApplyUpgrade(10); Hit(3);
        score.AddScore(100); score.HandleBallOut(ball);
        Assert.That(Run.Charge, Is.Zero); Assert.That(Run.ChargeLostTotal, Is.EqualTo(300));
        Assert.That(Run.CapacitorLevel + Run.LastBallLevel, Is.EqualTo(2)); Assert.That(score.curScore, Is.EqualTo(100));
        Hit(1); score.HandleBallOut(ball); Assert.That(Run.LastBallActive, Is.True);
        Hit(1); Assert.That(Run.Charge, Is.EqualTo(200));
    }
    [UnityTest] public IEnumerator RoundBoundaryPreservesChargeAndBlackholeTransactionIsAtomic()
    {
        Run.ApplyUpgrade(9); Hit(3); score.targetScore = 500;
        score.AddScore(500); yield return null; yield return null;
        Assert.That(score.RoundRespone(), Is.EqualTo(2)); Assert.That(Run.Charge, Is.EqualTo(300));
        score.targetScore = 1000;
        score.AddBlackholeScoreAt(3000, Vector3.zero);
        Assert.That(score.curScore, Is.EqualTo(3800)); Assert.That(Run.ChargePaidTotal, Is.EqualTo(300));
        yield return null; yield return null;
        Assert.That(score.RoundRespone(), Is.EqualTo(3));
    }
    [Test] public void PausedAndEndedRunRejectLateScoringAndCharge()
    {
        Run.ApplyUpgrade(9); Hit(1); Time.timeScale = 0;
        Hit(3); score.AddScore(100); score.AddBlackholeScoreAt(3000, Vector3.zero);
        Assert.That(Run.Charge, Is.EqualTo(100)); Assert.That(score.curScore, Is.Zero);
        Time.timeScale = 1; ball.SetBallCount(1); score.HandleBallOut(ball);
        Hit(3); score.AddBlackholeScoreAt(3000, Vector3.zero);
        Assert.That(score.HasEnded, Is.True); Assert.That(Run.Charge, Is.Zero);
        Assert.That(Run.ApplyUpgrade(10), Is.False); Assert.That(score.curScore, Is.Zero);
    }
    [Test] public void ExistingTotalMultiplierDoesNotRecursivelyApplyBuildBonuses()
    {
        Run.ApplyUpgrade(9); Run.ApplyUpgrade(10); ball.SetBallCount(1); Hit(1);
        score.curScore = 100; score.MultiplyScore(3);
        Assert.That(score.curScore, Is.EqualTo(300)); Assert.That(Run.Charge, Is.EqualTo(200));
        Assert.That(Run.ChargePaidTotal + Run.LastBallBonusTotal, Is.Zero);
    }
    [Test] public void TutorialAndResetDisableAllBuildState()
    {
        Run.ApplyUpgrade(9); Run.ApplyUpgrade(10); Hit(1);
        KHS_Script_ResetController.ResetRun();
        Assert.That(Run.IsEnabled, Is.False);
        Assert.That(Run.CapacitorLevel + Run.LastBallLevel + Run.Charge + Run.ChargeLostTotal, Is.Zero);
        score.ConfigureTutorial(1000); Assert.That(Run.ApplyUpgrade(9), Is.False);
    }
    [UnityTest] public IEnumerator RealBlackholeCapturePaysOnlyOnceAndDrainCancelsPendingPayout()
    {
        Run.ApplyUpgrade(9); Hit(1);
        var hole = New("Blackhole").AddComponent<YJ_Script_BlackholeController>(); Set(hole, "holdTime", .02f);
        yield return null;
        Invoke(hole, "OnTriggerStay", ball.GetComponent<Collider>());
        Invoke(hole, "OnTriggerStay", ball.GetComponent<Collider>());
        yield return new WaitForSecondsRealtime(.08f);
        Assert.That(score.curScore, Is.EqualTo(3100)); Assert.That(Run.ChargePaidTotal, Is.EqualTo(100));
        ball.transform.position = hole.transform.position; Hit(1);
        Invoke(hole, "OnTriggerStay", ball.GetComponent<Collider>());
        KHS_Script_BallOutController.Drain();
        yield return new WaitForSecondsRealtime(.08f);
        Assert.That(score.curScore, Is.EqualTo(3100)); Assert.That(Run.Charge, Is.Zero);
    }
    void Hit(int n) { for (int i = 0; i < n; i++) Run.RecordBumperHit(); }
    GameObject New(string name, params Type[] types) { var go = new GameObject(name, types); objects.Add(go); return go; }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
}
