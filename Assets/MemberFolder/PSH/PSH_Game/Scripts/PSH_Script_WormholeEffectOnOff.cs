// PSH_Script_WormholeEffectOnOff.cs (이벤트 기반 버전)
using UnityEngine;

[RequireComponent(typeof(YJ_Script_WormholeController))]
public class PSH_Script_WormholeEffectOnOff : MonoBehaviour
{
    [SerializeField]
    private GameObject wormholeEffectObject;
    private YJ_Script_WormholeController wormholeController;

    private void Awake()
    {
        wormholeController = GetComponent<YJ_Script_WormholeController>();
    }

    private void Start()
    {
        if (wormholeEffectObject == null)
        {
            Debug.LogError("Wormhole Effect Object가 할당되지 않았습니다.", this.gameObject);
            this.enabled = false;
            return;
        }

        // 초기 상태 동기화
        HandleActivationChanged(wormholeController.isActivated);
    }

    private void OnEnable()
    {
        // 1. YJ 스크립트의 '방송'을 '구독'
        wormholeController.OnActivationChanged += HandleActivationChanged;
    }

    private void OnDisable()
    {
        // 2. 오브젝트가 꺼질 때 '구독 해지' (메모리 누수 방지)
        wormholeController.OnActivationChanged -= HandleActivationChanged;
    }

    // 3. '방송'이 올 때만(상태가 변할 때만) 이 함수가 호출됨
    private void HandleActivationChanged(bool isActive)
    {
        // [수정] Debug.Log 추가
        Debug.Log($"[PSH_Script] 이벤트 수신! 이펙트 상태 변경: {isActive}", this.gameObject);

        if (wormholeEffectObject != null)
        {
            wormholeEffectObject.SetActive(isActive);
        }
    }

    // 4. Update() 함수 자체가 필요 없음!
}