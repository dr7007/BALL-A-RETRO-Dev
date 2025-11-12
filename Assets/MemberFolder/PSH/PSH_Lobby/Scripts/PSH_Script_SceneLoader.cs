using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.EventSystems.EventTrigger;

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

        [Tooltip("업데이트가 필요한 Screen Space - Camera 캔버스")]
        [SerializeField] private Canvas canvasToUpdate;

        // 💡 [신규] 카메라를 재설정할 로비 씬의 이름을 지정합니다.
        [Tooltip("카메라를 재설정할 로비 씬의 이름")]
        [SerializeField] private string lobbySceneName = "Main_Scene_Lobby"; // (씬 이름이 다르다면 인스펙터에서 변경)

        [Tooltip("로딩 UI를 최소한 보여줄 시간 (초)")]
        [SerializeField] private float minLoadingTime = 2.0f;

        public static PSH_Script_SceneLoader Instance { get; private set; }
        private Coroutine currentLoadingCoroutine = null;
        public static event Action OnSceneLoadStart;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

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

        public void LoadSceneAsyncByName(string sceneName, bool showLoadingPanel = true)
        {
            // 글리치 끄는 코드 (안전장치)
            GlitchEffect_RendererFeature.IsEnabled = false;
            OnSceneLoadStart?.Invoke(); // 씬 로드 시작 알림

            if (currentLoadingCoroutine != null)
            {
                StopCoroutine(currentLoadingCoroutine);
            }
            currentLoadingCoroutine = StartCoroutine(LoadSceneCoroutine(sceneName, showLoadingPanel));
        }

        public void LoadSceneByPath_Editor(string scenePath, bool showLoadingPanel = true)
        {
#if UNITY_EDITOR
            // 글리치 끄는 코드 (안전장치)
            GlitchEffect_RendererFeature.IsEnabled = false;
            OnSceneLoadStart?.Invoke(); // 씬 로드 시작 알림

            if (currentLoadingCoroutine != null)
            {
                StopCoroutine(currentLoadingCoroutine);
            }
            currentLoadingCoroutine = StartCoroutine(LoadSceneCoroutine_Editor(scenePath, showLoadingPanel));
#else
            Debug.LogError("이 기능은 유니티 에디터에서만 사용할 수 있습니다!");
#endif
        }

        private IEnumerator LoadSceneCoroutine(string sceneName, bool showPanel)
        {
            float startTime = Time.time;

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
                if (showPanel)
                {
                    yield return new WaitForSeconds(minLoadingTime - elapsedTime);
                }
            }

            asyncLoad.allowSceneActivation = true;
            yield return null;

            if (loadingPanel != null && showPanel)
            {
                loadingPanel.SetActive(false);
            }
            currentLoadingCoroutine = null;
        }


#if UNITY_EDITOR
        private IEnumerator LoadSceneCoroutine_Editor(string scenePath, bool showPanel)
        {
            float startTime = Time.time;

            if (loadingPanel != null && showPanel)
                loadingPanel.SetActive(true);

            yield return null;

            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minLoadingTime)
            {
                if (showPanel)
                    yield return new WaitForSeconds(minLoadingTime - elapsedTime);
            }

            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));

            yield return null;

            if (loadingPanel != null && showPanel)
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

            // 💡 [핵심 수정]
            // 로드된 씬이 '로비 씬'일 때만 카메라를 재설정합니다.
            if (canvasToUpdate != null && scene.name == lobbySceneName)
            {
                Camera newMainCamera = Camera.main;
                if (newMainCamera != null)
                {
                    canvasToUpdate.worldCamera = newMainCamera;
                    Debug.Log($"[SceneLoader] 캔버스({canvasToUpdate.name})의 카메라를 {newMainCamera.name}으로 재설정했습니다.");
                }
                else
                {
                    Debug.LogWarning($"[SceneLoader] '{lobbySceneName}' 씬에 'MainCamera' 태그가 달린 카메라가 없습니다!");
                }
            }
            else if (canvasToUpdate != null && scene.name != lobbySceneName)
            {
                // 💡 (선택사항) 로비가 아닌 씬(예: 게임 씬)에서는 캔버스를 꺼버릴 수도 있습니다.
                // canvasToUpdate.worldCamera = null; 
            }
        }
    }
}