using UnityEngine;
using System.Collections; // 코루틴(Coroutine)을 사용하기 위해 추가

/// <summary>
/// "Ball" 태그를 가진 오브젝트가 트리거에 들어오면
/// 지정된 시간(1초) 동안 머테리얼을 변경했다가 원래대로 복구합니다.
/// </summary>
public class PSH_Script_WormholeMaterialChanger : MonoBehaviour
{
    [Header("적용 대상 본체")]
    [Tooltip("머테리얼을 변경할 '본체'의 메쉬 렌더러")]
    [SerializeField]
    private MeshRenderer targetRenderer;

    [Header("머테리얼")]
    [Tooltip("공이 부딪혔을 때(트리거 진입 시) 적용할 머테리얼")]
    [SerializeField]
    private Material hitMaterial;

    [Header("지속 시간")]
    [Tooltip("머테리얼이 변경된 상태로 유지될 시간 (초)")]
    [SerializeField]
    private float changeDuration = 1.0f; // 1초로 기본값 설정

    // '본체'의 원래 머테리얼을 저장할 변수
    private Material originalMaterial;

    // 현재 실행 중인 머테리얼 복구 코루틴을 저장할 변수
    private Coroutine materialRevertCoroutine;


    private void Start()
    {
        // 1. 방어 코드: 필수 참조가 할당되었는지 확인
        if (targetRenderer == null)
        {
            Debug.LogError("[PSH_Script] 'targetRenderer'가 할당되지 않았습니다! " +
                             "머테리얼 변경 스크립트가 작동하지 않습니다.", this.gameObject);
            this.enabled = false; // 스크립트 비활성화
            return;
        }

        if (hitMaterial == null)
        {
            Debug.LogError("[PSH_Script] 'hitMaterial'이 할당되지 않았습니다! " +
                             "머테리얼 변경 스크립트가 작동하지 않습니다.", this.gameObject);
            this.enabled = false;
            return;
        }

        // 2. 시작할 때 'targetRenderer'의 '현재' 머테리얼을 '원본'으로 캐싱(저장)
        originalMaterial = targetRenderer.material;
    }

    /// <summary>
    /// 트리거 영역에 다른 Collider가 들어왔을 때 호출
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 들어온 것이 "Ball" 태그인지 확인
        if (other.CompareTag("Ball"))
        {
            // 3. 만약 이전에 실행 중이던 복구 코루틴이 있다면 (1초가 다 지나기 전에 공이 또 들어온 경우)
            //    일단 이전 코루틴을 중지시킵니다.
            if (materialRevertCoroutine != null)
            {
                StopCoroutine(materialRevertCoroutine);
            }

            // 4. 머테리얼을 변경하고 1초 뒤에 복구하는 새 코루틴을 시작하고,
            //    그 참조를 변수에 저장합니다.
            materialRevertCoroutine = StartCoroutine(ChangeMaterialForDuration());
        }
    }

    /// <summary>
    /// 머테리얼을 변경하고, 'changeDuration'초 후에 원본으로 복구하는 코루틴
    /// </summary>
    private IEnumerator ChangeMaterialForDuration()
    {
        // 1. 'hitMaterial'로 즉시 변경
        targetRenderer.material = hitMaterial;

        // 2. 설정된 'changeDuration' (1초) 만큼 대기
        yield return new WaitForSeconds(changeDuration);

        // 3. 'originalMaterial'(원본)로 복구
        targetRenderer.material = originalMaterial;

        // 4. 코루틴이 완료되었으므로, 참조 변수를 비워줍니다.
        materialRevertCoroutine = null;
    }

    // 5. OnTriggerExit는 1초 뒤 자동 복구되므로 더 이상 필요 없음
}