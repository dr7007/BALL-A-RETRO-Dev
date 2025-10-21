using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


namespace PSH
{
    public class PSH_Script_LobbyUIManager : MonoBehaviour
    {
        [Header("Editor Settings")]
        [Tooltip("에디터에서 테스트할 대상 게임 씬 에셋")]
        [SerializeField] private SceneAsset targetScene;

        [Header("Build Settings")]
        [Tooltip("빌드 버전에서 로드할 게임 씬의 이름")]


        [SerializeField] private string sceneNameForBuild = "";

        private PSH_Script_SceneLoader sceneLoader;

        private void Start()
        {
            sceneLoader = FindFirstObjectByType<PSH_Script_SceneLoader>();

            if (sceneLoader == null)
            {
                Debug.LogError("SceneLoader를 찾을 수 없습니다! 씬에 SceneLoader 오브젝트가 있는지 확인하세요.");
            }
        }


        public void OnClick_StartGame()
        {
            if (sceneLoader == null)
            {
                Debug.LogError("SceneLoader가 할당되지 않아 게임을 시작할 수 없습니다.");
                return;
            }

#if UNITY_EDITOR
            if (targetScene == null)
            {
                Debug.LogError("인스펙터에서 Target Scene을 설정해주세요!");
                return;
            }
            string scenePath = AssetDatabase.GetAssetPath(targetScene);
            sceneLoader.LoadSceneByPath_Editor(scenePath);
#else
            // --- 빌드 전용 로직 ---
            if (string.IsNullOrEmpty(sceneNameForBuild))
            {
                Debug.LogError("인스펙터에서 Scene Name For Build를 설정해주세요!");
                return;
            }
            sceneLoader.LoadSceneAsyncByName(sceneNameForBuild);
#endif
        }

        public void OnClick_QuitGame()
        {
            Debug.Log("Game Quit");
            Application.Quit();
        }
    }
}
