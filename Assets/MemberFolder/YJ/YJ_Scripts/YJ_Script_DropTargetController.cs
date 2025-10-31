using UnityEngine;

public class YJ_Script_DropTargetController : MonoBehaviour
{
    [Header("드롭 타겟")]
    [SerializeField]
    private bool b_Drop_Target = true;           // 체크 시 드롭타겟, 체크 해제 시 고정 타겟

    private bool b_MoveObject = false;
    private float target = 0;

    [Header("타겟 활성화/비활성화 시 로컬 포지션")]
    [SerializeField]
    private float ActivatePosY = .11f;      // 타겟 활성화 시 Y축 값
    [SerializeField]
    private float DesactivatePosY = -.25f;  // 타겟 비활성화 시 Y축 값
    [SerializeField]
    private float MoveSpeed = 5;            // speed to reach the target

    [Header("사운드")]
    public AudioClip Sfx_Hit;               // Sound when the target is hit
    private AudioSource sound_;


    private void Start()
    {
        if (transform.localPosition.y == ActivatePosY)
        {
            target = ActivatePosY;
        }

        else
        {
            target = DesactivatePosY;
        }

        sound_ = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // 
        if (b_MoveObject)
        {
            float YPos = Mathf.MoveTowards(transform.localPosition.y, target, MoveSpeed * Time.deltaTime);

            transform.localPosition = new Vector3(
                transform.localPosition.x,
                YPos,
                transform.localPosition.z
            );

            if (transform.localPosition.y == target)
            {
                b_MoveObject = false;
            }
        }
    }

    void OnCollisionEnter(Collision _collision)
    {
        if (_collision.gameObject.name == "Ball")
        {	
            if (b_Drop_Target)
                Desactivate_Object();		

            if (!sound_.isPlaying && Sfx_Hit) sound_.PlayOneShot(Sfx_Hit, 1);
        }
    }

    public void Desactivate_Object()
    {   // 타겟 비활성화
        target = DesactivatePosY;
        b_MoveObject = true;
    }

    public void Activate_Object()
    {   // 타겟 활성화
        target = ActivatePosY;
        b_MoveObject = true;
    }
}
