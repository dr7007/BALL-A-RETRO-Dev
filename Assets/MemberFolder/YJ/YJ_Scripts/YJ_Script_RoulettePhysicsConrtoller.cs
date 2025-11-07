using System; // 1. Action을 사용하기 위해 추가
using System.Collections;
using UnityEngine;

// 'Roulette_Body'(숫자판) 오브젝트에 부착
public class YJ_Script_RoulettePhysicsController : MonoBehaviour
{
    [Header("룰렛 값 설정")]
    [Tooltip("각 칸에 해당하는 실제 값. 0번 인덱스부터 시계방향으로 입력.")]
    public int[] slotValues; // 예: { 0, 32, 19, ... }
    [Tooltip("룰렛의 '0번 인덱스' 값이 시작되는 각도 (3시 방향=0)")]
    public float startAngleOffset = 0f;

    [Header("회전 설정")]
    [Tooltip("초기 상태: 룰렛이 도는 속도 (초당 각도)")]
    public float constantSpinSpeed = -90f;
    [Tooltip("정지 시퀀스가 시작되고 완전히 멈추는 데 걸리는 시간")]
    public float stopDuration = 5f;

    private YJ_Script_BallController capturedBall;
    private bool isSlowingDown = false;
    private Collider landTrigger; // 착지 트리거 (리셋용)

    public static event Action<int> OnRouletteResult; // 점수 계산기에 보낼 이벤트

    private void Start()
    {
        // 착지 트리거(Roulette_Land_Trigger)를 찾아 저장
        landTrigger = GetComponentInChildren<YJ_Script_RouletteLandTrigger>().GetComponent<Collider>();
    }

    // --- 1. BallOutEvt 구독 ---
    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += ResetRoulette;
    }
    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= ResetRoulette;
    }

    // --- 2. 룰렛 리셋 (공 아웃 시) ---
    public void ResetRoulette()
    {
        StopAllCoroutines(); // 정지 시퀀스 중단
        isSlowingDown = false; // 다시 돌기 시작

        // 착지 트리거(Land Trigger) 다시 활성화
        if (landTrigger != null)
        {
            landTrigger.enabled = true;
        }

        // 룰렛에 공이 갇혀있었다면 강제로 릴리즈
        if (capturedBall != null)
        {
            capturedBall.ReleaseForFalling(Vector3.zero); // 자유 낙하 시킴
            capturedBall = null;
        }
    }

    // --- 3. 평소 상태 (요청사항 3번: 일정 속도로 회전) ---
    void Update()
    {
        // 정지 시퀀스가 아닐 때만 계속 회전
        if (!isSlowingDown)
        {
            transform.Rotate(Vector3.up, constantSpinSpeed * Time.deltaTime, Space.World);
        }
    }

    // --- 4. 정지 시퀀스 (요청사항 4, 5, 6번) ---
    // (Roulette_Land_Trigger가 이 함수를 호출)
    public void StartStopSequence(YJ_Script_BallController ball)
    {
        if (isSlowingDown) return; // 이미 정지 시퀀스 중이면 무시

        isSlowingDown = true;
        capturedBall = ball;

        // 1. 공을 캡처해서 룰렛판에 고정(자식으로 만듦)
        // (공은 물리 운동을 멈추고 룰렛판을 따라감)
        ball.CaptureAndParent(this.transform);

        // 2. 룰렛판을 서서히 멈추는 코루틴 시작
        StartCoroutine(SlowDownAndStop());
    }

    // 5. 서서히 멈추는 코루틴
    private IEnumerator SlowDownAndStop()
    {
        float timer = 0f;
        float startSpeed = constantSpinSpeed;

        while (timer < stopDuration)
        {
            timer += Time.deltaTime;

            // Ease-Out 효과 (점점 느려지게)
            float t = timer / stopDuration;
            t = 1 - (1 - t) * (1 - t); // (t-1)^2 -> 1 - (1-t)^2 (EaseOut Quad)

            float currentSpeed = Mathf.Lerp(startSpeed, 0f, t);

            // 룰렛판을 감속하며 회전
            transform.Rotate(Vector3.up, currentSpeed * Time.deltaTime, Space.World);

            yield return null;
        }

        // 룰렛이 완전히 멈춤
        CalculateResult();
    }

    // --- 6. 숫자 인식 및 로그 (요청사항 7번) ---
    private void CalculateResult()
    {
        if (capturedBall == null) return;

        // 1. 공과 룰렛 중심 사이의 방향 벡터 계산
        Vector3 direction = capturedBall.transform.position - this.transform.position;

        // 2. X, Z 평면의 각도 계산 (Atan2 사용, 3시 방향이 0도)
        float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;

        // 3. 0~360도 범위로 변환
        if (angle < 0) angle += 360f;

        // 4. 칸 인덱스 계산
        int numberOfSlots = slotValues.Length;
        if (numberOfSlots == 0)
        {
            Debug.LogError("SlotValues 배열이 비어있습니다!");
            return;
        }

        float sliceAngle = 360f / numberOfSlots;

        // 'startAngleOffset'이 칸의 '중앙'을 가리키므로,
        // 각도 계산의 기준점을 '반 칸' 뒤로(시계방향으로) 이동시킵니다.
        float halfSlice = sliceAngle / 2.0f;

        // 5. 텍스처 시작점(중앙) 오프셋 및 '반 칸' 오프셋 적용
        angle = (angle - startAngleOffset - halfSlice + 360f) % 360f;
        // (참고: 360을 더하고 % 연산을 하는 것은 각도가 음수가 되는 것을 방지합니다)
        // --- (수정 끝) ---

        int stoppedSlotIndex = Mathf.FloorToInt(angle / sliceAngle);

        // 6. 인덱스로 실제 값 조회
        int resultNumber = slotValues[stoppedSlotIndex];

        // 7. 로그 띄우기 및 이벤트 방송
        Debug.Log($"--- 룰렛 결과: {resultNumber} ---");
        OnRouletteResult?.Invoke(resultNumber);

        // (이후 공을 다시 튕겨내는 로직을 여기에 추가할 수 있습니다)
        // 예: StartCoroutine(ReleaseBallAfterDelay(2f));
    }
}