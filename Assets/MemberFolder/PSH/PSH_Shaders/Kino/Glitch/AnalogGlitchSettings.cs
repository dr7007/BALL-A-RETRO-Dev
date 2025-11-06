using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class AnalogGlitchSettings : ScriptableRendererFeature
{
    // --- 1. 인스펙터에서 조절할 글리치 설정 ---
    [System.Serializable]
    public class GlitchSettings
    {
        public Shader shader;
        [Range(0, 1)] public float scanLineJitter = 0.0f;
        [Range(0, 1)] public float verticalJump = 0.0f;
        [Range(0, 1)] public float horizontalShake = 0.0f;
        [Range(0, 1)] public float colorDrift = 0.0f;
    }

    public GlitchSettings settings = new GlitchSettings();
    private Material glitchMaterial;

    // --- 2. 렌더 패스 클래스 ---
    class CustomRenderPass : ScriptableRenderPass
    {
        // 셰이더 프로퍼티 ID (미리 캐싱)
        private static readonly int ScanLineJitterID = Shader.PropertyToID("_ScanLineJitter");
        private static readonly int VerticalJumpID = Shader.PropertyToID("_VerticalJump");
        private static readonly int HorizontalShakeID = Shader.PropertyToID("_HorizontalShake");
        private static readonly int ColorDriftID = Shader.PropertyToID("_ColorDrift");

        private GlitchSettings currentSettings;
        private Material currentMaterial;
        private float verticalJumpTime;

        public CustomRenderPass(Material material)
        {
            this.currentMaterial = material;
        }

        public void Setup(GlitchSettings newSettings)
        {
            this.currentSettings = newSettings;
        }

        // PassData: 렌더링에 필요한 데이터를 담습니다.
        private class PassData
        {
            public Material glitchMaterial;
            public TextureHandle sourceHandle;
            public Vector4 scaleBias; // Blitter.BlitTexture에 필요
        }

        // ExecutePass: 실제 Blit 로직
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Blitter.BlitTexture(cmd, source, scaleBias, material, passIndex)
            Blitter.BlitTexture(
                context.cmd,            // RasterCommandBuffer
                data.sourceHandle,      // TextureHandle (Source)
                data.scaleBias,         // new Vector4(1, 1, 0, 0)
                data.glitchMaterial,    // Material
                0                       // passIndex
            );
        }

        // RecordRenderGraph: RenderGraph에 패스를 등록합니다.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (currentMaterial == null || currentSettings == null)
                return;

            const string passName = "Analog Glitch Pass";

            // --- 셰이더 값 계산 (매 프레임) ---
            verticalJumpTime += Time.deltaTime * currentSettings.verticalJump * 11.3f;
            var sl_thresh = Mathf.Clamp01(1.0f - currentSettings.scanLineJitter * 1.2f);
            var sl_disp = 0.002f + Mathf.Pow(currentSettings.scanLineJitter, 3) * 0.05f;
            currentMaterial.SetVector(ScanLineJitterID, new Vector2(sl_disp, sl_thresh));

            var vj = new Vector2(currentSettings.verticalJump, verticalJumpTime);
            currentMaterial.SetVector(VerticalJumpID, vj);

            currentMaterial.SetFloat(HorizontalShakeID, currentSettings.horizontalShake * 0.2f);

            var cd = new Vector2(currentSettings.colorDrift * 0.04f, Time.time * 606.11f);
            currentMaterial.SetVector(ColorDriftID, cd);

            // --- 래스터 패스 추가 ---
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // PassData에 실행에 필요한 데이터들을 채워 넣습니다.
                passData.glitchMaterial = this.currentMaterial;
                passData.sourceHandle = resourceData.activeColorTexture;
                passData.scaleBias = new Vector4(1, 1, 0, 0);

                // --- 💡 [수정] 런타임 오류 해결 ---
                // "나는 이 텍스처를 읽기도 하고(Read) 쓰기도(Write) 할 것이다"
                builder.UseTexture(passData.sourceHandle, AccessFlags.ReadWrite);

                // 'activeColorTexture'를 '출력(Write)'으로 설정합니다 (Render Target)
                builder.SetRenderAttachment(passData.sourceHandle, 0);

                // ExecutePass 함수를 실행하도록 설정합니다.
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

        // --- 호환성 모드(Execute) 함수들은 비워둡니다 ---
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }
        public override void OnCameraCleanup(CommandBuffer cmd) { }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        if (settings.shader == null)
        {
            Debug.LogError("Analog Glitch Shader가 할당되지 않았습니다. URP 렌더러 기능에서 셰이더를 지정해주세요.");
            return;
        }

        glitchMaterial = CoreUtils.CreateEngineMaterial(settings.shader);
        m_ScriptablePass = new CustomRenderPass(glitchMaterial);
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(glitchMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (glitchMaterial == null)
            return;

        m_ScriptablePass.Setup(settings);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}