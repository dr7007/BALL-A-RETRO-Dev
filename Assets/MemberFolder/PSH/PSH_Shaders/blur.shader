Shader "Custom/Blur"

{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {

        Tags{ "Queue" = "Transparent" }

        GrabPass
        {   
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 grabPos : TEXCOORD0;
                float4 pos : SV_POSITION;
                float4 vertColor : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.vertColor = v.color;
                return o;
            }

            sampler2D _GrabTexture;

            half4 frag(v2f i) : SV_Target
            {
                half4 col = tex2Dproj(_GrabTexture, i.grabPos);
                return col;
            }
            ENDCG
        }
    }
}