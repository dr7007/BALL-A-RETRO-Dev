using UnityEngine;
using TMPro;
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

        [Header("Ending UI Set")]
        [SerializeField] GameObject endingPanelRoot;
        [SerializeField] TMP_Text endingText;

        [Header("Data")]
        [SerializeField] string csvFileName = "dialogues";
        [SerializeField] float charInterval = 0.04f;

        private List<string> currentLines = new();
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
            Debug.Log($"[DialogueUI] Play 요청 받음: ID = {cutsceneId}");

            if (idx != -1)
            {
                Debug.LogWarning($"[DialogueUI] 이미 대화({currentCutsceneId})가 진행 중이라 요청 무시됨.");
                return;
            }

            currentCutsceneId = cutsceneId;
            currentLines = LoadLines(cutsceneId);

            if (currentLines.Count == 0)
            {
                Debug.LogError($"🚨 [DialogueUI] CSV에서 ID '{cutsceneId}'에 해당하는 대사를 찾을 수 없습니다! CSV 파일을 확인하세요.");
                EndDialogue();
                return;
            }

            Debug.Log($"[DialogueUI] 대사 로드 성공. 총 {currentLines.Count}줄.");

            // 패널 선택 로직
            if (cutsceneId == "Intro")
            {
                currentActivePanel = introPanelRoot;
                currentActiveText = introText;
                originalTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                currentActivePanel = endingPanelRoot;
                currentActiveText = endingText;
            }

            // 패널 활성화 시도
            if (currentActivePanel != null && currentActiveText != null)
            {
                Debug.Log($"[DialogueUI] 패널 활성화: {currentActivePanel.name}");
                currentActivePanel.SetActive(true);
                idx = 0;
                StartTyping(currentLines[idx]);
            }
            else
            {
                Debug.LogError($"🚨 [DialogueUI] 사용할 패널이나 텍스트가 연결되지 않았습니다! ID: {cutsceneId}, 인스펙터 확인 요망.");
                // 강제 종료 처리
                EndDialogue();
            }
        }

        public void Update()
        {
            if (idx != -1 && Input.anyKeyDown)
            {
                if (currentLines.Count == 0) return;
                if (isTyping) { StopCoroutine(typingCo); if (currentActiveText) currentActiveText.text = currentLines[idx]; isTyping = false; }
                else { idx++; if (idx < currentLines.Count) StartTyping(currentLines[idx]); else EndDialogue(); }
            }
        }

        public void OnPointerClick(PointerEventData _)
        {
            if (idx == -1 || currentLines.Count == 0) return;
            if (isTyping) { StopCoroutine(typingCo); if (currentActiveText) currentActiveText.text = currentLines[idx]; isTyping = false; }
            else { idx++; if (idx < currentLines.Count) StartTyping(currentLines[idx]); else EndDialogue(); }
        }

        private void EndDialogue()
        {
            Debug.Log($"[DialogueUI] 대화 종료: {currentCutsceneId}");

            if (currentActivePanel) currentActivePanel.SetActive(false);

            string finishedId = currentCutsceneId;
            currentLines.Clear();
            idx = -1;
            currentCutsceneId = null;
            currentActivePanel = null;
            currentActiveText = null;

            if (finishedId == "Intro")
            {
                Time.timeScale = originalTimeScale;
                DialogueEvt?.Invoke("Intro");
            }

            OnDialogueComplete?.Invoke(finishedId);
        }

        private List<string> LoadLines(string id)
        {
            var result = new List<string>();
            TextAsset ta = Resources.Load<TextAsset>(csvFileName);
            if (ta == null) return result;
            var raw = ta.text;
            if (raw.StartsWith("\uFEFF")) raw = raw.Substring(1);
            raw = raw.Replace("\r\n", "\n").Replace("\r", "\n");

            var rows = raw.Split('\n');
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row)) continue;
                var fields = row.Split(',');
                if (fields.Length < 2) continue;

                if (fields[0].Trim() == id)
                {
                    string line = fields[1].Trim();
                    if (line.StartsWith("\"") && line.EndsWith("\""))
                        line = line.Substring(1, line.Length - 2);
                    line = line.Replace("\"\"", "\"");
                    if (!string.IsNullOrEmpty(line)) result.Add(line);
                }
            }
            return result;
        }

        private void StartTyping(string text) { if (typingCo != null) StopCoroutine(typingCo); typingCo = StartCoroutine(TypeRoutine(text)); }

        private IEnumerator TypeRoutine(string text)
        {
            isTyping = true;
            if (currentActiveText)
            {
                currentActiveText.text = "";
                foreach (char c in text)
                {
                    currentActiveText.text += c;
                    yield return new WaitForSecondsRealtime(charInterval);
                }
            }
            isTyping = false;
        }
    }
}