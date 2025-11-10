using UnityEngine;

namespace PSH
{
    /// <summary>
    /// 게임 씬의 UI(설명 팝업 등)를 관리합니다.
    /// 이 스크립트는 게임 씬에만 존재해야 합니다.
    /// </summary>
    public class PSH_Script_GameUIManager : MonoBehaviour
    {
        [Tooltip("게임 시작 시 보여줄 설명 팝업 UI")]
        [SerializeField] private GameObject gameDescriptionPopup;

        private void Start()
        {
            ShowDescriptionPopup();
        }

        private void ShowDescriptionPopup()
        {
            if (gameDescriptionPopup != null)
            {
                gameDescriptionPopup.SetActive(true);
            }

            Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        public void ClosePopupAndStartGame()
        {
            if (gameDescriptionPopup != null)
            {
                gameDescriptionPopup.SetActive(false);
            }
            Time.timeScale = 1f;
        }
    }
}
