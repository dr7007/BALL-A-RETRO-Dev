#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PSH;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class FullGameSceneAudit
{
    const string Phase = "BallARetro.FullAudit.Phase";
    public const string Output = "BallARetro.FullAudit.Output";
    static FullGameSceneAudit() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run()
    {
        string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-auditOutput");
        if (i < 0 || i + 1 == args.Length) throw new ArgumentException("-auditOutput required");
        if (BallARetroPlatform.IsDemoBuild) throw new InvalidOperationException("Use full-game symbols");
        SessionState.SetString(Output, args[i + 1]);
        SessionState.SetBool("BallARetro.FullAudit.Baseline", args.Contains("-auditBaseline"));
        SessionState.SetInt(Phase, 1);
        EditorSceneManager.OpenScene("Assets/MainScenes/Main_Scene_Lobby.unity");
        EditorApplication.isPlaying = true;
    }
    static void OnState(PlayModeStateChange state)
    {
        if (SessionState.GetInt(Phase, 0) == 1 && state == PlayModeStateChange.EnteredPlayMode)
        { SessionState.SetInt(Phase, 2); new GameObject("Full scene audit").AddComponent<FullGameSceneProbe>(); }
    }
    public static void Exit(int code) { SessionState.SetInt(Phase, 0); EditorApplication.Exit(code); }
}

public sealed class FullGameSceneProbe : MonoBehaviour
{
    readonly List<string> checks = new(), errors = new();
    readonly List<string> expectedOfflineMessages = new();
    string output; bool finished; float started;
    KHS_Script_ScoreManager score; YJ_Script_BallController ball;
    KHS_Script_CameraManager cameras; CJS_Script_ChoiceUIController choices;
    [Serializable] class Report { public bool success; public string[] checks, errors, expectedOfflineMessages; public string scope = "Real scenes and serialized UI callbacks; stationary ball and injected accepted scores. Steam SDK deliberately offline; no manual physics or authentication claim."; }
    [Serializable] class FrameReport { public string unity, scene, scope; public int width, height, frames; public double medianMs, p95Ms; }
    void Awake()
    {
        DontDestroyOnLoad(gameObject); output = SessionState.GetString(FullGameSceneAudit.Output, "");
        started = Time.realtimeSinceStartup; Application.logMessageReceived += OnLog; StartCoroutine(Guarded());
    }
    void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error && !SteamManager.Initialized && message == "[GameOverUI] Submit fail: Steam login required")
        { expectedOfflineMessages.Add(message); return; }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + trace);
    }
    void Update() { if (!finished && Time.realtimeSinceStartup - started > 180) { errors.Add("Watchdog expired"); Finish(); } }
    IEnumerator Guarded()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Run());
        while (!finished && stack.Count > 0)
        {
            var current = stack.Peek(); bool next;
            try { next = current.MoveNext(); } catch (Exception ex) { errors.Add(ex.ToString()); Finish(); yield break; }
            if (!next) { stack.Pop(); continue; }
            if (current.Current is IEnumerator nested) stack.Push(nested); else yield return current.Current;
        }
        if (!finished) Finish();
    }
    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(4);
        Check(!BallARetroPlatform.IsDemoBuild && !SteamManager.Initialized, "Full game; Steam SDK isolated");
        var notice = Object.FindFirstObjectByType<BallARetroStartupNotice>();
        if (notice != null)
        {
            var button = notice.transform.Find("Notice/Continue").GetComponent<Button>();
            yield return WaitFor(() => button.interactable, "Notice reading delay completes"); button.onClick.Invoke();
            yield return WaitFor(() => !BallARetroStartupNotice.IsVisible, "Notice closes through real button");
        }
        var lobby = Object.FindFirstObjectByType<PSH_Script_LobbyUIManager>();
        lobby.OnClick_Tutorial();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "Main_Scene_Tutorial", "Lobby loads tutorial");
        yield return new WaitForSecondsRealtime(2);
        var tutorial = Object.FindFirstObjectByType<CJS_TutorialDirector>();
        Check(tutorial.scoreManager.IsTutorialRun && (BallARetroRunBuild.Current == null || !BallARetroRunBuild.Current.IsEnabled), "Tutorial excludes full build rules");
        tutorial.GoGame();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "Main_Scene_Game", "Tutorial loads main game");
        yield return null; yield return null; Bind();
        yield return WaitFor(() => Read<string>(PSH_Script_DialogueUI.Instance, "currentCutsceneId") == "Intro", "Intro starts");
        PSH_Script_DialogueUI.Instance.SkipDialogue();
        yield return WaitFor(() => Time.timeScale == 1 && cameras.cameraGos[0].activeInHierarchy, "Intro returns to gameplay");
        yield return new WaitForSecondsRealtime(2);
        ball.GetComponent<Rigidbody>().isKinematic = true;
        yield return MeasureFrames();
        if (SessionState.GetBool("BallARetro.FullAudit.Baseline", false)) yield break;
        Check(BallARetroRunBuild.Current.IsEnabled && BallARetroDemoRun.Current == null, "Only full build model is active");
        Check(score.RoundRespone() == 1 && score.targetScore == 2000, "Original round and target retained");
        // Use the shipped pool and real callbacks. Seed search does not replace any choice data.
        var roller = Object.FindFirstObjectByType<CJS_Script_ChoiceRoller>();
        Check(roller.allChoices.Count == 9, "Real scene has nine authored effects");
        SeedFor(roller, 9); cameras.MonitorOnButton(); choices.ShowChoices();
        yield return new WaitForSecondsRealtime(1.5f);
        var reroll = Read<Button>(choices, "btnReroll");
        Check(reroll.gameObject.activeInHierarchy && reroll.interactable, "Full game exposes reroll button");
        for (int i = 0; i < 3; i++)
        {
            reroll.onClick.Invoke(); reroll.onClick.Invoke();
            Check(Read<int>(choices, "usedReroll") == i + 1, "Reroll consumes once " + i);
            yield return new WaitForSecondsRealtime(.25f);
        }
        Check(!reroll.interactable, "Fourth reroll disabled");
        // Rebind a seeded offer through the existing refresh path only for deterministic effect coverage.
        SeedFor(roller, 9); Invoke(choices, "RefreshCards", false, false);
        foreach (string code in new[] { "ko", "en", "ja" })
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(code);
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var text in reroll.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                Check(!text.isTextOverflowing && text.textInfo.lineCount == 1, code + " reroll label fits one line");
            }
            foreach (var card in Cards())
            {
                var description = Read<TMP_Text>(card, "txtDesc"); description.ForceMeshUpdate();
                Check(!description.isTextOverflowing && !description.text.StartsWith("Build_"), code + " card description fits: " + Read<CJS_ChoiceData>(card, "data").funcIdx);
            }
            Capture("choices-" + code);
        }
        int picksBefore = CJS_Script_ChoiceState.I.Picked.Count;
        Pick(9); Check(BallARetroRunBuild.Current.CapacitorLevel == 1, "Real card applies capacitor");
        while (Read<bool>(choices, "isOpen"))
        {
            int next = BallARetroRunBuild.Current.LastBallLevel < 2 ? 10 : 6;
            SeedFor(roller, next); Invoke(choices, "RefreshCards", false, false); Pick(next);
        }
        yield return WaitFor(() => Time.timeScale == 1 && cameras.cameraGos[0].activeInHierarchy, "Final pick resumes camera and physics");
        var entry = Read<List<Button>>(choices, "entryButtons");
        Check(entry.Count > 0 && entry.All(b => !b.interactable), "Serialized entry buttons disabled after completed choice");
        cameras.MonitorOnButton(); choices.ShowChoices(); yield return new WaitForSecondsRealtime(.3f);
        Check(Time.timeScale == 1 && cameras.cameraGos[0].activeInHierarchy, "Completed choice rejects camera-only reopening");
        Check(CJS_Script_ChoiceState.I.Picked.Count > picksBefore, "History records choices");
        // If the authored first-screen budget is one, the next actual round supplies the second card.
        if (BallARetroRunBuild.Current.LastBallLevel == 0)
        {
            SeedFor(roller, 10); score.AddScore(score.targetScore - score.curScore);
            yield return WaitFor(() => Read<bool>(choices, "isOpen"), "Next round opens serialized choice UI");
            yield return new WaitForSecondsRealtime(1.5f);
            while (Read<bool>(choices, "isOpen"))
            {
                int next = BallARetroRunBuild.Current.LastBallLevel < 2 ? 10 : 6;
                SeedFor(roller, next); Invoke(choices, "RefreshCards", false, false); Pick(next);
            }
            yield return WaitFor(() => Time.timeScale == 1 && cameras.cameraGos[0].activeInHierarchy, "Round choice returns to game");
        }
        ball.SetBallCount(1); BallARetroRunBuild.Current.RecordBumperHit();
        Check(BallARetroRunBuild.Current.Charge == 200, "Pair doubles final-ball charging");
        var drawer = Object.FindFirstObjectByType<PSH_Script_QuickChoiceOnOff>();
        foreach (string code in new[] { "ko", "en", "ja" })
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (!drawer.IsOpen) Check(drawer.TryHandleTabInput(true, false, false, false), "Tab opens history " + code);
            yield return new WaitForSecondsRealtime(.6f);
            var label = drawer.StatusHost.Find("BuildStatus").GetComponent<TMP_Text>(); label.ForceMeshUpdate();
            Check(label.text.Contains("200") && !label.isTextOverflowing, "Localized build status fits " + code);
            Check(Time.timeScale == 1 && cameras.cameraGos[0].activeInHierarchy, "Tab preserves camera and timescale " + code);
            Capture("history-" + code);
        }
        score.HandleBallOut(ball);
        Check(score.HasEnded && BallARetroRunBuild.Current.Charge == 0, "Drain loses unpaid charge and ends run");
        yield return WaitFor(() => Read<string>(PSH_Script_DialogueUI.Instance, "currentCutsceneId") == "Ending2", "Failure ending starts");
        PSH_Script_DialogueUI.Instance.SkipDialogue(); yield return new WaitForSecondsRealtime(1);
        Check(!drawer.IsOpen, "Ending closes Tab drawer"); Capture("failure-result");
        var previous = score;
        Object.FindFirstObjectByType<KHS_Script_ResetController>().GameResetFunc();
        yield return WaitFor(() => KHS_Script_ScoreManager.Instance != null && KHS_Script_ScoreManager.Instance != previous, "Retry reloads scene");
        yield return null; yield return null; Bind();
        Check(BallARetroRunBuild.Current.CapacitorLevel + BallARetroRunBuild.Current.LastBallLevel + BallARetroRunBuild.Current.Charge == 0, "Retry clears build state");
        Check(CJS_Script_ChoiceState.I.Picked.Count == 0, "Retry clears history");
        yield return WaitFor(() => Time.timeScale == 1 && cameras.cameraGos[0].activeInHierarchy, "Retry resumes gameplay");
        Check(Read<string>(PSH_Script_DialogueUI.Instance, "currentCutsceneId") != "Intro", "Retry preserves one-time intro behavior");
        for (int round = 1; round < 3; round++)
        {
            score.AddScore(score.targetScore - score.curScore);
            yield return WaitFor(() => Read<bool>(choices, "isOpen"), "Win path round choice opens " + round);
            yield return new WaitForSecondsRealtime(1.5f);
            while (Read<bool>(choices, "isOpen"))
            {
                SeedFor(roller = Object.FindFirstObjectByType<CJS_Script_ChoiceRoller>(), 6);
                Invoke(choices, "RefreshCards", false, false); Pick(6);
            }
            yield return WaitFor(() => Time.timeScale == 1 && cameras.cameraGos[0].activeInHierarchy, "Win path choice resumes " + round);
        }
        score.AddScore(score.targetScore - score.curScore);
        Check(score.IsRecordChallenge && !score.HasEnded, "Final target starts record challenge");
        ball.SetBallCount(1); score.HandleBallOut(ball);
        yield return WaitFor(() => Read<string>(PSH_Script_DialogueUI.Instance, "currentCutsceneId") == "Ending1", "Success ending starts");
        PSH_Script_DialogueUI.Instance.SkipDialogue(); yield return new WaitForSecondsRealtime(1);
        Check(score.HasEnded && !Read<bool>(choices, "isOpen"), "Success ending preserves closed choice state");
        Capture("success-result");
    }
    void Bind()
    {
        score = KHS_Script_ScoreManager.Instance; ball = Object.FindFirstObjectByType<YJ_Script_BallController>();
        cameras = Object.FindFirstObjectByType<KHS_Script_CameraManager>(); choices = Object.FindFirstObjectByType<CJS_Script_ChoiceUIController>();
        ball.GetComponent<Rigidbody>().isKinematic = true;
    }
    void SeedFor(CJS_Script_ChoiceRoller roller, int id)
    {
        for (int seed = 1; seed <= 10000; seed++)
        {
            Set(roller, "rnd", new System.Random(seed));
            if (!roller.Roll3(out _).Any(c => c.funcIdx == id)) continue;
            Set(roller, "rnd", new System.Random(seed)); return;
        }
        throw new Exception("No eligible seeded offer for " + id);
    }
    List<CJS_Script_ChoiceCard> Cards() => Read<List<CJS_Script_ChoiceCard>>(choices, "liveCards");
    void Pick(int id)
    {
        var button = Cards().Single(c => Read<CJS_ChoiceData>(c, "data").funcIdx == id).GetComponent<Button>();
        button.onClick.Invoke(); button.onClick.Invoke();
    }
    IEnumerator MeasureFrames()
    {
        QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
        for (int i = 0; i < 60; i++) yield return null;
        var times = new double[240];
        for (int i = 0; i < times.Length; i++) { yield return null; times[i] = Time.unscaledDeltaTime * 1000; }
        Array.Sort(times);
        File.WriteAllText(Path.Combine(output, "frames.json"), JsonUtility.ToJson(new FrameReport {
            unity = Application.unityVersion, scene = SceneManager.GetActiveScene().name,
            scope = "Editor batch render, stationary authored ball, no manual input; not player FPS or hardware minimum",
            width = Screen.width, height = Screen.height, frames = times.Length, medianMs = times[120], p95Ms = times[228]
        }, true));
    }
    void Capture(string name)
    {
        var camera = cameras.cameraGos.First(go => go.activeInHierarchy).GetComponent<Camera>();
        var overlays = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            .Select(c => (canvas: c, camera: c.worldCamera, distance: c.planeDistance)).ToArray();
        var target = RenderTexture.GetTemporary(1920, 1080, 24);
        var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active; Texture2D pixels = null;
        RenderPipelineManager.beginCameraRendering += EmitUI;
        try
        {
            camera.targetTexture = target;
            foreach (var item in overlays) { item.canvas.renderMode = RenderMode.ScreenSpaceCamera; item.canvas.worldCamera = camera; item.canvas.planeDistance = camera.nearClipPlane + .1f; }
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target; pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), pixels.EncodeToPNG());
        }
        finally
        {
            RenderPipelineManager.beginCameraRendering -= EmitUI; camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            foreach (var item in overlays) { item.canvas.renderMode = RenderMode.ScreenSpaceOverlay; item.canvas.worldCamera = item.camera; item.canvas.planeDistance = item.distance; }
            RenderTexture.ReleaseTemporary(target); if (pixels != null) Object.Destroy(pixels);
        }
    }
    static void EmitUI(ScriptableRenderContext context, Camera camera) => ScriptableRenderContext.EmitGeometryForCamera(camera);
    IEnumerator WaitFor(Func<bool> ready, string label)
    { float until = Time.realtimeSinceStartup + 30; while (!ready() && Time.realtimeSinceStartup < until) yield return null; Check(ready(), label); }
    void Check(bool condition, string label) { if (!condition) throw new Exception(label); checks.Add(label); }
    void Finish()
    {
        if (finished) return; finished = true; Application.logMessageReceived -= OnLog;
        File.WriteAllText(Path.Combine(output, "full-scene-audit.json"), JsonUtility.ToJson(new Report { success = errors.Count == 0, checks = checks.ToArray(), errors = errors.ToArray(), expectedOfflineMessages = expectedOfflineMessages.ToArray() }, true));
        FullGameSceneAudit.Exit(errors.Count == 0 ? 0 : 1);
    }
    static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
}
#endif
