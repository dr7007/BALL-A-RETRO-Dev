using UnityEngine;

// 37개의 각 숫자 칸(Slot_XX)에 부착되는 스크립트
public class YJ_Script_SlotSensor : MonoBehaviour
{
    [Header("이 칸의 실제 숫자")]
    [Tooltip("이 칸에 해당하는 숫자를 정확히 입력하세요.")]
    public int slotNumber;

    private YJ_Script_RoulettePhysicsController mainController;

    void Start()
    {
        // 부모(Roulette_Body)에 있는 메인 컨트롤러를 찾아 저장
        mainController = GetComponentInParent<YJ_Script_RoulettePhysicsController>();
    }

    // 공이 이 칸(트리거)에 '진입'했을 때
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            YJ_Script_BallController ball = other.GetComponent<YJ_Script_BallController>();
            if (ball != null && mainController != null)
            {
                // 메인 컨트롤러에게 "공이 이 칸(slotNumber)에 들어왔으니 정지 시퀀스 시작!"
                mainController.StartStopSequence(ball);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            if (mainController != null)
            {
                // 메인 컨트롤러에게 "공이 지금 이 칸 위에 있다"고 계속 알림
                mainController.UpdateCurrentSlot(slotNumber);
            }
        }
    }
}