using System;
using UnityEngine;

public class KHS_Script_FliperController : MonoBehaviour
{
    // 유니코드 변경 후 테스트용
    [Header("플리퍼 세팅")]
    [SerializeField] private YJ_Script_Flipper[] flippers; // 여러 플리퍼를 관리할 배열
    [SerializeField] private float flipperSpeed = 800f; // 플리퍼 속도

    public float impactForceMultiplier = 80f; // 충격량 계수
    
    private void Start()
    {
        // 각 플리퍼의 초기 회전값과 작동시 회전값을 계산하고 저장
        foreach (var flipper in flippers)
        {
            if (flipper.rigidbody != null)
            {
                flipper.restRotation = flipper.rigidbody.rotation;  // 플리퍼 멈춘 상태의 회전값 저장
                flipper.activeRotation = flipper.restRotation * Quaternion.Euler(0, flipper.flipperAngle, 0);   // 플리퍼 작동시의 회전값 저장
            }
        }
    }

    private void Update()
    {
        // 지정한 키 입력을 감지
        foreach (var flipper in flippers)
        {
            if (flipper.rigidbody != null)
            {
                if (Input.GetKeyDown(flipper.inputKey))
                {
                    flipper.isPressed = true;
                    flipper.invisibleCollider.isTrigger = false;
                }
                if (Input.GetKeyUp(flipper.inputKey))
                {
                    flipper.isPressed = false;
                    flipper.invisibleCollider.isTrigger = true;
                }
            }
        }
    }

    private void FixedUpdate()  // 플리퍼 회전 처리
    {
        foreach (var flipper in flippers)
        {
            if (flipper.rigidbody != null)
            {
                // isPressed 상태에 따라 회전값을 지정
                Quaternion targetRotation = flipper.isPressed ? flipper.activeRotation : flipper.restRotation;

                // 플리퍼 회전
                flipper.rigidbody.MoveRotation(
                    Quaternion.RotateTowards(
                        flipper.rigidbody.rotation,
                        targetRotation,
                        flipperSpeed * Time.fixedDeltaTime
                    )
                );
            }
        }
    }
}
