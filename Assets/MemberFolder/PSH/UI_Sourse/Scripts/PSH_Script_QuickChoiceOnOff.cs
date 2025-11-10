using System.Collections;
using UnityEngine;

public class PSH_Script_QuickChoiceOnOff : MonoBehaviour
{
    private RectTransform rectTransform;

    [Header("Slide")]
    public float duration = 0.5f;
    private float originalX;
    private readonly float hiddenXOffset = -460f;

    [Header("Inventory Hook")]
    [SerializeField] private CJS_Script_ChoiceInventoryUI inventoryUI;
    [SerializeField] private bool rebuildOnEveryOpen = true;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalX = rectTransform.anchoredPosition.x;

        // start hidden
        var pos = rectTransform.anchoredPosition;
        pos.x = originalX + hiddenXOffset;
        rectTransform.anchoredPosition = pos;

        // 처음엔 안 보이는 상태로 통지
        inventoryUI?.SetVisible(false);
    }

    public void OnToggleValueChanged(bool isToggledOn)
    {
        //  가시성 먼저 통지
        inventoryUI?.SetVisible(isToggledOn);

        // 열릴 때 최신 상태로
        if (isToggledOn && rebuildOnEveryOpen)
            inventoryUI?.RebuildAll();

        float targetX = isToggledOn ? originalX : originalX + hiddenXOffset;
        StopAllCoroutines();
        StartCoroutine(MoveUI(targetX));
    }

    private IEnumerator MoveUI(float targetX)
    {
        float t = 0f;
        var start = rectTransform.anchoredPosition;
        var end = new Vector2(targetX, start.y);

        while (t < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(start, end, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = end;
    }
}
