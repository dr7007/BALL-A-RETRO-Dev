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
    [Tooltip("2F 진입 타임라인인지 체크")]
    public bool is2FEntrance = false;

    private YJ_Script_BallController capturedBall = null; // 어떤 공을 태웠는지 기억
    private bool isHoleClosed = false; // 2층 플랫폼으로 갈 수 있는지에 대한 여부

    private void OnEnable()
    {
        KHS_Script_BatteryLedManager.HoleCoverActiveEvt += HoleActive;
        KHS_Script_BatteryLedManager.HoleCoverUnActiveEvt += HoleUnActive;
    }
    private void OnDisable()
    {
        KHS_Script_BatteryLedManager.HoleCoverActiveEvt -= HoleActive;
        KHS_Script_BatteryLedManager.HoleCoverUnActiveEvt -= HoleUnActive;
    }

    private void HoleActive() => isHoleClosed = true;
    private void HoleUnActive() => isHoleClosed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;
        var ball = other.GetComponent<YJ_Script_BallController>();
        if (!ball) return;

        mainCam.cullingMask |= (1 << LayerMask.NameToLayer("2F"));

        capturedBall = ball;
        ball.CaptureAndParent(railMover);

        railTimeline.stopped += OnRailRideEnd;

        // ★ 레일 루프 시작
        CJS_Script_AudioDirector.I?.PlayRailRideLoop();

        railTimeline.Play();

        if (oneTimeUse)
            GetComponent<Collider>().enabled = false;
    }

    private void OnRailRideEnd(PlayableDirector director)
    {
        railTimeline.stopped -= OnRailRideEnd;

        // ★ 레일 루프 정지
        CJS_Script_AudioDirector.I?.StopRailRideLoop();

        if (capturedBall != null)
        {
            capturedBall.ReleaseForFalling(Vector3.zero, false);
            capturedBall = null;
        }

        if (isHoleClosed && camSwitch && is2FEntrance)
        {
            camSwitch.ToMaze();
        }
    }



}
