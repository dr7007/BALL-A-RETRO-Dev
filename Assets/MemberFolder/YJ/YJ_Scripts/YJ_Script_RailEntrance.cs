// RailEntrance.cs (레일 입구 트리거)
using System;
using UnityEngine;
using UnityEngine.Playables;

public class YJ_Script_RailEntrance : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("애니메이션될 'RailMover' 오브젝트의 Transform")]
    public Transform railMover; // 2단계에서 만든 'RailMover' 연결

    [Tooltip("RailMover에 붙어있는 Playable Director")]
    public PlayableDirector railTimeline; // 'RailMover' 연결
    
    [Header("카메라 설정")]
    [Tooltip("2층 레이어 관리를 위한 카메라")]
    public Camera mainCam;

    [Header("설정")]
    [Tooltip("한 번 작동하면 비활성화")]
    public bool oneTimeUse = true;

    [Header("카메라 전환")]
    public CJS_Script_CameraSwitcher camSwitch; // ★ 새로 추가

    private YJ_Script_BallController capturedBall = null; // 어떤 공을 태웠는지 기억

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;

        var ball = other.GetComponent<YJ_Script_BallController>();
        if (!ball) return;

        mainCam.cullingMask |= (1 << LayerMask.NameToLayer("2F"));   // mainCam의 cullingMask에 2F 레이어 추가

        // 1) 공을 레일에 태움
        capturedBall = ball;
        ball.CaptureAndParent(railMover);

        // 2) 타임라인 재생 & 종료 콜백
        railTimeline.stopped += OnRailRideEnd;
        railTimeline.Play();

        if (oneTimeUse)
            GetComponent<Collider>().enabled = false;
    }

    // 3) 타임라인 종료 시
    private void OnRailRideEnd(PlayableDirector director)
    {
        railTimeline.stopped -= OnRailRideEnd;

        if (capturedBall != null)
        {
            if (camSwitch) camSwitch.ToMain();
            // 5. 공을 '기차'에서 내려서 낙하하게 함
            capturedBall.ReleaseForFalling(Vector3.zero, false);

            capturedBall = null;
        }

        // 레일 주행 끝 = 2층 미로 시작 → 탑뷰 카메라로 스위치
        if (camSwitch) camSwitch.ToMaze();
    }
}
