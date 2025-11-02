using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Toggle 컴포넌트를 사용하기 위해 필요

public class PSH_Script_QuickChoiceOnOff : MonoBehaviour
{
    private RectTransform rectTransform;
    public float duration = 0.5f; 
    
    private float originalX; 
    private readonly float hiddenXOffset = -460f; 

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("RectTransform 컴포넌트를 찾을 수 없습니다. 이 스크립트는 UI 요소에 부착되어야 합니다.");
            return;
        }

        // 1. 원래 위치 저장
        originalX = rectTransform.anchoredPosition.x; 
        
        // 2. 초기 상태를 '숨김'으로 설정 (필요에 따라 주석 처리 가능)
        Vector2 hiddenPos = rectTransform.anchoredPosition;
        hiddenPos.x = originalX + hiddenXOffset;
        rectTransform.anchoredPosition = hiddenPos;
    }
    

   
    public void OnToggleValueChanged(bool isToggledOn)
    {
        Debug.Log("토글 상태 변경 감지: " + isToggledOn); 

        float targetX = isToggledOn ? originalX : originalX + hiddenXOffset;
        
        // ⭐ 여기에 Debug.Log 추가
        Debug.Log($"[이동 시작] 목표 X: {targetX}, 현재 X: {rectTransform.anchoredPosition.x}");
        
        StopAllCoroutines(); 
        StartCoroutine(MoveUI(targetX));
    }

   IEnumerator MoveUI(float targetX)
    {
        float timeElapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = new Vector2(targetX, startPos.y);
        
        Debug.Log("코루틴 시작! 목표: " + targetX); 

        while (timeElapsed < duration)
        {
            // 1. 경과 시간을 0~1 사이의 비율(t)로 변환
            float t = timeElapsed / duration;

            // [선택 사항] 더 부드러운 움직임을 위한 SmoothStep
            // t = t * t * (3f - 2f * t);

            // 2. Lerp를 사용해 현재 프레임의 위치 계산
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            
            // 3. 경과 시간 증가
            timeElapsed += Time.deltaTime;
            
            yield return null; // 다음 프레임까지 대기
        }
        
        // 4. 루프가 끝나면 정확히 목표 위치로 설정
        rectTransform.anchoredPosition = endPos;
        Debug.Log("코루틴 완료!");
    }
}