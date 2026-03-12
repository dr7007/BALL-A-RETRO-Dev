using UnityEngine;

public class PSH_Script_SettingController : MonoBehaviour
{
    [Header("설정창 오브젝트 연결")]
    public GameObject panelSetting; // Hierarchy에서 Panel_Setting을 드래그앤드롭

    private bool isSettingOpen = false;

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
            
            // [추가] 메뉴가 열릴 때 오디오가 너무 크다면 살짝 줄이는 연출도 가능합니다.
            // if (CJS_Script_AudioDirector.I != null) CJS_Script_AudioDirector.I.bgmSource.volume *= 0.5f;
        }
        else
        {
            Time.timeScale = 1f;
            // 게임 환경에 맞춰 아래 주석을 조절하세요.
           // Cursor.visible = false; 
           // Cursor.lockState = CursorLockMode.Locked; 
        }
    }
}