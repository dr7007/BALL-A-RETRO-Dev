using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class StartButton : BaseButtonHover, IPointerClickHandler
{
    public UnityEvent onClick; 

    [Header("Fade Effect")]
    [SerializeField] private bool useFadeEffect = true;
    [SerializeField] private float hoverAlpha = 0.7f;
    [SerializeField] private float fadeDuration = 0.2f;
    private float originalAlpha; 

    [Header("Change Image")]
    [SerializeField] private Image targetBackground;
    [SerializeField] private Sprite hoverBackgroundSprite; 
    
    [Tooltip("Set Background Native Size On Hover")] // <--- 툴팁 추가
    [SerializeField] private bool setBackgroundNativeSizeOnHover = false; // <--- 새 옵션 추가
    
    private Sprite originalBackgroundSprite; 
    private Vector2 originalBackgroundSize; // <--- 새 변수 추가: 원본 배경 크기 저장용


    protected override void Awake()
    {
        base.Awake(); 
        
        if (buttonImage != null)
        {
            originalAlpha = buttonImage.color.a;
        }
        if (onClick == null)
        {
            onClick = new UnityEvent();
        }

        if (targetBackground != null)
        {
            originalBackgroundSprite = targetBackground.sprite;
            // (추가) 원본 배경의 크기도 저장해둡니다.
            originalBackgroundSize = targetBackground.rectTransform.sizeDelta; 
        }
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData); 
        
        if (useFadeEffect)
        {
            StartImageFade(hoverAlpha, fadeDuration);
        }

        if (targetBackground != null && hoverBackgroundSprite != null)
        {
            targetBackground.sprite = hoverBackgroundSprite;

            // ▼▼▼ 새 기능: 호버 시 배경 크기 조절 ▼▼▼
            if (setBackgroundNativeSizeOnHover)
            {
                targetBackground.SetNativeSize(); // 바뀐 스프라이트의 원본 크기로 변경
            }
            // ▲▲▲ 여기까지 ▲▲▲
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData); 
        
        if (useFadeEffect)
        {
            StartImageFade(originalAlpha, fadeDuration);
        }

        if (targetBackground != null)
        {
            targetBackground.sprite = originalBackgroundSprite;

            // ▼▼▼ 새 기능: 배경 크기 원상 복구 ▼▼▼
            // 호버 시 크기를 변경했다면, 다시 원래 크기로 되돌립니다.
            if (setBackgroundNativeSizeOnHover)
            {
                targetBackground.rectTransform.sizeDelta = originalBackgroundSize;
            }
            // ▲▲▲ 여기까지 ▲▲▲
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }
}