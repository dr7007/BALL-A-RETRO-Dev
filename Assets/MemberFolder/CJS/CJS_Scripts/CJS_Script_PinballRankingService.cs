using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CJS_Script_PinballRankingService : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] string baseUrl = "http://localhost/pinball"; 
    [SerializeField] string apiKey = "CHANGE_ME_STRONG_KEY";     

    public string Nickname
    {
        get => PlayerPrefs.GetString("nickname", "");
        set { PlayerPrefs.SetString("nickname", value); PlayerPrefs.Save(); }
    }

    // 닉네임 저장 (게임 시작 시 1회)
    public void SetNicknameAndStart(string nick)
    {
        if (string.IsNullOrWhiteSpace(nick)) nick = "Guest";
        Nickname = nick.Trim();
    }

    // 점수 제출
    public void SubmitScore(int score, string gameMode, int level,
                            System.Action<SubmitResp> onDone,
                            System.Action<string> onFail)
    {
        StartCoroutine(CoSubmit(score, gameMode, level, onDone, onFail));
    }

    IEnumerator CoSubmit(int score, string gameMode, int level,
                         System.Action<SubmitResp> onDone,
                         System.Action<string> onFail)
    {
        WWWForm f = new WWWForm();
        f.AddField("nickname", Nickname);
        f.AddField("score", score);
        if (!string.IsNullOrEmpty(gameMode)) f.AddField("game_mode", gameMode);
        if (level > 0) f.AddField("level", level);
        f.AddField("key", apiKey); 

        using (var req = UnityWebRequest.Post($"{baseUrl}/submit_score.php", f))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                onFail?.Invoke(req.error); yield break;
            }
            var resp = JsonUtility.FromJson<SubmitResp>(req.downloadHandler.text);
            onDone?.Invoke(resp);
        }
    }

    // 상위 10 조회
    public void FetchLeaderboard(string gameMode, int level,
                                 System.Action<LbResp> onDone,
                                 System.Action<string> onFail)
    {
        StartCoroutine(CoFetch(gameMode, level, onDone, onFail));
    }

    IEnumerator CoFetch(string gameMode, int level,
                        System.Action<LbResp> onDone,
                        System.Action<string> onFail)
    {
        WWWForm f = new WWWForm();
        if (!string.IsNullOrEmpty(gameMode)) f.AddField("game_mode", gameMode);
        if (level > 0) f.AddField("level", level);
        f.AddField("key", apiKey);

        using (var req = UnityWebRequest.Post($"{baseUrl}/get_leaderboard.php", f))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                onFail?.Invoke(req.error); yield break;
            }
            var resp = JsonUtility.FromJson<LbResp>(req.downloadHandler.text);
            onDone?.Invoke(resp);
        }
    }
}
