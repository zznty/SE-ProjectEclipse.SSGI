#ifndef SAMPLING_GGX
#define SAMPLING_GGX

#include "constants.hlsli"

// alphaSq = roughness^4
float D_GGX(float alphaSq, float n_dot_h)
{
    float divisor = PI * pow((alphaSq - 1.0) * (n_dot_h * n_dot_h) + 1.0f, 2);
    return alphaSq / divisor;
}

float Smith_G1_GGX(float alphaSquared, float n_dot_v_squared) // only used for pdf calculation?
{
    return 2.0 / (sqrt(((alphaSquared * (1.0 - n_dot_v_squared)) + n_dot_v_squared) / n_dot_v_squared) + 1.0);
}

float4 getRotationToZAxis(float3 input)
{
	// Handle special case when input is exact or near opposite of (0, 0, 1)
    if (input.z < -0.99999f)
        return float4(1.0f, 0.0f, 0.0f, 0.0f);

    return normalize(float4(input.y, -input.x, 0.0f, 1.0f + input.z));
}

float3 rotatePoint(float4 q, float3 v)
{
    const float3 qAxis = float3(q.x, q.y, q.z);
    return 2.0f * dot(qAxis, v) * qAxis + (q.w * q.w - dot(qAxis, qAxis)) * v + 2.0f * q.w * cross(qAxis, v);
}

float4 invertRotation(float4 q)
{
    return float4(-q.x, -q.y, -q.z, q.w);
}

// viewDir: wo. dir from surfce to observer (camera)
// normal: surface normal
// alpha: usually roughness^2
// rand2: two (different) uniform random numbers in [0, 1]
// returns: sampled direction
float3 SampleGGXVNDF(const float3 viewDir, const float3 normal, const float roughness, const float2 rand2)
{
    // transform viewDir to normal-tangent space (up is 0,0,1)
    float4 rotation = getRotationToZAxis(normal);
    float3 Ve = rotatePoint(rotation, viewDir); // local view dir
    float alpha_x = roughness * roughness;
    float alpha_y = alpha_x;
    
    float3 Ne; // local microfacet normal (half vector)
    
    // https://www.jcgt.org/published/0007/04/01/paper.pdf
    {        
        // Section 3.2: transforming the view direction to the hemisphere configuration
        float3 Vh = normalize(float3(alpha_x * viewDir.x, alpha_y * viewDir.y, viewDir.z));
        
        // Section 4.1: orthonormal basis (with special case if cross product is zero)
        float lengthSq = dot(Vh.xy, Vh.xy);
        float3 T1 = lengthSq > 0 ? float3(-Vh.y, Vh.x, 0) * rsqrt(lengthSq) : float3(1, 0, 0);
        float3 T2 = cross(Vh, T1);
        
        // Section 4.2: parameterization of the projected area
        float r = sqrt(rand2.x);
        float phi = 2.0 * PI * rand2.y;
        float cosPhi, sinPhi;
        sincos(phi, sinPhi, cosPhi);
        float t1 = r * cosPhi;
        float t2 = r * sinPhi;
        float s = 0.5 * (1.0 + Vh.z);
        t2 = (1.0 - s) * sqrt(1.0 - t1 * t1) + s * t2;
        
        // Section 4.3: reprojection onto hemisphere
        float3 Nh = t1 * T1 + t2 * T2 + sqrt(max(0.0, 1.0 - t1 * t1 - t2 * t2)) * Vh; // note: confusingly named variables
        
        // Section 3.4: transforming the normal back to the ellipsoid configuration
        Ne = normalize(float3(alpha_x * Nh.x, alpha_y * Nh.y, max(0.0, Nh.z)));
    }
    
    float3 sampledDir = reflect(-Ve, Ne);
    sampledDir = normalize(rotatePoint(invertRotation(rotation), sampledDir));
    return sampledDir;
}

// https://www.shadertoy.com/view/MX3XDf
// rand2: 2 different uniform random numbers in [0, 1]
// wi: surface -> camera direction in surface normal tangent space
// alpha: roughness * roughness
//float3 SphericalCapBoundedWithPDFRatio(float2 rand2, float3 wi, const float alpha, out float pdf)
//{
//    // warp to the hemisphere configuration
//    const float alpha_x = alpha;
//    const float alpha_y = alpha;
//    
//    //PGilcher: save the length t here for pdf ratio
//    float3 wiStd = float3(wi.x * alpha_x, wi.y * alpha_y, wi.z);
//    float t = length(wiStd);
//    wiStd /= t;
//    
//    // sample a spherical cap in (-wi.z, 1]
//    float phi = (2.0f * rand2.x - 1.0f) * PI;
//    
//    float a = saturate(min(alpha_x, alpha_y)); // Eq. 6
//    float s = 1.0f + length(wi.xy); // Omit sgn for a <=1
//    float a2 = a * a;
//    float s2 = s * s;
//    float k = (1.0 - a2) * s2 / (s2 + a2 * wi.z * wi.z);
//
//    float b = wiStd.z;
//    b = wi.z > 0.0 ? k * b : b;
//
//    //PGilcher: compute ratio of unchanged pdf to actual pdf (ndf/2 cancels out)
//    //Dupuy's method is identical to this except that "k" is always 1, so
//    //we extract the differences of the PDFs (Listing 2 in the paper)
//    float pdf_ratio = (k * wi.z + t) / (wi.z + t);
//    
//    float z = mad((1.0f - rand2.y), (1.0f + b), -b);
//    float sinTheta = sqrt(clamp(1.0f - z * z, 0.0f, 1.0f));
//    float x = sinTheta * cos(phi);
//    float y = sinTheta * sin(phi);
//    float3 c = float3(x, y, z);
//    // compute halfway direction as standard normal
//    float3 wmStd = c + wiStd;
//    // warp back to the ellipsoid configuration
//    float3 wm = normalize(float3(wmStd.x * alpha_x, wmStd.y * alpha_y, wmStd.z));
//    
//    float v_h = dot(wi, wm);
//    float n_v = dot(float3(0, 0, 1), wi);
//    float n_l = dot(float3(0, 0, 1), reflect(-wi, wm));
//    float f = fresnel_schlick_(v_h, 0.04);
//    float g1 = smith_G1(n_v, alpha);
//    float g2 = smith_G2(n_l, n_v, alpha);
//    
//    pdf = (f * g2 / g1 * pdf_ratio);
//    
//    // return final normal
//    return wm;
//}

float PdfGGXVNDF(float3 viewDir, float3 lightDir, float3 normal, float roughness)
{
    float3 halfDir = normalize(viewDir + lightDir);
    float n_dot_v = dot(normal, viewDir);
    float n_dot_h = dot(normal, halfDir);
    float alpha = roughness * roughness;
    float alphaSquared = alpha * alpha;
    
    return (D_GGX(alphaSquared, n_dot_h) * Smith_G1_GGX(alphaSquared, n_dot_v * n_dot_v)) / (4.0 * n_dot_v);
}

#endif
