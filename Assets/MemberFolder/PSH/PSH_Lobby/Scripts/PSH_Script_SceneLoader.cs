using System;
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
    /// 

    public class PSH_Script_SceneLoader : MonoBehaviour
    {
        [Tooltip("씬 로딩 시 활성화할 UI 패널")]
        [SerializeField] private GameObject loadingPanel;

        [Tooltip("로딩 UI를 최소한 보여줄 시간 (초)")]
        [SerializeField] private float minLoadingTime = 2.0f;
        // 1. 싱글톤 인스턴스를 저장할 static 변수 추가
        public static PSH_Script_SceneLoader Instance { get; private set; }

        private Coroutine currentLoadingCoroutine = null;

        private void Awake()
        {
            // 2. 싱글톤 중복 방지 처리
            if (Instance == null)
            {
                // 인스턴스가 없으면, 나를 인스턴스로 지정
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                // 이미 인스턴스가 있는데, 그게 내가 아니면? (중복)
                // 나는 파괴하고 즉시 리턴
                Destroy(gameObject);
                return;
            }

            // 3. 기존 Awake 로직 (인스턴스일 때만 실행됨)
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

        // 💡 [수정] bool 파라미터를 다시 받습니다. 기본값은 true
        public void LoadSceneAsyncByName(string sceneName, bool showLoadingPanel = true)
        {
            if (currentLoadingCoroutine != null)
            {
                StopCoroutine(currentLoadingCoroutine);
            }
            // 💡 코루틴에도 bool 값을 넘겨줍니다.
            currentLoadingCoroutine = StartCoroutine(LoadSceneCoroutine(sceneName, showLoadingPanel));
        }

        public void LoadSceneByPath_Editor(string scenePath, bool showLoadingPanel = true) // 💡 여기도 수정
        {
#if UNITY_EDITOR
            if (currentLoadingCoroutine != null)
            {
                StopCoroutine(currentLoadingCoroutine);
            }
            // 💡 코루틴에도 bool 값을 넘겨줍니다.
            currentLoadingCoroutine = StartCoroutine(LoadSceneCoroutine_Editor(scenePath, showLoadingPanel));
#else
            Debug.LogError("이 기능은 유니티 에디터에서만 사용할 수 있습니다!");
#endif
        }

        /// <summary>
        /// (빌드용) 비동기 씬 로딩 코루틴
        /// </summary>
        // 💡 [수정] bool 파라미터를 다시 받습니다.
        private IEnumerator LoadSceneCoroutine(string sceneName, bool showPanel)
        {
            float startTime = Time.time;

            // 💡 [수정] showPanel이 true일 때만 켜고 끕니다.
            if (loadingPanel != null && showPanel)
            {
                loadingPanel.SetActive(true);
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minLoadingTime)
            {
                // 💡 showPanel이 true (인트로가 보였을) 때만 최소 시간을 기다립니다.
                if (showPanel)
                {
                    yield return new WaitForSeconds(minLoadingTime - elapsedTime);
                }
            }

            asyncLoad.allowSceneActivation = true;
            yield return null;

            // 💡 [수정] showPanel이 true였을 때만 끕니다.
            if (loadingPanel != null && showPanel)
            {
                loadingPanel.SetActive(false);
            }
            currentLoadingCoroutine = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// (에디터용) 씬 로딩 코루틴
        /// </summary>
        private IEnumerator LoadSceneCoroutine_Editor(string scenePath, bool showPanel) // 💡 여기도 수정
        {
            float startTime = Time.time;

            if (loadingPanel != null && showPanel) // 💡 여기도 수정
                loadingPanel.SetActive(true);

            yield return null;

            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minLoadingTime)
            {
                if (showPanel) // 💡 여기도 수정
                    yield return new WaitForSeconds(minLoadingTime - elapsedTime);
            }

            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));

            yield return null;

            if (loadingPanel != null && showPanel) // 💡 여기도 수정
            {
                loadingPanel.SetActive(false);
            }
            currentLoadingCoroutine = null;
        }
#endif

        /// <summary>
        /// 씬 로드가 완료되었을 때 호출됩니다.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"'{scene.name}' 씬 로드가 완료되었습니다.");
        }
    }
}