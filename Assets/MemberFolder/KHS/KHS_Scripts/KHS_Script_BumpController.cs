using UnityEngine;

public class KHS_Script_BumpController : MonoBehaviour
{
    public float bounceForce = 2f;

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody ballRb = collision.collider.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // 충돌 지점의 반대 방향으로 힘을 실어 튕겨냄
            Vector3 direction = collision.contacts[0].normal;
            ballRb.AddForce(-direction * bounceForce, ForceMode.Impulse);

            // 여기에 사운드 재생, 파티클 효과 등 추가 가능
        }
    }
}
