using System;
using UnityEngine;
using PSH; // DialogueUI 이벤트를 받기 위해

namespace PSH
{
    /// <summary>
    /// 게임 씬의 시작 시퀀스를 지휘합니다. (예: 인트로 애니메이션 -> 대사 시작 -> 메인 UI 켜기)
    /// </summary>
    public class PSH_Script_GameSceneDirector : MonoBehaviour
    {
        public static event Action<PSH_Script_DialogueUI> OpeningEyeEvt;
        public static event Action NoIntroStartEvt;

        [Header("Scene Objects")]
        [Tooltip("눈 뜨는 효과 오브젝트")]
        [SerializeField] private PSH_Script_EyeOpenEffect eyeOpenEffect;
        [Tooltip("게임 시작 후 켜질 메인 게임 캔버스 (HUD)")]
        [SerializeField] private GameObject mainGameCanvas;

        // 이 씬의 인트로가 이미 한 번 재생되었는지 확인하는 static 변수
        private static bool hasPlayedIntro = false;

        public static void ResetIntroFlag()
        {
            hasPlayedIntro = false;
            Debug.Log("[GameSceneDirector] 인트로 플래그 리셋 완료.");
        }

        private void Awake()
        {
            // 게임 시작 전에는 메인 캔버스를 확실하게 꺼둡니다.
            if (mainGameCanvas != null)
            {
                mainGameCanvas.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // 인트로 대사가 끝나는 타이밍을 알기 위해 구독
            PSH_Script_DialogueUI.DialogueEvt += HandleDialogueEvent;
        }

        private void OnDisable()
        {
            PSH_Script_DialogueUI.DialogueEvt -= HandleDialogueEvent;
        }

        private void Start()
        {
            PSH_Script_DialogueUI dialogueUI = PSH_Script_DialogueUI.Instance;

            if (dialogueUI == null) Debug.LogError("GameSceneDirector: DialogueUI Instance 없음!");
            if (eyeOpenEffect == null) Debug.LogError("GameSceneDirector: EyeOpenEffect 할당 안됨!");

            if (hasPlayedIntro)
            {
                // --- [케이스 1] 인트로 스킵 (재시도 등) ---
                Debug.Log("[GameSceneDirector] 인트로 스킵 -> 바로 게임 시작");

                // 1. 연출용 오브젝트 정리
                if (eyeOpenEffect)
                {
                    eyeOpenEffect.SetOpenImmediate();
                    eyeOpenEffect.gameObject.SetActive(false);
                }

                // 2. 메인 게임 캔버스 켜기 (가장 중요!)
                if (mainGameCanvas != null)
                {
                    mainGameCanvas.SetActive(true);
                }

                // 3. 이벤트 전파
                NoIntroStartEvt?.Invoke();
            }
            else
            {
                // --- [케이스 2] 인트로 재생 시작 ---
                Debug.Log("[GameSceneDirector] 인트로 시퀀스 시작");
                hasPlayedIntro = true;

                // 눈 뜨기 효과 시작 -> 완료되면 DialogueUI가 이어받음
                if (eyeOpenEffect)
                {
                    eyeOpenEffect.OnEyeOpenComplete += () => {
                        OpeningEyeEvt?.Invoke(dialogueUI);
                    };
                    eyeOpenEffect.BeginAnimation();
                }
            }
        }

        // DialogueUI로부터 대사가 끝났다는 신호를 받음
        private void HandleDialogueEvent(string cutsceneId)
        {
            // 인트로 대사가 끝났다면 메인 캔버스를 켭니다.
            if (cutsceneId == "Intro")
            {
                Debug.Log("[GameSceneDirector] 인트로 대사 종료 -> 메인 게임 캔버스 ON");
                if (mainGameCanvas != null)
                {
                    mainGameCanvas.SetActive(true);
                }
            }
        }
    }
}