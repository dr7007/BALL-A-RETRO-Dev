// Only the isolated test project compiles these event/scene-loading stand-ins.
// Settings, dialogue, cursor state and pointer events use production behavior.
using System;
using UnityEngine;

public static class KHS_Script_ScoreManager
{
    public static event Action OnGameClear, OnGameOver;
    public static void EndGame(string ending)
    {
        if (ending == "Ending1") OnGameClear?.Invoke();
        else OnGameOver?.Invoke();
    }
}

public class KHS_Script_ResetController : MonoBehaviour
{
    public void GameGoToLobbyFunc() { }
}
