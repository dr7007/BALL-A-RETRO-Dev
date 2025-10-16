using UnityEngine;


public class YJ_Script_FlipperController : MonoBehaviour
{
    [Header("플리퍼 세팅")]
    [SerializeField] private YJ_Script_Flipper[] flippers; // 여러 플리퍼를 관리할 배열
    [SerializeField] private float flipperSpeed = 500f; // 플리퍼 속도
    [SerializeField] private float impactForceMultiplier = 50f; // 충격량 계수

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
                }
                if (Input.GetKeyUp(flipper.inputKey))
                {
                    flipper.isPressed = false;
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

    private void OnCollisionEnter(Collision collision)  // 공 충돌 관련 물리 처리
    {
        // 공이 충돌하는 지점에서의 플리퍼 속도를 계산
        Rigidbody flipperRb = collision.collider.GetComponent<Rigidbody>();
        if (flipperRb != null)
        {
            // 공과 접촉하는 지점과 플리퍼의 Angular Velocity를 이용해
            // 해당 지점에서의 Linear Velocity를 계산
            Vector3 pointVelocity = flipperRb.GetPointVelocity(collision.contacts[0].point);

            // 공의 Rigidbody에 계산된 속도 방향으로 힘을 가함
            Rigidbody ballRb = collision.collider.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.AddForce(pointVelocity * impactForceMultiplier, ForceMode.Impulse);
            }
        }
    }
}