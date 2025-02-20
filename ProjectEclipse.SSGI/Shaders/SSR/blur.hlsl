#include "../color.hlsli"

SamplerState PointSampler : register(s1);
SamplerState LinearSampler : register(s2);

Texture2D<float3> sourceTex : register(t0);

cbuffer constants : register(b0)
{
    float2 GaussianDir;
    float MipLevel;
};

static const int2 blurOffsets[7] = { { -3, -3 }, { -2, -2 }, { -1, -1 }, { 0, 0 }, { 1, 1 }, { 2, 2 }, { 3, 3 } };
static const float blurWeights[7] = { 0.001f, 0.028f, 0.233f, 0.474f, 0.233f, 0.028f, 0.001f };

float3 MipBlur(const float4 position : SV_Position, const float2 uv : TEXCOORD0) : SV_Target
{
    static const int numSamples = 7;
    
    float3 result = 0;
    for (int i = 0; i < numSamples; i++)
    {
        float2 offset = blurOffsets[i] * GaussianDir;
        float3 sampleColor = sourceTex.SampleLevel(LinearSampler, uv + offset, MipLevel);

        result += sampleColor * blurWeights[i];
    }
    
    return result;
}
