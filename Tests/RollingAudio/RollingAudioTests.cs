using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class RollingAudioTestRoller : MonoBehaviour, CJS_IChoiceRoller
{
    public int Picks;
    public List<CJS_ChoiceData> Roll3(out Dictionary<CJS_ChoiceData, float> chances)
    {
        chances = null;
        return new List<CJS_ChoiceData> { new CJS_ChoiceData(), new CJS_ChoiceData(), new CJS_ChoiceData() };
    }
    public void PushPicked(CJS_ChoiceData picked) { Picks++; }
}

public class RollingAudioTests
{
    private readonly List<Object> objects = new List<Object>();
    private YJ_Script_BallController ball;
    private Rigidbody body;
    private AudioSource rolling, bgm;
    private CJS_Script_ChoiceUIController choices;
    private RollingAudioTestRoller roller;
    private Transform content;
    private Button skip;
    private float previousTimeScale;
    private bool previousListenerPause;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousTimeScale = Time.timeScale;
        previousListenerPause = AudioListener.pause;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Keep(new GameObject("Test listener", typeof(AudioListener)));
        var clip = Keep(AudioClip.Create("Silent rolling fixture", 48000, 1, 48000, false));
        var go = Keep(new GameObject("Ball", typeof(Rigidbody), typeof(AudioSource)));
        go.SetActive(false);
        body = go.GetComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        rolling = go.GetComponent<AudioSource>();
        rolling.playOnAwake = false;
        ball = go.AddComponent<YJ_Script_BallController>();
        SetField(ball, "rollingSource", rolling);
        SetField(ball, "sfxRollingLoop", clip);
        SetField(ball, "Gravity", 0f);
        ball.SetBallCount(1);
        go.SetActive(true);

        bgm = Keep(new GameObject("BGM", typeof(AudioSource))).GetComponent<AudioSource>();
        bgm.clip = clip;
        bgm.loop = true;
        bgm.volume = 0.6f;
        bgm.Play();

        var ui = Keep(new GameObject("Choice UI"));
        ui.SetActive(false);
        roller = ui.AddComponent<RollingAudioTestRoller>();
        var panel = Keep(new GameObject("Choice panel", typeof(RectTransform)));
        content = new GameObject("Cards", typeof(RectTransform)).transform;
        content.SetParent(panel.transform, false);
        skip = MakeButton("Skip", panel.transform);

        var template = Keep(new GameObject("Card template", typeof(RectTransform), typeof(Image), typeof(Button)));
        template.SetActive(false);
        var card = template.AddComponent<CJS_Script_ChoiceCard>();
        SetField(card, "btn", template.GetComponent<Button>());
        template.SetActive(true);

        choices = ui.AddComponent<CJS_Script_ChoiceUIController>();
        SetField(choices, "rollerBehaviour", roller);
        SetField(choices, "panelRoot", panel);
        SetField(choices, "content", content);
        SetField(choices, "cardPrefab", card);
        SetField(choices, "ballCon", ball);
        SetField(choices, "btnSkip", skip);
        ui.SetActive(true);
        yield return null; // Let the real BallController.Start initialize its motion/audio state.
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] is GameObject) Object.DestroyImmediate(objects[i]);
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear();
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousListenerPause;
    }

    [UnityTest]
    public IEnumerator OpeningChoicesStopsRollingAndItsFadeWithoutMutingBgm()
    {
        yield return StartRolling();
        var velocity = body.linearVelocity;
        bool bgmWasPlaying = bgm.isPlaying;
        choices.ShowChoices();
        Assert.That(Time.timeScale, Is.Zero);
        yield return null;
        yield return null;
        AssertSilent();
        yield return new WaitForSecondsRealtime(0.2f);
        AssertSilent(); // An unscaled fade must not restore the volume while paused.
        Assert.That(body.linearVelocity, Is.EqualTo(velocity));
        Assert.That(AudioListener.pause, Is.False);
        Assert.That(bgm.volume, Is.EqualTo(0.6f));
        Assert.That(bgm.isPlaying, Is.EqualTo(bgmWasPlaying));
    }

    [UnityTest]
    public IEnumerator PickingCardResumesRollingWhenBallMoves()
    {
        yield return StartRolling();
        choices.ShowChoices();
        yield return null;
        yield return null;
        AssertSilent();
        content.GetComponentInChildren<Button>().onClick.Invoke();
        Assert.That(roller.Picks, Is.EqualTo(1));
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(rolling.volume, Is.GreaterThan(0f));
    }

    [UnityTest]
    public IEnumerator SkipResumesRollingWhenBallMoves()
    {
        yield return StartRolling();
        choices.ShowChoices();
        yield return null;
        yield return null;
        AssertSilent();
        skip.onClick.Invoke();
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(rolling.volume, Is.GreaterThan(0f));
    }

    [UnityTest]
    public IEnumerator StationaryBallDoesNotResumeFromStaleSpeedOrPausedRelocation()
    {
        yield return StartRolling();
        choices.ShowChoices();
        body.linearVelocity = Vector3.zero;
        ball.transform.position += Vector3.right * 100f;
        yield return null;
        yield return null;
        AssertSilent();
        skip.onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.15f);
        AssertSilent();
    }

    [UnityTest]
    public IEnumerator RepeatedChoiceOpenAndCloseDoesNotLeaveSoundPaused()
    {
        yield return StartRolling();
        for (int i = 0; i < 3; i++)
        {
            choices.ShowChoices();
            yield return null;
            yield return null;
            AssertSilent();
            skip.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(rolling.volume, Is.GreaterThan(0f));
        }
    }

    [UnityTest]
    public IEnumerator SlowMotionKeepsRollingAudioActive()
    {
        yield return StartRolling();
        Time.timeScale = 0.25f;
        yield return new WaitForSecondsRealtime(0.25f);
        Assert.That(rolling.volume, Is.GreaterThan(0f));
    }

    [UnityTest]
    public IEnumerator DisablingBallStopsRollingEvenDuringPause()
    {
        yield return StartRolling();
        choices.ShowChoices();
        ball.enabled = false;
        yield return new WaitForSecondsRealtime(0.2f);
        AssertSilent();
    }

    private IEnumerator StartRolling()
    {
        body.linearVelocity = Vector3.right * 6f;
        yield return new WaitForSecondsRealtime(0.25f);
        Assert.That(rolling.volume, Is.GreaterThan(0f), "Fixture must establish rolling before opening the menu.");
    }

    private void AssertSilent()
    {
        Assert.That(rolling.volume, Is.Zero);
        Assert.That(rolling.isPlaying, Is.False);
    }

    private Button MakeButton(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Button>();
    }

    private T Keep<T>(T value) where T : Object { objects.Add(value); return value; }
    private void SetField(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
}
