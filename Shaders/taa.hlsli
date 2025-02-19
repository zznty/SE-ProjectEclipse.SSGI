#include "constants.hlsli"

cbuffer TaaConstants : register(b0)
{
    uint FrameIndex;
    uint2 ViewportSize;
    float CurrentFrameWeight;
};

Texture2D<float4> TaaHistory : register(t0);
RWTexture2D<float4> TaaNewHistory : register(u0);
RWTexture2D<float4> TaaInput : register(u1);
RWTexture2D<float4> TaaOutput : register(u2);

Texture2D<float> gbufferDepth : register(t0, space1);
Texture2D<float3> gbufferVelocities : register(t1, space1);

SamplerState LinearClampSampler : register(s1);

#define THREAD_COUNT 8

float luminance(float3 color)
{
    return dot(color, float3(0.2126f, 0.7152f, 0.0722f));
}

float3 ClampPrevColor(float3 currentColor, float3 prevColor, float2 uv)
{
    return prevColor;
    
    // doesn't work well with noisy ray traced renders
    float3 minColor = 1000, maxColor = -1000;
    
    static const int sampleRadius = 1;
    
    [unroll]
    for (int x = -sampleRadius; x <= sampleRadius; ++x)
    {
        [unroll]
        for (int y = -sampleRadius; y <= sampleRadius; ++y)
        {
            float2 neighbor = uv + float2(x, y);
            float3 color = max(0, TaaInput[neighbor].xyz); // Sample neighbor
            minColor = min(minColor, color); // Take min and max
            maxColor = max(maxColor, color);
        }
    }
    
    float3 prevColorClamped = clamp(prevColor, minColor, maxColor);
    return prevColorClamped;
}

float3 BlendColors(float3 currentColor, float3 prevColor, float currentWeight)
{
    float prevWeight = 1.0 - currentWeight;
    
    currentWeight /= 1.0 + luminance(currentColor);
    prevWeight /= 1.0 + luminance(prevColor);
    
    return (currentColor * currentWeight + prevColor * prevWeight) / max(currentWeight + prevWeight, 0.00001);
}

float4 SampleTextureCatmullRom(in Texture2D<float4> tex, in SamplerState linearSampler, in float2 uv, in float2 texSize)
{
    // We're going to sample a a 4x4 grid of texels surrounding the target UV coordinate. We'll do this by rounding
    // down the sample location to get the exact center of our "starting" texel. The starting texel will be at
    // location [1, 1] in the grid, where [0, 0] is the top left corner.
    float2 samplePos = uv * texSize;
    float2 texPos1 = floor(samplePos - 0.5f) + 0.5f;

    // Compute the fractional offset from our starting texel to our original sample location, which we'll
    // feed into the Catmull-Rom spline function to get our filter weights.
    float2 f = samplePos - texPos1;

    // Compute the Catmull-Rom weights using the fractional offset that we calculated earlier.
    // These equations are pre-expanded based on our knowledge of where the texels will be located,
    // which lets us avoid having to evaluate a piece-wise function.
    float2 w0 = f * (-0.5f + f * (1.0f - 0.5f * f));
    float2 w1 = 1.0f + f * f * (-2.5f + 1.5f * f);
    float2 w2 = f * (0.5f + f * (2.0f - 1.5f * f));
    float2 w3 = f * f * (-0.5f + 0.5f * f);

    // Work out weighting factors and sampling offsets that will let us use bilinear filtering to
    // simultaneously evaluate the middle 2 samples from the 4x4 grid.
    float2 w12 = w1 + w2;
    float2 offset12 = w2 / (w1 + w2);

    // Compute the final UV coordinates we'll use for sampling the texture
    float2 texPos0 = texPos1 - 1;
    float2 texPos3 = texPos1 + 2;
    float2 texPos12 = texPos1 + offset12;

    texPos0 /= texSize;
    texPos3 /= texSize;
    texPos12 /= texSize;

    float4 result = 0.0f;
    result += tex.SampleLevel(linearSampler, float2(texPos0.x, texPos0.y), 0.0f) * w0.x * w0.y;
    result += tex.SampleLevel(linearSampler, float2(texPos12.x, texPos0.y), 0.0f) * w12.x * w0.y;
    result += tex.SampleLevel(linearSampler, float2(texPos3.x, texPos0.y), 0.0f) * w3.x * w0.y;

    result += tex.SampleLevel(linearSampler, float2(texPos0.x, texPos12.y), 0.0f) * w0.x * w12.y;
    result += tex.SampleLevel(linearSampler, float2(texPos12.x, texPos12.y), 0.0f) * w12.x * w12.y;
    result += tex.SampleLevel(linearSampler, float2(texPos3.x, texPos12.y), 0.0f) * w3.x * w12.y;

    result += tex.SampleLevel(linearSampler, float2(texPos0.x, texPos3.y), 0.0f) * w0.x * w3.y;
    result += tex.SampleLevel(linearSampler, float2(texPos12.x, texPos3.y), 0.0f) * w12.x * w3.y;
    result += tex.SampleLevel(linearSampler, float2(texPos3.x, texPos3.y), 0.0f) * w3.x * w3.y;

    return result;
}

uint2 ThreadGroupTilingX(
	const uint2 dipatchGridDim, // Arguments of the Dispatch call (typically from a ConstantBuffer)
	const uint2 ctaDim, // Already known in HLSL, eg:[numthreads(8, 8, 1)] -> uint2(8, 8)
	const uint maxTileWidth, // User parameter (N). Recommended values: 8, 16 or 32.
	const uint2 groupThreadID, // SV_GroupThreadID
	const uint2 groupId // SV_GroupID
)
{
	// A perfect tile is one with dimensions = [maxTileWidth, dipatchGridDim.y]
    const uint Number_of_CTAs_in_a_perfect_tile = maxTileWidth * dipatchGridDim.y;

	// Possible number of perfect tiles
    const uint Number_of_perfect_tiles = dipatchGridDim.x / maxTileWidth;

	// Total number of CTAs present in the perfect tiles
    const uint Total_CTAs_in_all_perfect_tiles = Number_of_perfect_tiles * maxTileWidth * dipatchGridDim.y;
    const uint vThreadGroupIDFlattened = dipatchGridDim.x * groupId.y + groupId.x;

	// Tile_ID_of_current_CTA : current CTA to TILE-ID mapping.
    const uint Tile_ID_of_current_CTA = vThreadGroupIDFlattened / Number_of_CTAs_in_a_perfect_tile;
    const uint Local_CTA_ID_within_current_tile = vThreadGroupIDFlattened % Number_of_CTAs_in_a_perfect_tile;
    uint Local_CTA_ID_y_within_current_tile;
    uint Local_CTA_ID_x_within_current_tile;

    if (Total_CTAs_in_all_perfect_tiles <= vThreadGroupIDFlattened)
    {
		// Path taken only if the last tile has imperfect dimensions and CTAs from the last tile are launched. 
        uint X_dimension_of_last_tile = dipatchGridDim.x % maxTileWidth;
#ifdef DXC_STATIC_DISPATCH_GRID_DIM
		X_dimension_of_last_tile = max(1, X_dimension_of_last_tile);
#endif
        Local_CTA_ID_y_within_current_tile = Local_CTA_ID_within_current_tile / X_dimension_of_last_tile;
        Local_CTA_ID_x_within_current_tile = Local_CTA_ID_within_current_tile % X_dimension_of_last_tile;
    }
    else
    {
        Local_CTA_ID_y_within_current_tile = Local_CTA_ID_within_current_tile / maxTileWidth;
        Local_CTA_ID_x_within_current_tile = Local_CTA_ID_within_current_tile % maxTileWidth;
    }

    const uint Swizzled_vThreadGroupIDFlattened =
		Tile_ID_of_current_CTA * maxTileWidth +
		Local_CTA_ID_y_within_current_tile * dipatchGridDim.x +
		Local_CTA_ID_x_within_current_tile;

    uint2 SwizzledvThreadGroupID;
    SwizzledvThreadGroupID.y = Swizzled_vThreadGroupIDFlattened / dipatchGridDim.x;
    SwizzledvThreadGroupID.x = Swizzled_vThreadGroupIDFlattened % dipatchGridDim.x;

    uint2 SwizzledvThreadID;
    SwizzledvThreadID.x = ctaDim.x * SwizzledvThreadGroupID.x + groupThreadID.x;
    SwizzledvThreadID.y = ctaDim.y * SwizzledvThreadGroupID.y + groupThreadID.y;

    return SwizzledvThreadID.xy;
}

[numthreads(THREAD_COUNT, THREAD_COUNT, 1)]
void main(const uint3 dispatchThreadId : SV_DispatchThreadID, const uint2 groupThreadId : SV_GroupThreadID, const uint2 groupId : SV_GroupID)
{
    uint2 dispatchGridDim = uint2(ViewportSize / THREAD_COUNT.xx);
    const int3 pixelPos = int3(ThreadGroupTilingX(dispatchGridDim, THREAD_COUNT.xx, 16, groupThreadId, groupId), 0);
    //const int3 pixelPos = int3(dispatchThreadId.xy, 0);
    const float2 uv = (pixelPos.xy + 0.5) / ViewportSize;
    const float3 motionVector = gbufferVelocities.Load(pixelPos) * float3(0.5, -0.5, 1.0);
    
    float4 currentColor = TaaInput[pixelPos.xy];
    
    float2 prevUV = uv - motionVector.xy;
    float3 prevColor = saturate(SampleTextureCatmullRom(TaaHistory, LinearClampSampler, prevUV, ViewportSize).xyz);
    
    float currDepth = gbufferDepth.Load(pixelPos);
    float prevDepth = TaaHistory[prevUV * ViewportSize].w;
    
    // depth rejection
    if (FrameIndex == 1 || currDepth == 1.0 || any(prevUV < 0 || prevUV >= 1) || (currDepth > 1.01 * prevDepth) || (currDepth < 0.99 * prevDepth))
    {
        TaaNewHistory[pixelPos.xy] = float4(currentColor.xyz, currDepth);
        TaaOutput[pixelPos.xy] = float4(currentColor.xyz, 1);
        return;
    }
    
    float3 prevColorClamped = ClampPrevColor(currentColor.xyz, prevColor, uv);
    float3 blended = BlendColors(currentColor.xyz, prevColor, CurrentFrameWeight);
    
    TaaNewHistory[pixelPos.xy] = float4(blended, currDepth);
    TaaOutput[pixelPos.xy] = float4(blended, 1);
}
