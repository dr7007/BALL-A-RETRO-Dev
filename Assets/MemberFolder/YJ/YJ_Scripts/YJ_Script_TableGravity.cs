using UnityEngine;

public class YJ_Script_TableGravity : MonoBehaviour
{
    private void Start()
    {
        // 현실 핀볼 테이블의 기울기(6.5도)를 반영한 중력 설정
        Physics.gravity = new Vector3(0, 0, -30f * Mathf.Sin(6.5f * Mathf.Deg2Rad));
    }
}