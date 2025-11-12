using UnityEngine;
using UnityEngine.UI; // UI 이미지를 쓰려면 필요

public class PSH_Script_CheckmarkUI : MonoBehaviour
{

    public Toggle checkmark1;
    public Toggle checkmark2;
    public Toggle checkmark3;
    
    void Start()
    {
        // 이 스크립트가(UI가) 켜지자마자, 
        // 토글을 0개 상태(모두 꺼짐)로 초기화합니다.
        UpdateCheckmarks(0); 
    }
    // 이 함수를 Manager의 OnTargetHitCountChanged 이벤트에 연결합니다.
    public void UpdateCheckmarks(int hitCount)
    {
        // =============================================
        // ▼▼▼ 이 부분의 주석 '//'을 제거하거나 추가하세요 ▼▼▼
        // =============================================
        Debug.Log($"[CheckmarkUI] 이벤트 수신! 현재 맞은 개수: {hitCount}");
        // =============================================

        // Toggle의 '.isOn' 속성을 true/false로 직접 변경합니다.
        if(checkmark1) checkmark1.isOn = (hitCount >= 1);
        if(checkmark2) checkmark2.isOn = (hitCount >= 2);
        if(checkmark3) checkmark3.isOn = (hitCount >= 3);
    }
}