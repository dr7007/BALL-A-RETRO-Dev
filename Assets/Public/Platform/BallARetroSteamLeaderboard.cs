using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if BALLARETRO_STEAM && !BALLARETRO_STOVE
using Steamworks;
#endif

/// <summary>Steam account scores; all requests use the Steam client, without a web API key.</summary>
public sealed class BallARetroSteamLeaderboard : MonoBehaviour
{
    private readonly List<IDisposable> pending = new();
    private bool uploading;
    internal float requestTimeout = 15f;

    public static string BoardName(string mode, int level)
    {
        if (string.IsNullOrWhiteSpace(mode)) mode = "Classic";
        // Keep mode/level partitions deterministic without silently merging their names.
        if (mode.Length > 64) throw new ArgumentException("Game mode is too long.");
        return "BALLARETRO_" + Uri.EscapeDataString(mode.Trim()) + "_" + Mathf.Max(1, level);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        foreach (var call in pending) call.Dispose();
        pending.Clear();
        uploading = false;
    }

    public void Fetch(string mode, int level, Action<LbResp> done, Action<string> failed)
    {
#if BALLARETRO_STEAM && !BALLARETRO_STOVE
        StartCoroutine(FetchRoutine(mode, level, done, failed));
#else
        failed?.Invoke("Steam only");
#endif
    }

    public void Submit(int score, string mode, int level, Action<SubmitResp> done, Action<string> failed)
    {
#if BALLARETRO_STEAM && !BALLARETRO_STOVE
        if (uploading) { failed?.Invoke("Score upload already in progress"); return; }
        if (score < 0) { failed?.Invoke("Score must not be negative"); return; }
        StartCoroutine(SubmitRoutine(score, mode, level, done, failed));
#else
        failed?.Invoke("Steam only");
#endif
    }

#if BALLARETRO_STEAM && !BALLARETRO_STOVE
    private bool LoggedIn(Action<string> failed)
    {
        if (SteamManager.TryGetLoggedInPlayer(out _, out _)) return true;
        failed?.Invoke("Steam login required");
        return false;
    }

    private IEnumerator WaitForCall<T>(SteamAPICall_t handle, Action<T> done, Action<string> failed)
    {
        if (handle.m_SteamAPICall == 0) { failed?.Invoke("Steam request unavailable"); yield break; }
        bool finished = false, ioFailure = false;
        T response = default;
        var call = CallResult<T>.Create((value, error) => { response = value; ioFailure = error; finished = true; });
        pending.Add(call);
        try
        {
            call.Set(handle);
            float deadline = Time.realtimeSinceStartup + requestTimeout;
            while (!finished && Time.realtimeSinceStartup < deadline) yield return null;
            if (!finished || ioFailure) failed?.Invoke("Steam request failed or timed out");
            else done(response);
        }
        finally { pending.Remove(call); call.Dispose(); }
    }

    private IEnumerator FindBoard(string mode, int level, Action<SteamLeaderboard_t> done, Action<string> failed)
    {
        if (!LoggedIn(failed)) yield break;
        yield return WaitForCall<LeaderboardFindResult_t>(SteamUserStats.FindOrCreateLeaderboard(BoardName(mode, level),
            ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric),
            result =>
            {
                if (result.m_bLeaderboardFound == 0) failed?.Invoke("Steam leaderboard unavailable");
                else done(result.m_hSteamLeaderboard);
            }, failed);
    }

    private IEnumerator FetchRoutine(string mode, int level, Action<LbResp> done, Action<string> failed)
    {
        SteamLeaderboard_t board = default;
        bool found = false;
        yield return FindBoard(mode, level, value => { board = value; found = true; }, failed);
        if (!found || !LoggedIn(failed)) yield break;
        yield return Download(board, mode, level, done, failed);
    }

    private IEnumerator Download(SteamLeaderboard_t board, string mode, int level, Action<LbResp> done, Action<string> failed)
    {
        yield return WaitForCall<LeaderboardScoresDownloaded_t>(SteamUserStats.DownloadLeaderboardEntries(board,
            ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 1, 10), result =>
            {
                var rows = new List<ScoreRow>();
                for (int i = 0; i < result.m_cEntryCount; i++)
                {
                    if (!SteamUserStats.GetDownloadedLeaderboardEntry(result.m_hSteamLeaderboardEntries, i,
                        out LeaderboardEntry_t entry, Array.Empty<int>(), 0))
                    {
                        failed?.Invoke("Could not read a Steam leaderboard entry");
                        return;
                    }
                    rows.Add(new ScoreRow { rank = entry.m_nGlobalRank, score = entry.m_nScore,
                        nickname = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser), game_mode = mode, level = level });
                }
                done?.Invoke(new LbResp { ok = true, top10 = rows.ToArray() });
            }, failed);
    }

    private IEnumerator SubmitRoutine(int score, string mode, int level, Action<SubmitResp> done, Action<string> failed)
    {
        uploading = true;
        try
        {
            if (!LoggedIn(failed)) yield break;
            SteamManager.TryGetLoggedInPlayer(out _, out string originalUser);
            SteamLeaderboard_t board = default;
            bool found = false;
            yield return FindBoard(mode, level, value => { board = value; found = true; }, failed);
            if (!found) yield break;
            if (!SteamManager.TryGetLoggedInPlayer(out _, out string currentUser) || originalUser != currentUser)
            { failed?.Invoke("Steam account changed before upload"); yield break; }
            SubmitResp submitted = null;
            yield return WaitForCall<LeaderboardScoreUploaded_t>(SteamUserStats.UploadLeaderboardScore(board,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, Array.Empty<int>(), 0), result =>
                {
                    if (result.m_bSuccess == 0) { failed?.Invoke("Steam score upload failed"); return; }
                    submitted = new SubmitResp { ok = true, your_score = result.m_nScore,
                        rank = result.m_nGlobalRankNew, top10 = Array.Empty<ScoreRow>() };
                }, failed);
            if (submitted == null) yield break;
            // Saving is successful even if the subsequent top-ten refresh is unavailable.
            yield return Download(board, mode, level, result => submitted.top10 = result.top10, _ => { });
            done?.Invoke(submitted);
        }
        finally { uploading = false; }
    }
#endif
}
