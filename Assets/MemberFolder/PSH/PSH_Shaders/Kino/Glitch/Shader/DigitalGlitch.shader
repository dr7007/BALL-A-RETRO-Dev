//
// KinoGlitch - Digital Glitch effect for URP (Path Independent Fix)
//
// Based on the original by Keijiro Takahashi (MIT License)
//
Shader "Kino/Glitch/DigitalURP"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _TrashTex ("Trash Frame Texture", 2D) = "black" {}
        _Intensity ("Glitch Intensity", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }
        LOD 100

        Pass
        {
            Name "GlitchPass"
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment frag
            #pragma target 4.5 

            // URP 헬퍼 함수 정의:
            // URP 경로 오류를 우회하기 위해 Fullscreen 헬퍼 함수를 셰이더 내에 직접 정의합니다.
            float2 FullScreenTriangleUV(uint vertexID)
            {
                // vertexID를 사용하여 전체 화면 UV (0,0), (0,2), (2,0)을 생성합니다.
                return float2((vertexID << 1) & 2, vertexID & 2);
            }

            float4 FullScreenTriangleVertexPosition(uint vertexID)
            {
                float2 uv = FullScreenTriangleUV(vertexID);
                // UV를 클립 공간 좌표 (-1, 1)로 변환합니다.
                return float4(uv * 2.0 - 1.0, 0.0, 1.0);
            }

            // URP 기본 라이브러리만 인클루드합니다. (경로가 비교적 안정적입니다.)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // URP Fullscreen Vertex Shader
            Varyings FullscreenVert(Attributes input)
            {
                Varyings output;
                // 이제 셰이더 내에 정의된 로컬 헬퍼 함수를 사용합니다.
                output.uv = FullScreenTriangleUV(input.vertexID);
                output.positionCS = FullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            // URP 텍스처 선언
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            TEXTURE2D(_TrashTex);
            SAMPLER(sampler_TrashTex);

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
            CBUFFER_END

            float4 frag(Varyings i) : SV_Target
            {
                // 프래그먼트 로직
                float4 glitch = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv);

                float thresh = 1.001 - _Intensity * 1.001;
                float w_d = step(thresh, pow(glitch.z, 2.5)); // displacement glitch
                float w_f = step(thresh, pow(glitch.w, 2.5)); // frame glitch
                float w_c = step(thresh, pow(glitch.z, 3.5)); // color glitch

                // Displacement.
                float2 uv = frac(i.uv + glitch.xy * w_d);
                float4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // Mix with trash frame.
                float3 color = lerp(source.rgb, SAMPLE_TEXTURE2D(_TrashTex, sampler_TrashTex, uv).rgb, w_f);

                // Shuffle color components.
                float3 neg = saturate(color.grb + (1 - dot(color, 1)) * 0.5);
                color = lerp(color, neg, w_c);

                return float4(color, source.a);
            }
            ENDHLSL
        }
    }
}