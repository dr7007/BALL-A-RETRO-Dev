//using System;
//using UnityEngine;

//public class KHS_Script_ScoreManager : MonoBehaviour
//{
//    public static event Action OnGameOver;
//    public static event Action OnGameClear;

//    public static event Action<int> OnGameOverWithScore;
//    public static event Action<int> OnGameClearWithScore;

//    public static event Action<int> OnScoreGained; // 이펙트

//    [Header("스코어 정보")]
//    [Tooltip("현재 스코어 정보를 표시합니다")]
//    public int curScore = 0;
//    [Tooltip("목표 스코어 정보를 표시합니다")]
//    public int targetScore = 5000;

//    [Header("볼 관련 정보")]
//    [Tooltip("추후 점수 계산에 사용할 볼의 정보를 저장하기 위함")]
//    [SerializeField] private int numOfBounce = 0;

//    [Header("결과 연동")]
//    [SerializeField] private CJS_Script_GameOverUI gameOverUI;
//    [SerializeField] private CJS_Script_PinballRankingService rankingService;
//    [SerializeField] private string gameMode = "Classic";
//    [SerializeField] private int level = 1;

//    [Header("FX (Camera Shake)")]
//    [Tooltip("카메라넣는곳")]
//    [SerializeField] private CJS_Script_CameraShaker cameraShaker;
//    [Tooltip("카메라쉐이크기능쓸지말지")]
//    [SerializeField] private bool shakeOnScore = true;

//    void Start()
//    {
//        curScore = 0;
//        numOfBounce = 0;
//    }

//    private void OnEnable()
//    {
//        KHS_Script_ResetController.OnReset += ScoreReset;
//        KHS_Script_BallController.GameOverEvt += GameResultJudge;
//        KHS_Script_DumpManager.OnBallTrigger += BallTrigger;
//        KHS_Script_DumpManager.OnBallCollision += BallCollision;
//        KHS_Script_DumpManager.OnScore += AddScore;
//    }

//    private void OnDisable()
//    {
//        KHS_Script_ResetController.OnReset -= ScoreReset;
//        KHS_Script_BallController.GameOverEvt -= GameResultJudge;
//        KHS_Script_DumpManager.OnBallTrigger -= BallTrigger;
//        KHS_Script_DumpManager.OnBallCollision -= BallCollision;
//        KHS_Script_DumpManager.OnScore -= AddScore;
//    }

//    private void GameResultJudge()
//    {
//        Debug.LogError("게임종료 결과 판정중!!!");
//        Debug.LogWarning($"최종 스코어 : {curScore}");
//        if (curScore >= targetScore)
//            OnGameClear?.Invoke();
//        else
//            OnGameOver?.Invoke();

//        int finalScore = curScore;

//        // 점수 포함 이벤트 방송
//        if (curScore >= targetScore)
//            OnGameClearWithScore?.Invoke(finalScore);
//        else
//            OnGameOverWithScore?.Invoke(finalScore);

//        if (gameOverUI != null)
//        {
//            gameOverUI.Show(finalScore);
//        }
//        else if (rankingService != null)
//        {
//            rankingService.SubmitScore(finalScore, gameMode, level,
//                onDone: resp => Debug.Log($"[ScoreManager] Submit OK rank=#{resp.rank}"),
//                onFail: err => Debug.LogError($"[ScoreManager] Submit FAIL: {err}")
//            );
//        }

//        curScore = 0;
//        numOfBounce = 0;
//    }

//    private void FliperBallCollision()
//    {
//        Debug.LogWarning($"플리퍼 충돌! 튕긴 횟수 초기화! 이번에 튕긴횟수 {numOfBounce}");
//        numOfBounce = 0;
//    }

//    public void AddScore(int value)
//    {
//        // value는 '이번에 얻은 점수(델타)'
//        curScore += value;
//        Debug.LogWarning($"현재 스코어: {curScore} (+{value})");

//        // 델타 방송 (다른 시스템에서 필요 시 사용)
//        OnScoreGained?.Invoke(value);

//        // 카메라 쉐이크
//        if (shakeOnScore && cameraShaker != null && value > 0)
//            cameraShaker.OnScored(value);
//    }

//    public void MultiplyScore(int value)
//    {
//        // 곱셈이므로 '실제 증가분'을 계산해 쉐이크 강도에 반영
//        int before = curScore;
//        curScore *= value;
//        int delta = Mathf.Max(0, curScore - before);

//        Debug.LogWarning($"현재 스코어: {curScore} (*{value})");

//        OnScoreGained?.Invoke(delta);

//        if (shakeOnScore && cameraShaker != null && delta > 0)
//            cameraShaker.OnScored(delta);
//    }

//    private void BallTrigger(Collider _other)
//    {
//        numOfBounce++;
//        Debug.Log($"볼 튕긴(Trigger) 횟수 증가 : {numOfBounce}");
//    }

//    private void BallCollision(Collision collision)
//    {
//        numOfBounce++;
//        Debug.Log($"볼 튕긴(Collision) 횟수 증가 : {numOfBounce}");
//    }

//    private void ScoreReset()
//    {
//        Debug.LogWarning($"리셋 전 마지막 스코어 표기 : {curScore}");
//        curScore = 0;
//        Debug.LogWarning($"리셋 전 마지막 튕김 수 표기 : {numOfBounce}");
//        numOfBounce = 0;
//    }
//}
using System;
using UnityEngine;

public class KHS_Script_ScoreManager : MonoBehaviour
{
    public static event Action OnGameOver;
    public static event Action OnGameClear;

    public static event Action<int> OnGameOverWithScore;
    public static event Action<int> OnGameClearWithScore;

    // 이펙트용 이벤트
    public static event Action<int> OnScoreGained;                 // 점수 델타만
    public static event Action<int, Vector3> OnScoreGainedAt;      // 점수 + 월드위치

    [Header("스코어 정보")]
    [Tooltip("현재 스코어 정보를 표시합니다")]
    public int curScore = 0;
    [Tooltip("목표 스코어 정보를 표시합니다")]
    public int targetScore = 5000;

    [Header("볼 관련 정보")]
    [Tooltip("추후 점수 계산에 사용할 볼의 정보를 저장하기 위함")]
    [SerializeField] private int numOfBounce = 0;
    [Tooltip("득점 위치가 없을 때 대체로 사용할 볼 Transform(선택)")]
    [SerializeField] private Transform ballTransformFallback;

    [Header("결과 연동")]
    [SerializeField] private CJS_Script_GameOverUI gameOverUI;
    [SerializeField] private CJS_Script_PinballRankingService rankingService;
    [SerializeField] private string gameMode = "Classic";
    [SerializeField] private int level = 1;

    [Header("FX (Camera Shake)")]
    [Tooltip("카메라넣는곳")]
    [SerializeField] private KHS_Script_CameraManager camHolder;
    [SerializeField] private CJS_Script_CameraShaker cameraShaker;
    [Tooltip("카메라쉐이크기능쓸지말지")]
    [SerializeField] private bool shakeOnScore = true;

    void Start()
    {
        ChangingMainCam();
        curScore = 0;
        numOfBounce = 0;
    }

    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += ScoreReset;
        KHS_Script_BallController.GameOverEvt += GameResultJudge;
        KHS_Script_DumpManager.OnBallTrigger += BallTrigger;
        KHS_Script_DumpManager.OnBallCollision += BallCollision;
        KHS_Script_DumpManager.OnScore += AddScore; // 기존: 위치정보 없음
        // 위치를 알고 호출할 수 있으면 DumpManager에서 AddScoreAt(value, worldPos) 호출 권장
        KHS_Script_PortalController.portalEvt += ChangingSubCam;
        KHS_Script_PlincoFunction.ReturnPortalEvt += ChangingMainCam;
    }

    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= ScoreReset;
        KHS_Script_BallController.GameOverEvt -= GameResultJudge;
        KHS_Script_DumpManager.OnBallTrigger -= BallTrigger;
        KHS_Script_DumpManager.OnBallCollision -= BallCollision;
        KHS_Script_DumpManager.OnScore -= AddScore;

        KHS_Script_PortalController.portalEvt -= ChangingSubCam;
        KHS_Script_PlincoFunction.ReturnPortalEvt -= ChangingMainCam;
    }

    private void GameResultJudge()
    {
        Debug.LogError("게임종료 결과 판정중!!!");
        Debug.LogWarning($"최종 스코어 : {curScore}");
        if (curScore >= targetScore) OnGameClear?.Invoke();
        else OnGameOver?.Invoke();

        int finalScore = curScore;

        if (curScore >= targetScore) OnGameClearWithScore?.Invoke(finalScore);
        else OnGameOverWithScore?.Invoke(finalScore);

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

    // 기존 API (위치를 모르는 경우) ----------------------------------------
    public void AddScore(int value)
    {
        // value는 '이번에 얻은 점수(델타)'
        var pos = ballTransformFallback != null
            ? ballTransformFallback.position
            : (cameraShaker != null ? cameraShaker.transform.position : Vector3.zero);

        AddScoreAt(value, pos);
    }

    // 새 API (권장): 득점 지점(worldPos)까지 같이 전달 -----------------------
    public void AddScoreAt(int value, Vector3 worldPos)
    {
        curScore += value;
        Debug.LogWarning($"현재 스코어: {curScore} (+{value})");

        OnScoreGained?.Invoke(value);
        OnScoreGainedAt?.Invoke(value, worldPos);

        if (shakeOnScore && cameraShaker != null && value > 0)
            cameraShaker.OnScored(value);
    }

    public void MultiplyScore(int value)
    {
        int before = curScore;
        curScore *= value;
        int delta = Mathf.Max(0, curScore - before);

        Debug.LogWarning($"현재 스코어: {curScore} (*{value})");

        // 위치 정보가 없으므로 볼/카메라 기준으로 Fallback
        var pos = ballTransformFallback != null
            ? ballTransformFallback.position
            : (cameraShaker != null ? cameraShaker.transform.position : Vector3.zero);

        OnScoreGained?.Invoke(delta);
        OnScoreGainedAt?.Invoke(delta, pos);

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

    private void ChangingSubCam()
    {
        cameraShaker = camHolder.cameraGos[1].GetComponent<CJS_Script_CameraShaker>();
    }

    private void ChangingMainCam()
    {
        cameraShaker = camHolder.cameraGos[0].GetComponent<CJS_Script_CameraShaker>();
    }
}
