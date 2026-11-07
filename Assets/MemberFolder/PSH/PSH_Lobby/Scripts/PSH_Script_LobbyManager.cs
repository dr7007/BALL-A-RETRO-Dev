using UnityEngine;
using TMPro;
// UnityEditor 관련 using 문 제거

namespace PSH
{
    public class PSH_Script_LobbyUIManager : MonoBehaviour
    {
        // --- [Editor Settings] 및 targetScene 변수 제거 ---

        [Header("Build Settings")]
        [Tooltip("빌드 버전에서 로드할 게임 씬의 이름")]
        [SerializeField] private string sceneNameForBuild = "Main_Scene_Game";

        [Header("Manager Prefabs")]
        [Tooltip("게임 시작 시 생성할 CursorManager 프리팹")]
        [SerializeField] private GameObject cursorManagerPrefab;

        [Header("InpuEmain Prefabs")]
        [Tooltip("메일 입력창 프리팹")]
        [SerializeField] private GameObject inputEmailPrefab;
        [SerializeField] private GameObject RnakingPanel;
        [SerializeField] private Sprite rankingIcon;

        [Header("Tutorial Settings")]
        [Tooltip("튜토리얼 씬의 이름")]
        [SerializeField] private string tutorialSceneName = "Main_Scene_Tutorial";

        private PSH_Script_SceneLoader sceneLoader;
        private bool isLoading;
        private PSH_Script_WorldRankingUI worldRanking;

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
            if (RnakingPanel != null) RnakingPanel.SetActive(false);
            var label = GetComponentInChildren<TMP_Text>(true);
            worldRanking = PSH_Script_WorldRankingUI.Install(transform, label != null ? label.font : null, true, rankingIcon);
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

        public void OnClick_StartGame() => LoadScene(sceneNameForBuild);

        // Compatibility for old serialized callbacks; entering a nickname is no longer a step.
        public void OnClick_InputEmail() => OnClick_StartGame();
        public void OnClick_RankingPanelOpen() { if (worldRanking) worldRanking.Show(); }
        public void OnClick_RankingPanelClose() { if (worldRanking) worldRanking.Close(); }

        private void LoadScene(string sceneName)
        {
            if (isLoading) return;
            if (sceneLoader == null) sceneLoader = PSH_Script_SceneLoader.Instance;
            if (sceneLoader == null || string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneLoader 또는 이동할 씬이 설정되지 않았습니다.");
                return;
            }
            if (inputEmailPrefab) inputEmailPrefab.SetActive(false);
            isLoading = true;
            Time.timeScale = 1f;
            sceneLoader.LoadSceneAsyncByName(sceneName);
        }

        public void OnClick_QuitGame()
        {
            Debug.Log("Game Quit");
            Application.Quit();
        }

        public void OnClick_Tutorial() => LoadScene(tutorialSceneName);
    }
}
