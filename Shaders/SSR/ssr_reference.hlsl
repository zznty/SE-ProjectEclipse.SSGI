#include "../random.hlsli"
//#include "../Pcss/frame.hlsli"
#include "brdf_ggx.hlsli"
#include "common.hlsli"
#include "trace_hiz.hlsli"
#include "../sampling_ggx.hlsli"
#include "../sampler_sobol.hlsli"

float3 main(float4 position : SV_Position, float2 uv : TEXCOORD0) : SV_Target0
{
    const uint2 pixelPos = uint2(position.xy);
    const uint pixelIndex = pixelPos.y * GetScreenSize().x + pixelPos.x;
    float3 pixelColor = FrameBuffer[pixelPos];
    SSRInput input = LoadSSRInput(pixelPos);
    
#if REFLECTION_ONLY
    pixelColor = 0;
    input.Color = 1;
#endif
    
    [branch]
    if (!input.IsForeground || input.Gloss < REFLECT_GLOSS_THRESHOLD)
    {
        return pixelColor;
    }
    
    const bool isMirror = input.Gloss > MIRROR_REFLECTION_THRESHOLD;
    
    [branch]
    if (isMirror) // fast 1rpp path for mirror reflections
    {
        input.Gloss = 0.95;
        
        float3 reflectDir = reflect(input.RayDirView, input.NormalView);
        
        float2 uvHit;
        float hitConfidence = TraceRayHiZ(uv, input, reflectDir, uvHit);
        
        [branch]
        if (hitConfidence != 0)
        {
            const float3 hitColor = FrameBuffer.SampleLevel(LinearSampler, uvHit, 0).xyz;
            const float3 brdfSpecular = BrdfSpecular(1 - input.Gloss, input.Metalness, input.Color, -input.RayDirView, reflectDir, input.NormalView);
            const float pdf = PdfGGXVNDF(-input.RayDirView, reflectDir, input.NormalView, 1 - input.Gloss);
            pixelColor += hitColor * hitConfidence * brdfSpecular / pdf * IndirectLightMulti;
        }
    }
    else
    {
        uint randState = asuint(InterleavedGradientNoise(pixelPos)) * RandomSeed;
        static const uint raysPerPixel = 4;
        static const float contributionPerRay = 1.0 / raysPerPixel;
        
        SobolOwenSampler qrng;
        qrng.Init(FrameIndex * raysPerPixel, uint2(PCG_Rand(randState), PCG_Rand(randState)));
        
        for (uint i = 0; i < raysPerPixel; i++)
        {
            float pdf = 0;
            float3 reflectDir = 0;
            
            // contribution is 0 if ray reflect dir is below the surface
            // try to generate a valid reflect ray up to 5 times
            float n_dot_l = 0;
            for (uint j = 0; j < 5 && n_dot_l <= 0; j++)
            {
                float2 qRand2 = qrng.Next();
                reflectDir = SampleGGXVNDF(-input.RayDirView, input.NormalView, 1 - input.Gloss, qRand2);
                pdf = PdfGGXVNDF(-input.RayDirView, reflectDir, input.NormalView, 1.0 - input.Gloss);
                n_dot_l = dot(input.NormalView, reflectDir);
            }
            
            if (n_dot_l <= 0)
            {
                continue;
            }
            
            float2 uvHit;
            float hitConfidence = TraceRayHiZ(uv, input, reflectDir, uvHit);
            
            if (hitConfidence != 0)
            {
                const float3 hitColor = FrameBuffer.SampleLevel(LinearSampler, uvHit, 0).xyz;
                float3 brdf = Brdf(1 - input.Gloss, input.Metalness, input.Color, -input.RayDirView, reflectDir, input.NormalView);
                brdf /= pdf;
                pixelColor += (hitColor * hitConfidence) * brdf * contributionPerRay * IndirectLightMulti;
            }
        }
    }
    
    return pixelColor;
}
