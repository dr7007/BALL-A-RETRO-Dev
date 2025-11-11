using UnityEngine;
using System.Collections;
using PSH;
using UnityEngine.UI;

public class PSH_Script_GameOverSequenceDirector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("글리치 지속 시간")]
    [SerializeField] private float glitchDuration = 1.5f;
    [Tooltip("글리치 재생 중 시간 속도 (1 = 정상 속도)")]
    [SerializeField] private float glitchTimeScale = 1.0f; // 글리치 효과가 멈추면 1.0으로 설정
    [Tooltip("폭죽 터지는 시간 (대사 시작 전 대기 시간)")]
    [SerializeField] private float fireworkDuration = 2.0f;

    [Header("Actors & Props")]
    [SerializeField] private KHS_Script_GameOverUI finalUIScript;
    [Tooltip("게임 클리어 시 터뜨릴 폭죽 오브젝트")]
    [SerializeField] private GameObject fireworkObject;

    [Header("Cutscene IDs (CSV Key)")]
    [SerializeField] private string gameOverCutsceneId = "Ending2";
    [SerializeField] private string gameClearCutsceneId = "Ending1";

    [Header("대사중 비활성화 버튼")]
    [SerializeField] Button lobbyBtn;
    [SerializeField] Button resetBtn;

    private void Start()
    {
        GlitchEffect_RendererFeature.IsEnabled = false;
        if (fireworkObject) fireworkObject.SetActive(false);

        // 시작할 때 버튼을 미리 꺼둡니다.
        SetButtonsState(false);
    }

    private void OnEnable() { KHS_Script_ScoreManager.OnGameOver += StartGameOverSequence; KHS_Script_ScoreManager.OnGameClear += StartGameClearSequence; PSH_Script_DialogueUI.OnDialogueComplete += HandleDialogueComplete; }
    private void OnDisable() { KHS_Script_ScoreManager.OnGameOver -= StartGameOverSequence; KHS_Script_ScoreManager.OnGameClear -= StartGameClearSequence; PSH_Script_DialogueUI.OnDialogueComplete -= HandleDialogueComplete; }

    private void StartGameOverSequence() { StartCoroutine(GameOverRoutine()); }
    private void StartGameClearSequence() { StartCoroutine(GameClearRoutine()); }

    // --- [연출 1] 게임 오버 시퀀스 (글리치 -> 대사) ---
    private IEnumerator GameOverRoutine()
    {
        Debug.Log("[Director] Game Over 시퀀스 시작");

        // 1. 글리치 재생 (설정한 시간 속도로)
        GlitchEffect_RendererFeature.IsEnabled = true;
        Time.timeScale = glitchTimeScale;
        yield return new WaitForSecondsRealtime(glitchDuration);

        // 2. 대사 시작 직전에 시간 완전 정지!
        Time.timeScale = 0f;

        // 3. [순서 변경] 최종 UI 패널을 먼저 켜서 대사창이 보이게 함
        if (finalUIScript) finalUIScript.ShowGameOverPanel();

        // 4. 버튼 비활성화 (패널이 켜진 *후에* 실행)
        SetButtonsState(false);
        lobbyBtn.gameObject.SetActive(false);
        resetBtn.gameObject.SetActive(false);
        // 5. 대사 재생
        PSH_Script_DialogueUI.Instance.Play(gameOverCutsceneId);
    }

    // --- [연출 2] 게임 클리어 시퀀스 (폭죽 -> 대사 -> (대사 끝나면) 글리치) ---
    private IEnumerator GameClearRoutine()
    {
        Debug.Log("[Director] Game Clear 시퀀스 시작");

        // 1. 폭죽 발사! (시간은 아직 정상)
        if (fireworkObject) fireworkObject.SetActive(true);
        yield return new WaitForSecondsRealtime(fireworkDuration);

        // 2. 시간 완전 정지
        Time.timeScale = 0f;

        // 3. [순서 변경] 최종 UI 패널 먼저 켜기
        if (finalUIScript) finalUIScript.ShowGameClearPanel();

        // 4. 버튼 비활성화
        SetButtonsState(false);
        lobbyBtn.gameObject.SetActive(false);
        resetBtn.gameObject.SetActive(false);
        // 5. 대사 재생
        PSH_Script_DialogueUI.Instance.Play(gameClearCutsceneId);
    }

    // --- 대사 종료 후속 처리 ---
    private void HandleDialogueComplete(string finishedCutsceneId)
    {
        if (finishedCutsceneId == gameOverCutsceneId)
        {
            // 게임오버 대사 끝 -> 글리치 끄고 버튼 활성화
            GlitchEffect_RendererFeature.IsEnabled = false;
            SetButtonsState(true);
            lobbyBtn.gameObject.SetActive(true);
            resetBtn.gameObject.SetActive(true);
        }
        else if (finishedCutsceneId == gameClearCutsceneId)
        {
            // 게임클리어 대사 끝 -> 이제 글리치 시작!
            StartCoroutine(PostClearGlitchRoutine());
        }
    }

    // --- [연출 3] 게임 클리어 후속 글리치 ---
    private IEnumerator PostClearGlitchRoutine()
    {
        Debug.Log("[Director] 클리어 대사 종료 -> 후속 글리치 시작");

        GlitchEffect_RendererFeature.IsEnabled = true;
        yield return new WaitForSecondsRealtime(glitchDuration);
        GlitchEffect_RendererFeature.IsEnabled = false;

        // 글리치가 끝난 후 버튼 활성화
        SetButtonsState(true);
        lobbyBtn.gameObject.SetActive(true);
        resetBtn.gameObject.SetActive(true);
    }

    // [수정] 버튼 활성화/비활성화를 안전하게 처리하는 헬퍼 함수
    // (interactable과 enabled를 모두 제어)
    private void SetButtonsState(bool state)
    {
        if (lobbyBtn != null)
        {
            lobbyBtn.interactable = state;
            lobbyBtn.enabled = state; // enabled도 같이 제어
            Debug.Log($"[Director] Lobby 버튼 상태 = {state}");
        }
        else
        {
            Debug.LogError("[Director] 'Lobby Btn'이 인스펙터에 연결되지 않았습니다!");
        }

        if (resetBtn != null)
        {
            resetBtn.interactable = state;
            resetBtn.enabled = state; // enabled도 같이 제어
            Debug.Log($"[Director] Reset 버튼 상태 = {state}");
        }
        else
        {
            Debug.LogError("[Director] 'Reset Btn'이 인스펙터에 연결되지 않았습니다!");
        }
    }
}