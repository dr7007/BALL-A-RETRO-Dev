// RailEntrance.cs (레일 입구의 'Is Trigger'가 켜진 콜라이더에 부착)

using System;
using UnityEngine;
using UnityEngine.Playables; // Timeline을 제어하기 위해 필요

public class YJ_Script_RailEntrance : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("애니메이션될 'RailMover' 오브젝트의 Transform")]
    public Transform railMover; // 2단계에서 만든 'RailMover' 연결

    [Tooltip("RailMover에 붙어있는 Playable Director")]
    public PlayableDirector railTimeline; // 'RailMover' 연결

    [Header("설정")]
    [Tooltip("한 번 작동하면 비활성화")]
    public bool oneTimeUse = true;

    [Header("카메라 설정")]
    [Tooltip("2층 레이어 관리를 위한 카메라")]
    public Camera mainCam;

    private YJ_Script_BallController capturedBall = null; // 어떤 공을 태웠는지 기억

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            YJ_Script_BallController ball = other.GetComponent<YJ_Script_BallController>();
            if (ball != null)
            {
                // 1. 공을 캡처해서 'RailMover'의 자식으로 만듦
                capturedBall = ball; // 공을 기억
                ball.CaptureAndParent(railMover);

                mainCam.cullingMask |= (1 << LayerMask.NameToLayer("2F"));   // mainCam의 cullingMask에 2F 레이어 추가
                // 2. Timeline 애니메이션 재생
                railTimeline.Play();

                // 3. (선택) Timeline 종료 시 호출할 함수를 연결
                // (Timeline이 끝나면 OnRailRideEnd 함수를 호출하도록 예약)
                railTimeline.stopped += OnRailRideEnd;

                if (oneTimeUse)
                {
                    GetComponent<Collider>().enabled = false; // 트리거 비활성화
                }
            }
        }
    }

    // 4. Timeline 재생이 'stopped' 되었을 때 호출되는 함수
    private void OnRailRideEnd(PlayableDirector director)
    {
        if (capturedBall != null)
        {
            // 5. 공을 '기차'에서 내려서 낙하하게 함
            capturedBall.ReleaseForFalling(Vector3.zero);
            capturedBall = null;

            // 6. 이벤트 리스너 해제 (중요)
            railTimeline.stopped -= OnRailRideEnd;
        }
    }
}