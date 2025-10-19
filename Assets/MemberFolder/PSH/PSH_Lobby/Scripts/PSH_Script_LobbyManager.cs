using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PSH
{
    public class PSH_Script_LobbyManager : MonoBehaviour
    {
        // 씬 로더의 참조를 저장할 변수
        private PSH_Script_SceneLoader sceneLoader;
        [SerializeField] private SceneAsset targetScene;

        private void Start()
        {
            sceneLoader = FindObjectOfType<PSH_Script_SceneLoader>();

            if (sceneLoader == null)
            {
                Debug.LogError("SceneLoaderPSH를 찾을 수 없습니다! 씬에 SceneLoaderPSH 오브젝트가 있는지 확인하세요.");
            }
        }

public void OnClick_StartGame()
        {
            if (sceneLoader == null) return;

#if UNITY_EDITOR
            if (targetScene != null)
            {
                // SceneAsset에서 실제 파일 경로를 가져옵니다.
                string scenePath = AssetDatabase.GetAssetPath(targetScene);
                sceneLoader.LoadSceneByPath_EditorOnly(scenePath);
            }
            else
            {
                Debug.LogError("인스펙터에서 Target Scene이 설정되지 않았습니다!");
            }
#else
            // 빌드된 게임에서는 이 코드가 실행됩니다.
            // 여기서는 기존 방식대로 정확한 씬 이름을 사용해야 합니다.
            sceneLoader.LoadSceneByName("Game"); 
#endif
        }

        public void OnClick_QuitGame()
        {
            Debug.LogError("Game Quit");
            Application.Quit();
        }
    }
}