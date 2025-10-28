using System;
using UnityEngine;

public class KHS_Script_ScoreManager : MonoBehaviour
{
    public static event Action OnGameOver;
    public static event Action OnGameClear;

    [Header("스코어 정보")]
    [Tooltip("현재 스코어 정보를 표시합니다")]
    public int curScore = 0;
    [Tooltip("목표 스코어 정보를 표시합니다")]
    public int targetScore = 5000;

    [Header("볼 관련 정보")]
    [Tooltip("추후 점수 계산에 사용할 볼의 정보를 저장하기 위함")]
    [SerializeField]
    private int numOfBounce = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        curScore = 0;
        numOfBounce = 0;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += ScoreReset;
        KHS_Script_BallController.GameOverEvt += GameResultJudge;
        KHS_Script_DumpManager.OnBallTrigger += BallTrigger;
        KHS_Script_DumpManager.OnBallCollision += BallCollision;
        KHS_Script_DumpManager.OnScore += AddScore;
    }


    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= ScoreReset;
        KHS_Script_BallController.GameOverEvt -= GameResultJudge;
        KHS_Script_DumpManager.OnBallTrigger -= BallTrigger;
        KHS_Script_DumpManager.OnBallCollision -= BallCollision;
        KHS_Script_DumpManager.OnScore -= AddScore;
    }

    private void GameResultJudge()
    {
        Debug.LogError("게임종료 결과 판정중!!!");
        Debug.LogWarning($"최종 스코어 : {curScore}");
        if (curScore >= targetScore)
            OnGameClear.Invoke();
        else
            OnGameOver.Invoke();
        curScore = 0;
        numOfBounce = 0;
    }

    private void FliperBallCollision()
    {
        Debug.LogWarning($"플리퍼 충돌! 튕긴 횟수 초기화! 이번에 튕긴횟수 {numOfBounce}");
        numOfBounce = 0;
    }

    public void AddScore(int value)
    {
        curScore += value;
        Debug.LogWarning($"현재 스코어: {curScore} (+{value})");
    }
    public void MultiplyScore(int value)
    {
        curScore *= value;
        Debug.LogWarning($"현재 스코어: {curScore} (*{value})");
    }

    private void BallTrigger(Collider _other)
    {
        numOfBounce++;
        Debug.Log($"볼 튕긴(Trigger) 횟수 증가 : {numOfBounce}");
    }
    private void BallCollision(Collision collision)
    {
        numOfBounce++;
        Debug.Log($"볼 튕긴(Collision) 횟수 증가 : {numOfBounce}");
    }

    private void ScoreReset()
    {
        Debug.LogWarning($"리셋 전 마지막 스코어 표기 : {curScore}");
        curScore = 0;
        Debug.LogWarning($"리셋 전 마지막 튕김 수 표기 : {numOfBounce}");
        numOfBounce = 0;
    }
}
