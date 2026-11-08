Shader "BALL-A-RETRO/Original Pixel Trail"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Fragment(Varyings input) : SV_Target
            {
                // A texture-free ribbon; the TrailRenderer retains its own lifetime and color fade.
                half band = saturate((.5 - abs(input.uv.y - .5)) * 4);
                band = floor(band * 4) / 4;
                return half4(input.color.rgb, input.color.a * band);
            }
            ENDHLSL
        }
    }
}
