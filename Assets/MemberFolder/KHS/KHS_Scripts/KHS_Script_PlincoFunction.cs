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
    private bool ownsSettlement;
    private Coroutine settlement;

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += CancelSettlement;
        KHS_Script_ResetController.OnReset += CancelSettlement;
        KHS_Script_ScoreManager.OnGameOver += CancelSettlement;
        KHS_Script_ScoreManager.OnGameClear += CancelSettlement;
    }
    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= CancelSettlement;
        KHS_Script_ResetController.OnReset -= CancelSettlement;
        KHS_Script_ScoreManager.OnGameOver -= CancelSettlement;
        KHS_Script_ScoreManager.OnGameClear -= CancelSettlement;
        CancelSettlement();
    }
    private void CancelSettlement()
    {
        // Demo and tutorial retain their original coroutine behavior.
        if (!ownsSettlement) return;
        if (settlement != null) StopCoroutine(settlement);
        settlement = null;
        ownsSettlement = false;
        if (scoreManager != null) scoreManager.EndBonusSettlement();
    }

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
            if (!BallARetroPlatform.IsDemoBuild && scoreManager != null && !scoreManager.IsTutorialRun)
            {
                if (!isActiveAndEnabled || portalCon == null || other.attachedRigidbody == null
                    || !scoreManager.TryBeginBonusSettlement()) return;
                ownsSettlement = true;
            }
            settlement = StartCoroutine(ScoreMultipleAndTeleportCoroutine(other));
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
        ReturnPortalEvt?.Invoke();
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
        if (ownsSettlement)
        {
            ownsSettlement = false;
            scoreManager.EndBonusSettlement();
            settlement = null;
        }
    }
    public int ScoreMulti_Response()
    {
        return scoreMulti;
    }

    public void AddScoreMulti(int _multi)
    {
        scoreMulti *= _multi;
    }
}
