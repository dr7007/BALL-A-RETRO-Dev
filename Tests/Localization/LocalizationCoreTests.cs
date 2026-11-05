using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class LocalizationCoreTests
{
    private LocalizationSettings settings;
    private GameObject root;
    private string savedLocale;
    [UnitySetUp] public IEnumerator Setup()
    {
        savedLocale = PlayerPrefs.HasKey("selected-locale") ? PlayerPrefs.GetString("selected-locale") : null;
        settings = LocaleTestContext.Create();
        yield return LocalizationSettings.InitializationOperation;
        root = new GameObject("Localization fixture");
    }
    [TearDown] public void Cleanup()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(settings);
        LocalizationSettings.Instance = null;
        if (savedLocale == null) PlayerPrefs.DeleteKey("selected-locale"); else PlayerPrefs.SetString("selected-locale", savedLocale);
        PlayerPrefs.Save(); Time.timeScale = 1;
    }
    [TestCase("ko")][TestCase("en")][TestCase("ja")]
    public void BothTablesHaveEveryKeyAndFormatArguments(string code)
    {
        LocaleTestContext.Select(code);
        foreach (var name in new[] { "UITable", "DialogueTable" })
        {
            var table = PSH_Script_Localization.Table(name);
            Assert.That(table, Is.Not.Null);
            foreach (var key in table.SharedData.Entries)
            {
                string text = table.GetEntry(key.Id)?.LocalizedValue;
                Assert.That(text, Is.Not.Null.And.Not.Empty, key.Key);
                Assert.That(text.IndexOf('\uFFFD'), Is.EqualTo(-1), key.Key);
                Assert.DoesNotThrow(() => string.Format(text, 6000, 6000, 0, "/", "Done"), key.Key);
            }
        }
    }
    [UnityTest] public IEnumerator VisibleAndInactiveLabelsFollowLanguageWithoutReopeningTheMenu()
    {
        var go = new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(root.transform);
        var binding=go.AddComponent<PSH_Script_LocalizedText>();binding.SetKey("Sys_Details");
        var label=go.GetComponent<TMP_Text>();
        Time.timeScale=0;
        foreach (var pair in new[]{new[]{"ko","세부 설정"},new[]{"en","Details"},new[]{"ja","詳細設定"}})
        {
            LocaleTestContext.Select(pair[0]);yield return LocalizationSettings.InitializationOperation;yield return null;
            Assert.That(label.text,Is.EqualTo(pair[1]));
        }
        go.SetActive(false);LocaleTestContext.Select("en");yield return null;go.SetActive(true);
        Assert.That(label.text,Is.EqualTo("Details"));Assert.That(Time.timeScale,Is.Zero);
    }
    [UnityTest] public IEnumerator DropdownSavesSelectionAndSynchronizesAfterReopening()
    {
        var go=new GameObject("Language",typeof(RectTransform));go.transform.SetParent(root.transform);go.SetActive(false);
        var dropdown=go.AddComponent<TMP_Dropdown>();var manager=go.AddComponent<PSH_Script_LanguageManager>();
        typeof(PSH_Script_LanguageManager).GetField("languageDropdown",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(manager,dropdown);
        go.SetActive(true);yield return null;yield return null;
        Assert.That(dropdown.options.Select(o=>o.text),Is.EqualTo(new[]{"한국어","English","日本語"}));
        dropdown.value=2;yield return LocalizationSettings.InitializationOperation;
        Assert.That(PSH_Script_Localization.LanguageCode,Is.EqualTo("ja"));
        Assert.That(PlayerPrefs.GetString("selected-locale"),Is.EqualTo("ja"));
        go.SetActive(false);LocaleTestContext.Select("en");yield return null;go.SetActive(true);yield return null;yield return null;
        Assert.That(dropdown.value,Is.EqualTo(1));
        dropdown.value=0;Assert.That(PlayerPrefs.GetString("selected-locale"),Is.EqualTo("ko"));
    }
    [Test] public void SavedLanguageTakesPriorityOverSystemLanguage()
    {
        PlayerPrefs.SetString("selected-locale","ja");
        var selector=new PlayerPrefLocaleSelector();
        Assert.That(selector.GetStartupLocale(LocalizationSettings.AvailableLocales).Identifier.Code,Is.EqualTo("ja"));
        var configured=AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/GameLocalizationSettings.asset").GetStartupLocaleSelectors();
        Assert.That(configured[1],Is.TypeOf<PlayerPrefLocaleSelector>());
        Assert.That(configured[2],Is.TypeOf<SystemLocaleSelector>());
    }
    [TestCase("Galmuri11-Bold SDF")][TestCase("Galmuri11 SDF_dialogue")]
    public void EveryTranslatedCharacterExistsInTheActualGameFont(string fontName)
    {
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/"+fontName+".asset");
        foreach(var code in new[]{"ko","en","ja"})
        foreach(var name in new[]{"UITable","DialogueTable"})
        {
            var table=Resources.Load<StringTable>("LocalizationData/"+name+"_"+code);
            foreach(var entry in table.Values)
                Assert.That(font.HasCharacters(entry.LocalizedValue,out uint[] missing,true,true),Is.True,
                    entry.Key+" missing "+string.Join(",",missing??new uint[0]));
        }
    }
}
