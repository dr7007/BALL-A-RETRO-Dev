using System;
using TMPro;
using UnityEngine;

public class KHS_Script_UIToBall : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI currentBallCountUI = null;
    private int ballCount = 0;
    private bool isOver = false;

    private void OnEnable()
    {
        KHS_Script_BallController.GameOverEvt += GameOverUI;
    }
    private void OnDisable()
    {
        KHS_Script_BallController.GameOverEvt -= GameOverUI;
    }


    void Start()
    {
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isOver)
        {
            // To Do : 현재는 구현의 편의성을 위해 FixedUpdate에서 갱신하지만, 추후에는 점수 업데이트 이벤트나 점수 변화 이벤트에 따라 갱신하도록 최적화 필요
        }
    }
    private void GameOverUI()
    {
        isOver = true;
    }
}
