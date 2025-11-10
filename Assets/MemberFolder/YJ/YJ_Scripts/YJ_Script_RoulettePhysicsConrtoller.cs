using System;
using System.Collections;
using UnityEngine;


public class YJ_Script_RoulettePhysicsController : MonoBehaviour
{
    [Header("회전 설정")]
    [Tooltip("초기 상태: 룰렛이 도는 속도 (초당 각도)")]
    public float constantSpinSpeed = -90f;
    [Tooltip("정지 시퀀스가 시작되고 완전히 멈추는 데 걸리는 시간")]
    public float stopDuration = 5f;

    private YJ_Script_BallController capturedBall;
    private bool isSlowingDown = false;

    private int lastKnownSlotNumber = -1;

    //public static event Action<int> OnRouletteResult; // 점수 계산기에 보낼 이벤트

    private KHS_Script_ScoreManager scoreManager;

    private void Start()
    {
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();
    }

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += ResetRoulette;
    }
    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= ResetRoulette;
    }

    public void ResetRoulette()
    {
        StopAllCoroutines();
        isSlowingDown = false;

        if (capturedBall != null)
        {
            capturedBall.ReleaseForFalling(Vector3.zero);
            capturedBall = null;
        }
    }

    void Update()
    {
        if (!isSlowingDown)
        {
            transform.Rotate(Vector3.up, constantSpinSpeed * Time.deltaTime, Space.World);
        }
    }

    public void StartStopSequence(YJ_Script_BallController ball)
    {
        if (isSlowingDown) return;

        isSlowingDown = true;

        capturedBall = ball;

        StartCoroutine(SlowDownAndStop());
    }

    public void UpdateCurrentSlot(int number)
    {
        // 룰렛이 멈추는 동안에만 슬롯 번호를 갱신
        if (isSlowingDown)
        {
            lastKnownSlotNumber = number;
        }
    }

    private IEnumerator SlowDownAndStop()
    {
        float timer = 0f;
        float startSpeed = constantSpinSpeed;
        lastKnownSlotNumber = -1; // 멈추기 시작할 때 리셋

        while (timer < stopDuration)
        {
            timer += Time.deltaTime;
            float t = timer / stopDuration;
            t = 1 - (1 - t) * (1 - t);
            float currentSpeed = Mathf.Lerp(startSpeed, 0f, t);
            transform.Rotate(Vector3.up, currentSpeed * Time.deltaTime, Space.World);
            yield return null;
        }

        Debug.Log($"--- 룰렛 결과: {lastKnownSlotNumber} ---");
        //OnRouletteResult?.Invoke(lastKnownSlotNumber);
        scoreManager.MultiplyScore(lastKnownSlotNumber);
    }
}