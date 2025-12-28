using UnityEngine;

public class PSH_MenuController : MonoBehaviour
{
    [Header("설정창 오브젝트 연결")]
    public GameObject panelSetting; // Hierarchy에서 Panel_Setting을 드래그앤드롭

    private bool isMenuOpen = false;

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

        isMenuOpen = !isMenuOpen;
        panelSetting.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
            // 메뉴가 열릴 때: 게임 일시정지 및 마우스 커서 보이기
            Time.timeScale = 0f; 
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            // 메뉴가 닫힐 때: 게임 재개 및 마우스 커서 숨기기 (필요 시)
            Time.timeScale = 1f;
            // Cursor.visible = false; // 게임 특성에 따라 주석 해제
            // Cursor.lockState = CursorLockMode.Locked;
        }
    }
}