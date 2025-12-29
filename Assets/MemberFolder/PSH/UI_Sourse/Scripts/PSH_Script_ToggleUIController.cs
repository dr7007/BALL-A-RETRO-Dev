using UnityEngine;
using UnityEngine.UI;

public class PSH_Script_ToggleUIController : MonoBehaviour
{
    private Animator anim;
    private Toggle toggle;

    [Header("UI References")]
    public Text label; 
    private PSH_Script_UI_SoundBridge bridge;

    void Awake()
    {
        anim = GetComponent<Animator>();
        toggle = GetComponent<Toggle>();
        bridge = FindFirstObjectByType<PSH_Script_UI_SoundBridge>();

        if (anim != null) anim.updateMode = AnimatorUpdateMode.UnscaledTime; 

        // 1. 저장된 데이터가 있으면 토글 스위치 위치 초기화
        bool savedState = PlayerPrefs.GetInt("Settings_VFX_On", 1) == 1;
        toggle.isOn = savedState;

        // 2. 리스너 연결
        toggle.onValueChanged.AddListener(OnToggleChanged);
        
        // 3. 초기 비주얼(텍스트, 애니메이션) 업데이트
        UpdateVisuals(savedState);
    }

    public void OnToggleChanged(bool value)
    {
        UpdateVisuals(value);

        // 실제 로직 및 데이터 저장 요청
        if (bridge != null) bridge.ToggleVFX(value);
        else PlayerPrefs.SetInt("Settings_VFX_On", value ? 1 : 0);
    }

    private void UpdateVisuals(bool value)
    {
        if (anim != null) anim.SetBool("isOn", value);
        if (label != null) label.text = value ? "ON" : "OFF";
    }
}