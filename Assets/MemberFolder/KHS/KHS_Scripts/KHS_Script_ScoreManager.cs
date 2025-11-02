using System;
using UnityEngine;

public class KHS_Script_ScoreManager : MonoBehaviour
{
    public static event Action OnGameOver;
    public static event Action OnGameClear;

    public static event Action<int> OnGameOverWithScore;
    public static event Action<int> OnGameClearWithScore;

    public static event Action<int> OnScoreGained; // 이펙트

    [Header("스코어 정보")]
    [Tooltip("현재 스코어 정보를 표시합니다")]
    public int curScore = 0;
    [Tooltip("목표 스코어 정보를 표시합니다")]
    public int targetScore = 5000;

    [Header("볼 관련 정보")]
    [Tooltip("추후 점수 계산에 사용할 볼의 정보를 저장하기 위함")]
    [SerializeField] private int numOfBounce = 0;

    [Header("결과 연동")]
    [SerializeField] private CJS_Script_GameOverUI gameOverUI;
    [SerializeField] private CJS_Script_PinballRankingService rankingService;
    [SerializeField] private string gameMode = "Classic";
    [SerializeField] private int level = 1;

    [Header("FX (Camera Shake)")]
    [Tooltip("카메라넣는곳")]
    [SerializeField] private CJS_Script_CameraShaker cameraShaker;
    [Tooltip("카메라쉐이크기능쓸지말지")]
    [SerializeField] private bool shakeOnScore = true;

    void Start()
    {
        curScore = 0;
        numOfBounce = 0;
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
            OnGameClear?.Invoke();
        else
            OnGameOver?.Invoke();

        int finalScore = curScore;

        // 점수 포함 이벤트 방송
        if (curScore >= targetScore)
            OnGameClearWithScore?.Invoke(finalScore);
        else
            OnGameOverWithScore?.Invoke(finalScore);

        if (gameOverUI != null)
        {
            gameOverUI.Show(finalScore);
        }
        else if (rankingService != null)
        {
            rankingService.SubmitScore(finalScore, gameMode, level,
                onDone: resp => Debug.Log($"[ScoreManager] Submit OK rank=#{resp.rank}"),
                onFail: err => Debug.LogError($"[ScoreManager] Submit FAIL: {err}")
            );
        }

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
        // value는 '이번에 얻은 점수(델타)'
        curScore += value;
        Debug.LogWarning($"현재 스코어: {curScore} (+{value})");

        // 델타 방송 (다른 시스템에서 필요 시 사용)
        OnScoreGained?.Invoke(value);

        // 카메라 쉐이크
        if (shakeOnScore && cameraShaker != null && value > 0)
            cameraShaker.OnScored(value);
    }

    public void MultiplyScore(int value)
    {
        // 곱셈이므로 '실제 증가분'을 계산해 쉐이크 강도에 반영
        int before = curScore;
        curScore *= value;
        int delta = Mathf.Max(0, curScore - before);

        Debug.LogWarning($"현재 스코어: {curScore} (*{value})");

        OnScoreGained?.Invoke(delta);

        if (shakeOnScore && cameraShaker != null && delta > 0)
            cameraShaker.OnScored(delta);
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
