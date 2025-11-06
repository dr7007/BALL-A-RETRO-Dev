using System;
using TMPro;
using UnityEngine;

public class KHS_Script_UIToBall : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI BallCountUI = null;
    [SerializeField]
    private KHS_Script_BallController ballCon = null;
    private int ballCount = 0;

    private void Awake()
    {
        ballCon = FindAnyObjectByType<KHS_Script_BallController>();
    }
    private void Start()
    {
        BallCountUI = GetComponent<TextMeshProUGUI>();
        if (ballCon != null)
        {
            ballCount = ballCon.BallCountResponse();
            BallCountUI.text = "" + ballCount;
        }
    }
    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += BallOutUI;
    }
    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= BallOutUI;
    }

    private void BallOutUI()
    {
        --ballCount;
        BallCountUI.text = "" + ballCount;
    }
}
