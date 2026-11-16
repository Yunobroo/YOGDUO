#ifndef GOBLIN_PEEPHOLE_INCLUDED
#define GOBLIN_PEEPHOLE_INCLUDED

// Peephole design: cuts a clean round hole in a wall around the goblin's on-screen position,
// with a smooth glow around its rim. Globals are set by CameraOcclusion; _PeepOpen (0-1) is
// set on each wall's private material.
float4 _PeepholeTarget;   // xyz: goblin chest position in world space
float4 _PeepholeShape;    // x: radius (fraction of screen height), y: glow width (fraction of radius), z: glow strength, w: sill height (m)
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

    // Keep a low sill of wall uncut. There is usually no floor under a wall, so cutting its
    // base showed black empty space at the goblin's feet. Feet = chest point - 0.65.
    float2 screenUV = positionCS.xy * rcp(GetScaledScreenParams().xy);
    float3 positionWS = ComputeWorldSpacePosition(screenUV, positionCS.z, UNITY_MATRIX_I_VP);
    if (positionWS.y < _PeepholeTarget.y - 0.65 + _PeepholeShape.w) return 0;

    // Project the goblin the same way GetNormalizedScreenSpaceUV maps this pixel.
    float4 targetCS = TransformWorldToHClip(_PeepholeTarget.xyz);
    float4 scaledCS = targetCS * 0.5;
    float2 targetUV = (float2(scaledCS.x, scaledCS.y * _ProjectionParams.x) + scaledCS.w) / targetCS.w;
    float2 uv = GetNormalizedScreenSpaceUV(positionCS);

    float2 delta = (uv - targetUV) * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
    float dist = length(delta);
    float radius = max(_PeepholeShape.x * _PeepOpen, 1e-4);

    // A clean, round hole: no dithering, so the edge has no grain.
    clip(dist - radius);

    // A smooth torch glow on the wall just outside the edge; softness sets how far it spreads.
    float band = max(radius * clamp(_PeepholeShape.y, 0.01, 1.0), 1e-4);
    half glow = saturate(1.0 - (dist - radius) / band);
    return glow * glow * _PeepholeShape.z * _PeepOpen;
}

#endif
