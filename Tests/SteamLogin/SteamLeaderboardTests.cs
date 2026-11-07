using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Steamworks;
using UnityEngine;
using UnityEngine.TestTools;

public class SteamLeaderboardTests
{
    GameObject serviceObject;
    BallARetroSteamLeaderboard backend;
    [SetUp] public void Setup()
    {
        Fixture.Reset();LeaderboardFixture.Reset();
        typeof(SteamManager).GetMethod("InitOnPlayMode",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        serviceObject=new GameObject("Steam leaderboard");backend=serviceObject.AddComponent<BallARetroSteamLeaderboard>();
    }
    [TearDown] public void Cleanup()
    {
        Object.DestroyImmediate(serviceObject);
        foreach(var manager in Object.FindObjectsByType<SteamManager>(FindObjectsSortMode.None))Object.DestroyImmediate(manager.gameObject);
        Time.timeScale=1;
    }
    IEnumerator Wait(System.Func<bool> completed)
    {float end=Time.realtimeSinceStartup+2;while(!completed()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(completed(),Is.True,"Native request did not complete.");}
    [UnityTest] public IEnumerator GlobalTopTenUsesSteamNamesAndGlobalRanksWhilePaused()
    {
        Time.timeScale=0;
        LeaderboardFixture.Rows=new[]{new LeaderboardEntry_t{m_steamIDUser=new CSteamID{Value=42},m_nGlobalRank=1,m_nScore=12000}};
        LbResp result=null;backend.Fetch("Classic",1,r=>result=r,Assert.Fail);
        yield return Wait(()=>result!=null);
        Assert.That(LeaderboardFixture.Board,Is.EqualTo("BALLARETRO_Classic_1"));
        Assert.That(LeaderboardFixture.Start,Is.EqualTo(1));Assert.That(LeaderboardFixture.End,Is.EqualTo(10));
        Assert.That(result.top10[0].nickname,Is.EqualTo("World Player 42"));
        Assert.That(result.top10[0].rank,Is.EqualTo(1));Assert.That(result.top10[0].score,Is.EqualTo(12000));
    }
    [UnityTest] public IEnumerator SubmittingALowerScoreKeepsTheExistingBest()
    {
        LeaderboardFixture.Best=9000;
        SubmitResp result=null;backend.Submit(2000,"Classic",1,r=>result=r,Assert.Fail);
        yield return Wait(()=>result!=null);
        Assert.That(result.your_score,Is.EqualTo(9000));Assert.That(result.rank,Is.EqualTo(3));
        Assert.That(LeaderboardFixture.Method,Is.EqualTo(ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest));
    }
    [Test] public void LoggedOutRequestsDoNotReachSteamLeaderboards()
    {
        Fixture.LoggedOn=false;string error=null;
        backend.Fetch("Classic",1,_=>Assert.Fail(),e=>error=e);
        Assert.That(error,Is.Not.Null);Assert.That(LeaderboardFixture.Finds,Is.Zero);
    }
    [UnityTest] public IEnumerator AccountChangesCannotUploadThePreviousPlayersRun()
    {
        LeaderboardFixture.Hold=true;string error=null;
        backend.Submit(9000,"Classic",1,_=>Assert.Fail(),e=>error=e);
        Fixture.UserId++;LeaderboardFixture.Hold=false;
        yield return Wait(()=>error!=null);
        Assert.That(LeaderboardFixture.Uploads,Is.Zero);
    }
    [UnityTest] public IEnumerator FailedDownloadAndTimeoutReturnErrorsInsteadOfEmptySuccess()
    {
        LeaderboardFixture.DownloadFails=true;string error=null;
        backend.Fetch("Classic",1,_=>Assert.Fail(),e=>error=e);
        yield return Wait(()=>error!=null);
        LeaderboardFixture.DownloadFails=false;LeaderboardFixture.Hold=true;error=null;
        typeof(BallARetroSteamLeaderboard).GetField("requestTimeout",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(backend,.04f);
        backend.Fetch("Classic",1,_=>Assert.Fail(),e=>error=e);
        yield return Wait(()=>error!=null);
        LeaderboardFixture.Hold=false;yield return null;
    }
    [UnityTest] public IEnumerator ClosingTheBackendCancelsPendingCallbacks()
    {
        LeaderboardFixture.Hold=true;bool callback=false;
        backend.Fetch("Classic",1,_=>callback=true,_=>callback=true);
        backend.enabled=false;LeaderboardFixture.Hold=false;
        yield return null;yield return null;
        Assert.That(callback,Is.False);
    }
}
