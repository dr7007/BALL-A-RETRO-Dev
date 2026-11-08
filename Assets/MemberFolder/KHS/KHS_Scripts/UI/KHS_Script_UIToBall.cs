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
        ResolveReferences();
    }
    private void Start()
    {
        BallCountReset();
    }
    private void OnEnable()
    {
        KHS_Script_ScoreManager.UILateUpdate += BallOutUI;
        KHS_Script_ScoreManager.Next_Round_Init += BallCountInitReset;
        BallARetroDemoRun.Changed += BallInfoUpdate;
    }
    private void OnDisable()
    {
        KHS_Script_ScoreManager.UILateUpdate -= BallOutUI;
        KHS_Script_ScoreManager.Next_Round_Init -= BallCountInitReset;
        BallARetroDemoRun.Changed -= BallInfoUpdate;
    }

    private void BallCountReset()
    {
        if (ResolveReferences())
        {
            ballCount = ballCon.GetBallCount();
            BallCountUI.text = "" + ballCount;
        }
    }

    private void BallCountInitReset()
    {
        if (ResolveReferences())
        {
            ballCount = ballCon.BallCountInitResponse();
            BallCountUI.text = "" + ballCount;
        }
    }

    private void BallOutUI()
    {
        if (!ResolveReferences()) return;
        ballCount = ballCon.GetBallCount();
        BallCountUI.text = "" + ballCount;
    }

    public void BallInfoUpdate()
    {
        if (!ResolveReferences()) return;
        BallCountUI.text = "" + ballCon.GetBallCount();
    }

    // UI events can arrive from another component's Start before our Start runs.
    private bool ResolveReferences()
    {
        if (!BallCountUI) BallCountUI = GetComponent<TextMeshProUGUI>();
        if (!ballCon) ballCon = FindAnyObjectByType<YJ_Script_BallController>();
        return BallCountUI != null && ballCon != null;
    }
}
