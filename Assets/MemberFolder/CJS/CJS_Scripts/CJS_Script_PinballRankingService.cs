using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CJS_Script_PinballRankingService : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string baseUrl = "http://localhost/pinball";
    [SerializeField] private string apiKey = "CHANGE_ME_STRONG_KEY";

    // ── Singleton ────────────────────────────────────────────────────────────
    private static CJS_Script_PinballRankingService instance;
    public static CJS_Script_PinballRankingService Instance
    {
        get
        {
            if (instance == null || instance.Equals(null))
                EnsureInstance(); // 항상 존재 보장
            return instance;
        }
    }

    /// <summary>싱글톤 인스턴스가 준비되면 알림 (Awake에서 호출)</summary>
    public static event System.Action<CJS_Script_PinballRankingService> InstanceReady;

    /// Resources 경로: Assets/Resources/Services/RankingService.prefab (선택)
    private const string ResourcesPrefabPath = "Services/RankingService";

    private static void EnsureInstance()
    {
        // 1) 기존 찾기(비활성 포함)
        var existing = Object.FindObjectOfType<CJS_Script_PinballRankingService>(true);
        if (existing != null)
        {
            instance = existing;
            DontDestroyOnLoad(instance.gameObject);
            return;
        }

        // 2) Resources 프리팹에서 생성(있으면)
        var prefab = Resources.Load<GameObject>(ResourcesPrefabPath);
        if (prefab != null)
        {
            var go = Object.Instantiate(prefab);
            instance = go.GetComponent<CJS_Script_PinballRankingService>();
            if (!instance) instance = go.AddComponent<CJS_Script_PinballRankingService>();
            DontDestroyOnLoad(instance.gameObject);
            return;
        }

        // 3) 최후: 빈 GO 생성
        {
            var go = new GameObject("RankingService");
            instance = go.AddComponent<CJS_Script_PinballRankingService>();
            DontDestroyOnLoad(instance.gameObject);
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            // 중복 생성 시 새걸 자멸시킴 (씬에 프리팹이 있어도 안전)
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        InstanceReady?.Invoke(this);

        Debug.Log($"[RankingService.Awake] baseUrl={baseUrl} apiKey.len={apiKey?.Length ?? 0} nick='{Nickname}'");
    }

    // ── Player nickname ──────────────────────────────────────────────────────
    public string Nickname
    {
        get => PlayerPrefs.GetString("nickname", "");
        set { PlayerPrefs.SetString("nickname", value); PlayerPrefs.Save(); }
    }

    public void SetNicknameAndStart(string nick)
    {
        if (string.IsNullOrWhiteSpace(nick)) nick = "Guest";
        Nickname = nick.Trim();
        RegisterNickname(null, err => Debug.LogWarning("[RegisterNickname] fail: " + err));
    }

    // ── Public API ───────────────────────────────────────────────────────────
    public void RegisterNickname(System.Action onDone, System.Action<string> onFail)
    {
        StartCoroutine(CoRegisterNickname(onDone, onFail));
    }

    public void SubmitScore(int score, string gameMode, int level,
                            System.Action<SubmitResp> onDone,
                            System.Action<string> onFail)
    {
        Debug.Log($"[SubmitScore()] called score={score} mode='{gameMode}' level={level}");
        StartCoroutine(CoSubmit(score, gameMode, level, onDone, onFail));
    }

    public void FetchLeaderboard(string gameMode, int level,
                                 System.Action<LbResp> onDone,
                                 System.Action<string> onFail)
    {
        StartCoroutine(CoFetch(gameMode, level, onDone, onFail));
    }

    // ── Coroutines ───────────────────────────────────────────────────────────
    private IEnumerator CoRegisterNickname(System.Action onDone, System.Action<string> onFail)
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

    private IEnumerator CoSubmit(int score, string gameMode, int level,
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

    private IEnumerator CoFetch(string gameMode, int level,
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
