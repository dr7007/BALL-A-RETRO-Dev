using UnityEngine;
using UnityEngine.UI;
using TMPro; // 텍스트 제어를 위해 반드시 필요합니다.

public class PSH_Script_ToggleUIController : MonoBehaviour
{
    private Animator anim;
    private Toggle toggle;

    [Header("연결할 텍스트")]
    public Text label; // Hierarchy에 있는 Text(TMP)를 여기에 연결하세요.

    void Awake()
    {
        anim = GetComponent<Animator>();
        toggle = GetComponent<Toggle>();

        // 게임이 멈춰도 애니메이션이 돌아가도록 설정
        anim.updateMode = AnimatorUpdateMode.UnscaledTime; 

        toggle.onValueChanged.AddListener(OnToggleChanged);
        OnToggleChanged(toggle.isOn);
    }

    public void OnToggleChanged(bool value)
    {
        // 1. 애니메이션 파라미터 전달 (기존 코드)
        anim.SetBool("isOn", value);

        // 2. 텍스트 변경
        if (label != null)
        {
            // 삼항 연산자를 쓰면 코드가 깔끔해집니다.
            label.text = value ? "ON" : "OFF";
            
            // (보너스) 글자 색상도 바꾸고 싶다면 아래 주석을 푸세요.
            // label.color = value ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        }
    }
}