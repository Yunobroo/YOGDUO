#ifndef GOBLIN_PEEPHOLE_INCLUDED
#define GOBLIN_PEEPHOLE_INCLUDED

// Peephole design: cuts a soft, dithered hole in a wall around the goblin's on-screen position.
// Globals are set by CameraOcclusion; _PeepOpen (0-1) is set on each wall's private material.
float4 _PeepholeTarget;   // xyz: goblin chest position in world space
float4 _PeepholeShape;    // x: radius (fraction of screen height), y: edge softness (0-1), z: rim glow strength
half4 _PeepholeRimColor;
float _PeepOpen;

float PeepholeEyeDepth(float deviceZ)
{
    if (unity_OrthoParams.w > 0.5)
    {
        #if UNITY_REVERSED_Z
            deviceZ = 1.0 - deviceZ;
        #endif
        return lerp(_ProjectionParams.y, _ProjectionParams.z, deviceZ);
    }
    return LinearEyeDepth(deviceZ, _ZBufferParams);
}

// Discards pixels inside the hole. Returns how strongly the pixel sits on the hole's rim (0-1).
half PeepholeClip(float4 positionCS)
{
    if (_PeepOpen <= 0.001) return 0;

    // Only wall in front of the goblin gets cut; anything behind it stays solid.
    float targetDepth = -TransformWorldToView(_PeepholeTarget.xyz).z;
    if (PeepholeEyeDepth(positionCS.z) > targetDepth - 0.3) return 0;

    // Project the goblin the same way GetNormalizedScreenSpaceUV maps this pixel.
    float4 targetCS = TransformWorldToHClip(_PeepholeTarget.xyz);
    float4 scaledCS = targetCS * 0.5;
    float2 targetUV = (float2(scaledCS.x, scaledCS.y * _ProjectionParams.x) + scaledCS.w) / targetCS.w;
    float2 uv = GetNormalizedScreenSpaceUV(positionCS);

    float2 delta = (uv - targetUV) * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
    float radius = max(_PeepholeShape.x * _PeepOpen, 1e-4);
    float softness = clamp(_PeepholeShape.y, 0.01, 1.0);
    // 0 in the middle of the hole, 1 at and beyond its edge.
    float mask = saturate((length(delta) - radius * (1.0 - softness)) / (radius * softness));

    // Interleaved-gradient dither turns the soft edge into a crumbly, stippled border.
    float dither = frac(52.9829189 * frac(dot(positionCS.xy, float2(0.06711056, 0.00583715))));
    clip(mask - dither);
    return (1.0 - mask) * _PeepholeShape.z * _PeepOpen;
}

#endif
