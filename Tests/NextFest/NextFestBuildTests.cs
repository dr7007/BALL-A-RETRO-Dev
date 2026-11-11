using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public partial class NextFestBuildTests
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
    [Test] public void RarityDistributionUsesDocumentedPerCardWeights()
    {
        MaxStrategies();
        var roller = MakeRoller(4, 5, 6, 8);
        string[] rarity = { "Common", "Rare", "Epic", "Legendary" };
        for (int i = 0; i < 4; i++) roller.allChoices[i].rarity = rarity[i];
        int[] counts = new int[4];
        for (int i = 0; i < 20000; i++)
        {
            var offer = roller.Roll3(out var chance);
            int index = roller.allChoices.FindIndex(c => c.funcIdx == offer[0].funcIdx);
            counts[index]++;
            Assert.That(chance[offer[0]], Is.EqualTo(new[] { 60, 30, 9, 1 }[index]).Within(.001f));
            Assert.That(offer.Select(c => c.funcIdx).Distinct().Count(), Is.EqualTo(3));
        }
        for (int i = 0; i < 4; i++)
            Assert.That(counts[i] / 200f, Is.EqualTo(new[] { 60, 30, 9, 1 }[i]).Within(1.2f));
        TestContext.WriteLine("20,000 first draws: " + string.Join(",", counts));
    }
    [Test] public void InvalidDisabledConsumedAndMaxedEffectsNeverReturn()
    {
        MaxStrategies(); var roller = MakeRoller(0, 1, 2, 3, 4, 5, 6, 7, 8);
        roller.UnActiveChoices(5); roller.UnActiveChoices(7);
        roller.allChoices[0].weight = 0; roller.allChoices[1].weight = float.NaN;
        roller.allChoices[2].weight = float.PositiveInfinity; roller.allChoices[3].rarity = "invalid";
        roller.allChoices.Add(new CJS_ChoiceData { funcIdx = 99, weight = 10 });
        roller.allChoices.Add(roller.allChoices[6]);
        for (int i = 0; i < 50; i++)
            CollectionAssert.AreEquivalent(new[] { 4, 6, 8 }, roller.Roll3(out _).Select(c => c.funcIdx));
        foreach (var choice in roller.allChoices) choice.weight = 0;
        Assert.That(roller.Roll3(out _), Is.Empty);
    }
    [Test] public void RerollChangesAnOfferWhenAlternativesExistAndHandlesSmallPool()
    {
        var roller = MakeRoller(0, 1, 2, 3, 4, 5, 6, 7, 8);
        var prior = roller.Roll3(out _).Select(c => c.funcIdx).ToArray();
        for (int i = 0; i < 100; i++)
        {
            var offer = roller.Reroll3(prior, out _);
            Assert.That(offer.Count, Is.EqualTo(3));
            Assert.That(offer.Count(c => c.funcIdx <= 3), Is.LessThanOrEqualTo(1));
            Assert.That(offer.Any(c => !prior.Contains(c.funcIdx)), Is.True);
            prior = offer.Select(c => c.funcIdx).ToArray();
        }
        MaxStrategies(); roller.allChoices.Clear();
        Assert.That(roller.Reroll3(prior, out _), Is.Empty);
        roller = MakeRoller(6);
        Assert.That(roller.Reroll3(new[] { 6 }, out _).Single().funcIdx, Is.EqualTo(6));
    }
    [UnityTest] public IEnumerator RerollBudgetPersistsAcrossReopenAndPickButRenewsNextRound()
    {
        var ui = MakeUI(out var roller, out var reroll);
        ui.ShowChoices();
        var stale = Cards(ui)[0].GetComponent<Button>();
        for (int i = 0; i < 3; i++)
        {
            reroll.onClick.Invoke(); reroll.onClick.Invoke();
            Assert.That(Read<int>(ui, "usedReroll"), Is.EqualTo(i + 1));
            stale.onClick.Invoke();
            Assert.That(CJS_Script_ChoiceState.I.Picked, Is.Empty);
            yield return new WaitForSecondsRealtime(.22f);
        }
        reroll.onClick.Invoke(); Assert.That(Read<int>(ui, "usedReroll"), Is.EqualTo(3));
        Assert.That(reroll.interactable, Is.False);
        var before = Cards(ui).Select(c => Read<CJS_ChoiceData>(c, "data").funcIdx).ToArray();
        ui.Hide(); ui.ShowChoices();
        CollectionAssert.AreEqual(before, Cards(ui).Select(c => Read<CJS_ChoiceData>(c, "data").funcIdx));
        Assert.That(Read<int>(ui, "usedReroll"), Is.EqualTo(3));
        Cards(ui)[0].GetComponent<Button>().onClick.Invoke();
        Assert.That(CJS_Script_ChoiceState.I.Picked.Count, Is.EqualTo(1));
        Assert.That(Read<int>(ui, "usedReroll"), Is.EqualTo(3));
        ui.Hide(); Set(score, "currentRound", 2); ui.ShowChoices();
        Assert.That(Read<int>(ui, "usedReroll"), Is.Zero);
        Assert.That(reroll.interactable, Is.True);
    }
    [UnityTest] public IEnumerator EmptyPoolAndCompletedScreenCannotTrapOrGrantMorePicks()
    {
        MaxStrategies(); ball.SetBallCount(1);
        var ui = MakeUI(out var roller, out _);
        roller.allChoices.Clear(); ui.ShowChoices();
        Assert.That(Read<bool>(ui, "isOpen"), Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
        ui.ShowChoices(); Assert.That(Read<bool>(ui, "isOpen"), Is.False);
        yield return null;
    }

    CJS_Script_ChoiceRoller MakeRoller(params int[] ids)
    {
        var go = New("Roller"); var dispatcher = go.AddComponent<KHS_Script_RogueLikeManager>();
        var roller = go.AddComponent<CJS_Script_ChoiceRoller>(); roller.allChoices.Clear();
        foreach (int id in ids) roller.allChoices.Add(new CJS_ChoiceData { funcIdx = id, weight = 1, roguelike = dispatcher });
        Set(roller, "rnd", new System.Random(271828)); return roller;
    }
    CJS_Script_ChoiceUIController MakeUI(out CJS_Script_ChoiceRoller roller, out Button reroll)
    {
        New("History").AddComponent<CJS_Script_ChoiceState>();
        roller = MakeRoller(4, 5, 6, 7, 8);
        var go = New("UI"); go.SetActive(false);
        var panel = New("Panel", typeof(RectTransform));
        var content = New("Content", typeof(RectTransform)); content.transform.SetParent(panel.transform, false);
        var template = New("Card", typeof(RectTransform), typeof(Button)); template.SetActive(false);
        var card = template.AddComponent<CJS_Script_ChoiceCard>(); Set(card, "btn", template.GetComponent<Button>()); template.SetActive(true);
        reroll = New("Reroll", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
        var ui = go.AddComponent<CJS_Script_ChoiceUIController>();
        Set(ui, "rollerBehaviour", roller); Set(ui, "panelRoot", panel); Set(ui, "content", content.transform);
        Set(ui, "cardPrefab", card); Set(ui, "ballCon", ball); Set(ui, "btnReroll", reroll);
        go.SetActive(true); return ui;
    }
    void MaxStrategies() { Run.ApplyUpgrade(9); Run.ApplyUpgrade(9); Run.ApplyUpgrade(10); Run.ApplyUpgrade(10); }
    static List<CJS_Script_ChoiceCard> Cards(object ui) => Read<List<CJS_Script_ChoiceCard>>(ui, "liveCards");
    static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
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
