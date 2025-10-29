using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using PSH;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


namespace PSH
{
    public class PSH_Script_DialogueUI : MonoBehaviour, IPointerClickHandler
    {

        public static PSH_Script_DialogueUI Instance { get; private set; }

        [Header("UI")]
        [SerializeField] GameObject panelRoot;          // 대사 UI 루트
        [SerializeField] TMP_Text dialogueText;

        [Header("Data")]
        [SerializeField] string csvFileName = "dialogues";
        [SerializeField] float charInterval = 0.04f;
        [SerializeField] private string lobbySceneName = "PSH_Scene_Lobby";
        [SerializeField] private PSH_Script_SceneLoader sceneLoader;

        // 내부 상태
        private List<string> currentLines = new();
        private int idx = -1;
        private bool isTyping = false;
        private Coroutine typingCo;
        private string currentCutsceneId;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[DialogueUI] Duplicate detected and destroyed. Kept: {Instance.gameObject.name}, Destroyed: {gameObject.name}");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            //if (panelRoot) panelRoot.SetActive(false);

            //// SceneLoader 자동 연결(비활성 포함)
            //if (sceneLoader == null)
            //{
            //    sceneLoader = FindObjectOfType<PSH_Script_SceneLoader>(true);
            //    if (sceneLoader != null)
            //        Debug.Log($"[DialogueUI] SceneLoader auto-wired -> {sceneLoader.gameObject.name}");
            //    else
            //        Debug.LogWarning("[DialogueUI] SceneLoader not found. Will fallback to SceneManager.");
            //}

            //Debug.Log($"[DialogueUI] Active Instance: {gameObject.name} (scene: {gameObject.scene.name})");
        }

        private void OnEnable()
        {
            Debug.Log("<color=yellow>DialogueUI [OnEnable]:</color> GameManager 방송 구독");
            // GameManager.OnStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            Debug.Log("<color=orange>DialogueUI [OnDisable]:</color> GameManager 방송 구독 해지");
            // GameManager.OnStateChanged -= HandleGameStateChanged;
        }

        //private void HandleGameStateChanged(GameState state)
        //{
        //    Debug.Log($"<color=cyan>DialogueUI:</color> state = {state}");

        //    if (state == GameState.Intro || state == GameState.GameEnding)
        //    {
        //        if (panelRoot) panelRoot.SetActive(true);
        //        Play(state == GameState.Intro ? "Intro" : "Ending");
        //    }
        //    else
        //    {
        //        if (panelRoot) panelRoot.SetActive(false);
        //    }
        //}
        public void Start()
        {
            Play("Intro");
        }
        public void Play(string cutsceneId)
        {
            Debug.Log($"--- Play('{cutsceneId}') ---");
            currentCutsceneId = cutsceneId;
            currentLines = LoadLines(cutsceneId);

            if (currentLines.Count == 0)
            {
                Debug.LogError($"### Play 실패: '{cutsceneId}' 섹션 대사를 찾을 수 없음 ###");
                return;
            }

            idx = 0;
            StartTyping(currentLines[idx]);
        }
        public void Update()
        {
            // '아무 키' (키보드 또는 마우스 클릭)가 눌렸는지 확인
            if (Input.anyKeyDown)
            {
                // 로드된 대사가 없으면 무시
                if (currentLines.Count == 0) return;

                // 1. 타이핑 중일 때 -> 타이핑 스킵
                if (isTyping)
                {
                    StopCoroutine(typingCo);
                    dialogueText.text = currentLines[idx];
                    isTyping = false;
                }
                // 2. 타이핑이 끝났을 때 -> 다음 대사로
                else
                {
                    idx++;
                    if (idx < currentLines.Count)
                    {
                        StartTyping(currentLines[idx]);
                    }
                    else
                    {
                        // 대화가 모두 끝남
                        Debug.Log("대화 끝");
                        // (필요시 이곳에 씬 이동 등 다음 로직 추가)
                        // 예: TransitionToGameScene();
                    }
                }
            }
        }
        private List<string> LoadLines(string id)
        {
            var result = new List<string>();
            TextAsset ta = Resources.Load<TextAsset>(csvFileName);
            if (ta == null)
            {
                Debug.LogError($"### LoadLines 실패: Resources/{csvFileName}.csv 없음 ###");
                return result;
            }

            var raw = RemoveBom(ta.text).Replace("\r", "");
            var rows = raw.Split('\n');
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row)) continue;
                var fields = row.Split(',');
                if (fields.Length < 2) continue;

                string cutsceneId = fields[0].Trim();
                string line = fields[1].Trim().Trim('"');

                if (cutsceneId == id && !string.IsNullOrEmpty(line))
                    result.Add(line);
            }
            return result;
        }

        public void OnPointerClick(PointerEventData _)
        {

            if (currentLines.Count == 0) return;

            if (isTyping)
            {
                StopCoroutine(typingCo);
                dialogueText.text = currentLines[idx];
                isTyping = false;
                return;
            }

            idx++;
            if (idx < currentLines.Count)
            {
                StartTyping(currentLines[idx]);
            }
            else
            {
                //// 대사 끝난 지점에서 분기
                //if (currentCutsceneId == "Intro")
                //{
                //    GameManager.Instance.ChangeState(GameState.Playing);
                //}
                //else if (currentCutsceneId == "Ending")
                //{
                //    ShowEndingChoices();
                //}
                //currentLines.Clear();
            }
        }



        private void StartTyping(string text)
        {
            if (typingCo != null) StopCoroutine(typingCo);
            typingCo = StartCoroutine(TypeRoutine(text));
        }

        private IEnumerator TypeRoutine(string text)
        {
            isTyping = true;
            dialogueText.text = "";
            foreach (char c in text.Replace("\r", ""))
            {
                dialogueText.text += c;
                yield return new WaitForSecondsRealtime(charInterval);
            }
            isTyping = false;
        }

        private string RemoveBom(string s)
        {
            if (!string.IsNullOrEmpty(s) && s[0] == '\uFEFF') return s.Substring(1);
            return s;
        }

        public static void HideGameSceneCanvas()
        {
            GameObject canvasObject = GameObject.Find("Canvas_GameScene");
            if (canvasObject != null)
            {
                canvasObject.SetActive(false);
                Debug.Log("Canvas_GameScene 비활성화");
            }
            else
            {
                Debug.LogWarning("'Canvas_GameScene' 오브젝트를 찾지 못함");
            }
        }

        private void TransitionToGameScene()
        {
            // 다음 씬에서 Intro로 시작하도록 상태 지정
            //  GameManager.StateForNextScene = GameState.Intro;

            if (sceneLoader != null)
            {
                sceneLoader.LoadSceneAsyncByName(lobbySceneName);
                return;
            }

            // 폴백: SceneManager 직접 로드
            if (!string.IsNullOrEmpty(lobbySceneName))
            {
                Debug.LogWarning("[DialogueUI] SceneLoader null → SceneManager.LoadScene 폴백");
                SceneManager.LoadScene(lobbySceneName);
            }
            else
            {
                Debug.LogError("[DialogueUI] lobbySceneName 비어있음. 인스펙터 확인 필요");
            }
        }
    }

}