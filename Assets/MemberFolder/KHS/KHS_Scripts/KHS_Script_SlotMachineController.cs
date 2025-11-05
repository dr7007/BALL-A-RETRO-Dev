//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class KHS_Script_SlotMachineController : MonoBehaviour
//{
//    [Header("슬롯머신 룰렛정보")]
//    [Tooltip("슬롯머신의 현재 배열")]
//    [SerializeField] private int[] numberLists = new int[3];
//    [SerializeField] private GameObject[] gameObjects = new GameObject[3];

//    [Header("스코어 매니저 참조")]
//    [SerializeField] private KHS_Script_ScoreManager scoreManager; // 점수 매니저

//    // 내부 상태 변수
//    private bool isSMActive = true; // 슬롯머신이 공을 받아들일 수 있는 상태인지 확인

//    private void Awake()
//    {
//        // 에디터에서 직접 연결 안 됐을 경우 자동 탐색
//        if (scoreManager == null)
//            scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
//    }

//    private void OnEnable()
//    {
//        KHS_Script_DumpManager.OnBallTrigger += BallDumpFunc;
//    }
//    private void OnDisable()
//    {
//        KHS_Script_DumpManager.OnBallTrigger -= BallDumpFunc;
//    }

//    private void BallDumpFunc(Collider _collider)
//    {
//        if (isSMActive)
//        {
//            Rigidbody ballRb = _collider.GetComponent<Rigidbody>();
//            if (ballRb != null)
//            {
//                StartCoroutine(SlotMachineSequence(ballRb));
//            }
//        }
//    }

//    private IEnumerator SlotMachineSequence(Rigidbody _rb)
//    {
//        isSMActive = false;

//        // 1. 입사각(속도 벡터)을 기억하고 공을 물리적으로 고정 후 숨김
//        Vector3 incomingVelocity = _rb.linearVelocity;
//        _rb.isKinematic = true;
//        _rb.gameObject.SetActive(false);


//        // 2. 슬롯머신 결과가 나올 때까지 대기 및 연출
//        float spinDuration = 4.0f; // 슬롯 회전 총 시간
//        float elapsed = 0f;

//        while (elapsed < spinDuration)
//        {
//            for (int i = 0; i < numberLists.Length; i++)
//            {
//                numberLists[i] = Random.Range(1,4); // 1~4 범위
//            }

//            // 디버그용으로 회전 중 상태 출력 (선택사항)
//            Debug.Log($"Spinning... {numberLists[0]} {numberLists[1]} {numberLists[2]}");

//            elapsed += 0.5f;
//            yield return new WaitForSeconds(0.5f);
//        }

//        // 최종 결과 확정
//        for (int i = 0; i < numberLists.Length; i++)
//        {
//            numberLists[i] = Random.Range(1, 4);
//        }

//        // 결과 출력
//        Debug.LogWarning($"슬롯머신 결과: {numberLists[0]} | {numberLists[1]} | {numberLists[2]}");

//        // 3. 점수 계산 및 전달
//        int score = CalculateSlotScore(numberLists[0], numberLists[1], numberLists[2]);
//        if (scoreManager != null)
//        {
//            scoreManager.AddScore(score);
//            Debug.Log($"▶ 획득 점수: {score}");
//        }
//        else
//        {
//            Debug.LogError("ScoreManager를 찾을 수 없습니다!");
//        }


//        // 4. 공의 모습과 물리 효과를 다시 활성화하고, 반사벡터 방향으로 발사
//        _rb.gameObject.SetActive(true);
//        _rb.isKinematic = false;
//        _rb.linearVelocity = - incomingVelocity;

//        // 5. 짧은 시간 후 슬롯머신을 다시 활성화 (공이 완전히 벗어날 시간 확보)
//        yield return new WaitForSeconds(0.5f);
//        isSMActive = true;

//    }
//    private int CalculateSlotScore(int a, int b, int c)
//    {
//        if (a == b && b == c)
//            return 5000;
//        else if (a == b || b == c || a == c)
//            return 1000;
//        else
//            return 500;
//    }
//}
using System.Collections;
using UnityEngine;

public class KHS_Script_SlotMachineController : MonoBehaviour
{
    [Header("슬롯머신 룰렛정보")]
    [Tooltip("각 릴(왼/중/오)순서대로")]
    [SerializeField] private GameObject[] gameObjects = new GameObject[3];
    [Tooltip("최종 숫자(디버그 확인용). 실행 중 자동 채움")]
    [SerializeField] private int[] numberLists = new int[3];

    [Header("스코어 매니저 참조")]
    [SerializeField] private KHS_Script_ScoreManager scoreManager;

    [Header("스핀/정지 설정")]
    [Tooltip("번호 1→2의 각도 증가량(도). 요구: 50도")]
    [SerializeField] private float stepAngle = 50f;
    [Tooltip("표시 가능한 최소/최대 번호(최대는 포함)")]
    [SerializeField] private int minNumber = 1;
    [SerializeField] private int maxNumberInclusive = 3;
    [Tooltip("회전 시작 속도(도/초) → 감속하며 정지")]
    [SerializeField] private float startSpeed = 1440f;
    [SerializeField] private float endSpeed = 180f;
    [Tooltip("스핀 연출 시간 / 목표 눈금으로 맞추는 시간")]
    [SerializeField] private float spinDuration = 1.2f;
    [SerializeField] private float settleDuration = 0.35f;
    [Tooltip("릴마다 멈추는 간격(순차 정지 느낌)")]
    [SerializeField] private float stopStagger = 0.15f;
    [Tooltip("숫자가 증가할 때 반대 방향으로 회전해야 하면 체크")]
    [SerializeField] private bool invertDirection = false;

    // 내부 상태
    private bool isSMActive = true;

    private void Awake()
    {
        if (scoreManager == null) scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
    }

    private void OnEnable()
    {
        KHS_Script_DumpManager.OnBallTrigger += BallDumpFunc;
    }
    private void OnDisable()
    {
        KHS_Script_DumpManager.OnBallTrigger -= BallDumpFunc;
    }

    private void BallDumpFunc(Collider col)
    {
        if (!isSMActive) return;
        var rb = col.attachedRigidbody;
        if (rb != null) StartCoroutine(SlotMachineSequence(rb));
    }

    private IEnumerator SlotMachineSequence(Rigidbody rb)
    {
        isSMActive = false;

        // 1) 공 잠시 숨김
        Vector3 incomingVelocity = rb.linearVelocity;
        rb.isKinematic = true;
        rb.gameObject.SetActive(false);

        // 2) 스핀 연출(릴 개별 회전: 본체/부모는 절대 회전 안 함)
        float t = 0f;
        float[] reelSpeed = new float[3] { startSpeed, startSpeed, startSpeed };

        while (t < spinDuration)
        {
            float k = Mathf.Clamp01(t / spinDuration);
            // 감속
            float curSpeed = Mathf.Lerp(startSpeed, endSpeed, k);

            for (int i = 0; i < gameObjects.Length; i++)
            {
                var tr = gameObjects[i].transform;
                float dir = invertDirection ? -1f : 1f;
                tr.Rotate(Vector3.right, dir * curSpeed * Time.deltaTime, Space.Self);
            }

            t += Time.deltaTime;
            yield return null;
        }

        // 3) 최종 숫자 결정
        for (int i = 0; i < numberLists.Length; i++)
        {
            numberLists[i] = Random.Range(minNumber, maxNumberInclusive + 1); // max 포함
        }
        Debug.LogWarning($"슬롯 결과: {numberLists[0]} | {numberLists[1]} | {numberLists[2]}");

        // 4) 각 릴을 해당 숫자의 '딱 맞는' X각도로 부드럽게 정렬
        for (int i = 0; i < gameObjects.Length; i++)
        {
            var tr = gameObjects[i].transform;

            float baseAngle = (numberLists[i] - minNumber) * stepAngle;
            if (invertDirection) baseAngle = -baseAngle;

            float curX = tr.localEulerAngles.x;
            float baseMod = Mathf.Repeat(baseAngle, 360f);
            float n = Mathf.Round((curX - baseMod) / 360f);
            float targetX = baseMod + 360f * n;

            yield return StartCoroutine(EaseLocalEulerX(tr, targetX, settleDuration));

            // 다음 릴 멈추기 전 약간의 간격
            if (stopStagger > 0f) yield return new WaitForSeconds(stopStagger);
        }

        // 5) 점수 처리
        int score = CalculateSlotScore(numberLists[0], numberLists[1], numberLists[2]);
        if (scoreManager != null) scoreManager.AddScore(score);

        // 6) 공 복귀
        rb.gameObject.SetActive(true);
        rb.isKinematic = false;
        rb.linearVelocity = -incomingVelocity;

        yield return new WaitForSeconds(0.5f);
        isSMActive = true;
    }

    private IEnumerator EaseLocalEulerX(Transform tr, float targetX, float duration)
    {
        float startX = tr.localEulerAngles.x;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float n = Mathf.Clamp01(elapsed / duration);
            float s = Mathf.SmoothStep(0f, 1f, n);
            float x = Mathf.LerpAngle(startX, targetX, s);

            Vector3 e = tr.localEulerAngles;
            e.x = x;
            tr.localEulerAngles = e;

            yield return null;
        }

        Vector3 fin = tr.localEulerAngles;
        fin.x = targetX;
        tr.localEulerAngles = fin;
    }

    private int CalculateSlotScore(int a, int b, int c)
    {
        if (a == b && b == c) return 5000;
        if (a == b || b == c || a == c) return 1000;
        return 500;
    }
}
