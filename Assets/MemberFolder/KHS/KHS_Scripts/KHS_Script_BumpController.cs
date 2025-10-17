using UnityEngine;

public class KHS_Script_BumpController : MonoBehaviour
{
    public float bounceForce = 2f;

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody ballRb = collision.collider.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // �浹 ������ �ݴ� �������� ���� �Ǿ� ƨ�ܳ�
            Vector3 direction = collision.contacts[0].normal;
            ballRb.AddForce(-direction * bounceForce, ForceMode.Impulse);

            // ���⿡ ���� ���, ��ƼŬ ȿ�� �� �߰� ����
        }
    }
}
