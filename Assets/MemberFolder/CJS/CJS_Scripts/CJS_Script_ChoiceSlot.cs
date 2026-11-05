using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class CJS_Script_ChoiceSlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image imgIcon;
    [SerializeField] private TMP_Text txtName;
    [SerializeField] private TMP_Text txtRarity;

    [Header("Tooltip (optional)")]
    [SerializeField] private CJS_Script_TooltipTarget tooltipTarget; // 없으면 자동 탐색/추가

    private CJS_ChoiceSnapshot snapshot;
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += RefreshText;
        RefreshText(null);
    }
    private void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= RefreshText;
    private void RefreshText(Locale _)
    {
        if (snapshot == null) return;
        if (txtName) txtName.text = CJS_ChoiceData.Localized(snapshot.localizationKey, "Name", snapshot.name);
        if (txtRarity) txtRarity.text = CJS_ChoiceData.LocalizedRarity(snapshot.rarity);
        tooltipTarget?.Set("", CJS_ChoiceData.Localized(snapshot.localizationKey, "Description", snapshot.description));
    }

    void Reset() { AutoWire(); Clear(); }
    void Awake() { AutoWire(); Clear(); }

    private void AutoWire()
    {
        // 아이콘 이미지 자동 탐색
        if (!imgIcon)
        {
            var t = transform.Find("Icon");
            if (t) imgIcon = t.GetComponent<Image>();
            if (!imgIcon) imgIcon = GetComponentInChildren<Image>(true);
        }
        if (imgIcon) imgIcon.preserveAspect = true;

        // 툴팁 타깃 자동 탐색(없으면 아이콘 오브젝트에 부착)
        if (!tooltipTarget)
            tooltipTarget = GetComponentInChildren<CJS_Script_TooltipTarget>(true);
        if (!tooltipTarget && imgIcon)
            tooltipTarget = imgIcon.gameObject.AddComponent<CJS_Script_TooltipTarget>();
    }

    public void Bind(CJS_ChoiceSnapshot s)
    {
        if (s == null) { Clear(); return; }
        snapshot = s;

        if (imgIcon)
        {
            imgIcon.sprite = s.icon;
            imgIcon.enabled = (s.icon != null);
            imgIcon.color = Color.white;
            imgIcon.material = null;    
            imgIcon.maskable = true;
        }
        RefreshText(null);

   


        Debug.Log($"[ChoiceSlot] Bind icon={(s.icon ? s.icon.name : "null")}");
    }

    public void Bind(CJS_ChoiceData d)
    {
        if (d == null) { Clear(); return; }
        snapshot = CJS_ChoiceSnapshot.From(d);

        if (imgIcon)
        {
            imgIcon.sprite = d.icon;
            imgIcon.enabled = (d.icon != null);
            imgIcon.color = Color.white;
            imgIcon.material = null;
            imgIcon.maskable = true;
        }
        RefreshText(null);




        Debug.Log($"[ChoiceSlot] Bind(Data) icon={(d.icon ? d.icon.name : "null")}");
    }

    public void Clear()
    {
        snapshot = null;
        if (imgIcon) { imgIcon.sprite = null; imgIcon.enabled = false; }
        if (txtName) txtName.text = "";
        if (txtRarity) txtRarity.text = "";
        tooltipTarget?.Set("", "");
    }
}
