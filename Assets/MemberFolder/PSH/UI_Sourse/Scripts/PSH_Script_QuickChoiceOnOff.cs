using System.Collections;
using UnityEngine;
using UnityEngine.UI; 

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

        originalX = rectTransform.anchoredPosition.x; 
        
        Vector2 hiddenPos = rectTransform.anchoredPosition;
        hiddenPos.x = originalX + hiddenXOffset;
        rectTransform.anchoredPosition = hiddenPos;
    }
   
    public void OnToggleValueChanged(bool isToggledOn)
    {
        float targetX = isToggledOn ? originalX : originalX + hiddenXOffset;
        StopAllCoroutines(); 
        StartCoroutine(MoveUI(targetX));
    }

   IEnumerator MoveUI(float targetX)
    {
        float timeElapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = new Vector2(targetX, startPos.y);
        while (timeElapsed < duration)
        {
            float t = timeElapsed / duration;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            timeElapsed += Time.deltaTime;
            
            yield return null;
        }
        rectTransform.anchoredPosition = endPos;

    }
}