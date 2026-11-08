using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class YJ_Script_RoulettePhysicsController : MonoBehaviour
{
    public enum SlotColor
    {
        None, // 유효하지 않음 (-1)
        Green,
        Red,
        Black
    }

    [Header("슬롯 색상 설정")]
    [Tooltip("초록색(0, 00)에 해당하는 숫자들을 입력하세요.")]
    public List<int> greenSlotNumbers;

    [Tooltip("빨간색에 해당하는 숫자들을 입력하세요.")]
    public List<int> redSlotNumbers;

    [Tooltip("검은색에 해당하는 숫자들을 입력하세요.")]
    public List<int> blackSlotNumbers;

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

    private MeshRenderer plateRenderer;
    [SerializeField]
    private MeshRenderer numberRenderer;
    private MeshCollider plateCollider;
    private bool plateRendererEnabled;
    private bool numberRendererEnabled;
    private bool plateColliderEnabled;

    public static event Action<int, SlotColor> OnRouletteResult;

    private void Awake()
    {
        plateRenderer = GetComponent<MeshRenderer>();
        plateCollider = GetComponent<MeshCollider>();
        plateRendererEnabled = plateRenderer != null && plateRenderer.enabled;
        numberRendererEnabled = numberRenderer != null && numberRenderer.enabled;
        plateColliderEnabled = plateCollider != null && plateCollider.enabled;
    }

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
        // Deactivating the wheel stops its coroutine. Clear that partial spin
        // now, but do not change a ball owned by gameplay/scene teardown.
        ResetSequence(false);
    }

    public void ResetRoulette() => ResetSequence(true);

    private void ResetSequence(bool releaseBall)
    {
        StopAllCoroutines();
        isSlowingDown = false;
        lastKnownSlotNumber = -1;
        RestorePlate();

        var ball = capturedBall;
        capturedBall = null;
        if (releaseBall && ball != null)
        {
            ball.ReleaseForFalling(Vector3.zero, false);
        }
    }

    private void RestorePlate()
    {
        // Ball-out can interrupt the five-second hidden window before the
        // coroutine reaches its normal restoration. Retain the prefab's state.
        if (plateRenderer != null) plateRenderer.enabled = plateRendererEnabled;
        if (numberRenderer != null) numberRenderer.enabled = numberRendererEnabled;
        if (plateCollider != null) plateCollider.enabled = plateColliderEnabled;
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

    private SlotColor GetColorFromNumber(int number)
    {
        if (greenSlotNumbers.Contains(number))
        {
            return SlotColor.Green;
        }
        if (redSlotNumbers.Contains(number))
        {
            return SlotColor.Red;
        }
        if (blackSlotNumbers.Contains(number))
        {
            return SlotColor.Black;
        }
        return SlotColor.None; // -1 (경계선) 또는 목록에 없는 숫자
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

        SlotColor resultColor = GetColorFromNumber(lastKnownSlotNumber);

        Debug.Log($"--- 룰렛 결과: {lastKnownSlotNumber} ({resultColor}) ---");
        //OnRouletteResult?.Invoke(lastKnownSlotNumber, resultColor);

        scoreManager.AddScore(lastKnownSlotNumber * 100); // 룰렛 숫자의 100배를 기존 스코어에 더함(로그라이크 선택지로 변형 가능)

        // 점수 계산 후 2초 대기
        yield return new WaitForSeconds(2.0f);

        Debug.Log("룰렛 판 숨기기!");
        if (plateRenderer != null) plateRenderer.enabled = false;
        if (numberRenderer!= null) numberRenderer.enabled = false;
        if (plateCollider != null) plateCollider.enabled = false;

        capturedBall = null; // 공 추적 중지
        yield return new WaitForSeconds(5.0f);

        Debug.Log("룰렛 판 다시 생성.");
        RestorePlate();

        isSlowingDown = false;
    }
}
