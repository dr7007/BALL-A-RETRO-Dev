using TMPro;
using UnityEngine;

public class CJS_TutorialDirector : MonoBehaviour
{
    public enum Step { Intro = 1, Flippers = 2, Entrance2F = 3, Plunger = 4, Done = 99 }

    [Header("Refs")]
    public CJS_TutorialCameraFocus focus;
    public TMP_Text tutorialText;

    [Header("Focus Targets")]
    public Transform focusFlippers;
    public Transform focus2FEntrance;
    public Transform focusPlunger;

    [Header("Input Keys (튜토리얼 표시용)")]
    public KeyCode leftFlipperKey = KeyCode.Z;
    public KeyCode rightFlipperKey = KeyCode.Slash;

    [Header("Optional Gates")]
    public KHS_Script_PlungerController plunger; // 여기에 연결(없으면 Find)
    public Collider railEntranceTrigger;         // 막고 싶으면 연결(선택)

    private Step _step = Step.Intro;
    private bool _leftDone, _rightDone;

    void OnEnable()
    {
        KHS_Script_PlungerController.OnBallLaunched += OnBallLaunched;
    }

    void OnDisable()
    {
        KHS_Script_PlungerController.OnBallLaunched -= OnBallLaunched;
    }

    void Start()
    {
        if (!focus) focus = FindObjectOfType<CJS_TutorialCameraFocus>(true);
        if (!plunger) plunger = FindObjectOfType<KHS_Script_PlungerController>(true);

        // Step4 전까지 플런저 잠금
        if (plunger) plunger.tutorialForceLock = true;

        // (선택) 튜토리얼 중엔 2F 입구 막기
        if (railEntranceTrigger) railEntranceTrigger.enabled = false;

        GoToStep(Step.Intro);
    }

    void Update()
    {
        switch (_step)
        {
            case Step.Intro:
                // 아무 키나 누르면 다음(또는 UI 버튼으로 바꿔도 됨)
                if (Input.anyKeyDown) GoToStep(Step.Flippers);
                break;

            case Step.Flippers:
                if (Input.GetKeyDown(leftFlipperKey)) _leftDone = true;
                if (Input.GetKeyDown(rightFlipperKey)) _rightDone = true;

                if (_leftDone && _rightDone)
                    GoToStep(Step.Entrance2F);
                break;

            case Step.Entrance2F:
                // 여기서는 "설명만" 하고, 플레이어가 확인 키를 누르면 다음
                // (원하면 여기서 '장애물 5개 맞추기' 실제 카운트 방식으로 바꿔줄 수 있어)
                if (Input.anyKeyDown) GoToStep(Step.Plunger);
                break;

            case Step.Plunger:
                // Space로 발사하면 OnBallLaunched 이벤트로 완료 처리됨
                break;
        }
    }

    private void GoToStep(Step next)
    {
        _step = next;

        if (!focus) return;

        switch (_step)
        {
            case Step.Intro:
                focus.CaptureOriginal();
                focus.Restore();
                if (tutorialText)
                    tutorialText.text =
                        "?? 게임 설명\n" +
                        "핀볼(1F)에서 점수를 올리고, 조건을 달성하면 2F로 진입할 수 있어요.\n" +
                        "아무 키나 눌러 계속…";
                break;

            case Step.Flippers:
                _leftDone = _rightDone = false;
                if (focusFlippers) focus.FocusTo(focusFlippers);

                if (tutorialText)
                    tutorialText.text =
                        "?? 플리퍼 조작\n" +
                        $"왼쪽: [{leftFlipperKey}] / 오른쪽: [{rightFlipperKey}]\n" +
                        "두 키를 한 번씩 눌러보세요!";
                break;

            case Step.Entrance2F:
                if (focus2FEntrance) focus.FocusTo(focus2FEntrance);

                if (tutorialText)
                    tutorialText.text =
                        "?? 2층 입장\n" +
                        "장애물 5개를 맞추면 2F 입장이 가능해져요.\n" +
                        "아무 키나 눌러 계속…";
                break;

            case Step.Plunger:
                if (focusPlunger) focus.FocusTo(focusPlunger);

                // Step4에서 플런저 잠금 해제
                if (plunger) plunger.tutorialForceLock = false;

                if (tutorialText)
                    tutorialText.text =
                        "?? 공 발사\n" +
                        "Space를 눌러 공을 발사하면 튜토리얼이 시작됩니다!";
                break;

            case Step.Done:
                EndTutorial();
                break;
        }
    }

    private void OnBallLaunched()
    {
        if (_step != Step.Plunger) return;
        GoToStep(Step.Done);
    }

    private void EndTutorial()
    {
        // (선택) 2F 입구 다시 열기
        if (railEntranceTrigger) railEntranceTrigger.enabled = true;

        // 카메라 원복 + 비활성 스크립트 다시 켜기
        if (focus)
        {
            focus.Restore();
            focus.ReEnableDisabled();
        }

        if (tutorialText) tutorialText.text = ""; // 또는 패널 끄기
        Debug.Log("[Tutorial] Done");
    }
}
