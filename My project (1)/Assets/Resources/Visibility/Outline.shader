Shader "Goblin/Outline"
{
    Properties { _Color ("Color", Color) = (1,0.25,0.15,1) _Width ("Width", Float) = 0.035 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        Pass
        {
            Cull Front ZWrite Off ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Width;
            CBUFFER_END
            struct Input { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            float4 Vert(Input v) : SV_POSITION
            {
                float3 position = TransformObjectToWorld(v.positionOS.xyz);
                position += TransformObjectToWorldNormal(v.normalOS) * _Width;
                return TransformWorldToHClip(position);
            }
            half4 Frag() : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
