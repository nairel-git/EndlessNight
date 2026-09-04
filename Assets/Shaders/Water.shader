Shader "Custom/Water"
{
    Properties
    {
        [Header(Surface)]
        _ShallowColor ("Shallow Color", Color) = (0.3, 0.7, 1.0, 0.5)
        _DeepColor ("Deep Color", Color) = (0.0, 0.2, 0.5, 0.9)
        _Smoothness ("Smoothness", Range(0,1)) = 0.9

        [Header(Depth and Shoreline)]
        _DepthFactor ("Depth Factor", Float) = 1.0
        _DepthPower ("Depth Blend Power", Float) = 1.0

        [Header(Waves)]
        _WaveSpeed ("Wave Speed", Vector) = (0.1, 0.1, 0, 0)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Strength", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _NormalMap;
            sampler2D _CameraDepthTexture;

            float4 _ShallowColor;
            float4 _DeepColor;
            float _DepthFactor;
            float _DepthPower;
            float4 _WaveSpeed;
            float _NormalScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv + _WaveSpeed.xy * _Time.y;
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Screen UV
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                // Scene depth
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth);

                // Water surface depth
                float surfaceDepth = i.screenPos.w;

                float depthDiff = sceneDepth - surfaceDepth;
                float depthMask = saturate(depthDiff * _DepthFactor);
                depthMask = pow(depthMask, _DepthPower);

                // Color blend
                fixed4 col = lerp(_ShallowColor, _DeepColor, depthMask);
                col.a *= saturate(depthDiff);

                return col;
            }
            ENDCG
        }
    }
}
