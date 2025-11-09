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
            if (loadingPanel == null) return;
        }

        // OnEnable/OnDisable 및 OnSceneLoaded는 이제 UI를 직접 제어하지 않습니다.
        // 필요에 따라 로깅이나 다른 초기화 용도로 남겨둘 수 있습니다.
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

        /// <summary>
        /// (빌드용) 비동기 씬 로딩 코루틴
        /// </summary>
        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            float startTime = Time.time;
            if (loadingPanel != null) loadingPanel.SetActive(true);
           //else Debug.LogWarning("SceneLoader에 Loading Panel이 할당되지 않았습니다.");

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false; // 씬이 준비되어도 바로 활성화하지 않음

            // 씬 로딩이 90% (준비 완료)될 때까지 대기
            while (asyncLoad.progress < 0.9f)
            {
                // TODO: 여기에 로딩 바/퍼센트 업데이트 로직 추가 (asyncLoad.progress)
                yield return null;
            }

            // 씬 로딩은 완료됨 (활성화만 남음)
            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minLoadingTime)
            {
                // 최소 로딩 시간 보장
                yield return new WaitForSeconds(minLoadingTime - elapsedTime);
            }

            // 씬 활성화
            asyncLoad.allowSceneActivation = true;

            // 씬 활성화가 완료될 때까지 한 프레임 대기
            yield return null;

            // 씬 활성화가 완료되었으므로, 여기서 패널을 닫고 코루틴을 정리
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            currentLoadingCoroutine = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// (에디터용) 씬 로딩 코루틴
        /// </summary>
        private IEnumerator LoadSceneCoroutine_Editor(string scenePath)
        {
            float startTime = Time.time;
            if (loadingPanel != null) loadingPanel.SetActive(true);
           // else Debug.LogWarning("SceneLoader에 Loading Panel이 할당되지 않았습니다.");

            // 패널이 한 프레임이라도 보이도록 대기
            yield return null;

            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minLoadingTime)
            {
                yield return new WaitForSeconds(minLoadingTime - elapsedTime);
            }

            // 에디터용 씬 로드 (이 함수는 씬 로드를 시작하고 즉시 반환됨)
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));

            // OnSceneLoaded가 호출될 때까지 기다리지 않고,
            // 씬 로드가 완료될 것으로 예상되는 다음 프레임까지 대기
            yield return null;

            // 여기서 직접 패널 닫기
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            currentLoadingCoroutine = null;
        }
#endif

        /// <summary>
        /// 씬 로드가 완료되었을 때 호출됩니다.
        /// (UI 제어 로직을 코루틴으로 옮겼으므로, 여기서는 로깅만 수행)
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"'{scene.name}' 씬 로드가 완료되었습니다.");

            // 코루틴이 스스로를 정리하므로, OnSceneLoaded에서 UI를 제어하거나
            // currentLoadingCoroutine을 null로 설정하지 않습니다.
        }
    }
}
