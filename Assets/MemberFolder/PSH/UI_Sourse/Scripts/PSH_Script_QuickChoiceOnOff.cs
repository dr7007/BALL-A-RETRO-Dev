using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Toggle을 사용하기 위해 추가
using UnityEngine.EventSystems; // 마우스 이벤트를 감지하기 위해 추가

// 1. 마우스 이벤트 감지를 위해 인터페이스 2개 추가
public class PSH_Script_QuickChoiceOnOff : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;

    [Header("Slide")]
    public float duration = 0.5f;
    private float originalX;
    private readonly float hiddenXOffset = -460f;

    [Header("Inventory Hook")]
    [SerializeField] private CJS_Script_ChoiceInventoryUI inventoryUI;
    [SerializeField] private bool rebuildOnEveryOpen = true;

    [Header("Arrow Settings")]
    [SerializeField] private RectTransform arrowRectTransform;
    [SerializeField] private float arrowRotationOn = 180f;
    [SerializeField] private float arrowRotationOff = 0f;

    // ▼▼▼ 자동 닫기 기능 추가 ▼▼▼
    [Header("Auto Close")]
    [Tooltip("이 패널을 제어하는 Toggle을 여기에 연결해야 합니다.")]
    [SerializeField] private Toggle controllingToggle; // 닫기 위해 상태를 변경할 Toggle
    [SerializeField] private float autoCloseDelay = 3f; // 자동 닫기 지연 시간

    private Coroutine moveCoroutine; // MoveUI 코루틴 참조
    private Coroutine autoCloseCoroutine; // 자동 닫기 타이머 코루틴 참조
    private bool isPanelOpen = false; // 패널의 현재 열림/닫힘 상태
    // ▲▲▲ 여기까지 ▲▲▲

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
        isPanelOpen = false; // 초기 상태는 닫힘

        if (arrowRectTransform != null)
        {
            arrowRectTransform.localRotation = Quaternion.Euler(0, 0, arrowRotationOff);
        }
    }

    public void OnToggleValueChanged(bool isToggledOn)
    {
        isPanelOpen = isToggledOn; // 패널 상태 갱신

        // 가시성 먼저 통지
        inventoryUI?.SetVisible(isToggledOn);

        // 열릴 때 최신 상태로
        if (isToggledOn && rebuildOnEveryOpen)
            inventoryUI?.RebuildAll();

        float targetX = isToggledOn ? originalX : originalX + hiddenXOffset;
        float targetRotationZ = isToggledOn ? arrowRotationOn : arrowRotationOff;

        // ▼▼▼ 수정: StopAllCoroutines() 대신 특정 코루틴 중지 ▼▼▼
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }
        moveCoroutine = StartCoroutine(MoveUI(targetX, targetRotationZ));
        // ▲▲▲ 여기까지 ▲▲▲

        // ▼▼▼ 추가: 자동 닫기 타이머 관리 ▼▼▼
        if (isToggledOn)
        {
            // 패널이 열리면, 3초 타이머 시작 (마우스가 안 올라와 있을 수 있으므로)
            StartAutoCloseTimer();
        }
        else
        {
            // 패널이 닫히면, 타이머 중지
            StopAutoCloseTimer();
        }
        // ▲▲▲ 여기까지 ▲▲▲
    }

    private IEnumerator MoveUI(float targetX, float targetRotationZ)
    {
        float t = 0f;
        var start = rectTransform.anchoredPosition;
        var end = new Vector2(targetX, start.y);

        Quaternion startRotation = arrowRectTransform != null ? arrowRectTransform.localRotation : Quaternion.identity;
        Quaternion endRotation = Quaternion.Euler(0, 0, targetRotationZ);

        while (t < duration)
        {
            float normalizedTime = t / duration;
            rectTransform.anchoredPosition = Vector2.Lerp(start, end, normalizedTime);

            if (arrowRectTransform != null)
            {
                arrowRectTransform.localRotation = Quaternion.Lerp(startRotation, endRotation, normalizedTime);
            }

            t += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = end;
        if (arrowRectTransform != null)
        {
            arrowRectTransform.localRotation = endRotation;
        }

        moveCoroutine = null; // 코루틴이 끝났으므로 참조 비우기
    }

    // ▼▼▼ 3초 자동 닫기를 위한 새 함수들 ▼▼▼

    /// <summary>
    /// 마우스가 패널 영역에 들어왔을 때 호출됩니다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPanelOpen)
        {
            // 마우스가 올라왔으므로, 자동 닫기 타이머 중지
            StopAutoCloseTimer();
        }
    }

    /// <summary>
    /// 마우스가 패널 영역에서 나갔을 때 호출됩니다.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPanelOpen)
        {
            // 마우스가 나갔으므로, 자동 닫기 타이머 시작
            StartAutoCloseTimer();
        }
    }

    /// <summary>
    /// 자동 닫기 타이머를 (재)시작합니다.
    /// </summary>
    private void StartAutoCloseTimer()
    {
        StopAutoCloseTimer(); // 이미 실행 중인 타이머가 있다면 중지
        autoCloseCoroutine = StartCoroutine(AutoCloseTimerCoroutine());
    }

    /// <summary>
    /// 자동 닫기 타이머를 중지합니다.
    /// </summary>
    private void StopAutoCloseTimer()
    {
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
    }

    /// <summary>
    /// 3초를 기다린 후 토글을 끄는 코루틴입니다.
    /// </summary>
    private IEnumerator AutoCloseTimerCoroutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        // 3초가 지났을 때, 패널이 여전히 열려있고 토글이 연결되어 있다면
        if (isPanelOpen && controllingToggle != null)
        {
            // 토글을 끔으로써 패널을 닫습니다.
            // (이 코드는 OnToggleValueChanged(false)를 자동으로 호출합니다)
            controllingToggle.isOn = false;
        }
        autoCloseCoroutine = null; // 코루틴이 끝나면 참조 비우기
    }
    // ▲▲▲ 여기까지 ▲▲▲
}