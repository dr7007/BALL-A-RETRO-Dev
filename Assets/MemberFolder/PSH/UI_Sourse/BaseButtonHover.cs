using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class BaseButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("필수 연결: 버튼 텍스트")]
    [SerializeField] protected TextMeshProUGUI buttonText;

    [Header("고정 설정")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] protected Color textHoverColor = Color.cyan;

    protected RectTransform rectTransform;
    protected Image buttonImage;

    private Vector3 originalScale;
    private Color originalTextColor;
    private float originalImageAlpha; // 알파 값만 따로 저장

    // --- 코루틴 안전 장치 ---
    private Coroutine fadeCoroutine;

    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>();

        originalScale = rectTransform.localScale;

        if (buttonImage != null)
        {
            originalImageAlpha = buttonImage.color.a; // 원본 알파 저장
        }

        if (buttonText != null)
        {
            originalTextColor = buttonText.color;
        }
        else
        {
            Debug.LogWarning("ButtonText가 연결되지 않았습니다.", this.gameObject);
        }
    }

    // --- 마우스 호버 이벤트 ---

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        // 1. (고정) 스케일 키우기
        rectTransform.localScale = originalScale * hoverScale;

        // 2. (고정) TMP 텍스트 하이라이트
        if (buttonText != null)
        {
            buttonText.color = textHoverColor;
        }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        // 1. (고정) 스케일 복원
        rectTransform.localScale = originalScale;

        // 2. (고정) TMP 텍스트 복원
        if (buttonText != null)
        {
            buttonText.color = originalTextColor;
        }
    }

    /// <summary>
    /// (안전 장치 1) 오브젝트가 비활성화될 때 상태를 강제 원복
    /// </summary>
    protected virtual void OnDisable()
    {
        // 코루틴이 실행 중일 수 있으므로 확실하게 중지
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // 호버 상태에서 비활성화될 경우를 대비해 값 원복
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
        if (buttonText != null)
        {
            buttonText.color = originalTextColor;
        }
        if (buttonImage != null)
        {
            // 페이드가 진행 중이었어도 원본 알파로 즉시 복구
            Color color = buttonImage.color;
            color.a = originalImageAlpha;
            buttonImage.color = color;
        }
    }


    // --- 선택적 기능 (자식이 호출해서 사용) ---

    /// <summary>
    /// 버튼 이미지를 지정한 알파값으로 페이드합니다. (안전 장치 적용됨)
    /// </summary>
    protected void StartImageFade(float targetAlpha, float duration)
    {
        if (buttonImage == null) return;

        // (안전 장치 2)
        // 오브젝트가 비활성화 상태면 코루틴을 시작하지 않음 (에러 방지)
        if (!this.gameObject.activeInHierarchy)
        {
            return;
        }

        // (안전 장치 3)
        // 이전에 실행 중인 페이드가 있다면 확실하게 중지하고 참조를 비움
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 새 페이드 코루틴 시작
        fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha, duration));
    }

    private IEnumerator FadeCoroutine(float targetAlpha, float duration)
    {
        float startAlpha = buttonImage.color.a;
        Color color = buttonImage.color;
        float time = 0f;

        // (안전 장치 4)
        // duration이 0 이하면 즉시 완료 (0으로 나누기 방지)
        if (duration <= 0f)
        {
            color.a = targetAlpha;
            buttonImage.color = color;
            fadeCoroutine = null; // (안전 장치 5)
            yield break; // 코루틴 즉시 종료
        }

        while (time < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            color.a = alpha;
            buttonImage.color = color;

            time += Time.deltaTime;
            yield return null;
        }

        // 정확하게 목표 알파 값으로 설정
        color.a = targetAlpha;
        buttonImage.color = color;

        // (안전 장치 5)
        // 코루틴이 정상적으로 완료되면 참조를 null로 비워줌
        fadeCoroutine = null;
    }
}