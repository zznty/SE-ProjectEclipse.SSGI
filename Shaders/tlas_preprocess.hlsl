#include "common_defs.hlsli"

#define ushort uint16_t
#define ulong uint64_t

struct RaytracingInstanceDesc
{
    // as input: row-major float4x3 transposed to column-major float3x4
    // as output: row-major float3x4 transposed to float4x3 packed in a float3x4 (wtf)
    float3x4 Transform;
    uint InstanceIdAndMask;
    uint HitGroupIndexAndFlags;
    ulong BlasAddress;
};

cbuffer Constants : register(b0)
{
    uint InstanceCount;
};

StructuredBuffer<PackedMatrix> SecondaryTransforms : register(t0);
RWStructuredBuffer<RaytracingInstanceDesc> TlasInstances : register(u0);

[numthreads(64, 1, 1)]
void main(const uint3 dispatchThreadId : SV_DispatchThreadID)
{
    if (dispatchThreadId.x >= InstanceCount)
    {
        return;
    }
    
    float3x4 worldTransformTransposed = TlasInstances[dispatchThreadId.x].Transform;
    
    float4x4 worldTransform4x4 = float4x4(
        float4(worldTransformTransposed._11_21_31, 0),
        float4(worldTransformTransposed._12_22_32, 0),
        float4(worldTransformTransposed._13_23_33, 0),
        float4(worldTransformTransposed._14_24_34, 1));
    
    float4x4 relativeTransform4x4 = SecondaryTransforms[dispatchThreadId.x].Unpack();
    
    float4x3 instanceTransform = (float4x3) mul(relativeTransform4x4, worldTransform4x4);
    
    TlasInstances[dispatchThreadId.x].Transform = float3x4(
        float4(instanceTransform._11_41_32_23),
        float4(instanceTransform._21_12_42_33),
        float4(instanceTransform._31_22_13_43));
}