// Soft glowing disc drawn on a quad, added on top of whatever is behind it.
// Used for the title screen's out-of-focus background lights and the goblin's glowing eyes.
Shader "Goblin/Bokeh"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.6, 0.3, 1)
        _Softness ("Edge Softness", Range(0.01, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _Softness;
            CBUFFER_END
            struct Input { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Output Vert(Input v)
            {
                Output o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv * 2.0 - 1.0;
                return o;
            }
            half4 Frag(Output v) : SV_Target
            {
                // 1 inside the disc, fading to 0 at its edge; a brighter rim like real lens bokeh.
                half r = length(v.uv);
                half disc = saturate((1.0h - r) / _Softness);
                half rim = smoothstep(0.55h, 0.9h, r) * disc * 0.35h;
                return half4(_Color.rgb * (disc * 0.8h + rim) * _Color.a, 1);
            }
            ENDHLSL
        }
    }
}
