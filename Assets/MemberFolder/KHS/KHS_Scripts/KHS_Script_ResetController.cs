using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class KHS_Script_ResetController : MonoBehaviour
{
    public static event Action OnGameStart;
    public static event Action OnReset;
    public string gameSceneName;
    public string lobbySceneName;

    private bool isClear = false;

    private void Start()
    {
        isClear = false;
    }

    void Update()
    {
        if(isClear)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                GameResetFunc();
            }
            else if (Input.anyKeyDown)
            {
                GameGoToLobbyFunc();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                GameResetFunc();
            }
        }
    }

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

    private void GameOver()
    {
        isClear = false;

    }

    private void GameClear()
    {
        isClear = true;
    }

    public void GameResetFunc()
    {
        Time.timeScale = 1f;
        OnReset.Invoke();
        Debug.LogError("Reset!");
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void GameGoToLobbyFunc()
    {
        Time.timeScale = 1f;
        OnReset.Invoke();
        Debug.LogError("Go to Lobby");
        SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }
}
