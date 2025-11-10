using System;
using UnityEngine;
using UnityEngine.Playables;

// 2층 출구 레일 전용 스크립트
public class YJ_Script_RouletteEntrance : MonoBehaviour
{
    public static event Action OnPacManExit;

    [Header("연결")]
    public Transform railMover;
    public PlayableDirector railTimeline;

    [Header("카메라 설정")]
    public Camera mainCam;

    private YJ_Script_BallController capturedBall = null;
    private Collider triggerCollider; // 리셋을 위해 콜라이더 저장

    private bool isTrackingVelocity = false;
    private Vector3 lastMoverPosition;
    private Vector3 currentMoverVelocity; // 타임라인의 현재 속도를 저장

    private void Start()
    {
        triggerCollider = GetComponent<Collider>();
    }

    private void LateUpdate()
    {
        // 타임라인이 재생 중일 때만 속도를 추적
        if (isTrackingVelocity && railMover != null)
        {
            // (현재 위치 - 이전 위치) / 시간 = 속도
            Vector3 deltaPosition = railMover.position - lastMoverPosition;

            if (Time.deltaTime > 0f) // 0으로 나누기 방지
            {
                currentMoverVelocity = deltaPosition / Time.deltaTime;
            }

            // 다음 프레임 계산을 위해 현재 위치를 '이전 위치'로 저장
            lastMoverPosition = railMover.position;
        }
    }

    private void OnEnable()
    {
        // 공이 아웃되면 트리거를 다시 활성화
        KHS_Script_BallOutController.BallOutEvt += ResetTrigger;
    }

    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= ResetTrigger;
    }

    private void ResetTrigger()
    {
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 중복 실행 방지
        if (other.CompareTag("Ball") && capturedBall == null)
        {
            YJ_Script_BallController ball = other.GetComponent<YJ_Script_BallController>();
            if (ball != null)
            {
                // 1. 공 캡처
                capturedBall = ball;
                ball.CaptureAndParent(railMover);

                // 2. 속도 추적 시작
                lastMoverPosition = railMover.position; // 추적 시작 위치 초기화
                currentMoverVelocity = Vector3.zero;
                isTrackingVelocity = true;

                // 3. 타임라인 재생
                railTimeline.Play();
                railTimeline.stopped += OnRailRideEnd;
            }
        }
    }

    private void OnRailRideEnd(PlayableDirector director)
    {
        isTrackingVelocity = false;

        if (capturedBall != null)
        {
            // 1. 공을 '자유 낙하' 모드로 전환 (계산된 속도 전달)
            capturedBall.ReleaseForFalling(currentMoverVelocity, true);
            capturedBall = null; // 공 참조 해제

            // 2. 2층 카메라 숨김
            if (mainCam != null)
            {
                mainCam.cullingMask &= ~(1 << LayerMask.NameToLayer("2F"));
                Debug.Log("2F 레이어 컬링 마스크 제거 (숨김)");
            }

            // 3. 이벤트 리스너 해제
            railTimeline.stopped -= OnRailRideEnd;
        }
    }
}