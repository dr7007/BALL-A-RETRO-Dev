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

    [Header("Options")]
    public string gameMode = "Classic";
    public int level = 1;
    public bool autoSubmitOnShow = true;

    int _finalScore;

    void Awake()
    {
        if (service == null)
        {
            service = FindObjectOfType<CJS_Script_PinballRankingService>(includeInactive: true);
            if (service == null) Debug.LogError("[GameOverUI] RankingService not found in scene.");
            else Debug.Log("[GameOverUI] Service auto-wired.");
        }
    }

    public void Show(int finalScore)
    {
        _finalScore = finalScore;
        gameObject.SetActive(true);
        if (textFinalScore) textFinalScore.text = $"최종 점수: {_finalScore:N0}";
        if (textResult) textResult.text = "제출 대기중…";

        Debug.Log($"[GameOverUI.Show] finalScore={_finalScore} autoSubmit={autoSubmitOnShow}");
        if (autoSubmitOnShow) OnClickSubmit();
    }

    public void OnClickSubmit()
    {
        if (service == null) { Debug.LogError("[GameOverUI] service is null"); return; }

        if (textResult) textResult.text = "서버에 전송중…";
        Debug.Log("[GameOverUI] OnClickSubmit()");

        service.SubmitScore(_finalScore, gameMode, level,
            onDone: resp =>
            {
                if (textResult)
                    textResult.text = $"내 점수: {resp.your_score:N0}\n내 랭킹: #{resp.rank}";

                if (leaderboardList != null)
                    leaderboardList.Populate(resp.top10 ?? new ScoreRow[0]);
                else if (textLeaderboard != null)
                {
                    var sb = new StringBuilder();
                    var arr = resp.top10 ?? new ScoreRow[0];
                    for (int i = 0; i < arr.Length; i++)
                        sb.AppendLine($"{i + 1,2}. {Safe(arr[i].nickname, 18),-18}  {arr[i].score,8:N0}");
                    textLeaderboard.text = sb.ToString();
                }
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
        int rnd = Random.Range(1000, 50000);
        Debug.Log("[GameOverUI] OnClickSubmitRandomForTest() score=" + rnd);
        _finalScore = rnd;
        OnClickSubmit();
    }

    string Safe(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "Player";
        s = s.Trim();
        return s.Length > maxLen ? s.Substring(0, maxLen) + "…" : s;
    }
}
