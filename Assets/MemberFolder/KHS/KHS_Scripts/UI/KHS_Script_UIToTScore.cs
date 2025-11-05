using System;
using TMPro;
using UnityEngine;

public class KHS_Script_UIToTScore : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI targetScoreUI = null;
    private int targetScore = 0;
    private KHS_Script_ScoreManager scoreManager = null;
    private bool isOver = false;

    private void OnEnable()
    {
        KHS_Script_ScoreManager.OnGameOver += GameOverUI;
        KHS_Script_ScoreManager.OnGameClear += GameOverUI;
    }
    private void OnDisable()
    {
        KHS_Script_ScoreManager.OnGameOver -= GameOverUI;
        KHS_Script_ScoreManager.OnGameClear -= GameOverUI;
    }


    void Start()
    {
        isOver = false;
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
        if (scoreManager != null)
        {
            targetScore = scoreManager.targetScore;
        }
        targetScoreUI = GetComponent<TextMeshProUGUI>();
        targetScoreUI.text = " / " + targetScore;
    }

    // Update is called once per frame
    void FixedUpdate()
    {

    }
    private void GameOverUI()
    {
        isOver = true;
    }
}
