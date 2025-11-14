using UnityEngine;

public class KHS_Script_MovingObject : MonoBehaviour
{
    [Header("이동 거리 설정")]
    [SerializeField] private float distance = 1f;

    [Header("한쪽 방향 이동에 걸리는 시간")]
    [SerializeField] private float duration = 2f;

    [Header("이동 속도 곡선 (0~1 범위)")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float timer = 0f;
    private Vector3 initialLocalPos;

    void Start()
    {
        initialLocalPos = transform.localPosition;   // 로컬 기준
    }

    void Update()
    {
        if (duration <= 0f) return;

        timer += Time.deltaTime / duration;

        float t = Mathf.PingPong(timer, 1f);     // 0~1 왕복
        float curvedT = moveCurve.Evaluate(t);   // AnimationCurve 반영

        // 좌 → 우 이동 거리 계산 (-distance ~ +distance)
        float offset = Mathf.Lerp(-distance, distance, curvedT);

        // 로컬 좌표로 적용
        transform.localPosition = initialLocalPos + transform.forward * offset;
    }
}
