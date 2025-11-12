using UnityEngine;
using UnityEngine.UI;

public class CJS_Script_ChoiceSummaryUI : MonoBehaviour
{
    [SerializeField] private Transform grid;                 // Content (LayoutGroup 권장)
    [SerializeField] private CJS_Script_ChoiceSlot slotPrefab;

    void OnEnable()
    {
        Rebuild();   // 패널이 활성화될 때마다 자동 갱신

        var s = CJS_Script_ChoiceState.I;
        if (s != null) s.OnCleared += Rebuild;                
        KHS_Script_ResetController.OnReset += Rebuild;        
    }

    void OnDisable()
    {
        var s = CJS_Script_ChoiceState.I;
        if (s != null) s.OnCleared -= Rebuild;
        KHS_Script_ResetController.OnReset -= Rebuild;
    }

    public void Rebuild()
    {
        if (!grid || !slotPrefab)
        {
            Debug.LogWarning("[SummaryUI] grid/slotPrefab not assigned");
            return;
        }

        for (int i = grid.childCount - 1; i >= 0; --i)
            Destroy(grid.GetChild(i).gameObject);

        var state = CJS_Script_ChoiceState.I ?? FindObjectOfType<CJS_Script_ChoiceState>(true);
        var list = state != null ? state.Picked : null;

        if (list != null)
        {
            foreach (var snap in list)
            {
                var slot = Instantiate(slotPrefab, grid);
                slot.Bind(snap);
            }
        }

        ForceLayoutRefresh();
    }

    private void ForceLayoutRefresh()
    {
        Canvas.ForceUpdateCanvases();
        var rt = grid as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        Canvas.ForceUpdateCanvases();
    }
}
