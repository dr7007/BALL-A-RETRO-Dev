using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class PSH_Script_BaseButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        if (PSH_Script_CursorManager.Instance != null)
        {
            PSH_Script_CursorManager.Instance.SetHandCursor();
        }

    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        StartScaleTween(originalScale, scaleDuration); // <--- 이렇게 변경됨

        // 2. (고정) TMP 텍스트 복원
        if (buttonText != null)
        {
            buttonText.color = originalTextColor;
        }
        if (PSH_Script_CursorManager.Instance != null)
        {
            PSH_Script_CursorManager.Instance.SetDefaultCursor();
        }
    }

    protected virtual void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        

        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }

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
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        color.a = targetAlpha;
        buttonImage.color = color;
        fadeCoroutine = null;
    }


    /// <summary>
    /// 버튼 스케일을 부드럽게 변경
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
            float t = Mathf.SmoothStep(0.0f, 1.0f, time / duration);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            time += Time.unscaledDeltaTime;
            yield return null;
        }
        rectTransform.localScale = targetScale;
        scaleCoroutine = null; // 참조 비우기
    }
}