#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PSH;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Inject only into a disposable full-project snapshot AFTER its player build.
// Add a file-local DISABLESTEAMWORKS define to the disposable SteamManager copy,
// restoring its exact bytes after this audit. A global define would remove the
// Steamworks types needed by the real ranking code. This is not a client test.
[InitializeOnLoad]
public static class BallARetroDemoSceneAudit
{
    const string Phase = "BallARetro.DemoAudit.Phase";
    const string OriginalSymbols = "BallARetro.DemoAudit.OriginalSymbols";
    const string OriginalProductName = "BallARetro.DemoAudit.OriginalProductName";
    public const string Output = "BallARetro.DemoAudit.Output";
    static BallARetroDemoSceneAudit()
    {
        EditorApplication.playModeStateChanged += OnState;
        EditorApplication.delayCall += BeginWhenCompiled;
    }
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, "-auditOutput");
        if (i < 0 || i + 1 == args.Length) throw new ArgumentException("-auditOutput is required");
        if (!File.ReadAllText("Assets/Scripts/Steamworks.NET/SteamManager.cs").StartsWith("#define DISABLESTEAMWORKS"))
            throw new InvalidOperationException("Isolate the disposable SteamManager before running this scene audit.");
        Directory.CreateDirectory(args[i + 1]);
        SessionState.SetString(Output, args[i + 1]);
        SessionState.SetBool("BallARetro.DemoAudit.ResultOnly", args.Contains("-auditResultOnly"));
        SessionState.SetString(OriginalSymbols, PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone));
        SessionState.SetString(OriginalProductName, PlayerSettings.productName);
        // All implicit startup/local-record writes belong to an audit-only prefs
        // namespace, including when the build restored its authored product name.
        PlayerSettings.productName = "Ball-A-Retro Demo Scene Audit";
        SessionState.SetInt(Phase, 1);
        string demoSymbols = string.Join(";", PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)
            .Split(';').Where(s => !string.IsNullOrWhiteSpace(s) && s != "BALLARETRO_STOVE"
                && s != "BALLARETRO_STEAM" && s != "BALLARETRO_DEMO")
            .Concat(new[] { "BALLARETRO_STEAM", "BALLARETRO_DEMO" }).Distinct());
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, demoSymbols);
        EditorApplication.delayCall += BeginWhenCompiled;
    }
    static void BeginWhenCompiled()
    {
        if (SessionState.GetInt(Phase, 0) != 1) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !BallARetroPlatform.IsDemoBuild)
        { EditorApplication.delayCall += BeginWhenCompiled; return; }
        SessionState.SetInt(Phase, 2);
        EditorSceneManager.OpenScene("Assets/MainScenes/Main_Scene_Lobby.unity");
        EditorApplication.isPlaying = true;
    }
    static void OnState(PlayModeStateChange state)
    {
        if (SessionState.GetInt(Phase, 0) == 2 && state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(Phase, 3);
            new GameObject("Demo full scene audit").AddComponent<BallARetroDemoSceneProbe>();
        }
    }
    public static void Exit(int code)
    {
        SessionState.SetInt(Phase, 0);
        try
        {
            PlayerSettings.productName = SessionState.GetString(OriginalProductName, "Ball-A-Retro Demo");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, SessionState.GetString(OriginalSymbols, ""));
        }
        finally { EditorApplication.Exit(code); }
    }
}

public sealed class BallARetroDemoSceneProbe : MonoBehaviour
{
    readonly List<string> checks = new();
    readonly List<string> errors = new();
    string output;
    bool finished;
    float started;
    int retrySignals;
    int gameLoads;
    CJS_Script_ChoiceUIController choices;
    KHS_Script_CameraManager cameras;
    YJ_Script_BallController ball;
    KHS_Script_ScoreManager score;
    Transform content;
    [Serializable] sealed class Report
    {
        public bool success;
        public string[] checks, errors;
        public string instrumentation = "Real release scenes/prefabs, serialized callbacks, intro/banner/retry coroutines. Seeded random offers exercise specific upgrade callbacks without replacing the real choice pool. Ball held stationary; real accepted bumper-trigger callbacks drive round scoring. Steam SDK disabled only in this editor audit; not a manual physics or client authentication test.";
    }
    [Serializable] sealed class RenderState
    {
        public string capture;
        public bool recordedBeforeOffscreenConversion;
        public string offscreenCaptureNote = "PNG capture temporarily routes Overlay through the camera and can introduce geometry occlusion; these are the original runtime canvas settings.";
        public CanvasState[] rootCanvases;
        public CameraState[] cameras;
    }
    [Serializable] sealed class CanvasState
    {
        public string name, mode, camera;
        public float planeDistance;
        public int sortingOrder;
    }
    [Serializable] sealed class CameraState
    {
        public string name, renderType;
        public string[] stack;
    }
    [Serializable] sealed class NativeCaptureState { public bool written; public string note; }
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        output = SessionState.GetString(BallARetroDemoSceneAudit.Output, "");
        started = Time.realtimeSinceStartup;
        Application.logMessageReceived += OnLog;
        PSH_Script_GameSceneDirector.NoIntroStartEvt += OnRetrySignal;
        SceneManager.sceneLoaded += SeedAuditOffers;
        StartCoroutine(Guarded());
    }
    void SeedAuditOffers(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Main_Scene_Game") return;
        var roller = Object.FindFirstObjectByType<CJS_Script_ChoiceRoller>();
        Check(roller != null && (int)Read(roller, "seed") == 0,
            "Authored gameplay uses a fresh random sequence rather than a fixed seed");
        // sceneLoaded runs after Awake but before the first banner/choice. Keep
        // production randomness intact; only this disposable audit sets its RNG.
        typeof(CJS_Script_ChoiceRoller).GetField("rnd", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(roller, new System.Random(++gameLoads == 1 ? 6 : 5));
    }
    void OnRetrySignal() => retrySignals++;
    void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            errors.Add(message + "\n" + trace);
    }
    void Update()
    {
        if (!finished && Time.realtimeSinceStartup - started > 150)
        { errors.Add("Demo full scene flow timed out."); Finish(); }
    }
    IEnumerator Guarded()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Run());
        while (!finished && stack.Count > 0)
        {
            var current = stack.Peek(); bool next;
            try { next = current.MoveNext(); }
            catch (Exception ex) { errors.Add(ex.ToString()); Finish(); yield break; }
            if (!next) { stack.Pop(); continue; }
            if (current.Current is IEnumerator nested) stack.Push(nested);
            else yield return current.Current;
        }
        if (!finished) Finish();
    }
    void Check(bool ok, string label)
    { if (!ok) throw new Exception(label); checks.Add(label); }
    IEnumerator WaitFor(Func<bool> ready, string label, float seconds = 20)
    {
        float until = Time.realtimeSinceStartup + seconds;
        while (!ready() && Time.realtimeSinceStartup < until) yield return null;
        Check(ready(), label);
    }
    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(4);
        Check(Application.productName == "Ball-A-Retro Demo Scene Audit", "Audit has its own native save and preference namespace");
        Check(BallARetroPlatform.IsDemoBuild && !SteamManager.Initialized, "Demo gameplay enabled with SDK isolated in the audit");
        var notice = Object.FindFirstObjectByType<BallARetroStartupNotice>();
        if (notice != null)
        {
            var continueButton = notice.transform.Find("Notice/Continue").GetComponent<Button>();
            yield return WaitFor(() => continueButton.interactable, "Startup notice reading delay finishes");
            continueButton.onClick.Invoke();
            yield return WaitFor(() => !BallARetroStartupNotice.IsVisible,
                "Actual notice Continue action dismisses its input overlay after deferred destruction");
        }
        yield return null;
        var lobby = Object.FindFirstObjectByType<PSH_Script_LobbyUIManager>();
        Check(lobby != null && PSH_Script_SceneLoader.Instance != null, "Real lobby bootstraps the persistent scene loader");
        Capture("demo-lobby", Camera.main);
        if (SessionState.GetBool("BallARetro.DemoAudit.ResultOnly", false))
        {
            lobby.OnClick_StartGame();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Main_Scene_Game", "Actual lobby start action loads game for result-only audit");
            yield return null; yield return null;
            BindGame();
            yield return WaitFor(() => (string)Read(PSH_Script_DialogueUI.Instance, "currentCutsceneId") == "Intro", "Result audit reaches normal intro");
            PSH_Script_DialogueUI.Instance.SkipDialogue();
            yield return WaitFor(() => IsChoiceOpen, "Result audit reaches the actual starter");
            yield return new WaitForSecondsRealtime(2.2f);
            yield return Pick(9, 1);
            yield return CaptureFailureResult();
            yield break;
        }
        lobby.OnClick_Tutorial();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "Main_Scene_Tutorial", "Actual lobby tutorial action loads tutorial");
        yield return new WaitForSecondsRealtime(2);
        var tutorial = Object.FindFirstObjectByType<CJS_TutorialDirector>();
        Check(tutorial != null && tutorial.scoreManager.IsTutorialRun && !BallARetroDemoRun.Current.IsEnabled,
            "Authored tutorial remains outside demo mechanics");
        Check(Object.FindFirstObjectByType<YJ_Script_BallController>().GetBallCount() == 1,
            "Tutorial retains one authored ball rather than demo two");
        Check(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.name == "QuickChoiceTabHint").All(t => !t.gameObject.activeSelf),
            "Tutorial practice has no history hint overlapping its control instructions");
        tutorial.GoGame();
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "Main_Scene_Game", "Tutorial-to-game action loads the real game");
        yield return null;
        yield return null;
        BindGame();
        Check(ball.GetBallCount() == 2 && BallARetroDemoRun.BlocksLaunch, "New demo run has two balls and starter launch gate");
        yield return WaitFor(() => (string)Read(PSH_Script_DialogueUI.Instance, "currentCutsceneId") == "Intro", "Normal first-run intro reaches dialogue");
        PSH_Script_DialogueUI.Instance.SkipDialogue();
        yield return WaitFor(() => IsChoiceOpen, "Intro camera return and first round banner open starter");
        yield return new WaitForSecondsRealtime(2.2f);
        CheckChoiceView("starter");
        Capture("demo-starter");
        yield return Pick(9, 1);
        Check(BallARetroDemoRun.Current.CapacitorLevel == 1, "Starter capacitor applies through real card callback");
        yield return VerifyChoiceDrawer(false);

        for (int targetRound = 2; targetRound <= 3; targetRound++)
        {
            // Drive existing accepted physical-contact handlers without assigning
            // curScore, targetScore or the score manager's private state.
            var bumpers = Object.FindObjectsByType<KHS_Script_DumpManager>(FindObjectsSortMode.None)
                .Where(d => d.bumpScore > 0 && d.GetComponent<KHS_Script_BumperEffect>() != null).ToArray();
            Check(bumpers.Length > 0, "Real scene contains scoring bumpers for round " + targetRound);
            int before = score.RoundRespone();
            float contactDeadline = Time.realtimeSinceStartup + 20;
            while (score.IsPlaying && score.RoundRespone() == before && Time.realtimeSinceStartup < contactDeadline)
            {
                foreach (var bumper in bumpers)
                {
                    if (!score.IsPlaying || score.RoundRespone() != before) break;
                    bumper.SendMessage("OnTriggerEnter", ball.GetComponent<Collider>());
                }
                if (score.IsPlaying && score.RoundRespone() == before)
                    yield return new WaitForSeconds(bumpers.Max(b => b.waitTime) + .05f);
            }
            yield return WaitFor(() => score.RoundRespone() == targetRound && IsChoiceOpen,
                "Accepted bumper scoring reaches round " + targetRound + " and its serialized choice button");
            yield return new WaitForSecondsRealtime(2.2f);
            CheckChoiceView("round " + targetRound);
            int selectedId = targetRound == 2 ? 7 : 5;
            yield return Pick(selectedId, targetRound);
            var roller = (CJS_Script_ChoiceRoller)Read(choices, "rollerBehaviour");
            Check(roller.allChoices.Where(c => c.funcIdx == selectedId).All(c => !c.isEnabled),
                "Actual one-time effect " + selectedId + " removes its source card after the real selection callback");
            if (selectedId == 7)
                Check((bool)Read(Object.FindFirstObjectByType<KHS_Script_BatteryLedManager>(), "isForced"),
                    "Upper-floor choice really forces the passage open");
            else
            {
                var manager = roller.GetComponent<KHS_Script_RogueLikeManager>();
                var blackhole = (YJ_Script_BlackholeController)Read(manager, "blackHole");
                Check(((GameObject)Read(manager, "secretBumpersHolder")).activeSelf
                    && blackhole.isForceActive && blackhole.isActivated,
                    "Secret weapon choice really activates its bumpers and blackhole");
                for (int i = 0; i < 32; i++)
                    Check(roller.Roll3(out _).All(c => c.funcIdx != 5 && c.funcIdx != 7),
                        "Consumed one-time effects are absent from subsequent random offers " + i);
            }
        }
        Check(BallARetroDemoRun.Current.PicksTaken == 3 && CJS_Script_ChoiceState.I.Picked.Count == 3,
            "Starter plus two round upgrades create exactly three history records");
        choices.ShowChoices(); cameras.MonitorOnButton();
        yield return null;
        Check(!IsChoiceOpen && cameras.cameraGos[0].activeInHierarchy, "Consumed final choice cannot reopen the monitor");
        Capture("demo-three-upgrades");
        yield return VerifyChoiceDrawer(true);

        var previous = score;
        Object.FindFirstObjectByType<KHS_Script_ResetController>().GameResetFunc();
        yield return WaitFor(() => KHS_Script_ScoreManager.Instance != null && KHS_Script_ScoreManager.Instance != previous,
            "Real retry action reloads score and run state");
        yield return null; yield return null;
        BindGame();
        yield return WaitFor(() => IsChoiceOpen, "Retry opens its new starter without replaying intro");
        yield return new WaitForSecondsRealtime(2.2f);
        Check(BallARetroDemoRun.Current.PicksTaken == 0 && CJS_Script_ChoiceState.I.Picked.Count == 0
            && BallARetroDemoRun.Current.ChargePaidTotal == 0, "Retry has clean choices and contributions");
        Check(!FindDrawer().IsOpen, "Retry closes previously opened upgrade history");
        var flipper = Object.FindFirstObjectByType<KHS_Script_FliperController>();
        int originalFlips = flipper.fliper_Count;
        int originalRoundFlips = (int)Read(flipper, "fliper_Inital");
        yield return Pick(6, 1);
        Check(flipper.fliper_Count == originalFlips + 5 && (int)Read(flipper, "fliper_Inital") == originalRoundFlips + 5,
            "Existing flipper support applies once to current and future rounds through actual card and manager");
        yield return new WaitForSecondsRealtime(4);
        Check(retrySignals == 7 && !IsChoiceOpen && BallARetroDemoRun.Current.PicksTaken == 1,
            "All seven actual retry signals finish without reopening starter");
        Capture("demo-retry-resumed");
        yield return CaptureFailureResult();
    }
    IEnumerator CaptureFailureResult()
    {
        // Follow the same public exhaustion/end path as real ball-out events.
        // No score/state assignment, direct panel activation or mock submission.
        score.HandleBallOut(ball);
        Check(ball.GetBallCount() == 1 && !score.HasEnded, "First public drain preserves the demo run");
        score.HandleBallOut(ball);
        Check(score.HasEnded && !score.IsClearResult, "Second public drain triggers the actual failure ending");
        yield return WaitFor(() => (string)Read(PSH_Script_DialogueUI.Instance, "currentCutsceneId") == "Ending2",
            "Actual failure sequence reaches its ending dialogue");
        PSH_Script_DialogueUI.Instance.SkipDialogue();
        yield return WaitFor(() => Object.FindObjectsByType<BallARetroDemoPresentation>(FindObjectsSortMode.None)
            .Any(p => p.name == "DemoResult"), "Ending completion opens the actual demo result presentation");
        yield return new WaitForSecondsRealtime(.5f);
        var result = Object.FindObjectsByType<BallARetroDemoPresentation>(FindObjectsSortMode.None).Single(p => p.name == "DemoResult");
        var labels = result.GetComponentsInChildren<TMPro.TMP_Text>();
        foreach (string label in new[] { "ChargeContribution", "LastBallContribution", "PersonalBest", "SubmissionStatus" })
            Check(labels.Any(t => t.name == label && !string.IsNullOrWhiteSpace(t.text) && !t.text.StartsWith("Demo_")),
                "Actual result has localized " + label);
        Check(result.transform.Find("Image_RankOpen").GetComponent<Button>().isActiveAndEnabled,
            "Actual result retains its authored ranking button");
        var director = Object.FindFirstObjectByType<PSH_Script_GameOverSequenceDirector>();
        Check(((Button)Read(director, "resetBtn")).isActiveAndEnabled && ((Button)Read(director, "resetBtn")).interactable,
            "Actual result retry button is enabled after ending dialogue");
        Check(BallARetroPersonalRecordStore.Read(string.Empty, "Classic", 1).Length == 1 && !SteamManager.Initialized,
            "Result saves audit-only personal best without initializing Steam");
        Canvas.ForceUpdateCanvases();
        Check(result.GetComponentInParent<Canvas>().rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay,
            "Actual result root remains ScreenSpaceOverlay and ignores world geometry depth");
        string nativePath = Path.Combine(output, "demo-failure-result-native.png");
        ScreenCapture.CaptureScreenshot(nativePath);
        float nativeDeadline = Time.realtimeSinceStartup + 3;
        while (!File.Exists(nativePath) && Time.realtimeSinceStartup < nativeDeadline) yield return null;
        yield return new WaitForSecondsRealtime(.2f);
        File.WriteAllText(Path.Combine(output, "native-capture.json"), JsonUtility.ToJson(new NativeCaptureState {
            written = File.Exists(nativePath), note = "CaptureScreenshot requested with original rootCanvas Overlay; batch editor may not provide a Game View framebuffer."
        }, true));
        Capture("demo-failure-result");
    }
    void BindGame()
    {
        score = Object.FindFirstObjectByType<KHS_Script_ScoreManager>();
        ball = Object.FindFirstObjectByType<YJ_Script_BallController>();
        cameras = Object.FindFirstObjectByType<KHS_Script_CameraManager>();
        choices = Object.FindFirstObjectByType<CJS_Script_ChoiceUIController>(FindObjectsInactive.Include);
        Check(score && ball && cameras && choices, "Game scene contains actual score, ball, cameras and choice controller");
        ball.GetComponent<Rigidbody>().isKinematic = true;
        content = (Transform)Read(choices, "content");
        Check((YJ_Script_BallController)Read(choices, "ballCon") == ball
            && (KHS_Script_CameraManager)Read(choices, "camManager") == cameras
            && Read(choices, "rollerBehaviour") is CJS_Script_ChoiceRoller && Read(choices, "cardPrefab") != null,
            "Choice prefab references resolve to this scene's actual actors");
        var generator = Object.FindFirstObjectByType<KHS_Script_RogueLikeGenerate>(FindObjectsInactive.Include);
        Check(generator != null, "Game scene contains the authored round-choice button");
        var button = generator.GetComponent<Button>();
        var callbacks = Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Select(button.onClick.GetPersistentMethodName).ToArray();
        Check(callbacks.Contains("ShowChoices") && callbacks.Contains("MonitorOnButton"),
            "Authored next-round button has both serialized UI and camera callbacks");
    }
    bool IsChoiceOpen => choices != null && (bool)Read(choices, "isOpen");
    PSH_Script_QuickChoiceOnOff FindDrawer()
        => Object.FindFirstObjectByType<PSH_Script_QuickChoiceOnOff>(FindObjectsInactive.Include);

    IEnumerator VerifyChoiceDrawer(bool fullLayout)
    {
        var drawer = FindDrawer();
        Check(drawer != null && !drawer.IsOpen, "Upgrade history starts closed during unobstructed gameplay");
        var bonusPanels = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.name == "BonusStatus").ToArray();
        Check(bonusPanels.Length == 1 && bonusPanels[0].GetComponentInParent<PSH_Script_QuickChoiceOnOff>() == drawer,
            "Demo bonus status is inside the left history drawer, not an independent upper HUD");
        Check(!bonusPanels[0].gameObject.activeInHierarchy, "Bonus explanation is hidden while history is closed");
        if (!fullLayout) Capture("demo-tab-closed");
        float originalTime = Time.timeScale;
        bool originalCursor = Cursor.visible;
        var originalLock = Cursor.lockState;
        int originalScore = score.curScore;
        int originalPicks = BallARetroDemoRun.Current.PicksTaken;
        var activeCamera = cameras.cameraGos.Single(go => go.activeInHierarchy);
        Check(drawer.TryTogglePanel() && drawer.IsOpen, "Gameplay Tab action opens the real history drawer");
        yield return new WaitForSecondsRealtime(.65f);
        Check(bonusPanels[0].gameObject.activeInHierarchy, "Opening history reveals current demo effects");
        Check(drawer.GetComponentsInChildren<CJS_Script_ChoiceSlot>().Length == originalPicks,
            "Opened history contains exactly the chosen upgrades");
        Check(Time.timeScale == originalTime && Cursor.visible == originalCursor && Cursor.lockState == originalLock
            && score.curScore == originalScore && BallARetroDemoRun.Current.PicksTaken == originalPicks
            && cameras.cameraGos.Single(go => go.activeInHierarchy) == activeCamera,
            "Viewing upgrades preserves time, pointer, camera, score and choices");
        if (fullLayout)
        {
            var originalLocale = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
            foreach (string code in new[] { "ko", "en", "ja" })
            {
                UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale =
                    UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.GetLocale(code);
                yield return null;
                Canvas.ForceUpdateCanvases();
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(drawer.transform, bonusPanels[0]);
                var rect = ((RectTransform)drawer.transform).rect;
                Check(bounds.min.x >= rect.xMin - 1 && bounds.max.x <= rect.xMax + 1
                    && bounds.min.y >= rect.yMin - 1 && bounds.max.y <= rect.yMax + 1,
                    code + ": demo status stays inside history panel bounds");
                foreach (var text in bonusPanels[0].GetComponentsInChildren<TMPro.TMP_Text>())
                {
                    text.ForceMeshUpdate();
                    Check(!string.IsNullOrWhiteSpace(text.text) && !text.text.StartsWith("Demo_") && !text.isTextOverflowing,
                        code + ": readable localized drawer status " + text.name);
                }
                var hint = Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None)
                    .Single(t => t.name == "QuickChoiceTabHint");
                hint.ForceMeshUpdate();
                Check(hint.text.Contains("Tab") && !hint.isTextOverflowing
                    && hint.font.HasCharacters(hint.text, out uint[] missing, true, false),
                    code + ": actual Tab hint has every glyph and fits its label");
                Capture("demo-tab-open-" + code);
            }
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale = originalLocale;
            yield return null;
            // Leave this panel open intentionally: the following real scene retry
            // must discard it without leaking input, cursor or open state.
        }
        else
        {
            yield return new WaitForSecondsRealtime(3.2f);
            Check(drawer.IsOpen, "Tab-opened history remains available beyond the former three-second timeout");
            Check(drawer.TryTogglePanel() && !drawer.IsOpen, "Second gameplay Tab action closes history");
            yield return new WaitForSecondsRealtime(.65f);
            Check(!bonusPanels[0].gameObject.activeInHierarchy, "Closed history hides the bonus status again");
            Check(drawer.TryTogglePanel(), "History can reopen without recreating its owner");
            var settings = Object.FindFirstObjectByType<PSH_Script_SettingController>();
            settings.ToggleMenu();
            yield return null; yield return null;
            Check(PSH_Script_SettingController.IsMenuOpen && !drawer.IsOpen && !drawer.TryTogglePanel(),
                "Settings closes history and blocks Tab from opening behind the modal");
            settings.CloseMenu();
            yield return null; yield return null;
            Check(!drawer.IsOpen && Time.timeScale == originalTime && Cursor.visible == originalCursor,
                "Closing settings keeps history hidden and restores gameplay input");
        }
    }

    void CheckChoiceView(string stage)
    {
        var live = (List<CJS_Script_ChoiceCard>)Read(choices, "liveCards");
        Check(live.Count == 3 && live.Select(c => ((CJS_ChoiceData)Read(c, "data")).funcIdx).Distinct().Count() == 3,
            stage + ": three distinct real cards");
        Check(cameras.cameraGos.Count(c => c.activeInHierarchy) == 1 && cameras.cameraGos[2].activeInHierarchy
            && Time.timeScale == 0 && Cursor.visible, stage + ": one monitor camera, paused physics and visible cursor");
        Check(live.Count(c => ((CJS_ChoiceData)Read(c, "data")).funcIdx <= 3) <= 1,
            stage + ": randomized offer contains at most one bonus-map-only card");
        Check(!((Button)Read(choices, "btnSkip")).gameObject.activeInHierarchy
            && !((Button)Read(choices, "btnReroll")).gameObject.activeInHierarchy, stage + ": demo keeps one mandatory pick per round without manual rerolls");
        var drawer = FindDrawer();
        Check(drawer != null && !drawer.IsOpen && !drawer.TryTogglePanel(),
            stage + ": Tab cannot open history behind an active choice screen");
    }
    IEnumerator Pick(int id, int expectedPicks)
    {
        var card = ((List<CJS_Script_ChoiceCard>)Read(choices, "liveCards"))
            .Single(c => ((CJS_ChoiceData)Read(c, "data")).funcIdx == id);
        card.GetComponent<Button>().onClick.Invoke();
        card.GetComponent<Button>().onClick.Invoke();
        yield return WaitFor(() => !IsChoiceOpen && cameras.cameraGos[0].activeInHierarchy && Time.timeScale == 1,
            "Choice returns to sole gameplay camera without hidden delay", 12);
        Check(cameras.cameraGos.Count(c => c.activeInHierarchy) == 1 && !Cursor.visible,
            "Card selection restores gameplay input and hides pointer");
        Check(((GameObject)Read(choices, "gameUICanvasGo")).activeInHierarchy,
            "Choice completion restores actual gameplay HUD");
        if (expectedPicks == 1)
        {
            var ballHud = Object.FindObjectsByType<KHS_Script_UIToBall>(FindObjectsSortMode.None);
            Check(ballHud.Length > 0 && ballHud.All(ui => ui.GetComponent<TMPro.TMP_Text>().text == "2"),
                "Initial visible ball HUD shows two before first launch");
        }
        Check(BallARetroDemoRun.Current.PicksTaken == expectedPicks, "Duplicate card activation applies only one pick");
    }
    static object Read(object target, string field) => target?.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
    void Capture(string name, Camera explicitCamera = null)
    {
        var camera = explicitCamera != null ? explicitCamera
            : cameras.cameraGos.First(go => go.activeInHierarchy).GetComponent<Camera>();
        var originalCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToArray();
        File.WriteAllText(Path.Combine(output, name + "-render-state.json"), JsonUtility.ToJson(new RenderState {
            capture = name, recordedBeforeOffscreenConversion = true,
            rootCanvases = originalCanvases.Select(c => new CanvasState {
                name = c.name, mode = c.renderMode.ToString(), camera = c.worldCamera != null ? c.worldCamera.name : null,
                planeDistance = c.planeDistance, sortingOrder = c.sortingOrder
            }).ToArray(),
            cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(c => {
                var data = c.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                return new CameraState { name = c.name, renderType = data != null ? data.renderType.ToString() : "None",
                    stack = data != null && data.renderType == UnityEngine.Rendering.Universal.CameraRenderType.Base
                        ? data.cameraStack.Where(s => s != null).Select(s => s.name).ToArray() : Array.Empty<string>() };
            }).ToArray()
        }, true));
        var overlays = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            .Select(c => (canvas: c, camera: c.worldCamera, distance: c.planeDistance)).ToArray();
        var target = RenderTexture.GetTemporary(1920, 1080, 24);
        var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
        float oldAspect = camera.aspect; int oldMask = camera.cullingMask;
        Texture2D pixels = null;
        RenderPipelineManager.beginCameraRendering += EmitUI;
        try
        {
            camera.targetTexture = target;
            camera.aspect = 1920f / 1080f;
            camera.cullingMask |= 1 << 5;
            foreach (var item in overlays)
            {
                item.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                item.canvas.worldCamera = camera;
                item.canvas.planeDistance = camera.nearClipPlane + .1f;
            }
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), pixels.EncodeToPNG());
        }
        finally
        {
            RenderPipelineManager.beginCameraRendering -= EmitUI;
            camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            camera.aspect = oldAspect; camera.cullingMask = oldMask;
            foreach (var item in overlays)
            {
                item.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                item.canvas.worldCamera = item.camera;
                item.canvas.planeDistance = item.distance;
            }
            Canvas.ForceUpdateCanvases();
            RenderTexture.ReleaseTemporary(target); if (pixels) Object.Destroy(pixels);
        }
    }
    static void EmitUI(ScriptableRenderContext context, Camera camera) => ScriptableRenderContext.EmitGeometryForCamera(camera);
    void Finish()
    {
        if (finished) return; finished = true;
        Application.logMessageReceived -= OnLog;
        PSH_Script_GameSceneDirector.NoIntroStartEvt -= OnRetrySignal;
        SceneManager.sceneLoaded -= SeedAuditOffers;
        try
        {
            File.WriteAllText(Path.Combine(output, "demo-scene-audit.json"), JsonUtility.ToJson(new Report {
                success = errors.Count == 0, checks = checks.ToArray(), errors = errors.ToArray()
            }, true));
        }
        catch (Exception ex) { errors.Add(ex.ToString()); Debug.LogError(ex); }
        finally { BallARetroDemoSceneAudit.Exit(errors.Count == 0 ? 0 : 1); }
    }
}
#endif
