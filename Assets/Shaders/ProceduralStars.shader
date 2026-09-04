Shader "Custom/ProceduralStars"
{
    Properties
    {
        _MainColor ("Star Color", Color) = (1,1,1,1)
        _Intensity ("Base Intensity", Range(0,1)) = 1
        _StarDensity ("Star Density", Range(10, 500)) = 100
        _StarSize ("Star Size", Range(0.01, 0.5)) = 0.05
        _HorizonFade ("Horizon Fade", Range(0.0, 1.0)) = 0.1
        
        [Header(Twinkle Settings)]
        _MinTwinkleSpeed ("Min Twinkle Speed", Range(0, 5)) = 0.5
        _MaxTwinkleSpeed ("Max Twinkle Speed", Range(0, 5)) = 2.0
        _TwinkleAmount ("Twinkle Intensity", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { 
            "Queue"="Transparent+100" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
        }

        ZWrite Off
        ZTest LEqual 
        Cull Front
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _MainColor;
            float _Intensity;
            float _StarDensity;
            float _StarSize;
            float _HorizonFade;
            float _MinTwinkleSpeed;
            float _MaxTwinkleSpeed;
            float _TwinkleAmount;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float hash31(float3 p3) {
                p3  = frac(p3 * 0.1031);
                p3 += dot(p3, p3.zyx + 31.32);
                return frac((p3.x + p3.y) * p3.z);
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos);
                
                float horizon = smoothstep(-0.1, _HorizonFade, viewDir.y);
                if(horizon <= 0) return fixed4(0,0,0,1);

                float3 ray = viewDir * _StarDensity;
                float3 id = floor(ray);
                float3 f = frac(ray) - 0.5;

                float n = hash31(id); 
                float starMask = smoothstep(0.9, 1.0, n);
                
                float dist = length(f);
                float starShape = smoothstep(_StarSize, 0.0, dist);

                // --- NEW CONTROLLABLE TWINKLE SPEED ---
                // We pick a speed for THIS specific star between Min and Max
                float individualSpeed = lerp(_MinTwinkleSpeed, _MaxTwinkleSpeed, n);
                
                // We multiply Time by that speed, and keep the random offset (n * 6.28)
                // so they don't all start their blink at the same time.
                float wave = sin(_Time.y * individualSpeed + (n * 6.28));
                
                float twinkle = lerp(1.0, (wave * 0.5 + 0.5), _TwinkleAmount);

                float finalBrightness = starShape * starMask * twinkle * (_Intensity * 10.0) * horizon;
                
                return _MainColor * finalBrightness;
            }
            ENDCG
        }
    }
}