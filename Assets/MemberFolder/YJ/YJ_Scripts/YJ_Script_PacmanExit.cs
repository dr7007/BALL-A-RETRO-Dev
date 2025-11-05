// PacManExitTrigger.cs (새 스크립트)
using UnityEngine;

public class YJ_Script_PacManExit : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            YJ_Script_BallController ball = other.GetComponent<YJ_Script_BallController>();
            if (ball != null)
            {
                // 1. 컨트롤 모드를 '핀볼'로 복귀
                ball.SetControlMode(YJ_Script_BallController.ControlMode.Pinball);

                // 2. (선택) 핀볼 모드로 돌아갈 때 Y축 고정을 풀고 낙하
                // ball.ReleaseForFalling(); 

                // 3. (선택) 핀볼 모드로 돌아가 1층으로 즉시 이동
                // (BallController에 public float playfieldYLevel 변수가 있어야 함)
                // ball.Enter2DMode(0f); // 0f = 1층의 Y 높이
            }
        }
    }
}