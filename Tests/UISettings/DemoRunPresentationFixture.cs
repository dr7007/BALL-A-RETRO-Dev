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
