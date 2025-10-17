using System.Collections;
using UnityEngine;

public class KHS_Script_SlotMachineController : MonoBehaviour
{
    [Header("슬롯머신 룰렛정보")]
    [Tooltip("슬롯머신의 현재 배열")]
    [SerializeField] private int[] numberLists = new int[3];

    [Header("스코어 매니저 참조")]
    [SerializeField] private KHS_Script_ScoreManager scoreManager; // 점수 매니저

    // 내부 상태 변수
    private bool isSMActive = true; // 슬롯머신이 공을 받아들일 수 있는 상태인지 확인

    private void Awake()
    {
        // 에디터에서 직접 연결 안 됐을 경우 자동 탐색
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
    }

    private void OnTriggerEnter(Collider _collider)
    {
        if(_collider.CompareTag("Ball") && isSMActive)
        {
            Rigidbody ballRb = _collider.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                StartCoroutine(SlotMachineSequence(ballRb));
            }
        }
    }

    private IEnumerator SlotMachineSequence(Rigidbody _rb)
    {
        isSMActive = false;

        // 1. 입사각(속도 벡터)을 기억하고 공을 물리적으로 고정 후 숨김
        Vector3 incomingVelocity = _rb.linearVelocity;
        _rb.isKinematic = true;
        _rb.gameObject.SetActive(false);


        // 2. 슬롯머신 결과가 나올 때까지 대기 및 연출
        float spinDuration = 4.0f; // 슬롯 회전 총 시간
        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            for (int i = 0; i < numberLists.Length; i++)
            {
                numberLists[i] = Random.Range(1,4); // 1~4 범위
            }

            // 디버그용으로 회전 중 상태 출력 (선택사항)
            Debug.Log($"Spinning... {numberLists[0]} {numberLists[1]} {numberLists[2]}");

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        // 최종 결과 확정
        for (int i = 0; i < numberLists.Length; i++)
        {
            numberLists[i] = Random.Range(1, 4);
        }

        // 결과 출력
        Debug.LogWarning($"슬롯머신 결과: {numberLists[0]} | {numberLists[1]} | {numberLists[2]}");

        // 3. 점수 계산 및 전달
        int score = CalculateSlotScore(numberLists[0], numberLists[1], numberLists[2]);
        if (scoreManager != null)
        {
            scoreManager.AddScore(score);
            Debug.Log($"▶ 획득 점수: {score}");
        }
        else
        {
            Debug.LogError("ScoreManager를 찾을 수 없습니다!");
        }


        // 4. 공의 모습과 물리 효과를 다시 활성화하고, 반사벡터 방향으로 발사
        _rb.gameObject.SetActive(true);
        _rb.isKinematic = false;
        _rb.linearVelocity = - incomingVelocity;

        // 5. 짧은 시간 후 슬롯머신을 다시 활성화 (공이 완전히 벗어날 시간 확보)
        yield return new WaitForSeconds(0.5f);
        isSMActive = true;

    }
    private int CalculateSlotScore(int a, int b, int c)
    {
        if (a == b && b == c)
            return 5000;
        else if (a == b || b == c || a == c)
            return 1000;
        else
            return 500;
    }
}
