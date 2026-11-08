using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class RouletteFlowTests
{
    readonly List<GameObject> objects = new List<GameObject>();
    KHS_Script_ScoreManager score;
    YJ_Script_BallController ball;
    YJ_Script_RoulettePhysicsController roulette;
    MeshRenderer plate, numbers;
    MeshCollider collider;
    float previousTimeScale;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1;
        score = Make("Score manager").AddComponent<KHS_Script_ScoreManager>();
        ball = Make("Ball").AddComponent<YJ_Script_BallController>();
        var wheel = Make("Roulette plate");
        wheel.SetActive(false);
        plate = wheel.AddComponent<MeshRenderer>();
        collider = wheel.AddComponent<MeshCollider>();
        var numberObject = Make("Roulette numbers");
        numberObject.transform.SetParent(wheel.transform, false);
        numbers = numberObject.AddComponent<MeshRenderer>();
        roulette = wheel.AddComponent<YJ_Script_RoulettePhysicsController>();
        typeof(YJ_Script_RoulettePhysicsController).GetField("numberRenderer", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(roulette, numbers);
        roulette.greenSlotNumbers = new List<int> { 0 };
        roulette.redSlotNumbers = new List<int> { 36, 7 };
        roulette.blackSlotNumbers = new List<int> { 11 };
        roulette.stopDuration = .03f;
        wheel.SetActive(true);
        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        if (roulette != null) roulette.StopAllCoroutines();
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null) Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Time.timeScale = previousTimeScale;
    }

    [UnityTest]
    public IEnumerator ScoringPauseKeepsPlateUntilChoiceFlowResumes()
    {
        // AddScore invokes round-clear synchronously in the actual game. Keep
        // physics paused during choices; switching this wait to realtime is wrong.
        score.AfterScore = () => Time.timeScale = 0;
        roulette.StartStopSequence(ball);
        roulette.UpdateCurrentSlot(36);
        yield return WaitFor(() => score.Calls == 1, 1);
        Assert.That(score.Total, Is.EqualTo(3600));
        Assert.That(Time.timeScale, Is.Zero);
        yield return new WaitForSecondsRealtime(2.2f);
        AssertPlate(true);
        Assert.That(score.Calls, Is.EqualTo(1), "Paused result must not award twice.");

        Time.timeScale = 1;
        yield return WaitFor(() => !plate.enabled, 2.4f);
        AssertPlate(false);
        Assert.That(score.Calls, Is.EqualTo(1));
        roulette.ResetRoulette();
    }

    [UnityTest]
    public IEnumerator BallOutDuringHiddenWindowRestoresPlateImmediately()
    {
        roulette.StartStopSequence(ball);
        roulette.UpdateCurrentSlot(11);
        yield return WaitFor(() => !plate.enabled, 2.5f);
        AssertPlate(false);

        KHS_Script_BallOutController.Drain();
        AssertPlate(true);
        Assert.That(score.Calls, Is.EqualTo(1));

        // A subsequent spin must work immediately, without waiting for the old
        // five-second restoration coroutine or carrying the old slot result.
        roulette.StartStopSequence(ball);
        roulette.UpdateCurrentSlot(7);
        yield return WaitFor(() => score.Calls == 2, 1);
        Assert.That(score.Total, Is.EqualTo(1800));
        AssertPlate(true);
        roulette.ResetRoulette();
    }

    [UnityTest]
    public IEnumerator BallOutDuringDecelerationCancelsAwardAndReleasesOnce()
    {
        roulette.stopDuration = 1;
        roulette.StartStopSequence(ball);
        roulette.UpdateCurrentSlot(36);
        KHS_Script_BallOutController.Drain();
        Assert.That(ball.Releases, Is.EqualTo(1));
        Assert.That(ball.LastReleaseVelocity, Is.EqualTo(Vector3.zero));
        Assert.That(ball.LastReleaseWasRoulette, Is.False);
        AssertPlate(true);
        yield return new WaitForSeconds(1.1f);
        Assert.That(score.Calls, Is.Zero, "Interrupted spin must not award its stale slot.");
        KHS_Script_BallOutController.Drain();
        Assert.That(ball.Releases, Is.EqualTo(1));

        roulette.stopDuration = .03f;
        roulette.StartStopSequence(ball);
        roulette.UpdateCurrentSlot(7);
        yield return WaitFor(() => score.Calls == 1, 1);
        Assert.That(score.Total, Is.EqualTo(700));
        roulette.ResetRoulette();
    }

    [UnityTest]
    public IEnumerator DisableAndReenableCancelsPartialSpinAndRestoresHiddenPlate()
    {
        roulette.stopDuration = 1;
        roulette.StartStopSequence(ball);
        roulette.UpdateCurrentSlot(36);
        roulette.gameObject.SetActive(false);
        Assert.That(ball.Releases, Is.Zero, "Disabling a visual mechanism must not release another owner's ball.");
        AssertPlate(true);
        yield return null;

        roulette.gameObject.SetActive(true);
        roulette.stopDuration = .03f;
        roulette.StartStopSequence(ball);
        roulette.UpdateCurrentSlot(11);
        yield return WaitFor(() => score.Calls == 1, 1);
        yield return WaitFor(() => !plate.enabled, 2.5f);
        roulette.gameObject.SetActive(false);
        AssertPlate(true);
        Assert.That(ball.Releases, Is.Zero, "Already dropped ball must not be released again.");

        roulette.gameObject.SetActive(true);
        roulette.StartStopSequence(ball);
        roulette.UpdateCurrentSlot(7);
        yield return WaitFor(() => score.Calls == 2, 1);
        Assert.That(score.Total, Is.EqualTo(1800), "Interrupted result 36 must not leak into later spins.");
        roulette.ResetRoulette();
    }

    void AssertPlate(bool enabled)
    {
        Assert.That(plate.enabled, Is.EqualTo(enabled), "Plate visibility");
        Assert.That(numbers.enabled, Is.EqualTo(enabled), "Number visibility");
        Assert.That(collider.enabled, Is.EqualTo(enabled), "Plate collision");
    }

    static IEnumerator WaitFor(System.Func<bool> condition, float seconds)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "Timed out waiting for roulette transition.");
    }

    GameObject Make(string name)
    {
        var go = new GameObject(name);
        objects.Add(go);
        return go;
    }
}
