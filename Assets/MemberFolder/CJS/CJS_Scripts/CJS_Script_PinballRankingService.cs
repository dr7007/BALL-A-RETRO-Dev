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

    void Awake()
    {
        Debug.Log($"[RankingService.Awake] baseUrl={baseUrl} apiKey.len={apiKey?.Length ?? 0} nick='{Nickname}'");
        DontDestroyOnLoad(gameObject); // 씬 이동해도 유지
    }

    public void SetNicknameAndStart(string nick)
    {
        if (string.IsNullOrWhiteSpace(nick)) nick = "Guest";
        Nickname = nick.Trim();
        RegisterNickname(null, err => Debug.LogWarning("[RegisterNickname] fail: " + err));
    }

    public void RegisterNickname(System.Action onDone, System.Action<string> onFail)
    {
        StartCoroutine(CoRegisterNickname(onDone, onFail));
    }

    IEnumerator CoRegisterNickname(System.Action onDone, System.Action<string> onFail)
    {
        if (string.IsNullOrWhiteSpace(Nickname)) Nickname = "Guest";

        WWWForm f = new WWWForm();
        f.AddField("nickname", Nickname);
        f.AddField("key", apiKey);

        var url = $"{baseUrl}/register_nickname.php";
        Debug.Log($"[Register] url={url} nickname='{Nickname}'");
        using (var req = UnityWebRequest.Post(url, f))
        {
            yield return req.SendWebRequest();
            Debug.Log($"[Register] HTTP={req.responseCode} text={req.downloadHandler.text}");
            if (req.result != UnityWebRequest.Result.Success) { onFail?.Invoke(req.error); yield break; }

            RegisterResp resp = null;
            try { resp = JsonUtility.FromJson<RegisterResp>(req.downloadHandler.text); }
            catch { onFail?.Invoke("Invalid JSON"); yield break; }

            if (resp == null) { onFail?.Invoke("Empty response"); yield break; }
            if (!resp.ok) { onFail?.Invoke(resp.msg ?? "ok=false"); yield break; }

            onDone?.Invoke();
        }
    }

    public void SubmitScore(int score, string gameMode, int level,
                            System.Action<SubmitResp> onDone,
                            System.Action<string> onFail)
    {
        Debug.Log($"[SubmitScore()] called score={score} mode='{gameMode}' level={level}");
        StartCoroutine(CoSubmit(score, gameMode, level, onDone, onFail));
    }

    IEnumerator CoSubmit(int score, string gameMode, int level,
                         System.Action<SubmitResp> onDone,
                         System.Action<string> onFail)
    {
        if (string.IsNullOrWhiteSpace(Nickname)) Nickname = "Guest";

        WWWForm f = new WWWForm();
        f.AddField("nickname", Nickname);
        f.AddField("score", score);
        if (!string.IsNullOrEmpty(gameMode)) f.AddField("game_mode", gameMode);
        if (level > 0) f.AddField("level", level);
        f.AddField("key", apiKey);

        var url = $"{baseUrl}/submit_score.php";
        Debug.Log($"[Submit] url={url} nick='{Nickname}' score={score} mode='{gameMode}' level={level}");
        using (var req = UnityWebRequest.Post(url, f))
        {
            yield return req.SendWebRequest();
            Debug.Log($"[Submit] HTTP={req.responseCode} text={req.downloadHandler.text}");
            if (req.result != UnityWebRequest.Result.Success) { onFail?.Invoke(req.error); yield break; }

            SubmitResp resp = null;
            try { resp = JsonUtility.FromJson<SubmitResp>(req.downloadHandler.text); }
            catch { onFail?.Invoke("Invalid JSON"); yield break; }

            if (resp == null) { onFail?.Invoke("Empty response"); yield break; }
            if (!resp.ok) { onFail?.Invoke(resp.msg ?? "Server returned ok=false"); yield break; }

            onDone?.Invoke(resp);
        }
    }

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

        var url = $"{baseUrl}/get_leaderboard.php";
        Debug.Log($"[Leaderboard] url={url} mode='{gameMode}' level={level}");
        using (var req = UnityWebRequest.Post(url, f))
        {
            yield return req.SendWebRequest();
            Debug.Log($"[Leaderboard] HTTP={req.responseCode} text={req.downloadHandler.text}");
            if (req.result != UnityWebRequest.Result.Success) { onFail?.Invoke(req.error); yield break; }

            LbResp resp = null;
            try { resp = JsonUtility.FromJson<LbResp>(req.downloadHandler.text); }
            catch { onFail?.Invoke("Invalid JSON"); yield break; }

            if (resp == null) { onFail?.Invoke("Empty response"); yield break; }
            if (!resp.ok) { onFail?.Invoke(resp.msg ?? "Server returned ok=false"); yield break; }

            onDone?.Invoke(resp);
        }
    }
}
