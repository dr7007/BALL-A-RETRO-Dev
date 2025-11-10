using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CJS_Script_TooltipUI : MonoBehaviour
{
    public static CJS_Script_TooltipUI I { get; private set; }

    [Header("Refs")]
    [SerializeField] private Canvas rootCanvas;          // 최상위 캔버스
    [SerializeField] private GameObject panel;           // 툴팁 패널(비활성 시작)
    [SerializeField] private RectTransform panelRT;
    [SerializeField] private TMP_Text txtTitle;          // 사용 안함(숨김)
    [SerializeField] private TMP_Text txtBody;

    [Header("Behavior")]
    [SerializeField] private Vector2 screenOffset = new Vector2(24, -24);
    [SerializeField] private bool followMouse = true;

    private Camera uiCam;
    private CanvasGroup cg;
    private Coroutine hideCo;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>(true);
        if (!panelRT) panelRT = panel ? panel.GetComponent<RectTransform>() : null;
        if (rootCanvas && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = rootCanvas.worldCamera;

        // 툴팁이 레이캐스트를 절대 가로채지 않게
        if (panel)
        {
            cg = panel.GetComponent<CanvasGroup>();
            if (!cg) cg = panel.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;

            foreach (var g in panel.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
        }

        HideImmediate();
    }

    public void Show(string title, string body, Vector2 screenPos, bool follow = true)
    {
        if (hideCo != null) { StopCoroutine(hideCo); hideCo = null; }

        if (txtTitle) txtTitle.gameObject.SetActive(false);

        if (txtBody) txtBody.text = string.IsNullOrEmpty(body) ? "" : body;

        followMouse = follow;
        if (panel && !panel.activeSelf) panel.SetActive(true);

        UpdatePosition(screenPos);
    }

    // 약간의 지연 후 숨김(깜빡임 방지)
    public void Hide()
    {
        if (hideCo != null) StopCoroutine(hideCo);
        hideCo = StartCoroutine(HideAfter(0.08f));
    }

    private IEnumerator HideAfter(float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        HideImmediate();
        hideCo = null;
    }

    private void HideImmediate()
    {
        if (panel) panel.SetActive(false);
    }

    public void UpdatePosition(Vector2 screenPos)
    {
        if (!panel || !panel.activeSelf || !panelRT || !rootCanvas) return;

        var rootRT = rootCanvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootRT, screenPos + screenOffset, uiCam, out var local);

        // 경계 넘김 방지 pivot 보정
        Vector2 size = panelRT.rect.size;
        Vector2 half = rootRT.rect.size * 0.5f;
        Vector2 desired = local;
        Vector2 pivot = panelRT.pivot;
        bool changed = false;

        float left = desired.x - size.x * pivot.x;
        float right = desired.x + size.x * (1f - pivot.x);
        float bottom = desired.y - size.y * pivot.y;
        float top = desired.y + size.y * (1f - pivot.y);

        if (right > half.x) { pivot.x = 1f; changed = true; }
        if (left < -half.x) { pivot.x = 0f; changed = true; }
        if (top > half.y) { pivot.y = 1f; changed = true; }
        if (bottom < -half.y) { pivot.y = 0f; changed = true; }
        if (changed) panelRT.pivot = pivot;

        panelRT.anchoredPosition = desired;
    }

    void Update()
    {
        if (panel && panel.activeSelf && followMouse)
            UpdatePosition(Input.mousePosition);
    }
}
