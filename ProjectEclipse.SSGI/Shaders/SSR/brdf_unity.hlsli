#ifndef LIB_SSR_BRDF_UNITY
#define LIB_SSR_BRDF_UNITY

#include "common.hlsli"
#include "brdf_ggx.hlsli"
#include "trace_hiz.hlsli"

// Ref: http://jcgt.org/published/0003/02/03/paper.pdf
inline float SmithJointGGXVisibilityTerm(float NdotL, float NdotV, float roughness)
{
#if 0
    // Original formulation:
    //  lambda_v    = (-1 + sqrt(a2 * (1 - NdotL2) / NdotL2 + 1)) * 0.5f;
    //  lambda_l    = (-1 + sqrt(a2 * (1 - NdotV2) / NdotV2 + 1)) * 0.5f;
    //  G           = 1 / (1 + lambda_v + lambda_l);

    // Reorder code to be more optimal
    half a          = roughness;
    half a2         = a * a;

    half lambdaV    = NdotL * sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
    half lambdaL    = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    // Simplify visibility term: (2.0f * NdotL * NdotV) /  ((4.0f * NdotL * NdotV) * (lambda_v + lambda_l + 1e-5f));
    return 0.5f / (lambdaV + lambdaL + 1e-5f);  // This function is not intended to be running on Mobile,
                                                // therefore epsilon is smaller than can be represented by half
#else
    // Approximation of the above formulation (simplify the sqrt, not mathematically correct but close enough)
    float a = roughness;
    float lambdaV = NdotL * (NdotV * (1 - a) + a);
    float lambdaL = NdotV * (NdotL * (1 - a) + a);

#if defined(SHADER_API_SWITCH)
    return 0.5f / (lambdaV + lambdaL + 1e-4f); // work-around against hlslcc rounding error
#else
    return 0.5f / (lambdaV + lambdaL + 1e-5f);
#endif

#endif
}

inline float GGXTerm(float NdotH, float roughness)
{
    float a2 = roughness * roughness;
    float d = (NdotH * a2 - NdotH) * NdotH + 1.0f; // 2 mad
    return (1.0 / PI) * a2 / (d * d + 1e-7f); // This function is not intended to be running on Mobile,
                                            // therefore epsilon is smaller than what can be represented by half
}

float BRDF_Unity_Weight(float3 V, float3 L, float3 N, float Roughness)
{
    float3 H = normalize(L + V);

    float NdotH = saturate(dot(N, H));
    float NdotL = saturate(dot(N, L));
    float NdotV = saturate(dot(N, V));

    half G = SmithJointGGXVisibilityTerm(NdotL, NdotV, Roughness);
    half D = GGXTerm(NdotH, Roughness);

    return (D * G) * (PI / 4.0);
}

float4 TangentToWorld(float3 N, float4 H)
{
    float3 UpVector = abs(N.z) < 0.999 ? float3(0.0, 0.0, 1.0) : float3(1.0, 0.0, 0.0);
    float3 T = normalize(cross(UpVector, N));
    float3 B = cross(N, T);
				 
    return float4((T * H.x) + (B * H.y) + (N * H.z), H.w);
}

// Brian Karis, Epic Games "Real Shading in Unreal Engine 4"
float4 ImportanceSampleGGX(float2 Xi, float Roughness)
{
    float m = Roughness * Roughness;
    float m2 = m * m;
		
    float Phi = 2 * PI * Xi.x;
				 
    float CosTheta = sqrt((1.0 - Xi.y) / (1.0 + (m2 - 1.0) * Xi.y));
    float SinTheta = sqrt(max(1e-5, 1.0 - CosTheta * CosTheta));
				 
    float3 H;
    H.x = SinTheta * cos(Phi);
    H.y = SinTheta * sin(Phi);
    H.z = CosTheta;
		
    float d = (CosTheta * m2 - CosTheta) * CosTheta + 1;
    float D = m2 / (PI * d * d);
    float pdf = D * CosTheta;

    return float4(H, pdf);
}

void GenerateGGXRayAndPdf(SSRInput input, out float3 reflectDir, out float pdf, inout uint randState)
{
    float2 rand2 = NextFloat2(randState);
    rand2.y = lerp(rand2.y, 0, BRDF_BIAS); // bias the bias to cut off very low freq samples
    float4 h = TangentToWorld(input.NormalView, ImportanceSampleGGX(rand2, 1 - input.Gloss));
    reflectDir = normalize(reflect(input.RayDirView, h.xyz));
    pdf = h.w;
}

float GetGGXMirrorPdf(SSRInput input)
{
    float4 h = TangentToWorld(input.NormalView, ImportanceSampleGGX(0, 1 - input.Gloss));
    return h.w;
}

struct UnityLight
{
    float3 color;
    float3 dir;
    float ndotl;
};

struct UnityIndirect
{
    float3 diffuse;
    float3 specular;
};

inline half3 FresnelTerm(half3 F0, half cosA)
{
    half t = pow(1 - cosA, 5); // ala Schlick interpoliation
    return F0 + (1 - F0) * t;
}
inline half3 FresnelLerp(half3 F0, half3 F90, half cosA)
{
    half t = pow(1 - cosA, 5); // ala Schlick interpoliation
    return lerp(F0, F90, t);
}
half SpecularStrength(half3 specular)
{
    return max (max (specular.r, specular.g), specular.b);
}

// Diffuse/Spec Energy conservation
inline half3 EnergyConservationBetweenDiffuseAndSpecular(half3 albedo, half3 specColor, out half oneMinusReflectivity)
{
    oneMinusReflectivity = 1 - SpecularStrength(specColor);
    
    return albedo * (half3(1, 1, 1) - specColor);
}

float PerceptualRoughnessToRoughness(float perceptualRoughness)
{
    return perceptualRoughness * perceptualRoughness;
}

float SmoothnessToPerceptualRoughness(float smoothness)
{
    return (1 - smoothness);
}

// Note: Disney diffuse must be multiply by diffuseAlbedo / PI. This is done outside of this function.
half DisneyDiffuse(half NdotV, half NdotL, half LdotH, half perceptualRoughness)
{
    half fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
    // Two schlick fresnel term
    half lightScatter = (1 + (fd90 - 1) * pow(1 - NdotL, 5));
    half viewScatter = (1 + (fd90 - 1) * pow(1 - NdotV, 5));

    return lightScatter * viewScatter;
}

inline float3 Unity_SafeNormalize(float3 inVec)
{
    float dp3 = max(0.001f, dot(inVec, inVec));
    return inVec * rsqrt(dp3);
}

half4 BRDF1_Unity_PBS(half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness,
    float3 normal, float3 viewDir,
    UnityLight light, UnityIndirect gi)
{
    float perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
    float3 halfDir = Unity_SafeNormalize(float3(light.dir) + viewDir);

// NdotV should not be negative for visible pixels, but it can happen due to perspective projection and normal mapping
// In this case normal should be modified to become valid (i.e facing camera) and not cause weird artifacts.
// but this operation adds few ALU and users may not want it. Alternative is to simply take the abs of NdotV (less correct but works too).
// Following define allow to control this. Set it to 0 if ALU is critical on your platform.
// This correction is interesting for GGX with SmithJoint visibility function because artifacts are more visible in this case due to highlight edge of rough surface
// Edit: Disable this code by default for now as it is not compatible with two sided lighting used in SpeedTree.
#define UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV 0

#if UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV
    // The amount we shift the normal toward the view vector is defined by the dot product.
    half shiftAmount = dot(normal, viewDir);
    normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;
    // A re-normalization should be applied here but as the shift is small we don't do it to save ALU.
    //normal = normalize(normal);

    float nv = saturate(dot(normal, viewDir)); // TODO: this saturate should no be necessary here
#else
    half nv = abs(dot(normal, viewDir)); // This abs allow to limit artifact
#endif

    float nl = saturate(dot(normal, light.dir));
    float nh = saturate(dot(normal, halfDir));

    half lv = saturate(dot(light.dir, viewDir));
    half lh = saturate(dot(light.dir, halfDir));

    // Diffuse term
    half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;

    // Specular term
    // HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
    // BUT 1) that will make shader look significantly darker than Legacy ones
    // and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    
    // GGX with roughtness to 0 would mean no specular at all, using max(roughness, 0.002) here to match HDrenderloop roughtness remapping.
    roughness = max(roughness, 0.002);
    float V = SmithJointGGXVisibilityTerm (nl, nv, roughness);
    float D = GGXTerm (nh, roughness);

    float specularTerm = V * D * PI; // Torrance-Sparrow model, Fresnel is applied later

#ifdef UNITY_COLORSPACE_GAMMA
        specularTerm = sqrt(max(1e-4h, specularTerm));
#endif

    // specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
    specularTerm = max(0, specularTerm * nl);
#if defined(_SPECULARHIGHLIGHTS_OFF)
    specularTerm = 0.0;
#endif

    // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
    half surfaceReduction;
#ifdef UNITY_COLORSPACE_GAMMA
        surfaceReduction = 1.0-0.28*roughness*perceptualRoughness;      // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
#else
    surfaceReduction = 1.0 / (roughness * roughness + 1.0); // fade \in [0.5;1]
#endif

    // To provide true Lambert lighting, we need to be able to kill specular completely.
    specularTerm *= any(specColor) ? 1.0 : 0.0;

    half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
    half3 color = diffColor * (gi.diffuse + light.color * diffuseTerm)
                    + specularTerm * light.color * FresnelTerm(specColor, lh)
                    + surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, nv);

    return half4(color, 1);
}

#endif
