using System;
using System.Collections;
using UnityEngine;

// Full-game rules v2. The shipped demo keeps its independent v1 model.
public sealed class BallARetroRunBuild : MonoBehaviour
{
    public const int CapacitorChoiceId = 9;
    public const int LastBallChoiceId = 10;
    public static BallARetroRunBuild Current { get; private set; }
    public static event Action Changed;
    public int CapacitorLevel { get; private set; }
    public int LastBallLevel { get; private set; }
    public int Charge { get; private set; }
    public int ChargePaidTotal { get; private set; }
    public int ChargeLostTotal { get; private set; }
    public int LastBallBonusTotal { get; private set; }
    public int MaxCharge => CapacitorLevel == 0 ? 0 : CapacitorLevel == 1 ? 600 : 1000;
    public int LastBallBonusPercent => LastBallLevel == 0 ? 0 : LastBallLevel == 1 ? 50 : 75;
    public bool IsEnabled => initialized && !BallARetroPlatform.IsDemoBuild && score != null && !score.IsTutorialRun;
    public bool LastBallActive => IsEnabled && score.IsPlaying && !score.HasEnded && LastBallLevel > 0
        && ball != null && ball.gameObject.activeInHierarchy && ball.GetBallCount() == 1;
    public int ChargePerHit => CapacitorLevel == 0 ? 0 : LastBallActive ? 200 : 100;
    private KHS_Script_ScoreManager score;
    private YJ_Script_BallController ball;
    private bool initialized;

    private void Awake() { score = GetComponent<KHS_Script_ScoreManager>(); Current = this; }
    private IEnumerator Start()
    {
        yield return null;
        if (BallARetroPlatform.IsDemoBuild || score == null || score.IsTutorialRun) yield break;
        ball = FindAnyObjectByType<YJ_Script_BallController>();
        initialized = ball != null;
        NotifyStateChanged();
    }
    private void OnEnable() => KHS_Script_ResetController.OnReset += ResetRun;
    private void OnDisable() => KHS_Script_ResetController.OnReset -= ResetRun;
    private void OnDestroy() { if (Current == this) { Current = null; Changed?.Invoke(); } }

    public bool ApplyUpgrade(int id)
    {
        if (!IsEnabled || score.HasEnded) return false;
        if (id == CapacitorChoiceId && CapacitorLevel < 2) CapacitorLevel++;
        else if (id == LastBallChoiceId && LastBallLevel < 2) LastBallLevel++;
        else return false;
        NotifyStateChanged();
        return true;
    }
    public void RecordBumperHit()
    {
        if (!IsEnabled || !score.IsPlaying || score.HasEnded || Time.timeScale <= 0 || CapacitorLevel == 0) return;
        int next = Mathf.Min(MaxCharge, Charge + ChargePerHit);
        if (next == Charge) return;
        Charge = next;
        NotifyStateChanged();
    }
    // Only ScoreManager's accepted transaction may consume or lose charge.
    public int ConsumeCharge()
    {
        if (!IsEnabled || !score.IsPlaying || Time.timeScale <= 0) return 0;
        int paid = Charge; Charge = 0;
        return paid;
    }
    public int CalculateLastBallBonus(int awardedBase) => LastBallActive && awardedBase > 0
        ? Mathf.RoundToInt(awardedBase * (LastBallBonusPercent / 100f)) : 0;
    public void RecordAwarded(int chargePaid, int lastBallBonus)
    {
        if (chargePaid <= 0 && lastBallBonus <= 0) return;
        ChargePaidTotal += Mathf.Max(0, chargePaid);
        LastBallBonusTotal += Mathf.Max(0, lastBallBonus);
        NotifyStateChanged();
    }
    public void LoseUnpaidCharge()
    {
        if (!IsEnabled || !score.IsPlaying) return;
        ChargeLostTotal += Charge;
        Charge = 0;
        NotifyStateChanged();
    }
    public void NotifyStateChanged() => Changed?.Invoke();
    private void ResetRun()
    {
        initialized = false;
        CapacitorLevel = LastBallLevel = Charge = ChargePaidTotal = ChargeLostTotal = LastBallBonusTotal = 0;
        NotifyStateChanged();
    }
}
