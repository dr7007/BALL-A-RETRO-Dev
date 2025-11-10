using UnityEngine;
using System.Collections;
using PSH;

public class PSH_Script_GameOverSequenceDirector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float glitchDuration = 1.5f;
    // 💡 글리치 재생 중 시간 속도 (1 = 정상, 0.1 = 슬로우 모션)
    [SerializeField] private float glitchTimeScale = 1.0f;

    [Header("Actors & Props")]
    [SerializeField] private KHS_Script_GameOverUI finalUIScript;

    [Header("Cutscene IDs (CSV Key)")]
    [SerializeField] private string gameOverCutsceneId = "Ending2";
    [SerializeField] private string gameClearCutsceneId = "Ending1";

    // ... (Start, OnEnable, OnDisable 등은 그대로) ...
    private void Start() { GlitchEffect_RendererFeature.IsEnabled = false; }
    private void OnEnable() { KHS_Script_ScoreManager.OnGameOver += StartGameOverSequence; KHS_Script_ScoreManager.OnGameClear += StartGameClearSequence; PSH_Script_DialogueUI.OnDialogueComplete += HandleDialogueComplete; }
    private void OnDisable() { KHS_Script_ScoreManager.OnGameOver -= StartGameOverSequence; KHS_Script_ScoreManager.OnGameClear -= StartGameClearSequence; PSH_Script_DialogueUI.OnDialogueComplete -= HandleDialogueComplete; }
    private void StartGameOverSequence() { StartCoroutine(SequenceRoutine(gameOverCutsceneId)); }
    private void StartGameClearSequence() { StartCoroutine(SequenceRoutine(gameClearCutsceneId)); }

    private IEnumerator SequenceRoutine(string cutsceneId)
    {
        Debug.Log($"[Director] 1. 글리치 ON! (속도: {glitchTimeScale})");

        // 1. 글리치 ON
        GlitchEffect_RendererFeature.IsEnabled = true;
        // 💡 2. 시간을 멈추지 말고, 설정한 속도로 흐르게 함
        Time.timeScale = glitchTimeScale;

        // 3. 1.5초 대기 (이제 Realtime 아니어도 됨, 하지만 안전하게 Realtime 유지)
        yield return new WaitForSecondsRealtime(glitchDuration);

        Debug.Log("[Director] 2. 대사 시작! (이제 시간 완전 정지)");
        // 💡 4. 대사 나오기 직전에 완전 정지
        Time.timeScale = 0f;

        // 5. 배우에게 연기 지시
        PSH_Script_DialogueUI.Instance.Play(cutsceneId);
    }

    private void HandleDialogueComplete(string finishedCutsceneId)
    {
        if (finishedCutsceneId == gameOverCutsceneId || finishedCutsceneId == gameClearCutsceneId)
        {
            Debug.Log($"[Director] 3. 시퀀스 종료. 글리치 OFF, 최종 UI ON.");
            GlitchEffect_RendererFeature.IsEnabled = false;
            if (finalUIScript != null)
            {
                if (finishedCutsceneId == gameOverCutsceneId) finalUIScript.ShowGameOverPanel();
                else if (finishedCutsceneId == gameClearCutsceneId) finalUIScript.ShowGameClearPanel();
            }
        }
    }
}