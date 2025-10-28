using UnityEngine;

public class CJS_Script_Monster : MonoBehaviour
{
    public float speed = 3f;             
    public Vector3[] waypoints;          
    private int currentWaypointIndex = 0;

    public float health = 100f;          
    public Animator monsterAnimator;     

    private float lastHitTime = 0f;      
    private int hitCount = 0;            

    private Rigidbody rb;

    void Start()
    {
        if (monsterAnimator == null)
            monsterAnimator = GetComponent<Animator>(); 

        rb = GetComponent<Rigidbody>(); 
        rb.useGravity = true;           
        rb.isKinematic = false;         

        // 웨이포인트 배열 설정
        if (waypoints.Length == 0)
        {
            //웨이포인트
            waypoints = new Vector3[4];
            waypoints[0] = new Vector3(0, 0, 0);  
            waypoints[1] = new Vector3(5, 0, 0);  
            waypoints[2] = new Vector3(5, 0, 5);  
            waypoints[3] = new Vector3(0, 0, 5);  
        }
    }

    void Update()
    {
        // 목표 웨이포인트로 이동
        MoveToWaypoint();
    }

    // 웨이포인트로 이동하는 함수
    void MoveToWaypoint()
    {
        if (waypoints.Length == 0) return;

        // 현재 목표 웨이포인트와의 거리 계산
        Vector3 targetPosition = waypoints[currentWaypointIndex];
        float step = speed * Time.deltaTime; // 이동할 거리 계산

        // 목표 웨이포인트로 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

        // 웨이포인트에 도달하면 다음 웨이포인트로 이동
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentWaypointIndex++;  // 다음 웨이포인트로 이동
            if (currentWaypointIndex >= waypoints.Length)
            {
                currentWaypointIndex = 0;  // 처음 웨이포인트로 돌아감
            }
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        lastHitTime = Time.time;  // 마지막 맞은 시간 업데이트
        hitCount++;  // 맞은 횟수 증가

        if (health <= 0)
        {
            Die();  // 체력이 0이 되면 죽음 처리
        }
        else
        {
            // 맞은 횟수에 따른 애니메이션 처리
            switch (hitCount)
            {
                case 1:
                    monsterAnimator.SetTrigger("Hit1");  // 첫 번째 맞았을 때
                    break;
                case 2:
                    monsterAnimator.SetTrigger("Hit2");  // 두 번째 맞았을 때
                    break;
                case 3:
                    monsterAnimator.SetTrigger("Hit3");   // 세 번째 맞았을 때
                    break;
                default:
                    monsterAnimator.SetTrigger("Die");    // 주금
                    break;
            }
        }
    }

    void Die()
    {
        // 죽는 애니메이션
        monsterAnimator.SetTrigger("Die");

        // 1초 후에 몬스터 제거
        Destroy(gameObject, 1f);
    }
}
