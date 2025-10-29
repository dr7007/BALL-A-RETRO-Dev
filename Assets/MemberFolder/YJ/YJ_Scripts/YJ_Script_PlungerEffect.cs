using System;
using UnityEngine;

public class YJ_Script_PlungerEffect : MonoBehaviour
{

    [Header("Bumper Sound")]
    public AudioClip Sfx_Hit;              // 충돌 시 재생할 사운드
    private AudioSource sound_;            // AudioSource 컴포넌트

    [Header("LED connected to the bumper")]
    public GameObject obj_Led;                  // LED 오브젝트
    private ChangeSpriteRenderer ledRenderer;   // LED 제어용 스크립트 (ChangeSpriteRenderer)

    [SerializeField]
    private bool isBlink = false;
    private bool lightenable = false;

    private void Start()
    {
        gameObject.AddComponent<AudioSource>();

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
        if (ledRenderer != null)
        {
            ledRenderer.F_ChangeSprite_Off();
        }

        lightenable = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 공(Ball)과 닿았을 때만 반응
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
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 공(Ball)과 닿았을 때만 반응
        if (!other.CompareTag("Ball"))
            return;

        // LED 전환
        if (ledRenderer != null)
        {
            if (lightenable == true)
            {
                lightenable = false;
                ledRenderer.F_ChangeSprite_Off();
            }
        }
    }
}
