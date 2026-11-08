// Only copied into the isolated UI fixture. Demo rules have their own real-model tests.
using System;
public sealed class BallARetroDemoRun
{
    public static BallARetroDemoRun Current { get; set; }
    public static event Action Changed;
    public int Charge, MaxCharge, CapacitorLevel, LastBallLevel;
    public int LastBallBonusPercent, ChargePaidTotal, LastBallBonusTotal, PicksTaken;
    public bool IsEnabled, LastBallActive;
    public static void Notify() => Changed?.Invoke();
}

// The UI observes this production camera event without controlling the camera.
public static class KHS_Script_CameraManager
{
    public static event Action<bool> MonitorEvt;
    public static void SetMonitor(bool active) => MonitorEvt?.Invoke(active);
}
