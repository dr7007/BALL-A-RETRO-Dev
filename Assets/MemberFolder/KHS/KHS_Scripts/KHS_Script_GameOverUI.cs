using PSH;
using System;
using UnityEngine;

public class KHS_Script_GameOverUI : MonoBehaviour
{
    [Header("Final UI Panels (Lobby Button Included here)")]
    [Tooltip("로비로 가기 버튼이 포함된 게임오버 패널")]
    [SerializeField] private GameObject gameOverUIObj;
    [Tooltip("로비로 가기 버튼이 포함된 게임클리어 패널")]
    [SerializeField] private GameObject gameClearUIObj;

    private Canvas gameUICanvas; // 메인 게임 HUD (점수판 등)

    private void Awake()
    {
        gameUICanvas = GetComponent<Canvas>();
        if (gameUICanvas == null) gameUICanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        // 게임 시작 시에는 결과 창을 확실하게 꺼둡니다.
        if (gameOverUIObj) gameOverUIObj.SetActive(false);
        if (gameClearUIObj) gameClearUIObj.SetActive(false);
    }

    private void OnEnable()
    {
        // ✂️ [범인 제거] 이 두 줄이 문제를 일으키는 주범입니다.
        // KHS_Script_ScoreManager.OnGameOver += ShowGameOverPanel; // <-- 이 줄을 주석 처리하거나 삭제!
        // KHS_Script_ScoreManager.OnGameClear += ShowGameClearPanel; // <-- 이 줄도 삭제!

        // 인트로 관련 이벤트만 구독합니다.
        PSH_Script_GameSceneDirector.NoIntroStartEvt += HandleNoIntroStart;
        PSH_Script_DialogueUI.DialogueEvt += HandleDialogueEvent;
    }

    private void OnDisable()
    {
        // ✂️ [범인 제거]
        // KHS_Script_ScoreManager.OnGameOver -= ShowGameOverPanel; // <-- 삭제
        // KHS_Script_ScoreManager.OnGameClear -= ShowGameClearPanel; // <-- 삭제

        PSH_Script_GameSceneDirector.NoIntroStartEvt -= HandleNoIntroStart;
        PSH_Script_DialogueUI.DialogueEvt -= HandleDialogueEvent;
    }

    // 💡 [감독 전용] 감독(Director)만이 이 함수를 호출할 수 있습니다.
    public void ShowGameOverPanel()
    {
        Debug.Log("🚨 [GameOverUI] 감독의 지시로 최종 패널 활성화 (Game Over)");
        //if (gameUICanvas) gameUICanvas.enabled = false;
        if (gameOverUIObj) gameOverUIObj.SetActive(true);
    }

    // 💡 [감독 전용]
    public void ShowGameClearPanel()
    {
        Debug.Log("🚨 [GameOverUI] 감독의 지시로 최종 패널 활성화 (Game Clear)");
        //if (gameUICanvas) gameUICanvas.enabled = false;
        if (gameClearUIObj) gameClearUIObj.SetActive(true);
    }

    // (인트로 관련 로직은 그대로 유지)
    private void HandleNoIntroStart() { /* ... */ }
    private void HandleDialogueEvent(string eventId) { /* ... */ }
}