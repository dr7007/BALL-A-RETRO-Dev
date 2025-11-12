using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;

namespace PSH
{
    public class PSH_Script_DialogueUI : MonoBehaviour, IPointerClickHandler
    {
        public static event Action<string> DialogueEvt;
        public static event Action<string> OnDialogueComplete;

        public static PSH_Script_DialogueUI Instance { get; private set; }

        [Header("Intro UI Set")]
        [SerializeField] GameObject introPanelRoot;
        [SerializeField] TMP_Text introText;
        [Tooltip("인트로 대사 진행 시 이미지가 바뀔 타겟 Image 컴포넌트")]
        [SerializeField] Image introImage;

        [Header("Ending UI Set")]
        [SerializeField] GameObject endingPanelRoot;
        [SerializeField] TMP_Text endingText;
        [Tooltip("엔딩 대사 진행 시 이미지가 바뀔 타겟 Image 컴포넌트")]
        [SerializeField] Image endingImage;

        [Header("Dialogue Sprites")]
        [Tooltip("Intro 대사 순서에 맞춰 보여줄 스프라이트들")]
        [SerializeField] private List<Sprite> introSprites = new List<Sprite>();
        [Tooltip("Ending1(클리어) 대사 순서에 맞춰 보여줄 스프라이트들")]
        [SerializeField] private List<Sprite> ending1Sprites = new List<Sprite>();
        [Tooltip("Ending2(오버) 대사 순서에 맞춰 보여줄 스프라이트들")]
        [SerializeField] private List<Sprite> ending2Sprites = new List<Sprite>();

        [Header("Data")]
        [SerializeField] string csvFileName = "dialogues";
        [SerializeField] float charInterval = 0.04f;



        private List<string> currentLines = new();
        private List<Sprite> currentSprites = null;
        private Image currentActiveImage = null;

        private int idx = -1;
        private bool isTyping = false;
        private Coroutine typingCo;
        private string currentCutsceneId;
        private float originalTimeScale = 1.0f;

        private GameObject currentActivePanel;
        private TMP_Text currentActiveText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (introPanelRoot) introPanelRoot.SetActive(false);
            if (endingPanelRoot) endingPanelRoot.SetActive(false);
        }

        public void Play(string cutsceneId)
        {
            if (idx != -1) return;

            currentCutsceneId = cutsceneId;
            currentLines = LoadLines(cutsceneId);

            if (currentLines.Count == 0)
            {
                // 🚨 LoadLines에서 이미 자세한 에러를 출력했으므로 여기선 종료만 합니다.
                EndDialogue();
                return;
            }

            // 초기화
            currentSprites = null;
            currentActiveImage = null;

            if (cutsceneId == "Intro")
            {
                currentActivePanel = introPanelRoot;
                currentActiveText = introText;
                currentActiveImage = introImage;
                currentSprites = introSprites;
                originalTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else if (cutsceneId == "Ending1")
            {
                currentActivePanel = endingPanelRoot;
                currentActiveText = endingText;
                currentActiveImage = endingImage;
                currentSprites = ending1Sprites;
            }
            else if (cutsceneId == "Ending2")
            {
                currentActivePanel = endingPanelRoot;
                currentActiveText = endingText;
                currentActiveImage = endingImage;
                currentSprites = ending2Sprites;
            }
            //else if(cutsceneId == "Round1")
            //{

            //}
            if (currentActivePanel != null && currentActiveText != null)
            {
                currentActivePanel.SetActive(true);
                // 혹시 꺼져있을 Canvas 컴포넌트 강제 활성화
                Canvas canvas = currentActivePanel.GetComponent<Canvas>();
                if (canvas != null) canvas.enabled = true;

                idx = 0;
                UpdateDialogueImage();
                StartTyping(currentLines[idx]);
            }
            else
            {
                Debug.LogError($"🚨 [DialogueUI] '{cutsceneId}'를 위한 패널이나 텍스트가 인스펙터에 연결되지 않았습니다!");
                EndDialogue();
            }
        }

        public void Update()
        {
            if (idx != -1 && Input.anyKeyDown)
            {
                if (currentLines.Count == 0) return;
                if (isTyping) { StopCoroutine(typingCo); if (currentActiveText) currentActiveText.text = currentLines[idx]; isTyping = false; }
                else { idx++; if (idx < currentLines.Count) { UpdateDialogueImage(); StartTyping(currentLines[idx]); } else EndDialogue(); }
            }
        }

        public void OnPointerClick(PointerEventData _)
        {
            if (idx == -1 || currentLines.Count == 0) return;
            if (isTyping) { StopCoroutine(typingCo); if (currentActiveText) currentActiveText.text = currentLines[idx]; isTyping = false; }
            else { idx++; if (idx < currentLines.Count) { UpdateDialogueImage(); StartTyping(currentLines[idx]); } else EndDialogue(); }
        }

        private void UpdateDialogueImage()
        {
            if (currentActiveImage != null && currentSprites != null && currentSprites.Count > idx)
            {
                if (currentSprites[idx] != null)
                {
                    currentActiveImage.sprite = currentSprites[idx];
                    currentActiveImage.enabled = true;
                }
            }
        }

        private void EndDialogue()
        {
            // 💡 [수정] 대사가 끝난 ID를 먼저 저장합니다.
            string finishedId = currentCutsceneId;

            // 💡 [수정] "Intro"일 때만 패널을 끄고, "Ending1/2"일 때는 끄지 않습니다.
            if (finishedId == "Intro")
            {
                if (currentActivePanel) currentActivePanel.SetActive(false);
                Time.timeScale = originalTimeScale;
                DialogueEvt?.Invoke("Intro");
            }
            // (else: "Ending1" or "Ending2" - 패널을 끄지 않고 놔둡니다.)

            // 상태 초기화
            currentLines.Clear();
            currentSprites = null;
            currentActiveImage = null;
            currentActivePanel = null;
            currentActiveText = null;
            idx = -1;
            currentCutsceneId = null;
  

            // 감독에게 보고 (인트로가 끝났을 때도 보고는 해야 함)
            OnDialogueComplete?.Invoke(finishedId);

        }

        // 🔍 [강력해진 CSV 로더]
        private List<string> LoadLines(string id)
        {
            var result = new List<string>();
            TextAsset ta = Resources.Load<TextAsset>(csvFileName);
            if (ta == null)
            {
                Debug.LogError($"🚨 [DialogueUI] Resources 폴더에 '{csvFileName}' 파일이 없습니다!");
                return result;
            }

            var raw = ta.text;
            if (raw.StartsWith("\uFEFF")) raw = raw.Substring(1); // BOM 제거
            raw = raw.Replace("\r\n", "\n").Replace("\r", "\n"); // 줄바꿈 통일

            var rows = raw.Split('\n');
            HashSet<string> foundKeys = new HashSet<string>(); // 디버깅용 키 목록

            for (int i = 0; i < rows.Length; i++)
            {
                string row = rows[i];
                if (string.IsNullOrWhiteSpace(row)) continue;

                var fields = ParseCsvRow(row);
                if (fields.Count < 2) continue;

                string key = fields[0].Trim();
                foundKeys.Add(key); // 발견된 키 기록

                // 대소문자 구분 없이 비교 (Intro == intro)
                if (key.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    string line = fields[1].Trim();
                    // 엑셀이 만든 이상한 따옴표 제거 (예: "대사"" " -> 대사)
                    if (line.StartsWith("\""))
                    {
                        // 1. 맨 앞 따옴표 제거
                        line = line.Substring(1);
                        // 2. 맨 뒤에 있는 따옴표와 공백들 제거
                        int lastQuoteIndex = line.LastIndexOf('"');
                        if (lastQuoteIndex != -1)
                        {
                            line = line.Substring(0, lastQuoteIndex);
                        }
                        // 3. 이중 따옴표("")를 단일 따옴표(")로 복구
                        line = line.Replace("\"\"", "\"");
                    }

                    if (!string.IsNullOrEmpty(line)) result.Add(line);
                }
            }

            if (result.Count == 0)
            {
                // 🚨 범인 색출: 도대체 파일에 어떤 키값들이 있었는지 콘솔에 다 불어버립니다.
                string keysList = string.Join(", ", foundKeys);
                Debug.LogError($"🚨 [DialogueUI] '{id}' 대사를 한 줄도 못 찾았습니다!");
                Debug.LogError($"🔎 CSV 파일에서 발견된 키 목록: [{keysList}]");
                Debug.LogError("👉 팁: 위 목록에 'Intro'가 없다면 CSV 파일 인코딩이 깨졌거나 오타가 있는 것입니다.");
            }
            else
            {
                Debug.Log($"✅ [DialogueUI] '{id}' 대사 로드 성공: 총 {result.Count}줄");
            }

            return result;
        }

        // 쉼표가 들어간 대사도 찰떡같이 읽는 파서
        private List<string> ParseCsvRow(string row)
        {
            var list = new List<string>();
            bool insideQuotes = false;
            string currentField = "";
            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                if (c == '"') { insideQuotes = !insideQuotes; currentField += c; }
                else if (c == ',' && !insideQuotes) { list.Add(currentField); currentField = ""; }
                else { currentField += c; }
            }
            list.Add(currentField);
            return list;
        }

        private void StartTyping(string text) { if (typingCo != null) StopCoroutine(typingCo); typingCo = StartCoroutine(TypeRoutine(text)); }
        private IEnumerator TypeRoutine(string text) { isTyping = true; if (currentActiveText) { currentActiveText.text = ""; foreach (char c in text) { currentActiveText.text += c; yield return new WaitForSecondsRealtime(charInterval); } } isTyping = false; }
    }
}