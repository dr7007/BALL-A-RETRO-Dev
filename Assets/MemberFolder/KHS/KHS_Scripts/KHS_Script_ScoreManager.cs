//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

//public class KHS_Script_ScoreManager : MonoBehaviour
//{
//    public static event Action OnGameOver;
//    public static event Action OnGameClear;
//    public static event Action Round_Clear;
//    public static event Action Next_Round_Init;

//    public static event Action<int> OnGameOverWithScore;
//    public static event Action<int> OnGameClearWithScore;

//    // 이펙트용 이벤트
//    public static event Action<int> OnScoreGained;                 // 점수 델타만
//    public static event Action<int, Vector3> OnScoreGainedAt;      // 점수 + 월드위치

//    public static KHS_Script_ScoreManager Instance { get; private set; }

//    [Header("스코어 정보")]
//    [Tooltip("현재 스코어 정보를 표시합니다")]
//    public int curScore = 0;
//    [Tooltip("목표 스코어 정보를 표시합니다")]
//    public int targetScore = 5000;
//    [Tooltip("스코어의 기본 배수 값 - 로그라이크 선택지로 증강 가능")]
//    public float multiplier = 1.0f;
//    public int FinalUserScore = 0;

//    [Header("볼 관련 정보")]
//    [Tooltip("추후 점수 계산에 사용할 볼의 정보를 저장하기 위함")]
//    [SerializeField] private int numOfBounce = 0;
//    [Tooltip("득점 위치가 없을 때 대체로 사용할 볼 Transform(선택)")]
//    [SerializeField] private Transform ballTransformFallback;
//    [Tooltip("이번 라운드 동안 볼이 튕긴 최대 횟수")]
//    [SerializeField] private int maxBounce = 0;

//    [Header("Round Infomation")]
//    [Tooltip("목표 라운드 수")]
//    [SerializeField] private int goalRound = 0;
//    [SerializeField] private int currentRound = 0;
//    [SerializeField] private int[] currentgameScores;

//    [Header("결과 연동")]
//    [SerializeField] private CJS_Script_GameOverUI gameOverUI;
//    [SerializeField] private CJS_Script_PinballRankingService rankingService;
//    [SerializeField] private string gameMode = "Classic";
//    [SerializeField] private int level = 1;

//    [Header("FX (Camera Shake)")]
//    [Tooltip("카메라넣는곳")]
//    [SerializeField] private KHS_Script_CameraManager camHolder;
//    [SerializeField] private CJS_Script_CameraShaker cameraShaker;
//    [Tooltip("카메라쉐이크기능쓸지말지")]
//    [SerializeField] private bool shakeOnScore = true;

//    private void Awake()
//    {
//        if (Instance == null) Instance = this;
//        else Destroy(gameObject);
//    }

//    void Start()
//    {
//        for(int i = 0; i<goalRound; i++)
//        {
//            currentgameScores.SetValue(0, i);
//        }
//        ChangingMainCam();
//        curScore = 0;
//        numOfBounce = 0;
//    }

//    private void OnEnable()
//    {
//        KHS_Script_ResetController.OnReset += ScoreReset;
//        YJ_Script_BallController.GameOverEvt += GameResultJudge;
//        KHS_Script_DumpManager.OnBallTrigger += BallTrigger;
//        KHS_Script_DumpManager.OnBallCollision += BallCollision;
//        KHS_Script_DumpManager.OnScore += AddScore; // 기존: 위치정보 없음
//        // 위치를 알고 호출할 수 있으면 DumpManager에서 AddScoreAt(value, worldPos) 호출 권장
//        KHS_Script_PortalController.portalEvt += ChangingSubCam;
//        KHS_Script_PlincoFunction.ReturnPortalEvt += ChangingMainCam;
//        KHS_Script_FliperDumpManager.OnFliperCollision += FliperBallCollision;
//        Round_Clear += RoundClearAfter;
//    }

//    private void OnDisable()
//    {
//        KHS_Script_ResetController.OnReset -= ScoreReset;
//        YJ_Script_BallController.GameOverEvt -= GameResultJudge;
//        KHS_Script_DumpManager.OnBallTrigger -= BallTrigger;
//        KHS_Script_DumpManager.OnBallCollision -= BallCollision;
//        KHS_Script_DumpManager.OnScore -= AddScore;

//        KHS_Script_PortalController.portalEvt -= ChangingSubCam;
//        KHS_Script_PlincoFunction.ReturnPortalEvt -= ChangingMainCam;
//        KHS_Script_FliperDumpManager.OnFliperCollision -= FliperBallCollision;
//        Round_Clear -= RoundClearAfter;
//    }

//    private void GameResultJudge()
//    {
//        Debug.LogError("라운드종료 결과 판정중!!!");
//        Debug.LogWarning($"최종 스코어 : {curScore}");

//        int finalScore = curScore;

//        // ── 1) 목표 달성: 라운드 클리어만 알리고 종료
//        if (curScore >= targetScore)
//        {
//            Round_Clear?.Invoke();
//            return;
//        }

//        // ── 2) 게임오버: 이벤트 → 점수 표시 → (필요 시) 서버 제출
//        OnGameOver?.Invoke();
//        OnGameOverWithScore?.Invoke(finalScore);

//        if (gameOverUI != null)
//        {
//            // 점수 텍스트 즉시 갱신 후 패널 열기(Show 안에서 autoSubmitOnShow면 서버 전송)
//            gameOverUI.SetFinalScore(finalScore);
//            gameOverUI.Show(finalScore);
//        }
//        else if (rankingService != null)
//        {
//            // GameOverUI가 없을 때만 직접 제출(중복 제출 방지)
//            rankingService.SubmitScore(finalScore, gameMode, level,
//                onDone: resp => Debug.Log($"[ScoreManager] Submit OK rank=#{resp.rank}"),
//                onFail: err => Debug.LogError($"[ScoreManager] Submit FAIL: {err}")
//            );
//        }

//        // ── 3) 정리
//        curScore = 0;
//        numOfBounce = 0;
//    }


//    private void FliperBallCollision(Collision _collision)
//    {
//        Debug.LogWarning($"플리퍼 충돌! 튕긴 횟수 초기화! 이번에 튕긴횟수 {numOfBounce}");
//        if(numOfBounce >= maxBounce)
//            maxBounce = numOfBounce;
//        numOfBounce = 0;
//    }

//    // 기존 API (위치를 모르는 경우) ----------------------------------------
//    public void AddScore(int value)
//    {
//        // value는 '이번에 얻은 점수(델타)'
//        var pos = ballTransformFallback != null
//            ? ballTransformFallback.position
//            : (cameraShaker != null ? cameraShaker.transform.position : Vector3.zero);

//        AddScoreAt(Mathf.RoundToInt(value * multiplier), pos);
//    }

//    // 새 API (권장): 득점 지점(worldPos)까지 같이 전달 -----------------------
//    public void AddScoreAt(int value, Vector3 worldPos)
//    {
//        curScore += value;
//        Debug.LogWarning($"현재 스코어: {curScore} (+{value}*{multiplier})");

//        OnScoreGained?.Invoke(Mathf.RoundToInt(value * multiplier));
//        OnScoreGainedAt?.Invoke(Mathf.RoundToInt(value * multiplier), worldPos);

//        if (shakeOnScore && cameraShaker != null && Mathf.RoundToInt(value * multiplier) > 0)
//            cameraShaker.OnScored(Mathf.RoundToInt(value * multiplier));
//    }

//    public void MultiplyScore(int value)
//    {
//        int before = curScore;
//        curScore *= value;
//        int delta = Mathf.Max(0, curScore - before);

//        Debug.LogWarning($"현재 스코어: {curScore} (*{value})");

//        // 위치 정보가 없으므로 볼/카메라 기준으로 Fallback
//        var pos = ballTransformFallback != null
//            ? ballTransformFallback.position
//            : (cameraShaker != null ? cameraShaker.transform.position : Vector3.zero);

//        OnScoreGained?.Invoke(delta);
//        OnScoreGainedAt?.Invoke(delta, pos);

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

//    private void RoundClearAfter()
//    {
//        Debug.LogWarning("RoundClearAfter 진입");
//        if(currentRound < goalRound)
//        {
//            currentgameScores[currentRound-1] = curScore;
//            currentRound++;
//            targetScore += targetScore;
//            NextRoundInit();
//        }
//        else
//        {
//            int finalScore = curScore;

//            OnGameClear.Invoke();
//            if (curScore >= targetScore)
//            {
//                foreach(var score in currentgameScores)
//                {
//                    if (score > finalScore)
//                        finalScore = score;
//                }
//                FinalUserScore = finalScore;
//                OnGameClearWithScore?.Invoke(finalScore);
//            }

//            if (gameOverUI != null)
//            {
//                foreach (var score in currentgameScores)
//                {
//                    if (score > finalScore)
//                        finalScore = score;
//                }
//                gameOverUI.Show(finalScore);
//            }
//            else if (rankingService != null)
//            {
//                foreach (var score in currentgameScores)
//                {
//                    if (score > finalScore)
//                        finalScore = score;
//                }
//                rankingService.SubmitScore(finalScore, gameMode, level,
//                    onDone: resp => Debug.Log($"[ScoreManager] Submit OK rank=#{resp.rank}"),
//                    onFail: err => Debug.LogError($"[ScoreManager] Submit FAIL: {err}")
//                );
//            }

//            curScore = 0;
//            numOfBounce = 0;
//        }

//    }
//    private void NextRoundInit()
//    {
//        Debug.LogWarning("NextRoundInit 진입");
//        curScore = 0;
//        numOfBounce = 0;
//        ChangingMainCam();
//        Next_Round_Init.Invoke();
//    }

//    private void ChangingSubCam()
//    {
//        cameraShaker = camHolder.cameraGos[1].GetComponent<CJS_Script_CameraShaker>();
//    }

//    private void ChangingMainCam()
//    {
//        cameraShaker = camHolder.cameraGos[0].GetComponent<CJS_Script_CameraShaker>();
//    }

//    public int RoundRespone()
//    {
//        return currentRound;
//    }
//    public void RoundImagePhase()
//    {

//    }
//    public void HandleBallOut(YJ_Script_BallController ball)
//    {
//        Debug.Log("BallOut 감지됨 - 점수 및 라운드 상태 판단 중");

//        // 목표 점수 달성 → 즉시 라운드 클리어
//        if (curScore >= targetScore)
//        {
//            Debug.Log("목표점수 달성! Round_Clear 즉시 호출");
//            Round_Clear?.Invoke();
//            return;
//        }

//        // 목표 미달 → 볼 카운트 차감
//        int remain = ball.GetBallCount() - 1;
//        if (remain > 0)
//        {
//            Debug.Log($"목표점수 미달. 잔여 볼 {remain}개. 다음 볼로 진행");
//            ball.SetBallCount(remain); // 내부 BallCount 갱신
//            ball.SendMessage("KHS_BallReset", SendMessageOptions.DontRequireReceiver);
//        }
//        else
//        {
//            Debug.Log("볼 소진! Game Over 처리");
//            OnGameOver?.Invoke();
//            OnGameOverWithScore?.Invoke(curScore);
//        }
//    }
//}
using PSH;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KHS_Script_ScoreManager : MonoBehaviour
{
    public static event Action OnGameOver;
    public static event Action OnGameClear;
    public static event Action Round_Clear;
    public static event Action Next_Round_Init;
    public static event Action UILateUpdate;

    public static event Action<int> OnGameOverWithScore;
    public static event Action<int> OnGameClearWithScore;

    // 이펙트용 이벤트
    public static event Action<int> OnScoreGained;
    public static event Action<int, Vector3> OnScoreGainedAt;
    public static event Action<bool> PlungerDeathWait;

    public static KHS_Script_ScoreManager Instance { get; private set; }

    [Header("스코어 정보")]
    public int curScore = 0;
    public int targetScore = 0;
    public int[] targetScores = new int[3];
    public float multiplier = 1.0f;
    public int FinalUserScore = 0;

    [Header("볼 관련 정보")]
    [SerializeField] private int numOfBounce = 0;
    [SerializeField] private Transform ballTransformFallback;
    [SerializeField] private int maxBounce = 0;

    [Header("Round Infomation")]
    [SerializeField] private int goalRound = 0;
    [SerializeField] private int currentRound = 0;
    [SerializeField] private int[] currentgameScores;

    [Header("결과 연동")]
    [SerializeField] private CJS_Script_GameOverUI gameOverUI;                 // ← 인스펙터 연결
    [SerializeField] private CJS_Script_PinballRankingService rankingService;  // ← 인스펙터 연결
    [SerializeField] private string gameMode = "Classic";
    [SerializeField] private int level = 1;

    [Header("FX (Camera Shake)")]
    [SerializeField] private KHS_Script_CameraManager camHolder;
    [SerializeField] private CJS_Script_CameraShaker cameraShaker;
    [SerializeField] private bool shakeOnScore = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 씬에서 빠뜨렸을 때 자동 연결(안전장치)
        if (gameOverUI == null) gameOverUI = FindObjectOfType<CJS_Script_GameOverUI>(true);
        if (rankingService == null) rankingService = FindObjectOfType<CJS_Script_PinballRankingService>(true);
    }

    void Start()
    {
        for (int i = 0; i < goalRound; i++) currentgameScores.SetValue(0, i);
        ChangingMainCam();
        curScore = 0;
        numOfBounce = 0;
        targetScore = targetScores[0];
    }

    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += ScoreReset;
        YJ_Script_BallController.GameOverEvt += GameResultJudge;
        KHS_Script_DumpManager.OnBallTrigger += BallTrigger;
        KHS_Script_DumpManager.OnBallCollision += BallCollision;
        KHS_Script_DumpManager.OnScore += AddScore;
        KHS_Script_PortalController.portalEvt += ChangingSubCam;
        KHS_Script_PlincoFunction.ReturnPortalEvt += ChangingMainCam;
        KHS_Script_FliperDumpManager.OnFliperCollision += FliperBallCollision;
        Round_Clear += RoundClearAfter;
        PSH_Script_SceneLoader.OnSceneLoadStart += TargetScoreInit;
    }

    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= ScoreReset;
        YJ_Script_BallController.GameOverEvt -= GameResultJudge;
        KHS_Script_DumpManager.OnBallTrigger -= BallTrigger;
        KHS_Script_DumpManager.OnBallCollision -= BallCollision;
        KHS_Script_DumpManager.OnScore -= AddScore;
        KHS_Script_PortalController.portalEvt -= ChangingSubCam;
        KHS_Script_PlincoFunction.ReturnPortalEvt -= ChangingMainCam;
        KHS_Script_FliperDumpManager.OnFliperCollision -= FliperBallCollision;
        Round_Clear -= RoundClearAfter;
        PSH_Script_SceneLoader.OnSceneLoadStart -= TargetScoreInit;
    }

    private void TargetScoreInit()
    {
        targetScore = targetScores[0];
        UILateUpdate.Invoke();
    }
    private void GameResultJudge()
    {
        Debug.LogError("라운드종료 결과 판정중!!!");

        Debug.LogWarning($"최종 스코어 : {curScore}");

        int finalScore = curScore;

        // 1) 목표 달성 시 라운드 클리어 후 종료
        if (curScore >= targetScore)
        {
            Round_Clear?.Invoke();
            return;
        }

        foreach (var score in currentgameScores)
            if (score > finalScore) finalScore = score;

        FinalUserScore = finalScore;

        // 2) 게임오버 흐름
        OnGameOver?.Invoke();
        OnGameOverWithScore?.Invoke(finalScore);

        // UI 표시(숫자 즉시 반영 + 패널 열기)
        if (gameOverUI != null)
        {
            gameOverUI.SetFinalScore(finalScore);
            gameOverUI.Show(finalScore); // autoSubmitOnShow가 true면 여기서 서버 제출
        }

        // UI가 없거나 auto-submit이 꺼졌으면 직접 제출
        if (rankingService != null && (gameOverUI == null || !gameOverUI.autoSubmitOnShow))
        {
            rankingService.SubmitScore(finalScore, gameMode, level,
                onDone: resp => Debug.Log($"[ScoreManager] Submit OK rank=#{resp.rank}"),
                onFail: err => Debug.LogError($"[ScoreManager] Submit FAIL: {err}")
            );
        }

        // 3) 정리
        curScore = 0;
        numOfBounce = 0;
    }

    private void FliperBallCollision(Collision _collision)
    {
        if (numOfBounce >= maxBounce) maxBounce = numOfBounce;
        numOfBounce = 0;
    }

    // 기존 API (위치를 모르는 경우)
    public void AddScore(int value)
    {
        var pos = ballTransformFallback != null
            ? ballTransformFallback.position
            : (cameraShaker != null ? cameraShaker.transform.position : Vector3.zero);

        AddScoreAt(value, pos);
    }

    // 새 API (권장)
    public void AddScoreAt(int value, Vector3 worldPos)
    {
        int finalScore = Mathf.RoundToInt(value * multiplier);

        curScore += finalScore;

        OnScoreGained?.Invoke(finalScore);
        OnScoreGainedAt?.Invoke(finalScore, worldPos);

        if (shakeOnScore && cameraShaker != null && finalScore > 0)
            cameraShaker.OnScored(finalScore);
    }

    public void MultiplyScore(int value)
    {
        int before = curScore;
        curScore *= value;
        int delta = Mathf.Max(0, curScore - before);

        var pos = ballTransformFallback != null
            ? ballTransformFallback.position
            : (cameraShaker != null ? cameraShaker.transform.position : Vector3.zero);

        OnScoreGained?.Invoke(delta);
        OnScoreGainedAt?.Invoke(delta, pos);

        if (shakeOnScore && cameraShaker != null && delta > 0)
            cameraShaker.OnScored(delta);
    }

    private void BallTrigger(Collider _other) { numOfBounce++; }
    private void BallCollision(Collision collision) { numOfBounce++; }

    private void ScoreReset()
    {
        curScore = 0;
        numOfBounce = 0;
    }

    private void RoundClearAfter()
    {
        if (currentRound < goalRound)
        {
            currentgameScores[currentRound - 1] = curScore;
            targetScore = targetScores[currentRound++];
            StartCoroutine(NextRoundInit());
        }
        else
        {
            int finalScore = curScore;

            OnGameClear?.Invoke();
            if (curScore >= targetScore)
            {
                foreach (var score in currentgameScores)
                    if (score > finalScore) finalScore = score;

                FinalUserScore = finalScore;
                OnGameClearWithScore?.Invoke(finalScore);
            }

            if (gameOverUI != null)
            {
                foreach (var score in currentgameScores)
                    if (score > finalScore) finalScore = score;

                gameOverUI.SetFinalScore(finalScore);
                gameOverUI.Show(finalScore);
            }
            else if (rankingService != null)
            {
                foreach (var score in currentgameScores)
                    if (score > finalScore) finalScore = score;

                rankingService.SubmitScore(finalScore, gameMode, level,
                    onDone: resp => Debug.Log($"[ScoreManager] Submit OK rank=#{resp.rank}"),
                    onFail: err => Debug.LogError($"[ScoreManager] Submit FAIL: {err}")
                );
            }

            curScore = 0;
            numOfBounce = 0;
        }
    }

    private IEnumerator NextRoundInit()
    {
        PlungerDeathWait?.Invoke(false);
        yield return new WaitForSeconds(1.0f);
        curScore = 0;
        numOfBounce = 0;
        ChangingMainCam();
        Next_Round_Init?.Invoke();
    }

    private void ChangingSubCam(int _idx)
    {
        cameraShaker = camHolder.cameraGos[_idx].GetComponent<CJS_Script_CameraShaker>();
    }

    private void ChangingMainCam()
    {
        cameraShaker = camHolder.cameraGos[0].GetComponent<CJS_Script_CameraShaker>();
    }

    public int RoundRespone() => currentRound;
    public void RoundImagePhase() { }

    public void HandleBallOut(YJ_Script_BallController ball)
    {
        Debug.Log("BallOut 감지됨 - 점수 및 라운드 상태 판단 중");

        if (curScore >= targetScore)
        {
            Debug.Log("목표점수 달성! Round_Clear 즉시 호출");
            Round_Clear?.Invoke();
            return;
        }

        int remain = ball.GetBallCount() - 1;
        if (remain > 0)
        {
            Debug.Log($"목표점수 미달. 잔여 볼 {remain}개. 다음 볼로 진행");
            ball.SetBallCount(remain);
            ball.SendMessage("KHS_BallReset", SendMessageOptions.DontRequireReceiver);
            UILateUpdate.Invoke();
        }
        else
        {
            Debug.Log("볼 소진! Game Over 처리 → GameResultJudge 직접 호출");  // 추가 로그
            GameResultJudge();                                              // 핵심: 직접 호출
                                                                            // (이벤트는 선택) OnGameOver?.Invoke(); OnGameOverWithScore?.Invoke(curScore);
        }
    }

    public int ResponceFinal()
    {
        return currentRound;
    }
}

