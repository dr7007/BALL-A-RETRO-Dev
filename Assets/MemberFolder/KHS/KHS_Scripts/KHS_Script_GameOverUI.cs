using PSH;
using System;
using UnityEngine;

public class KHS_Script_GameOverUI : MonoBehaviour
{
    [Header("Final UI Panels")]
    [SerializeField] private GameObject gameOverUIObj;
    [SerializeField] private GameObject gameClearUIObj;

    private Canvas gameUICanvas;
    // isReset이 false면 "아직 인트로를 안 봤거나 진행 중"이라는 뜻입니다.
    private bool isIntroSkippedOrFinished = false;

    private void Awake()
    {
        gameUICanvas = GetComponent<Canvas>();
        if (gameUICanvas == null)
        {
            // 만약 이 스크립트가 Canvas 컴포넌트와 같은 오브젝트에 없다면, 
            // 부모나 자식에서 찾아보는 시도를 할 수도 있습니다.
            gameUICanvas = GetComponentInParent<Canvas>();
        }
    }

    private void Start()
    {
        // 씬 시작 시, 일단 기본 상태(인트로 중이면 꺼짐, 아니면 켜짐)로 설정
        UpdateGameCanvasState();

        if (gameOverUIObj) gameOverUIObj.SetActive(false);
        if (gameClearUIObj) gameClearUIObj.SetActive(false);
    }

    private void OnEnable()
    {
        PSH_Script_GameSceneDirector.NoIntroStartEvt += HandleNoIntroStart;
        // Intro 대사가 끝났을 때 캔버스를 켜기 위해 구독
        PSH_Script_DialogueUI.DialogueEvt += HandleDialogueEvent;
    }

    private void OnDisable()
    {
        PSH_Script_GameSceneDirector.NoIntroStartEvt -= HandleNoIntroStart;
        PSH_Script_DialogueUI.DialogueEvt -= HandleDialogueEvent;
    }

    // 💡 [감독 전용] 공개 함수
    public void ShowGameOverPanel()
    {
        Debug.Log("[GameOverUI] Game Over Panel ON");
        if (gameUICanvas) gameUICanvas.enabled = false; // 최종 결과 때는 메인 UI 숨김 (선택사항)
        if (gameOverUIObj) gameOverUIObj.SetActive(true);
    }

    // 💡 [감독 전용] 공개 함수
    public void ShowGameClearPanel()
    {
        Debug.Log("[GameOverUI] Game Clear Panel ON");
        if (gameUICanvas) gameUICanvas.enabled = false; // 최종 결과 때는 메인 UI 숨김 (선택사항)
        if (gameClearUIObj) gameClearUIObj.SetActive(true);
    }

    // "인트로 없이 시작" (재시도 등) 이벤트 핸들러
    private void HandleNoIntroStart()
    {
        Debug.Log("[GameOverUI] 인트로 스킵 확인 -> 게임 캔버스 ON");
        isIntroSkippedOrFinished = true;
        UpdateGameCanvasState();
    }

    // 대화 이벤트 핸들러
    private void HandleDialogueEvent(string eventId)
    {
        // "Intro" 대사가 끝났다는 신호를 받으면
        if (eventId == "Intro")
        {
            Debug.Log("[GameOverUI] 인트로 대사 종료 확인 -> 게임 캔버스 ON");
            isIntroSkippedOrFinished = true;
            UpdateGameCanvasState();
        }
    }

    // 캔버스 상태 업데이트 (중복 로직 통합)
    private void UpdateGameCanvasState()
    {
        if (gameUICanvas != null)
        {
            // 인트로가 스킵되었거나 끝났으면 Canvas 켜기, 아니면 끄기
            gameUICanvas.enabled = isIntroSkippedOrFinished;
            Debug.Log($"[GameOverUI] 메인 게임 캔버스 상태 변경: {gameUICanvas.enabled}");
        }
    }
}