// Isolated tests: the production roulette owns its coroutine and render/collider
// state. Scoring and ball-out are controlled collaborators for pause/reset cases.
using System;
using UnityEngine;

public class KHS_Script_ScoreManager : MonoBehaviour
{
    public int Calls, Total;
    public Action AfterScore;
    public void AddScore(int value)
    {
        Calls++;
        Total += value;
        AfterScore?.Invoke();
    }
}

public static class KHS_Script_BallOutController
{
    public static event Action BallOutEvt;
    public static void Drain() => BallOutEvt?.Invoke();
}

public class YJ_Script_BallController : MonoBehaviour
{
    public int Releases;
    public Vector3 LastReleaseVelocity;
    public bool LastReleaseWasRoulette;
    public void ReleaseForFalling(Vector3 velocity, bool isRoulette)
    {
        Releases++;
        LastReleaseVelocity = velocity;
        LastReleaseWasRoulette = isRoulette;
    }
}
