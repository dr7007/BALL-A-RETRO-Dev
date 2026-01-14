using System;
using UnityEngine;
using PSH;
using System.Collections;

namespace PSH
{
    public class PSH_Script_GameSceneDirector : MonoBehaviour
    {
        public static event Action<PSH_Script_DialogueUI> OpeningEyeEvt;
        public static event Action NoIntroStartEvt;

        [Header("Scene Mode")]
        [Tooltip("튜토리얼 씬이라면 이 체크박스를 체크하세요! 인트로가 안 나옵니다.")]
        [SerializeField] private bool isTutorialScene = false; // 👈 새로 추가됨

        [Header("Scene Objects")]
        [SerializeField] private PSH_Script_EyeOpenEffect eyeOpenEffect;
        [SerializeField] private GameObject eyeopenOBJ;
        
        [Header("Settings")]
        [SerializeField] private int repeatCount = 7;
        [SerializeField] private float interval = 0.1f;

        private static bool hasPlayedIntro = false;

        public static void ResetIntroFlag()
        {
            hasPlayedIntro = false;
        }

        private void OnEnable()
        {
            PSH_Script_DialogueUI.DialogueEvt += HandleDialogueEvent;
        }

        private void OnDisable()
        {
            PSH_Script_DialogueUI.DialogueEvt -= HandleDialogueEvent;
        }

        private void Start()
        {
            PSH_Script_DialogueUI dialogueUI = PSH_Script_DialogueUI.Instance;

           // 1. 튜토리얼 씬일 경우 (체크박스 V)
            if (isTutorialScene)
            {
                Debug.Log("[Director] 튜토리얼 모드 진입");

                // 방해되는 연출 끄기
                if (eyeOpenEffect) eyeOpenEffect.gameObject.SetActive(false);
                if (eyeopenOBJ) eyeopenOBJ.gameObject.SetActive(false);

                // ★ [핵심] 튜토리얼 첫 대사 실행! (CSV 키값: Tutorial_Intro)
                dialogueUI.Play("Tutorial_Intro"); 
                
                return; // 여기서 종료 (아래 인트로 로직 실행 X)
            }

            // 2. 인트로를 이미 본 경우 (게임 씬 재진입 등)
            if (hasPlayedIntro)
            {
                Debug.Log("[Director] 인트로 스킵 -> 게임 시작");
                if (eyeOpenEffect)
                {
                    eyeOpenEffect.SetOpenImmediate();
                    eyeOpenEffect.gameObject.SetActive(false);
                    eyeopenOBJ.gameObject.SetActive(false);
                }
                StartCoroutine(RepeatEventCoroutine());
            }
            // 3. 진짜 처음 (인트로 재생)
            else
            {
                Debug.Log("[Director] 인트로 시퀀스 시작");
                hasPlayedIntro = true;

                if (eyeOpenEffect)
                {
                    eyeOpenEffect.OnEyeOpenComplete += () => {
                        OpeningEyeEvt?.Invoke(dialogueUI);
                    };
                    eyeOpenEffect.BeginAnimation();
                }
            }
        }

        private void HandleDialogueEvent(string cutsceneId)
        {
            if (cutsceneId == "Intro")
            {
                eyeopenOBJ.gameObject.SetActive(false);
            }
            // ★ [추가] 튜토리얼 대사가 끝났을 때의 행동 정의
            else if (cutsceneId == "Tutorial_Intro")
            {
                Debug.Log("튜토리얼 첫 대사 끝! -> 이제 조작 가이드 활성화");
                
                // 예시: 플레이어 움직임 락 해제하기
                // PlayerController.Instance.CanMove = true; 
                
                // 예시: 다음 목표 지점 화살표 켜기
                // tutorialGuideArrow.SetActive(true);
            }
        }
    
        private IEnumerator RepeatEventCoroutine()
        {
            for (int i = 0; i < repeatCount; i++)
            {
                NoIntroStartEvt?.Invoke();
                yield return new WaitForSeconds(interval);
            }
        }
    }
}