using UnityEngine;

public class KHS_Script_MovingObject : MonoBehaviour
{
    
    [Header("이동 경계 설정 (로컬 기준)")]
    [SerializeField] private Vector3 leftBoundary = Vector3.zero;
    [SerializeField] private Vector3 rightBoundary = Vector3.zero;

    [Header("한쪽 방향 이동에 걸리는 시간")]
    [SerializeField] private float duration = 2f;

    [Header("이동 속도 곡선 (0~1 범위)")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float timer = 0f;
    private Vector3 initialLocalPos;

    void Start()
    {
        initialLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (duration <= 0f) return;

        timer += Time.deltaTime / duration;
        float pingpongT = Mathf.PingPong(timer, 1f);
        float curvedT = moveCurve.Evaluate(pingpongT);

        transform.localPosition = initialLocalPos + Vector3.Lerp(leftBoundary, rightBoundary, curvedT);
    }
}