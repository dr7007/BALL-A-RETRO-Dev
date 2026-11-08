// Only the isolated test project compiles these event/scene-loading stand-ins.
// Settings, dialogue, cursor state and pointer events use production behavior.
using System;
using UnityEngine;

public class KHS_Script_ScoreManager : MonoBehaviour
{
    public bool HasEnded { get; set; }
    public bool IsPlaying { get; set; } = true;
    public bool IsTutorialRun { get; set; }
    public static KHS_Script_ScoreManager Instance => FindAnyObjectByType<KHS_Script_ScoreManager>();
    public int RoundRespone() => 1;
    public static event Action OnGameClear, OnGameOver;
    public static void EndGame(string ending)
    {
        if (ending == "Ending1") OnGameClear?.Invoke();
        else OnGameOver?.Invoke();
    }
}

public class KHS_Script_ResetController : MonoBehaviour
{
    public static event Action OnReset;
    public static void ResetRun() => OnReset?.Invoke();
    public int LobbyRequests { get; private set; }
    public void GameGoToLobbyFunc() { LobbyRequests++; }
}
