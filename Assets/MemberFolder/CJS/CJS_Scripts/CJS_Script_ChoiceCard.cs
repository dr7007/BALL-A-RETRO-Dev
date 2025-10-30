using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        if (btn != null) btn.onClick.AddListener(InvokePick);
    }

    void OnDisable()
    {
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

        if (txtName != null) txtName.text = d != null ? d.name : "";
        if (txtRarity != null) txtRarity.text = d != null ? d.rarity : "";
        if (txtDesc != null) txtDesc.text = d != null ? d.description : "";
        if (imgIcon != null) imgIcon.sprite = d != null ? d.icon : null;

        if (txtChance != null)
        {
            if (chancePercent.HasValue)
                txtChance.text = $"{chancePercent.Value:0.0}%";
            else
                txtChance.text = "";
        }
    }

    private void InvokePick()
    {
        if (data == null) return;
        onPick?.Invoke(data);
    }
}
