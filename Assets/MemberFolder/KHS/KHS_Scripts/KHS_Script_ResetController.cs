using PSH;
using System;
using UnityEngine;

public class KHS_Script_ResetController : MonoBehaviour
{
    public static event Action OnGameStart;
    public static event Action OnReset;
    public string gameSceneName;
    public string lobbySceneName;

    private bool isClear = false;

    private void OnEnable()
    {
        KHS_Script_ScoreManager.OnGameClear += GameClear;
        KHS_Script_ScoreManager.OnGameOver += GameOver;
    }

    private void OnDisable()
    {
        KHS_Script_ScoreManager.OnGameClear -= GameClear;
        KHS_Script_ScoreManager.OnGameOver -= GameOver;
    }

    private void GameOver() { isClear = false; }
    private void GameClear() { isClear = true; }

    public void GameResetFunc()
    {
        Time.timeScale = 1f;

        CJS_Script_ChoiceState.I?.ResetForNewRun();

        OnReset?.Invoke();
        Debug.Log("[Reset] Reload GameScene");
        PSH_Script_SceneLoader.Instance.LoadSceneAsyncByName(gameSceneName, false);
    }

    public void GameGoToLobbyFunc()
    {
        Time.timeScale = 1f;

        CJS_Script_ChoiceState.I?.ResetForNewRun();

        OnReset?.Invoke();
        Debug.Log("[Reset] Go Lobby");
        PSH_Script_GameSceneDirector.ResetIntroFlag();
        PSH_Script_SceneLoader.Instance.LoadSceneAsyncByName(lobbySceneName, false);
    }
}
