//using System;
//using System.Text;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// 게임오버 패널 컨트롤 + 점수 제출 + 리더보드 + 선택지 요약 갱신
//public class CJS_Script_GameOverUI : MonoBehaviour
//{
//    [Header("Services")]
//    [SerializeField] private CJS_Script_PinballRankingService service;

//    [Header("Score Texts")]
//    [SerializeField] private TMP_Text textFinalScore;
//    [SerializeField] private TMP_Text textResult;

//    [Header("Leaderboard (choose one)")]
//    [SerializeField] private TMP_Text textLeaderboard;                 // 단순 텍스트 출력
//    [SerializeField] private CJS_Script_LeaderboardList leaderboardList; // 아이템 프리팹 방식

//    [Header("Panels")]
//    [SerializeField] private GameObject panelGameEnd;   // 게임오버 요약 패널
//    [SerializeField] private GameObject panelRank;      // 랭크 패널

//    [Header("Choice Summary (optional)")]
//    [SerializeField] private CJS_Script_ChoiceSummaryUI summaryUI; // 게임오버시 선택지 요약

//    [Header("Submit Options")]
//    [SerializeField] private string gameMode = "Classic";
//    [SerializeField] private int level = 1;
//    [SerializeField] private bool autoSubmitOnShow = true;

//    // ─────────────────────────────────────────────────────────────────────────────
//    private int _finalScore;
//    private SubmitResp _lastSubmit;

//    void Awake()
//    {
//        if (!service)
//            service = FindObjectOfType<CJS_Script_PinballRankingService>(includeInactive: true);

//        if (!panelGameEnd) panelGameEnd = gameObject; // 자기 자신을 사용할 수도 있음

//        if (!service)
//            Debug.LogError("[GameOverUI] RankingService not found in scene.");
//    }

//    // 외부에서 게임오버 시 호출
//    public void Show(int finalScore)
//    {
//        _finalScore = finalScore;

//        // 패널 전환
//        if (panelRank) panelRank.SetActive(false);
//        if (panelGameEnd) panelGameEnd.SetActive(true);

//        // 점수 출력
//        if (textFinalScore) textFinalScore.text = _finalScore.ToString("N0");
//        if (textResult) textResult.text = "제출 대기중…";

//        // 선택지 요약 재빌드 (패널 열릴 때 항상 최신)
//        summaryUI?.Rebuild();
//        ForceLayout();

//        Debug.Log($"[GameOverUI.Show] finalScore={_finalScore} autoSubmit={autoSubmitOnShow}");
//        if (autoSubmitOnShow) OnClickSubmit();
//    }

//    public void SetFinalScore(int score)
//    {
//        _finalScore = score;
//        if (textFinalScore) textFinalScore.text = score.ToString("N0");
//    }

//    // 점수 제출 버튼
//    public void OnClickSubmit()
//    {
//        if (!service) { Debug.LogError("[GameOverUI] service is null"); return; }

//        if (textResult) textResult.text = "서버에 전송중…";
//        Debug.Log("[GameOverUI] OnClickSubmit()");

//        service.SubmitScore(_finalScore, gameMode, level,
//            onDone: resp =>
//            {
//                _lastSubmit = resp;

//                if (textResult)
//                    textResult.text = $"내 점수: {resp.your_score:N0}\n내 랭킹: #{resp.rank}";

//                // Top10 초기 채우기 (랭크 패널 열릴 때도 재사용)
//                if (leaderboardList)
//                {
//                    leaderboardList.Populate(resp.top10 ?? Array.Empty<ScoreRow>());
//                }
//                else if (textLeaderboard)
//                {
//                    textLeaderboard.text = BuildTop10Text(resp.top10);
//                }
//            },
//            onFail: err =>
//            {
//                if (textResult) textResult.text = $"제출 실패: {err}";
//                Debug.LogError("[GameOverUI] Submit fail: " + err);
//            }
//        );
//    }

//    // 테스트용 랜덤 제출
//    public void OnClickSubmitRandomForTest()
//    {
//        int rnd = UnityEngine.Random.Range(1000, 50000);
//        Debug.Log("[GameOverUI] OnClickSubmitRandomForTest() score=" + rnd);
//        _finalScore = rnd;
//        OnClickSubmit();
//    }

//    // 랭크 패널 열기
//    public void OnClickOpenRank()
//    {
//        Debug.Log("[GameOverUI] OnClickOpenRank");

//        // 기존 응답이 있으면 재사용, 없으면 서버 조회
//        if (leaderboardList)
//        {
//            if (_lastSubmit?.top10 != null)
//            {
//                leaderboardList.Populate(_lastSubmit.top10);
//            }
//            else if (service)
//            {
//                service.FetchLeaderboard(gameMode, level,
//                    lb => leaderboardList.Populate(lb.top10 ?? Array.Empty<ScoreRow>()),
//                    err => Debug.LogError("[Rank] fetch fail: " + err));
//            }
//        }
//        else if (textLeaderboard)
//        {
//            if (_lastSubmit?.top10 != null)
//            {
//                textLeaderboard.text = BuildTop10Text(_lastSubmit.top10);
//            }
//            else if (service)
//            {
//                service.FetchLeaderboard(gameMode, level,
//                    lb => { textLeaderboard.text = BuildTop10Text(lb.top10); },
//                    err => Debug.LogError("[Rank] fetch fail: " + err));
//            }
//        }

//        if (panelGameEnd) panelGameEnd.SetActive(false);
//        if (panelRank) panelRank.SetActive(true);
//        ForceLayout();
//    }

//    public void OnClickCloseRank()
//    {
//        if (panelRank) panelRank.SetActive(false);
//        if (panelGameEnd) panelGameEnd.SetActive(true);

//        // 요약 패널로 돌아오면 한 번 더 레이아웃 갱신(애니메이션 사용 시 안전)
//        summaryUI?.Rebuild();
//        ForceLayout();
//    }

//    public void OnClickRefreshLeaderboard()
//    {
//        if (!service) return;

//        service.FetchLeaderboard(gameMode, level,
//            lb =>
//            {
//                if (leaderboardList) leaderboardList.Populate(lb.top10 ?? Array.Empty<ScoreRow>());
//                else if (textLeaderboard) textLeaderboard.text = BuildTop10Text(lb.top10);
//            },
//            err => Debug.LogError("[Rank] refresh fail: " + err));
//    }

//    // ─────────────────────────────────────────────────────────────────────────────
//    // 내부 유틸

//    private void ForceLayout()
//    {
//        // 패널 전환 직후 UI가 비어 보이는 현상 방지용
//        Canvas.ForceUpdateCanvases();
//        Canvas.ForceUpdateCanvases();
//    }

//    private string BuildTop10Text(ScoreRow[] rows)
//    {
//        var arr = rows ?? Array.Empty<ScoreRow>();
//        var sb = new StringBuilder();
//        for (int i = 0; i < arr.Length; i++)
//            sb.AppendLine($"{i + 1,2}. {Safe(arr[i].nickname, 18),-18}  {arr[i].score,8:N0}");
//        return sb.ToString();
//    }

//    private string Safe(string s, int maxLen)
//    {
//        if (string.IsNullOrEmpty(s)) return "Player";
//        s = s.Trim();
//        return s.Length > maxLen ? s.Substring(0, maxLen) + "…" : s;
//    }
//}
using System;
using System.Text;
using TMPro;
using UnityEngine;

public class CJS_Script_GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    public CJS_Script_PinballRankingService service;
    public TMP_Text textFinalScore;     // ← Image_Score_Init (TMP_Text) 연결
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

    private int _finalScore;
    private SubmitResp _lastSubmit;

    void Awake()
    {
        if (service == null)
            service = FindObjectOfType<CJS_Script_PinballRankingService>(includeInactive: true);

        if (panelGameEnd == null) panelGameEnd = gameObject;

        if (service == null)
            Debug.LogError("[GameOverUI] RankingService not found in scene.");
    }

    /// 외부에서 점수만 먼저 반영하고 싶을 때 사용
    public void SetFinalScore(int score)
    {
        _finalScore = score;
        if (textFinalScore) textFinalScore.text = score.ToString("N0"); // 숫자만
    }

    /// 게임 종료 시 호출
    public void Show(int finalScore)
    {
        _finalScore = finalScore;

        if (panelGameEnd) panelGameEnd.SetActive(true);
        if (panelRank) panelRank.SetActive(false);

        // 숫자만 표시
        if (textFinalScore) textFinalScore.text = _finalScore.ToString("N0");
        if (textResult) textResult.text = "제출 대기중…";

        Debug.Log($"[GameOverUI.Show] finalScore={_finalScore} autoSubmit={autoSubmitOnShow}");
        if (autoSubmitOnShow) OnClickSubmit();
    }

    public void OnClickSubmit()
    {
        if (service == null) { Debug.LogError("[GameOverUI] service is null"); return; }

        if (textResult) textResult.text = "서버에 전송중…";
        service.SubmitScore(_finalScore, gameMode, level,
            onDone: resp =>
            {
                _lastSubmit = resp;

                if (textResult)
                    textResult.text = $"내 점수: {resp.your_score:N0}\n내 랭킹: #{resp.rank}";

                if (leaderboardList != null)
                    leaderboardList.Populate(resp.top10 ?? Array.Empty<ScoreRow>());
                else if (textLeaderboard != null)
                    textLeaderboard.text = BuildTop10Text(resp.top10);
            },
            onFail: err =>
            {
                if (textResult) textResult.text = $"제출 실패: {err}";
                Debug.LogError("[GameOverUI] Submit fail: " + err);
            }
        );
    }

    public void OnClickOpenRank()
    {
        Debug.Log("[GameOverUI] OnClickOpenRank");

        if (leaderboardList)
        {
            if (_lastSubmit?.top10 != null)
                leaderboardList.Populate(_lastSubmit.top10);
            else if (service != null)
                service.FetchLeaderboard(gameMode, level,
                    lb => leaderboardList.Populate(lb.top10 ?? Array.Empty<ScoreRow>()),
                    err => Debug.LogError("[Rank] fetch fail: " + err));
        }
        else if (textLeaderboard)
        {
            if (_lastSubmit?.top10 != null)
                textLeaderboard.text = BuildTop10Text(_lastSubmit.top10);
            else if (service != null)
                service.FetchLeaderboard(gameMode, level,
                    lb => textLeaderboard.text = BuildTop10Text(lb.top10),
                    err => Debug.LogError("[Rank] fetch fail: " + err));
        }

        if (panelGameEnd) panelGameEnd.SetActive(false);
        if (panelRank) panelRank.SetActive(true);
    }

    public void OnClickCloseRank()
    {
        if (panelRank) panelRank.SetActive(false);
        if (panelGameEnd) panelGameEnd.SetActive(true);
    }

    public void OnClickRefreshLeaderboard()
    {
        if (service == null) return;

        service.FetchLeaderboard(gameMode, level,
            lb =>
            {
                if (leaderboardList) leaderboardList.Populate(lb.top10 ?? Array.Empty<ScoreRow>());
                else if (textLeaderboard) textLeaderboard.text = BuildTop10Text(lb.top10);
            },
            err => Debug.LogError("[Rank] refresh fail: " + err));
    }

    string BuildTop10Text(ScoreRow[] rows)
    {
        var arr = rows ?? Array.Empty<ScoreRow>();
        var sb = new StringBuilder();
        for (int i = 0; i < arr.Length; i++)
            sb.AppendLine($"{i + 1,2}. {Safe(arr[i].nickname, 18),-18}  {arr[i].score,8:N0}");
        return sb.ToString();
    }

    string Safe(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "Player";
        s = s.Trim();
        return s.Length > maxLen ? s.Substring(0, maxLen) + "…" : s;
    }
}
