using System;
using UnityEngine;

public class KHS_Script_FliperController : MonoBehaviour
{
    public static event Action FliperCountChangeEvt;
    public static event Action OnAnyFlipperPress;
    public static event Action OnAnyFlipperRelease;

    [Header("플리퍼 세팅")]
    [SerializeField] private YJ_Script_Flipper[] flippers; // 여러 플리퍼를 관리할 배열
    [SerializeField] private float flipperSpeed = 800f; // 플리퍼 속도

    private bool isCollision = false;
    public float impactForceMultiplier = 80f; // 충격량 계수
    public int fliper_Count = 10;
    private int fliper_Inital = -1;

    [Header("플리퍼 유예 시간")]
    [SerializeField] 
    private float fliperCooldown = 0.2f; // 0.2초 유예 (원하는 값으로 조정 가능)
    private float lastFliperUseTime = -999f; // 마지막으로 카운트 깎인 시간

    // 마지막 한 번의 플리퍼 동작을 보장하기 위한 상태값
    private bool lastFlipperActive = false;
    private float lastFlipperEndTime = 0f;
    [SerializeField] 
    private float lastFlipperActiveDuration = 0.75f; // 마지막 한 번 유지시간 (0.25초 정도 추천)

    private void OnEnable()
    {
        KHS_Script_FliperDumpManager.OnFliperCollision += OnFliper;
        KHS_Script_FliperDumpManager.OffFliperCollision += OffFliper;
        KHS_Script_BallOutController.BallOutEvt += FliperCountReset;
    }
    private void OnDisable()
    {
        KHS_Script_FliperDumpManager.OnFliperCollision -= OnFliper;
        KHS_Script_FliperDumpManager.OffFliperCollision -= OffFliper;
        KHS_Script_BallOutController.BallOutEvt -= FliperCountReset;
    }

    private void OffFliper(Collision collision)
    {
        // 유예 시간 내에는 무시
        if (Time.time - lastFliperUseTime < fliperCooldown)
            return;

        if (Input.GetKey(flippers[0].inputKey) || Input.GetKey(flippers[1].inputKey))
        {
            if (fliper_Count == 1)
            {
                lastFlipperActive = true;
                lastFlipperEndTime = Time.time + lastFlipperActiveDuration;
                fliper_Count--;
                FliperCountChangeEvt?.Invoke();
            }

            else if (fliper_Count > 0)
            {
                fliper_Count--;
                lastFliperUseTime = Time.time; // 시간 갱신
                FliperCountChangeEvt?.Invoke();
            }
        }
            isCollision = false;
    }
    private void OnFliper(Collision collision)
    {
        //// 유예 시간 내에는 무시
        //if (Time.time - lastFliperUseTime < fliperCooldown)
        //    return;

        //// 키 입력 시만 카운트 감소
        //// To Do : 현재 구조에서 플리퍼 구분이 안되어 공과 플리퍼 2개 중 반대편에 맞다아있을때 동작해서 추가적으로 감소하는 현상 발생
        //// 수정 요함
        //else if (Input.GetKey(flippers[0].inputKey) || Input.GetKey(flippers[1].inputKey))
        //{
        //    // 마지막 1회일 경우 → 감소 후에도 한 번은 강제 활성 유지
        //    if (fliper_Count == 1)
        //    {
        //        lastFlipperActive = true;
        //        lastFlipperEndTime = Time.time + lastFlipperActiveDuration;
        //    }

        //    fliper_Count--;
        //    lastFliperUseTime = Time.time; // 시간 갱신
        //    FliperCountChangeEvt?.Invoke();
        //}

        isCollision = true;
    }
    private void FliperCountReset()
    {
        fliper_Count = fliper_Inital;
        FliperCountChangeEvt.Invoke();
        lastFlipperActive = false;
    }

    private void Start()
    {
        fliper_Inital = fliper_Count;
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
            if (flipper.rigidbody != null && (fliper_Count > 0 || lastFlipperActive))
            {
                if (Input.GetKeyDown(flipper.inputKey))
                    OnAnyFlipperPress?.Invoke();

                if (Input.GetKey(flipper.inputKey))
                {
                    flipper.isPressed = true;
                    flipper.invisibleCollider.isTrigger = false;
                }

                if (Input.GetKeyUp(flipper.inputKey))
                {
                    flipper.isPressed = false;
                    flipper.invisibleCollider.isTrigger = true;
                    OnAnyFlipperRelease?.Invoke();
                }

                // 마지막 한 번 강제 활성 유지
                if (lastFlipperActive && Time.time < lastFlipperEndTime)
                {
                    flipper.isPressed = true;
                    flipper.invisibleCollider.isTrigger = false;
                }
                else if (lastFlipperActive && Time.time >= lastFlipperEndTime)
                {
                    lastFlipperActive = false;
                    flipper.isPressed = false;
                    flipper.invisibleCollider.isTrigger = true;
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
    public void FlipperCountUpdate()
    {
        FliperCountChangeEvt.Invoke();
    }
    public void FlipperCountUp(int _up)
    {
        fliper_Inital += _up;
    }
}
