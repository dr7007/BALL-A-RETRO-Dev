using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CJS_Script_ChoiceSlot : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image imgIcon;
    [SerializeField] private TMP_Text txtName;
    [SerializeField] private TMP_Text txtRarity;

    [Header("Tooltip (optional)")]
    [SerializeField] private CJS_Script_TooltipTarget tooltipTarget; // 없으면 자동 탐색/추가

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

        if (imgIcon)
        {
            imgIcon.sprite = s.icon;
            imgIcon.enabled = (s.icon != null);
            imgIcon.color = Color.white;
            imgIcon.material = null;    
            imgIcon.maskable = true;
        }
        if (txtName) txtName.text = s.name ?? "";
        if (txtRarity) txtRarity.text = s.rarity ?? "";

   
        tooltipTarget?.Set("", s.description);

        Debug.Log($"[ChoiceSlot] Bind icon={(s.icon ? s.icon.name : "null")}");
    }

    public void Bind(CJS_ChoiceData d)
    {
        if (d == null) { Clear(); return; }

        if (imgIcon)
        {
            imgIcon.sprite = d.icon;
            imgIcon.enabled = (d.icon != null);
            imgIcon.color = Color.white;
            imgIcon.material = null;
            imgIcon.maskable = true;
        }
        if (txtName) txtName.text = d.name ?? "";
        if (txtRarity) txtRarity.text = d.rarity ?? "";


        tooltipTarget?.Set("", d.description);

        Debug.Log($"[ChoiceSlot] Bind(Data) icon={(d.icon ? d.icon.name : "null")}");
    }

    public void Clear()
    {
        if (imgIcon) { imgIcon.sprite = null; imgIcon.enabled = false; }
        if (txtName) txtName.text = "";
        if (txtRarity) txtRarity.text = "";
        tooltipTarget?.Set("", "");
    }
}
