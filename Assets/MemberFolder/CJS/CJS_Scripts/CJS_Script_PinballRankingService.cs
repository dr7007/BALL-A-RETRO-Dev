using System.Collections;
using UnityEngine;

public class CJS_Script_PinballRankingService : MonoBehaviour
{
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


    }

    // ── Player nickname ──────────────────────────────────────────────────────
    public string Nickname
    {
        get
        {
            if (BallARetroPlatform.IsDemoBuild)
                return SteamManager.TryGetExistingLoggedInPlayer(out string demoName, out _) ? demoName : "Guest";
            if (BallARetroPlatform.IsSteamBuild)
                return SteamManager.TryGetLoggedInPlayer(out string name, out _) ? name : string.Empty;
            return PlayerPrefs.GetString("nickname", "");
        }
        set
        {
            // Demo scores use the live Steam name (or Guest offline); they must
            // never overwrite the full game's stored nickname in editor tests.
            if (BallARetroPlatform.IsDemoBuild) return;
            if (BallARetroPlatform.IsSteamBuild)
            {
                if (!SteamManager.TryGetLoggedInPlayer(out string name, out _)) return;
                value = name;
            }
            PlayerPrefs.SetString("nickname", value); PlayerPrefs.Save();
        }
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
                            System.Action<string> onFail, int step = 1)
    {
        Debug.Log($"[SubmitScore()] called score={score} mode='{gameMode}' level={level}");
        StartCoroutine(CoSubmit(score, gameMode, level, onDone, onFail, step));
    }

    public void FetchLeaderboard(string gameMode, int level,
                                 System.Action<LbResp> onDone,
                                 System.Action<string> onFail)
    {
        StartCoroutine(CoFetch(gameMode, level, onDone, onFail));
    }

    private BallARetroSteamLeaderboard SteamLeaderboard
    {
        get
        {
            var backend = GetComponent<BallARetroSteamLeaderboard>();
            return backend != null ? backend : gameObject.AddComponent<BallARetroSteamLeaderboard>();
        }
    }

    private IEnumerator CoRegisterNickname(System.Action onDone, System.Action<string> onFail)
    {
        // Steam identifies players itself; no nickname registration service is needed.
        if (BallARetroPlatform.UsesSteamLeaderboards && !BallARetroPlatform.IsDemoBuild && !SteamManager.TryGetLoggedInPlayer(out _, out _))
        { onFail?.Invoke("Steam login required"); yield break; }
        yield return null;
        onDone?.Invoke();
    }

    private IEnumerator CoSubmit(int score, string gameMode, int level,
        System.Action<SubmitResp> onDone, System.Action<string> onFail, int step)
    {
        if (score < 0) { onFail?.Invoke("Score must not be negative."); yield break; }
        if (BallARetroPlatform.IsDemoBuild)
        {
            // Keep the local best even if Steam is offline, the request fails,
            // or the player leaves the result screen while callbacks are pending.
            var demoRows = BallARetroPersonalRecordStore.Submit(Nickname, score, gameMode, level);
            var local = new SubmitResp { ok = true, local_only = true, your_score = score, rank = 0, top10 = demoRows };
            yield return null;
            if (!SteamManager.TryGetExistingLoggedInPlayer(out _, out _)) { onDone?.Invoke(local); yield break; }
            SteamLeaderboard.Submit(score, gameMode, level, onDone, _ => onDone?.Invoke(local), step);
            yield break;
        }
        if (BallARetroPlatform.UsesSteamLeaderboards)
        {
            SteamLeaderboard.Submit(score, gameMode, level, onDone, onFail, step);
            yield break;
        }
        // Offline personal best remains available for run progress, without a ranking UI.
        var rows = BallARetroPersonalRecordStore.Submit(Nickname, score, gameMode, level);
        yield return null;
        onDone?.Invoke(new SubmitResp { ok = true, your_score = score, rank = 1, top10 = rows });
    }

    private IEnumerator CoFetch(string gameMode, int level,
        System.Action<LbResp> onDone, System.Action<string> onFail)
    {
        if (BallARetroPlatform.IsDemoBuild)
        {
            // Offline local best is displayed separately; never manufacture a
            // global rank from a personal record or initialize Steam for this UI.
            var local = new LbResp { ok = true, local_only = true, top10 = System.Array.Empty<ScoreRow>() };
            yield return null;
            if (!SteamManager.TryGetExistingLoggedInPlayer(out _, out _)) { onDone?.Invoke(local); yield break; }
            SteamLeaderboard.Fetch(gameMode, level, onDone, _ => onDone?.Invoke(local));
            yield break;
        }
        if (BallARetroPlatform.UsesSteamLeaderboards)
        {
            SteamLeaderboard.Fetch(gameMode, level, onDone, onFail);
            yield break;
        }
        var rows = BallARetroPersonalRecordStore.Read(Nickname, gameMode, level);
        yield return null;
        onDone?.Invoke(new LbResp { ok = true, top10 = rows });
    }
}
