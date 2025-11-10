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
    private bool isReset = false;
    private void Awake()
    {
        gameUICanvas = GetComponent<Canvas>();
    }
    private void Start()
    {
        GameReset();
    }
    private void OnEnable()
    {
        PSH_Script_GameSceneDirector.NoIntroStartEvt += NoIntroReset;
        KHS_Script_ScoreManager.OnGameOver += GameOver;
        KHS_Script_ScoreManager.OnGameClear += GameClear;
        PSH_Script_DialogueUI.DialogueEvt += DialoguePreProcessing;
    }


    private void OnDisable()
    {
        PSH_Script_GameSceneDirector.NoIntroStartEvt -= NoIntroReset;
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
    private void NoIntroReset()
    {
        Debug.Log("NoIntroReset 이벤트로 인해 동작");
        isReset = true;
        GameReset();
    }

    private void GameReset()
    {
        if (!isReset)
        {
            gameUICanvas.enabled = false;
            gameOverUIObj.SetActive(false);
            gameClearUIObj.SetActive(false);
        }
        else
        {
            gameUICanvas.enabled = true;
            gameOverUIObj.SetActive(false);
            gameClearUIObj.SetActive(false);
        }
    }

    private void DialoguePreProcessing(string _str)
    {
        if(_str == "Intro")
        {
            gameUICanvas.enabled = true;
        }
    }
}
