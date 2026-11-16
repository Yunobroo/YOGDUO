Shader "Goblin/OccluderFade"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 0.2
        _Tint ("Faded Tint", Color) = (0.62, 0.53, 0.43, 1)
        _TintStrength ("Tint Strength", Range(0,1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Opacity;
            half4 _Tint;
            half _TintStrength;
            CBUFFER_END
            struct Input { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Output { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };
            Output Vert(Input v)
            {
                Output o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half4 Frag(Output v) : SV_Target
            {
                Light light = GetMainLight();
                half3 lighting = SampleSH(normalize(v.normalWS)) + light.color * saturate(dot(normalize(v.normalWS), light.direction));
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, v.uv) * _BaseColor;
                half3 lit = color.rgb * max(lighting, .25h);
                // Wash the faded wall toward dusty stone so it reads as grey-brown, not blue-tinted glass.
                half luma = dot(lit, half3(0.299h, 0.587h, 0.114h));
                half3 stone = _Tint.rgb * (0.35h + luma);
                return half4(lerp(lit, stone, _TintStrength), color.a * _Opacity);
            }
            ENDHLSL
        }
    }
}
