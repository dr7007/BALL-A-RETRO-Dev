using UnityEngine;

public class KHS_Script_PlungerController : MonoBehaviour
{
    [Header("발사 설정")]
    [Tooltip("최소 발사 힘")]
    [SerializeField] private float minForce = 1f;

    [Tooltip("최대 발사 힘")]
    [SerializeField] private float maxForce = 50f;

    [Tooltip("최대 힘까지 도달하는 시간 (초)")]
    [SerializeField] private float chargeTime = 2f;

    // 내부 변수
    private float currentForce;
    [SerializeField]
    private Rigidbody ballRigidbody;
    [SerializeField]
    private bool isBallReady = false;

    private void Start()
    {
        currentForce = minForce;
    }

    private void Update()
    {
        if (!isBallReady)
        {
            return;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            if (currentForce < maxForce)
            {
                currentForce += (maxForce - minForce) / chargeTime * Time.deltaTime;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            Launch();
        }
    }

    private void Launch()
    {
        if (ballRigidbody != null)
        {
            ballRigidbody.AddForce(Vector3.forward * currentForce, ForceMode.Impulse);
        }
        currentForce = minForce;
    }

    // 공이 발사 준비 위치에 들어왔을 때 호출
    private void OnTriggerEnter(Collider other)
    {
        // "Ball" 태그를 가진 오브젝트가 들어왔는지 확인
        if (other.CompareTag("Ball"))
        {
            ballRigidbody = other.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
                // 공의 직선 속도와 회전 속도를 즉시 0으로 만들어 튀는 현상을 방지
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;

                isBallReady = true;
            }
        }
    }

    // 공이 발사되어 위치를 벗어났을 때 호출
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            isBallReady = false;
            ballRigidbody = null;
        }
    }
}
