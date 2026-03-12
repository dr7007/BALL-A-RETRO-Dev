Shader "Custom/BlurWithImageOverlay"
{
    Properties
    {
        [Header(User Image)]
        [MainTexture] _MainTex ("Overlay Image", 2D) = "white" {} // 사용자 이미지
        _BaseColor ("Image Color & Alpha", Color) = (1,1,1,1) // 이미지 투명도 조절

        [Header(Blur Settings)]
        _Blur ("Blur Strength", Integer) = 1
        _Scale ("Blur Scale", Range(1, 5)) = 1
    }

    SubShader
    {
        // 배경이 비쳐야 하므로 Transparent 큐 사용
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            // 투명도 혼합을 위한 블렌드 모드 설정
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0; // 이미지용 UV
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos : TEXCOORD0; // 블러용 화면 좌표
                float2 uv : TEXCOORD1;        // 이미지용 UV
            };

            // 뒤에 있는 화면(Opaque)을 가져오는 텍스처
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            // 사용자가 넣을 이미지
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                int _Blur;
                float _Scale;
                float4 _MainTex_ST;
                float4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                
                // 1. 블러를 위한 화면 좌표 계산
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                
                // 2. 이미지를 위한 UV 좌표 계산
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // --- 1. 배경 블러 처리 (Blurring Background) ---
                float4 blurredBG = 0.0;
                half2 pos = IN.screenPos.xy / IN.screenPos.w;
                half2 texel = _Scale * (1.0 / _ScreenParams.xy); // 화면 해상도 기준 픽셀 크기

                int blur_size = _Blur > 0 ? _Blur : 1;

                for (int i = -blur_size; i <= blur_size; i++) {
                    for (int j = -blur_size; j <= blur_size; j++) {
                        blurredBG += SAMPLE_TEXTURE2D(
                          _CameraOpaqueTexture, 
                          sampler_CameraOpaqueTexture, 
                          pos + (half2(i, j) * texel));
                    }  
                }
                blurredBG = blurredBG / ((2 * blur_size + 1) * (2 * blur_size + 1));

                // --- 2. 사용자 이미지 샘플링 (Overlay Image) ---
                float4 userImage = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;

                // --- 3. 합성 (Composition) ---
                // 사용자 이미지의 알파(투명도)를 기준으로 배경과 합성합니다.
                // 이미지가 투명한 부분은 블러된 배경이 보이고, 불투명한 부분은 이미지가 보입니다.
                float3 finalRGB = lerp(blurredBG.rgb, userImage.rgb, userImage.a);
                
                // 전체 투명도는 1(불투명)로 설정하여 뒤가 완전히 뚫리지 않고 '블러된 유리'처럼 보이게 함
                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }
}