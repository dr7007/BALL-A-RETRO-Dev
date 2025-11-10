using UnityEngine;
using UnityEngine.UI;   // LayoutRebuilder
using System.Linq;

public class CJS_Script_ChoiceSummaryUI : MonoBehaviour
{
    [SerializeField] private Transform grid;                 // Content (LayoutGroup 권장)
    [SerializeField] private CJS_Script_ChoiceSlot slotPrefab;

    void OnEnable()
    {
        Rebuild();   // ★ 패널이 활성화될 때마다 자동 갱신
    }

    /// 외부에서도 호출 가능하도록 공개 메서드
    public void Rebuild()
    {
        if (!grid || !slotPrefab)
        {
            Debug.LogWarning("[SummaryUI] grid/slotPrefab not assigned");
            return;
        }

        // 1) 기존 항목 제거
        for (int i = grid.childCount - 1; i >= 0; --i)
            Destroy(grid.GetChild(i).gameObject);

        // 2) 상태 가져오기(싱글톤이 null이면 씬에서 찾아봄)
        var state = CJS_Script_ChoiceState.I ?? FindObjectOfType<CJS_Script_ChoiceState>(true);
        var list = state != null ? state.Picked : null;

        if (list == null)
        {
            Debug.LogWarning("[SummaryUI] ChoiceState or Picked list is null");
            ForceLayoutRefresh();
            return;
        }

        // 3) 슬롯 생성
        foreach (var snap in list)
        {
            var slot = Instantiate(slotPrefab, grid);
            slot.Bind(snap);
        }

        // 4) 레이아웃/캔버스 강제 갱신(닫았다 열어야 보이는 현상 방지)
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
