using System;
using System.Collections;
using UnityEngine;

public class KHS_Script_PortalController : MonoBehaviour
{
    public static event Action<int> portalEvt;
    public static event Action portalEndEvt;

    [Header("출구 포탈 지정")]
    [Tooltip("공이 튀어나갈 출구 포탈")]
    [SerializeField] private KHS_Script_PortalController exitPortal;

    [Header("포탈 사용 횟수")]
    [Tooltip("포탈을 사용할 수 있는 횟수, -1로 설정하면 무제한")]
    [SerializeField] private int activationCount = -1;
    [Tooltip("포탈과 맞물리는 카메라 홀더의 인덱스")]
    [SerializeField] private int portalIndex = -1;

    // 내부 상태 변수
    public bool isActivated = true; // 포탈이 공을 받아들일 수 있는 상태인지 확인
    private Transform spawnPoint;

    [Header("방출 속도 랜덤 설정")]
    [Tooltip("방출 속도의 최소값과 최대값")]
    [SerializeField] private Vector2 randomSpeedRange = new Vector2(5f, 15f);

    [Tooltip("방출 각도 범위 (도 단위, 예: 0~45면 45도 내에서 랜덤 방향)")]
    [SerializeField] private float randomAngleRange = 45f;

    private void Start()
    {
        // 다른 포탈에 들어간 공이 나올 위치(이 포탈의 위치)
        spawnPoint = GetComponent<Transform>();
    }

    // 에디터에서 입구와 출구의 연결을 시각적으로 보여주는 선을 그립니다.
    private void OnDrawGizmos()
    {
        if (exitPortal != null)
        {
            // 잔여 사용 횟수가 0이면 회색으로 표시
            Gizmos.color = (activationCount == 0) ? Color.gray : Color.cyan;
            Gizmos.DrawLine(transform.position, exitPortal.transform.position);
        }
    }

    // 트리거 안으로 다른 Collider가 들어오는 순간 호출
    private void OnTriggerEnter(Collider other)
    {
        // 포탈 사용 횟수가 0보다 크거나 무제한인지 확인
        bool isUsable = (activationCount > 0 || activationCount == -1);

        // 들어온 것이 "Ball" 태그를 가진 오브젝트이고, 포탈이 현재 활성 상태라면
        if (other.CompareTag("Ball") && isActivated && isUsable)
        {
            // 출구가 지정되지 않았다면 경고를 출력하고 종료
            if (exitPortal == null)
            {
                Debug.LogError("출구 포탈이 지정되지 않았습니다!", this.gameObject);
                return;
            }

            Rigidbody ballRigidbody = other.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
                // 사용 횟수가 무제한이 아니면 카운터를 1씩 차감
                if (activationCount > 0)
                {
                    activationCount--;
                }
            }

            StartCoroutine(TeleportCoroutine(ballRigidbody));
        }
    }

    // 텔레포트, 대기, 사출을 순서대로 진행하는 코루틴
    private IEnumerator TeleportCoroutine(Rigidbody rb)
    {
        // 입구와 출구 포탈을 모두 비활성화하여 중복 작동 방지
        isActivated = false;
        exitPortal.isActivated = false; // 출구에서 바로 다시 들어가는 현상 방지

        rb.isKinematic = true;
        rb.gameObject.SetActive(false);

        // 설정된 시간 동안 대기
        yield return StartCoroutine(TeleportSequenceCoroutine());

        // 출구 웜홀의 스폰 위치로 공을 이동시키고 다시 보이게 함
        rb.transform.position = exitPortal.spawnPoint.position;
        rb.gameObject.SetActive(true);

        portalEndEvt?.Invoke();

        // 기본 방향은 출구 포탈의 정면 방향을 기준으로 랜덤화
        Vector3 baseDir = exitPortal.transform.right;

        // randomAngleRange 내에서 임의 회전
        Quaternion randomRot = Quaternion.Euler(
            UnityEngine.Random.Range(-randomAngleRange, randomAngleRange),
            UnityEngine.Random.Range(-randomAngleRange, randomAngleRange),
            0f
        );

        Vector3 randomDir = randomRot * baseDir;

        // 속도 랜덤화
        float randomSpeed = UnityEngine.Random.Range(randomSpeedRange.x, randomSpeedRange.y);
        rb.isKinematic = false;
        // 최종 적용
        rb.linearVelocity = randomDir.normalized * randomSpeed;

        // 짧은 시간 후 출구 포탈을 재활성화 (공이 완전히 벗어날 시간 확보)
        yield return new WaitForSeconds(0.5f);
        exitPortal.isActivated = true;

        // 입구 포탈을 재활성화
        isActivated = true;
    }

    private IEnumerator TeleportSequenceCoroutine()
    {
        portalEvt.Invoke(portalIndex);
        yield return new WaitForSeconds(2f);
    }

    public void PortalTempUnactive()
    {
        StartCoroutine(PortalUnactiveCoroutine());
    }

    private IEnumerator PortalUnactiveCoroutine()
    {
        isActivated = false;
        yield return new WaitForSeconds(2.0f);
        isActivated = true;
    }
}
