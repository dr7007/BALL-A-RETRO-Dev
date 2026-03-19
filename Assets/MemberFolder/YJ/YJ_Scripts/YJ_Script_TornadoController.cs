using UnityEngine;

public class YJ_Script_TornadoController : MonoBehaviour
{
    // 회전 방향을 인스펙터에서 쉽게 선택하도록 설정
    public enum RotationDirection
    {
        Clockwise,        // 시계 방향
        CounterClockwise  // 반시계 방향
    }

    [Header("토네이도 설정")]
    [Tooltip("분당 회전수 (RPM)")]
    [SerializeField] private float rpm = 400f;

    [Tooltip("무작위 힘의 세기")]
    [SerializeField] private float randomKickForce = 2f;

    [Tooltip("회전 방향")]
    [SerializeField] private RotationDirection direction = RotationDirection.Clockwise;

    // 물리법칙 작용을 위해서는 Rigidbody 필수
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 토네이도 Rigidbody 초기 설정
        rb.isKinematic = false;
        rb.useGravity = false; // 중력 사용 안 함
        rb.mass = 100f; // 공에 쉽게 밀리지 않도록 매우 무겁게 설정
        rb.angularDamping = 0f; // 회전 저항 0 (속도를 직접 제어하므로)

        // 토네이도가 충격에 의해 넘어지거나 밀려나지 않도록 위치와 X, Z축 회전을 고정
        rb.constraints = RigidbodyConstraints.FreezePosition |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    // 물리 업데이트는 FixedUpdate에서 처리
    private void FixedUpdate()
    {
        // RPM(분당 회전수) -> 라디안/초 변환
        // 1. RPM / 60 = RPS (초당 회전수)
        float rps = rpm / 60f;
        // 2. RPS * 360 = 초당 각도(Degree)
        // 3. 초당 각도 * Mathf.Deg2Rad = 초당 라디안(Radian)
        float angularSpeedRadians = rps * 360f * Mathf.Deg2Rad;

        // 6. 방향 설정 (Unity의 Y축 기준: 시계방향 = -Y, 반시계방향 = +Y)
        float directionMultiplier = (direction == RotationDirection.Clockwise) ? -1f : 1f;

        // 7. 최종 각속도 벡터 계산
        Vector3 targetAngularVelocity = Vector3.up * directionMultiplier * angularSpeedRadians;

        // 8. Rigidbody의 각속도를 강제로 설정
        //   -> 물리 엔진이 이 속도로 회전하도록 유지하며, 
        //      다른 물체(공)와 충돌 시 이 속도를 계산에 반영.
        rb.angularVelocity = targetAngularVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            Rigidbody ballRb = collision.rigidbody;
            if (ballRb != null)
            {
                Vector3 currentVel = ballRb.linearVelocity;
                currentVel.y = 0;
                ballRb.linearVelocity = currentVel;

                float randomAngle = UnityEngine.Random.Range(-25f, 25f);
                Vector3 pushDir = Quaternion.Euler(0, randomAngle, 0) * collision.contacts[0].normal;

                float kickForce = 5f;
                ballRb.AddForce(pushDir * kickForce, ForceMode.Impulse);
            }
        }
    }

    // 외부에서 RPM을 변경할 때 사용할 메소드
    public void SetRPM(float newRPM)
    {
        this.rpm = newRPM;
    }

    // 외부에서 방향을 전환시킬 때 사용할 메소드
    public void SetDirection(RotationDirection newDirection)
    {
        this.direction = newDirection;
    }
}