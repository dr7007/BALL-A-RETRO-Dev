using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public class SteamIdentityTests
{
    BallARetroSteamSettings settings;
    GameObject input;
    uint originalId;
    readonly FieldInfo appId = typeof(BallARetroSteamSettings).GetField(
        BallARetroPlatform.IsDemoBuild ? "demoAppId" : "steamAppId", BindingFlags.Instance | BindingFlags.NonPublic);
    [SetUp] public void Setup()
    {
        Assert.That(BallARetroPlatform.IsSteamBuild, Is.True);
        settings = Resources.Load<BallARetroSteamSettings>(BallARetroSteamSettings.ResourcePath);
        originalId = settings.AppId;
        appId.SetValue(settings, Steamworks.Fixture.AppId);
        Steamworks.Fixture.Reset();
        typeof(SteamManager).GetMethod("InitOnPlayMode", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    }
    [TearDown] public void Cleanup()
    {
        if (input != null) Object.DestroyImmediate(input);
        foreach (var manager in Object.FindObjectsByType<SteamManager>(FindObjectsSortMode.None)) Object.DestroyImmediate(manager.gameObject);
        appId.SetValue(settings, originalId);
    }
    [Test] public void ConfiguredAppIdIsUsed_AndClientIdentityIsReadLive()
    {
        Assert.That(SteamManager.TryGetLoggedInPlayer(out var name, out var id), Is.True);
        Assert.That(name, Is.EqualTo("Steam Player"));
        Assert.That(id, Is.EqualTo(Steamworks.Fixture.UserId.ToString()));
        Assert.That(Steamworks.Fixture.RestartAppId, Is.EqualTo(Steamworks.Fixture.AppId));
        Steamworks.Fixture.Name = "Renamed Steam Player";
        Assert.That(SteamManager.TryGetLoggedInPlayer(out name, out _), Is.True);
        Assert.That(name, Is.EqualTo("Renamed Steam Player"));
    }
    [TestCase(0u)] [TestCase(480u)]
    public void MissingOrTestAppId_DoesNotInitializeSteam(uint id)
    {
        appId.SetValue(settings, id);
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.False);
        Assert.That(Steamworks.Fixture.InitCalls, Is.Zero);
    }
    [Test] public void WrongAppLaunchContext_IsRejected()
    {
        Steamworks.Fixture.RuntimeAppId = 480;
        LogAssert.Expect(LogType.Error, new Regex("실행된 App ID가"));
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.False);
        Assert.That(Steamworks.Fixture.ShutdownCalls, Is.EqualTo(1));
    }
    [Test] public void LoggedOutClient_DoesNotBecomeAGuestIdentity()
    {
        Steamworks.Fixture.LoggedOn = false;
        Assert.That(SteamManager.TryGetLoggedInPlayer(out var name, out var id), Is.False);
        Assert.That(name, Is.Empty); Assert.That(id, Is.Empty);
    }
    [Test] public void InvalidUserOrEmptyName_IsRejected()
    {
        Steamworks.Fixture.UserId = 0;
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.False);
        Steamworks.Fixture.UserId = 76561198000000001;
        Steamworks.Fixture.Name = "";
        Assert.That(SteamManager.TryGetLoggedInPlayer(out _, out _), Is.False);
    }
    [Test] public void NicknameFieldUsesCurrentSteamName_AndClearsAfterLogout()
    {
        input = new GameObject("Nickname", typeof(RectTransform), typeof(TMP_InputField), typeof(KHS_Script_SteamNickLoader));
        var field = input.GetComponent<TMP_InputField>();
        Assert.That(field.readOnly, Is.True);
        Assert.That(field.text, Is.EqualTo("Steam Player"));
        Steamworks.Fixture.Name = "Changed";
        input.GetComponent<KHS_Script_SteamNickLoader>().FillInputFieldToSteamNickname();
        Assert.That(field.text, Is.EqualTo("Changed"));
        Steamworks.Fixture.LoggedOn = false;
        input.GetComponent<KHS_Script_SteamNickLoader>().FillInputFieldToSteamNickname();
        Assert.That(field.text, Is.Empty);
    }
}
