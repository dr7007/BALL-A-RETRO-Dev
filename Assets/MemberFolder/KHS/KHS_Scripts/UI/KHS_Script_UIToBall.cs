using System;
using TMPro;
using UnityEngine;

public class KHS_Script_UIToBall : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI BallCountUI = null;
    [SerializeField]
    private YJ_Script_BallController ballCon = null;
    private int ballCount = 0;

    private void Awake()
    {
        ballCon = FindAnyObjectByType<YJ_Script_BallController>();
    }
    private void Start()
    {
        BallCountUI = GetComponent<TextMeshProUGUI>();
        BallCountReset();
    }
    private void OnEnable()
    {
        KHS_Script_ScoreManager.UILateUpdate += BallOutUI;
        KHS_Script_ScoreManager.Next_Round_Init += BallCountInitReset;
    }
    private void OnDisable()
    {
        KHS_Script_ScoreManager.UILateUpdate -= BallOutUI;
        KHS_Script_ScoreManager.Next_Round_Init -= BallCountInitReset;
    }

    private void BallCountReset()
    {
        if (ballCon != null)
        {
            ballCount = ballCon.GetBallCount();
            BallCountUI.text = "" + ballCount;
        }
    }

    private void BallCountInitReset()
    {
        if (ballCon != null)
        {
            ballCount = ballCon.BallCountInitResponse();
            BallCountUI.text = "" + ballCount;
        }
    }

    private void BallOutUI()
    {
        ballCount = ballCon.GetBallCount();
        BallCountUI.text = "" + ballCount;
    }

    public void BallInfoUpdate()
    {
        BallCountUI.text = "" + ballCon.GetBallCount();
    }
}
