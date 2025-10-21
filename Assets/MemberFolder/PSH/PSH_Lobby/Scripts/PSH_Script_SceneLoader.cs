using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace PSH
{
    /// <summary>
    /// 씬 로딩을 전담하며, 로딩 UI를 관리합니다.
    /// </summary>
    public class PSH_Script_SceneLoader : MonoBehaviour
    {
        [Tooltip("씬 로딩 시 활성화할 UI 패널")]
        [SerializeField] private GameObject loadingPanel;

        [Tooltip("로딩 UI를 최소한 보여줄 시간 (초)")]
        [SerializeField] private float minLoadingTime = 2.0f;

        private Coroutine currentLoadingCoroutine = null;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }


        public void LoadSceneAsyncByName(string sceneName)
        {
            if (currentLoadingCoroutine != null)
            {
                StopCoroutine(currentLoadingCoroutine);
            }
            currentLoadingCoroutine = StartCoroutine(LoadSceneCoroutine(sceneName));
        }


        public void LoadSceneByPath_Editor(string scenePath)
        {
#if UNITY_EDITOR
            if (currentLoadingCoroutine != null)
            {
                StopCoroutine(currentLoadingCoroutine);
            }
            currentLoadingCoroutine = StartCoroutine(LoadSceneCoroutine_Editor(scenePath));
#else
            Debug.LogError("이 기능은 유니티 에디터에서만 사용할 수 있습니다!");
#endif
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            float startTime = Time.time;
            if (loadingPanel != null) loadingPanel.SetActive(true);
            else Debug.LogWarning("SceneLoader에 Loading Panel이 할당되지 않았습니다.");

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minLoadingTime)
            {
                yield return new WaitForSeconds(minLoadingTime - elapsedTime);
            }
            // 씬 로드가 완료되면 OnSceneLoaded 이벤트가 나머지(패널 숨기기 등)를 처리합니다.
        }

#if UNITY_EDITOR
        private IEnumerator LoadSceneCoroutine_Editor(string scenePath)
        {
            float startTime = Time.time;
            if (loadingPanel != null) loadingPanel.SetActive(true);
            else Debug.LogWarning("SceneLoader에 Loading Panel이 할당되지 않았습니다.");

            yield return null; 
            
            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minLoadingTime)
            {
                yield return new WaitForSeconds(minLoadingTime - elapsedTime);
            }
            
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
        }
#endif

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"'{scene.name}' 씬 로드가 완료되었습니다.");
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            currentLoadingCoroutine = null;
        }
    }
}

