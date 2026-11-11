using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PersonalRecordTests
{
    string mode, oldNickname;
    GameObject serviceObject;
    [SetUp] public void Setup()
    {
        mode = "Test-" + Guid.NewGuid().ToString("N");
        oldNickname = PlayerPrefs.GetString("nickname", "");
    }
    [TearDown] public void Cleanup()
    {
        if (serviceObject != null) UnityEngine.Object.DestroyImmediate(serviceObject);
        foreach (var key in new[] { mode, mode + "Other" })
            for (int level = 1; level <= 2; level++) PlayerPrefs.DeleteKey(BallARetroPersonalRecordStore.StorageKey(key, level));
        PlayerPrefs.SetString("nickname", oldNickname);
        PlayerPrefs.Save();
    }
    [Test] public void BestScoreSurvivesLowerScoresAndNicknameChanges()
    {
        BallARetroPersonalRecordStore.Submit("First", 9000, mode, 1);
        BallARetroPersonalRecordStore.Submit("Renamed", 4000, mode, 1);
        var rows = BallARetroPersonalRecordStore.Read("Renamed", mode, 1);
        Assert.That(rows.Length, Is.EqualTo(1));
        Assert.That(rows[0].score, Is.EqualTo(9000));
        Assert.That(rows[0].nickname, Is.EqualTo("Renamed"));
        Assert.That(BallARetroPersonalRecordStore.Submit("Renamed", 10000, mode, 1)[0].score, Is.EqualTo(10000));
    }
    [Test] public void DifferentModesAndLevels_DoNotOverwriteEachOther()
    {
        BallARetroPersonalRecordStore.Submit("Me", 1, mode, 1);
        BallARetroPersonalRecordStore.Submit("Me", 2, mode, 2);
        BallARetroPersonalRecordStore.Submit("Me", 3, mode + "Other", 1);
        Assert.That(BallARetroPersonalRecordStore.Read("Me", mode, 1)[0].score, Is.EqualTo(1));
        Assert.That(BallARetroPersonalRecordStore.Read("Me", mode, 2)[0].score, Is.EqualTo(2));
    }
    [Test] public void NoRecord_IsEmptyAndZeroIsAValidFirstRecord()
    {
        Assert.That(BallARetroPersonalRecordStore.Read("Me", mode, 1), Is.Empty);
        Assert.That(BallARetroPersonalRecordStore.Submit("Me", 0, mode, 1).Length, Is.EqualTo(1));
    }
    [Test] public void NewRulesPreserveLegacyBestWithoutImportingIt()
    {
        string current = BallARetroPersonalRecordStore.StorageKey(mode, 1);
        string legacy = current.Replace(".v2.", ".v1.");
        Assert.That(current, Does.Contain(".v2."));
        PlayerPrefs.SetString(legacy, "legacy-record-sentinel");
        try
        {
            Assert.That(BallARetroPersonalRecordStore.Read("Me", mode, 1), Is.Empty);
            BallARetroPersonalRecordStore.Submit("Me", 500, mode, 1);
            Assert.That(PlayerPrefs.GetString(legacy), Is.EqualTo("legacy-record-sentinel"));
        }
        finally { PlayerPrefs.DeleteKey(legacy); }
    }
    [Test] public void InvalidSavedData_DoesNotPreventAReplacementRecord()
    {
        PlayerPrefs.SetString(BallARetroPersonalRecordStore.StorageKey(mode, 1), "broken json");
        Assert.That(BallARetroPersonalRecordStore.Read("Me", mode, 1), Is.Empty);
        Assert.That(BallARetroPersonalRecordStore.Submit("Me", 300, mode, 1)[0].score, Is.EqualTo(300));
        Assert.Throws<ArgumentOutOfRangeException>(() => BallARetroPersonalRecordStore.Submit("Me", -1, mode, 1));
    }
    [UnityTest] public IEnumerator StoveService_CompletesOfflineAndReturnsOnlyMyBest()
    {
        Assert.That(BallARetroPlatform.UsesPersonalRecords, Is.True);
        serviceObject = new GameObject("Ranking", typeof(CJS_Script_PinballRankingService));
        var service = serviceObject.GetComponent<CJS_Script_PinballRankingService>();
        service.Nickname = "Me";
        bool registered = false;
        service.RegisterNickname(() => registered = true, Assert.Fail);
        yield return null; yield return null;
        Assert.That(registered, Is.True);
        SubmitResp submit = null;
        service.SubmitScore(7500, mode, 1, value => submit = value, Assert.Fail);
        yield return null; yield return null;
        Assert.That(submit.ok, Is.True);
        Assert.That(submit.top10.Length, Is.EqualTo(1));
        LbResp board = null;
        service.FetchLeaderboard(mode, 1, value => board = value, Assert.Fail);
        yield return null; yield return null;
        Assert.That(board.top10[0].nickname, Is.EqualTo("Me"));
        Assert.That(board.top10[0].score, Is.EqualTo(7500));
    }
    [Test] public void StoveDoesNotInitializeSteam()
    {
        Assert.That(SteamManager.Initialized, Is.False);
        var go = new GameObject("Steam", typeof(SteamManager));
        Assert.That(go.GetComponent<SteamManager>().GetSteamNickname(), Is.EqualTo("Unknown"));
        UnityEngine.Object.DestroyImmediate(go);
    }
}
