Shader "Custom/GlitchEffectURP"
{
    Properties
    {
        // _MainTex는 C# 코드에서 자동으로 바인딩되므로 Inspector에 노출하지 않습니다.
        // _MainTex ("Texture", 2D) = "white" {}
        _ShakePower ("Shake Power", Float) = 0.03
        _ShakeRate ("Shake Rate", Range(0.0, 1.0)) = 0.2
        _ShakeSpeed ("Shake Speed", Float) = 5.0
        _ShakeBlockSize ("Shake Block Size", Float) = 30.5
        _ShakeColorRate ("Shake Color Rate", Range(0.0, 1.0)) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off
        Cull Off
        Blend Off

        Pass
        {
            Name "GlitchPass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP의 핵심 라이브러리를 포함합니다.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // [핵심 수정]
            // _MainTex를 직접 선언하지 않고, URP의 내장 텍스처인 _BlitTexture를 사용합니다.
            // Blitter.BlitCameraTexture를 사용하면 소스 텍스처가 _BlitTexture에 바인딩됩니다.
            TEXTURE2D_X_FLOAT(_BlitTexture);
            SAMPLER(sampler_BlitTexture); // _BlitTexture에 맞는 샘플러


            CBUFFER_START(UnityPerMaterial)
            float _ShakePower;
            float _ShakeRate;
            float _ShakeSpeed;
            float _ShakeBlockSize;
            float _ShakeColorRate;
            CBUFFER_END

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0; // 풀스크린 삼각형의 UV
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                OUT.uv = GetFullScreenTriangleTexCoord(IN.vertexID);
                return OUT;
            }

            float random(float seed)
            {
                return frac(543.2543 * sin(dot(float2(seed, seed), float2(3525.46, -54.3415))));
            }

            half4 frag(Varyings i) : SV_Target
            {
                float enable_shift = (random(trunc(_Time.y * _ShakeSpeed)) < _ShakeRate) ? 1.0 : 0.0;

                float2 fixed_uv = i.uv;
                
                float y_block = trunc(i.uv.y * _ShakeBlockSize) / _ShakeBlockSize;
                float random_offset = random(y_block + _Time.y) - 0.5;
                
                fixed_uv.x += random_offset * _ShakePower * enable_shift;

                // [핵심 수정]
                // _MainTex 대신 _BlitTexture에서 샘플링합니다.
                half4 pixel_color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, fixed_uv);

                pixel_color.r = lerp(
                    pixel_color.r,
                    SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, fixed_uv + float2(_ShakeColorRate, 0.0)).r,
                    enable_shift
                );
                
                pixel_color.b = lerp(
                    pixel_color.b,
                    SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, fixed_uv + float2(-_ShakeColorRate, 0.0)).b,
                    enable_shift
                );

                return pixel_color;
            }
            ENDHLSL
        }
    }
}