#ifndef LIB_LIGHTING
#define LIB_LIGHTING

#include "constants.hlsli"
#include "surfaceinfo.hlsli"

// https://github.com/NVIDIAGameWorks/MathLib/blob/407ecd0d1892d12ee1ec98c3d46cbeed73b79a0d/STL.hlsli#L2147

namespace Lighting_Internal
{
    void ConvertBaseColorMetalnessToAlbedoRf0(float3 baseColor, float metalness, out float3 albedo, out float3 f0)
    {
        albedo = baseColor * saturate(1.0f - metalness);
        f0 = lerp(DIELECTRIC_REFLECTANCE_F0, baseColor, metalness);
    }

    float PositiveRcp(float x)
    {
        return 1.0 / max(x, 1e-15);
    }

    // "Ray Tracing Gems", Chapter 32, Equation 4 - the approximation assumes GGX VNDF and Schlick's approximation
    float3 EnvironmentTerm_Rtg(float3 Rf0, float NoV, float linearRoughness)
    {
        float m = saturate(linearRoughness * linearRoughness);

        float4 X;
        X.x = 1.0;
        X.y = NoV;
        X.z = NoV * NoV;
        X.w = NoV * X.z;

        float4 Y;
        Y.x = 1.0;
        Y.y = m;
        Y.z = m * m;
        Y.w = m * Y.z;

        float2x2 M1 = float2x2(0.99044, -1.28514, 1.29678, -0.755907);
        float3x3 M2 = float3x3(1.0, 2.92338, 59.4188, 20.3225, -27.0302, 222.592, 121.563, 626.13, 316.627);

        float2x2 M3 = float2x2(0.0365463, 3.32707, 9.0632, -9.04756);
        float3x3 M4 = float3x3(1.0, 3.59685, -1.36772, 9.04401, -16.3174, 9.22949, 5.56589, 19.7886, -20.2123);

        float bias = dot(mul(M1, X.xy), Y.xy) * PositiveRcp(dot(mul(M2, X.xyw), Y.xyw));
        float scale = dot(mul(M3, X.xy), Y.xy) * PositiveRcp(dot(mul(M4, X.xzw), Y.xyw));

        return saturate(Rf0 * scale + bias);
    }
}

void DemodulateRadiance(SurfaceInfo surface, float3 viewDir, inout float3 diffuse, inout float3 specular)
{
    float3 albedo, f0;
    Lighting_Internal::ConvertBaseColorMetalnessToAlbedoRf0(surface.Color, surface.Metalness, albedo, f0);

    float n_dot_v = abs(dot(surface.Normal, viewDir));
    float3 f_env = Lighting_Internal::EnvironmentTerm_Rtg(f0, n_dot_v, surface.Roughness);
    
    float3 diffDemod = (1.0 - f_env) * albedo * 0.99 + 0.01;
    float3 specDemod = f_env * 0.99 + 0.01;
    
    diffuse /= diffDemod;
    specular /= specDemod;
}

void DemodulateRadiance(float3 baseColor, float3 normal, float metalness, float roughness, float3 viewDir, inout float3 diffuse, inout float3 specular)
{
    float3 albedo, f0;
    Lighting_Internal::ConvertBaseColorMetalnessToAlbedoRf0(baseColor, metalness, albedo, f0);

    float n_dot_v = abs(dot(normal, viewDir));
    float3 f_env = Lighting_Internal::EnvironmentTerm_Rtg(f0, n_dot_v, roughness);
    
    float3 diffDemod = (1.0 - f_env) * albedo * 0.99 + 0.01;
    float3 specDemod = f_env * 0.99 + 0.01;
    
    diffuse /= diffDemod;
    specular /= specDemod;
}

void ModulateRadiance(SurfaceInfo surface, float3 viewDir, inout float3 diffuse, inout float3 specular)
{
    float3 albedo, f0;
    Lighting_Internal::ConvertBaseColorMetalnessToAlbedoRf0(surface.Color, surface.Metalness, albedo, f0);
    
    float n_dot_v = abs(dot(surface.Normal, viewDir));
    float3 f_env = Lighting_Internal::EnvironmentTerm_Rtg(f0, n_dot_v, surface.Roughness);
    
    float3 diffDemod = (1.0 - f_env) * albedo * 0.99 + 0.01;
    float3 specDemod = f_env * 0.99 + 0.01;
    
    diffuse *= diffDemod;
    specular *= specDemod;
}

void ModulateRadiance(float3 baseColor, float3 normal, float metalness, float roughness, float3 viewDir, inout float3 diffuse, inout float3 specular)
{
    float3 albedo, f0;
    Lighting_Internal::ConvertBaseColorMetalnessToAlbedoRf0(baseColor, metalness, albedo, f0);
    
    float n_dot_v = abs(dot(normal, viewDir));
    float3 f_env = Lighting_Internal::EnvironmentTerm_Rtg(f0, n_dot_v, roughness);
    
    float3 diffDemod = (1.0 - f_env) * albedo * 0.99 + 0.01;
    float3 specDemod = f_env * 0.99 + 0.01;
    
    diffuse *= diffDemod;
    specular *= specDemod;
}

#endif
