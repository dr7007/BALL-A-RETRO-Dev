using System.Text;
using TMPro;
using UnityEngine;

public class CJS_ScriptRefreshLBButton : MonoBehaviour
{
    public CJS_Script_PinballRankingService service;
    public TMP_Text leaderboardText;

    public void OnClickRefresh()
    {
        service.FetchLeaderboard("Classic", 1,
            onDone: resp =>
            {
                var sb = new StringBuilder();
                for (int i = 0; i < resp.top10.Length; i++)
                    sb.AppendLine($"{i + 1}. {resp.top10[i].nickname} - {resp.top10[i].score:N0}");
                if (leaderboardText) leaderboardText.text = sb.ToString();
            },
            onFail: err => { if (leaderboardText) leaderboardText.text = "Fail: " + err; }
        );
    }
}
