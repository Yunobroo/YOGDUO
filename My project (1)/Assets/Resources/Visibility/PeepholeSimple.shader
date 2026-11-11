// Peephole design, fallback for walls whose material isn't URP Lit: simple lit and opaque,
// with the hole cut by Peephole.hlsl. Lit walls use Goblin/PeepholeLit so they look unchanged.
Shader "Goblin/PeepholeSimple"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        [HideInInspector] _PeepOpen ("Peephole Open", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Assets/Resources/Visibility/Peephole.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite On Cull Back
            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
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
                half rim = PeepholeClip(v.positionCS);
                Light light = GetMainLight();
                half3 lighting = SampleSH(normalize(v.normalWS)) + light.color * saturate(dot(normalize(v.normalWS), light.direction));
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, v.uv) * _BaseColor;
                return half4(color.rgb * max(lighting, .25h) + _PeepholeRimColor.rgb * rim, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull Back
            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
            struct Input { float4 positionOS : POSITION; };
            struct Output { float4 positionCS : SV_POSITION; };
            Output Vert(Input v) { Output o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half Frag(Output v) : SV_Target { PeepholeClip(v.positionCS); return v.positionCS.z; }
            ENDHLSL
        }
    }
}
