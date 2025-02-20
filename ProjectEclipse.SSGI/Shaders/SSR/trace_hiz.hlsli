#ifndef LIB_SSR_TRACE_HIZ
#define LIB_SSR_TRACE_HIZ

#include "common.hlsli"

void InitialAdvanceRay(float3 rayOrigin, inout float3 rayPos, float3 rayDir, float3 invRayDir, inout float current_t, float2 currentMipRes, float2 floorOffset, float2 uvOffset)
{
    float2 currentMipPos = currentMipRes * rayOrigin.xy;
    float2 xyPlane = floor(currentMipPos) + floorOffset;
    xyPlane = xyPlane * (1.0 / currentMipRes) + uvOffset;
    
    float2 t = (xyPlane - rayOrigin.xy) * invRayDir.xy;
    current_t = min(t.x, t.y);
    rayPos = rayOrigin + current_t * rayDir;
}

bool AdvanceRay(float3 rayOrigin, inout float3 rayPos, float3 rayDir, float3 invRayDir, inout float current_t, float2 currentMipPos, float2 currentMipRes, float currentCellDepth, float2 floorOffset, float2 uvOffset)
{
    float2 xyPlane = floor(currentMipPos) + floorOffset;
    xyPlane = xyPlane * rcp(currentMipRes) + uvOffset;
    float3 boundaryPlanes = float3(xyPlane, currentCellDepth);
    
    float3 t = (boundaryPlanes - rayOrigin) * invRayDir;
    
#if INVERTED_DEPTH
    t.z = rayDir.z < 0 ? t.z : FLOAT_MAX;
#else
    t.z = rayDir.z > 0 ? t.z : FLOAT_MAX;
#endif
    
    float tMin = min(min(t.x, t.y), t.z);
    
#if INVERTED_DEPTH
    bool aboveSurface = currentCellDepth < rayPos.z;
#else
    bool aboveSurface = currentCellDepth > rayPos.z;
#endif
    
    bool skippedTile = asuint(tMin) != asuint(t.z) && aboveSurface;
    
    current_t = aboveSurface ? tMin : current_t;
    rayPos = rayOrigin + current_t * rayDir;
    
    return skippedTile;
}

float GetHitConfidence(float3 hitTexPos, SSRInput input, float3 rayDirView, const float maxThickness)
{
    if (!all(saturate(hitTexPos.xy) == hitTexPos.xy))
    {
        return 0;
    }
    
    const float hitDepthRaw = HZB.SampleLevel(PointSampler, hitTexPos.xy, 0);
    const bool isForeground = hitDepthRaw != (INVERTED_DEPTH ? 0 : 1);
    //if (!isForeground)
    //{
    //    //return 0; // is ok since the skybox will be sampled
    //}
    
    const float3 hitNormal = LoadNormalInViewSpace(hitTexPos.xy);
    if (isForeground && dot(hitNormal, -rayDirView) < 0)
    {
        return 0;
    }
    
    const float3 vsSurface = TexToView(hitTexPos.xy, hitDepthRaw);
    const float3 vsHit = TexToView(hitTexPos.xy, hitTexPos.z);
    const float distance = length(vsSurface - vsHit);
    
    float2 fov = 0.05 * float2(ScreenSize.y / ScreenSize.x, 1);
    float2 border = smoothstep(0, fov, hitTexPos.xy) * (1 - smoothstep(1 - fov, 1, hitTexPos.xy));
    float vignette = border.x * border.y;
    
    float confidence = 1.0 - smoothstep(0, maxThickness, distance);
    confidence *= confidence;
    
    return vignette * confidence;
}

float TraceRayHiZ(float2 uvStart, SSRInput input, float3 rayDirView, out float2 uvHit)
{
    static const int maxMip = 8;
    static const int startMip = 0;
    static const int stopMip = 0;
    //static const uint maxIterations = 80;
    const uint maxIterations = MaxTraceIterations;
    static const float maxThickness = 0.05;
    
    const uint2 frameSize = GetScreenSize();
    const float nearPlane = ProjMatrix._43;
    
    const float rayLength = (input.PositionView.z + rayDirView.z) > nearPlane ? (nearPlane - -input.PositionView.z) / -rayDirView.z : 1;
    const float3 rayStartView = input.PositionView /*+ input.NormalView * 0.01*/;
    const float3 rayEndView = rayStartView + rayDirView * rayLength;
    
    const float4 rayStartClip = mul(float4(rayStartView, 1), ProjMatrix); // h0
    const float4 rayEndClip = mul(float4(rayEndView, 1), ProjMatrix); // h1
    
    const float k0 = 1.0 / rayStartClip.w; // perspective divisor's reciprocal (1 / zDepth)
    const float k1 = 1.0 / rayEndClip.w;
    
    const float z0 = rayStartClip.z * k0;
    const float z1 = rayEndClip.z * k1;
    
    const float2 p0 = ClipToTex(rayStartClip.xy * k0);
    const float2 p1 = ClipToTex(rayEndClip.xy * k1);
    
    float3 rayOrigin = float3(p0, z0);
    float3 rayPos = rayOrigin;
    float3 rayDir = normalize(float3(p1 - p0, z1 - z0));
    float3 invRayDir = 1.0 / rayDir;
    
    float ray_t = 0;
    
    const float2 uvOffset = (0.005 * exp2(0) / frameSize) * (rayDir.xy < 0 ? -1 : 1);
    const float2 floorOffset = rayDir.xy < 0 ? 0 : 1;
    
    InitialAdvanceRay(rayOrigin, rayPos, rayDir, invRayDir, ray_t, ComputeMipSize(frameSize, startMip), floorOffset, uvOffset);
    
    int mip = startMip;
    for (uint i = 0; i < maxIterations && mip >= stopMip && all(saturate(rayPos.xy) == rayPos.xy); i++)
    {
        const uint2 mipCellCount = ComputeMipSize(frameSize, mip);
        const float2 currentCell = rayPos.xy * mipCellCount;
        const float maxCellDepthRaw = HZB.Load(int3(currentCell, mip));
        
        bool skippedTile = AdvanceRay(rayOrigin, rayPos, rayDir, invRayDir, ray_t, currentCell, mipCellCount, maxCellDepthRaw, floorOffset, uvOffset);
        
        bool nextMipIsOutOfRange = skippedTile && (mip >= maxMip);
        if (!nextMipIsOutOfRange)
        {
            mip += skippedTile ? 1 : -1;
        }
    }
    
    uvHit = rayPos.xy;
    
    bool validHit = (mip < stopMip);
    return validHit ? GetHitConfidence(rayPos, input, rayDirView, maxThickness) : 0;
}

#endif
