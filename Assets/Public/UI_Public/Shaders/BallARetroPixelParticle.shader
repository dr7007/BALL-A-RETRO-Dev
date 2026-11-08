Shader "BALL-A-RETRO/Original Pixel Particle"
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
            float Hash(float2 samplePosition)
            {
                float3 value = frac(float3(samplePosition.xyx) * .1031);
                value += dot(value, value.yzx + 33.33);
                return frac((value.x + value.y) * value.z);
            }
            float Noise(float2 samplePosition)
            {
                float2 cell = floor(samplePosition);
                float2 blend = frac(samplePosition);
                blend = blend * blend * (3 - 2 * blend);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), blend.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), blend.x), blend.y);
            }
            half4 Fragment(Varyings input) : SV_Target
            {
                // Original analytic smoke density. No image, flipbook or downloaded noise map.
                // CPU motion expands and turns these soft strokes; the shader stays still when paused.
                float2 samplePosition = (input.uv - .5) * 2;
                float radiusSquared = dot(samplePosition, samplePosition);
                float2 variation = input.color.rg * float2(7.1, 11.3);
                float density = Noise(samplePosition * 4.6 + variation) * .6
                    + Noise(samplePosition * 10.1 - variation) * .4;
                float feather = 1 - smoothstep(.24, 1, radiusSquared);
                float core = exp2(-radiusSquared * 1.5);
                half opacity = feather * core * (.25 + density * .95);
                return half4(input.color.rgb * 2.1, input.color.a * opacity);
            }
            ENDHLSL
        }
    }
}
