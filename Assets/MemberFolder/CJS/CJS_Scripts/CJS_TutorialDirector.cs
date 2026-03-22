using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CJS_TutorialDirector : MonoBehaviour
{
    public enum Step
    {
        Intro = 1,
        Flippers = 2,
        Entrance2F = 3,
        Plunger = 4,
        ScoreMission = 5,
        Done = 99,
        Failed = 100
    }

    [Header("Refs")]
    public CJS_TutorialCameraFocus focus;
    public TMP_Text tutorialText;
    public KHS_Script_ScoreManager scoreManager;

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

    [Header("Tutorial Score Goal")]
    public int tutorialTargetScore = 1000;

    [Header("Result UI")]
    public GameObject resultPanel;
    public TMP_Text resultTitleText;
    public TMP_Text resultDescText;
    public Button retryButton;
    public Button goLobbyButton;
    public Button goGameButton;

    [Header("Scene Names")]
    public string tutorialSceneName = "CJS_Scene_Tutorial";
    public string lobbySceneName = "Main_Scene_Lobby";
    public string gameSceneName = "Main_Scene_Game";

    private Step _step = Step.Intro;
    private bool _leftDone;
    private bool _rightDone;
    private bool _resultShown;
    private bool _scoreMissionActive;

    void OnEnable()
    {
        KHS_Script_PlungerController.OnBallLaunched += OnBallLaunched;
        KHS_Script_ScoreManager.OnGameOverWithScore += OnGameOverWithScore;
        KHS_Script_ScoreManager.OnGameClearWithScore += OnGameClearWithScore;
    }

    void OnDisable()
    {
        KHS_Script_PlungerController.OnBallLaunched -= OnBallLaunched;
        KHS_Script_ScoreManager.OnGameOverWithScore -= OnGameOverWithScore;
        KHS_Script_ScoreManager.OnGameClearWithScore -= OnGameClearWithScore;
    }

    void Start()
    {
        if (!focus) focus = FindObjectOfType<CJS_TutorialCameraFocus>(true);
        if (!plunger) plunger = FindObjectOfType<KHS_Script_PlungerController>(true);
        if (!scoreManager) scoreManager = FindObjectOfType<KHS_Script_ScoreManager>(true);

        if (resultPanel) resultPanel.SetActive(false);

        BindButtons();

        // Step4 전까지 플런저 잠금
        if (plunger) plunger.tutorialForceLock = true;

        // 튜토리얼 진행 중 2층 입구 막기
        if (railEntranceTrigger) railEntranceTrigger.enabled = false;

        GoToStep(Step.Intro);
    }

    void Update()
    {
        if (_resultShown) return;

        switch (_step)
        {
            case Step.Intro:
                if (Input.anyKeyDown)
                    GoToStep(Step.Flippers);
                break;

            case Step.Flippers:
                if (Input.GetKeyDown(leftFlipperKey)) _leftDone = true;
                if (Input.GetKeyDown(rightFlipperKey)) _rightDone = true;

                if (_leftDone && _rightDone)
                    GoToStep(Step.Entrance2F);
                break;

            case Step.Entrance2F:
                if (Input.anyKeyDown)
                    GoToStep(Step.Plunger);
                break;

            case Step.Plunger:
                // Space 발사는 OnBallLaunched 이벤트로 처리
                break;

            case Step.ScoreMission:
                CheckScoreMission();
                break;
        }
    }

    private void BindButtons()
    {
        if (retryButton)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryTutorial);
        }

        if (goLobbyButton)
        {
            goLobbyButton.onClick.RemoveAllListeners();
            goLobbyButton.onClick.AddListener(GoLobby);
        }

        if (goGameButton)
        {
            goGameButton.onClick.RemoveAllListeners();
            goGameButton.onClick.AddListener(GoGame);
        }
    }

    private void GoToStep(Step next)
    {
        _step = next;

        switch (_step)
        {
            case Step.Intro:
                if (focus)
                {
                    focus.CaptureOriginal();
                    focus.Restore();
                }

                if (tutorialText)
                {
                    tutorialText.text =
                        "게임 설명\n" +
                        "핀볼(1F)에서 점수를 올리고, 조건을 달성하면 2F로 진입할 수 있어요.\n" +
                        "아무 키나 눌러 계속…";
                }
                break;

            case Step.Flippers:
                _leftDone = false;
                _rightDone = false;

                if (focus && focusFlippers)
                    focus.FocusTo(focusFlippers);

                if (tutorialText)
                    tutorialText.text = $"( {leftFlipperKey} / {rightFlipperKey} 를 누르세요! )";
                break;

            case Step.Entrance2F:
                if (focus && focus2FEntrance)
                    focus.FocusTo(focus2FEntrance);

                if (tutorialText)
                {
                    tutorialText.text =
                        "( 장애물 5개를 맞추면 2층 입장 가능! )\n" +
                        "아무 키나 눌러 계속…";
                }
                break;

            case Step.Plunger:
                if (focus && focusPlunger)
                    focus.FocusTo(focusPlunger);

                if (plunger)
                    plunger.tutorialForceLock = false;

                if (tutorialText)
                    tutorialText.text = "( Space 를 눌러 공을 발사하세요! )";
                break;

            case Step.ScoreMission:
                _scoreMissionActive = true;

                if (focus)
                {
                    focus.Restore();
                    focus.ReEnableDisabled();
                }

                if (railEntranceTrigger)
                    railEntranceTrigger.enabled = true;

                if (tutorialText)
                {
                    tutorialText.text =
                        $"( {tutorialTargetScore}점을 달성하면 튜토리얼 완료! )";
                }
                break;

            case Step.Done:
                EndTutorialSuccess();
                break;

            case Step.Failed:
                EndTutorialFail();
                break;
        }
    }

    private void OnBallLaunched()
    {
        if (_step != Step.Plunger) return;

        GoToStep(Step.ScoreMission);
    }

    private void CheckScoreMission()
    {
        if (!_scoreMissionActive) return;
        if (!scoreManager) return;

        if (scoreManager.curScore >= tutorialTargetScore)
        {
            GoToStep(Step.Done);
        }
    }

    private void OnGameOverWithScore(int finalScore)
    {
        if (!_scoreMissionActive) return;
        if (_resultShown) return;

        if (finalScore >= tutorialTargetScore)
            GoToStep(Step.Done);
        else
            GoToStep(Step.Failed);
    }

    private void OnGameClearWithScore(int finalScore)
    {
        if (!_scoreMissionActive) return;
        if (_resultShown) return;

        if (finalScore >= tutorialTargetScore)
            GoToStep(Step.Done);
        else
            GoToStep(Step.Failed);
    }

    private void EndTutorialSuccess()
    {
        if (_resultShown) return;

        _resultShown = true;
        _scoreMissionActive = false;

        if (tutorialText) tutorialText.text = "";

        ShowResultPanel(
            "튜토리얼 완료!",
            $"{tutorialTargetScore}점 달성 성공!\n로비로 갈지, 바로 게임하러 갈지 선택해줘."
        );

        Debug.Log("[Tutorial] Success");
    }

    private void EndTutorialFail()
    {
        if (_resultShown) return;

        _resultShown = true;
        _scoreMissionActive = false;

        if (tutorialText) tutorialText.text = "";

        ShowResultPanel(
            "다시 해볼래?",
            $"{tutorialTargetScore}점에 도달하지 못했어.\n다시 튜토리얼을 하거나, 로비/게임씬으로 이동할 수 있어."
        );

        Debug.Log("[Tutorial] Failed");
    }

    private void ShowResultPanel(string title, string desc)
    {
        if (resultPanel) resultPanel.SetActive(true);
        if (resultTitleText) resultTitleText.text = title;
        if (resultDescText) resultDescText.text = desc;

        Time.timeScale = 0f;
    }

    public void RetryTutorial()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void GoLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(lobbySceneName);
    }

    public void GoGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }
}