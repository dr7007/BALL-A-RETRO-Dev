using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class CJS_Script_ChoiceCard : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text txtName;
    [SerializeField] private TMP_Text txtRarity;
    [SerializeField] private TMP_Text txtDesc;
    [SerializeField] private Image imgIcon;
    [SerializeField] private TMP_Text txtChance; 

    [Header("Root Button")]
    [SerializeField] private Button btn;

    private CJS_ChoiceData data;
    private Action<CJS_ChoiceData> onPick;

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += RefreshText;
        RefreshText(null);
        if (btn != null) btn.onClick.AddListener(InvokePick);
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= RefreshText;
        if (btn != null) btn.onClick.RemoveListener(InvokePick);
    }

    void OnValidate()
    {
        if (btn == null) btn = GetComponent<Button>();
    }

    public void Bind(CJS_ChoiceData d, Action<CJS_ChoiceData> onPickCb)
    {
        BindWithChance(d, onPickCb, null);
    }

    public void BindWithChance(CJS_ChoiceData d, Action<CJS_ChoiceData> onPickCb, float? chancePercent)
    {
        data = d;
        onPick = onPickCb;

        RefreshText(null);
        if (imgIcon != null) imgIcon.sprite = d != null ? d.icon : null;

        if (txtChance != null)
        {
            if (chancePercent.HasValue)
                txtChance.text = $"{chancePercent.Value:0.0}%";
            else
                txtChance.text = "";
        }
    }

    private void RefreshText(Locale _)
    {
        if (txtName) txtName.text = data != null ? CJS_ChoiceData.Localized(data.localizationKey, "Name", data.name) : "";
        if (txtDesc) txtDesc.text = data != null ? CJS_ChoiceData.Localized(data.localizationKey, "Description", data.description) : "";
        if (txtRarity) txtRarity.text = data != null ? CJS_ChoiceData.LocalizedRarity(data.rarity) : "";
    }

    private void InvokePick()
    {
        if (data == null) return;
        onPick?.Invoke(data);
    }
}
