#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

using UnityEngine;
using UnityEngine.SceneManagement;

namespace PSH
{
    public class PSH_Script_SceneLoader : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }


        public void LoadSceneByName(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

/// <summary>
        /// [에디터 전용] 씬 에셋의 경로를 이용해 씬을 로드합니다. Build Settings에 없어도 됩니다.
        /// </summary>
        public void LoadSceneByPath_EditorOnly(string scenePath)
        {
// 이 코드는 유니티 에디터 안에서만 컴파일되고 실행됩니다.
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError("로드할 씬의 경로가 비어있습니다!");
                return;
            }
            Debug.Log($"[에디터 전용] 경로로 씬을 로드합니다: {scenePath}");
            // 플레이 모드에서 지정된 경로의 씬을 로드하는 에디터 전용 함수
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            // 에디터가 아닌 환경(빌드된 게임)에서는 이 코드가 실행됩니다.
            Debug.LogError("이 기능은 유니티 에디터에서만 사용할 수 있습니다! Build Settings를 확인하세요.");
#endif
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

            if (scene.name.Contains("Game"))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            // 다른 씬이 로드되었을 경우
            else
            {
                Debug.Log(scene.name + " 씬이 로드되었습니다.");
            }
        }
    }
}