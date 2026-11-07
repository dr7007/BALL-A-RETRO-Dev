using UnityEngine;

public class YJ_Script_BumperComboManager : MonoBehaviour
{
    [Header("관리할 범퍼 3개 (BumperEffect 연결)")]
    public KHS_Script_BumperEffect bumperA;
    public KHS_Script_BumperEffect bumperB;
    public KHS_Script_BumperEffect bumperC;

    [Header("배수 조명 (ChangeSpriteRenderer 연결)")]
    public ChangeSpriteRenderer lightX2;
    public ChangeSpriteRenderer lightX4;
    public ChangeSpriteRenderer lightX6;

    private int currentMultiplierLevel = 0; // 0: x1, 1: x2, 2: x4, 3: x6
    private float baseMultiplier = 1.0f;    // 게임 시작 시 기본 배수 보관용

    // The vendor LED caches its Renderer in Start. Scene startup order is undefined,
    // and inactive LEDs may not have started at all. Apply the public LED state here
    // so its later Start preserves the same state without relying on that cache.
    private static void SetComboLight(ChangeSpriteRenderer led, bool isOn)
    {
        if (led == null) return;
        led.On = isOn;
        if (led.obj_Light != null) led.obj_Light.enabled = isOn;
        var renderer = led.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.SetColor("_EmissionColor", isOn ? led.Emission_On : led.Emission_Off_);
    }

    private void Start()
    {
        // 시작 시 모든 배수 조명 끄기
        SetComboLight(lightX2, false);
        SetComboLight(lightX4, false);
        SetComboLight(lightX6, false);

        // KHS_Script_ScoreManager의 기본 배수 저장
        if (KHS_Script_ScoreManager.Instance != null)
        {
            baseMultiplier = KHS_Script_ScoreManager.Instance.multiplier;
        }
    }

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += ResetComboSystem; //
    }

    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= ResetComboSystem; //
    }

    // 공이 떨어졌을 때 호출되는 전체 초기화 함수
    private void ResetComboSystem()
    {
        // 1. 배수 레벨 초기화
        currentMultiplierLevel = 0;

        // 2. 배수 조명 모두 끄기
        SetComboLight(lightX2, false);
        SetComboLight(lightX4, false);
        SetComboLight(lightX6, false);

        // 3. ScoreManager의 점수 배율을 초기 상태(baseMultiplier)로 되돌림
        if (KHS_Script_ScoreManager.Instance != null)
        {
            KHS_Script_ScoreManager.Instance.multiplier = baseMultiplier;
            Debug.Log("<color=red>Ball Out! 콤보 시스템과 점수 배율이 초기화되었습니다.</color>");
        }
    }

    // 범퍼의 불이 켜질 때마다 호출됨
    public void CheckCombo()
    {
        // 3개의 범퍼가 모두 켜졌는지 확인 (public으로 열린 lightenable 변수 사용)
        if (bumperA.lightenable && bumperB.lightenable && bumperC.lightenable)
        {
            // 범퍼 불빛 초기화
            bumperA.ForceTurnOffComboLight();
            bumperB.ForceTurnOffComboLight();
            bumperC.ForceTurnOffComboLight();

            // 배수 레벨 증가 (최대 3단계: x6)
            if (currentMultiplierLevel < 3)
            {
                currentMultiplierLevel++;
                ApplyMultiplier();
            }
        }
    }

    private void ApplyMultiplier()
    {
        // 기존 켜져 있던 배수 조명을 모두 끕니다.
        SetComboLight(lightX2, false);
        SetComboLight(lightX4, false);
        SetComboLight(lightX6, false);

        float comboMultiplier = 1f;

        // 레벨에 맞는 조명을 켜고 콤보 배수 값을 설정합니다.
        switch (currentMultiplierLevel)
        {
            case 1:
                SetComboLight(lightX2, true);
                comboMultiplier = 2f;
                break;
            case 2:
                SetComboLight(lightX4, true);
                comboMultiplier = 4f;
                break;
            case 3:
                SetComboLight(lightX6, true);
                comboMultiplier = 6f;
                break;
        }

        // ScoreManager의 글로벌 multiplier 갱신
        if (KHS_Script_ScoreManager.Instance != null)
        {
            KHS_Script_ScoreManager.Instance.multiplier = baseMultiplier * comboMultiplier;
            Debug.Log($"콤보 달성! 글로벌 점수 배수가 {comboMultiplier}배로 증가했습니다.");
        }
    }
}
