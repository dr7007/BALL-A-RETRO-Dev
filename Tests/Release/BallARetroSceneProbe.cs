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
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
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
        yield return new WaitForSecondsRealtime(3);
        Check(!BallARetroPlatform.IsSteamBuild && !SteamManager.Initialized, "STOVE starts without Steam initialization");
        yield return LocalizationSettings.InitializationOperation;
        var lobby = Object.FindFirstObjectByType<PSH.PSH_Script_LobbyUIManager>();
        Check(lobby != null, "Full lobby scene initializes");
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
        while (SceneManager.GetActiveScene().name != "Main_Scene_Tutorial") yield return null;
        yield return new WaitForSecondsRealtime(4);
        var tutorial = Object.FindFirstObjectByType<CJS_TutorialDirector>();
        Check(tutorial != null && tutorial.scoreManager != null && tutorial.plunger != null, "Full tutorial scene initializes with score and plunger");
        Check(tutorial.tutorialTargetScore == 1000 && !tutorial.resultPanel.activeSelf, "Tutorial goal and initial result state");
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
        lobby = Object.FindFirstObjectByType<PSH.PSH_Script_LobbyUIManager>();
        lobby.OnClick_StartGame();
        while (SceneManager.GetActiveScene().name != "Main_Scene_Game") yield return null;
        yield return new WaitForSecondsRealtime(4);
        Check(Object.FindFirstObjectByType<KHS_Script_ScoreManager>() != null, "Full game scene initializes");
        targetHUD = Object.FindFirstObjectByType<KHS_Script_UIToTScore>();
        Check(targetHUD != null && targetHUD.GetComponent<TMPro.TMP_Text>().text.Replace(" ", "") == "/" + KHS_Script_ScoreManager.Instance.targetScore,
            "Game HUD displays the initialized round target");
        yield return null;
        CaptureFrame("game");
        settings = Object.FindFirstObjectByType<PSH_Script_SettingController>();
        settings.ToggleMenu();
        settings.OnClickGoToLobby();
        yield return null;
        Check(settings.panelWarning.activeSelf, "Game return confirmation opens");
        settings.ConfirmExit();
        while (SceneManager.GetActiveScene().name != "Main_Scene_Lobby") yield return null;
        yield return new WaitForSecondsRealtime(2);
        Check(BallARetroPersonalRecordStore.Read("Reviewer", "ReleaseSmoke", 1)[0].score == 9000, "Personal best survives all scene transitions");
        Check(!SteamManager.Initialized, "Steam remains disabled after scene transitions");
    }
    static void EmitUI(ScriptableRenderContext context, Camera camera) => ScriptableRenderContext.EmitGeometryForCamera(camera);
    void CaptureFrame(string name)
    {
        var camera = Camera.main;
        if (camera == null) throw new Exception("No main camera for " + name);
        // Batch editors have no Game View. Temporarily route overlay UI through the
        // real scene camera for an offscreen render, restoring it before continuing.
        var overlays = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            .Select(c => (canvas: c, camera: c.worldCamera, distance: c.planeDistance)).ToArray();
        var target = RenderTexture.GetTemporary(1920, 1080, 24);
        var previous = RenderTexture.active;
        var previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;
        int mask = camera.cullingMask;
        Texture2D pixels = null;
        RenderPipelineManager.beginCameraRendering += EmitUI;
        try
        {
            camera.cullingMask |= 1 << 5;
            camera.targetTexture = target;
            camera.aspect = 1920f / 1080f;
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
