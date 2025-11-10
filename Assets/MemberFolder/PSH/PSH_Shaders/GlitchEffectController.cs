using UnityEngine;
using PSH; // PSH_Script_GameSceneDirector.NoIntroStartEvt를 위해
using System; // KHS_Script_ScoreManager.OnGameOver를 위해 (네임스페이스가 다를 수 있음)

// KHS_Script_GameOverUI와 상관없이, 이벤트를 직접 구독하는 컨트롤러
public class GlitchEffectController : MonoBehaviour
{
    // 이 스크립트는 씬에 하나만 있으면 됩니다.
    
    private void Start()
    {
        // 시작할 때 확실하게 끕니다.
        GlitchEffect_RendererFeature.IsEnabled = false;
    }

    // 스크립트가 활성화될 때 이벤트 구독
    private void OnEnable()
    {
        KHS_Script_ScoreManager.OnGameOver += StartGlitchEffect;
        PSH_Script_GameSceneDirector.NoIntroStartEvt += StopGlitchEffect;
        KHS_Script_ResetController.OnReset += StopGlitchEffect;
    }

    // 스크립트가 비활성화될 때 이벤트 구독 해제 (메모리 누수 방지)
    private void OnDisable()
    {
        KHS_Script_ScoreManager.OnGameOver -= StartGlitchEffect;
        PSH_Script_GameSceneDirector.NoIntroStartEvt -= StopGlitchEffect;
        KHS_Script_ResetController.OnReset -= StopGlitchEffect;
    }

    // 게임 오버 이벤트가 발생하면 이 함수가 호출됨
    private void StartGlitchEffect()
    {
        GlitchEffect_RendererFeature.IsEnabled = true;
    }

    // 게임 리셋 이벤트가 발생하면 이 함수가 호출됨
    private void StopGlitchEffect()
    {
        GlitchEffect_RendererFeature.IsEnabled = false;
    }
}