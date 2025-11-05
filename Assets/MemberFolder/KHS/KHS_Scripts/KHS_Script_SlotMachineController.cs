//using System.Collections;
//using UnityEngine;

//public class KHS_Script_SlotMachineController : MonoBehaviour
//{
//    [Header("슬롯머신 룰렛정보")]
//    [Tooltip("각 릴(왼/중/오)순서대로")]
//    [SerializeField] private GameObject[] gameObjects = new GameObject[3];
//    [Tooltip("최종 숫자(디버그 확인용). 실행 중 자동 채움")]
//    [SerializeField] private int[] numberLists = new int[3];

//    [Header("스코어 매니저 참조")]
//    [SerializeField] private KHS_Script_ScoreManager scoreManager;

//    [Header("스핀/정지 설정")]
//    [Tooltip("번호 1→2의 각도 증가량(도). 요구: 50도")]
//    [SerializeField] private float stepAngle = 50f;
//    [Tooltip("표시 가능한 최소/최대 번호(최대는 포함)")]
//    [SerializeField] private int minNumber = 1;
//    [SerializeField] private int maxNumberInclusive = 3;
//    [Tooltip("회전 시작 속도(도/초) → 감속하며 정지")]
//    [SerializeField] private float startSpeed = 1440f;
//    [SerializeField] private float endSpeed = 180f;
//    [Tooltip("스핀 연출 시간 / 목표 눈금으로 맞추는 시간")]
//    [SerializeField] private float spinDuration = 1.2f;
//    [SerializeField] private float settleDuration = 0.35f;
//    [Tooltip("릴마다 멈추는 간격(순차 정지 느낌)")]
//    [SerializeField] private float stopStagger = 0.15f;
//    [Tooltip("숫자가 증가할 때 반대 방향으로 회전해야 하면 체크")]
//    [SerializeField] private bool invertDirection = false;

//    // 내부 상태
//    private bool isSMActive = true;

//    private void Awake()
//    {
//        if (scoreManager == null) scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
//    }

//    private void OnEnable()
//    {
//        KHS_Script_DumpManager.OnBallTrigger += BallDumpFunc;
//    }
//    private void OnDisable()
//    {
//        KHS_Script_DumpManager.OnBallTrigger -= BallDumpFunc;
//    }

//    private void BallDumpFunc(Collider col)
//    {
//        if (!isSMActive) return;
//        var rb = col.attachedRigidbody;
//        if (rb != null) StartCoroutine(SlotMachineSequence(rb));
//    }

//    private IEnumerator SlotMachineSequence(Rigidbody rb)
//    {
//        isSMActive = false;

//        // 1) 공 잠시 숨김
//        Vector3 incomingVelocity = rb.linearVelocity;
//        rb.isKinematic = true;
//        rb.gameObject.SetActive(false);

//        // 2) 스핀 연출(릴 개별 회전: 본체/부모는 절대 회전 안 함)
//        float t = 0f;
//        float[] reelSpeed = new float[3] { startSpeed, startSpeed, startSpeed };

//        while (t < spinDuration)
//        {
//            float k = Mathf.Clamp01(t / spinDuration);
//            // 감속
//            float curSpeed = Mathf.Lerp(startSpeed, endSpeed, k);

//            for (int i = 0; i < gameObjects.Length; i++)
//            {
//                var tr = gameObjects[i].transform;
//                float dir = invertDirection ? -1f : 1f;
//                tr.Rotate(Vector3.right, dir * curSpeed * Time.deltaTime, Space.Self);
//            }

//            t += Time.deltaTime;
//            yield return null;
//        }

//        // 3) 최종 숫자 결정
//        for (int i = 0; i < numberLists.Length; i++)
//        {
//            numberLists[i] = Random.Range(minNumber, maxNumberInclusive + 1); // max 포함
//        }
//        Debug.LogWarning($"슬롯 결과: {numberLists[0]} | {numberLists[1]} | {numberLists[2]}");

//        // 4) 각 릴을 해당 숫자의 '딱 맞는' X각도로 부드럽게 정렬
//        for (int i = 0; i < gameObjects.Length; i++)
//        {
//            var tr = gameObjects[i].transform;

//            float baseAngle = (numberLists[i] - minNumber) * stepAngle;
//            if (invertDirection) baseAngle = -baseAngle;

//            float curX = tr.localEulerAngles.x;
//            float baseMod = Mathf.Repeat(baseAngle, 360f);
//            float n = Mathf.Round((curX - baseMod) / 360f);
//            float targetX = baseMod + 360f * n;

//            yield return StartCoroutine(EaseLocalEulerX(tr, targetX, settleDuration));

//            // 다음 릴 멈추기 전 약간의 간격
//            if (stopStagger > 0f) yield return new WaitForSeconds(stopStagger);
//        }

//        // 5) 점수 처리
//        int score = CalculateSlotScore(numberLists[0], numberLists[1], numberLists[2]);
//        if (scoreManager != null) scoreManager.AddScore(score);

//        // 6) 공 복귀
//        rb.gameObject.SetActive(true);
//        rb.isKinematic = false;
//        rb.linearVelocity = -incomingVelocity;

//        yield return new WaitForSeconds(0.5f);
//        isSMActive = true;
//    }

//    private IEnumerator EaseLocalEulerX(Transform tr, float targetX, float duration)
//    {
//        float startX = tr.localEulerAngles.x;
//        float elapsed = 0f;

//        while (elapsed < duration)
//        {
//            elapsed += Time.deltaTime;
//            float n = Mathf.Clamp01(elapsed / duration);
//            float s = Mathf.SmoothStep(0f, 1f, n);
//            float x = Mathf.LerpAngle(startX, targetX, s);

//            Vector3 e = tr.localEulerAngles;
//            e.x = x;
//            tr.localEulerAngles = e;

//            yield return null;
//        }

//        Vector3 fin = tr.localEulerAngles;
//        fin.x = targetX;
//        tr.localEulerAngles = fin;
//    }

//    private int CalculateSlotScore(int a, int b, int c)
//    {
//        if (a == b && b == c) return 5000;
//        if (a == b || b == c || a == c) return 1000;
//        return 500;
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

        // 2) 프롤로그 스핀(모든 릴 회전)
        float t = 0f;
        while (t < spinDuration)
        {
            float k = Mathf.Clamp01(t / spinDuration);
            float curSpeed = Mathf.Lerp(startSpeed, endSpeed, k);
            SpinAll(curSpeed);
            t += Time.deltaTime;
            yield return null;
        }

        // 3) 순차 정지: 0 → 1 → 2
        for (int i = 0; i < gameObjects.Length; i++)
        {
            // 이번 릴의 결과만 즉시 확정(다른 릴은 계속 회전)
            numberLists[i] = Random.Range(minNumber, maxNumberInclusive + 1);

            // 목표 각도 계산 (가장 가까운 눈금으로 스냅)
            Transform tr = gameObjects[i].transform;
            float baseAngle = (numberLists[i] - minNumber) * stepAngle;
            if (invertDirection) baseAngle = -baseAngle;

            float curX = tr.localEulerAngles.x;
            float baseMod = Mathf.Repeat(baseAngle, 360f);
            float n = Mathf.Round((curX - baseMod) / 360f);
            float targetX = baseMod + 360f * n;

            // 이 릴은 target으로 부드럽게 정렬,
            // 뒤의 릴(j > i)은 계속 회전
            float elapsed = 0f;
            float startX = curX;

            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / settleDuration));

                // i번 릴 정렬
                float x = Mathf.LerpAngle(startX, targetX, s);
                Vector3 e = tr.localEulerAngles; e.x = x; tr.localEulerAngles = e;

                // 뒤 릴은 계속 스핀
                float spinSpd = Mathf.Lerp(endSpeed, endSpeed * 0.8f, s); // 살짝 감속 느낌
                SpinRange(i + 1, gameObjects.Length - 1, spinSpd);

                yield return null;
            }
            // 스냅 보정
            Vector3 fin = tr.localEulerAngles; fin.x = targetX; tr.localEulerAngles = fin;

            // 다음 릴 정지 전, 짧은 기다림 동안 뒤 릴만 스핀
            if (stopStagger > 0f)
            {
                float gap = 0f;
                while (gap < stopStagger)
                {
                    gap += Time.deltaTime;
                    SpinRange(i + 1, gameObjects.Length - 1, endSpeed);
                    yield return null;
                }
            }
        }

        Debug.LogWarning($"슬롯 결과: {numberLists[0]} | {numberLists[1]} | {numberLists[2]}");

        // 4) 점수 처리
        int score = CalculateSlotScore(numberLists[0], numberLists[1], numberLists[2]);
        if (scoreManager != null) scoreManager.AddScore(score);

        // 5) 공 복귀
        rb.gameObject.SetActive(true);
        rb.isKinematic = false;
        rb.linearVelocity = -incomingVelocity;

        yield return new WaitForSeconds(0.5f);
        isSMActive = true;
    }

    private void SpinAll(float speed)
    {
        float dir = invertDirection ? -1f : 1f;
        for (int i = 0; i < gameObjects.Length; i++)
            gameObjects[i].transform.Rotate(Vector3.right, dir * speed * Time.deltaTime, Space.Self);
    }

    private void SpinRange(int fromInclusive, int toInclusive, float speed)
    {
        if (fromInclusive > toInclusive) return;
        float dir = invertDirection ? -1f : 1f;
        for (int i = fromInclusive; i <= toInclusive; i++)
            gameObjects[i].transform.Rotate(Vector3.right, dir * speed * Time.deltaTime, Space.Self);
    }

    private int CalculateSlotScore(int a, int b, int c)
    {
        if (a == b && b == c) return 5000;
        if (a == b || b == c || a == c) return 1000;
        return 500;
    }
}
