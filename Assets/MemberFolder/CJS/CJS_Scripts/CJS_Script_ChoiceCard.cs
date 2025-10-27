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

    [Header("Root Button")]
    [SerializeField] private Button btn; // 카드 Root에 붙은 Button

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
        // 인스펙터에서 버튼 연결을 깜빡해도 자동으로 루트에서 찾음
        if (btn == null) btn = GetComponent<Button>();
    }

    public void Bind(CJS_ChoiceData d, Action<CJS_ChoiceData> onPickCb)
    {
        data = d;
        onPick = onPickCb;

        if (txtName != null) txtName.text = d != null ? d.name : "";
        if (txtRarity != null) txtRarity.text = d != null ? d.rarity : "";
        if (txtDesc != null) txtDesc.text = d != null ? d.description : "";
        if (imgIcon != null) imgIcon.sprite = d != null ? d.icon : null;
    }

    private void InvokePick()
    {
        if (data == null) return;
        onPick?.Invoke(data);
    }
}
