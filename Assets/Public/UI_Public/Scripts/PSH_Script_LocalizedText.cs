using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PSH_Script_LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;
    [SerializeField] private bool fitText = true;
    private TMP_Text tmp;
    private Text legacy;
    private void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        legacy = GetComponent<Text>();
        if (fitText && tmp != null && !tmp.enableAutoSizing)
        {
            tmp.fontSizeMax = tmp.fontSize;
            tmp.fontSizeMin = tmp.fontSize * .65f;
            tmp.enableAutoSizing = true;
        }
        if (fitText && legacy != null)
        {
            legacy.resizeTextMaxSize = legacy.fontSize;
            legacy.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(legacy.fontSize * .65f));
            legacy.resizeTextForBestFit = true;
        }
    }
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += Refresh;
        Refresh(null);
    }
    private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= Refresh;
    public void SetKey(string value)
    {
        key = value;
        Refresh(null);
    }
    private void Refresh(Locale _)
    {
        if (string.IsNullOrEmpty(key)) return;
        string value = PSH_Script_Localization.Text(key);
        if (tmp != null) tmp.text = value;
        if (legacy != null) legacy.text = value;
    }
}
