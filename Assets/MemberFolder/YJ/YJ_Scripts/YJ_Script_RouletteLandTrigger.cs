using UnityEngine;

// 1. '착지 감지' 트리거 (Roulette_Land_Trigger)에 부착
public class YJ_Script_RouletteLandTrigger : MonoBehaviour
{
    // 2. 부모 오브젝트(Roulette_Body)에 있는 메인 컨트롤러
    private YJ_Script_RoulettePhysicsController mainController;

    void Start()
    {
        // 3. 부모에게서 메인 컨트롤러 스크립트를 찾아 저장
        mainController = GetComponentInParent<YJ_Script_RoulettePhysicsController>();
        if (mainController == null)
        {
            Debug.LogError("부모 오브젝트에 Roulette_Physics_Controller가 없습니다!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            YJ_Script_BallController ball = other.GetComponent<YJ_Script_BallController>();
            if (ball != null && mainController != null)
            {
                // 4. 공이 닿으면, 메인 컨트롤러의 '정지 시퀀스' 시작
                mainController.StartStopSequence(ball);

                // 5. 한 번만 작동하도록 자신을 비활성화
                GetComponent<Collider>().enabled = false;
            }
        }
    }

    // (참고) 이 트리거는 BallOutEvt에 연결되어 리셋되어야 함
    // (이 부분은 Roulette_Physics_Controller에서 통합 관리)
}