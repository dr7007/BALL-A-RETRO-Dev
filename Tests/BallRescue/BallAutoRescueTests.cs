using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class BallAutoRescueTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private YJ_Script_BallAutoRescue rescue;
    private YJ_Script_BallController ball;
    private Rigidbody body;
    private KHS_Script_ScoreManager score;
    private KHS_Script_PlungerController plunger;
    private KHS_Script_FliperController flippers;
    private YJ_Script_BallAutoRescue.RescueZone zone;
    private GameObject floor;
    private SimulationMode previousSimulation;
    private float previousFixedDelta;
    private Vector3 stuck = new Vector3(0, 0.25f, 0);

    [SetUp]
    public void SetUp()
    {
        stuck = new Vector3(0, 0.25f, 0);
        previousSimulation = Physics.simulationMode;
        previousFixedDelta = Time.fixedDeltaTime;
        Physics.simulationMode = SimulationMode.Script;
        Time.fixedDeltaTime = 0.02f;
        Time.timeScale = 1f;
        score = New("Score").AddComponent<KHS_Script_ScoreManager>();
        Set(score, "currentState", KHS_Script_ScoreManager.RoundState.Playing);
        score.curScore = 2350;
        flippers = New("Flippers").AddComponent<KHS_Script_FliperController>();
        Set(flippers, "flippers", new YJ_Script_Flipper[0]);
        flippers.fliper_Count = 8;
        plunger = New("Plunger").AddComponent<KHS_Script_PlungerController>();
        floor = Box("Floor", new Vector3(0, 0, 0), new Vector3(20, 0.24f, 20));
        var go = New("Ball");
        go.transform.position = stuck;
        body = go.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.mass = 0.2f;
        body.constraints = RigidbodyConstraints.FreezePositionY;
        go.AddComponent<SphereCollider>().radius = 0.125f;
        ball = go.AddComponent<YJ_Script_BallController>();
        ball.SetBallCount(2);
        Call(ball, "Start");
        rescue = go.AddComponent<YJ_Script_BallAutoRescue>();
        zone = new YJ_Script_BallAutoRescue.RescueZone {
            label = "Test corner", anchor = New("Obstacle anchor").transform,
            localBounds = new Bounds(stuck, new Vector3(0.7f, 0.32f, 0.7f)),
            localPushDirection = Vector3.left,
            localRecoveryPosition = new Vector3(-0.75f, 0.25f, -0.25f)
        };
        Set(rescue, "zones", new[] { zone });
        Set(rescue, "plunger", plunger);
        Physics.SyncTransforms();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Physics.simulationMode = previousSimulation;
        Time.fixedDeltaTime = previousFixedDelta;
        Time.timeScale = 1f;
    }

    [Test]
    public void ConfinementPushesOnceThenRecoversWithoutSpendingResources()
    {
        Hold(145);
        Assert.That(Get<bool>(rescue, "pushed"), Is.False);
        Hold(10);
        Assert.That(Get<bool>(rescue, "pushed"), Is.True);
        Assert.That(body.position.x, Is.GreaterThan(-0.1f), "First stage must not teleport.");
        Hold(90);
        Assert.That(body.position.x, Is.GreaterThan(-0.1f));
        HoldUntilRecovered(20);
        Assert.That(Vector3.Distance(body.position, zone.localRecoveryPosition), Is.LessThan(0.03f));
        Assert.That(ball.GetBallCount(), Is.EqualTo(2));
        Assert.That(score.curScore, Is.EqualTo(2350));
        Assert.That(score.IsPlaying, Is.True);
        Assert.That(flippers.fliper_Count, Is.EqualTo(8));
    }

    [Test]
    public void PhysicalTrapSurvivesThePushAndThenReleasesToTheSafePoint()
    {
        Box("Left wall", stuck + Vector3.left * 0.25f, new Vector3(0.24f, 0.4f, 1f));
        Box("Right wall", stuck + Vector3.right * 0.25f, new Vector3(0.24f, 0.4f, 1f));
        Box("Back wall", stuck + Vector3.back * 0.25f, new Vector3(1f, 0.4f, 0.24f));
        Box("Front wall", stuck + Vector3.forward * 0.25f, new Vector3(1f, 0.4f, 0.24f));
        Physics.SyncTransforms();
        bool sawPush = false;
        for (int i = 0; i < 280 && body.position.x > -0.6f; i++)
        {
            Call(rescue, "FixedUpdate");
            sawPush |= Get<bool>(rescue, "pushed");
            Physics.Simulate(0.02f);
        }
        Assert.That(sawPush, Is.True);
        Assert.That(Vector3.Distance(body.position, zone.localRecoveryPosition), Is.LessThan(0.03f));
    }

    [Test]
    public void PushHasOnlyTheConfiguredHorizontalVelocity()
    {
        for (int i = 0; i < 160 && !Get<bool>(rescue, "pushed"); i++) Hold(1);
        Assert.That(Get<bool>(rescue, "pushed"), Is.True);
        Assert.That(body.linearVelocity.x, Is.EqualTo(-0.8f).Within(0.01f));
        Assert.That(body.linearVelocity.y, Is.Zero.Within(0.001f));
    }

    [TestCase("outside")]
    [TestCase("other_floor")]
    [TestCase("kinematic")]
    [TestCase("falling")]
    [TestCase("pacman")]
    [TestCase("launch_wait")]
    [TestCase("tutorial")]
    [TestCase("round_transition")]
    [TestCase("ended")]
    [TestCase("monitor")]
    [TestCase("round_ui")]
    [TestCase("disabled_ball")]
    public void IntentionalStopsAndPositionsOutsideTheRegionAreNeverRescued(string reason)
    {
        if (reason == "outside") stuck.x = 2;
        if (reason == "other_floor") stuck.y = 2;
        if (reason == "kinematic") body.isKinematic = true;
        if (reason == "falling") body.constraints = RigidbodyConstraints.None;
        if (reason == "pacman") ball.SetControlMode(YJ_Script_BallController.ControlMode.PacMan);
        if (reason == "launch_wait") plunger.isBallReady = true;
        if (reason == "tutorial") plunger.tutorialForceLock = true;
        if (reason == "round_transition") Set(score, "currentState", KHS_Script_ScoreManager.RoundState.RoundClear);
        if (reason == "ended") Set(score, "currentState", KHS_Script_ScoreManager.RoundState.GameClear);
        if (reason == "monitor") KHS_Script_CameraManager.SetMonitor(true);
        if (reason == "round_ui") KHS_Script_UIImgFunc.SetOpen(true);
        if (reason == "disabled_ball") ball.enabled = false;
        for (int i = 0; i < 400; i++) { body.position = stuck; Call(rescue, "FixedUpdate"); }
        Assert.That(body.position, Is.EqualTo(stuck));
        Assert.That(Get<bool>(rescue, "pushed"), Is.False);
    }

    [Test]
    public void APausedChoiceDiscardsThePrePauseCountdown()
    {
        Hold(145);
        Time.timeScale = 0;
        Call(rescue, "Update");
        Time.timeScale = 1;
        Hold(10);
        Assert.That(Get<bool>(rescue, "pushed"), Is.False);
        Hold(145);
        Assert.That(Get<bool>(rescue, "pushed"), Is.True);
    }

    [Test]
    public void GenuineMovementRestartsCountdownButSmallJitterDoesNot()
    {
        for (int i = 0; i < 400; i++)
        {
            body.position = stuck + Vector3.right * (i % 40 < 20 ? -0.2f : 0.2f);
            Call(rescue, "FixedUpdate");
        }
        Assert.That(Get<bool>(rescue, "pushed"), Is.False);
        Hold(155);
        Assert.That(Get<bool>(rescue, "pushed"), Is.True);
    }

    [TestCase("occupied")]
    [TestCase("trigger")]
    [TestCase("no_floor")]
    [TestCase("different_floor")]
    public void InvalidDestinationNeverTeleportsTheBall(string reason)
    {
        if (reason == "occupied") Box("Obstacle", zone.localRecoveryPosition, Vector3.one * 0.2f);
        if (reason == "trigger") Box("Portal trigger", zone.localRecoveryPosition, Vector3.one * 0.2f).GetComponent<Collider>().isTrigger = true;
        if (reason == "no_floor") floor.SetActive(false);
        if (reason == "different_floor") zone.localRecoveryPosition += Vector3.up;
        Physics.SyncTransforms();
        Hold(500);
        Assert.That(body.position.x, Is.GreaterThan(-0.1f));
        Assert.That(Get<bool>(rescue, "pushed"), Is.True);
    }

    [Test]
    public void ABlockedDestinationCanRecoverAfterItBecomesClear()
    {
        var obstacle = Box("Obstacle", zone.localRecoveryPosition, Vector3.one * 0.2f);
        Physics.SyncTransforms();
        Hold(270);
        Assert.That(body.position.x, Is.GreaterThan(-0.1f));
        obstacle.SetActive(false);
        Physics.SyncTransforms();
        HoldUntilRecovered(120);
        Assert.That(body.position.x, Is.LessThan(-0.6f));
    }

    [Test]
    public void RegionMovesWithItsConfiguredObstacle()
    {
        zone.anchor.position = new Vector3(5, 0, 3);
        zone.anchor.rotation = Quaternion.Euler(0, 90, 0);
        Assert.That(zone.Contains(stuck), Is.False);
        stuck = zone.anchor.TransformPoint(zone.localBounds.center);
        Assert.That(zone.Contains(stuck), Is.True);
        Hold(155);
        Assert.That(Get<bool>(rescue, "pushed"), Is.True);
    }

    private void Hold(int steps)
    {
        for (int i = 0; i < steps; i++) StepHeld(i);
    }
    private void HoldUntilRecovered(int steps)
    {
        for (int i = 0; i < steps && body.position.x > -0.6f; i++) StepHeld(i);
    }
    private void StepHeld(int i)
    {
        body.position = stuck + Vector3.right * (i % 2 == 0 ? 0.015f : -0.015f);
        body.linearVelocity = Vector3.zero;
        Call(rescue, "FixedUpdate");
        Physics.Simulate(0.02f);
    }
    private GameObject New(string name) { var go = new GameObject(name); objects.Add(go); return go; }
    private GameObject Box(string name, Vector3 position, Vector3 size)
    {
        var go = New(name); go.transform.position = position; go.AddComponent<BoxCollider>().size = size; return go;
    }
    private static void Call(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
}
