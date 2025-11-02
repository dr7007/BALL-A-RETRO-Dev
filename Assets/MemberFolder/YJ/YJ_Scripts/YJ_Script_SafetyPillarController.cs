using UnityEngine;

public class YJ_Script_SafetyPillarController : MonoBehaviour
{
    [Header("Pillar 위치 설정")]
    [Tooltip("Pillar가 활성화(솟아오른)되었을 때의 Y축 로컬 위치")]
    [SerializeField] private float activatePosY = 0.18f;
    [Tooltip("Pillar가 비활성화(숨겨진)되었을 때의 Y축 로컬 위치")]
    [SerializeField] private float desactivatePosY = -0.2f;

    [Header("Pillar 이동 속도")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("사운드 (선택 사항)")]
    [SerializeField] private AudioClip activateSound;
    private AudioSource sound_;

    // 내부 상태 변수
    private bool b_MoveObject = false;
    private float target_y = 0;

    private void Awake()
    {
        sound_ = GetComponent<AudioSource>();
        if (sound_ == null)
        {
            sound_ = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // 시작할 때 비활성화 위치로 강제 설정 및 초기화
        transform.localPosition = new Vector3(
            transform.localPosition.x,
            desactivatePosY,
            transform.localPosition.z
        );
        target_y = desactivatePosY;
    }

    private void Update()
    {
        // b_MoveObject가 true일 때만 목표 위치(target_y)로 이동
        if (b_MoveObject)
        {
            float YPos = Mathf.MoveTowards(transform.localPosition.y, target_y, moveSpeed * Time.deltaTime);
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                YPos,
                transform.localPosition.z
            );

            // 목표 위치에 도달하면 움직임 중지
            if (Mathf.Abs(transform.localPosition.y - target_y) < 0.001f)
            {
                b_MoveObject = false;
            }
        }
    }

    // --- public 제어 함수 ---

    [ContextMenu("Pillar 활성화 테스트")]
    public void ActivatePillar()
    {
        target_y = activatePosY;
        b_MoveObject = true;
        if (sound_ && activateSound && !sound_.isPlaying)
        {
            sound_.PlayOneShot(activateSound);
        }
    }

    [ContextMenu("Pillar 비활성화 테스트")]
    public void DesactivatePillar()
    {
        target_y = desactivatePosY;
        b_MoveObject = true;
    }

    public void SetActivated(bool value)
    {
        if (value)
        {
            ActivatePillar();
        }
        else
        {
            DesactivatePillar();
        }
    }
}