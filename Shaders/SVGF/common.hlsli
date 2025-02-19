#ifndef SVGF_COMMON
#define SVGF_COMMON

float computeWeight(
    float depthCenter,
    float depthP,
    float phiDepth,
    float3 normalCenter,
    float3 normalP,
    float phiNormal,
    float luminanceIllumCenter,
    float luminanceIllumP,
    float phiIllum
)
{
    const float weightNormal = pow(saturate(dot(normalCenter, normalP)), phiNormal);
    const float weightZ = (phiDepth == 0) ? 0.0f : abs(depthCenter - depthP) / phiDepth;
    const float weightLillum = abs(luminanceIllumCenter - luminanceIllumP) / phiIllum;

    const float weightIllum = exp(0.0 - max(weightLillum, 0.0) - max(weightZ, 0.0)) * weightNormal;

    return weightIllum;
}

#endif
