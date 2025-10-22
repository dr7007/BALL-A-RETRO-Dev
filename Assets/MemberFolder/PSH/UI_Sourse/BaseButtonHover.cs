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
    [SerializeField] private float scaleDuration = 0.2f; 
    [SerializeField] protected Color textHoverColor = Color.cyan;

    protected RectTransform rectTransform;
    protected Image buttonImage;
    
    private Vector3 originalScale;
    private Color originalTextColor;
    private float originalImageAlpha;
    
    // --- Coroutine Safe ---
    private Coroutine fadeCoroutine;
    private Coroutine scaleCoroutine; 

    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>();

        originalScale = rectTransform.localScale; 
        if (buttonImage != null)
        {
            originalImageAlpha = buttonImage.color.a;
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
        
        StartScaleTween(originalScale * hoverScale, scaleDuration); 

        if (buttonText != null)
        {
            buttonText.color = textHoverColor;
        }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        // 1. (고정) 스케일 복원 (코루틴으로 변경)
        // rectTransform.localScale = originalScale; // <--- 이 코드가
        StartScaleTween(originalScale, scaleDuration); // <--- 이렇게 변경됨

        // 2. (고정) TMP 텍스트 복원
        if (buttonText != null)
        {
            buttonText.color = originalTextColor;
        }
    }

    protected virtual void OnDisable()
    {
        // 페이드 코루틴 중지
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        // <--- 추가됨: 스케일 코루틴 중지 ---
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        // <--- 여기까지 ---

        // <--- 변경됨: 스케일 즉시 원복 ---
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale; 
        }
        // <--- 여기까지 ---

        if (buttonText != null)
        {
            buttonText.color = originalTextColor;
        }
        if (buttonImage != null)
        {
            Color color = buttonImage.color;
            color.a = originalImageAlpha;
            buttonImage.color = color;
        }
    }


    protected void StartImageFade(float targetAlpha, float duration)
    {
        if (buttonImage == null || !this.gameObject.activeInHierarchy) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha, duration));
    }

    private IEnumerator FadeCoroutine(float targetAlpha, float duration)
    {
        float startAlpha = buttonImage.color.a;
        Color color = buttonImage.color;
        float time = 0f;

        if (duration <= 0f)
        {
            color.a = targetAlpha;
            buttonImage.color = color;
            fadeCoroutine = null;
            yield break;
        }

        while (time < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            color.a = alpha;
            buttonImage.color = color;
            time += Time.deltaTime;
            yield return null;
        }

        color.a = targetAlpha;
        buttonImage.color = color;
        fadeCoroutine = null;
    }

    // --- (내부) 스케일 트위닝 기능 (추가됨) ---

    /// <summary>
    /// 버튼 스케일을 부드럽게 변경합니다. (안전 장치 적용)
    /// </summary>
    private void StartScaleTween(Vector3 targetScale, float duration)
    {
        if (rectTransform == null || !this.gameObject.activeInHierarchy) return;

        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleCoroutine(targetScale, duration));
    }

    private IEnumerator ScaleCoroutine(Vector3 targetScale, float duration)
    {
        Vector3 startScale = rectTransform.localScale;
        float time = 0f;

        if (duration <= 0f)
        {
            rectTransform.localScale = targetScale;
            scaleCoroutine = null;
            yield break;
        }

        while (time < duration)
        {
            // Vector3.Lerp를 사용해 startScale에서 targetScale로 부드럽게 보간
            // Mathf.SmoothStep을 사용해 좀 더 부드러운 Ease In/Out 효과 적용
            float t = Mathf.SmoothStep(0.0f, 1.0f, time / duration);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            time += Time.deltaTime;
            yield return null;
        }

        // 정확하게 목표 값으로 설정
        rectTransform.localScale = targetScale;
        scaleCoroutine = null; // 참조 비우기
    }
}