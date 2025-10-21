using UnityEngine;

public class YJ_LightSwitchController : MonoBehaviour
{
    public enum SwitchMode // 라이트 스위치 모드
    {
        Once,   // 한 번만 켜짐
        Toggle  // 켜졌다 꺼졌다 반복
    }

    [Header("동작 설정")]
    [SerializeField] private SwitchMode mode = SwitchMode.Once; // 기본값: 'Once'
                                                                // 충돌 판정이 필요한 오브젝트면 'Toggle'로 둘 것

    [Header("설정")]
    [SerializeField] private Material offMaterial; // 불이 꺼졌을 때 머티리얼
    [SerializeField] private Material onMaterial;  // 불이 켜졌을 때 머티리얼
    [SerializeField] private GameObject lightVisualObject; // 머티리얼을 바꿀 대상 (없으면 이 오브젝트)

    public bool isActivated = false;   // 현재 불이 켜져있는지 상태
    private Renderer targetRenderer;   // 머티리얼을 교체할 대상의 렌더러

    private void Start()
    {
        // 렌더러 찾기
        targetRenderer = lightVisualObject ? lightVisualObject.GetComponent<Renderer>() : GetComponent<Renderer>();

        // 시작 시 무조건 '꺼짐' 상태로 초기화
        Deactivate();
    }

    // 트리거 오브젝트용 작동 루틴
    private void OnTriggerEnter(Collider other)
    {
        // 닿은 오브젝트 태그가 공이면 실행
        if (other.CompareTag("Ball"))
        {
            ProcessHit();
        }
    }

    // 충돌 오브젝트용 작동 루틴
    private void OnCollisionEnter(Collision collision)
    {
        // 닿은 오브젝트 태그가 공이면 실행
        if (collision.gameObject.CompareTag("Ball"))
        {
            ProcessHit();
        }
    }

    // 스위치를 '처리'하는 공통 로직
    private void ProcessHit()
    {
        // 모드에 따라 다르게 동작
        if (mode == SwitchMode.Once)
        {
            // '한 번만' 모드일 때
            if (!isActivated) // 아직 켜지지 않았을 때만
            {
                Activate(); // 켠다
            }
        }
        else if (mode == SwitchMode.Toggle)
        {
            // '토글' 모드일 때
            if (isActivated) // 켜져있으면
            {
                Deactivate(); // 끈다
            }
            else // 꺼져있으면
            {
                Activate(); // 켠다
            }
        }
    }

    // 켜는 동작
    private void Activate()
    {
        isActivated = true;
        if (targetRenderer != null && onMaterial != null)
        {
            targetRenderer.material = onMaterial;
        }
        Debug.Log("스위치 ON");
    }

    // 끄는 동작
    private void Deactivate()
    {
        isActivated = false;
        if (targetRenderer != null && offMaterial != null)
        {
            targetRenderer.material = offMaterial;
        }
        Debug.Log("스위치 OFF");
    }

    // 게임 매니저 등이 호출할 수 있는 공용 리셋 함수
    public void ResetSwitch()
    {
        Deactivate();
    }
}