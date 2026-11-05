using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PSH;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class RunProgressionTests
{
    private class TestDatabase : LocalizedStringDatabase
    {
        public StringTable UI, Dialogue;
        public override StringTable GetTable(TableReference reference, Locale locale = null)
            => reference.TableCollectionName == "UITable" ? UI : Dialogue;
    }
    private readonly List<Object> objects = new List<Object>();
    private KHS_Script_ScoreManager manager;
    private YJ_Script_BallController ball;
    private KHS_Script_FliperController flippers;
    private CJS_Script_PinballRankingService service;
    private TMP_Text scoreText, ballText, resultScore, summary;
    private GameObject canvas, endingPanel;
    private PSH_Script_DialogueUI dialogue;
    private TestDatabase database;
    private int gameOvers, clears, rounds;
    private static readonly int[] ClearScores = { 6000, 7150, 20000 };

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        var settings = Keep(ScriptableObject.CreateInstance<LocalizationSettings>());
        LocalizationSettings.Instance = settings;
        database = new TestDatabase();
        database.UI = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/UITable_ko.asset");
        database.Dialogue = Keep(ScriptableObject.CreateInstance<StringTable>());
        database.Dialogue.SharedData = Keep(ScriptableObject.CreateInstance<SharedTableData>());
        database.Dialogue.SharedData.TableCollectionName = "DialogueTable";
        database.Dialogue.AddEntry("Ending2_01", "Last dialogue line");
        database.Dialogue.AddEntry("Ending1_01", "Successful run dialogue");
        LocalizationSettings.StringDatabase = database;

        service = New("Ranking").AddComponent<CJS_Script_PinballRankingService>();
        canvas = New("Canvas_GameScene", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scoreText = MakeText("Score", canvas.transform, new Vector2(-240, -30), new Vector2(400, 60));
        scoreText.gameObject.AddComponent<KHS_Script_UIToScore>();
        ballText = MakeText("BallCredits", canvas.transform, new Vector2(240, -30), new Vector2(400, 60));
        endingPanel = New("Ending", typeof(RectTransform));
        endingPanel.transform.SetParent(canvas.transform, false);
        var endRect = (RectTransform)endingPanel.transform;
        endRect.anchorMin = Vector2.zero;
        endRect.anchorMax = Vector2.one;
        endRect.sizeDelta = Vector2.zero;
        // Main_Scene_Game overrides the prefab's text width to 816 reference pixels.
        summary = MakeText("Summary", endingPanel.transform, new Vector2(0, -160), new Vector2(816f, 90.01f));
        resultScore = MakeText("FinalScore", endingPanel.transform, new Vector2(0, -360), new Vector2(600, 100));
        var result = endingPanel.AddComponent<CJS_Script_GameOverUI>();
        result.textFinalScore = resultScore;

        var flipperGO = New("Flippers");
        flipperGO.SetActive(false);
        flippers = flipperGO.AddComponent<KHS_Script_FliperController>();
        Set(flippers, "flippers", new YJ_Script_Flipper[0]);
        flippers.fliper_Count = 10;
        flipperGO.SetActive(true);

        var ballGO = New("Ball", typeof(Rigidbody));
        ballGO.GetComponent<Rigidbody>().useGravity = false;
        ballGO.GetComponent<Rigidbody>().isKinematic = true;
        ball = ballGO.AddComponent<YJ_Script_BallController>();
        ball.SetBallCount(1);
        ballText.gameObject.AddComponent<KHS_Script_UIToBall>();

        var camHolder = New("CameraHolder").AddComponent<KHS_Script_CameraManager>();
        camHolder.cameraGos = new[] { New("PlayCamera", typeof(Camera)) };
        var managerGO = New("ScoreManager");
        managerGO.SetActive(false);
        manager = managerGO.AddComponent<KHS_Script_ScoreManager>();
        manager.targetScores = new[] { 2000, 4000, 6000 };
        Set(manager, "goalRound", 3);
        Set(manager, "currentgameScores", new int[3]);
        Set(manager, "camHolder", camHolder);
        Set(manager, "gameOverUI", result);
        managerGO.SetActive(true);

        var dialogueGO = New("Dialogue");
        dialogueGO.SetActive(false);
        dialogue = dialogueGO.AddComponent<PSH_Script_DialogueUI>();
        Set(dialogue, "endingPanelRoot", endingPanel);
        Set(dialogue, "endingText", summary);
        Set(dialogue, "charInterval", 10f);
        dialogueGO.SetActive(true);
#if !BASELINE
        var feedback = canvas.AddComponent<PSH_Script_RunStatusUI>();
        Set(feedback, "resultSummaryText", summary);
#endif
        KHS_Script_ScoreManager.OnGameOver += Ended;
        KHS_Script_ScoreManager.OnGameClear += Cleared;
        KHS_Script_ScoreManager.Round_Clear += RoundCleared;
        KHS_Script_FliperController.FliperCountChangeEvt += FlipperCountChanged;
        gameOvers = clears = rounds = 0;
        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        KHS_Script_ScoreManager.OnGameOver -= Ended;
        KHS_Script_ScoreManager.OnGameClear -= Cleared;
        KHS_Script_ScoreManager.Round_Clear -= RoundCleared;
        KHS_Script_FliperController.FliperCountChangeEvt -= FlipperCountChanged;
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear();
        LocalizationSettings.Instance = null;
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator FirstTwoGoalsStillAdvanceRounds()
    {
        manager.AddScore(2000);
        yield return null;
        yield return null;
        Assert.That(manager.RoundRespone(), Is.EqualTo(2));
        Assert.That(manager.targetScore, Is.EqualTo(4000));
        manager.AddScore(2000);
        yield return null;
        yield return null;
        Assert.That(manager.RoundRespone(), Is.EqualTo(3));
        Assert.That(manager.targetScore, Is.EqualTo(6000));
        Assert.That(clears + gameOvers, Is.Zero);
    }

    [UnityTest]
    public IEnumerator SixThousandKeepsBallAndScoringAliveWithoutRoundTransition()
    {
        FinalRound(5990);
        manager.AddScore(10);
        yield return null;
        yield return null;
        manager.AddScore(1150);
        Assert.That(clears + gameOvers + rounds, Is.Zero);
        Assert.That(manager.curScore, Is.EqualTo(7150));
        Assert.That(scoreText.text, Is.EqualTo("7150"));
        Assert.That(ball.gameObject.activeSelf, Is.True);
        Assert.That(ball.GetBallCount(), Is.EqualTo(1));
        Assert.That(service.Submissions, Is.Zero);
    }

    [Test]
    public void LastScoreAndZeroBallCreditsAreVisibleInTheSameFrameAsGameOver()
    {
        FinalRound(4340);
        manager.AddScore(10);
        KHS_Script_BallOutController.Drain();
        Assert.That(scoreText.text, Is.EqualTo("4350"));
        Assert.That(resultScore.text, Is.EqualTo("4,350"));
        Assert.That(ballText.text, Is.EqualTo("0"));
        Assert.That(manager.curScore, Is.EqualTo(4350));
    }

    [Test]
    public void EndedRunRejectsLateBonusAndDuplicateDrainSubmissions()
    {
        FinalRound(4350);
        KHS_Script_BallOutController.Drain();
        manager.AddScore(20000);
        manager.MultiplyScore(5);
        manager.HandleBallOut(ball);
        Assert.That(manager.curScore, Is.EqualTo(4350));
        Assert.That(service.Submissions, Is.EqualTo(1));
        Assert.That(service.LastScore, Is.EqualTo(4350));
        Assert.That(gameOvers, Is.EqualTo(1));
    }

    [Test]
    public void ExtraBallRestoresFlippersAndDoesNotEndRun()
    {
        ball.SetBallCount(2);
        flippers.fliper_Count = 0;
        KHS_Script_BallOutController.Drain();
        Assert.That(ball.GetBallCount(), Is.EqualTo(1));
        Assert.That(flippers.fliper_Count, Is.EqualTo(10));
        Assert.That(gameOvers, Is.Zero);
        Assert.That(service.Submissions, Is.Zero);
    }

#if !BASELINE
    [Test]
    public void MultiplierCrossesGoalOnceThenContinuesScoring()
    {
        FinalRound(3000);
        manager.MultiplyScore(2);
        Assert.That(manager.IsRecordChallenge, Is.True);
        var notification = canvas.transform.Find("RecordChallengeNotification");
        manager.MultiplyScore(2);
        manager.AddScore(50);
        Assert.That(manager.curScore, Is.EqualTo(12050));
        Assert.That(canvas.transform.Find("RecordChallengeNotification"), Is.EqualTo(notification));
        Assert.That(rounds + clears + gameOvers, Is.Zero);
    }

    [Test]
    public void FlipperExhaustionWaitsForDrainAndPreservesCause()
    {
        FinalRound(4350);
        flippers.fliper_Count = 0;
        manager.AddScore(50);
        Assert.That(gameOvers, Is.Zero);
        KHS_Script_BallOutController.Drain();
        Assert.That(manager.EndReason, Is.EqualTo(KHS_Script_ScoreManager.GameOverReason.FlippersExhausted));
        Assert.That(flippers.fliper_Count, Is.Zero);
        Assert.That(manager.FinalUserScore, Is.EqualTo(4400));
    }

    [Test]
    public void TargetMissAndReasonReplaceLastDialogueWithoutChangingSubmittedScore()
    {
        FinalRound(4350);
        flippers.fliper_Count = 0;
        KHS_Script_BallOutController.Drain();
        FinishDialogue();
        Assert.That(summary.text, Does.Contain("플리퍼 횟수 소진"));
        Assert.That(summary.text, Does.Contain("4,350 / 6,000점 (1,650점 부족)"));
        Assert.That(summary.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
        Assert.That(service.LastScore, Is.EqualTo(4350));
    }

    [Test]
    public void ChallengeEndsOnlyOnFinalBallAndReportsTargetAchieved()
    {
        FinalRound(4350);
        manager.AddScore(2800);
        Assert.That(service.Submissions, Is.Zero);
        KHS_Script_BallOutController.Drain();
        FinishDialogue();
        Assert.That(summary.text, Does.Contain("클리어 · 기록 도전 종료"));
        Assert.That(summary.text, Does.Not.Contain("게임 오버"));
        Assert.That(summary.text, Does.Contain("최종 7,150점 · 목표 6,000점 달성"));
        Assert.That(scoreText.text, Is.EqualTo("7150"));
        Assert.That(resultScore.text, Is.EqualTo("7,150"));
        Assert.That(service.Submissions, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ClearedRunPlaysFireworksAndSuccessDialogueOnlyAfterFinalBall(
        [ValueSource(nameof(ClearScores))] int score)
    {
        var sequence = CreateEndSequence();
        yield return null; // Allow the production director to initialize its actors.
        FinalRound(5990);
        manager.AddScore(score - 5990);
        ball.SetBallCount(2);
        Assert.That(sequence.Fireworks.activeSelf, Is.False);
        Assert.That(gameOvers + clears, Is.Zero);

        KHS_Script_BallOutController.Drain();
        Assert.That(sequence.Fireworks.activeSelf, Is.False);
        Assert.That(ball.gameObject.activeSelf, Is.True);
        Assert.That(service.Submissions, Is.Zero);
        flippers.fliper_Count = 0; // Exhaustion must not undo an already earned clear.
        KHS_Script_BallOutController.Drain();
        Assert.That(sequence.Fireworks.activeSelf, Is.True);
        Assert.That(clears, Is.EqualTo(1));
        Assert.That(gameOvers, Is.Zero);
        Assert.That(scoreText.text, Is.EqualTo(score.ToString()));
        Assert.That(ballText.text, Is.EqualTo("0"));
        Assert.That(manager.curScore, Is.EqualTo(score));
        Assert.That(manager.FinalTargetScore, Is.EqualTo(6000));
        Assert.That(canvas.transform.Find("RecordChallengeNotification").gameObject.activeSelf, Is.False);

        yield return null;
        yield return null;
        Assert.That(Get(dialogue, "currentCutsceneId"), Is.EqualTo("Ending1"));
        Assert.That(sequence.Retry.interactable, Is.False);
        dialogue.SkipDialogue();
        yield return null;
        yield return null;
        Assert.That(sequence.ClearPanel.activeSelf, Is.True);
        Assert.That(sequence.FailurePanel.activeSelf, Is.False);
        Assert.That(sequence.Retry.interactable, Is.True);
        Assert.That(summary.text, Does.Contain("클리어 · 기록 도전 종료"));
        Assert.That(summary.text, Does.Contain("플리퍼 횟수 소진"));
        summary.ForceMeshUpdate();
        Assert.That(summary.isTextOverflowing, Is.False);
        Capture($"clear-{score}-ko.png");

        database.UI = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/UITable_en.asset");
        Invoke(canvas.GetComponent<PSH_Script_RunStatusUI>(), "OnLocaleChanged", (Locale)null);
        summary.ForceMeshUpdate();
        Assert.That(summary.text, Does.Contain("Clear"));
        Assert.That(summary.text, Does.Not.Contain("Game over"));
        Assert.That(summary.isTextOverflowing, Is.False);
        manager.AddScore(20000);
        manager.MultiplyScore(5);
        manager.HandleBallOut(ball);
        Assert.That(manager.curScore, Is.EqualTo(score));
        Assert.That(service.Submissions, Is.EqualTo(1));
        Assert.That(service.LastScore, Is.EqualTo(score));
        Assert.That(resultScore.text, Is.EqualTo(score.ToString("N0")));
    }

    [UnityTest]
    public IEnumerator BelowGoalPlaysFailureDialogueWithoutFireworks()
    {
        var sequence = CreateEndSequence();
        yield return null;
        FinalRound(5990);
        manager.AddScore(9);
        KHS_Script_BallOutController.Drain();
        Assert.That(sequence.Fireworks.activeSelf, Is.False);
        Assert.That(gameOvers, Is.EqualTo(1));
        Assert.That(clears, Is.Zero);
        yield return null;
        yield return null;
        Assert.That(Get(dialogue, "currentCutsceneId"), Is.EqualTo("Ending2"));
        dialogue.SkipDialogue();
        Assert.That(sequence.FailurePanel.activeSelf, Is.True);
        Assert.That(sequence.ClearPanel.activeSelf, Is.False);
        Assert.That(summary.text, Does.Contain("게임 오버"));
        Assert.That(summary.text, Does.Contain("1점 부족"));
    }

    private (GameObject Fireworks, GameObject ClearPanel, GameObject FailurePanel, Button Retry) CreateEndSequence()
    {
        var fireworks = New("Fireworks");
        var clearPanel = New("ClearResult");
        var failurePanel = New("FailureResult");
        var finalUI = canvas.AddComponent<KHS_Script_GameOverUI>();
        Set(finalUI, "gameClearUIObj", clearPanel);
        Set(finalUI, "gameOverUIObj", failurePanel);
        var directorGO = New("EndSequence");
        directorGO.SetActive(false);
        var director = directorGO.AddComponent<PSH_Script_GameOverSequenceDirector>();
        var retry = New("Retry", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
        var lobby = New("Lobby", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
        Set(director, "finalUIScript", finalUI);
        Set(director, "fireworkObject", fireworks);
        Set(director, "fireworkDuration", 0f);
        Set(director, "glitchDuration", 0f);
        Set(director, "resetBtn", retry);
        Set(director, "lobbyBtn", lobby);
        directorGO.SetActive(true);
        return (fireworks, clearPanel, failurePanel, retry);
    }

    [UnityTest]
    public IEnumerator NotificationNeverPausesOrCapturesClicksAndIsNotRepeated()
    {
        FinalRound(5990);
        manager.AddScore(10);
        var notification = canvas.transform.Find("RecordChallengeNotification").gameObject;
        foreach (var graphic in notification.GetComponentsInChildren<Graphic>()) Assert.That(graphic.raycastTarget, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(notification.GetComponentInChildren<TMP_Text>().text, Does.Contain("최고 기록"));
        Capture("goal-ko.png");
        var feedback = canvas.GetComponent<PSH_Script_RunStatusUI>();
        Set(feedback, "notificationRemaining", 0.001f);
        yield return null;
        yield return null;
        Assert.That(notification.activeSelf, Is.False);
        manager.AddScore(500);
        Assert.That(notification.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator KoreanAndEnglishResultFitTheExistingDialogueRectangle()
    {
        FinalRound(4350);
        flippers.fliper_Count = 0;
        KHS_Script_BallOutController.Drain();
        FinishDialogue();
        yield return null;
        summary.ForceMeshUpdate();
        Assert.That(summary.isTextOverflowing, Is.False);
        Assert.That(summary.textInfo.lineCount, Is.LessThanOrEqualTo(2));
        Capture("result-ko.png");
        database.UI = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/UITable_en.asset");
        Invoke(canvas.GetComponent<PSH_Script_RunStatusUI>(), "OnLocaleChanged", (Locale)null);
        yield return null;
        summary.ForceMeshUpdate();
        Assert.That(summary.text, Does.Contain("Goal missed"));
        Assert.That(summary.isTextOverflowing, Is.False);
        Assert.That(summary.textInfo.lineCount, Is.LessThanOrEqualTo(2));
        Capture("result-en.png");
        yield return null;
    }
#endif
    private void FinalRound(int score)
    {
        Set(manager, "currentRound", 3);
        manager.targetScore = 6000;
        manager.curScore = score;
    }
    private void FinishDialogue() { dialogue.Play(clears > 0 ? "Ending1" : "Ending2"); dialogue.SkipDialogue(); }
    private void Ended() => gameOvers++;
    private void Cleared() => clears++;
    private void RoundCleared() => rounds++;
    private void FlipperCountChanged() { } // The scene's flipper counter subscribes here.
    private T Keep<T>(T value) where T : Object { objects.Add(value); return value; }
    private GameObject New(string name, params System.Type[] components) => Keep(new GameObject(name, components));
    private TMP_Text MakeText(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = New(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Galmuri11-Bold SDF.asset");
        text.fontSize = 36;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = size;
        return text;
    }
    private static void Set(object target, string field, object value)
        => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Get(object target, string field)
        => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private void Capture(string file)
    {
        // Explicit rendering works in batchmode; CaptureScreenshot requires a Game view.
        var cameraGO = new GameObject("PreviewCamera", typeof(Camera));
        var camera = cameraGO.GetComponent<Camera>();
        var target = new RenderTexture(1920, 1080, 24);
        var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var uiCanvas = canvas.GetComponent<Canvas>();
        var previousTarget = RenderTexture.active;
        try
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.04f, 0.08f);
            camera.targetTexture = target;
            uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            uiCanvas.worldCamera = camera;
            uiCanvas.planeDistance = 10;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            texture.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", file)), texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previousTarget;
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.worldCamera = null;
            Object.DestroyImmediate(cameraGO);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
        }
    }
}
