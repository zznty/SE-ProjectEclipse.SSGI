#ifndef LIB_BRDF_GGX
#define LIB_BRDF_GGX

#include "constants.hlsli"
#include "surfaceinfo.hlsli"
#include "color.hlsli"
#include "sampling_ggx.hlsli"

// most stuff is from https://boksajak.github.io/files/CrashCourseBRDF.pdf
// and https://github.com/boksajak/brdf/blob/master/brdf.h

// normal distribution function
float ggx_d(float a2, float n_dot_h)
{
    float divisor = PI * pow((a2 - 1.0) * (n_dot_h * n_dot_h) + 1.0f, 2);
    return a2 / divisor;
}

float smith_ggx_g1(float n_dot_s, float a2)
{
    float n_dot_s_2 = n_dot_s * n_dot_s;
    return 2.0f / (sqrt(((a2 * (1.0f - n_dot_s_2)) + n_dot_s_2) / n_dot_s_2) + 1.0f);
}

// S can be L or V
float smith_ggx_g1_alpha(float a, float n_dot_s)
{
    return n_dot_s / (max(0.000001f, a) * sqrt(1.0 - min(0.999999f, n_dot_s * n_dot_s)));
}

float smith_ggx_g_lambda(float a) // a is not roughness
{
    return (-1.0 + sqrt(1.0 + (1.0 / (a * a)))) * 0.5f;
}

float smith_ggx_g2(float a, float n_dot_l, float n_dot_v)
{
    float aL = smith_ggx_g1_alpha(a, n_dot_l);
    float aV = smith_ggx_g1_alpha(a, n_dot_v);
    return 1.0 / (1.0 + smith_ggx_g_lambda(aL) + smith_ggx_g_lambda(aV));
}

// geometric attenuation/shadowing
float smith_ggx_g(float a, float n_dot_l, float n_dot_v)
{
    return smith_ggx_g2(a, n_dot_l, n_dot_v);
}

// f0 and f90: reflectance (average of both polarizations) at 0 degrees and 90 degrees
float3 fresnel_schlick(float3 f0, float f90, float v_dot_h)
{
    // if x param of pow is < 0, the result is null
    // if x == 0, result is 0, 1, or nan
    // https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-pow#remarks
    float oneMinusVdotH = 1 - v_dot_h;
    float pow5 = oneMinusVdotH <= 0 ? 0 : pow(oneMinusVdotH, 5);
    return f0 + (f90 - f0) * pow5;
}

float3 lambertianDiffuse(float3 diffuseReflectance, float n_dot_l)
{
    return diffuseReflectance * (1.0 / PI) * n_dot_l;
}

float3 fresnel(float3 baseColor, float metalness, float v_dot_h)
{
    float3 f0 = lerp(DIELECTRIC_REFLECTANCE_F0, baseColor, metalness);
    float f90 = min(1.0, DIELECTRIC_REFLECTANCE_F0_RCP * luminance(f0));
    
    return fresnel_schlick(f0, f90, v_dot_h);
}

float3 specular(float a, float n_dot_h, float n_dot_l, float n_dot_v, float3 fresnel)
{
    float d = ggx_d(max(0.000001, a * a), n_dot_h);
    float g2 = smith_ggx_g(a, n_dot_l, n_dot_v);
    
    return ((fresnel * g2 * d) / (4.0f * n_dot_l * n_dot_v)) * n_dot_l;
}

float3 diffuse(float3 baseColor, float metalness, float n_dot_l)
{
    return lambertianDiffuse(baseColor * (1.0 - metalness), n_dot_l);
}

float3 Brdf(float roughness, float metalness, float3 baseColor, float3 viewDir, float3 lightDir, float3 surfaceNormal)
{
    float a = roughness * roughness;
    float3 h = normalize(viewDir + lightDir);
    float n_dot_h = dot(surfaceNormal, h);
    float n_dot_v = dot(surfaceNormal, viewDir);
    float n_dot_l = dot(surfaceNormal, lightDir);
    float v_dot_h = dot(viewDir, h); // same as l_dot_h
    
    float3 f = fresnel(baseColor, metalness, v_dot_h);
    float3 s = specular(a, n_dot_h, n_dot_l, n_dot_v, f);
    float3 d = diffuse(baseColor, metalness, n_dot_l);
    
    bool valid = n_dot_l > 0 && n_dot_v > 0;
    return ((1.0 - f) * d + s) * valid;
}

void Brdf(float roughness, float metalness, float3 baseColor, float3 viewDir, float3 lightDir, float3 surfaceNormal, out float3 diff, out float3 spec)
{
    float a = roughness * roughness;
    float3 h = normalize(viewDir + lightDir);
    float n_dot_h = dot(surfaceNormal, h);
    float n_dot_v = dot(surfaceNormal, viewDir);
    float n_dot_l = dot(surfaceNormal, lightDir);
    float v_dot_h = dot(viewDir, h); // same as l_dot_h
    
    float3 f = fresnel(baseColor, metalness, v_dot_h);
    float3 s = specular(a, n_dot_h, n_dot_l, n_dot_v, f);
    float3 d = diffuse(baseColor, metalness, n_dot_l);
    
    bool valid = n_dot_l > 0 && n_dot_v > 0;
    diff = valid ? (1.0 - f) * d : float3(0, 0, 0);
    spec = valid ? s : float3(0, 0, 0);
}

float3 Brdf(SurfaceInfo surface, float3 viewDir, float3 lightDir)
{
    return Brdf(surface.Roughness, surface.Metalness, surface.Color, viewDir, lightDir, surface.Normal) * surface.AmbientOcclusion;
}

void Brdf(SurfaceInfo surface, float3 viewDir, float3 lightDir, out float3 diff, out float3 spec)
{
    Brdf(surface.Roughness, surface.Metalness, surface.Color, viewDir, lightDir, surface.Normal, diff, spec);
    diff *= surface.AmbientOcclusion;
    spec *= surface.AmbientOcclusion;
}

float sampleGGXVNDFReflectionPdf(SurfaceInfo surface, float3 viewDir, float3 lightDir)
{
    return PdfGGXVNDF(viewDir, lightDir, surface.Normal, surface.Roughness);
}

void SampleWeightedBrdf(SurfaceInfo surface, float3 viewDir, float2 rand2, out float pdf, out float3 reflectDir)
{
    reflectDir = SampleGGXVNDF(viewDir, surface.Normal, surface.Roughness, rand2);
    pdf = PdfGGXVNDF(viewDir, reflectDir, surface.Normal, surface.Roughness);
}

#endif
