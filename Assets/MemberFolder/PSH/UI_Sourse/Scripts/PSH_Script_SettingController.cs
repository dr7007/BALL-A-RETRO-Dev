using UnityEngine;
using UnityEngine.SceneManagement; // 씬 판별을 위해 추가

public class PSH_Script_SettingController : MonoBehaviour
{
    [Header("설정창 오브젝트 연결")]
    
    [Header("씬 이름 설정")]
    public string gameSceneName = "GameScene"; // [추가] 실제 게임 씬 이름으로 변경 필요
    
    [Header("설정창 및 경고창 UI")]
    public GameObject panelSetting;
    public GameObject panelWarning;   
    public GameObject goToLobbyButton; 
    
    // [추가] PSH 스크립트가 스스로 게임 종료 여부를 기억할 변수
    private bool isGameEnded = false;
    private bool isSettingOpen = false;
    private void OnEnable()
    {
        KHS_Script_ScoreManager.OnGameClear += SetGameEndedTrue;
        KHS_Script_ScoreManager.OnGameOver += SetGameEndedTrue;
    }

    private void OnDisable()
    {
        KHS_Script_ScoreManager.OnGameClear -= SetGameEndedTrue;
        KHS_Script_ScoreManager.OnGameOver -= SetGameEndedTrue;
    }
    void Start()
    {
        // 시작할 때 설정창은 꺼져 있어야 함
        if (panelSetting != null)
            panelSetting.SetActive(false);
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
    }
    public void ToggleMenu()
    {
        if (panelSetting == null) return;

        isSettingOpen = !isSettingOpen;
        panelSetting.SetActive(isSettingOpen);

        if (isSettingOpen)
        {
            Time.timeScale = 0f; 
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // [추가] 메뉴가 열릴 때 씬을 판별해서 버튼 활성화 상태를 업데이트
            UpdateButtonVisibility();
            
            // 메뉴가 열릴 때 오디오가 너무 크다면 살짝 줄이는 연출도 가능합니다.
            // if (CJS_Script_AudioDirector.I != null) CJS_Script_AudioDirector.I.bgmSource.volume *= 0.5f;
        }
        else
        {
            Time.timeScale = 1f;
            // 게임 환경에 맞춰 아래 주석을 조절하세요.
             Cursor.visible = false; 
             Cursor.lockState = CursorLockMode.Locked; 
        }
    }
    private void UpdateButtonVisibility()
    {
        if (goToLobbyButton == null) return;
        // 현재 씬이 게임씬일 때만 로비 버튼 활성화
        goToLobbyButton.SetActive(SceneManager.GetActiveScene().name == "PSH_Scene_Game");
    }
    // [중요] 로비 버튼을 눌렀을 때 실행될 함수
    public void OnClickGoToLobby()
    {
        Debug.Log($"[디버그] 로비 가기 버튼 클릭됨! (현재 게임 종료 상태: {isGameEnded})");

        if (!isGameEnded)
        {
            if (panelWarning != null) 
            {
                panelWarning.SetActive(true);
                Debug.Log("[디버그] 경고창 띄우기 성공!");
            }
        }
        else
        {
            Debug.Log("[디버그] 이미 게임이 끝나서 경고 없이 바로 로비로 갑니다.");
            ConfirmGoToLobby();
        }
    }

    // 경고창에서 "예"를 눌렀을 때 실행될 최종 함수
    public void ConfirmGoToLobby()
    {
        Debug.Log("[디버그] '예' 버튼 클릭됨! 로비 이동 시도 중...");
        Time.timeScale = 1f; 

        // 씬에서 KHS_Script_ResetController를 찾아서 이동 함수를 실행
        var resetCtrl = FindObjectOfType<KHS_Script_ResetController>();
        
        if (resetCtrl != null)
        {
            Debug.Log("[디버그] KHS_Script_ResetController를 찾았습니다! 이동 시작!");
            resetCtrl.GameGoToLobbyFunc(); 
        }
        else
        {
            Debug.LogError("[에러] 현재 씬에서 KHS_Script_ResetController가 붙어있는 오브젝트를 찾을 수 없습니다!");
        }
    }
        public void OnClick_WarningQuitGame()
    {
        Debug.Log("Game Quit");
        Application.Quit();
    }
    }