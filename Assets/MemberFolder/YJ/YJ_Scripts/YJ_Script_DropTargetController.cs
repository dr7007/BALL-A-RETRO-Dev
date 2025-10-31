using UnityEngine;

public class YJ_Script_DropTargetController : MonoBehaviour
{
    [Header("사운드 (개별 설정)")]
    public AudioClip Sfx_Hit;                   // 타격음 지정

    // 매니저가 설정해줄 변수들
    private float ActivatePosY;
    private float DesactivatePosY;
    private float MoveSpeed;
    private YJ_Script_DropTargetManager manager; // 부모 오브젝트에 부착된 매니저

    // 내부 변수
    private AudioSource sound_;
    private bool b_MoveObject = false;
    private float target_y = 0;
    private bool b_IsDesactivated = false; // 나의 현재 상태

    private void Awake()
    {
        sound_ = GetComponent<AudioSource>();
    }

    // 부모 매니저가 호출하여 나를 초기화하는 함수
    public void Initialize(YJ_Script_DropTargetManager mgr, float activeY, float desactiveY, float speed)
    {
        manager = mgr;
        ActivatePosY = activeY;
        DesactivatePosY = desactiveY;
        MoveSpeed = speed;

        // 시작 위치에 따라 초기 상태 설정
        if (Mathf.Abs(transform.localPosition.y - ActivatePosY) < 0.01f)
        {
            target_y = ActivatePosY;
            b_IsDesactivated = false;
        }
        else
        {
            target_y = DesactivatePosY;
            b_IsDesactivated = true;
        }
    }

    private void Update()
    {
        if (b_MoveObject)
        {
            float YPos = Mathf.MoveTowards(transform.localPosition.y, target_y, MoveSpeed * Time.deltaTime);
            transform.localPosition = new Vector3(transform.localPosition.x, YPos, transform.localPosition.z);

            if (transform.localPosition.y == target_y)
            {
                b_MoveObject = false;
            }
        }
    }

    private void OnCollisionEnter(Collision _collision)
    {
        if (_collision.gameObject.CompareTag("Ball") && !b_MoveObject)
        {
            // 내가 판단하지 않고, 즉시 매니저에게 "맞았다"고 보고
            manager.HandleTargetHit(this);
        }
    }

    // --- 매니저가 나를 제어하는 함수들 ---

    public void Desactivate_Object()
    {
        target_y = DesactivatePosY;
        b_MoveObject = true;
        b_IsDesactivated = true; // 나의 상태를 '비활성'으로 기록
    }

    public void Activate_Object()
    {
        target_y = ActivatePosY;
        b_MoveObject = true;
        b_IsDesactivated = false; // 나의 상태를 '활성'으로 기록
    }

    public void PlayHitSound()
    {
        if (!sound_.isPlaying && Sfx_Hit)
            sound_.PlayOneShot(Sfx_Hit, 1);
    }

    public bool IsDesactivated()
    {
        return b_IsDesactivated;
    }
}