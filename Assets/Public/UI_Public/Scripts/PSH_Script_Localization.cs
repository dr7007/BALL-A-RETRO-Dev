using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

// Shared StringTable assets serve LocalizeStringEvent and code-driven UI alike.
// Resource access lets paused UI and dialogue change language synchronously.
public static class PSH_Script_Localization
{
    public const string PreferenceKey = "selected-locale";
    private static readonly Dictionary<string, StringTable> tables = new();
    public static string LanguageCode
    {
        get
        {
            var locale = LocalizationSettings.SelectedLocale;
            string code = locale != null ? locale.Identifier.Code : PlayerPrefs.GetString(PreferenceKey, "en");
            if (code.StartsWith("ko")) return "ko";
            if (code.StartsWith("ja")) return "ja";
            return "en";
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => tables.Clear();

    public static StringTable Table(string name)
    {
        string path = "LocalizationData/" + name + "_" + LanguageCode;
        if (!tables.TryGetValue(path, out var table))
        {
            table = Resources.Load<StringTable>(path);
            tables[path] = table;
        }
        return table;
    }
    public static string Text(string key, params object[] arguments)
    {
        string value = Table("UITable")?.GetEntry(key)?.LocalizedValue;
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogError("[Localization] Missing UITable key: " + key + " (" + LanguageCode + ")");
            return key;
        }
        return arguments.Length == 0 ? value : string.Format(CultureInfo.GetCultureInfo(LanguageCode), value, arguments);
    }
}
