using System.Collections;
using UnityEngine;

public class YJ_Script_WormholeController : MonoBehaviour
{
    [Header("출구 웜홀 지정")]
    [Tooltip("공이 튀어나갈 출구 웜홀")]
    [SerializeField] private YJ_Script_WormholeController exitWormhole;

    [Header("웜홀 대기 시간")]
    [Tooltip("공이 웜홀 안에 머무는 시간 (초)")]
    [SerializeField] private float waitTime = 2f;

    // 내부 상태 변수
    private bool isWormholeActive = true; // 웜홀이 공을 받아들일 수 있는 상태인지 확인
    private Transform spawnPoint;


    private void Start()
    {
        // 다른 웜홀에 들어간 공이 나올 위치(이 웜홀의 위치)
        spawnPoint = GetComponent<Transform>();
    }

    // 에디터에서 입구와 출구의 연결을 시각적으로 보여주는 선을 그립니다.
    private void OnDrawGizmos()
    {
        if (exitWormhole != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, exitWormhole.transform.position);
        }
    }

    // 트리거 안으로 다른 Collider가 들어오는 순간 호출
    private void OnTriggerEnter(Collider other)
    {
        // 들어온 것이 "Ball" 태그를 가진 오브젝트이고, 웜홀이 현재 활성 상태라면
        if (other.CompareTag("Ball") && isWormholeActive)
        {
            // 출구가 지정되지 않았다면 경고를 출력하고 종료
            if (exitWormhole == null)
            {
                Debug.LogError("출구 웜홀이 지정되지 않았습니다!", this.gameObject);
                return;
            }

            Rigidbody ballRigidbody = other.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
                StartCoroutine(TeleportSequence(ballRigidbody));
            }
        }
    }

    // 텔레포트, 대기, 사출을 순서대로 진행하는 코루틴
    private IEnumerator TeleportSequence(Rigidbody rb)
    {
        // 1. 입구와 출구 웜홀을 모두 비활성화하여 중복 작동 방지
        isWormholeActive = false;
        exitWormhole.isWormholeActive = false; // 출구에서 바로 다시 들어가는 현상 방지

        // 2. 입사각(속도 벡터)을 기억하고 공을 물리적으로 고정 후 숨김
        Vector3 incomingVelocity = rb.linearVelocity;
        rb.isKinematic = true;
        rb.gameObject.SetActive(false);

        // 3. 설정된 시간 동안 대기
        yield return new WaitForSeconds(waitTime);

        // 4. 출구 웜홀의 스폰 위치로 공을 이동시키고 다시 보이게 함
        rb.transform.position = exitWormhole.spawnPoint.position;
        rb.gameObject.SetActive(true);

        // 5. 물리 효과를 다시 활성화하고, 기억해둔 입사각(속도)을 그대로 적용
        rb.isKinematic = false;
        rb.linearVelocity = incomingVelocity;

        // 6. 짧은 시간 후 출구 웜홀을 다시 활성화 (공이 완전히 벗어날 시간 확보)
        yield return new WaitForSeconds(0.5f);
        exitWormhole.isWormholeActive = true;

        // 입구 웜홀을 재활성화
        isWormholeActive = true;
    }
}