#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Copy only into the isolated project's Assets. Never included in a player build.
[InitializeOnLoad]
public static class BallARetroSceneAudit
{
    const string Active = "BallARetro.ReleaseSceneAudit.Active";
    public const string Output = "BallARetro.ReleaseSceneAudit.Output";
    static BallARetroSceneAudit() => EditorApplication.playModeStateChanged += OnState;
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-auditOutput");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("-auditOutput is required");
        Directory.CreateDirectory(args[index + 1]);
        SessionState.SetString(Output, args[index + 1]);
        SessionState.SetBool(Active, true);
        EditorSceneManager.OpenScene("Assets/MainScenes/Main_Scene_Lobby.unity");
        EditorApplication.isPlaying = true;
    }
    static void OnState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Active, false) || state != PlayModeStateChange.EnteredPlayMode) return;
        SessionState.SetBool(Active, false);
        new GameObject("Release scene audit").AddComponent<BallARetroSceneProbe>();
    }
}

public class BallARetroSceneProbe : MonoBehaviour
{
    readonly List<string> passed = new();
    readonly List<string> errors = new();
    string output;
    float started;
    bool finished;
    string previousLocale;
    string previousRecord;
    string recordKey;
    int quitRequests;
    [Serializable] class Result { public bool success; public string[] checks, errors; }
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        output = SessionState.GetString(BallARetroSceneAudit.Output, "");
        started = Time.realtimeSinceStartup;
        previousLocale = PlayerPrefs.HasKey(PSH_Script_Localization.PreferenceKey) ? PlayerPrefs.GetString(PSH_Script_Localization.PreferenceKey) : null;
        recordKey = BallARetroPersonalRecordStore.StorageKey("ReleaseSmoke", 1);
        previousRecord = PlayerPrefs.HasKey(recordKey) ? PlayerPrefs.GetString(recordKey) : null;
        Application.logMessageReceived += OnLog;
        StartCoroutine(GuardedRun());
    }
    void OnLog(string message, string trace, LogType type)
    {
        if (message == "Game Quit") quitRequests++;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert
            || (type == LogType.Warning && message.Contains("DontDestroyOnLoad only works for root")))
            errors.Add(message + "\n" + trace);
    }
    void Update()
    {
        if (!finished && Time.realtimeSinceStartup - started > 210f)
        { errors.Add("Full scene audit timed out."); Finish(); }
    }
    IEnumerator GuardedRun()
    {
        var routines = new Stack<IEnumerator>();
        routines.Push(Run());
        while (!finished && routines.Count > 0)
        {
            var run = routines.Peek();
            bool next;
            try { next = run.MoveNext(); }
            catch (Exception ex) { errors.Add(ex.ToString()); Finish(); yield break; }
            if (!next) { routines.Pop(); continue; }
            if (run.Current is IEnumerator nested) routines.Push(nested);
            else yield return run.Current;
        }
        if (!finished) Finish();
    }
    void Check(bool ok, string label)
    {
        if (!ok) throw new Exception(label);
        passed.Add(label);
    }
    IEnumerator Run()
    {
        var dependencies = AssetDatabase.GetDependencies(new[] { "Assets/MainScenes/Main_Scene_Lobby.unity",
            "Assets/MainScenes/Main_Scene_Tutorial.unity", "Assets/MainScenes/Main_Scene_Game.unity" }, true);
        // Editor dependency graphs include overridden/removed prefab sources. Keep
        // this diagnostic; the packed-assets gate verifies actual shipped resources.
        File.WriteAllLines(Path.Combine(output, "editor-vendor-dependencies.txt"),
            dependencies.Where(IsReplacedVendorVisual));
        var notice = Object.FindFirstObjectByType<BallARetroStartupNotice>();
        Check(notice != null && BallARetroStartupNotice.IsVisible, "Startup notice appears before lobby input");
        var proceed = notice.transform.Find("Notice/Continue").GetComponent<UnityEngine.UI.Button>();
        Check(!proceed.interactable, "Startup notice initially prevents accidental dismissal");
        Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == proceed.gameObject,
            "Startup notice captures keyboard selection before its reading delay");
        UnityEngine.EventSystems.ExecuteEvents.Execute(proceed.gameObject,
            new UnityEngine.EventSystems.BaseEventData(UnityEngine.EventSystems.EventSystem.current),
            UnityEngine.EventSystems.ExecuteEvents.submitHandler);
        proceed.onClick.Invoke();
        Check(BallARetroStartupNotice.IsVisible, "Startup notice rejects dismissal before the reading delay");
        yield return new WaitForSecondsRealtime(3);
        Check(!BallARetroPlatform.IsSteamBuild && !SteamManager.Initialized, "STOVE starts without Steam initialization");
        yield return LocalizationSettings.InitializationOperation;
        yield return new WaitForSecondsRealtime(2.2f);
        Check(proceed.interactable, "Startup notice becomes actionable using unscaled time");
        var startupSelectors = LocalizationSettings.StartupLocaleSelectors.Select(s => s.GetType().Name).ToArray();
        Check(Array.IndexOf(startupSelectors, "PlayerPrefLocaleSelector") < Array.IndexOf(startupSelectors, "SystemLocaleSelector")
            && Array.IndexOf(startupSelectors, "SystemLocaleSelector") < Array.IndexOf(startupSelectors, "SpecificLocaleSelector"),
            "Startup locale prefers saved language, then OS language, then English fallback");
        var backdrop = notice.transform.Find("Backdrop").GetComponent<UnityEngine.UI.Image>();
        Check(backdrop.color.a == 1 && backdrop.raycastTarget && notice.GetComponent<Canvas>().sortingOrder == 30000,
            "Static startup notice fully covers and blocks the underlying menu");
        Check(AudioListener.pause, "Startup notice pauses background audio");
        var blockedSettings = Object.FindFirstObjectByType<PSH_Script_SettingController>();
        blockedSettings.ToggleMenu();
        Check(!PSH_Script_SettingController.IsMenuOpen, "Startup notice blocks the settings shortcut");
        foreach (string code in new[] { "ko", "en", "ja" })
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(code);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var labels = notice.GetComponentsInChildren<TMPro.TMP_Text>();
            foreach (var label in labels) label.ForceMeshUpdate();
            Check(labels.All(label => !string.IsNullOrWhiteSpace(label.text) && !label.text.StartsWith("Startup_")
                && !label.isTextOverflowing), "Startup notice is translated and unclipped: " + code);
            CaptureFrame("startup-notice-" + code);
            if (code == "en") CaptureFrame("startup-notice-en-1280x720", width: 1280, height: 720);
            if (code == "ja") CaptureFrame("startup-notice-ja-1024x768", width: 1024, height: 768);
        }
        var startupPointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, proceed.transform.position)
        };
        var startupHits = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(startupPointer, startupHits);
        Check(startupHits.Count > 0 && startupHits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == proceed,
            "Startup continue button receives the pointer above the lobby");
        bool[] effectsBefore = Enum.GetValues(typeof(PSH_Script_EffectSettings.Effect)).Cast<PSH_Script_EffectSettings.Effect>()
            .Select(effect => PSH_Script_EffectSettings.Get(effect)).ToArray();
        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(startupHits[0].gameObject, startupPointer,
            UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        yield return null;
        Check(!BallARetroStartupNotice.IsVisible && !AudioListener.pause, "Startup continue dismisses the notice and restores audio");
        Check(Enum.GetValues(typeof(PSH_Script_EffectSettings.Effect)).Cast<PSH_Script_EffectSettings.Effect>()
            .Select(effect => PSH_Script_EffectSettings.Get(effect)).SequenceEqual(effectsBefore),
            "Continue preserves the player's effect settings");
        // Exercise the alternate real UI action without changing the default startup behavior.
        notice = Object.Instantiate(Resources.Load<GameObject>("Release/StartupNotice")).GetComponent<BallARetroStartupNotice>();
        yield return new WaitForSecondsRealtime(2.2f);
        notice.transform.Find("Notice/ReduceAndContinue").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        yield return null;
        Check(!BallARetroStartupNotice.IsVisible && !PSH_Script_EffectSettings.IsEnabled,
            "Reduce effects and continue disables every supported effect before revealing the lobby");
        for (int i = 0; i < effectsBefore.Length; i++) PSH_Script_EffectSettings.Set((PSH_Script_EffectSettings.Effect)i, effectsBefore[i]);
        var lobby = Object.FindFirstObjectByType<PSH.PSH_Script_LobbyUIManager>();
        Check(lobby != null, "Full lobby scene initializes");
        Canvas.ForceUpdateCanvases();
        var quit = lobby.transform.Find("Panel_Bg/Panel_Close/Button_Close").GetComponent<UnityEngine.UI.Button>();
        var quitCanvas = quit.GetComponentInParent<Canvas>();
        var quitRect = (RectTransform)quit.transform;
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(quitCanvas.worldCamera, quitRect.TransformPoint(quitRect.rect.center))
        };
        var hits = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == quit,
            "Lobby top-right X receives the pointer instead of its transparent wrapper");
        // Application.Quit is deliberately ignored by the editor; observe the real serialized callback.
        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer,
            UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        Check(quitRequests == 1, "Lobby top-right X invokes the application quit callback once");
        CaptureFrame("lobby");
        yield return BallARetroRenderProbe.Measure(output, "lobby");
        // Reproduce the real scene's manager-before-LED Start order deterministically.
        var earlyLed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        earlyLed.SetActive(false);
        var led = earlyLed.AddComponent<ChangeSpriteRenderer>();
        led.obj_Light = earlyLed.AddComponent<Light>();
        led.On = true;
        var managerObject = new GameObject("Early bumper manager");
        managerObject.SetActive(false);
        var manager = managerObject.AddComponent<YJ_Script_BumperComboManager>();
        manager.lightX2 = led;
        typeof(YJ_Script_BumperComboManager).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(manager, null);
        Check(!led.On && !led.obj_Light.enabled, "Bumper startup clears inactive LED before its Start");
        Check(earlyLed.GetComponent<Renderer>().material.GetColor("_EmissionColor") == led.Emission_Off_, "Bumper startup applies LED emission before its Start");
        Object.Destroy(earlyLed);
        Object.Destroy(managerObject);
        var nicknameObject = new GameObject("Inactive nickname panel");
        nicknameObject.SetActive(false);
        var nickname = nicknameObject.AddComponent<CJS_Script_NicknameSetter>();
        int errorsBefore = errors.Count;
        typeof(CJS_Script_NicknameSetter).GetMethod("OnSceneLoaded", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(nickname, new object[] { SceneManager.GetActiveScene(), LoadSceneMode.Single });
        Check(errors.Count == errorsBefore, "Inactive nickname panel does not start a coroutine on scene load");
        Object.Destroy(nicknameObject);
        foreach (string code in new[] { "ko", "en", "ja" })
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            Check(locale != null, "Locale available: " + code);
            LocalizationSettings.SelectedLocale = locale;
            yield return null;
            Check(PSH_Script_Localization.Text("Sys_Title") != "Sys_Title", "UI text resolves: " + code);
        }
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ko");
        var settings = Object.FindFirstObjectByType<PSH_Script_SettingController>();
        Check(settings != null, "Lobby settings controller exists");
        settings.ToggleMenu();
        yield return null;
        Check(PSH_Script_SettingController.IsMenuOpen && Time.timeScale == 0, "Lobby settings pause and open");
        CaptureFrame("lobby-settings");
        settings.CloseMenu();
        yield return null;
        Check(!PSH_Script_SettingController.IsMenuOpen && Time.timeScale == 1, "Lobby settings close and resume");
        lobby.OnClick_RankingPanelOpen();
        yield return null;
        CaptureFrame("stove-ranking");
        lobby.OnClick_RankingPanelClose();
        BallARetroPersonalRecordStore.Submit("Reviewer", 9000, "ReleaseSmoke", 1);
        BallARetroPersonalRecordStore.Submit("Reviewer", 5000, "ReleaseSmoke", 1);
        Check(BallARetroPersonalRecordStore.Read("Reviewer", "ReleaseSmoke", 1)[0].score == 9000, "Offline best survives lower score");
        int loadStarts = 0;
        Action countLoad = () => loadStarts++;
        PSH.PSH_Script_SceneLoader.OnSceneLoadStart += countLoad;
        lobby.OnClick_Tutorial();
        lobby.OnClick_Tutorial();
        Check(loadStarts == 1, "Repeated menu activation starts only one scene load");
        PSH.PSH_Script_SceneLoader.OnSceneLoadStart -= countLoad;
        yield return null;
        var loadingVfx = Object.FindFirstObjectByType<BallARetroLoadingVfx>();
        Check(loadingVfx != null && !loadingVfx.raycastTarget, "Original loading particles appear without intercepting input");
        Check(loadingVfx.GetComponentInParent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay,
            "Loading indicator does not depend on the outgoing scene camera");
        var loadingBackdrop = loadingVfx.transform.parent.GetComponent<UnityEngine.UI.Image>();
        Check(loadingBackdrop != null && loadingBackdrop.enabled && loadingBackdrop.raycastTarget
            && loadingBackdrop.color.a == 0, "Loading blocker keeps the lobby visible and blocks duplicate clicks");
        var rowButtons = lobby.GetComponentsInChildren<PSH_Script_StartButton>();
        var buttonBounds = rowButtons.Select(button => ScreenBounds((RectTransform)button.transform)).ToArray();
        var indicatorBounds = ScreenBounds(loadingVfx.rectTransform);
        Check(buttonBounds.Length == 2 && indicatorBounds.yMax < buttonBounds.Min(rect => rect.yMin)
            && indicatorBounds.yMin >= 0, "Loading indicator sits fully below the middle menu buttons");
        var rowBounds = ScreenBounds((RectTransform)rowButtons[0].transform.parent);
        Check(Mathf.Abs(indicatorBounds.center.x - rowBounds.center.x) < 2,
            "Loading indicator is centered beneath the menu row");
        var loadingHits = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(
            UnityEngine.EventSystems.EventSystem.current) { position = buttonBounds[0].center }, loadingHits);
        Check(loadingHits.Count > 0 && loadingHits[0].gameObject == loadingBackdrop.gameObject,
            "Transparent loading panel blocks clicks reaching the menu");
        CaptureFrame("loading-original");
        CaptureFrame("loading-1280x720", width: 1280, height: 720);
        CaptureFrame("loading-1024x768", width: 1024, height: 768);
        while (SceneManager.GetActiveScene().name != "Main_Scene_Tutorial") yield return null;
        Check(!loadingVfx.gameObject.activeInHierarchy, "Loading indicator is hidden on destination scene activation");
        yield return new WaitForSecondsRealtime(4);
        var tutorial = Object.FindFirstObjectByType<CJS_TutorialDirector>();
        Check(tutorial != null && tutorial.scoreManager != null && tutorial.plunger != null, "Full tutorial scene initializes with score and plunger");
        CheckSceneVisualReferences("tutorial");
        Check(tutorial.tutorialTargetScore == 1000 && !tutorial.resultPanel.activeSelf, "Tutorial goal and initial result state");
        var tutorialChoices = CJS_Script_ChoiceState.I;
        Check(tutorialChoices != null && tutorialChoices.gameObject.scene == SceneManager.GetActiveScene()
            && tutorialChoices.Picked.Count == 0, "Tutorial choice history starts empty in its own scene");
        tutorialChoices.Add(new CJS_ChoiceData { name = "Scene audit tutorial choice" });
        Check(tutorialChoices.Picked.Count == 1, "Tutorial choice history retains the current run selection");
        var targetHUD = Object.FindFirstObjectByType<KHS_Script_UIToTScore>();
        Check(tutorial.scoreManager.targetScore == 1000 && targetHUD != null
            && targetHUD.GetComponent<TMPro.TMP_Text>().text.Replace(" ", "") == "/1000", "Tutorial HUD displays the actual 1000-point goal before launch");
        yield return null;
        CaptureFrame("tutorial");
        yield return BallARetroRenderProbe.Measure(output, "tutorial");
        yield return CheckTutorialCameraRecovery(tutorial);
        settings = Object.FindFirstObjectByType<PSH_Script_SettingController>();
        settings.ToggleMenu();
        settings.OnClickGoToLobby();
        yield return null;
        Check(settings.panelWarning.activeSelf, "Tutorial return requires confirmation");
        settings.ConfirmExit();
        while (SceneManager.GetActiveScene().name != "Main_Scene_Lobby") yield return null;
        yield return new WaitForSecondsRealtime(2);
        Check(tutorialChoices == null && CJS_Script_ChoiceState.I == null, "Tutorial choice history is released on return to lobby");
        lobby = Object.FindFirstObjectByType<PSH.PSH_Script_LobbyUIManager>();
        lobby.OnClick_StartGame();
        while (SceneManager.GetActiveScene().name != "Main_Scene_Game") yield return null;
        yield return new WaitForSecondsRealtime(4);
        Check(Object.FindFirstObjectByType<KHS_Script_ScoreManager>() != null, "Full game scene initializes");
        CheckSceneVisualReferences("game");
        var gameChoices = CJS_Script_ChoiceState.I;
        Check(gameChoices != null && gameChoices.gameObject.scene == SceneManager.GetActiveScene()
            && gameChoices.Picked.Count == 0, "Game starts with fresh choice history after tutorial");
        gameChoices.Add(new CJS_ChoiceData { name = "Scene audit game choice" });
        Check(gameChoices.Picked.Count == 1, "Game choice history retains the current run selection");
        targetHUD = Object.FindFirstObjectByType<KHS_Script_UIToTScore>();
        Check(targetHUD != null && targetHUD.GetComponent<TMPro.TMP_Text>().text.Replace(" ", "") == "/" + KHS_Script_ScoreManager.Instance.targetScore,
            "Game HUD displays the initialized round target");
        yield return null;
        Object.FindFirstObjectByType<PSH.PSH_Script_DialogueUI>().SkipDialogue();
        yield return new WaitForSecondsRealtime(3);
        var portals = Object.FindObjectsByType<BallARetroPortalVfx>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        yield return BallARetroRenderProbe.Measure(output, "game");
        if (Environment.GetCommandLineArgs().Contains("-measureRendering"))
        {
            var managerForRendering = Object.FindFirstObjectByType<KHS_Script_CameraManager>();
            managerForRendering.SubCamOn(1);
            yield return null;
            yield return BallARetroRenderProbe.Measure(output, "game-bonus", managerForRendering.cameraGos[1].GetComponent<Camera>());
            CaptureFrame("game-bonus", managerForRendering.cameraGos[1].GetComponent<Camera>());
            managerForRendering.MainCamOn();
            yield return null;
            var mainForRendering = managerForRendering.cameraGos[0].GetComponent<Camera>();
            int originalMask = mainForRendering.cullingMask;
            try
            {
                mainForRendering.cullingMask |= 1 << LayerMask.NameToLayer("2F");
                yield return BallARetroRenderProbe.Measure(output, "game-floor-visible", mainForRendering);
            }
            finally { mainForRendering.cullingMask = originalMask; }
        }
        Check(portals.Length == 9, "All nine portal effects use the original particle implementation");
        Check(Object.FindObjectsByType<UnityEngine.VFX.VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
            "No borrowed VFX Graph portal remains in the game scene");
        Check(portals.All(p => p.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name == "BALL-A-RETRO/Original Pixel Particle"
            && p.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.isSupported), "Original portal shader is supported on the active graphics device");
        Check(portals.All(p => p.GetComponent<ParticleSystem>().main.scalingMode == ParticleSystemScalingMode.Hierarchy),
            "Portal particles follow the original parent scale in the board hierarchy");
        var activePortal = portals.First(p => p.gameObject.activeInHierarchy);
        var system = activePortal.GetComponent<ParticleSystem>();
        var particleBuffer = new ParticleSystem.Particle[BallARetroPortalVfx.ParticleBudget];
        Check(system.GetParticles(particleBuffer) > 0 && system.particleCount <= BallARetroPortalVfx.ParticleBudget,
            "Original portal stays within its bounded particle budget");
        var firstPositions = particleBuffer.Select(p => p.position).ToArray();
        yield return new WaitForSecondsRealtime(.2f);
        system.GetParticles(particleBuffer);
        Check(particleBuffer.Where((p, index) => (p.position - firstPositions[index]).sqrMagnitude > .000001f).Any(),
            "Original portal particles animate during gameplay");
        Check(portals.Count(p => new SerializedObject(p).FindProperty("style").enumValueIndex == 1) == 4,
            "Four fire vortices retain their distinct visual role");
        CheckPortalVisibility("game");
        ExerciseRailVisibility(portals);
        bool previousParticles = PSH_Script_EffectSettings.ParticlesEnabled;
        PSH_Script_EffectSettings.Set(PSH_Script_EffectSettings.Effect.Particles, false);
        yield return null;
        Check(portals.All(p => p.GetComponent<ParticleSystemRenderer>().forceRenderingOff), "Particles OFF suppresses every original portal renderer");
        PSH_Script_EffectSettings.Set(PSH_Script_EffectSettings.Effect.Particles, true);
        yield return null;
        Check(portals.All(p => !p.GetComponent<ParticleSystemRenderer>().forceRenderingOff), "Particles ON restores every original portal renderer");
        CapturePortal(portals.First(p => p.name == "Original Portal blue"), "portal-halo-preview");
        CapturePortal(portals.First(p => new SerializedObject(p).FindProperty("style").enumValueIndex == 1), "portal-fire-preview");
        // Optional visual reference capture uses development-only originals. These
        // prefabs remain excluded from the strict packed-assets release gate.
        if (Environment.GetCommandLineArgs().Contains("-comparePortalBaseline"))
        {
            foreach (var item in new[] {
                (path: "Assets/Public/Main_Game/Effect/Hovl Studio/Magic effects pack/Prefabs/Portals/Portal blue.prefab", radius: .7f, name: "portal-baseline-halo"),
                (path: "Assets/Cool Visual Effects - Part 1/Effects/Portals/Portal2.prefab", radius: 2f, name: "portal-baseline-fire") })
            {
                var original = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(item.path));
                original.transform.position = new Vector3(1000, 1000, 1000);
                original.transform.rotation = Quaternion.identity;
                foreach (var child in original.GetComponentsInChildren<Transform>()) child.gameObject.layer = 30;
                var warmCameraObject = new GameObject("Reference VFX warmup camera", typeof(Camera));
                var warmCamera = warmCameraObject.GetComponent<Camera>();
                warmCamera.transform.position = original.transform.position + Vector3.forward * item.radius * 4;
                warmCamera.transform.LookAt(original.transform.position, Vector3.up);
                warmCamera.orthographic = true;
                warmCamera.orthographicSize = item.radius * 1.8f;
                warmCamera.cullingMask = 1 << 30;
                var warmTarget = RenderTexture.GetTemporary(256, 256, 24);
                warmCamera.targetTexture = warmTarget;
                yield return new WaitForSecondsRealtime(1.5f);
                foreach (var effect in original.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>())
                {
                    effect.Reinit();
                    effect.Simulate(1f / 60, 120);
                }
                yield return null;
                yield return null;
                CaptureVisual(original, item.radius, item.name);
                File.WriteAllLines(Path.Combine(output, item.name + "-state.txt"),
                    original.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>()
                        .Select(effect => "culled=" + effect.culled + ", alive=" + effect.aliveParticleCount));
                warmCamera.targetTexture = null;
                Object.Destroy(warmCameraObject);
                RenderTexture.ReleaseTemporary(warmTarget);
                Object.Destroy(original);
                yield return null;
            }
        }
        PSH_Script_EffectSettings.Set(PSH_Script_EffectSettings.Effect.Particles, previousParticles);
        foreach (string code in new[] { "ko", "en", "ja" })
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(code);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var checklist = Object.FindObjectsByType<PSH_Script_LocalizedText>(FindObjectsSortMode.None)
                .Where(t => new[] { "HUD_Safety", "HUD_BlackHole", "HUD_Entrance", "HUD_Launcher" }
                    .Contains(new SerializedObject(t).FindProperty("key").stringValue)).ToArray();
            Check(checklist.Length == 4, "Four active gameplay checklist rows: " + code);
            foreach (var label in checklist)
            {
                var text = label.GetComponent<TMPro.TMP_Text>();
                text.ForceMeshUpdate();
                Check(text.textInfo.lineCount == 1 && !text.isTextOverflowing,
                    "Checklist stays on one line without clipping: " + code + "/" + text.text);
            }
            CaptureFrame("game-checklist-" + code);
        }
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ko");
        yield return null;
        CaptureFrame("game");
        var entry = Object.FindObjectsByType<KHS_Script_PortalController>(FindObjectsSortMode.None).First(p =>
        {
            var data = new SerializedObject(p);
            return p.isActivated && data.FindProperty("exitPortal").objectReferenceValue != null && data.FindProperty("portalIndex").intValue >= 0;
        });
        var trigger = typeof(KHS_Script_PortalController).GetMethod("OnTriggerEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var testBall = new GameObject("Portal audit ball");
        testBall.tag = "Ball";
        testBall.transform.position = new Vector3(1000, 1000, 1000);
        var testCollider = testBall.AddComponent<SphereCollider>();
        trigger.Invoke(entry, new object[] { testCollider });
        Check(entry.isActivated, "Portal ignores a Ball collider without a Rigidbody");
        var testBody = testBall.AddComponent<Rigidbody>();
        testBody.useGravity = false;
        trigger.Invoke(entry, new object[] { testCollider });
        Check(!testBall.activeSelf && testBody.isKinematic, "Portal transfer hides and holds the entering ball");
        yield return new WaitForSecondsRealtime(2.15f);
        Check(testBall.activeSelf && !testBody.isKinematic && testBody.linearVelocity.sqrMagnitude > 0,
            "Portal transfer restores and launches the ball at the exit");
        yield return new WaitForSecondsRealtime(.55f);
        Check(entry.isActivated, "Portal reactivates after the transfer cooldown");
        Object.Destroy(testBall);
        yield return CheckRouletteRoundTransition();
        settings = Object.FindFirstObjectByType<PSH_Script_SettingController>();
        settings.ToggleMenu();
        settings.OnClickGoToLobby();
        yield return null;
        Check(settings.panelWarning.activeSelf, "Game return confirmation opens");
        settings.ConfirmExit();
        while (SceneManager.GetActiveScene().name != "Main_Scene_Lobby") yield return null;
        yield return new WaitForSecondsRealtime(2);
        Check(gameChoices == null && CJS_Script_ChoiceState.I == null, "Game choice history is released on return to lobby");
        Check(!BallARetroStartupNotice.IsVisible, "Startup notice does not repeat when returning to the lobby");
        Check(BallARetroPersonalRecordStore.Read("Reviewer", "ReleaseSmoke", 1)[0].score == 9000, "Personal best survives all scene transitions");
        Check(!SteamManager.Initialized, "Steam remains disabled after scene transitions");
    }
    IEnumerator CheckRouletteRoundTransition()
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var manager = Object.FindFirstObjectByType<KHS_Script_CameraManager>();
        var score = KHS_Script_ScoreManager.Instance;
        var roulette = Object.FindFirstObjectByType<YJ_Script_RoulettePhysicsController>();
        var choices = Object.FindFirstObjectByType<CJS_Script_ChoiceUIController>();
        var panel = (GameObject)typeof(CJS_Script_ChoiceUIController).GetField("panelRoot", flags).GetValue(choices);
        var skip = (UnityEngine.UI.Button)typeof(CJS_Script_ChoiceUIController).GetField("btnSkip", flags).GetValue(choices);
        Check(roulette != null && panel != null && skip != null && score.IsPlaying,
            "Roulette round regression uses the real scene score, wheel and choice UI");
        float duration = roulette.stopDuration;
        // Exercise the real scoring coroutine without replaying the unrelated rail ride.
        // Null capture is supported by the production coroutine; no physical ball is moved.
        roulette.stopDuration = .15f;
        score.curScore = score.targetScore - 1;
        manager.MainCamOn();
        manager.SubCamOn(3);
        Check(manager.cameraGos.Where(go => go.activeInHierarchy).SequenceEqual(new[] { manager.cameraGos[3] }),
            "Only the roulette camera is active before the scoring transition");
        roulette.StartStopSequence(null);
        roulette.UpdateCurrentSlot(1);
        float deadline = Time.realtimeSinceStartup + 2f;
        while (score.IsPlaying && Time.realtimeSinceStartup < deadline) yield return null;
        Check(!score.IsPlaying && Time.timeScale == 0f,
            "Roulette score enters the real paused round transition within two seconds");
        Check(manager.cameraGos.Where(go => go.activeInHierarchy).SequenceEqual(new[] { manager.cameraGos[0] }),
            "Round banner camera immediately replaces roulette instead of remaining hidden");
        CaptureFrame("roulette-round-banner", manager.cameraGos[0].GetComponent<Camera>());
        deadline = Time.realtimeSinceStartup + 9f;
        while ((!panel.activeInHierarchy || !manager.cameraGos[2].activeInHierarchy)
            && Time.realtimeSinceStartup < deadline) yield return null;
        Check(panel.activeInHierarchy && manager.cameraGos[2].activeInHierarchy,
            "Round progression displays the choice panel without a hidden roulette wait");
        yield return new WaitForSecondsRealtime(2.2f);
        Check(manager.cameraGos.Where(go => go.activeInHierarchy).SequenceEqual(new[] { manager.cameraGos[2] }),
            "Monitor is the sole camera throughout the paused choice screen");
        Check(Cursor.visible && Cursor.lockState == CursorLockMode.None && Time.timeScale == 0f,
            "Choices remain visible, clickable and paused after roulette scoring");
        CaptureFrame("roulette-visible-choices", manager.cameraGos[2].GetComponent<Camera>());
        skip.onClick.Invoke();
        yield return new WaitForSecondsRealtime(2.3f);
        Check(!(bool)typeof(CJS_Script_ChoiceUIController).GetField("isOpen", flags).GetValue(choices) && Time.timeScale == 1f,
            "Skipping a roulette round choice resumes gameplay after camera return");
        Check(!Cursor.visible && (Application.isBatchMode || !Application.isFocused || Cursor.lockState == CursorLockMode.Locked),
            "Gameplay hides the cursor after skipping the choice (native lock requires focused player)");
        Check(manager.cameraGos.Where(go => go.activeInHierarchy).SequenceEqual(new[] { manager.cameraGos[0] }),
            "Returning from choices cannot leave a second roulette camera active");
        yield return new WaitForSecondsRealtime(2.2f);
        Check(!roulette.GetComponent<Renderer>().enabled && !roulette.GetComponent<Collider>().enabled,
            "Roulette resumes its result hold and opens the drop after the choice closes");
        roulette.ResetRoulette();
        Check(roulette.GetComponent<Renderer>().enabled && roulette.GetComponent<Collider>().enabled,
            "Resetting the interrupted roulette restores its plate and collider");
        roulette.stopDuration = duration;
        CaptureFrame("roulette-gameplay-resumed", manager.cameraGos[0].GetComponent<Camera>());
    }
    IEnumerator CheckTutorialCameraRecovery(CJS_TutorialDirector tutorial)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var manager = Object.FindFirstObjectByType<KHS_Script_CameraManager>();
        var follow = Object.FindFirstObjectByType<KHS_Script_CameraController>();
        var camera = manager.cameraGos[0].GetComponent<Camera>();
        Check(follow != null && !Object.FindObjectsByType<CJS_Script_CameraFollowBall>(FindObjectsSortMode.None).Any(component => component.enabled),
            "Tutorial has one gameplay camera follower and no competing legacy follower");
        var defaultPosition = (Vector3)typeof(KHS_Script_CameraController).GetField("_defaultPos", flags).GetValue(follow);
        var localPosition = camera.transform.localPosition;
        var localRotation = camera.transform.localRotation;
        float defaultFov = camera.fieldOfView;
        Object.FindFirstObjectByType<PSH.PSH_Script_DialogueUI>().SkipDialogue();
        yield return new WaitForSecondsRealtime(.5f);
        var step = typeof(CJS_TutorialDirector).GetMethod("GoToStep", flags);
        var stepType = step.GetParameters()[0].ParameterType;
        foreach (string name in new[] { "Flippers", "Entrance2F", "Plunger", "ScoreMission" })
        {
            step.Invoke(tutorial, new[] { Enum.Parse(stepType, name) });
            yield return new WaitForSecondsRealtime(.45f);
        }
        Check(Vector3.Distance(camera.transform.localPosition, localPosition) < .02f
            && Quaternion.Angle(camera.transform.localRotation, localRotation) < .2f,
            "Tutorial focus sequence restores the camera child offset before gameplay tracking");
        // Mimic an escaped tracked ball without moving the real physics object.
        Transform realBall = follow.ball;
        var escaped = new GameObject("Camera bounds audit target");
        escaped.transform.position = new Vector3(0, 0, 1000);
        follow.ball = escaped.transform;
        follow.StartFollow();
        yield return new WaitForSecondsRealtime(.3f);
        Check(follow.transform.position.z <= follow.maxWorldZ + .01f,
            "Tutorial camera does not follow an escaped ball beyond the upper board bound");
        escaped.transform.position = new Vector3(0, 0, -1000);
        yield return new WaitForSecondsRealtime(.3f);
        Check(follow.transform.position.z >= follow.minWorldZ - .01f,
            "Tutorial camera does not follow an escaped ball behind the lower board bound");
        follow.ball = realBall;
        Object.Destroy(escaped);
        tutorial.scoreManager.AddScore(100);
        // Failure can happen while a focus restoration is still in progress.
        tutorial.focus.FocusTo(tutorial.focusPlunger);
        step.Invoke(tutorial, new[] { Enum.Parse(stepType, "Failed") });
        yield return new WaitForSecondsRealtime(1.2f);
        Check(tutorial.resultPanel.activeInHierarchy && Time.timeScale == 0,
            "Tutorial failure result remains paused and visible during camera recovery");
        Check(Vector3.Distance(follow.transform.position, defaultPosition) < .02f
            && Vector3.Distance(camera.transform.localPosition, localPosition) < .02f
            && Quaternion.Angle(camera.transform.localRotation, localRotation) < .2f
            && Mathf.Abs(camera.fieldOfView - defaultFov) < .1f,
            "Tutorial retry screen restores the authored rig, camera offset and FOV while paused");
        Check(Cursor.visible && Cursor.lockState == CursorLockMode.None,
            "Tutorial failure controls retain a visible unlocked pointer");
        CaptureFrame("tutorial-retry-camera-restored", camera);
    }
    static bool IsReplacedVendorVisual(string path) => !path.EndsWith(".cs") && (path.Contains("/Hovl Studio/")
        || path.Contains("/VisualX_Studio/") || path.Contains("/Cool Visual Effects - Part 1/"));
    void CheckSceneVisualReferences(string scene)
    {
        CheckPortalVisibility(scene);
        var materials = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct().ToArray();
        Check(!materials.Any(material => IsReplacedVendorVisual(AssetDatabase.GetAssetPath(material))
            || material.GetTexturePropertyNames().Any(name => IsReplacedVendorVisual(AssetDatabase.GetAssetPath(material.GetTexture(name)))))
            && !Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(img => IsReplacedVendorVisual(AssetDatabase.GetAssetPath(img.sprite))),
            "No replaced vendor material, texture or UI sprite is assigned in " + scene);
        var trail = materials.FirstOrDefault(material => material.shader != null && material.shader.name == "BALL-A-RETRO/Original Pixel Trail");
        Check(trail != null && trail.shader.isSupported, "Original trail shader is assigned and supported in " + scene);
    }
    static void EmitUI(ScriptableRenderContext context, Camera camera) => ScriptableRenderContext.EmitGeometryForCamera(camera);
    static Rect ScreenBounds(RectTransform rect)
    {
        var canvas = rect.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var points = corners.Select(p => RectTransformUtility.WorldToScreenPoint(camera, p)).ToArray();
        return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
    }
    void CheckPortalVisibility(string scene)
    {
        int floor = LayerMask.NameToLayer("2F");
        var portals = Object.FindObjectsByType<BallARetroPortalVfx>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var floorPortals = portals.Where(p => p.name == "Original Portal red" || p.name == "Original Portal blue").ToArray();
        Check(floorPortals.Length == 2 && floorPortals.All(p => p.gameObject.layer == floor),
            "Red and blue portal effects retain the 2F layer: " + scene);
        Check(portals.Except(floorPortals).All(p => p.gameObject.layer == 0),
            "Yellow and fire portal effects retain their original layer: " + scene);
        var cameras = Object.FindFirstObjectByType<KHS_Script_CameraManager>();
        var mainCamera = cameras.cameraGos[0].GetComponent<Camera>();
        Check((mainCamera.cullingMask & (1 << floor)) == 0,
            "Main board camera hides floor portal effects before entry: " + scene);
    }
    void ExerciseRailVisibility(BallARetroPortalVfx[] portals)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var entry = Object.FindObjectsByType<YJ_Script_RailEntrance>(FindObjectsSortMode.None)
            .First(e => e.mainCam != null && e.railMover != null && e.railTimeline != null);
        var exit = Object.FindObjectsByType<YJ_Script_RouletteEntrance>(FindObjectsSortMode.None)
            .First(e => e.mainCam == entry.mainCam && e.railTimeline != null);
        var ball = new GameObject("Rail visibility audit ball");
        ball.SetActive(false);
        ball.tag = "Ball";
        var body = ball.AddComponent<Rigidbody>();
        body.useGravity = false;
        var collider = ball.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        var controller = ball.AddComponent<YJ_Script_BallController>();
        controller.enabled = false;
        typeof(YJ_Script_BallController).GetField("rigidBody", flags).SetValue(controller, body);
        ball.SetActive(true);
        int layerBit = 1 << LayerMask.NameToLayer("2F");
        bool triggerWasEnabled = entry.GetComponent<Collider>().enabled;
        for (int pass = 0; pass < 2; pass++)
        {
            typeof(YJ_Script_RailEntrance).GetMethod("OnTriggerEnter", flags).Invoke(entry, new object[] { collider });
            Check((entry.mainCam.cullingMask & layerBit) != 0,
                "Actual rail entry exposes 2F portal effects: pass " + pass);
            if (pass == 0) CaptureFrame("game-portals-floor-visible", entry.mainCam);
            entry.railTimeline.Stop();
            typeof(YJ_Script_RouletteEntrance).GetField("capturedBall", flags).SetValue(exit, controller);
            typeof(YJ_Script_RouletteEntrance).GetMethod("OnRailRideEnd", flags).Invoke(exit, new object[] { exit.railTimeline });
            Check((exit.mainCam.cullingMask & layerBit) == 0,
                "Actual rail exit hides 2F portal effects again: pass " + pass);
        }
        entry.GetComponent<Collider>().enabled = triggerWasEnabled;
        Object.Destroy(ball);
        CaptureFrame("game-portals-hidden-after-exit", entry.mainCam);
    }
    void CapturePortal(BallARetroPortalVfx portal, string name)
    {
        var clone = Object.Instantiate(portal.gameObject);
        clone.transform.SetParent(null);
        clone.transform.position = new Vector3(1000, 1000, 1000);
        clone.transform.rotation = Quaternion.identity;
        clone.transform.localScale = Vector3.one;
        try { CaptureVisual(clone, new SerializedObject(portal).FindProperty("radius").floatValue, name); }
        finally { Object.DestroyImmediate(clone); }
    }
    void CaptureVisual(GameObject visual, float radius, string name)
    {
        var layers = visual.GetComponentsInChildren<Transform>(true).Select(t => (go: t.gameObject, layer: t.gameObject.layer)).ToArray();
        var cameraObject = new GameObject("Original portal preview", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false;
        camera.transform.position = visual.transform.position + visual.transform.forward * Mathf.Max(radius * 4, 1);
        camera.transform.LookAt(visual.transform.position, visual.transform.up);
        camera.orthographic = true;
        camera.orthographicSize = radius * 1.8f;
        camera.nearClipPlane = .01f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.015f, .025f, .04f);
        camera.cullingMask = 1 << 30;
        try
        {
            foreach (var item in layers) item.go.layer = 30;
            CaptureFrame(name, camera);
        }
        finally
        {
            foreach (var item in layers) item.go.layer = item.layer;
            Object.Destroy(cameraObject);
        }
    }
    void CaptureFrame(string name, Camera previewCamera = null, int width = 1920, int height = 1080)
    {
        var camera = previewCamera != null ? previewCamera : Camera.main;
        if (camera == null) throw new Exception("No main camera for " + name);
        // Batch editors have no Game View. Temporarily route overlay UI through the
        // real scene camera for an offscreen render, restoring it before continuing.
        var overlays = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => previewCamera == null && c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            .Select(c => (canvas: c, camera: c.worldCamera, distance: c.planeDistance)).ToArray();
        var target = RenderTexture.GetTemporary(width, height, 24);
        var previous = RenderTexture.active;
        var previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;
        int mask = camera.cullingMask;
        Texture2D pixels = null;
        RenderPipelineManager.beginCameraRendering += EmitUI;
        try
        {
            if (previewCamera == null) camera.cullingMask |= 1 << 5;
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            foreach (var item in overlays)
            {
                item.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                item.canvas.worldCamera = camera;
                item.canvas.planeDistance = camera.nearClipPlane + 0.1f;
            }
            Canvas.ForceUpdateCanvases();
            RefreshLoadingLayout();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), pixels.EncodeToPNG());
        }
        finally
        {
            RenderPipelineManager.beginCameraRendering -= EmitUI;
            camera.cullingMask = mask;
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            foreach (var item in overlays)
            {
                item.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                item.canvas.worldCamera = item.camera;
                item.canvas.planeDistance = item.distance;
            }
            Canvas.ForceUpdateCanvases();
            RefreshLoadingLayout();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            if (pixels != null) Object.Destroy(pixels);
        }
    }
    static void RefreshLoadingLayout()
    {
        foreach (var loading in Object.FindObjectsByType<BallARetroLoadingVfx>(FindObjectsSortMode.None))
            typeof(BallARetroLoadingVfx).GetMethod("PositionBelowLobbyButtons",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(loading, null);
        Canvas.ForceUpdateCanvases();
    }
    void Finish()
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnLog;
        if (previousLocale == null) PlayerPrefs.DeleteKey(PSH_Script_Localization.PreferenceKey);
        else PlayerPrefs.SetString(PSH_Script_Localization.PreferenceKey, previousLocale);
        if (previousRecord == null) PlayerPrefs.DeleteKey(recordKey);
        else PlayerPrefs.SetString(recordKey, previousRecord);
        PlayerPrefs.Save();
        File.WriteAllText(Path.Combine(output, "scene-audit.json"), JsonUtility.ToJson(new Result {
            success = errors.Count == 0, checks = passed.ToArray(), errors = errors.ToArray()
        }, true));
        EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
    }
}
#endif
