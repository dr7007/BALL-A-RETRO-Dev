using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AudioSettingsTests
{
    readonly List<Object> objects = new();
    readonly Dictionary<string, float> saved = new();
    static readonly string[] Keys = { "Settings_Lobby_BGM", "Settings_Game_BGM", "Settings_SFX_Vol" };
    [SetUp] public void Setup()
    {
        foreach (string key in Keys)
            if (PlayerPrefs.HasKey(key)) saved[key] = PlayerPrefs.GetFloat(key);
        PlayerPrefs.SetFloat(Keys[0], .23f);
        PlayerPrefs.SetFloat(Keys[1], .31f);
        PlayerPrefs.SetFloat(Keys[2], .47f);
    }
    [TearDown] public void Cleanup()
    {
        foreach (var item in objects) if (item != null) Object.DestroyImmediate(item);
        objects.Clear();
        foreach (string key in Keys)
            if (saved.TryGetValue(key, out float value)) PlayerPrefs.SetFloat(key, value);
            else PlayerPrefs.DeleteKey(key);
        saved.Clear();
        PlayerPrefs.Save();
    }
    T Create<T>() where T : Component
    {
        var go = new GameObject(typeof(T).Name);
        objects.Add(go);
        return go.AddComponent<T>();
    }
    [Test] public void LobbyRestoresSavedVolume_WhenAudioStartsAfterSettingsUI()
    {
        Create<PSH_Script_UI_SoundBridge>();
        var bgm = Create<CJS_Script_LobbyBGM>();
        Assert.That(bgm.volume, Is.EqualTo(.23f).Within(.001f));
    }
    [Test] public void GameRestoresSavedVolumes_WithoutDependingOnSettingsPanelAwake()
    {
        var director = Create<CJS_Script_AudioDirector>();
        Assert.That(director.bgmVolume, Is.EqualTo(.31f).Within(.001f));
        Assert.That(director.sfxVolume, Is.EqualTo(.47f).Within(.001f));
    }
    [UnityTest] public IEnumerator MutingDuringLobbyFade_RemainsMuted()
    {
        var bgm = Create<CJS_Script_LobbyBGM>();
        bgm.playOnAwake = false;
        bgm.bgmClip = AudioClip.Create("fade-test", 44100, 1, 44100, false);
        objects.Add(bgm.bgmClip);
        bgm.fadeInSeconds = .12f;
        bgm.Play(true);
        yield return null;
        bgm.SetVolume(0f);
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(bgm.GetComponent<AudioSource>().volume, Is.Zero.Within(.001f));
    }
    [Test] public void SfxVolume_IsAppliedOnceByPlayOneShot()
    {
        var director = Create<CJS_Script_AudioDirector>();
        director.SyncAllVolumes(.3f, .5f);
        Assert.That(director.sfxSource.volume, Is.EqualTo(1f));
        Assert.That(director.sfxVolume, Is.EqualTo(.5f));
    }
}
