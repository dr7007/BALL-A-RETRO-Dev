using System;
using System.Text;
using TMPro;
using UnityEngine;

public class CJS_Script_GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    public CJS_Script_PinballRankingService service;
    public TMP_Text textFinalScore;
    public TMP_Text textResult;

    [Header("Leaderboard (choose one)")]
    public TMP_Text textLeaderboard;
    public CJS_Script_LeaderboardList leaderboardList;

    [Header("Panels")]
    public GameObject panelGameEnd;
    public GameObject panelRank;

    [Header("Choice Summary")]
    public CJS_Script_ChoiceSummaryUI choiceSummaryUI;

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
        else
            Debug.Log("[GameOverUI] Service wired.");
    }

    public void Show(int finalScore)
    {
        _finalScore = finalScore;

        if (panelGameEnd) panelGameEnd.SetActive(true);
        if (panelRank) panelRank.SetActive(false);

        if (textFinalScore) textFinalScore.text = _finalScore.ToString("N0");
        if (textResult) textResult.text = "제출 대기중…";

        //  최종 선택지 요약 생성
        choiceSummaryUI?.ShowSummary();

        Debug.Log($"[GameOverUI.Show] finalScore={_finalScore} autoSubmit={autoSubmitOnShow}");
        if (autoSubmitOnShow) OnClickSubmit();
    }

    public void SetFinalScore(int score)
    {
        _finalScore = score;
        if (textFinalScore) textFinalScore.text = score.ToString("N0");
    }

    public void OnClickSubmit()
    {
        if (service == null) { Debug.LogError("[GameOverUI] service is null"); return; }

        if (textResult) textResult.text = "서버에 전송중…";
        Debug.Log("[GameOverUI] OnClickSubmit()");

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

    public void OnClickSubmitRandomForTest()
    {
        int rnd = UnityEngine.Random.Range(1000, 50000);
        Debug.Log("[GameOverUI] OnClickSubmitRandomForTest() score=" + rnd);
        _finalScore = rnd;
        OnClickSubmit();
    }

    public void OnClickOpenRank()
    {
        Debug.Log("[GameOverUI] OnClickOpenRank");

        if (leaderboardList)
        {
            if (_lastSubmit?.top10 != null)
            {
                leaderboardList.Populate(_lastSubmit.top10);
            }
            else if (service != null)
            {
                service.FetchLeaderboard(gameMode, level,
                    lb => leaderboardList.Populate(lb.top10 ?? Array.Empty<ScoreRow>()),
                    err => Debug.LogError("[Rank] fetch fail: " + err));
            }
        }
        else if (textLeaderboard)
        {
            if (_lastSubmit?.top10 != null)
            {
                textLeaderboard.text = BuildTop10Text(_lastSubmit.top10);
            }
            else if (service != null)
            {
                service.FetchLeaderboard(gameMode, level,
                    lb => { textLeaderboard.text = BuildTop10Text(lb.top10); },
                    err => Debug.LogError("[Rank] fetch fail: " + err));
            }
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
