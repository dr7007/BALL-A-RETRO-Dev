using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Toggle))]
public class PSH_Script_ToggleUIController : MonoBehaviour
{
    private Animator anim;
    private Toggle toggle;

    [Header("UI References")]
    public Text label; 
    [SerializeField] private Button detailsButton;
    [SerializeField] private TMP_FontAsset effectsFont;
    private PSH_Script_EffectOptionsPanel detailsPanel;

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
        if (detailsButton != null) detailsButton.onClick.AddListener(OpenDetails);
        
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

    private void OnDisable()
    {
        PSH_Script_EffectSettings.Changed -= Sync;
        if (detailsPanel != null) detailsPanel.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggleChanged);
        if (detailsButton != null) detailsButton.onClick.RemoveListener(OpenDetails);
        if (detailsPanel != null) Destroy(detailsPanel.gameObject);
    }

    public void OpenDetails()
    {
        if (detailsPanel == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            detailsPanel = PSH_Script_EffectOptionsPanel.Create(effectsFont,
                canvas != null ? Mathf.Max(150, canvas.sortingOrder + 10) : 150);
        }
        detailsPanel.gameObject.SetActive(true);
    }

    private void Sync(bool value)
    {
        toggle.SetIsOnWithoutNotify(value);
        UpdateVisuals(value);
    }

    private void UpdateVisuals(bool value)
    {
        if (anim != null && anim.runtimeAnimatorController != null) anim.SetBool("isOn", value);
        if (label != null) label.text = value ? (PSH_Script_EffectSettings.AllEnabled ? "ON" : "일부") : "OFF";
    }
}
