using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Steamworks;
using UnityEngine;
using UnityEngine.TestTools;

public class SteamDemoPlatformTests
{
    private BallARetroSteamSettings settings;
    private uint originalDemoId;
    private string mode, fullRecordKey, previousNickname;
    private bool hadNickname;
    private GameObject serviceObject, backendObject;
    private readonly FieldInfo demoField = typeof(BallARetroSteamSettings).GetField("demoAppId", BindingFlags.NonPublic | BindingFlags.Instance);

    [SetUp] public void Setup()
    {
        Assert.That(BallARetroPlatform.IsDemoBuild, Is.True);
        Fixture.Reset(); LeaderboardFixture.Reset();
        settings = Resources.Load<BallARetroSteamSettings>(BallARetroSteamSettings.ResourcePath);
        originalDemoId = settings.DemoAppId;
        demoField.SetValue(settings, Fixture.AppId);
        typeof(SteamManager).GetMethod("InitOnPlayMode", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        mode = "DemoTest-" + Guid.NewGuid().ToString("N");
        fullRecordKey = BallARetroPersonalRecordStore.StorageKey(mode, 1).Replace("SteamDemo", "STOVE");
        hadNickname = PlayerPrefs.HasKey("nickname");
        previousNickname = PlayerPrefs.GetString("nickname", "");
        PlayerPrefs.SetString(fullRecordKey, "full-game-record-sentinel");
        PlayerPrefs.SetString("nickname", "full-game-nickname-sentinel");
    }

    [TearDown] public void Cleanup()
    {
        if (serviceObject) UnityEngine.Object.DestroyImmediate(serviceObject);
        if (backendObject) UnityEngine.Object.DestroyImmediate(backendObject);
        foreach (var manager in UnityEngine.Object.FindObjectsByType<SteamManager>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(manager.gameObject);
        demoField.SetValue(settings, originalDemoId);
        PlayerPrefs.DeleteKey(BallARetroPersonalRecordStore.StorageKey(mode, 1));
        PlayerPrefs.DeleteKey(fullRecordKey);
        if (hadNickname) PlayerPrefs.SetString("nickname", previousNickname); else PlayerPrefs.DeleteKey("nickname");
        PlayerPrefs.Save();
    }

    [Test] public void DemoUsesItsAppIdAndFullGameWishlistDestination()
    {
        Assert.That(BallARetroPlatform.IsSteamBuild, Is.True);
        Assert.That(BallARetroPlatform.UsesSteamLeaderboards, Is.True);
        Assert.That(BallARetroPlatform.UsesPersonalRecords, Is.True);
        Assert.That(BallARetroSteamSettings.TryGetAppId(out uint id), Is.True);
        Assert.That(id, Is.EqualTo(settings.DemoAppId));
        Assert.That(id, Is.Not.EqualTo(settings.FullAppId));
        Assert.That(BallARetroSteamSettings.FullGameStoreUrl, Is.EqualTo("https://store.steampowered.com/app/" + settings.FullAppId + "/"));
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.True);
        Assert.That(Fixture.RestartAppId, Is.EqualTo(settings.DemoAppId));
    }

    [TestCase(0u)] [TestCase(480u)] [TestCase(123456u)]
    public void MissingTestOrFullGameDemoIdIsRejected(uint badId)
    {
        demoField.SetValue(settings, badId);
        Assert.That(BallARetroSteamSettings.TryGetAppId(true, out _), Is.False);
        Assert.That(BallARetroSteamSettings.TryGetAppId(false, out uint fullId), Is.True);
        Assert.That(fullId, Is.EqualTo(settings.FullAppId));
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.False);
        Assert.That(Fixture.InitCalls, Is.Zero);
    }

    [Test] public void FullGameLaunchContextCannotInitializeDemo()
    {
        Fixture.RuntimeAppId = settings.FullAppId;
        LogAssert.Expect(LogType.Error, new Regex("실행된 App ID가"));
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.False);
        Assert.That(Fixture.ShutdownCalls, Is.EqualTo(1));
    }

    [UnityTest] public IEnumerator LocalScoresCompleteOfflineWithoutChangingFullGameRecords()
    {
        Fixture.LoggedOn = false;
        serviceObject = new GameObject("DemoRanking", typeof(CJS_Script_PinballRankingService));
        var service = serviceObject.GetComponent<CJS_Script_PinballRankingService>();
        service.Nickname = "Demo name";
        bool registered = false;
        service.RegisterNickname(() => registered = true, Assert.Fail);
        yield return null; yield return null;
        Assert.That(registered, Is.True);
        SubmitResp first = null, second = null;
        service.SubmitScore(9000, mode, 1, value => first = value, Assert.Fail);
        yield return null; yield return null;
        service.SubmitScore(4000, mode, 1, value => second = value, Assert.Fail);
        yield return null; yield return null;
        Assert.That(first.ok && second.ok, Is.True);
        Assert.That(first.local_only && second.local_only, Is.True);
        Assert.That(second.top10[0].score, Is.EqualTo(9000));
        Assert.That(second.top10[0].nickname, Is.EqualTo("Guest"));
        LbResp fetched = null;
        service.FetchLeaderboard(mode, 1, value => fetched = value, Assert.Fail);
        yield return null; yield return null;
        Assert.That(fetched.local_only, Is.True);
        Assert.That(fetched.top10, Is.Empty);
        Assert.That(fetched.personal, Is.Null, "An offline personal best has no global rank.");
        Assert.That(BallARetroPersonalRecordStore.Read("Guest", mode, 1)[0].score, Is.EqualTo(9000));
        Assert.That(LeaderboardFixture.Finds, Is.Zero);
        Assert.That(LeaderboardFixture.Uploads, Is.Zero);
        Assert.That(Fixture.InitCalls, Is.Zero, "Local demo scoring must not start Steam authentication.");
        StringAssert.StartsWith("BallARetro.SteamDemo.Best.v1.", BallARetroPersonalRecordStore.StorageKey(mode, 1));
        Assert.That(PlayerPrefs.GetString(fullRecordKey), Is.EqualTo("full-game-record-sentinel"));
        Assert.That(PlayerPrefs.GetString("nickname"), Is.EqualTo("full-game-nickname-sentinel"));
    }

    [Test] public void DirectDemoLeaderboardCallsDoNotInitializeAnOfflineClient()
    {
        backendObject = new GameObject("DemoBackend", typeof(BallARetroSteamLeaderboard));
        var backend = backendObject.GetComponent<BallARetroSteamLeaderboard>();
        string submitError = null, fetchError = null;
        backend.Submit(100, "Classic", 1, _ => Assert.Fail("Demo uploaded online"), value => submitError = value);
        backend.Fetch("Classic", 1, _ => Assert.Fail("Demo fetched online"), value => fetchError = value);
        Assert.That(submitError, Is.Not.Null); Assert.That(fetchError, Is.Not.Null);
        Assert.That(LeaderboardFixture.Finds, Is.Zero); Assert.That(LeaderboardFixture.Uploads, Is.Zero);
        Assert.That(Fixture.InitCalls, Is.Zero);
    }

    [UnityTest] public IEnumerator SteamInitializationFailureStillSavesDemoBest()
    {
        Fixture.InitResult = false;
        serviceObject = new GameObject("OfflineDemoRanking", typeof(CJS_Script_PinballRankingService));
        var service = serviceObject.GetComponent<CJS_Script_PinballRankingService>();
        LogAssert.Expect(LogType.Error, new Regex("SteamAPI_Init\\(\\) failed"));
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.False);
        SubmitResp result = null;
        service.SubmitScore(1234, mode, 1, value => result = value, Assert.Fail);
        yield return null; yield return null;
        Assert.That(result.ok, Is.True);
        Assert.That(result.local_only, Is.True);
        Assert.That(result.top10[0].score, Is.EqualTo(1234));
        Assert.That(Fixture.InitCalls, Is.EqualTo(1));
        Assert.That(LeaderboardFixture.Finds, Is.Zero);
        Assert.That(LeaderboardFixture.Uploads, Is.Zero);
        Assert.That(PlayerPrefs.GetString(fullRecordKey), Is.EqualTo("full-game-record-sentinel"));
    }

    private IEnumerator WaitFor(Func<bool> complete)
    {
        float timeout = Time.realtimeSinceStartup + 2f;
        while (!complete() && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.That(complete(), Is.True, "Demo native callback did not complete.");
    }

    [UnityTest] public IEnumerator LoggedInDemoSavesLocalBeforeUploadingToDemoAppLeaderboard()
    {
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.True);
        Assert.That(Fixture.RuntimeAppId, Is.EqualTo(settings.DemoAppId));
        serviceObject = new GameObject("OnlineDemoRanking", typeof(CJS_Script_PinballRankingService));
        var service = serviceObject.GetComponent<CJS_Script_PinballRankingService>();
        LeaderboardFixture.Hold = true;
        SubmitResp submitted = null;
        service.SubmitScore(7500, mode, 1, value => submitted = value, Assert.Fail, 2);
        Assert.That(BallARetroPersonalRecordStore.Read("Me", mode, 1)[0].score, Is.EqualTo(7500), "Local best must exist before the first online yield/callback.");
        yield return null; yield return null;
        Assert.That(submitted, Is.Null);
        Assert.That(LeaderboardFixture.Finds, Is.EqualTo(1));
        Assert.That(LeaderboardFixture.Board, Is.EqualTo(BallARetroSteamLeaderboard.BoardName(mode, 1)));
        Assert.That(Fixture.RestartAppId, Is.EqualTo(settings.DemoAppId));
        LeaderboardFixture.Hold = false;
        yield return WaitFor(() => submitted != null);
        Assert.That(submitted.ok, Is.True); Assert.That(submitted.local_only, Is.False);
        Assert.That(LeaderboardFixture.Uploads, Is.EqualTo(1));
        Assert.That(LeaderboardFixture.BestStep, Is.EqualTo(2));
        Assert.That(PlayerPrefs.GetString(fullRecordKey), Is.EqualTo("full-game-record-sentinel"));
    }

    [UnityTest] public IEnumerator OnlineUploadFailureKeepsLocalSuccessAndDoesNotRaiseUiError()
    {
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.True);
        LeaderboardFixture.UploadFails = true;
        serviceObject = new GameObject("FailingDemoRanking", typeof(CJS_Script_PinballRankingService));
        var service = serviceObject.GetComponent<CJS_Script_PinballRankingService>();
        SubmitResp submitted = null;
        service.SubmitScore(4321, mode, 1, value => submitted = value, Assert.Fail);
        yield return WaitFor(() => submitted != null);
        Assert.That(submitted.ok && submitted.local_only, Is.True);
        Assert.That(submitted.rank, Is.Zero);
        Assert.That(BallARetroPersonalRecordStore.Read("Me", mode, 1)[0].score, Is.EqualTo(4321));
        Assert.That(LeaderboardFixture.Uploads, Is.EqualTo(1));
    }

    [UnityTest] public IEnumerator DemoFetchReturnsOnlineRanksOrEmptyLocalFallbackWithoutInventedRank()
    {
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.True);
        LeaderboardFixture.Rows = new[] { new LeaderboardEntry_t { m_steamIDUser = new CSteamID { Value = 42 }, m_nGlobalRank = 1, m_nScore = 12000 } };
        serviceObject = new GameObject("FetchingDemoRanking", typeof(CJS_Script_PinballRankingService));
        var service = serviceObject.GetComponent<CJS_Script_PinballRankingService>();
        LbResp fetched = null;
        service.FetchLeaderboard(mode, 1, value => fetched = value, Assert.Fail);
        yield return WaitFor(() => fetched != null);
        Assert.That(fetched.local_only, Is.False);
        Assert.That(fetched.top10[0].rank, Is.EqualTo(1));
        LeaderboardFixture.DownloadFails = true;
        fetched = null;
        service.FetchLeaderboard(mode, 1, value => fetched = value, Assert.Fail);
        yield return WaitFor(() => fetched != null);
        Assert.That(fetched.local_only, Is.True);
        Assert.That(fetched.top10, Is.Empty); Assert.That(fetched.personal, Is.Null);
    }
}
