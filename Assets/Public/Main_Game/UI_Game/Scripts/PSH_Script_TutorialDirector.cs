using UnityEngine;
using System.Collections;
using PSH;

namespace PSH
{
    public class PSH_Script_TutorialDirector : MonoBehaviour
    {
        [Header("Guide UI")]
        [Tooltip("화살표나 키보드 그림 같은 가이드 UI 연결")]
        [SerializeField] private GameObject guidePanel;

        private void OnEnable()
        {
            PSH_Script_DialogueUI.DialogueEvt += HandleDialogueEvent;
        }

        private void OnDisable()
        {
            PSH_Script_DialogueUI.DialogueEvt -= HandleDialogueEvent;
        }

        // 🟢 씬이 켜지자마자 실행되는 곳
        private void Start()
        {
            if (guidePanel) guidePanel.SetActive(false);

            // 안전하게 0.1초 뒤에 실행 (UI 초기화 대기)
            StartCoroutine(StartTutorialSequence());
        }

        private IEnumerator StartTutorialSequence()
        {
            yield return null; // 1프레임 대기

            // 📢 [여기입니다!] 시작하자마자 "Tutorial_Intro" 실행
            Debug.Log("튜토리얼 시작: Intro 대사 재생");
            PSH_Script_DialogueUI.Instance.Play("Tutorial_Intro");
        }


        // 🟢 대사가 끝날 때마다 다음 단계로 넘겨주는 곳
        private void HandleDialogueEvent(string cutsceneId)
        {
            // 1. "Intro" 대사가 끝났으면 -> "Flipper" 설명 재생
            if (cutsceneId == "Tutorial_Intro")
            {
                Debug.Log("Intro 끝남 -> Flipper 설명 재생");
                PSH_Script_DialogueUI.Instance.Play("Tutorial_Flipper");
            }
            // 2. "Flipper" 대사가 끝났으면 -> "Space" 설명 재생
            else if (cutsceneId == "Tutorial_Flipper")
            {
                Debug.Log("Flipper 설명 끝남 -> Space 설명 재생");
                PSH_Script_DialogueUI.Instance.Play("Tutorial_Space");
            }
            // 3. "Space" 설명까지 다 끝났으면 -> 진짜 게임(조작) 시작
            else if (cutsceneId == "Tutorial_Space")
            {
                Debug.Log("모든 설명 끝! -> 가이드 UI 켜고 조작 시작");
                
                if (guidePanel) guidePanel.SetActive(true);
                
                // 여기에 조작 락 해제 코드 추가
                // PlayerController.Instance.CanMove = true;
            }
        }
    }
}