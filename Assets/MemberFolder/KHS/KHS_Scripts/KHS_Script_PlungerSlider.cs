using UnityEngine;
using UnityEngine.UI;

public class KHS_Script_PlungerSlider : MonoBehaviour
{
    [SerializeField] private KHS_Script_PlungerController plunCon;
    [SerializeField] private Slider slider;

    private void Awake()
    {
        plunCon = FindAnyObjectByType<KHS_Script_PlungerController>();
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        if (plunCon == null || slider == null) return;

        // 볼 준비 완료일 때만 슬라이더 업데이트
        if (plunCon.isBallReady)
        {
            // 현재 차지 비율 계산 (0~1)
            slider.value = plunCon.currentGaze;
        }
        else
        {
            // 볼이 없으면 초기화
            slider.value = 0f;
        }
    }
}
