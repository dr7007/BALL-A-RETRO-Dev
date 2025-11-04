using System;
using UnityEngine;
using UnityEngine.UIElements;

public class YJ_Script_BallController : MonoBehaviour
{
    public static event Action GameOverEvt;

    [SerializeField]
    private Rigidbody rigidBody = null;
    private RigidbodyConstraints defaultConstraints;
    [Header("핀볼 테이블의 기본 Y 높이")]
    [SerializeField]
    private float playfieldYLevel = 0.25f;
    [SerializeField]
    private float Gravity = 9.8f;
    [SerializeField]
    private Vector3 GravDirection = Vector3.zero;
    [SerializeField]
    private int BallCount = 0;

    private Vector3 initBallPos = Vector3.zero;
    private Transform originParent;

    void Start()
    {
        initBallPos = transform.position;
        GravDirection = GetComponentInParent<Transform>().forward * -1;
        rigidBody = GetComponentInChildren<Rigidbody>();
        defaultConstraints = rigidBody.constraints; // 평소의 Y축 고정 상태 저장
        originParent = transform.parent;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Y축이 고정되어 있는지(2D 모드인지) 확인
        if ((rigidBody.constraints & RigidbodyConstraints.FreezePositionY) != 0)
        {
            // 2D 모드: 핀볼 테이블 경사(커스텀 중력) 적용
            rigidBody.AddForce(Gravity * GravDirection);
        }
        else
        {
            // 3D 모드 (낙하 중): 유니티 기본 중력과 유사한 힘을 Y축(-)로 적용
            rigidBody.AddForce(Vector3.down * Gravity * rigidBody.mass);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            rigidBody.AddForce(3f, 3f, 3f, ForceMode.Impulse);
        }
    }

    private void OnEnable()
    {
        KHS_Script_ResetController.OnReset += KHS_BallReset;
        KHS_Script_BallOutController.BallOutEvt += KHS_GameOverBall;
    }


    private void OnDisable()
    {
        KHS_Script_ResetController.OnReset -= KHS_BallReset;
        KHS_Script_BallOutController.BallOutEvt -= KHS_GameOverBall;
    }

    private void OnCollisionEnter(Collision collision)
    {
        bool isFalling = (rigidBody.constraints & RigidbodyConstraints.FreezePositionY) == 0;

        if (!isFalling) return;

        if (collision.gameObject.CompareTag("Playfield"))
        {
            Enter2DMode(playfieldYLevel);
        }
    }

    private void KHS_GameOverBall()
    {
        --BallCount;
        if (BallCount <= 0)
        {
            gameObject.SetActive(false);
            GameOverEvt.Invoke();
        }
        else
            KHS_BallReset();
    }
    private void KHS_BallReset()
    {
        transform.position = initBallPos;
        rigidBody.angularVelocity = Vector3.zero;
        rigidBody.linearVelocity = Vector3.zero;
    }

    public int BallCountResponse()
    {
        return BallCount;
    }

    public void CaptureAndParent(Transform parent)
    {
        rigidBody.isKinematic = true; // 물리 엔진 정지
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;

        // 이 'parent' 오브젝트(보이지 않는 기차)를 따라가도록 자식으로 만듦
        transform.parent = parent;

        // 기차의 로컬 위치 (0,0,0)으로 즉시 이동 (선택 사항이지만 깔끔함)
        transform.localPosition = Vector3.zero;

        Debug.Log("레일 기차에 탑승 (캡처됨)");
    }

    public void ReleaseAndUnparent()
    {
        transform.parent = originParent; // 부모-자식 관계 복원
        rigidBody.isKinematic = false;  // 물리 엔진 다시 켜기
        Enter2DMode(playfieldYLevel); // 2D 모드 복귀 함수 호출
        Debug.Log("2D 모드로 즉시 복귀 (해제됨)");
    }

    public void ReleaseForFalling()
    {
        transform.parent = originParent;
        rigidBody.isKinematic = false;

        // Y축 고정 해제
        rigidBody.constraints = defaultConstraints & ~RigidbodyConstraints.FreezePositionY;

        Debug.Log("3D 낙하 모드로 해제됨 (Y축 고정 해제)");
    }

    public void Enter2DMode(float targetYLevel)
    {
        if (rigidBody.isKinematic)
        {
            rigidBody.isKinematic = false;
        }

        // Y축 다시 고정
        rigidBody.constraints = defaultConstraints;

        // 'targetYLevel'을 Snap 함수로 전달
        SnapToPlayfield(targetYLevel);
        Debug.Log($"2D 모드 복귀 (Y축 잠김, Y 높이: {targetYLevel})");
    }

    private void SnapToPlayfield(float targetYLevel)
    {
        Vector3 snappedPosition = transform.position;

        // 하드코딩된 'playfieldYLevel' 대신 'targetYLevel' 사용
        snappedPosition.y = targetYLevel;

        transform.position = snappedPosition;

        // Y축 속도도 0으로 (낙하 속도 제거)
        Vector3 flatVelocity = rigidBody.linearVelocity;
        flatVelocity.y = 0;
        rigidBody.linearVelocity = flatVelocity;
    }
}