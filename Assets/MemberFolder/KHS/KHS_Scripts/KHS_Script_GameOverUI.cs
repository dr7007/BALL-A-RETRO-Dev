using System;
using UnityEngine;

public class KHS_Script_GameOverUI : MonoBehaviour
{
    [SerializeField]
    private GameObject gameOverUIObj;
    private void Start()
    {
        gameOverUIObj.SetActive(false);
    }
    private void OnEnable()
    {
        KHS_Script_BallController.GameOverEvt += GameOver;
    }
    private void OnDisable()
    {
        KHS_Script_BallController.GameOverEvt -= GameOver;
    }

    private void GameOver()
    {
        gameOverUIObj.SetActive(true);
    }
}
