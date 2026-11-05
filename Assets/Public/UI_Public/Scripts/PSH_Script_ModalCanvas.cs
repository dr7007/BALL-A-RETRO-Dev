using UnityEngine;
using UnityEngine.UI;

/// <summary>시스템창/확인창이 활성화될 때 표시 순서와 UI 클릭 처리를 함께 적용한다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
public class PSH_Script_ModalCanvas : MonoBehaviour
{
    [SerializeField] private int sortingOrder = 100;

    public void SetSortingOrder(int order)
    {
        sortingOrder = order;
        if (isActiveAndEnabled) ApplySorting();
    }

    private void OnEnable()
    {
        // 비활성 오브젝트에 추가한 Canvas의 초기화가 끝난 뒤 순서를 적용한다.
        ApplySorting();
    }

    private void ApplySorting()
    {
        var canvas = GetComponent<Canvas>();
        var parentCanvas = transform.parent != null
            ? transform.parent.GetComponentInParent<Canvas>(true)
            : null;
        canvas.enabled = true;
        canvas.sortingLayerID = parentCanvas != null ? parentCanvas.sortingLayerID : 0;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        GetComponent<GraphicRaycaster>().enabled = true;
    }
}
