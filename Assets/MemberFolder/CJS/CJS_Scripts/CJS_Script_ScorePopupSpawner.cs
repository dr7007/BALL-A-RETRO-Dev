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

    [Header("Anchor To Ball (World-Side Offset)")]
    [Tooltip("OnScoreGainedAt(worldPos)를 공 옆에 붙여 찍을지")]
    public bool anchorToWorld = true;

    [Tooltip("카메라의 오른쪽 방향으로 월드 오프셋(미터)")]
    public float worldOffsetRight = 0.15f;

    [Tooltip("카메라의 위쪽 방향으로 월드 오프셋(미터)")]
    public float worldOffsetUp = 0.15f;

    [Tooltip("화면 밖으로 나가지 않게 패딩 안쪽으로 클램핑")]
    public bool clampToScreen = true;

    [Tooltip("클램핑 시 화면 가장자리로부터 여유 픽셀")]
    public Vector2 clampPadding = new Vector2(24, 24);

    [Header("Fallback (No Position Event)")]
    [Tooltip("OnScoreGained(위치 없음) 수신 시 사용할 스크린 오프셋(픽셀)")]
    public Vector2 screenOffset = new Vector2(0, 80);

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
        if (worldCamera == null) worldCamera = Camera.main;
    }

    void OnEnable()
    {
        KHS_Script_ScoreManager.OnScoreGainedAt += HandleScoreAt;
        KHS_Script_ScoreManager.OnScoreGained += HandleScoreNoPos;
    }

    void OnDisable()
    {
        KHS_Script_ScoreManager.OnScoreGainedAt -= HandleScoreAt;
        KHS_Script_ScoreManager.OnScoreGained -= HandleScoreNoPos;
    }

    private void HandleScoreAt(int delta, Vector3 worldPos)
    {
        if (delta <= 0 || popupPrefab == null || canvasRoot == null) return;
        if (worldCamera == null) worldCamera = Camera.main;

        // 1) 공(또는 득점 지점) 옆으로 월드 오프셋
        Vector3 pos = worldPos;
        if (anchorToWorld && worldCamera != null)
        {
            var cam = worldCamera.transform;
            pos += cam.right * worldOffsetRight + cam.up * worldOffsetUp;
        }

        // 2) 스크린 좌표 변환(+옵션 클램프)
        Vector3 scr = worldCamera.WorldToScreenPoint(pos);
        if (scr.z < 0f) // 카메라 뒤에 있으면 중앙으로 폴백
        {
            scr = new Vector3(Screen.width * 0.5f, Screen.height * 0.6f, 0f);
        }
        Vector2 screenPos = new Vector2(scr.x, scr.y);
        if (clampToScreen)
        {
            float minX = clampPadding.x;
            float maxX = Screen.width - clampPadding.x;
            float minY = clampPadding.y;
            float maxY = Screen.height - clampPadding.y;
            screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
            screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);
        }

        Show(screenPos, string.Format(positiveFormat, delta));
    }

    private void HandleScoreNoPos(int delta)
    {
        if (delta <= 0 || popupPrefab == null || canvasRoot == null) return;
        // 위치 정보가 없으면 화면 중앙 약간 위
        Vector2 screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.6f) + screenOffset;
        Show(screenPos, string.Format(positiveFormat, delta));
    }

    public void Show(Vector2 screenPosition, string text)
    {
        var ui = Instantiate(popupPrefab, spawnParent != null ? spawnParent : canvasRoot);
        ui.text = text;

        var cg = ui.GetComponent<CanvasGroup>();
        if (cg == null) cg = ui.gameObject.AddComponent<CanvasGroup>();

        // 스크린 → 캔버스 로컬 변환
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
