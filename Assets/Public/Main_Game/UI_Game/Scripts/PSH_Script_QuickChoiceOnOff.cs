using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A read-only drawer: opening it never owns pause, camera or cursor state.
public class PSH_Script_QuickChoiceOnOff : MonoBehaviour
{
    [Header("Slide")]
    public float duration = 0.5f;
    [Header("Inventory Hook")]
    [SerializeField] private CJS_Script_ChoiceInventoryUI inventoryUI;
    [Header("Arrow Settings")]
    [SerializeField] private RectTransform arrowRectTransform;
    [SerializeField] private float arrowRotationOn = 180f;
    [SerializeField] private float arrowRotationOff;
    [SerializeField] private Toggle controllingToggle;

    private const float HiddenXOffset = -460f;
    private RectTransform rectTransform;
    private float originalX;
    private Coroutine moveCoroutine;
    private GameObject hint;
    private bool gameEnded;
    private bool monitorActive;
    private int blockedFrame = -1;
    public bool IsOpen { get; private set; }
    public event Action<bool> OpenStateChanged;
    public RectTransform StatusHost => transform.Find("Image_QuickChoice_Bg") as RectTransform ?? rectTransform;
    private bool IsTutorialScene => gameObject.scene.name.IndexOf("Tutorial", StringComparison.OrdinalIgnoreCase) >= 0;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        originalX = rectTransform.anchoredPosition.x;
        if (!inventoryUI) inventoryUI = GetComponentInChildren<CJS_Script_ChoiceInventoryUI>(true);
        if (!controllingToggle) controllingToggle = GetComponentInChildren<Toggle>(true);
        if (controllingToggle) controllingToggle.SetIsOnWithoutNotify(false);
        SetPosition(false);
        inventoryUI?.SetVisible(false);
        CreateHint();
    }

    private void OnEnable()
    {
        KHS_Script_ScoreManager.OnGameOver += OnGameEnded;
        KHS_Script_ScoreManager.OnGameClear += OnGameEnded;
        KHS_Script_ResetController.OnReset += OnReset;
        KHS_Script_CameraManager.MonitorEvt += OnMonitor;
        SceneManager.activeSceneChanged += OnSceneChanged;
        UpdateHintVisibility();
    }

    private void OnDisable()
    {
        KHS_Script_ScoreManager.OnGameOver -= OnGameEnded;
        KHS_Script_ScoreManager.OnGameClear -= OnGameEnded;
        KHS_Script_ResetController.OnReset -= OnReset;
        KHS_Script_CameraManager.MonitorEvt -= OnMonitor;
        SceneManager.activeSceneChanged -= OnSceneChanged;
        CloseImmediately();
        if (hint) hint.SetActive(false);
    }

    private void OnDestroy() { if (hint) Destroy(hint); }
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;
        TryHandleTabInput(true,
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
            Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
    }
    public bool TryHandleTabInput(bool pressed, bool shift, bool control, bool alt)
        => pressed && !shift && !control && !alt && TryTogglePanel();
    private void LateUpdate()
    {
        UpdateHintVisibility();
        if (IsOpen && !CanOpen()) CloseImmediately();
    }
    private void UpdateHintVisibility()
    {
        if (!hint) return;
        bool visible = isActiveAndEnabled && !IsTutorialScene && !(KHS_Script_ScoreManager.Instance?.IsTutorialRun ?? false);
        if (hint.activeSelf != visible) hint.SetActive(visible);
    }
    private void OnGameEnded() { gameEnded = true; CloseImmediately(); }
    private void OnReset() { gameEnded = false; monitorActive = false; CloseImmediately(); }
    private void OnMonitor(bool active) { monitorActive = active; if (active) CloseImmediately(); }
    private void OnSceneChanged(Scene previous, Scene next) => CloseImmediately();

    private bool CanOpen()
    {
        var score = KHS_Script_ScoreManager.Instance;
        if (!isActiveAndEnabled || gameEnded || monitorActive || blockedFrame == Time.frameCount || Time.timeScale <= 0f
            || PSH_Script_SettingController.BlocksDialogueKeyboardInput
            || IsTutorialScene || score == null || score.HasEnded || score.IsTutorialRun || !score.IsPlaying) return false;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected == null || (selected.GetComponent<TMP_InputField>() == null && selected.GetComponent<InputField>() == null);
    }

    public bool TryTogglePanel()
    {
        if (IsOpen) { SetOpen(false, false); return true; }
        if (!CanOpen()) return false;
        SetOpen(true, false);
        return true;
    }

    // Existing serialized Toggle callbacks follow exactly the same guard as Tab.
    public void OnToggleValueChanged(bool open)
    {
        if (open && !CanOpen()) { if (controllingToggle) controllingToggle.SetIsOnWithoutNotify(false); return; }
        SetOpen(open, false);
    }

    public void CloseImmediately()
    {
        blockedFrame = Time.frameCount;
        SetOpen(false, true);
    }

    private void SetOpen(bool open, bool immediate)
    {
        bool changed = IsOpen != open;
        IsOpen = open;
        if (controllingToggle) controllingToggle.SetIsOnWithoutNotify(open);
        if (!inventoryUI) inventoryUI = GetComponentInChildren<CJS_Script_ChoiceInventoryUI>(true);
        inventoryUI?.SetVisible(open);
        if (moveCoroutine != null) { StopCoroutine(moveCoroutine); moveCoroutine = null; }
        if (rectTransform != null)
        {
            if (immediate || duration <= 0f || !isActiveAndEnabled) SetPosition(open);
            else moveCoroutine = StartCoroutine(MoveUI(open));
        }
        if (changed) OpenStateChanged?.Invoke(open);
    }

    private void SetPosition(bool open)
    {
        if (rectTransform == null) return;
        var position = rectTransform.anchoredPosition;
        position.x = originalX + (open ? 0f : HiddenXOffset);
        rectTransform.anchoredPosition = position;
        if (arrowRectTransform) arrowRectTransform.localRotation = Quaternion.Euler(0, 0, open ? arrowRotationOn : arrowRotationOff);
    }

    private IEnumerator MoveUI(bool open)
    {
        var start = rectTransform.anchoredPosition;
        var end = new Vector2(originalX + (open ? 0f : HiddenXOffset), start.y);
        var startRotation = arrowRectTransform ? arrowRectTransform.localRotation : Quaternion.identity;
        var endRotation = Quaternion.Euler(0, 0, open ? arrowRotationOn : arrowRotationOff);
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = elapsed / duration;
            rectTransform.anchoredPosition = Vector2.Lerp(start, end, t);
            if (arrowRectTransform) arrowRectTransform.localRotation = Quaternion.Lerp(startRotation, endRotation, t);
            yield return null;
        }
        SetPosition(open);
        moveCoroutine = null;
    }

    private void CreateHint()
    {
        if (IsTutorialScene) return;
        // Resolve an authored font before adding our label, otherwise the search
        // can select that new label's default Latin-only font.
        var template = GetComponentInChildren<TMP_Text>(true);
        var canvas = GetComponentInParent<Canvas>();
        if (template == null && canvas != null) template = canvas.GetComponentInChildren<TMP_Text>(true);
        var hintFont = template != null ? template.font : TMP_Settings.defaultFontAsset;
        hint = new GameObject("QuickChoiceTabHint", typeof(RectTransform));
        hint.layer = gameObject.layer;
        var rect = (RectTransform)hint.transform;
        rect.SetParent(transform.parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(12, -72);
        rect.sizeDelta = new Vector2(250, 26);
        var label = hint.AddComponent<TextMeshProUGUI>();
        label.font = hintFont;
        label.fontSize = 18;
        label.color = new Color32(190, 242, 176, 255);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        hint.AddComponent<PSH_Script_LocalizedText>().SetKey("QuickChoice_TabHint");
    }
}
