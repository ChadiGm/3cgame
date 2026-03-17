Shader "Custom/ProceduralRock"
{
    Properties
    {
        _Color1 ("Crevice Color", Color) = (0.15, 0.15, 0.18, 1)
        _Color2 ("Surface Color", Color) = (0.45, 0.45, 0.50, 1)
        _Scale ("Noise Scale", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color1;
                float4 _Color2;
                float _Scale;
            CBUFFER_END

            // 2D Hash function
            float2 hash22(float2 p)
            {
                p = float2(dot(p,float2(127.1,311.7)), dot(p,float2(269.5,183.3)));
                return -1.0 + 2.0*frac(sin(p)*43758.5453123);
            }

            // Simplex noise in 2D
            float noise(float2 p)
            {
                const float K1 = 0.366025404; // (sqrt(3)-1)/2
                const float K2 = 0.211324865; // (3-sqrt(3))/6

                float2 i = floor(p + (p.x+p.y)*K1);
                float2 a = p - i + (i.x+i.y)*K2;
                float2 o = (a.x>a.y) ? float2(1.0,0.0) : float2(0.0,1.0);
                float2 b = a - o + K2;
                float2 c = a - 1.0 + 2.0*K2;

                float3 h = max(0.5-float3(dot(a,a), dot(b,b), dot(c,c) ), 0.0);
                float3 n = h*h*h*h*float3( dot(a,hash22(i+0.0)), dot(b,hash22(i+o)), dot(c,hash22(i+1.0)));

                return dot(n, float3(70.0, 70.0, 70.0));
            }

            Varyings vert(Attributes v)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                OUT.uv = v.uv;
                OUT.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Use world position so the noise is seamless across separate wall objects
                float2 pos = IN.positionWS.xy * _Scale;
                
                // Add fractal detail (FBM)
                float n = noise(pos) * 0.5 + 0.5;
                n += noise(pos * 2.1) * 0.25;
                n += noise(pos * 4.3) * 0.125;
                n += noise(pos * 8.4) * 0.0625;
                n = n / 1.9375; // Normalize sum of weights
                
                // Create a 'ridged' look typical for rocks by taking absolute value
                // and inverting
                n = 1.0 - abs(n - 0.5) * 2.0;
                
                // Power function to sharpen the ridges
                n = pow(n, 1.5);

                half4 finalColor = lerp(_Color1, _Color2, n);
                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
