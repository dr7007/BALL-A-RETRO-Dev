using System.Linq;
using UnityEngine;
using UnityEngine.Events;
// (이름은 아무거나 상관없지만, 가독성을 위해 만듭니다)
[System.Serializable]
public class IntUnityEvent : UnityEvent<int> { }
public class YJ_Script_DropTargetManager : MonoBehaviour
{
    [Header("타겟 유형")]
    [SerializeField]
    private bool isDropTargets = true; // 활성화 시 드롭 타겟으로 작동, 비활성화 시 고정 타겟으로 작동

    [Header("타겟 위치 설정")]
    [SerializeField]
    private float ActivatePosY = 0.015f;
    [SerializeField]
    private float DesactivatePosY = -0.03f;

    [Header("타겟 속도")]
    [SerializeField]
    private float MoveSpeed = 5;

    [Space(10)]
    [Header("이벤트 연결")]

    [Tooltip("모든 타겟이 비활성화되었을 때 호출할 이벤트")]
    public UnityEvent OnAllTargetsDeactivated;

    [Tooltip("타겟이 하나라도 다시 활성화될 때 (리셋 시) 호출할 이벤트")]
    public UnityEvent OnTargetsActivated;

    // 이 매니저가 관리할 타겟들의 리스트
    private YJ_Script_DropTargetController[] targets;

    // 이전 상태를 기억할 변수 추가
    private bool b_AllTargetsWereDown = false;
    
    // --- 추가된 부분 ---
    [Space(5)]
    [Header("개별 타겟 이벤트 (토글용)")]
    [Tooltip("개별 타겟이 맞을 때마다 호출됩니다. (int: 현재까지 맞은 타겟 수)")]
    public IntUnityEvent OnTargetHitCountChanged; // int 값을 전달할 수 있는 새 이벤트
    // --- 여기까지 ---

    private void Awake()
    {
        // 자식 오브젝트에 있는 모든 ManagedDropTarget 스크립트를 찾아옴
        targets = GetComponentsInChildren<YJ_Script_DropTargetController>();

        // 찾아온 모든 자식 타겟들에게 매니저(자기 자신)와 설정값들을 전달
        foreach (YJ_Script_DropTargetController target in targets)
        {
            target.Initialize(this, ActivatePosY, DesactivatePosY, MoveSpeed);
        }

        b_AllTargetsWereDown = AreAllTargetsDesactivated();
    }

    // 자식 타겟이 공에 맞았을 때 호출될 함수
    public void HandleTargetHit(YJ_Script_DropTargetController hitTarget)
    {
         // --- 1. 이미 맞은 타겟인지 '먼저' 확인 ---
        // (IsDesactivated()는 타겟의 현재 상태를 물어보는 것입니다)
        if (hitTarget.IsDesactivated())
        {
            // 이미 비활성화된(이미 맞은) 타겟을 또 맞췄다면,
            // 중복 이벤트를 막기 위해 아무것도 하지 않고 즉시 종료합니다.
            return;
        }

        // --- 2. (신규 타격 확정) 사운드 재생 및 비활성화 ---
        hitTarget.PlayHitSound();

        if (isDropTargets)
        {
            // 타겟을 비활성화시킵니다.
            hitTarget.Desactivate_Object();

            // --- 3. (중요) 비활성화 '시킨 후'에 카운트 계산 및 이벤트 호출 ---
            // GetDeactivatedTargetCount()는 방금 맞은 타겟을 '포함한' 개수를 반환합니다.
            int currentHitCount = GetDeactivatedTargetCount();
            
            Debug.Log($"타겟 히트! 현재 카운트: {currentHitCount}"); // 디버그 로그 추가
            
            OnTargetHitCountChanged.Invoke(currentHitCount);
        }
        
        /* * (참고) 기존의 중복되고 순서가 꼬인 로직은 모두 제거했습니다.
         */

        // --- 4. 모든 타겟이 맞았는지 최종 확인 (블랙홀 용) ---
        CheckAllTargetsState();
    }

    // 현재 비활성화된(맞은) 타겟의 '총 개수'를 반환하는 함수
    public int GetDeactivatedTargetCount()
    {
        if (targets == null) return 0;
        
        // Linq를 사용하여 비활성화된 타겟의 '개수(Count)'를 셉니다.
        return targets.Count(target => target.IsDesactivated());
    }
    
    // 모든 타겟이 비활성화 상태인지 확인하는 함수
    public bool AreAllTargetsDesactivated()
    {
        if (targets == null) return false; // 방어 코드
        // Linq를 사용하여 "모든(All) 타겟이 비활성화(IsDesactivated) 상태인가?"를 한 줄로 검사
        return targets.All(target => target.IsDesactivated());
    }

    // 모든 타겟의 상태를 확인하고 특정 행동을 하는 함수
    private void CheckAllTargetsState()
    {
        // 현재 모든 타겟이 비활성화 상태인지 확인
        bool allCurrentlyDown = AreAllTargetsDesactivated();

        // 상태가 '변경'되었는지 확인
        if (allCurrentlyDown && !b_AllTargetsWereDown)
        {
            // 모든 타겟이 비활성화된 경우
            Debug.Log("모든 타겟 비활성화! OnAllTargetsDeactivated 이벤트를 호출합니다.");
            OnAllTargetsDeactivated.Invoke();
        }
        else if (!allCurrentlyDown && b_AllTargetsWereDown)
        {
            // 타겟이 리셋되어 하나라도 활성화된 경우
            Debug.Log("타겟 리셋! OnTargetsActivated 이벤트를 호출합니다.");
            OnTargetsActivated.Invoke();
        }

        // 현재 상태를 '이전 상태'로 저장 (다음 검사를 위해)
        b_AllTargetsWereDown = allCurrentlyDown;
    }

    // 외부에서 모든 타겟을 활성화시킬 때 사용
    [ContextMenu("모든 타겟 활성화")] // 인스펙터에서 우클릭으로 테스트 가능
    public void ActivateAllTargets()
    {
        // 리셋 직전 상태 확인
        bool wasPreviouslyDown = AreAllTargetsDesactivated();

        foreach (YJ_Script_DropTargetController target in targets)
        {
            target.Activate_Object();
        }

        // 강제 활성화 시에도 이벤트 호출
        if (wasPreviouslyDown)
        {
            Debug.Log("타겟 (강제) 리셋! OnTargetsActivated 이벤트를 호출합니다.");
            OnTargetsActivated.Invoke();
        }
        b_AllTargetsWereDown = false; // 상태 강제 갱신
        OnTargetHitCountChanged.Invoke(0);
    }

    // 외부에서 모든 타겟을 비활성화시킬 때 사용
    [ContextMenu("모든 타겟 비활성화")]
    public void DesactivateAllTargets()
    {
        // 강제 비활성 직전 상태 확인
        bool wasPreviouslyDown = AreAllTargetsDesactivated();

        foreach (YJ_Script_DropTargetController target in targets)
        {
            target.Desactivate_Object();
        }

        // 강제 비활성화 시에도 이벤트 호출
        if (!wasPreviouslyDown)
        {
            Debug.Log("타겟 (강제) 비활성화! OnAllTargetsDeactivated 이벤트를 호출합니다.");
            OnAllTargetsDeactivated.Invoke();
        }
        b_AllTargetsWereDown = true; // 상태 강제 갱신

        if (targets != null)
        {
            OnTargetHitCountChanged.Invoke(targets.Length);
        }
    }
}