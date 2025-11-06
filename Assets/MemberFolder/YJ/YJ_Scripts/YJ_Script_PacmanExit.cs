using System;
using UnityEngine;
using UnityEngine.Playables;

// 2층 출구 레일 전용 스크립트
public class YJ_Script_PacManExit : MonoBehaviour
{
    public static event Action OnPacManExit;

    [Header("연결")]
    public Transform railMover;
    public PlayableDirector railTimeline;

    [Header("카메라 설정")]
    public Camera mainCam;

    private YJ_Script_BallController capturedBall = null;
    private Collider triggerCollider; // 리셋을 위해 콜라이더 저장

    // --- (신규) 리셋 로직 ---
    private void Start()
    {
        triggerCollider = GetComponent<Collider>();
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
                // 1. 팩맨 모드를 핀볼 모드로 되돌림
                ball.SetControlMode(YJ_Script_BallController.ControlMode.Pinball);
                OnPacManExit?.Invoke();

                // 2. 공 캡처
                capturedBall = ball;
                ball.CaptureAndParent(railMover);

                // 3. 타임라인 재생
                railTimeline.Play();
                railTimeline.stopped += OnRailRideEnd;

                // 4. 트리거 비활성화 (BallOut 시 리셋됨)
                if (triggerCollider != null)
                {
                    //triggerCollider.enabled = false;
                }
            }
        }
    }

    private void OnRailRideEnd(PlayableDirector director)
    {
        if (capturedBall != null)
        {
            // 1. 공을 '자유 낙하' 모드로 전환
            capturedBall.ReleaseForFalling();
            capturedBall = null; // 공 참조 해제

            // 2. --- (중요) ---
            // 타임라인이 끝나고 공이 낙하를 시작하는 *이 순간*에 2층을 숨깁니다.
            if (mainCam != null)
            {
                mainCam.cullingMask &= ~(1 << LayerMask.NameToLayer("2F"));
                Debug.Log("2F 레이어 컬링 마스크 제거 (숨김)");
            }

            // 3. 이벤트 리스너 해제 (중요)
            railTimeline.stopped -= OnRailRideEnd;
        }
    }
}