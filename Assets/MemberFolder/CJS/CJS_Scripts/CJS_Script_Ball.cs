using UnityEngine;

public class CJS_Script_Ball : MonoBehaviour
{
    public float damage = 25f;   // 뎀지
    private void OnCollisionEnter(Collision collision)
    {
        // 볼과 몬스터가 충돌하면
        if (collision.gameObject.CompareTag("Monster"))
        {
            CJS_Script_Monster monster = collision.gameObject.GetComponent<CJS_Script_Monster>();
            if (monster != null)
            {
                monster.TakeDamage(damage); 
            }

            // 볼이 몬스터에 맞았을 때 반동을 줄 수 있도록 물리적 반응 추가
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 충돌 후 튕겨 나가는 힘을 적용
                rb.AddForce(collision.contacts[0].normal * 10f, ForceMode.Impulse);
            }
        }
    }
}
