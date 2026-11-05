using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization.Settings;

[SetUpFixture]
public class DefaultLocaleFixture
{
    private LocalizationSettings settings;
    [OneTimeSetUp] public void Initialize()
    {
        settings = LocaleTestContext.Create();
        LocalizationSettings.InitializationOperation.WaitForCompletion();
    }
    [OneTimeTearDown] public void Cleanup()
    {
        Object.DestroyImmediate(settings);
        LocalizationSettings.Instance = null;
    }
}
