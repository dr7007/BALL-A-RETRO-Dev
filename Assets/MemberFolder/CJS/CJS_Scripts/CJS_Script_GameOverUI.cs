using System;
using System.Text;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CJS_Script_GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    public CJS_Script_PinballRankingService service;
    public TMP_Text textFinalScore;
    public TMP_Text textResult;

    [Header("Leaderboard (choose one)")]
    public TMP_Text textLeaderboard;                    // 텍스트 방식
    public CJS_Script_LeaderboardList leaderboardList;  // 리스트 프리팹 방식

    [Header("Panels")]
    public GameObject panelGameEnd;   // 이 스크립트가 붙은 패널이면 자기 자신 할당
    public GameObject panelRank;      // Top10 패널(비활성 시작 권장)

    [Header("Options")]
    public string gameMode = "Classic";
    public int level = 1;
    public bool autoSubmitOnShow = true;

    [Tooltip("랭크 패널을 열 때 서버에서 항상 최신 Top10을 받아옵니다.")]
    public bool preferFreshRankOnOpen = true;

    [Tooltip("랭크 패널이 열린 동안 주기적으로 새로고침(초). 0이면 끔.")]
    public float autoRefreshIntervalOnRankOpen = 0f;

    [Tooltip("서비스가 없으면 런타임에 자동 생성합니다(개발 편의용).")]
    public bool autoCreateServiceIfMissing = false;

    private string statusKey = "Rank_Ready";
    private object[] statusArguments = Array.Empty<object>();
    private int _finalScore;
    private SubmitResp _lastSubmit;
    private bool _rankPanelOpen;
    private float _refreshElapsed;
    private PSH_Script_WorldRankingUI worldRanking;
    private BallARetroDemoPresentation demoPresentation;

    // ─────────────────────────────────────────────────────────────────────────
    #region Lifecycle & Wiring

    void Awake()
    {
        TryWireService();
        EnsureDemoPresentation();

        if (panelGameEnd == null) panelGameEnd = gameObject;

        if (service == null)
            Debug.LogError("[GameOverUI] RankingService not found in scene.");
    }

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += RefreshLanguage;
        RefreshLanguage(null);
        // 씬이 바뀔 때마다 서비스 재확보 (중복 파괴 타이밍 대비)
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        TryWireService();
    }

    void Start()
    {
        TryWireService();
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= RefreshLanguage;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void SetStatus(string key, params object[] arguments)
    {
        statusKey = key;
        statusArguments = arguments;
        RefreshLanguage(null);
    }
    private void RefreshLanguage(Locale _)
    {
        if (textResult) textResult.text = BallARetroPlatform.UsesSteamLeaderboards && !BallARetroPlatform.IsDemoBuild
            ? PSH_Script_Localization.Text(statusKey, statusArguments) : string.Empty;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        TryWireService();
    }

    private void TryWireService()
    {
        // 파괴된 참조(or 미할당)이면 재확보
        if (!service)
        {
            // 싱글톤 우선
            var instProp = GetInstanceOrNull();
            if (instProp != null)
            {
                service = instProp;
            }
            else
            {
                // 씬 검색(비활성 포함)
                service = FindObjectOfType<CJS_Script_PinballRankingService>(includeInactive: true);
            }
        }

        // 여전히 없고 자동 생성 옵션이 켜져 있으면 부트스트랩
        if (!service && autoCreateServiceIfMissing)
        {
            var go = new GameObject("RankingService_Auto");
            service = go.AddComponent<CJS_Script_PinballRankingService>();
            DontDestroyOnLoad(go);
            Debug.LogWarning("[GameOverUI] RankingService auto-created for convenience.");
        }
    }

    // 리플렉션 없이도 쓸 수 있도록 Instance getter 호출 래퍼
    private CJS_Script_PinballRankingService GetInstanceOrNull()
    {
        try
        {
            // 프로젝트에 public static CJS_Script_PinballRankingService Instance {get;} 가 있어야 함
            return CJS_Script_PinballRankingService.Instance;
        }
        catch
        {
            return null;
        }
    }

    #endregion
    // ─────────────────────────────────────────────────────────────────────────

    void Update()
    {
        // 랭크 패널 자동 새로고침(선택)
        if (_rankPanelOpen && autoRefreshIntervalOnRankOpen > 0f && service != null)
        {
            _refreshElapsed += Time.unscaledDeltaTime;
            if (_refreshElapsed >= autoRefreshIntervalOnRankOpen)
            {
                _refreshElapsed = 0f;
                FetchFreshLeaderboardAndDraw();
            }
        }
    }

    /// 외부에서 점수만 먼저 반영하고 싶을 때 사용
    public void SetFinalScore(int score)
    {
        _finalScore = score;
        if (textFinalScore) textFinalScore.text = score.ToString("N0");
    }

    /// 게임 종료 시 호출
    public void Show(int finalScore)
    {
        _finalScore = finalScore;
        EnsureDemoPresentation();

        if (textFinalScore) textFinalScore.text = _finalScore.ToString("N0");
        SetStatus("Rank_Ready");

        Debug.Log($"[GameOverUI.Show] finalScore={_finalScore} autoSubmit={autoSubmitOnShow}");
        if (autoSubmitOnShow) OnClickSubmit();
    }

    public void OnClickSubmit()
    {
        TryWireService();
        if (service == null) { Debug.LogError("[GameOverUI] service is null"); return; }

        SetStatus("Rank_Saving");

        service.SubmitScore(_finalScore, gameMode, level,
            onDone: resp =>
            {
                if (!this) return;
                _lastSubmit = resp;
                if (demoPresentation != null) demoPresentation.SetSubmissionState(resp.local_only);

                if (BallARetroPlatform.IsDemoBuild && resp.local_only) SetStatus("Demo_SavedLocally");
                else SetStatus("Rank_Saved", resp.your_score, resp.rank);

                // 1) 낙관적 업데이트: 서버가 돌려준 스냅샷 먼저 표시
                RefreshLeaderboardUI(BallARetroPlatform.IsDemoBuild && resp.local_only ? Array.Empty<ScoreRow>() : resp.top10);

                // 2) 확정 동기화: 방금 저장된 내용이 Top10에 반영됐는지 서버에서 "다시" 받아서 갱신
                FetchFreshLeaderboardAndDraw();
            },
            onFail: err =>
            {
                if (!this) return;
                SetStatus("Rank_Failed");
                Debug.LogError("[GameOverUI] Submit fail: " + err);
            },
            step: KHS_Script_ScoreManager.Instance != null ? KHS_Script_ScoreManager.Instance.RoundRespone() : 1
        );
    }

    public void OnClickOpenRank()
    {
        if (!worldRanking)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            worldRanking = PSH_Script_WorldRankingUI.Install(canvas.rootCanvas.transform,
                textFinalScore != null ? textFinalScore.font : null, false);
            worldRanking.Closed += () => _rankPanelOpen = false;
        }
        worldRanking.Show();
        Debug.Log("[GameOverUI] OnClickOpenRank");
        _rankPanelOpen = true;
        _refreshElapsed = 0f;
    }

    private void EnsureDemoPresentation()
    {
        if (demoPresentation == null) demoPresentation = BallARetroDemoPresentation.InstallResult(this);
        if (demoPresentation != null) demoPresentation.Refresh();
    }

    public void OnClickCloseRank()
    {
        _rankPanelOpen = false;
        if (worldRanking) worldRanking.Close();
        if (panelRank) panelRank.SetActive(false);
        if (panelGameEnd) panelGameEnd.SetActive(true);
    }

    public void OnClickRefreshLeaderboard()
    {
        FetchFreshLeaderboardAndDraw();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 내부 유틸

    private void FetchFreshLeaderboardAndDraw()
    {
        TryWireService();
        if (service == null) return;

        service.FetchLeaderboard(gameMode, level,
            lb => { if (this) RefreshLeaderboardUI(lb.top10); },
            err => { if (this) Debug.LogError("[Rank] refresh fail: " + err); });
    }

    private void RefreshLeaderboardUI(ScoreRow[] rows)
    {
        var data = rows ?? Array.Empty<ScoreRow>();

        if (leaderboardList != null)
            leaderboardList.Populate(data);
        else if (textLeaderboard != null)
            textLeaderboard.text = BuildTop10Text(data);
    }

    private static bool HasTop10(SubmitResp resp)
        => (resp != null && resp.top10 != null && resp.top10.Length > 0);

    private string BuildTop10Text(ScoreRow[] rows)
    {
        var arr = rows ?? Array.Empty<ScoreRow>();
        var sb = new StringBuilder();
        for (int i = 0; i < arr.Length; i++)
            sb.AppendLine($"{i + 1,2}. {Safe(arr[i].nickname, 18),-18}  {arr[i].score,8:N0}");
        return sb.ToString();
    }

    private string Safe(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "Player";
        s = s.Trim();
        return s.Length > maxLen ? s.Substring(0, maxLen) + "…" : s;
    }
}
