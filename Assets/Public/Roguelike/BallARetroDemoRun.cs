using System;
using System.Collections;
using UnityEngine;

// The demo's small run model is independent of table combo multipliers and UI.
// It is created by the score manager only for explicitly marked demo players.
public sealed class BallARetroDemoRun : MonoBehaviour
{
    public const int CapacitorChoiceId = 9;
    public const int LastBallChoiceId = 10;
    public static BallARetroDemoRun Current { get; private set; }
    public static event Action Changed;

    public int Charge { get; private set; }
    public int MaxCharge => CapacitorLevel == 0 ? 0 : CapacitorLevel == 1 ? 600 : 1000;
    public int CapacitorLevel { get; private set; }
    public int LastBallLevel { get; private set; }
    public int LastBallBonusPercent => LastBallLevel == 0 ? 0 : LastBallLevel == 1 ? 50 : 75;
    public int ChargePaidTotal { get; private set; }
    public int LastBallBonusTotal { get; private set; }
    public int PicksTaken { get; private set; }
    public bool IsEnabled => initialized && BallARetroPlatform.IsDemoBuild && score != null && !score.IsTutorialRun;
    public bool LastBallActive => IsEnabled && score.IsPlaying && !score.HasEnded
        && LastBallLevel > 0 && ball != null && ball.gameObject.activeInHierarchy && ball.GetBallCount() == 1;
    public static bool BlocksLaunch => Current != null && Current.IsEnabled
        && (Current.PicksTaken == 0 || Current.monitorActive);

    private KHS_Script_ScoreManager score;
    private YJ_Script_BallController ball;
    private bool initialized;
    private int lastPickedRound;
    private bool monitorActive;

    private void Awake()
    {
        score = GetComponent<KHS_Script_ScoreManager>();
        Current = this;
    }

    private IEnumerator Start()
    {
        // The tutorial configures its score manager in Start, and the ball records
        // its authored credits there too. Wait until both have initialized.
        yield return null;
        if (!BallARetroPlatform.IsDemoBuild || score == null || score.IsTutorialRun) yield break;
        ball = FindAnyObjectByType<YJ_Script_BallController>();
        if (ball == null) yield break;
        initialized = true;
        ball.ConfigureRunBallCredits(2);
        NotifyStateChanged();
    }

    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += ResetRun;
        KHS_Script_CameraManager.MonitorEvt += SetMonitorActive;
    }
    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= ResetRun;
        KHS_Script_CameraManager.MonitorEvt -= SetMonitorActive;
    }
    private void SetMonitorActive(bool active) => monitorActive = active;
    private void OnDestroy()
    {
        if (Current == this) { Current = null; Changed?.Invoke(); }
    }

    public bool CanChoose(int round) => IsEnabled && !score.HasEnded && round >= 1 && round <= 3
        && score.RoundRespone() == round && PicksTaken < 3 && lastPickedRound < round && PicksTaken == round - 1;

    // Reservation and effect application share one owner in ChoiceUI. A duplicate
    // button event cannot apply the upgrade or record its history a second time.
    public bool TryBeginPick(int round)
    {
        if (!CanChoose(round)) return false;
        lastPickedRound = round;
        PicksTaken++;
        NotifyStateChanged();
        return true;
    }

    public bool ApplyUpgrade(int choiceId)
    {
        if (!IsEnabled || score.HasEnded) return false;
        if (choiceId == CapacitorChoiceId && CapacitorLevel < 2) CapacitorLevel++;
        else if (choiceId == LastBallChoiceId && LastBallLevel < 2) LastBallLevel++;
        else return false;
        NotifyStateChanged();
        return true;
    }

    public void RecordBumperHit()
    {
        if (!IsEnabled || !score.IsPlaying || CapacitorLevel == 0 || Time.timeScale <= 0f) return;
        int next = Mathf.Min(MaxCharge, Charge + 100);
        if (next == Charge) return;
        Charge = next;
        NotifyStateChanged();
    }

    // Called only inside the score manager's accepted blackhole transaction.
    public int ConsumeCharge()
    {
        if (!IsEnabled || !score.IsPlaying) return 0;
        int result = Charge;
        Charge = 0;
        return result;
    }

    public int CalculateLastBallBonus(int awardedBase)
        => LastBallActive && awardedBase > 0
            ? Mathf.RoundToInt(awardedBase * (LastBallBonusPercent / 100f)) : 0;

    public void RecordAwarded(int chargePaid, int lastBallBonus)
    {
        ChargePaidTotal += Mathf.Max(0, chargePaid);
        LastBallBonusTotal += Mathf.Max(0, lastBallBonus);
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => Changed?.Invoke();

    private void ResetRun()
    {
        initialized = false;
        Charge = CapacitorLevel = LastBallLevel = ChargePaidTotal = LastBallBonusTotal = PicksTaken = lastPickedRound = 0;
        NotifyStateChanged();
    }
}
