using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

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
    private bool _playedLeft;
    private bool _playedRight;
    private string _lastMissionHint;
    private float _lastPracticeTime;

    private bool HasPracticedFlippers => _playedLeft && _playedRight;

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += RefreshLocalizedText;
        KHS_Script_PlungerController.OnBallLaunched += OnBallLaunched;
        KHS_Script_FliperController.OnFlipperPressed += OnFlipperPressed;
        KHS_Script_ScoreManager.OnGameOverWithScore += OnGameOverWithScore;
        KHS_Script_ScoreManager.OnGameClearWithScore += OnGameClearWithScore;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= RefreshLocalizedText;
        KHS_Script_PlungerController.OnBallLaunched -= OnBallLaunched;
        KHS_Script_FliperController.OnFlipperPressed -= OnFlipperPressed;
        KHS_Script_ScoreManager.OnGameOverWithScore -= OnGameOverWithScore;
        KHS_Script_ScoreManager.OnGameClearWithScore -= OnGameClearWithScore;
    }

    void Start()
    {
        tutorialSceneName = gameObject.scene.name;
        if (!focus) focus = FindObjectOfType<CJS_TutorialCameraFocus>(true);
        if (!plunger) plunger = FindObjectOfType<KHS_Script_PlungerController>(true);
        if (!scoreManager) scoreManager = FindObjectOfType<KHS_Script_ScoreManager>(true);

        if (resultPanel) resultPanel.SetActive(false);

        BindButtons();
        foreach (var text in new[] { tutorialText, resultTitleText, resultDescText })
        {
            if (text == null) continue;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = text.fontSize * .65f;
            text.enableAutoSizing = true;
        }

        // Step4 전까지 플런저 잠금
        if (plunger) plunger.tutorialForceLock = true;

        // 튜토리얼 진행 중 2층 입구 막기
        if (railEntranceTrigger) railEntranceTrigger.enabled = false;

        GoToStep(Step.Intro);
    }

    void Update()
    {
        if (_resultShown || Time.timeScale <= 0f || PSH_Script_SettingController.BlocksDialogueKeyboardInput) return;

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


                break;

            case Step.Flippers:
                _leftDone = false;
                _rightDone = false;

                if (focus && focusFlippers)
                    focus.FocusTo(focusFlippers);


                break;

            case Step.Entrance2F:
                if (focus && focus2FEntrance)
                    focus.FocusTo(focus2FEntrance);


                break;

            case Step.Plunger:
                if (focus && focusPlunger)
                    focus.FocusTo(focusPlunger);

                if (plunger)
                    plunger.tutorialForceLock = false;


                break;

            case Step.ScoreMission:
                _scoreMissionActive = true;
                _playedLeft = false;
                _playedRight = false;
                _lastMissionHint = null;

                if (focus)
                {
                    focus.Restore();
                    focus.ReEnableDisabled();
                }

                if (railEntranceTrigger)
                    railEntranceTrigger.enabled = true;

                RefreshMissionHint();
                break;

            case Step.Done:
                EndTutorialSuccess();
                break;

            case Step.Failed:
                EndTutorialFail();
                break;
        }
        RefreshLocalizedText(null);
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

        // Let the last press visibly move the flipper before pausing for the result.
        if (scoreManager.curScore >= tutorialTargetScore && HasPracticedFlippers
            && Time.time - _lastPracticeTime >= 0.25f)
        {
            GoToStep(Step.Done);
        }
        else
        {
            RefreshMissionHint();
        }
    }

    private void OnFlipperPressed(KeyCode key)
    {
        if (!_scoreMissionActive || _resultShown || Time.timeScale <= 0f) return;
        if (key == leftFlipperKey) _playedLeft = true;
        if (key == rightFlipperKey) _playedRight = true;
        if (key == leftFlipperKey || key == rightFlipperKey) _lastPracticeTime = Time.time;
        CheckScoreMission();
    }

    private void RefreshMissionHint()
    {
        string left = PSH_Script_Localization.Text(_playedLeft ? "Tut_Practiced" : "Tut_Practice");
        string right = PSH_Script_Localization.Text(_playedRight ? "Tut_Practiced" : "Tut_Practice");
        string score = scoreManager && scoreManager.curScore >= tutorialTargetScore
            ? PSH_Script_Localization.Text("Tut_TargetReached") : PSH_Script_Localization.Text("Tut_Target", tutorialTargetScore);
        string hint = PSH_Script_Localization.Text("Tut_Mission", score, KeyLabel(leftFlipperKey), left, KeyLabel(rightFlipperKey), right);
        if (hint == _lastMissionHint) return;
        _lastMissionHint = hint;
        if (tutorialText) tutorialText.text = hint;
    }

    private void OnGameOverWithScore(int finalScore)
    {
        if (!_scoreMissionActive) return;
        if (_resultShown) return;

        if (finalScore >= tutorialTargetScore && HasPracticedFlippers)
            GoToStep(Step.Done);
        else
            GoToStep(Step.Failed);
    }

    private void OnGameClearWithScore(int finalScore)
    {
        if (!_scoreMissionActive) return;
        if (_resultShown) return;

        if (finalScore >= tutorialTargetScore && HasPracticedFlippers)
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

        ShowResultPanel();

        Debug.Log("[Tutorial] Success");
    }

    private void EndTutorialFail()
    {
        if (_resultShown) return;

        _resultShown = true;
        _scoreMissionActive = false;

        if (tutorialText) tutorialText.text = "";

        ShowResultPanel();

        Debug.Log("[Tutorial] Failed");
    }

    private void ShowResultPanel()
    {
        if (resultPanel)
        {
            var modal = resultPanel.GetComponent<PSH_Script_ModalCanvas>();
            if (!modal) modal = resultPanel.AddComponent<PSH_Script_ModalCanvas>();
            modal.SetSortingOrder(50);
            resultPanel.SetActive(true);
        }
        RefreshLocalizedText(null);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static string KeyLabel(KeyCode key) => key == KeyCode.Slash ? "/" : key.ToString();

    private void RefreshLocalizedText(Locale _)
    {
        // Only redraw text: never replay a step, camera move, launch or completion event.
        if (_resultShown)
        {
            bool success = _step == Step.Done;
            if (resultTitleText) resultTitleText.text = PSH_Script_Localization.Text(success ? "Tut_SuccessTitle" : "Tut_FailTitle");
            if (resultDescText)
                resultDescText.text = success ? PSH_Script_Localization.Text("Tut_SuccessBody", tutorialTargetScore)
                    : HasPracticedFlippers ? PSH_Script_Localization.Text("Tut_FailScore", tutorialTargetScore)
                    : PSH_Script_Localization.Text("Tut_FailPractice", KeyLabel(leftFlipperKey), KeyLabel(rightFlipperKey));
            return;
        }
        if (tutorialText == null) return;
        switch (_step)
        {
            case Step.Intro: tutorialText.text = PSH_Script_Localization.Text("Tut_Intro"); break;
            case Step.Flippers: tutorialText.text = PSH_Script_Localization.Text("Tut_Flippers", KeyLabel(leftFlipperKey), KeyLabel(rightFlipperKey)); break;
            case Step.Entrance2F: tutorialText.text = PSH_Script_Localization.Text("Tut_Entrance"); break;
            case Step.Plunger: tutorialText.text = PSH_Script_Localization.Text("Tut_Launch"); break;
            case Step.ScoreMission: _lastMissionHint = null; RefreshMissionHint(); break;
        }
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
