using System;
using UnityEngine;

public class KHS_Script_BatteryLedManager : MonoBehaviour
{
    public static event Action HoleCoverActiveEvt;
    public static event Action HoleCoverUnActiveEvt;

    [SerializeField]
    private KHS_Script_BatteryLedReceiver[] receivers;
    private KHS_Script_ScoreManager scoreManager;

    [Header("연결")]
    [Tooltip("5개가 켜졌을 때 활성화할 오브젝트")]
    [SerializeField]
    private GameObject goEnable; // 인스펙터에서 뚜껑 오브젝트를 연결해주세요.

    private int ledOnMount = 0;
    private bool init_delay = false;
    private bool isForced = false;

    private bool hasAwardedScore = false;

    [Space(5)]
    [Header("개별 타겟 이벤트 (토글용)")]
    [Tooltip("개별 타겟이 맞을 때마다 호출됩니다. (int: 현재까지 맞은 타겟 수)")]
    public IntUnityEvent OnTargetHitCountChanged; // int 값을 전달할 수 있는 새 이벤트

    private void OnEnable()
    {
        // 공이 아웃되면 뚜껑을 닫는 함수(ResetDropHole)를 연결
        KHS_Script_BallOutController.BallOutEvt += ResetGoEnable;
    }

    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= ResetGoEnable;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        init_delay = false;
        scoreManager = FindAnyObjectByType<KHS_Script_ScoreManager>();

        ResetGoEnable();
    }

    // Update is called once per frame
    void Update()
    {
        if(!init_delay)
        {
            foreach (var receiver in receivers)
            {
                receiver.GetComponent<ChangeSpriteRenderer>().F_ChangeSprite_Off();
            }
            init_delay = true;
        }

        ledOnMount = 0;

        for (int i = 0; i < receivers.Length; i++)
        {
            if (receivers[i].LEDCheck())
            {
                ledOnMount++;
                OnTargetHitCountChanged?.Invoke(ledOnMount);
            }
        }
        
        if(ledOnMount >= 5)
        {
            if (!hasAwardedScore)
            {
                scoreManager.AddScore(3000);
                hasAwardedScore = true;

                if (goEnable != null)
                {
                    goEnable.SetActive(true);
                    Debug.Log("오브젝트가 활성화 되었습니다!");
                    HoleCoverActiveEvt.Invoke();
                }
            }
        }
        else
        {
            hasAwardedScore = false;
            ledOnMount = 0;
        }
    }

    private void ResetGoEnable()
    {
        if (goEnable != null && !isForced)
        {
            goEnable.SetActive(false);
            //HoleCoverUnActiveEvt.Invoke();
        }
    }

    // --- 💡 테스트용: 모든 LED 켜기 ---
    [ContextMenu("TEST: Turn All LEDs ON")]
    public void Test_TurnAllLedsOn()
    {
        if (receivers == null || receivers.Length == 0) return;

        Debug.LogWarning("--- 테스트: 모든 LED를 강제로 켭니다. ---");
        foreach (var receiver in receivers)
        {
            // 1. 상태를 켬
            receiver.IsOnResponse();
            // 2. 스프라이트도 켬 (ChangeSpriteRenderer가 null이 아닐 때만)
            receiver.GetComponent<ChangeSpriteRenderer>()?.F_ChangeSprite_On();
        }
    }

    // --- 💡 테스트용: 모든 LED 끄기 ---
    [ContextMenu("TEST: Turn All LEDs OFF")]
    public void Test_TurnAllLedsOff()
    {
        if (receivers == null || receivers.Length == 0) return;

        Debug.LogWarning("--- 테스트: 모든 LED를 강제로 끕니다. ---");
        foreach (var receiver in receivers)
        {
            // 1. 상태를 끔
            receiver.ResetLED();
            // 2. 스프라이트도 끔
            receiver.GetComponent<ChangeSpriteRenderer>()?.F_ChangeSprite_Off();
        }
    }
    public void ForcedOn()
    {
        isForced = true;
        goEnable.SetActive(true);
        HoleCoverActiveEvt.Invoke();
    }
}
