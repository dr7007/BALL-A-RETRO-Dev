using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GlitchEffect_Pass : ScriptableRenderPass
{
    private static readonly string ProfilerTag = "Glitch Effect Pass";
    private Material glitchMaterial;
    private RTHandle _tempRTHandle; // 임시 텍스처 핸들

    public GlitchEffect_Pass(Material material)
    {
        glitchMaterial = material;
        //'하늘 렌더링 직후'가 아니라, '투명 렌더링 직후'로 변경합니다.
      renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // 렌더링 시작 전 임시 텍스처 설정
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;

        // RTHandles.Alloc을 사용하여 임시 텍스처 재사용 및 관리
        RenderingUtils.ReAllocateIfNeeded(ref _tempRTHandle, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_GlitchTempTexture");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // 씬 뷰와 게임 뷰 모두 허용
        var cameraType = renderingData.cameraData.cameraType;
        if (cameraType != CameraType.Game)
        {
            return; // 게임 카메라가 아니면 여기서 즉시 종료
        }

        CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);

        // [핵심] "신형" RTHandle을 가져옵니다.
        RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

        // Blitter API를 사용하여 Blit 수행 (URP 최신 표준)
        // 이 API는 CS0619 오류를 발생시키지 않으며 RTHandle을 올바르게 처리합니다.
        
        // 1. 소스 -> 임시 텍스처 (글리치 적용)
        Blitter.BlitCameraTexture(cmd, source, _tempRTHandle, glitchMaterial, 0);
        
        // 2. 임시 텍스처 -> 소스 (원본에 덮어쓰기)
        Blitter.BlitCameraTexture(cmd, _tempRTHandle, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    // 렌더링 종료 후 정리
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // RTHandle은 자동으로 관리되므로 별도 해제 필요 없음 (RenderingUtils.ReAllocateIfNeeded 사용 시)
    }
    
    // 객체 파괴 시 텍스처 해제
    public void Dispose()
    {
        _tempRTHandle?.Release();
    }
}