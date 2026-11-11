using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Same fixture and workloads before/after. CPU microbenchmarks, not an FPS claim.
public class NextFestPerformanceTests
{
    readonly List<GameObject> objects = new List<GameObject>();
    KHS_Script_ScoreManager score;
    CJS_Script_ChoiceRoller roller;

    [UnitySetUp] public IEnumerator Setup()
    {
        Time.timeScale = 1;
        var ball = New("Ball", typeof(Rigidbody)).AddComponent<YJ_Script_BallController>();
        ball.GetComponent<Rigidbody>().isKinematic = true;
        ball.SetBallCount(3);
        var camera = New("Camera holder").AddComponent<KHS_Script_CameraManager>();
        camera.cameraGos = new[] { New("Camera") };
        var go = New("Score"); go.SetActive(false);
        score = go.AddComponent<KHS_Script_ScoreManager>();
        score.targetScores = new[] { int.MaxValue };
        Set(score, "goalRound", 1); Set(score, "currentgameScores", new int[1]); Set(score, "camHolder", camera);
        go.SetActive(true);
        var rollObject = New("Roller");
        var dispatcher = rollObject.AddComponent<KHS_Script_RogueLikeManager>();
        roller = rollObject.AddComponent<CJS_Script_ChoiceRoller>();
        roller.allChoices.Clear();
        string[] rarities = { "Common", "Rare", "Epic", "Legendary" };
        for (int i = 0; i < 9; i++) roller.allChoices.Add(new CJS_ChoiceData {
            funcIdx = i, rarity = rarities[i % 4], weight = i + 1, localizationKey = "Choice_" + i, roguelike = dispatcher });
        Set(roller, "rnd", new System.Random(271828));
        yield return null; yield return null; yield return null;
    }

    [TearDown] public void Cleanup()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear(); Time.timeScale = 1;
    }

    [Serializable] class Measurement { public string workload; public int batches, iterations; public double medianMicroseconds, p95Microseconds, bytesPerOperation; }
    [Serializable] class Report { public string unity; public Measurement roll, score; }

    [Test] public void RecordComparableCpuAndAllocationBaseline()
    {
        var report = new Report { unity = Application.unityVersion,
            roll = Measure("Roll3: nine authored cards, seeded 271828", 2000, () => roller.Roll3(out _)),
            score = Measure("AddScoreAt: direct score, no UI listeners", 10000, () => score.AddScoreAt(1, Vector3.zero)) };
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../performance.json"));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        TestContext.WriteLine(File.ReadAllText(path));
        Assert.That(score.curScore, Is.GreaterThan(0));
    }

    static Measurement Measure(string name, int iterations, Action action)
    {
        for (int i = 0; i < 200; i++) action();
        var times = new double[11]; long allocated = 0;
        var watch = new Stopwatch();
        for (int b = 0; b < times.Length; b++)
        {
            GC.Collect();
            long before = GC.GetAllocatedBytesForCurrentThread();
            watch.Restart();
            for (int i = 0; i < iterations; i++) action();
            watch.Stop();
            allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            times[b] = watch.Elapsed.TotalMilliseconds * 1000 / iterations;
        }
        Array.Sort(times);
        return new Measurement { workload = name, batches = times.Length, iterations = iterations,
            medianMicroseconds = times[5], p95Microseconds = times[10], bytesPerOperation = (double)allocated / (iterations * times.Length) };
    }
    GameObject New(string name, params Type[] types) { var go = new GameObject(name, types); objects.Add(go); return go; }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
}
