using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class PSH_Script_LanguageManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown languageDropdown;
    private readonly List<Locale> locales = new();
    private Coroutine initialization;

    private void OnEnable()
    {
        if (languageDropdown == null) return;
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        LocalizationSettings.SelectedLocaleChanged += Sync;
        initialization = StartCoroutine(Initialize());
    }
    private void OnDisable()
    {
        if (initialization != null) StopCoroutine(initialization);
        initialization = null;
        if (languageDropdown != null) languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        LocalizationSettings.SelectedLocaleChanged -= Sync;
    }
    private IEnumerator Initialize()
    {
        languageDropdown.interactable = false;
        yield return LocalizationSettings.InitializationOperation;
        locales.Clear();
        var options = new List<string>();
        foreach (string code in new[] { "ko", "en", "ja" })
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale == null) continue;
            locales.Add(locale);
            options.Add(code == "ko" ? "한국어" : code == "ja" ? "日本語" : "English");
        }
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(options);
        Sync(LocalizationSettings.SelectedLocale);
        languageDropdown.interactable = locales.Count > 0;
        initialization = null;
    }
    private void Sync(Locale locale)
    {
        if (languageDropdown == null) return;
        int index = locales.IndexOf(locale);
        if (index < 0) return;
        languageDropdown.SetValueWithoutNotify(index);
        languageDropdown.RefreshShownValue();
    }
    private void OnLanguageChanged(int index)
    {
        if (index < 0 || index >= locales.Count) return;
        var locale = locales[index];
        PlayerPrefs.SetString(PSH_Script_Localization.PreferenceKey, locale.Identifier.Code);
        PlayerPrefs.Save();
        LocalizationSettings.SelectedLocale = locale;
    }
}
