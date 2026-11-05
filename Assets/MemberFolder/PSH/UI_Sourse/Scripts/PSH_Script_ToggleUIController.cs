using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class PSH_Script_ToggleUIController : MonoBehaviour
{
    private Animator anim;
    private Toggle toggle;

    [Header("UI References")]
    public Text label; 

    void Awake()
    {
        anim = GetComponent<Animator>();
        toggle = GetComponent<Toggle>();

        if (anim != null) anim.updateMode = AnimatorUpdateMode.UnscaledTime; 

        // 1. 저장된 데이터가 있으면 토글 스위치 위치 초기화
        bool savedState = PSH_Script_EffectSettings.IsEnabled;
        toggle.SetIsOnWithoutNotify(savedState);

        // 2. 리스너 연결
        toggle.onValueChanged.AddListener(OnToggleChanged);
        
        // 3. 초기 비주얼(텍스트, 애니메이션) 업데이트
        UpdateVisuals(savedState);
    }

    public void OnToggleChanged(bool value)
    {
        UpdateVisuals(value);

        // 실제 로직 및 데이터 저장 요청
        PSH_Script_EffectSettings.SetEnabled(value);
    }

    private void OnEnable()
    {
        PSH_Script_EffectSettings.Changed += Sync;
        Sync(PSH_Script_EffectSettings.IsEnabled);
    }

    private void OnDisable() => PSH_Script_EffectSettings.Changed -= Sync;
    private void OnDestroy() => toggle.onValueChanged.RemoveListener(OnToggleChanged);

    private void Sync(bool value)
    {
        toggle.SetIsOnWithoutNotify(value);
        UpdateVisuals(value);
    }

    private void UpdateVisuals(bool value)
    {
        if (anim != null && anim.runtimeAnimatorController != null) anim.SetBool("isOn", value);
        if (label != null) label.text = value ? "ON" : "OFF";
    }
}
