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
        if (!finished && Time.realtimeSinceStartup - started > 150f)
        { errors.Add("Full scene audit timed out."); Finish(); }
    }
    IEnumerator GuardedRun()
    {
        var run = Run();
        while (!finished)
        {
            bool next;
            try { next = run.MoveNext(); }
            catch (Exception ex) { errors.Add(ex.ToString()); Finish(); yield break; }
            if (!next) { Finish(); yield break; }
            yield return run.Current;
        }
    }
    void Check(bool ok, string label)
    {
        if (!ok) throw new Exception(label);
        passed.Add(label);
    }
    IEnumerator Run()
    {
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
        lobby.OnClick_Tutorial();
        yield return null;
        var loadingVfx = Object.FindFirstObjectByType<BallARetroLoadingVfx>();
        Check(loadingVfx != null && !loadingVfx.raycastTarget, "Original loading particles appear without intercepting input");
        Check(loadingVfx.GetComponentInParent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay,
            "Loading indicator does not depend on the outgoing scene camera");
        var loadingBackdrop = loadingVfx.transform.parent.GetComponent<UnityEngine.UI.Image>();
        Check(loadingBackdrop != null && loadingBackdrop.enabled && loadingBackdrop.raycastTarget
            && loadingBackdrop.color.a == 1, "Loading backdrop covers the previous menu and blocks duplicate clicks");
        CaptureFrame("loading-original");
        while (SceneManager.GetActiveScene().name != "Main_Scene_Tutorial") yield return null;
        yield return new WaitForSecondsRealtime(4);
        var tutorial = Object.FindFirstObjectByType<CJS_TutorialDirector>();
        Check(tutorial != null && tutorial.scoreManager != null && tutorial.plunger != null, "Full tutorial scene initializes with score and plunger");
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
        Check(portals.Length == 9, "All nine portal effects use the original particle implementation");
        Check(Object.FindObjectsByType<UnityEngine.VFX.VisualEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
            "No borrowed VFX Graph portal remains in the game scene");
        Check(portals.All(p => p.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name == "BALL-A-RETRO/Original Pixel Particle"
            && p.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.isSupported), "Original portal shader is supported on the active graphics device");
        var activePortal = portals.First(p => p.gameObject.activeInHierarchy);
        var system = activePortal.GetComponent<ParticleSystem>();
        var particleBuffer = new ParticleSystem.Particle[144];
        Check(system.GetParticles(particleBuffer) == 144, "Original portal emits its bounded particle count");
        var firstPosition = particleBuffer[0].position;
        yield return new WaitForSecondsRealtime(.2f);
        system.GetParticles(particleBuffer);
        Check((particleBuffer[0].position - firstPosition).sqrMagnitude > .000001f, "Original portal particles animate during gameplay");
        bool previousParticles = PSH_Script_EffectSettings.ParticlesEnabled;
        PSH_Script_EffectSettings.Set(PSH_Script_EffectSettings.Effect.Particles, false);
        yield return null;
        Check(portals.All(p => p.GetComponent<ParticleSystemRenderer>().forceRenderingOff), "Particles OFF suppresses every original portal renderer");
        PSH_Script_EffectSettings.Set(PSH_Script_EffectSettings.Effect.Particles, true);
        yield return null;
        Check(portals.All(p => !p.GetComponent<ParticleSystemRenderer>().forceRenderingOff), "Particles ON restores every original portal renderer");
        CapturePortal(activePortal);
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
    static void EmitUI(ScriptableRenderContext context, Camera camera) => ScriptableRenderContext.EmitGeometryForCamera(camera);
    void CapturePortal(BallARetroPortalVfx portal)
    {
        int layer = portal.gameObject.layer;
        var cameraObject = new GameObject("Original portal preview", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false;
        float radius = new SerializedObject(portal).FindProperty("radius").floatValue * Mathf.Abs(portal.transform.lossyScale.x);
        camera.transform.position = portal.transform.position + portal.transform.forward * Mathf.Max(radius * 4, 1);
        camera.transform.LookAt(portal.transform.position, portal.transform.up);
        camera.orthographic = true;
        camera.orthographicSize = radius * 1.4f;
        camera.nearClipPlane = .01f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.015f, .025f, .04f);
        camera.cullingMask = 1 << 30;
        try
        {
            portal.gameObject.layer = 30;
            CaptureFrame("portal-original-preview", camera);
        }
        finally
        {
            portal.gameObject.layer = layer;
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
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            if (pixels != null) Object.Destroy(pixels);
        }
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
