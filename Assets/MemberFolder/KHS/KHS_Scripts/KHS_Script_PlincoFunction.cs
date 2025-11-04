using System;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class KHS_Script_PlincoFunction : MonoBehaviour
{
    public static event Action ReturnPortalEvt;

    [SerializeField]
    private int scoreMulti = 1;
    [SerializeField]
    private KHS_Script_PortalController portalCon;

    [Header("방출 속도 랜덤 설정")]
    [Tooltip("방출 속도의 최소값과 최대값")]
    [SerializeField] private Vector2 randomSpeedRange = new Vector2(10f, 30f);

    [Tooltip("방출 각도 범위")]
    [SerializeField] private float randomAngleRange = 360f;


    private KHS_Script_ScoreManager scoreManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Ball"))
        {
            StartCoroutine(ScoreMultipleAndTeleportCoroutine(other));
        }
    }

    private IEnumerator ScoreMultipleAndTeleportCoroutine(Collider other)
    {
        yield return new WaitForSeconds(1f);
        if (scoreManager == null)
        {
            Debug.Log("ScoreManager가 씬에 없음");
        }
        else
        {
            scoreManager.MultiplyScore(scoreMulti);
        }
        yield return new WaitForSeconds(1f);
        ReturnPortalEvt.Invoke();
        yield return new WaitForSeconds(2f);
        portalCon.PortalTempUnactive();

        Rigidbody rb = other.GetComponent<Rigidbody>();
        other.transform.position = portalCon.transform.position;

        Vector3 baseDir = portalCon.transform.right;

        // randomAngleRange 내에서 임의 회전
        Quaternion randomRot = Quaternion.Euler(
            UnityEngine.Random.Range(-randomAngleRange, randomAngleRange),
            UnityEngine.Random.Range(-randomAngleRange, randomAngleRange),
            0f
        );
        Vector3 randomDir = randomRot * baseDir;

        // 속도 랜덤화
        float randomSpeed = UnityEngine.Random.Range(randomSpeedRange.x, randomSpeedRange.y);
        rb.isKinematic = false;
        // 최종 적용
        rb.linearVelocity = randomDir.normalized * randomSpeed;
    }

    public void AddScoreMulti(int _multi)
    {
        scoreMulti += _multi;
    }
}
