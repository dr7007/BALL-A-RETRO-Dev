using UnityEngine;
// UnityEditor 관련 using 문 제거

namespace PSH
{
    public class PSH_Script_LobbyUIManager : MonoBehaviour
    {
        // --- [Editor Settings] 및 targetScene 변수 제거 ---

        [Header("Build Settings")]
        [Tooltip("빌드 버전에서 로드할 게임 씬의 이름")]
        [SerializeField] private string sceneNameForBuild = "";

        [Header("Manager Prefabs")]
        [Tooltip("게임 시작 시 생성할 CursorManager 프리팹")]
        [SerializeField] private GameObject cursorManagerPrefab;

        [Header("InpuEmain Prefabs")]
        [Tooltip("메일 입력창 프리팹")]
        [SerializeField] private GameObject inputEmailPrefab;
        [SerializeField] private GameObject RnakingPanel;

        private PSH_Script_SceneLoader sceneLoader;

        private void Awake()
        {
            if (PSH_Script_CursorManager.Instance == null)
            {
                if (cursorManagerPrefab != null)
                {
                    Instantiate(cursorManagerPrefab);
                }
                else
                {
                    Debug.LogError("CursorManager 프리팹이 LobbyUIManager에 할당되지 않았습니다!");
                }
            }
            if(inputEmailPrefab !=null)
            {
                inputEmailPrefab.SetActive(false);
            }
            else
            {
                Debug.LogError("inputEmailPrefab 프리팹이 LobbyUIManager에 할당되지 않았습니다!");
            }
            if (RnakingPanel != null)
            {
                RnakingPanel.SetActive(false);
            }
        }

        private void Start()
        {
            sceneLoader = FindFirstObjectByType<PSH_Script_SceneLoader>();

            if (sceneLoader == null)
            {
                Debug.LogError("SceneLoader를 찾을 수 없습니다! 씬에 SceneLoader 오브젝트가 있는지 확인하세요.");
            }

           // inputEmailPrefab = FindFirstObjectByType<난중에>();

        }

        public void OnClick_StartGame()
        {
            inputEmailPrefab.SetActive(true);
        }
        public void OnClick_InputEmail()
        {
            if (sceneLoader == null)
            {
                Debug.LogError("SceneLoader가 할당되지 않아 게임을 시작할 수 없습니다.");
                return;
            }

            if (string.IsNullOrEmpty(sceneNameForBuild))
            {
                Debug.LogError("인스펙터에서 Scene Name For Build를 설정해주세요!");
                return;
            }
            sceneLoader.LoadSceneAsyncByName(sceneNameForBuild);
        }
        public void OnClick_RankingPanelOpen()
        {
            RnakingPanel.SetActive(true);
        }
        public void OnClick_RankingPanelClose()
        {
            RnakingPanel.SetActive(false);
        }

        public void OnClick_QuitGame()
        {
            Debug.Log("Game Quit");
            Application.Quit();
        }

    }
}

