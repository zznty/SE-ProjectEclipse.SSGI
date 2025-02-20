#define RESTIR_CS

#include "common.hlsli"
#include "brdf_ggx.hlsli"
#include "../lighting.hlsli"

Texture2D<float3> DiffuseIrradiance  : register(t6);
Texture2D<float3> SpecularIrradiance : register(t7);

[numthreads(NUM_THREADS_XY, NUM_THREADS_XY, 1)]
void cs(const uint3 dispatchThreadId : SV_DispatchThreadID)
{
    const uint2 pixelPos = dispatchThreadId.xy;
    const float2 uv = (pixelPos + 0.5) / ScreenSize;
    const SSRInput input = LoadSSRInput(pixelPos);
    
    float3 pixelColor = FrameBuffer[pixelPos];
    
    if (!input.IsForeground)
    {
        RestirOutputTexture[pixelPos] = float4(pixelColor, 1);
        return;
    }
    
    float3 diffuse = DiffuseIrradiance[pixelPos];
    float3 specular = SpecularIrradiance[pixelPos];
    
    diffuse = isfinite(diffuse) ? diffuse : 0; // bandaid fix. TODO: fix nan issue
    specular = isfinite(specular) ? specular : 0;
    
    ModulateRadiance(input.Color, input.NormalView, input.Metalness, 1 - input.Gloss, -input.RayDirView, diffuse, specular);
    
    RestirOutputTexture[pixelPos] = float4(pixelColor + (diffuse + specular) * IndirectLightMulti, 1);
}
