// Silhouette design: drawn only where a character is hidden behind something (ZTest Greater).
// Vertices are pulled toward the camera by _DepthBias so a character's own arms and
// clothes never trigger it - only real cover in front of them does.
Shader "Goblin/XRay"
{
    Properties
    {
        _Color ("Color", Color) = (0.45, 1, 0.4, 0.55)
        _DepthBias ("Depth Bias", Float) = 0.35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+50" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest Greater Cull Back
            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _DepthBias;
            CBUFFER_END
            struct Input { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Output { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };
            Output Vert(Input v)
            {
                Output o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 positionVS = TransformWorldToView(positionWS);
                positionVS.z += _DepthBias; // view space looks down -z, so this moves toward the camera
                o.positionCS = TransformWViewToHClip(positionVS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceNormalizeViewDir(positionWS);
                return o;
            }
            half4 Frag(Output v) : SV_Target
            {
                // Brighter at the edges so the shape reads as a glowing outline with a faint fill.
                half rim = 1.0h - saturate(dot(normalize(v.normalWS), normalize(v.viewWS)));
                half strength = 0.3h + 0.7h * rim * rim;
                return half4(_Color.rgb * (0.6h + 0.8h * rim), _Color.a * strength);
            }
            ENDHLSL
        }
    }
}
