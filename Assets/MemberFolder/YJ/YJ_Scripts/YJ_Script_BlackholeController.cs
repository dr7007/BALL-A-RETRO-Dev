using System.Collections;
using UnityEngine;

public class YJ_Script_BlackholeController : MonoBehaviour
{
    [Header("중력장 설정")]
    [Tooltip("중력이 작용하는 반경(콜라이더 반경과 일치해야 함)")]
    [SerializeField] private float gravityRadius = 1f;
    [Tooltip("공을 중심으로 끌어당기는 힘")]
    [SerializeField] private float pullForce = 640f;
    [Tooltip("공이 타원 궤적을 그리게 하는 공전 힘")]
    [SerializeField] private float orbitalForce = 160f;

    [Header("캡처 및 발사 설정")]
    [Tooltip("공이 멈춰있는 시간 (초)")]
    [SerializeField] private float holdTime = 1.5f;
    [Tooltip("공을 사출하는 힘")]
    [SerializeField] private float launchForce = 10f;

    // 내부 상태 변수
    private bool isBallCaptured = false;

    // 에디터에서 중력 범위를 하늘색 선으로 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, gravityRadius);
    }

    // 트리거 범위 안에 다른 Collider가 머무는 동안 매 물리 프레임마다 호출
    private void OnTriggerStay(Collider other)
    {
        // 공이 "Ball" 태그를 가졌고, 아직 붙잡힌 상태가 아니라면 중력장 효과를 적용
        if (other.CompareTag("Ball") && !isBallCaptured)
        {
            Rigidbody ballRigidbody = other.GetComponent<Rigidbody>();
            if (ballRigidbody == null) return;

            // 블랙홀 중심과 공의 거리 계산
            float distance = Vector3.Distance(transform.position, other.transform.position);

            // 1. 공이 중심에 거의 도달했을 때 캡처 로직 실행
            if (distance < 0.1f)
            {
                StartCoroutine(HoldingAndLaunchCoroutine(ballRigidbody));
            }
            // 아직 거리가 있다면 중력과 공전력을 계속 적용
            else
            {
                BlackholeGravity(ballRigidbody);
            }
        }
    }

    // 공에 중력과 공전력을 가하는 함수
    private void BlackholeGravity(Rigidbody rb)
    {
        float distance = Vector3.Distance(transform.position, rb.position);

        // 현재 거리가 중력 반경에서 차지하는 비율 (0.0 ~ 1.0)
        // 중심에 있으면 0, 경계에 있으면 1
        float distanceRatio = Mathf.Clamp01(distance / gravityRadius);

        // 중심으로 당기는 힘 (Pull Force)
        // 경계에 가까울수록(distanceRatio가 1에 가까울수록) 힘을 더 강하게 줌
        float dynamicPullForce = pullForce * (1 + distanceRatio); // 경계에서 최대 2배의 힘
        Vector3 pullDirection = (transform.position - rb.position).normalized;
        rb.AddForce(pullDirection * dynamicPullForce * Time.fixedDeltaTime);

        // 궤도를 도는 힘 (Orbital Force)
        // 경계에 가까울수록(distanceRatio가 1에 가까울수록) 힘을 약하게 만들어 밖으로 벗어나지 않게 함
        float dynamicOrbitalForce = orbitalForce * (1 - distanceRatio);
        Vector3 orbitalDirection = Vector3.Cross(pullDirection, Vector3.up).normalized;
        rb.AddForce(orbitalDirection * dynamicOrbitalForce * Time.fixedDeltaTime);
    }


    // 캡처, 대기, 발사, 비활성화를 순서대로 진행하는 코루틴
    private IEnumerator HoldingAndLaunchCoroutine(Rigidbody rb)
    {
        // 1. 공을 캡처 상태로 전환
        isBallCaptured = true;

        // 2. 공의 움직임을 멈추고 중력 영향을 받지 않도록 isKinematic 활성화
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.transform.position = transform.position; // 위치를 블랙홀 중심으로 완벽히 일치시킴

        // 3. 설정된 시간 동안 대기
        yield return new WaitForSeconds(holdTime);

        // 4. 중력 재활성화 및 랜덤 방향으로 사출
        rb.isKinematic = false; // AddForce를 적용하기 위해 isKinematic을 다시 비활성화

        // 5. 랜덤한 2D 방향(XZ 평면) 생성 및 해당 방향으로 공 사출
        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        Vector3 launchDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
        rb.AddForce(launchDirection * launchForce, ForceMode.Impulse);

        // 6. 블랙홀 자기 자신을 비활성화하여 공을 다시 붙잡지 않도록 함
        gameObject.SetActive(false);
    }
}