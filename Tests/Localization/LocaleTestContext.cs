using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class LocaleTestContext
{
    private sealed class FixtureLocales : ILocalesProvider
    {
        public List<Locale> Locales { get; } = new List<Locale>();
        public Locale GetLocale(LocaleIdentifier id) => Locales.FirstOrDefault(l => l.Identifier == id);
        public void AddLocale(Locale locale) => Locales.Add(locale);
        public bool RemoveLocale(Locale locale) => Locales.Remove(locale);
    }
    public static LocalizationSettings Create(string code = "ko")
    {
        var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
        LocalizationSettings.Instance = settings;
        var locales = new FixtureLocales();
        foreach (var name in new[] { "Korean (ko)", "English (en)", "Japanese (ja)" })
            locales.AddLocale(Resources.Load<Locale>("LocalizationData/" + name));
        LocalizationSettings.AvailableLocales = locales;
        LocalizationSettings.PreloadBehavior = PreloadBehavior.NoPreloading;
        settings.GetStartupLocaleSelectors().Clear();
        settings.GetStartupLocaleSelectors().Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier(code) });
        return settings;
    }
    public static void Select(string code) => LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(code);
}
