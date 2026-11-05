using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Project after the camera controllers have finished following the ball.
[DefaultExecutionOrder(100)]
public class CJS_Script_ScorePopupSpawner : MonoBehaviour
{
    [Header("Bindings")]
    public RectTransform canvasRoot;
    public RectTransform spawnParent;
    public TextMeshProUGUI popupPrefab;
    public Camera worldCamera;

    [Header("Position")]
    [Tooltip("Pixel offset from the hit position on screen.")]
    public Vector2 screenOffset = Vector2.zero;

    [Header("Anim")]
    public float riseDistance = 60f;
    public float duration = 0.6f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve alphaCurve = new AnimationCurve(
        new Keyframe(0, 1), new Keyframe(0.7f, 1), new Keyframe(1, 0));

    [Header("Style")]
    public string positiveFormat = "+{0}";

    private sealed class Popup
    {
        public TextMeshProUGUI text;
        public CanvasGroup group;
        public Vector3 origin;
        public bool worldSpace;
        public float age;
    }

    private readonly List<Popup> popups = new List<Popup>();

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        KHS_Script_ScoreManager.OnScoreGainedAt += HandleScoreAt;
        KHS_Script_CameraManager.CameraChangeEvt += ChangeCamera;
        KHS_Script_ResetController.OnReset += ClearPopups;
    }

    private void OnDisable()
    {
        KHS_Script_ScoreManager.OnScoreGainedAt -= HandleScoreAt;
        KHS_Script_CameraManager.CameraChangeEvt -= ChangeCamera;
        KHS_Script_ResetController.OnReset -= ClearPopups;
        ClearPopups();
    }

    private void ChangeCamera(Camera camera)
    {
        if (worldCamera == camera) return;
        worldCamera = camera;
        ClearPopups();
    }

    private void HandleScoreAt(int delta, Vector3 position)
    {
        if (delta <= 0) return;
        Spawn(position, true, string.Format(positiveFormat, delta));
    }

    // Keep the existing explicit screen-position API for other callers.
    public void Show(Vector2 screenPosition, string text) => Spawn(screenPosition, false, text);

    private void Spawn(Vector3 origin, bool worldSpace, string text)
    {
        var parent = spawnParent != null ? spawnParent : canvasRoot;
        if (popupPrefab == null || parent == null || duration <= 0f) return;
        var popup = new Popup { origin = origin, worldSpace = worldSpace };
        if (!TryGetLocalPosition(popup, parent, out var local)) return;

        popup.text = Instantiate(popupPrefab, parent);
        popup.text.text = text;
        popup.text.alignment = TextAlignmentOptions.Center;
        popup.text.raycastTarget = false;
        popup.group = popup.text.GetComponent<CanvasGroup>();
        if (popup.group == null) popup.group = popup.text.gameObject.AddComponent<CanvasGroup>();
        popup.group.blocksRaycasts = false;
        popup.group.interactable = false;
        popup.group.alpha = 1f;
        popup.text.rectTransform.localPosition = new Vector3(local.x, local.y, 0f);
        popup.text.rectTransform.localScale = Vector3.one;
        popups.Add(popup);
    }

    private bool TryGetLocalPosition(Popup popup, RectTransform parent, out Vector2 local)
    {
        local = default;
        Vector2 screen = popup.origin;
        if (popup.worldSpace)
        {
            var camera = worldCamera != null && worldCamera.isActiveAndEnabled ? worldCamera : Camera.main;
            if (camera == null || !camera.isActiveAndEnabled) return false;
            Vector3 viewport = camera.WorldToViewportPoint(popup.origin);
            if (viewport.z < camera.nearClipPlane || viewport.z > camera.farClipPlane
                || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return false;
            screen = (Vector2)camera.WorldToScreenPoint(popup.origin) + screenOffset;
        }
        var canvas = parent.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        Camera uiCamera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out local);
    }

    private void LateUpdate()
    {
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            Popup popup = popups[i];
            popup.age += Time.unscaledDeltaTime;
            if (popup.text == null || popup.age >= duration)
            {
                if (popup.text != null) Destroy(popup.text.gameObject);
                popups.RemoveAt(i);
                continue;
            }
            var rect = popup.text.rectTransform;
            if (!TryGetLocalPosition(popup, (RectTransform)rect.parent, out var local))
            {
                popup.group.alpha = 0f;
                continue;
            }
            float n = Mathf.Clamp01(popup.age / duration);
            float move = moveCurve.Evaluate(n);
            rect.localPosition = new Vector3(local.x, local.y + riseDistance * move, 0f);
            float scale = 1f + 0.15f * move;
            rect.localScale = new Vector3(scale, scale, 1f);
            popup.group.alpha = alphaCurve.Evaluate(n);
        }
    }

    private void ClearPopups()
    {
        foreach (Popup popup in popups)
            if (popup.text != null) Destroy(popup.text.gameObject);
        popups.Clear();
    }
}
