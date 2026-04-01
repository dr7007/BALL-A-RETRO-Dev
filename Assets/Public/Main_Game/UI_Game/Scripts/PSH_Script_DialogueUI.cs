using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization; // 🟢 [추가됨] Locale 클래스 사용을 위해 필요

namespace PSH
{
    public class PSH_Script_DialogueUI : MonoBehaviour, IPointerClickHandler
    {
        public static event Action<string> DialogueEvt;
        public static event Action<string> OnDialogueComplete;
        public static event Action<bool> DialogueWaitingEvt;

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
        [Tooltip("Localization 창에서 만든 String Table의 이름")]
        [SerializeField] string stringTableName = "DialogueTable"; 
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

        // 🟢 [추가됨] 스크립트가 활성화될 때 유니티 언어 변경 이벤트를 구독 (귀를 엶)
        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        // 🟢 [추가됨] 스크립트가 꺼지거나 파괴될 때 이벤트 구독 해제 (메모리 누수 방지)
        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        // 🟢 [추가됨] 게임 도중 드롭다운 등으로 시스템 언어가 바뀌면 자동으로 실행되는 함수
        private void OnLocaleChanged(Locale newLocale)
        {
            // 현재 대사창이 꺼져있거나 진행 중인 대사가 없다면 무시
            if (idx == -1 || string.IsNullOrEmpty(currentCutsceneId)) return;

            // 바뀐 언어로 대사 리스트를 백그라운드에서 다시 싹 불러옴
            currentLines = LoadLines(currentCutsceneId);

            // 데이터가 없거나 인덱스가 꼬였다면 안전하게 무시
            if (currentLines.Count == 0 || idx >= currentLines.Count) return;

            // 현재 한 글자씩 타이핑을 치고 있던 도중이라면?
            if (isTyping)
            {
                // 바뀐 언어로 처음부터 다시 찰진 타이핑 시작!
                StartTyping(currentLines[idx]);
            }
            else
            {
                // 타이핑은 다 끝났고 유저의 클릭을 기다리고 있던 상태라면?
                // 바뀐 언어의 완성된 문장으로 텍스트만 샥 바꿔줌
                if (currentActiveText != null)
                {
                    currentActiveText.text = currentLines[idx];
                }
            }
        }

        public void Play(string cutsceneId)
        {
            if (idx != -1) return;

            currentCutsceneId = cutsceneId;
            currentLines = LoadLines(cutsceneId);

            if (currentLines.Count == 0)
            {
                EndDialogue();
                return;
            }

            currentSprites = null;
            currentActiveImage = null;

            if (cutsceneId == "Intro")
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                currentActivePanel = introPanelRoot;
                currentActiveText = introText;
                currentActiveImage = introImage;
                currentSprites = introSprites;
                originalTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else if (cutsceneId == "Ending1")
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                currentActivePanel = endingPanelRoot;
                currentActiveText = endingText;
                currentActiveImage = endingImage;
                currentSprites = ending1Sprites;
            }
            else if (cutsceneId == "Ending2")
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                currentActivePanel = endingPanelRoot;
                currentActiveText = endingText;
                currentActiveImage = endingImage;
                currentSprites = ending2Sprites;
            }

            if (currentActivePanel != null && currentActiveText != null)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                currentActivePanel.SetActive(true);
                Canvas canvas = currentActivePanel.GetComponent<Canvas>();
                if (canvas != null) canvas.enabled = true;

                idx = 0;
                UpdateDialogueImage();
                StartTyping(currentLines[idx]);
            }
            else
            {
                Debug.LogError($"🚨 [DialogueUI] '{cutsceneId}'를 위한 패널이나 텍스트가 연결되지 않았습니다!");
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
            string finishedId = currentCutsceneId;

            if (finishedId == "Intro")
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                if (currentActivePanel) currentActivePanel.SetActive(false);
                Time.timeScale = originalTimeScale;
                DialogueEvt?.Invoke("Intro");
            }

            currentLines.Clear();
            currentSprites = null;
            currentActiveImage = null;
            currentActivePanel = null;
            currentActiveText = null;
            idx = -1;
            currentCutsceneId = null;

            OnDialogueComplete?.Invoke(finishedId);
            DialogueWaitingEvt?.Invoke(false);
        }

        private List<string> LoadLines(string id)
        {
            var result = new List<string>();
            var table = LocalizationSettings.StringDatabase.GetTable(stringTableName);
            
            if (table == null)
            {
                Debug.LogError($"🚨 [DialogueUI] '{stringTableName}' 테이블을 찾을 수 없습니다! 패키지 세팅을 확인하세요.");
                return result;
            }

            int index = 1;
            while (true)
            {
                string key = $"{id}_{index:D2}"; 
                var entry = table.GetEntry(key);
                
                if (entry != null && !string.IsNullOrEmpty(entry.LocalizedValue))
                {
                    result.Add(entry.LocalizedValue);
                    index++;
                }
                else
                {
                    break;
                }
            }

            if (result.Count == 0)
            {
                Debug.LogError($"🚨 [DialogueUI] '{stringTableName}' 테이블에서 '{id}_01' 로 시작하는 대사를 찾을 수 없습니다.");
            }
            
            return result;
        }

        private void StartTyping(string text) { if (typingCo != null) StopCoroutine(typingCo); typingCo = StartCoroutine(TypeRoutine(text)); }
        private IEnumerator TypeRoutine(string text) { isTyping = true; if (currentActiveText) { currentActiveText.text = ""; foreach (char c in text) { currentActiveText.text += c; yield return new WaitForSecondsRealtime(charInterval); } } isTyping = false; }
    }
}