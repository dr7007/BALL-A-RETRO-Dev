using Unity.VisualScripting;
using UnityEngine;

public class KHS_Script_BatteryLedController : MonoBehaviour
{
    [Header("LED connected to the bumper")]
    public GameObject obj_Led;             // LED 오브젝트
    private ChangeSpriteRenderer ledRenderer; // LED 제어용 스크립트 (ChangeSpriteRenderer)
    private KHS_Script_BatteryLedReceiver led_Receiver;

    private void Start()
    {
        ledRenderer = obj_Led.GetComponent<ChangeSpriteRenderer>();
        led_Receiver = obj_Led.GetComponent<KHS_Script_BatteryLedReceiver>();
    }

    private void OnEnable()
    {
        KHS_Script_BallOutController.BallOutEvt += ResetEffect;
    }
    private void OnDisable()
    {
        KHS_Script_BallOutController.BallOutEvt -= ResetEffect;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 공(Ball)과 충돌했을 때만 반응하도록 (선택사항)
        if (!collision.gameObject.CompareTag("Ball"))
            return;

        // LED 전환
        if (ledRenderer != null)
        {
            led_Receiver.IsOnResponse();
            ledRenderer.F_ChangeSprite_On();
        }
    }

    public void ResetEffect()
    {
        if (ledRenderer != null)
        {
            ledRenderer.F_ChangeSprite_Off();
        }
    }


}
