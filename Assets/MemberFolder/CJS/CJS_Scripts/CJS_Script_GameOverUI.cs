// CJS_Script_GameOverUI.cs
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

    int _finalScore;

    // 게임 종료 시
    public void Show(int finalScore)
    {
        _finalScore = finalScore;
        gameObject.SetActive(true);
        if (textFinalScore) textFinalScore.text = $"최종 점수: {_finalScore:N0}";
        if (textResult) textResult.text = "제출 대기중…";
    }

    // 버튼에 연결
    public void OnClickSubmit()
    {
        if (textResult) textResult.text = "서버에 전송중…";

        service.SubmitScore(_finalScore, gameMode, level,
            onDone: resp =>
            {
                if (textResult)
                    textResult.text = $"내 점수: {resp.your_score:N0}\n내 랭킹: #{resp.rank}";

                // 둘 중 사용 중인 쪽으로 갱신
                if (leaderboardList != null)
                {
                    leaderboardList.Populate(resp.top10);
                }
                else if (textLeaderboard != null)
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < resp.top10.Length; i++)
                        sb.AppendLine($"{i + 1,2}. {Safe(resp.top10[i].nickname, 18),-18}  {resp.top10[i].score,8:N0}");
                    textLeaderboard.text = sb.ToString();
                }
            },
            onFail: err =>
            {
                if (textResult) textResult.text = $"제출 실패: {err}";
            }
        );
    }

    public void OnClickRefreshLeaderboard()
    {
        service.FetchLeaderboard(gameMode, level,
            onDone: resp =>
            {
                if (leaderboardList != null)
                {
                    leaderboardList.Populate(resp.top10);
                }
                else if (textLeaderboard != null)
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < resp.top10.Length; i++)
                        sb.AppendLine($"{i + 1,2}. {Safe(resp.top10[i].nickname, 18),-18}  {resp.top10[i].score,8:N0}");
                    textLeaderboard.text = sb.ToString();
                }
            },
            onFail: err =>
            {
                if (textResult) textResult.text = $"로드 실패: {err}";
            }
        );
    }

    string Safe(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "Player";
        s = s.Trim();
        return s.Length > maxLen ? s.Substring(0, maxLen) + "…" : s;
    }
}
