using UnityEngine;

public class KHS_Script_PlungerController : MonoBehaviour
{
    [Header("�߻� ����")]
    [Tooltip("�ּ� �߻� ��")]
    [SerializeField] private float minForce = 1f;

    [Tooltip("�ִ� �߻� ��")]
    [SerializeField] private float maxForce = 50f;

    [Tooltip("�ִ� ������ �����ϴ� �ð� (��)")]
    [SerializeField] private float chargeTime = 2f;

    // ���� ����
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

    // ���� �߻� �غ� ��ġ�� ������ �� ȣ��
    private void OnTriggerEnter(Collider other)
    {
        // "Ball" �±׸� ���� ������Ʈ�� ���Դ��� Ȯ��
        if (other.CompareTag("Ball"))
        {
            ballRigidbody = other.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
                // ���� ���� �ӵ��� ȸ�� �ӵ��� ��� 0���� ����� Ƣ�� ������ ����
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;

                isBallReady = true;
            }
        }
    }

    // ���� �߻�Ǿ� ��ġ�� ����� �� ȣ��
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            isBallReady = false;
            ballRigidbody = null;
        }
    }
}
