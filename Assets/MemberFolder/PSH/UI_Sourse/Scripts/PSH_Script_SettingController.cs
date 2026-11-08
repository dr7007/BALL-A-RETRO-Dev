using UnityEngine;

public class PSH_Script_SettingController : MonoBehaviour
{
    // HUD(0), 대사(3) 위에 시스템창과 확인창만 별도로 올린다.
    private const int MenuSortingOrder = 100;
    private static PSH_Script_SettingController openMenu;
    private static int lastMenuToggleFrame = -1;
    public static bool IsMenuOpen => openMenu != null && openMenu.isActiveAndEnabled && openMenu.isSettingOpen;
    public static bool BlocksDialogueKeyboardInput => IsMenuOpen || lastMenuToggleFrame == Time.frameCount;

    [Header("설정창 오브젝트 연결")]
    
    [Header("씬 이름 설정")]
    public string gameSceneName = "Main_Scene_Game";
    public string tutorialSceneName = "Main_Scene_Tutorial";
    
    [Header("설정창 및 경고창 UI")]
    public GameObject panelSetting;
    public GameObject panelWarning;   
    public GameObject goToLobbyButton; 
    [SerializeField] private GameObject quitGameButton;
    [SerializeField] private PSH_Script_LocalizedText warningMessage;
    [SerializeField] private PSH_Script_LocalizedText warningConfirmLabel;

    private enum ExitAction { None, Lobby, Quit }
    private ExitAction pendingExit;
    private bool isLeaving;
    private bool IsGameplayScene => gameObject.scene.name == gameSceneName
        || gameObject.scene.name == tutorialSceneName;
    
    // [추가] PSH 스크립트가 스스로 게임 종료 여부를 기억할 변수
    private bool isGameEnded = false;
    private bool isSettingOpen = false;
    private float timeScaleBeforeMenu;
    private bool cursorVisibleBeforeMenu;
    private CursorLockMode cursorLockBeforeMenu;
    private void OnEnable()
    {
        if (isSettingOpen) openMenu = this;
        KHS_Script_ScoreManager.OnGameClear += SetGameEndedTrue;
        KHS_Script_ScoreManager.OnGameOver += SetGameEndedTrue;
        PSH_Script_GameplayInput.Resumed += OnGameplayResumed;
    }

    private void OnDisable()
    {
        if (openMenu == this) openMenu = null;
        KHS_Script_ScoreManager.OnGameClear -= SetGameEndedTrue;
        KHS_Script_ScoreManager.OnGameOver -= SetGameEndedTrue;
        PSH_Script_GameplayInput.Resumed -= OnGameplayResumed;
    }
    void Start()
    {
        // 시작할 때 설정창은 꺼져 있어야 함
        if (panelSetting != null && !isSettingOpen)
            panelSetting.SetActive(false);
        if (panelWarning != null) panelWarning.SetActive(false);
        UpdateButtonVisibility();
    }

    void Update()
    {
        // ESC 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }
    private void SetGameEndedTrue() 
    { 
        isGameEnded = true;
        if (isSettingOpen)
        {
            timeScaleBeforeMenu = 0f;
            cursorVisibleBeforeMenu = true;
            cursorLockBeforeMenu = CursorLockMode.None;
        }
    }

    private void OnGameplayResumed()
    {
        if (!isSettingOpen) return;
        // A choice/popup can finish under settings (for example during a camera
        // return). Keep the modal usable and resume gameplay only after it closes.
        if (!isGameEnded)
        {
            timeScaleBeforeMenu = 1f;
            cursorVisibleBeforeMenu = false;
            cursorLockBeforeMenu = CursorLockMode.Locked;
        }
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void ToggleMenu()
    {
        if (isLeaving || BallARetroStartupNotice.IsVisible) return;
        if (isSettingOpen && panelWarning != null && panelWarning.activeSelf)
        {
            panelWarning.SetActive(false);
            pendingExit = ExitAction.None;
            return;
        }
        SetMenuOpen(!isSettingOpen);
    }

    // A close button must not toggle the menu back on if invoked more than once.
    public void CloseMenu() => SetMenuOpen(false);

    private void SetMenuOpen(bool open)
    {
        if (isLeaving || panelSetting == null || isSettingOpen == open) return;

        if (open)
        {
            // 대사/선택창/로비 등 메뉴를 열기 전 화면의 입력 및 일시정지 상태를 보존한다.
            timeScaleBeforeMenu = Time.timeScale;
            cursorVisibleBeforeMenu = Cursor.visible;
            cursorLockBeforeMenu = Cursor.lockState;
            EnsureModalCanvas(panelSetting, MenuSortingOrder);
            EnsureModalCanvas(panelWarning, MenuSortingOrder + 1);
        }

        isSettingOpen = open;
        lastMenuToggleFrame = Time.frameCount;
        if (isSettingOpen) openMenu = this;
        else if (openMenu == this) openMenu = null;

        if (!isSettingOpen && panelWarning != null)
            panelWarning.SetActive(false);
        pendingExit = ExitAction.None;
        panelSetting.SetActive(isSettingOpen);

        if (isSettingOpen)
        {
            Time.timeScale = 0f; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            UpdateButtonVisibility();
        }
        else
        {
            Time.timeScale = timeScaleBeforeMenu;
            // 메뉴가 열린 사이 게임이 끝났어도 엔딩 대사와 결과 버튼을 클릭할 수 있어야 한다.
            Cursor.lockState = isGameEnded ? CursorLockMode.None : cursorLockBeforeMenu;
            Cursor.visible = isGameEnded || cursorVisibleBeforeMenu;
        }
    }

    private static void EnsureModalCanvas(GameObject panel, int sortingOrder)
    {
        if (panel == null) return;

        var modalCanvas = panel.GetComponent<PSH_Script_ModalCanvas>();
        if (modalCanvas == null) modalCanvas = panel.AddComponent<PSH_Script_ModalCanvas>();
        modalCanvas.SetSortingOrder(sortingOrder);
        modalCanvas.enabled = true;
    }

    private void UpdateButtonVisibility()
    {
        if (goToLobbyButton != null) goToLobbyButton.SetActive(IsGameplayScene);
        if (quitGameButton != null) quitGameButton.SetActive(!IsGameplayScene);
    }

    public void OnClickGoToLobby()
    {
        if (IsGameplayScene) ShowExitConfirmation(ExitAction.Lobby);
    }

    public void OnClickQuitGame()
    {
        if (!IsGameplayScene) ShowExitConfirmation(ExitAction.Quit);
    }

    private void ShowExitConfirmation(ExitAction action)
    {
        if (!isSettingOpen || isLeaving || panelWarning == null) return;
        pendingExit = action;
        warningMessage?.SetKey(action == ExitAction.Lobby ? "Warning_ReturnToLobby" : "Warning_Quit");
        warningConfirmLabel?.SetKey(action == ExitAction.Lobby ? "Button_ReturnToLobby" : "Button_Quit");
        panelWarning.SetActive(true);
    }

    public void ConfirmExit()
    {
        // A stale/double click, or a hidden confirmation, must never discard a run.
        if (isLeaving || !isSettingOpen || panelWarning == null || !panelWarning.activeSelf
            || pendingExit == ExitAction.None) return;

        var action = pendingExit;
        KHS_Script_ResetController resetCtrl = null;
        if (action == ExitAction.Lobby)
        {
            resetCtrl = FindFirstObjectByType<KHS_Script_ResetController>();
            if (resetCtrl == null)
            {
                Debug.LogError("[Settings] Cannot return to the lobby: ResetController is missing.");
                return;
            }
        }

        CloseMenu();
        isLeaving = true;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (action == ExitAction.Lobby) resetCtrl.GameGoToLobbyFunc();
        else Application.Quit();
    }

    // Retain callbacks used by the older development scenes.
    public void ConfirmGoToLobby() => ConfirmExit();
    public void OnClick_WarningQuitGame() => OnClickQuitGame();
}
