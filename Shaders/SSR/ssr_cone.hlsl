#include "common.hlsli"
#include "trace_hiz.hlsli"

float SpecularConeAngleRadians(float roughness)
{
    // TODO: use something resembling GGX
    const float blinnPhongSpecularPower = (2.0 / pow(roughness, 4.0)) - 2.0;
    const float xi = 0.244;
    const float exponent = 1.0 / (blinnPhongSpecularPower + 1.0);
    return acos(pow(xi, exponent));
}

float isoscelesTriangleOpposite(float adjacentLength, float coneTheta)
{
    // simple trig and algebra - soh, cah, toa - tan(theta) = opp/adj, opp = tan(theta) * adj, then multiply * 2.0f for isosceles triangle base
    return 2.0f * tan(coneTheta) * adjacentLength;
}

float isoscelesTriangleInRadius(float a, float h)
{
    float a2 = a * a;
    float fh2 = 4.0f * h * h;
    return (a * (sqrt(a2 + fh2) - a)) / (4.0f * h);
}

float4 coneSampleWeightedColor(float2 samplePos, float mipChannel, float gloss)
{
    float3 sampleColor = FrameBuffer.SampleLevel(LinearSampler, samplePos, mipChannel).rgb;
    return float4(sampleColor * gloss, gloss);
}

float isoscelesTriangleNextAdjacent(float adjacentLength, float incircleRadius)
{
    // subtract the diameter of the incircle to get the adjacent side of the next level on the cone
    return adjacentLength - (incircleRadius * 2.0f);
}

float3 main(const float4 position : SV_Position, const float2 uv : TEXCOORD0) : SV_Target
{
    //return FrameBuffer.SampleLevel(LinearSampler, uv, 4);
    
    const uint2 pixelPos = position.xy;
    const SSRInput input = LoadSSRInput(pixelPos);
    
    float3 pixelColor = FrameBuffer[position.xy];
    
    [branch]
    if (!IsDepthForeground(input.Depth) || input.Gloss < REFLECT_GLOSS_THRESHOLD)
    {
        return pixelColor;
    }
    
    float3 reflectDir = reflect(input.RayDirView, input.NormalView);
    
    float2 uvHit;
    float hitConfidence = TraceRayHiZ(uv, input, reflectDir, uvHit);
    
    if (hitConfidence == 0)
    {
        return pixelColor;
    }
    
    float hitDist = length(TexToView(uvHit, LoadDepth(uvHit)) - input.PositionView);
    float coneTheta = SpecularConeAngleRadians(1 - input.Gloss) * 0.5;
    
    float adjacentLength = length(uvHit - uv);
    float2 adjacentUnit = normalize(uvHit - uv);
    
    const float maxMipLevel = 9;
    
    float glossMulti = input.Gloss;
    float remainingAlpha = 1;
    float4 totalReflectedColor = 0;
    for (int i = 0; i < 2; i++)
    {
        float oppositeLength = isoscelesTriangleOpposite(adjacentLength, coneTheta);
        float incircleSize = isoscelesTriangleInRadius(oppositeLength, adjacentLength);
        float2 samplePos = uv + adjacentUnit * (adjacentLength - incircleSize);
        float mipLevel = clamp(log2(incircleSize * max(GetScreenSize().x, GetScreenSize().y)), 0, maxMipLevel);
        float4 newColor = coneSampleWeightedColor(samplePos, mipLevel, glossMulti);
        remainingAlpha -= newColor.a;
        if (remainingAlpha < 0)
        {
            newColor.rgb *= (1.0 - abs(remainingAlpha));
        }
        totalReflectedColor += newColor;
        if (totalReflectedColor.a >= 1)
        {
            break;
        }
        adjacentLength = isoscelesTriangleNextAdjacent(adjacentLength, incircleSize);
        glossMulti *= input.Gloss;
    }
    
    {
        float3 reflectHalf = input.NormalView; // normal is the half for perfect reflections
        float3 viewDir = -input.RayDirView;
        float v_dot_h = dot(viewDir, reflectHalf);
        float3 f = fresnel(input.Color, input.Metalness, v_dot_h);
        pixelColor += totalReflectedColor.xyz * f;
    }
    
    return pixelColor;
}
