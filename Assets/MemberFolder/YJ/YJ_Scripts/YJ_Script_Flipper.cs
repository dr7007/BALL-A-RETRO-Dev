using UnityEngine;

[System.Serializable]
public class YJ_Script_Flipper
{
    public Rigidbody rigidbody;         // 각 플리퍼의 Rigidbody 컴포넌트
    public KeyCode inputKey;            // 이 플리퍼를 조작할 키
    public float flipperAngle;          // 플리퍼가 꺾일 최대 각도

    // 내부 계산에 사용될 변수들
    [HideInInspector] public Quaternion restRotation;   // 시작시의 플리퍼 회전값
    [HideInInspector] public Quaternion activeRotation; // 버튼을 눌렀을 때의 회전값
    [HideInInspector] public bool isPressed = false;    // 버튼이 눌렸는지 판정
}