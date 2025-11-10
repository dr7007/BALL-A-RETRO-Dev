using UnityEngine;
using UnityEngine.Rendering.Universal;

// 이 클래스는 Unity Editor에서 Feature 에셋으로 나타나게 합니다.
public class GlitchEffect_RendererFeature : ScriptableRendererFeature
{
    // Inspector에서 Material을 할당할 수 있도록 필드를 만듭니다.
    [SerializeField] 
    private Material glitchMaterial;

    // 실제 렌더링 패스를 인스턴스화할 변수
    private GlitchEffect_Pass glitchPass;

    // Feature가 활성화될 때 한 번 호출됩니다. (초기화)
    public static bool IsEnabled = false;


    public override void Create()
    {
        if (glitchMaterial == null)
        {
            Debug.LogError("GlitchEffect_RendererFeature: Glitch Material is not assigned!");
            return;
        }
        glitchPass = new GlitchEffect_Pass(glitchMaterial);
    }
    // 렌더러가 활성화될 때마다 호출됩니다. 렌더 패스를 큐에 추가합니다.        
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // [!!!!!!!!! 2. 스위치 확인 로직 추가 !!!!!!!!!]
        // 스위치가 꺼져있으면(false) 렌더 패스를 아예 추가하지 않음
        if (!IsEnabled)
        {
            return;
        }

        if (glitchMaterial == null)
        {
            return;
        }
        renderer.EnqueuePass(glitchPass);
    }
    // Feature가 파괴될 때 호출됩니다. (리소스 정리)
    protected override void Dispose(bool disposing)
    {
        // Material은 다른 곳에서 관리될 수 있으므로 여기서 파괴하지 않습니다.
        // 만약 Feature 내에서 Material을 직접 생성했다면 여기서 파괴해야 합니다.
    }
}