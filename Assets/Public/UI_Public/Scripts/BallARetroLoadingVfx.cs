using UnityEngine;
using UnityEngine.UI;

// Original vector particles: no downloaded textures, materials or effect prefabs.
[DisallowMultipleComponent]
public sealed class BallARetroLoadingVfx : MaskableGraphic
{
    float phase;
    bool positionDirty = true;
    bool updatingPosition;
    Vector2 lastParentSize;
    Vector2Int lastScreenSize;
    static readonly Color Amber = new Color32(255, 180, 58, 255);
    static readonly Color Cyan = new Color32(76, 226, 235, 255);

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    void Update()
    {
        if (!PSH_Script_EffectSettings.ParticlesEnabled) return;
        phase += Time.unscaledDeltaTime;
        SetVerticesDirty();
    }

    void LateUpdate()
    {
        var parentRect = rectTransform.parent as RectTransform;
        if (parentRect != null && (positionDirty || parentRect.rect.size != lastParentSize
            || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y))
            PositionBelowLobbyButtons();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        if (!updatingPosition) positionDirty = true;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        phase = 0;
        positionDirty = true;
        PSH_Script_EffectSettings.Changed += EffectsChanged;
        PositionBelowLobbyButtons();
    }

    protected override void OnDisable()
    {
        PSH_Script_EffectSettings.Changed -= EffectsChanged;
        base.OnDisable();
    }

    void EffectsChanged(bool _) => SetVerticesDirty();

    void PositionBelowLobbyButtons()
    {
        if (updatingPosition) return;
        var lobby = FindFirstObjectByType<PSH.PSH_Script_LobbyUIManager>();
        var parentRect = rectTransform.parent as RectTransform;
        var overlayCanvas = canvas;
        // A duplicate persistent loader can enable its children while its parent
        // is being destroyed on a return to the lobby. Its canvas is then absent.
        if (lobby == null || parentRect == null || overlayCanvas == null) return;

        // The menu uses a camera canvas; the loading overlay uses screen space.
        // Measure the actual row so the indicator stays below it at every resolution.
        updatingPosition = true;
        try
        {
            Canvas.ForceUpdateCanvases();
            if (!this || !isActiveAndEnabled || lobby == null || parentRect == null || overlayCanvas == null) return;

            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            float bottom = float.PositiveInfinity;
            var corners = new Vector3[4];
            RectTransform row = null;
            Camera rowCamera = null;
            foreach (var button in lobby.GetComponentsInChildren<PSH_Script_StartButton>())
            {
                if (button == null) continue;
                var buttonRect = button.transform as RectTransform;
                var buttonCanvas = button.GetComponentInParent<Canvas>();
                if (buttonRect == null || buttonCanvas == null) continue;
                var camera = buttonCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : buttonCanvas.worldCamera;
                if (row == null)
                {
                    row = buttonRect.parent as RectTransform;
                    rowCamera = camera;
                }
                buttonRect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var point = RectTransformUtility.WorldToScreenPoint(camera, corner);
                    left = Mathf.Min(left, point.x);
                    right = Mathf.Max(right, point.x);
                    bottom = Mathf.Min(bottom, point.y);
                }
            }
            if (float.IsInfinity(bottom)) return;

            // Hover animates one button's scale; the row center remains stable.
            float centerX = row != null
                ? RectTransformUtility.WorldToScreenPoint(rowCamera, row.TransformPoint(row.rect.center)).x
                : (left + right) * .5f;
            var overlayCamera = overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : overlayCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect,
                new Vector2(centerX, bottom), overlayCamera, out var rowBottom))
            {
                rectTransform.localPosition = new Vector3(rowBottom.x,
                    rowBottom.y - 24f - rectTransform.rect.height * .5f, 0);
            }
            lastParentSize = parentRect.rect.size;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            positionDirty = false;
        }
        finally
        {
            updatingPosition = false;
        }
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var center = rectTransform.rect.center;
        float scale = Mathf.Min(rectTransform.rect.width / 640f, rectTransform.rect.height / 360f);
        if (scale <= 0) return;
        // A static pinball and orbit remain readable when particles are disabled.
        float time = PSH_Script_EffectSettings.ParticlesEnabled ? phase : 0;
        for (int ring = 0; ring < 2; ring++)
        {
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2 / 32 + time * (ring == 0 ? .75f : -.45f);
                float radius = ring == 0 ? 66 : 94;
                var point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var tint = ring == 0 ? Amber : Cyan;
                tint.a = i % 4 == 0 ? .95f : .35f;
                Quad(mesh, center + point * scale, (i % 4 == 0 ? 5 : 3) * scale, tint);
            }
        }
        // Sparse converging motes complement the static pinball indicator.
        if (PSH_Script_EffectSettings.ParticlesEnabled)
            for (int i = 0; i < 28; i++)
            {
                float progress = Mathf.Repeat(time * .35f + i * .61803399f, 1f);
                float angle = i * 2.399963f + time * .14f;
                float radius = Mathf.Lerp(158, 104, progress);
                var tint = i % 2 == 0 ? Amber : Cyan;
                tint.a = Mathf.Sin(progress * Mathf.PI) * .6f;
                Quad(mesh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * scale, 2.5f * scale, tint);
            }
        Quad(mesh, center, 15 * scale, Amber);
        Quad(mesh, center + new Vector2(-4, 4) * scale, 5 * scale, Color.white);
    }

    static void Quad(VertexHelper mesh, Vector2 center, float halfSize, Color tint)
    {
        int first = mesh.currentVertCount;
        mesh.AddVert(center + new Vector2(-halfSize, -halfSize), tint, Vector2.zero);
        mesh.AddVert(center + new Vector2(-halfSize, halfSize), tint, Vector2.zero);
        mesh.AddVert(center + new Vector2(halfSize, halfSize), tint, Vector2.zero);
        mesh.AddVert(center + new Vector2(halfSize, -halfSize), tint, Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
        mesh.AddTriangle(first, first + 2, first + 3);
    }
}
