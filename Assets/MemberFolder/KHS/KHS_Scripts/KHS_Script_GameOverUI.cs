using System;
using UnityEngine;

public class KHS_Script_GameOverUI : MonoBehaviour
{
    [SerializeField]
    private GameObject gameOverUIObj;
    [SerializeField]
    private GameObject gameClearUIObj;

    private void Start()
    {
        gameOverUIObj.SetActive(false);
        gameClearUIObj.SetActive(false);
    }
    private void OnEnable()
    {
        KHS_Script_ScoreManager.OnGameOver += GameOver;
        KHS_Script_ScoreManager.OnGameClear += GameClear;
    }


    private void OnDisable()
    {
        KHS_Script_ScoreManager.OnGameOver -= GameOver;
        KHS_Script_ScoreManager.OnGameClear -= GameClear;
    }

    private void GameOver()
    {
        gameOverUIObj.SetActive(true);
    }
    private void GameClear()
    {
        gameClearUIObj.SetActive(true);
    }
}
