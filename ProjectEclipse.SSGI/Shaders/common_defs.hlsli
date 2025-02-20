#ifndef LIB_COMMON_DEFS
#define LIB_COMMON_DEFS

struct PerFrameData
{
    uint RandomSeed;
    int2 Viewport;
    float ViewDistance;
    
    float4 SunDirection;
    float4 CameraMovement;
    
    float4x4 ViewMatrix;
    float4x4 InvViewMatrix;
    float4x4 ProjectionMatrix;
    float4x4 ViewProjectionMatrix;
    float4x4 InvViewProjectionMatrix;
    float4x4 PrevViewProjMatrix;
    
    float4x4 InvSkyboxMatrix;
};

struct PerFrameGeometryData
{
    float4x4 WorldMatrix;
    float4x4 PrevWorldMatrix;
    float3 ColorHsvOffset;
    bool MetalnessColorable;
};

struct Ray
{
    float3 Origin;
    float3 Direction;
};

struct SphereLightSource
{
    // used ONLY for getting the index of the light using the index from the previous frame
    // uint.max if the light no longer exists
    // otherwise, value is between 0 to (buffer size - 1)
    uint ReprojectionIndex;
    
    float3 Position;
    float Radius;
    float3 Color;
    float Intensity;
};

struct Triangle
{
    int Index0; // means the index of the first vertex in the vertex buffer
    int Index1;
    int Index2;
};

struct PackedMatrix
{
    float4 Value;
    
    float4x4 Unpack()
    {
        const float4 Directions[6] =
        {
            float4( 0, 0,-1, 0), // Forward
            float4( 0, 0, 1, 0), // Backward
            float4(-1, 0, 0, 0), // Left
            float4( 1, 0, 0, 0), // Right
            float4( 0, 1, 0, 0), // Up
            float4( 0,-1, 0, 0), // Down
        };
        
        int key = (int)Value.w;
        float4 backward = -Directions[key / 6];
        float4 up = Directions[key % 6];
        
        float4 cross1 = float4(cross(up.xyz, backward.xyz), 0);
        
        return float4x4(
            cross1, up, backward,
            float4(Value.xyz, 1));
    }
};

struct RandomState
{
    uint State;
};

struct Byte4
{
    uint Packed;
    
    uint4 Unpack()
    {
        return uint4(Packed, Packed >> 8, Packed >> 16, Packed >> 24) & 0x000000FF;
    }
};

struct Half4
{
    uint2 Packed;
    
    float4 Unpack()
    {
        return f16tof32(uint4(Packed.x, Packed.x >> 16, Packed.y, Packed.y >> 16));
    }
};

struct Half2
{
    uint Packed;
    
    float2 Unpack()
    {
        return f16tof32(uint2(Packed, Packed >> 16));
    }
};

#endif
