Shader "Custom/URP_MetallicMatCap"
{
    Properties
    {
        [Header(Base Settings)]
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        
        [Header(MatCap Settings)]
        _MatCapTex ("MatCap Texture (RGB)", 2D) = "black" {}
        _MatCapIntensity ("MatCap Intensity", Range(0, 2)) = 1.0
        
        [Header(PBR Settings (Metal  Smoothness))]
        _Metallic ("Metallic", Range(0, 1)) = 1.0       // 쇠구슬 느낌을 위한 메탈릭
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8   // 표면의 매끄러움 (빛 맺힘 크기 조절)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // --------------------------------------------------
            // 시니어 팁: URP의 실시간 라이팅과 반사를 완벽 지원하기 위한 매크로들
            // --------------------------------------------------
            
            // 메인 라이트 및 그림자
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            
            // 추가 라이트 (Point, Spot Light 실시간 처리용)
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            
            // 리플렉션 프로브(볼륨 박스 반사) 및 환경광(SH) 지원
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ LIGHTMAP_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
            };

            TEXTURE2D(_MatCapTex);
            SAMPLER(sampler_MatCapTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _MatCapIntensity;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. 노멀 및 뷰 방향 정규화 (빛 계산의 기본)
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);

                // 2. MatCap UV 및 색상 계산
                float3 normalVS = mul((float3x3)UNITY_MATRIX_V, normalWS);
                float2 matCapUV = normalVS.xy * 0.5 + 0.5;
                half4 matCap = SAMPLE_TEXTURE2D(_MatCapTex, sampler_MatCapTex, matCapUV);

                // 3. URP PBR SurfaceData 설정
                // 시니어들은 라이팅을 직접 다 계산하지 않고, 이 구조체에 값을 채워 URP 엔진에 넘깁니다.
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = _BaseColor.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                
                // MatCap을 PBR 환경에서 자연스럽게 섞기 위해 Emission(발광) 영역에 은은하게 더해줍니다.
                // 메탈릭 값이 높을수록 MatCap 반사 느낌이 강해지도록 곱해줍니다.
                surfaceData.emission = matCap.rgb * _MatCapIntensity * surfaceData.metallic;
                surfaceData.alpha = 1.0;

                // 4. URP InputData 설정 (그림자, 반사, 환경광 정보를 담는 구조체)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                
                // 그림자 좌표 계산
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                // 주변 환경광(Ambient) 계산 - 이전의 고정값이 아닌 Unity의 SH(Spherical Harmonics) 스카이박스 조명을 가져옵니다.
                inputData.bakedGI = SampleSH(normalWS);

                // 5. 최종 라이팅 합성 (UniversalFragmentPBR)
                // 메인 라이트, 포인트 라이트, 스펙큘러, 리플렉션 프로브(볼륨 박스 반사), 그림자 처리가 이 함수 하나로 모두 완벽하게 계산됩니다.
                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                return finalColor;
            }
            ENDHLSL
        }
    }
    // 다른 오브젝트에 그림자를 캐스팅하기 위해 내장 Lit 셰이더의 그림자 패스를 차용합니다.
    Fallback "Universal Render Pipeline/Lit"
}