using System.Collections;
using UnityEngine;
using TMPro;

public class CJS_Script_ScorePopupSpawner : MonoBehaviour
{
    [Header("Bindings")]
    public RectTransform canvasRoot;            // Overlay Canvas의 RectTransform
    public RectTransform spawnParent;           // 보통 Canvas 내 빈 RectTransform(비워두면 canvasRoot)
    public TextMeshProUGUI popupPrefab;         // TMP + (권장) CanvasGroup 포함
    public Camera worldCamera;                  // 월드→스크린 변환용 (없으면 Camera.main)

    [Header("Fallback (No Position Event)")]
    [Tooltip("OnScoreGained(위치 없음) 수신 시 사용할 스크린 오프셋(픽셀)")]
    public Vector2 screenOffset = new Vector2(0, 0); // 위치 고정을 위해 기본값을 (0, 0)으로 설정 권장

    [Header("Anim")]
    public float riseDistance = 60f;
    public float duration = 0.6f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve alphaCurve = new AnimationCurve(
        new Keyframe(0, 1), new Keyframe(0.7f, 1), new Keyframe(1, 0));

    [Header("Style")]
    public string positiveFormat = "+{0}";

    void Awake()
    {
        // 월드 카메라 관련 설정은 더 이상 사용되지 않으므로 제거하거나 그대로 둘 수 있지만,
        // 현재는 나머지 기능에 영향을 미치지 않으므로 유지
        if (worldCamera == null) worldCamera = Camera.main;
    }

    void OnEnable()
    {
        //  HandleScoreAt 관련 구독 제거
        KHS_Script_ScoreManager.OnScoreGained += HandleScoreNoPos;
    }

    void OnDisable()
    {
        // HandleScoreAt 관련 해지 제거
        KHS_Script_ScoreManager.OnScoreGained -= HandleScoreNoPos;
    }

    //  HandleScoreAt 함수는 완전히 제거되었습니다.

    private void HandleScoreNoPos(int delta)
    {
        if (delta <= 0 || popupPrefab == null || canvasRoot == null) return;

        // 모든 점수를 오른쪽 상단 (화면 너비의 80%, 높이의 85% 지점) 위치로 지정합니다.
        // screenOffset은 인스펙터에서 0으로 설정해야 합니다.
        Vector2 screenPos = new Vector2(Screen.width * 0.8f, Screen.height * 0.85f) + screenOffset;

        Show(screenPos, string.Format(positiveFormat, delta));
    }

    public void Show(Vector2 screenPosition, string text)
    {
        var ui = Instantiate(popupPrefab, spawnParent != null ? spawnParent : canvasRoot);
        ui.text = text;

        var cg = ui.GetComponent<CanvasGroup>();
        if (cg == null) cg = ui.gameObject.AddComponent<CanvasGroup>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot, screenPosition, null, out var localPos);
        var rt = ui.rectTransform;
        rt.anchoredPosition = localPos;

        StartCoroutine(CoRun(ui, cg, rt));
    }

    private IEnumerator CoRun(TextMeshProUGUI ui, CanvasGroup cg, RectTransform rt)
    {
        float t = 0f;
        Vector2 start = rt.anchoredPosition;
        Vector2 end = start + new Vector2(0, riseDistance);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // 연출은 실시간 기준
            float n = Mathf.Clamp01(t / duration);
            float mk = moveCurve.Evaluate(n);
            float ak = alphaCurve.Evaluate(n);

            rt.anchoredPosition = Vector2.LerpUnclamped(start, end, mk);
            cg.alpha = ak;

            // 살짝 스케일 업
            float scale = 1f + 0.15f * mk;
            rt.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        Destroy(ui.gameObject);
    }

}