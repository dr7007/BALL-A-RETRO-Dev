using System;
using UnityEngine;

public class KHS_Script_FliperController : MonoBehaviour
{
    public static event Action OnAnyFlipperPress;
    public static event Action OnAnyFlipperRelease;

    [Header("플리퍼 세팅")]
    [SerializeField] private YJ_Script_Flipper[] flippers; // 여러 플리퍼를 관리할 배열
    [SerializeField] private float flipperSpeed = 800f; // 플리퍼 속도

    private bool isCollision = false;
    public float impactForceMultiplier = 80f; // 충격량 계수
    public int fliper_Count = 10;

    private void OnEnable()
    {
        KHS_Script_FliperDumpManager.OnFliperCollision += OnFliper;
        KHS_Script_FliperDumpManager.OffFliperCollision += OffFliper;
    }
    private void OnDisable()
    {
        KHS_Script_FliperDumpManager.OnFliperCollision -= OnFliper;
        KHS_Script_FliperDumpManager.OffFliperCollision -= OffFliper;
    }

    private void OffFliper(Collision collision)
    {
        fliper_Count--;
        isCollision = false;
    }
    private void OnFliper(Collision collision)
    {
        isCollision = true;
    }

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
            if (flipper.rigidbody != null && fliper_Count > 0)
            {
                // 눌림 순간 이벤트
                if (Input.GetKeyDown(flipper.inputKey))
                    OnAnyFlipperPress?.Invoke();

                if (Input.GetKey(flipper.inputKey))
                {
                    flipper.isPressed = true;
                    flipper.invisibleCollider.isTrigger = false;
                }

                //  해제 순간 이벤트
                if (Input.GetKeyUp(flipper.inputKey))
                {
                    flipper.isPressed = false;
                    flipper.invisibleCollider.isTrigger = true;
                    OnAnyFlipperRelease?.Invoke();
                }
            }
            else
            {
                flipper.isPressed = false;
                flipper.invisibleCollider.isTrigger = true;
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
