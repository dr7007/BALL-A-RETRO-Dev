using UnityEngine;

public class KHS_Script_CameraRoulette : MonoBehaviour
{
    [SerializeField] private Transform ball;  // 공 Transform
    [SerializeField] private float followDistance = 3f; // 공과의 거리 유지
    [SerializeField] private float followHeight = 1.0f; // 살짝 위로 띄우기
    [SerializeField] private float followSpeed = 5f;    // 부드럽게 이동

    private bool followInRoulette = true; // 룰렛 카메라 모드인지 여부

    void LateUpdate()
    {
        if (!followInRoulette || ball == null) return;

        // 현재 카메라가 바라보는 forward 방향
        Vector3 camForward = transform.forward;

        // 공이 바라보는 방향으로부터 일정 거리만큼 떨어진 목표 위치 계산
        Vector3 targetPos = ball.position - camForward * followDistance + Vector3.up * followHeight;

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // 카메라 회전은 고정 (transform.rotation 유지)
    }
}
