using UnityEngine;

public class KHS_Script_BumperEffect : MonoBehaviour
{

    [Header("Bumper Sound")]
    public AudioClip Sfx_Hit;              // 충돌 시 재생할 사운드
    private AudioSource sound_;            // AudioSource 컴포넌트

    [Header("LED connected to the bumper")]
    public GameObject obj_Led;             // LED 오브젝트
    private ChangeSpriteRenderer ledRenderer; // LED 제어용 스크립트 (ChangeSpriteRenderer)

    private void Start()
    {
        // AudioSource 컴포넌트 가져오기
        sound_ = GetComponent<AudioSource>();

        // LED 스크립트 연결
        if (obj_Led != null)
            ledRenderer = obj_Led.GetComponent<ChangeSpriteRenderer>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 공(Ball)과 충돌했을 때만 반응하도록 (선택사항)
        if (!collision.gameObject.CompareTag("Ball"))
            return;

        // 사운드 재생
        if (Sfx_Hit != null && sound_ != null)
            sound_.PlayOneShot(Sfx_Hit);

        // LED 점등 (0.2초 동안)
        if (ledRenderer != null)
            ledRenderer.Led_On_With_Timer(0.2f);
    }
}
