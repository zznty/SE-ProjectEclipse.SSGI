#ifndef LIB_SURFACE_INFO
#define LIB_SURFACE_INFO

struct SurfaceInfo
{
    float3 Color;
    float3 Normal;
    float Metalness;
    float Roughness;
    float AmbientOcclusion;
    float Emissiveness;
    float3 Position;
};

#endif
