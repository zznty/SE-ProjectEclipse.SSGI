#ifndef LIB_MATH
#define LIB_MATH

#include "constants.hlsli"
#include "common_defs.hlsli"

float3 CalculateRayDirection(const float2 uv, const float4x4 invViewProjMatrix)
{
    const float2 ndc = mad(uv, float2(2.0, -2.0), float2(-1.0, 1.0));
    const float4 clipPos = float4(ndc, 1.0, 1.0);
    const float4 transformed = mul(invViewProjMatrix, clipPos);
    return normalize(transformed.xyz / transformed.w);
}

float3 UnpackNormal(uint packedNormal)
{
    uint2 packed = uint2(packedNormal & 0x0000ffff, packedNormal >> 16);
    float zsign = -1;
    if (packed.x > 32767)
    {
        zsign = 1;
        packed.x &= (1 << 15) - 1;
    }
    float2 xy = mad(packed / 32767.0, 2.0, -1.0);
    float z = zsign * sqrt(saturate(1 - dot(xy, xy)));
    return float3(xy, z);
}

// same as above but the input is precomputed 'packed' value
float3 UnpackNormal_uint16(uint2 packed)
{
    float zsign = -1;
    if (packed.x > 32767)
    {
        zsign = 1;
        packed.x &= (1 << 15) - 1;
    }
    float2 xy = mad(packed / 32767.0, 2.0, -1.0);
    float z = zsign * sqrt(saturate(1 - dot(xy, xy)));
    return float3(xy, z);
}

// Spheremap Transform, http://aras-p.info/texts/CompactNormalStorage.html
float3 unpack_normals2(float2 enc)
{
    float2 fenc = enc * 4 - 2;
    float f = dot(fenc, fenc);
    float g = sqrt(1 - f / 4);
    float3 n;
    n.xy = fenc*g;
    n.z = 1 - f / 2;
    return n;
}

float4 ToVector4(uint packedValue)
{
    float4 result;
    result.x = packedValue & 0xFFu;
    result.y = (packedValue >> 8) & 0xFFu;
    result.z = (packedValue >> 16) & 0xFFu;
    result.w = (packedValue >> 24) & 0xFFu;
    return result;
}

float4 UnpackTangentSign(uint packedTangent)
{
    float4 vec = ToVector4(packedTangent);
    float2 ywSign = mad((vec.yw > 127.5), 2, -1);
    
    vec.yw -= (128 * (ywSign > 0));

    float2 xy = mad(2, mad(256, vec.yw, vec.xz) / 32767.0, -1);
    float num7 = max(0, 1 - dot(xy, xy));
    float z = ywSign.x * sqrt(num7);
    return float4(xy, z, ywSign.y);
}

// result is a unit vector
//float3 SphericalToCartesianCoords(float elevation, float azimuth)
//{
//    // TODO: use sincos
//    return float3(sin(elevation) * cos(azimuth), sin(elevation) * sin(azimuth), cos(elevation));
//}

float lengthSq(float3 vec)
{
    return dot(vec, vec);
}
#define lengthSq(x) dot(x, x)

float distSq(float3 pos1, float3 pos2)
{
    return lengthSq(pos1 - pos2);
}
#define distSq(x, y) lengthSq(x - y)

// https://gamedev.stackexchange.com/questions/96459/fast-ray-sphere-collision-code
float RayIntersectsSphere(float3 rayOrigin, float3 rayDir, float3 spherePos, float sphereRadius)
{
    float3 m = rayOrigin - spherePos;
    float b = dot(m, rayDir);
    float c = dot(m, m) - (sphereRadius * sphereRadius);
    float discriminant = b * b - c;

    float t = max(0, -b - sqrt(discriminant));

    return ((c <= 0 || b <= 0) && discriminant >= 0) ? t : FLOAT_MAX;
}

float RayIntersectsSphere(Ray ray, float3 spherePos, float sphereRadius)
{
    return RayIntersectsSphere(ray.Origin, ray.Direction, spherePos, sphereRadius);
}

float3 GetDirectionInConeWithUV(float3 coneCenterDir, float angularRadiusRad, float2 uv)
{
    float u = uv.x, v = uv.y;
    float oneMinusCosTheta = 1.0 - cos(angularRadiusRad);
    
    float phi = v * PI_2; // Random azimuthal angle (0 to 2*pi)
    //float z = (1.0 - oneMinusCosTheta) + oneMinusCosTheta * u; // Interpolation between cos(theta) and 1
    float z = lerp(1.0, u, oneMinusCosTheta);
    float sinTheta = sqrt(1.0 - z * z);
    float x = sinTheta * cos(phi);
    float y = sinTheta * sin(phi);

    // Create the local direction vector in spherical coordinates
    float3 localDir = float3(x, y, z);

    // Align localDir with the given direction using a rotation matrix
    float3 up = abs(coneCenterDir.z) < 0.999 ? float3(0, 0, 1) : float3(1, 0, 0);
    float3 right = normalize(cross(up, coneCenterDir));
    up = cross(coneCenterDir, right);

    // Construct the rotation matrix
    float3x3 rotationMatrix = float3x3(right, up, coneCenterDir);

    // Rotate localDir to align with the specified direction
    return mul(localDir, rotationMatrix);
}

float3 WorldDirToConeDir(float3 coneCenterDir, float oneMinusCosTheta, float3 worldDir)
{
    float3 up = abs(coneCenterDir.z) < 0.999 ? float3(0, 0, 1) : float3(1, 0, 0);
    float3 right = normalize(cross(up, coneCenterDir));
    up = cross(coneCenterDir, right);
    
    float3x3 invRotationMatrix = transpose(float3x3(right, up, coneCenterDir));
    float3 localDir = mul(worldDir, invRotationMatrix);
    return localDir;
}

float3 ConeDirToWorldDir(float3 coneCenterDir, float oneMinusCosTheta, float3 coneLocalDir)
{
    float3 up = abs(coneCenterDir.z) < 0.999 ? float3(0, 0, 1) : float3(1, 0, 0);
    float3 right = normalize(cross(up, coneCenterDir));
    up = cross(coneCenterDir, right);

    float3x3 rotationMatrix = float3x3(right, up, coneCenterDir);
    float3 worldDir = mul(coneLocalDir, rotationMatrix);
    return worldDir;
}

// uv: polar/elevation
float3 SphericalToCartesian(float2 uv)
{
    float sinV, cosV, sinU, cosU;
    sincos(uv.y, sinV, cosV);
    sincos(uv.x, sinU, cosU);
    
    float3 result;
    result.x = cosV * cosU;
    result.y = sinV;
    result.z = cosV * sinU;
    return result;
}

// polar/elevation
float2 CartesianToSpherical(float3 coords)
{
    float2 uv;
    uv.x = atan2(coords.y, coords.x);
    uv.y = acos(coords.z);
    return uv;
}

// converts the normal map direction from tangent space to world space
float3 ConvertNormalMapToWorld(float3 tangentNormal, float3x3 tbnWorld)
{
    // normalize the tangent normal (idk what this is)
    float3 normalMap = tangentNormal * 2.0 - 1.0;
    normalMap.y *= -1;
    
    // convert tangent normal to world space normal
    // (tbn vectors already transformed to world space in the vertex shader)
    float3 worldNormal = normalize(mul(normalMap, tbnWorld));
    
    return worldNormal;
}

float sq(float x)
{
    return x * x;
}
#define sq(x) x * x

float invlerp(float x, float y, float v)
{
    return (v - x) / (y - x);
}

#endif
