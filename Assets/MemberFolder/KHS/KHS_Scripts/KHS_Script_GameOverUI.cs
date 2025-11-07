using PSH;
using System;
using UnityEngine;

public class KHS_Script_GameOverUI : MonoBehaviour
{
    [SerializeField]
    private GameObject gameOverUIObj;
    [SerializeField]
    private GameObject gameClearUIObj;

    private Canvas gameUICanvas;

    private void Awake()
    {
        gameUICanvas = GetComponent<Canvas>();
    }
    private void Start()
    {
        gameUICanvas.enabled = false;
        gameOverUIObj.SetActive(false);
        gameClearUIObj.SetActive(false);
    }
    private void OnEnable()
    {
        PSH_Script_GameSceneDirector.NoIntroStartEvt += GameReset;
        KHS_Script_ScoreManager.OnGameOver += GameOver;
        KHS_Script_ScoreManager.OnGameClear += GameClear;
        PSH_Script_DialogueUI.DialogueEvt += DialoguePreProcessing;
    }


    private void OnDisable()
    {
        PSH_Script_GameSceneDirector.NoIntroStartEvt -= GameReset;
        KHS_Script_ScoreManager.OnGameOver -= GameOver;
        KHS_Script_ScoreManager.OnGameClear -= GameClear;
        PSH_Script_DialogueUI.DialogueEvt -= DialoguePreProcessing;
    }

    private void GameOver()
    {
        gameOverUIObj.SetActive(true);
    }
    private void GameClear()
    {
        gameClearUIObj.SetActive(true);
    }
    private void GameReset()
    {
        gameUICanvas.enabled = true;
    }

    private void DialoguePreProcessing(string _str)
    {
        if(_str == "Intro")
        {
            gameUICanvas.enabled = true;
        }
    }
}
