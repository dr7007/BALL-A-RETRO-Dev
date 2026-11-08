#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Standalone alternative to calling the probe from BallARetroSceneProbe.
// Use an isolated project, -executeMethod BallARetroChoiceIconAudit.Run
// -auditOutput <folder>, optionally -choiceIconScene tutorial and
// -choiceIconLanguage ko|en|ja|all (default ko). Do not use -quit:
// the play-mode runner exits after validation and screenshot capture.
public static class BallARetroChoiceIconAudit
{
    const string Pending = "BallARetro.ChoiceIconAudit.Pending";
    public const string Output = "BallARetro.ChoiceIconAudit.Output";
    public const string Label = "BallARetro.ChoiceIconAudit.Label";
    public const string Language = "BallARetro.ChoiceIconAudit.Language";
    public const string HadLocalePreference = "BallARetro.ChoiceIconAudit.HadLocalePreference";
    public const string PreviousLocalePreference = "BallARetro.ChoiceIconAudit.PreviousLocalePreference";
    public static readonly List<string> Errors = new();
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        int output = Array.IndexOf(args, "-auditOutput");
        if (output < 0 || output + 1 >= args.Length) throw new ArgumentException("-auditOutput is required");
        int scene = Array.IndexOf(args, "-choiceIconScene");
        bool tutorial = scene >= 0 && scene + 1 < args.Length && args[scene + 1] == "tutorial";
        int languageIndex = Array.IndexOf(args, "-choiceIconLanguage");
        string language = languageIndex < 0 ? "ko" : languageIndex + 1 < args.Length ? args[languageIndex + 1] : "";
        if (language != "ko" && language != "en" && language != "ja" && language != "all")
            throw new ArgumentException("-choiceIconLanguage must be ko, en, ja or all");
        Directory.CreateDirectory(args[output + 1]);
        SessionState.SetString(Output, args[output + 1]);
        SessionState.SetString(Label, tutorial ? "tutorial" : "game");
        SessionState.SetString(Language, language);
        SessionState.SetBool(HadLocalePreference, PlayerPrefs.HasKey(PSH_Script_Localization.PreferenceKey));
        SessionState.SetString(PreviousLocalePreference, PlayerPrefs.GetString(PSH_Script_Localization.PreferenceKey, ""));
        foreach (string releaseScene in new[] { "Main_Scene_Game", "Main_Scene_Tutorial" })
            AssetDatabase.ImportAsset("Assets/MainScenes/" + releaseScene + ".unity",
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        SessionState.SetBool(Pending, true);
        EditorSceneManager.OpenScene("Assets/MainScenes/Main_Scene_Lobby.unity");
        EditorApplication.isPlaying = true;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void CaptureBeforeSceneInitialization()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        Errors.Clear();
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateRunner()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("Choice icon audit runner").AddComponent<BallARetroChoiceIconAuditRunner>();
    }
    static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            Errors.Add(message + "\n" + trace);
    }
    public static void StopCapture() => Application.logMessageReceived -= OnLog;
}

public sealed class BallARetroChoiceIconAuditRunner : MonoBehaviour
{
    [Serializable] sealed class Result { public bool success; public string[] checks, errors; }
    readonly List<string> checks = new();
    readonly List<string> errors = BallARetroChoiceIconAudit.Errors;
    bool finished;
    float started;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        started = Time.realtimeSinceStartup;
    }
    void Update()
    {
        if (!finished && Time.realtimeSinceStartup - started > 90)
        { errors.Add("Choice icon audit timed out."); Finish(); }
    }
    IEnumerator Start()
    {
        yield return null;
        yield return LocalizationSettings.InitializationOperation;
        string output = SessionState.GetString(BallARetroChoiceIconAudit.Output, "");
        string label = SessionState.GetString(BallARetroChoiceIconAudit.Label, "game");
        var notice = UnityEngine.Object.FindFirstObjectByType<BallARetroStartupNotice>();
        if (notice != null)
        {
            var proceed = notice.transform.Find("Notice/Continue").GetComponent<Button>();
            while (proceed != null && !proceed.interactable) yield return null;
            proceed.onClick.Invoke();
            yield return null;
        }
        var lobby = UnityEngine.Object.FindFirstObjectByType<PSH.PSH_Script_LobbyUIManager>();
        if (lobby == null || PSH.PSH_Script_SceneLoader.Instance == null)
        { errors.Add("Lobby did not initialize its normal scene loader."); Finish(); yield break; }
        if (label == "tutorial") lobby.OnClick_Tutorial();
        else lobby.OnClick_StartGame();
        string destination = label == "tutorial" ? "Main_Scene_Tutorial" : "Main_Scene_Game";
        while (SceneManager.GetActiveScene().name != destination) yield return null;
        yield return new WaitForSecondsRealtime(4);
        checks.Add("Entered " + destination + " through the lobby and persistent scene loader");
        string language = SessionState.GetString(BallARetroChoiceIconAudit.Language, "ko");
        var languages = language == "all" ? new[] { "ko", "en", "ja" } : new[] { language };
        var previousLocale = LocalizationSettings.SelectedLocale;
        foreach (string code in languages)
        {
            try
            {
                var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
                if (locale == null) throw new InvalidOperationException("Missing requested locale: " + code);
                LocalizationSettings.SelectedLocale = locale;
            }
            catch (Exception ex) { errors.Add(ex.ToString()); break; }
            yield return null;
            try
            {
                BallARetroChoiceIconProbe.ValidateAndCapture(output, label + "-" + code, (ok, check) => {
                    if (!ok) throw new InvalidOperationException(check);
                    checks.Add(check);
                });
            }
            catch (Exception ex) { errors.Add(ex.ToString()); }
        }
        LocalizationSettings.SelectedLocale = previousLocale;
        Finish();
    }
    void Finish()
    {
        if (finished) return;
        finished = true;
        if (SessionState.GetBool(BallARetroChoiceIconAudit.HadLocalePreference, false))
            PlayerPrefs.SetString(PSH_Script_Localization.PreferenceKey,
                SessionState.GetString(BallARetroChoiceIconAudit.PreviousLocalePreference, ""));
        else
            PlayerPrefs.DeleteKey(PSH_Script_Localization.PreferenceKey);
        PlayerPrefs.Save();
        string output = SessionState.GetString(BallARetroChoiceIconAudit.Output, "");
        string label = SessionState.GetString(BallARetroChoiceIconAudit.Label, "game");
        File.WriteAllText(Path.Combine(output, label + "-choice-icon-result.json"), JsonUtility.ToJson(new Result {
            success = errors.Count == 0, checks = checks.ToArray(), errors = errors.ToArray()
        }, true));
        BallARetroChoiceIconAudit.StopCapture();
        EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
    }
}
#endif
