using UnityEngine;
using System.Collections;
using PSH;

public class PSH_Script_GameOverSequenceDirector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("글리치 지속 시간")]
    [SerializeField] private float glitchDuration = 1.5f;
    [Tooltip("폭죽 터지는 시간 (대사 시작 전 대기 시간)")]
    [SerializeField] private float fireworkDuration = 2.0f;

    [Header("Actors & Props")]
    [SerializeField] private KHS_Script_GameOverUI finalUIScript;
    [Tooltip("게임 클리어 시 터뜨릴 폭죽 오브젝트")]
    [SerializeField] private GameObject fireworkObject;

    [Header("Cutscene IDs (CSV Key)")]
    [SerializeField] private string gameOverCutsceneId = "Ending2";
    [SerializeField] private string gameClearCutsceneId = "Ending1";

    private void Start() { GlitchEffect_RendererFeature.IsEnabled = false; if (fireworkObject) fireworkObject.SetActive(false); }
    private void OnEnable() { KHS_Script_ScoreManager.OnGameOver += StartGameOverSequence; KHS_Script_ScoreManager.OnGameClear += StartGameClearSequence; PSH_Script_DialogueUI.OnDialogueComplete += HandleDialogueComplete; }
    private void OnDisable() { KHS_Script_ScoreManager.OnGameOver -= StartGameOverSequence; KHS_Script_ScoreManager.OnGameClear -= StartGameClearSequence; PSH_Script_DialogueUI.OnDialogueComplete -= HandleDialogueComplete; }

    // 🎬 Action! (게임오버)
    private void StartGameOverSequence() { StartCoroutine(GameOverRoutine()); }
    // 🎬 Action! (게임클리어)
    private void StartGameClearSequence() { StartCoroutine(GameClearRoutine()); }

    // --- [연출 1] 게임 오버 시퀀스 (글리치 -> 대사) ---
    private IEnumerator GameOverRoutine()
    {
        Debug.Log("[Director] Game Over 시퀀스 시작");
        // 1. 글리치 먼저 재생
        Time.timeScale = 1.0f; // 글리치 재생 중엔 시간 흐르게 (선택사항)
        GlitchEffect_RendererFeature.IsEnabled = true;
        yield return new WaitForSecondsRealtime(glitchDuration);

        // 2. 대사 시작 (시간 정지)
        Time.timeScale = 0f;
        PSH_Script_DialogueUI.Instance.Play(gameOverCutsceneId);
    }

    // --- [연출 2] 게임 클리어 시퀀스 (폭죽 -> 대사 -> (대사 끝나면) 글리치) ---
    private IEnumerator GameClearRoutine()
    {
        Debug.Log("[Director] Game Clear 시퀀스 시작");
        // 1. 폭죽 발사!
        if (fireworkObject) fireworkObject.SetActive(true);
        yield return new WaitForSecondsRealtime(fireworkDuration);

        // 2. 대사 시작 (시간 정지)
        Time.timeScale = 0f;
        PSH_Script_DialogueUI.Instance.Play(gameClearCutsceneId);
    }

    // --- 대사 종료 후속 처리 ---
    private void HandleDialogueComplete(string finishedCutsceneId)
    {
        if (finishedCutsceneId == gameOverCutsceneId)
        {
            // 게임오버 대사 끝 -> 글리치 끄고 최종 UI
            GlitchEffect_RendererFeature.IsEnabled = false;
            if (finalUIScript) finalUIScript.ShowGameOverPanel();
        }
        else if (finishedCutsceneId == gameClearCutsceneId)
        {
            // 💡 게임클리어 대사 끝 -> 이제 글리치 시작!
            StartCoroutine(PostClearGlitchRoutine());
        }
    }

    // --- [연출 3] 게임 클리어 후속 글리치 ---
    private IEnumerator PostClearGlitchRoutine()
    {
        Debug.Log("[Director] 클리어 대사 종료 -> 후속 글리치 시작");
        // 1. 글리치 ON (게임 캔버스는 그대로 유지됨)
        GlitchEffect_RendererFeature.IsEnabled = true;
        yield return new WaitForSecondsRealtime(glitchDuration);

        // 2. 글리치 OFF 및 최종 클리어 UI
        GlitchEffect_RendererFeature.IsEnabled = false;
        if (finalUIScript) finalUIScript.ShowGameClearPanel();
    }
}