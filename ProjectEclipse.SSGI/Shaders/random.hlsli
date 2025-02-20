#ifndef LIB_RANDOM
#define LIB_RANDOM

#include "constants.hlsli"
#include "common_defs.hlsli"

// https://github.com/riccardoscalco/glsl-pcg-prng/blob/main/index.glsl
uint PCG_Rand(inout uint state)
{
    state = mad(state, 747796405u, 2891336453u);
    uint result = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (result >> 22u) ^ result;
}

float NextFloat(inout uint state)
{
    return PCG_Rand(state) / float(UINT_MAX);
}

float NextFloatBetween0And2(inout uint state)
{
    return PCG_Rand(state) / UINT_MAX_HALF;
}

float3 RandomPointOnSphere(inout uint state)
{
    float z = NextFloatBetween0And2(state) - 1.0f;
    float theta = sqrt(1.0f - z * z);
    float phi = NextFloat(state) * PI_2;
    float x, y;
    sincos(phi, y, x);
    return float3(theta * x, theta * y, z);
}

float3 RandomPointOnHemisphere(inout uint state, float3 surfaceNormal)
{
    float3 v = RandomPointOnSphere(state);
    return v * sign(dot(v, surfaceNormal));
}

float3 RandomDirectionInConeLocal(float oneMinusCosTheta, inout uint rand)
{
    float u = NextFloat(rand);
    float v = NextFloat(rand);
    
    float phi = v * PI_2;
    float z = lerp(1.0, u, oneMinusCosTheta);
    float sinTheta = sqrt(1.0 - z * z);
    float x = sinTheta * cos(phi);
    float y = sinTheta * sin(phi);
    
    float3 localDir = float3(x, y, z);
    return localDir;
}

float3 RandomDirectionInConeLocal(float oneMinusCosTheta, inout RandomState rand)
{
    return RandomDirectionInConeLocal(oneMinusCosTheta, rand.State);
}

// chatgpt made this function
// costheta: cosine of the max angle deviation angle radians
float3 RandomDirectionInCone(float3 direction, float oneMinusCosTheta, inout uint randState)
{
    // Generate random angles within the specified angular radius
    float u = NextFloat(randState);
    float v = NextFloat(randState);
    
    float phi = v * PI_2; // Random azimuthal angle (0 to 2*pi)
    //float z = (1.0 - oneMinusCosTheta) + oneMinusCosTheta * u; // Interpolation between cos(theta) and 1
    float z = lerp(1.0, u, oneMinusCosTheta);
    float sinTheta = sqrt(1.0 - z * z);
    float x = sinTheta * cos(phi);
    float y = sinTheta * sin(phi);

    // Create the local direction vector in spherical coordinates
    float3 localDir = float3(x, y, z);

    // Align localDir with the given direction using a rotation matrix
    float3 up = abs(direction.z) < 0.999 ? float3(0, 0, 1) : float3(1, 0, 0);
    float3 right = normalize(cross(up, direction));
    up = cross(direction, right);

    // Construct the rotation matrix
    float3x3 rotationMatrix = float3x3(right, up, direction);

    // Rotate localDir to align with the specified direction
    return mul(localDir, rotationMatrix);
}

#endif
