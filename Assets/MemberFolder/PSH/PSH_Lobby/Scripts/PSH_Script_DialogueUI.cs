using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
// using PSH; // 네임스페이스가 PSH이므로 PSH using은 불필요
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


namespace PSH
{
    // KHS_Script_ResetController.cs 파일은 이 스크립트와 직접적인 관련이 없어 수정하지 않았습니다.
    // 이 스크립트는 PSH_Script_DialogueUI.cs를 수정한 버전입니다.

    public class PSH_Script_DialogueUI : MonoBehaviour, IPointerClickHandler
    {
        public static PSH_Script_DialogueUI Instance { get; private set; }

        [Header("UI")]
        [SerializeField] GameObject panelRoot;            // 대사 UI 루트
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

        // [추가] 시간이 멈추기 전의 원래 TimeScale을 저장하기 위한 변수
        private float originalTimeScale = 1.0f;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 시작할 때는 꺼져있도록 확실하게 처리
            if (panelRoot) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            // TODO: (이전 제안) 점수 이벤트 구독
            // ScoreManager.OnScoreUpdated += HandleScoreUpdated;
        }

        private void OnDisable()
        {
            // TODO: (이전 제안) 점수 이벤트 구독 해지
            // ScoreManager.OnScoreUpdated -= HandleScoreUpdated;
        }

        // [수정] Start()에서 바로 Play를 호출하면 눈 뜨는 애니메이션을 기다릴 수 없습니다.
        // 이 함수는 GameSceneManager 같은 다른 스크립트가
        // '눈 뜨는 애니메이션'이 끝난 후 호출해야 합니다.
        // public void Start()
        // {
        //     Play("Intro");
        // }

        public void Play(string cutsceneId)
        {
            Debug.Log($"--- Play('{cutsceneId}') ---");

            // [추가] 대화가 시작될 때 패널을 켜고 시간을 멈춥니다.
            if (panelRoot) panelRoot.SetActive(true);
            originalTimeScale = Time.timeScale; // 원래 시간 저장
            Time.timeScale = 0f; // 시간 정지

            currentCutsceneId = cutsceneId;
            currentLines = LoadLines(cutsceneId);

            if (currentLines.Count == 0)
            {
                Debug.LogError($"### Play 실패: '{cutsceneId}' 섹션 대사를 찾을 수 없음 ###");
                EndDialogue(); // 대사가 없으면 바로 종료
                return;
            }

            idx = 0;
            StartTyping(currentLines[idx]);
        }

        public void Update()
        {
            // '아무 키' (키보드 또는 마우스 클릭)가 눌렸는지 확인
            // 대화가 진행 중일 때만(idx != -1) 입력을 받도록 수정
            if (idx != -1 && Input.anyKeyDown)
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
                        EndDialogue();
                    }
                }
            }
        }

        public void OnPointerClick(PointerEventData _)
        {
            // 대화가 진행 중일 때만(idx != -1) 입력을 받도록 수정
            if (idx == -1 || currentLines.Count == 0) return;

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
                // 대화가 모두 끝남
                EndDialogue();
            }
        }

        // [추가] 대화 종료 처리를 위한 헬퍼 함수
        private void EndDialogue()
        {
            Debug.Log("대화 끝");

            // [추가] 시간을 원래대로 되돌립니다.
            Time.timeScale = originalTimeScale;

            // [추가] 패널을 닫습니다.
            transform.GetChild(0).gameObject.SetActive(false);
            if (panelRoot) panelRoot.SetActive(false);

            // 상태 초기화
            currentLines.Clear();
            idx = -1;
            currentCutsceneId = null;

            // (필요시 이곳에 씬 이동 등 다음 로직 추가)
            // if (currentCutsceneId == "Intro")
            // {
            //     GameManager.Instance.ChangeState(GameState.Playing);
            // }
            // else if (currentCutsceneId == "Ending")
            // {
            //     TransitionToGameScene();
            // }
        }


        // --- (LoadLines, StartTyping, TypeRoutine 등 나머지 함수는 변경 없음) ---
        // ... (이하 생략) ...

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
                // `Time.timeScale`에 영향을 받지 않는 `WaitForSecondsRealtime` (아주 잘 하셨습니다!)
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
            if (sceneLoader != null)
            {
                sceneLoader.LoadSceneAsyncByName(lobbySceneName);
                return;
            }

            if (!string.IsNullOrEmpty(lobbySceneName))
            {
                Debug.LogWarning("[DialogueUI] SceneLoader null → SceneManager.LoadScene 폴백");
                // [수정] ResetController를 보니 비동기가 아닌 동기 로드를 사용하시는 것 같아 맞춥니다.
                SceneManager.LoadScene(lobbySceneName);
            }
            else
            {
                Debug.LogError("[DialogueUI] lobbySceneName 비어있음. 인스펙터 확인 필요");
            }
        }
    }
}
