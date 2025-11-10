using System;
using UnityEngine;

namespace PSH
{
    /// <summary>
    /// 게임 씬의 시작 시퀀스를 지휘합니다. (예: 인트로 애니메이션 -> 대사 시작)
    /// </summary>
    public class PSH_Script_GameSceneDirector : MonoBehaviour
    {
        public static event Action<PSH_Script_DialogueUI> OpeningEyeEvt;
        public static event Action NoIntroStartEvt;

        [SerializeField] private PSH_Script_EyeOpenEffect eyeOpenEffect; // 인스펙터에서 할당

        // 이 씬의 인트로가 이미 한 번 재생되었는지 확인하는 static 변수
        private static bool hasPlayedIntro = false;

        /// <summary>
        /// '인트로를 봤음' 플래그를 리셋합니다. 
        /// 로비로 돌아갈 때 호출해야 합니다.
        /// </summary>
        public static void ResetIntroFlag()
        {
            hasPlayedIntro = false;
            Debug.Log("GameSceneDirector: 인트로 플래그 리셋 완료.");
        }
        private void Start()
        {
            // PSH_Script_DialogueUI의 싱글톤 인스턴스를 찾음
            PSH_Script_DialogueUI dialogueUI = PSH_Script_DialogueUI.Instance;

            if (dialogueUI == null)
            {
                Debug.LogError("GameSceneDirector: PSH_Script_DialogueUI의 Instance를 찾을 수 없습니다.");
                return;
            }

            if (eyeOpenEffect == null)
            {
                Debug.LogError("GameSceneDirector: EyeOpenEffect가 인스펙터에 할당되지 않았습니다.");
                return;
            }


            if (hasPlayedIntro)
            {
                // --- 이미 인트로를 본 경우 (예: 죽어서 씬을 다시 로드) ---

                // 눈을 즉시 뜬 상태로 설정
                eyeOpenEffect.SetOpenImmediate();
                eyeOpenEffect.gameObject.SetActive(false);
                dialogueUI.gameObject.SetActive(false);
                NoIntroStartEvt.Invoke();

                // (필요하다면) 인트로 대사 없이 바로 게임 시작 로직 호출
            }
            else
            {
                // --- 씬에 처음 진입한 경우 ---
                hasPlayedIntro = true;

                // 1. EyeOpenEffect의 '애니메이션 완료' 방송을 '구독'합니다.
                //    "애니메이션이 끝나면, StartIntroDialogue 함수를 실행해줘"
                eyeOpenEffect.OnEyeOpenComplete += () => {
                    OpeningEyeEvt.Invoke(dialogueUI);
                };

                // 2. 애니메이션 시작을 '명령'합니다.
                eyeOpenEffect.BeginAnimation();
            }
        }
    }
}
