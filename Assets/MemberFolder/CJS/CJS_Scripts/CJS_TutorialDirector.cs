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
    public KHS_Script_PlungerController plunger; 
    public Collider railEntranceTrigger;         

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

        if (railEntranceTrigger) railEntranceTrigger.enabled = false;

        GoToStep(Step.Intro);
    }

    void Update()
    {
        switch (_step)
        {
            case Step.Intro:
                if (Input.anyKeyDown) GoToStep(Step.Flippers);
                break;

            case Step.Flippers:
                if (Input.GetKeyDown(leftFlipperKey)) _leftDone = true;
                if (Input.GetKeyDown(rightFlipperKey)) _rightDone = true;

                if (_leftDone && _rightDone)
                    GoToStep(Step.Entrance2F);
                break;

            case Step.Entrance2F:
                if (Input.anyKeyDown) GoToStep(Step.Plunger);
                break;

            case Step.Plunger:
                // Space 발사는 OnBallLaunched 이벤트로 완료 처리
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
                        "게임 설명\n" +
                        "핀볼(1F)에서 점수를 올리고, 조건을 달성하면 2F로 진입할 수 있어요.\n" +
                        "아무 키나 눌러 계속…";
                break;

            case Step.Flippers:
                _leftDone = _rightDone = false;

                if (focusFlippers) focus.FocusTo(focusFlippers);

                if (tutorialText)
                    tutorialText.text = $"( {leftFlipperKey} / {rightFlipperKey} 를 누르세요! )";
                break;

            case Step.Entrance2F:
                if (focus2FEntrance) focus.FocusTo(focus2FEntrance);

                if (tutorialText)
                    tutorialText.text =
                        "( 장애물 5개를 맞추면 2층 입장 가능! )\n" +
                        "아무 키나 눌러 계속…";
                break;

            case Step.Plunger:
                if (focusPlunger) focus.FocusTo(focusPlunger);

                // Step4에서 플런저 잠금 해제
                if (plunger) plunger.tutorialForceLock = false;

                if (tutorialText)
                    tutorialText.text = "( Space 를 눌러 공을 발사하세요! )";
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
        if (railEntranceTrigger) railEntranceTrigger.enabled = true;

        // 카메라 원복 + 비활성 스크립트 다시 켜기
        if (focus)
        {
            focus.Restore();
            focus.ReEnableDisabled();
        }

        if (tutorialText) tutorialText.text = "";
        Debug.Log("[Tutorial] Done");
    }
}
