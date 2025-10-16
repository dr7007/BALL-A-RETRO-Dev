using UnityEngine;

public class YJ_Script_BallResetter : MonoBehaviour
{
    [Header("리셋 설정")]
    [Tooltip("리셋할 공의 Transform 컴포넌트")]
    [SerializeField] private Transform ballTransform;

    [Tooltip("공이 되돌아갈 위치")]
    [SerializeField] private Vector3 resetPosition = new Vector3(0, 1, 0);

    // 공의 물리 효과를 제어하기 위한 Rigidbody 변수
    private Rigidbody ballRigidbody;

    private void Start()
    {
        if (ballTransform != null)
        {
            ballRigidbody = ballTransform.GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBall();
        }
    }

    private void ResetBall()
    {
        if (ballTransform == null || ballRigidbody == null)
        {
            Debug.LogError("리셋할 공이 지정되지 않았습니다!");
            return;
        }

        // 1. 공의 위치를 지정된 resetPosition으로 즉시 이동
        ballTransform.position = resetPosition;

        // 2. 공의 모든 움직임과 회전을 멈춤
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
    }
}