using System;
using UnityEngine;

public class KHS_Script_BumperEffect : MonoBehaviour
{

    [Header("Bumper Sound")]
    public AudioClip Sfx_Hit;              // 충돌 시 재생할 사운드
    private AudioSource sound_;            // AudioSource 컴포넌트

    [Header("LED connected to the bumper")]
    public GameObject obj_Led;             // LED 오브젝트
    private ChangeSpriteRenderer ledRenderer; // LED 제어용 스크립트 (ChangeSpriteRenderer)

    [Header("Combo Manager 연결")]
    public YJ_Script_BumperComboManager comboManager; // 콤보 매니저 연결 슬롯

    [SerializeField]
    private bool isBlink = false;
    public bool lightenable = false;

    private void Start()
    {
        // AudioSource 컴포넌트 가져오기
        sound_ = GetComponent<AudioSource>();

        // LED 스크립트 연결
        if (obj_Led != null)
            ledRenderer = obj_Led.GetComponent<ChangeSpriteRenderer>();

        lightenable = false;
    }

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += ResetEffect;
    }
    private void OnDisable()
    { 
        KHS_Script_BallOutController.BallOutEvt -= ResetEffect;
    }

    private void ResetEffect()
    {
        if(ledRenderer != null)
        {
            lightenable = false;
            ledRenderer.F_ChangeSprite_Off();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 공(Ball)과 충돌했을 때만 반응하도록 (선택사항)
        if (!collision.gameObject.CompareTag("Ball"))
            return;

        // 사운드 재생
        if (Sfx_Hit != null && sound_ != null)
            sound_.PlayOneShot(Sfx_Hit);

        // LED 전환
        if (ledRenderer != null && isBlink == false)
        {
            if (lightenable == false)
            {
                lightenable = true;
                ledRenderer.F_ChangeSprite_On();
                comboManager?.CheckCombo(); // 켜졌을 때 매니저에 콤보 확인 요청
            }
            else if(lightenable == true)
            {
                lightenable = false;
                ledRenderer.F_ChangeSprite_Off();
            }
        }
        if (ledRenderer != null && isBlink == true)
        {
            if (lightenable == false)
            {
                ledRenderer.Led_On_With_Timer(0.2f);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // 공(Ball)과 충돌했을 때만 반응하도록 (선택사항)
        if (!other.CompareTag("Ball"))
            return;

        Debug.Log("트리거 LED점등 체크용");
        // 사운드 재생
        if (Sfx_Hit != null && sound_ != null)
            sound_.PlayOneShot(Sfx_Hit);

        // LED 전환
        if (ledRenderer != null)
        {
            if (lightenable == false)
            {
                lightenable = true;
                ledRenderer.F_ChangeSprite_On();
                comboManager?.CheckCombo(); // 켜졌을 때 매니저에 콤보 확인 요청
            }
            else if (lightenable == true)
            {
                lightenable = false;
                ledRenderer.F_ChangeSprite_Off();
            }
        }
    }

    public void ForceTurnOffComboLight() // 콤보 매니저가 세 범퍼가 모두 켜졌을 때 강제로 불을 끄기 위한 함수
    {
        lightenable = false;
        if (ledRenderer != null)
        {
            ledRenderer.F_ChangeSprite_Off();
        }
    }
}
