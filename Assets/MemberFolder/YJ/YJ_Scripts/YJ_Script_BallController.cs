using System;
using UnityEngine;
using UnityEngine.UIElements;

public class YJ_Script_BallController : MonoBehaviour
{
    public static event Action GameOverEvt;

    public enum ControlMode
    {
        Pinball, // 핀볼 물리 (Z축 중력)
        PacMan   // 팩맨 조작 (WSAD 입력)
    }
    private ControlMode currentMode = ControlMode.Pinball;

    [Header("팩맨 모드 속도")]
    [SerializeField]
    private float pacManSpeed = 3f; // 팩맨 모드일 때의 이동 속도

    [Header("팩맨 벽 감지")]
    [Tooltip("벽을 감지할 Raycast의 거리 (공 반지름 + 여유)")]
    [SerializeField]
    private float wallCheckDistance = 0.6f;

    [Tooltip("벽으로 인식할 오브젝트의 레이어 마스크")]
    [SerializeField]
    private LayerMask wallLayerMask;

    private Vector3 pacManDirection = Vector3.zero;

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
        currentMode = ControlMode.Pinball;
    }

    void Update()
    {
        // 팩맨 모드일 때만 키 입력을 받음
        if (currentMode == ControlMode.PacMan)
        {
            HandlePacManInput();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (currentMode == ControlMode.Pinball)
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
        }
        else if (currentMode == ControlMode.PacMan)
        {
            rigidBody.linearVelocity = pacManDirection * pacManSpeed;
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

    private void OnTriggerEnter(Collider other)
    {
        bool isFalling = (rigidBody.constraints & RigidbodyConstraints.FreezePositionY) == 0;

        if (!isFalling) return;

        if (other.gameObject.CompareTag("Playfield"))
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
        currentMode = ControlMode.Pinball;
        rigidBody.constraints = defaultConstraints;
    }

    public int BallCountResponse()
    {
        return BallCount;
    }

    private void HandlePacManInput()
    {
        // 1. 유저가 누른 키에 따라 '시도할 방향'을 결정
        Vector3 tryDirection = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.W))
        {
            tryDirection = Vector3.forward;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            tryDirection = Vector3.back;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            tryDirection = Vector3.left;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            tryDirection = Vector3.right;
        }

        // 2. 새로운 방향키 입력이 있었는지 확인 (tryDirection이 (0,0,0)이 아님)
        if (tryDirection != Vector3.zero)
        {
            // 3. --- (핵심) Raycast로 '시도할 방향'에 벽이 있는지 확인 ---
            // (공의 현재 위치에서, tryDirection으로, wallCheckDistance만큼, wallLayerMask만)
            bool isWallAhead = Physics.Raycast(
                                    transform.position,     // 1. 시작 위치 (공 중앙)
                                    tryDirection,           // 2. 검사할 방향
                                    wallCheckDistance,      // 3. 검사할 거리
                                    wallLayerMask           // 4. "Wall" 레이어만 감지
                                 );

            // 4. Raycast가 아무것도 맞추지 않았다면 (벽이 없다면)
            if (!isWallAhead)
            {
                // 5. 그제서야 pacManDirection을 변경
                pacManDirection = tryDirection;
            }
            // 6. (else) Raycast가 벽에 맞았다면:
            //    tryDirection을 무시합니다. (아무것도 하지 않음)
            //    pacManDirection은 기존 값을 유지하고, 공은 원래 가던 방향으로 계속 갑니다.
        }
    }

    public void SetControlMode(ControlMode newMode)
    {
        currentMode = newMode;
        Debug.Log("볼 컨트롤 모드 변경: " + newMode.ToString());

        if (newMode == ControlMode.PacMan)
        {
            rigidBody.linearVelocity = Vector3.zero;

            pacManDirection = Vector3.forward;
        }
        else
        {
            pacManDirection = Vector3.zero;
        }
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

        if (currentMode == ControlMode.Pinball)
        {
            rigidBody.linearVelocity = Vector3.zero;
        }
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