cbuffer Constants : register(b0)
{
    uint2 SourceMipSize;
};

Texture2D<float> SourceMip : register(t0);

RWTexture2D<float> Mip1 : register(u0);
RWTexture2D<float> Mip2 : register(u1);
RWTexture2D<float> Mip3 : register(u2);
RWTexture2D<float> Mip4 : register(u3);
RWTexture2D<float> Mip5 : register(u4);

SamplerState PointSampler : register(s1);

#define NUM_THREADS_XY 16
#define MAX_MIP 5 // # of mip uavs
#define TOTAL_THREAD_COUNT (NUM_THREADS_XY * NUM_THREADS_XY)

groupshared float groupResultsFlat[NUM_THREADS_XY * NUM_THREADS_XY];
groupshared uint groupResultsFlatUint[NUM_THREADS_XY * NUM_THREADS_XY];

bool isodd(uint x)
{
    return (x & 1) == 1;
}

bool2 isodd(uint2 x)
{
    return (x & 1) == 1;
}

// input must be 65535 or less (uint16)
// it's uint32 here because 16 bit scalars are not available in this version of hlsl
uint2 DecodeMortonIndex(const uint mortonCode)
{
    uint t = (mortonCode & 0x5555) | ((mortonCode & 0xAAAA) << 15);
    t = (t ^ (t >> 1)) & 0x33333333;
    t = (t ^ (t >> 2)) & 0x0F0F0F0F;
    t = (t ^ (t >> 4));
    
    return uint2(t, t >> 16) & 0xFF;
}

[numthreads(NUM_THREADS_XY, NUM_THREADS_XY, 1)]
void cs_swizzle(const uint groupThreadIndex : SV_GroupIndex, const uint3 groupId : SV_GroupID)
{
    const uint2 groupOriginPixelPos = groupId.xy * NUM_THREADS_XY;
    const uint2 swizzledThreadId = DecodeMortonIndex(groupThreadIndex);
    const uint2 swizzledPixelPos = groupOriginPixelPos + swizzledThreadId;
    
    const uint2 srcPos = swizzledPixelPos * 2;
    const float depth0 = SourceMip[srcPos];
    const float depth1 = SourceMip[srcPos + uint2(1, 0)];
    const float depth2 = SourceMip[srcPos + uint2(0, 1)];
    const float depth3 = SourceMip[srcPos + uint2(1, 1)];
        
    const float result = max(max(depth0, depth1), max(depth2, depth3));
    Mip1[swizzledPixelPos] = result;
}

#define USE_ATOMIC_MAX 0

[numthreads(NUM_THREADS_XY, NUM_THREADS_XY, 1)]
void cs_swizzle_faster(const uint groupThreadIndex : SV_GroupIndex, const uint3 groupId : SV_GroupID)
{
    const uint2 groupOriginPixelPos = groupId.xy * NUM_THREADS_XY;
    const uint2 swizzledThreadId = DecodeMortonIndex(groupThreadIndex);
    
#if USE_ATOMIC_MAX
    groupResultsFlatUint[groupThreadIndex] = 0;
#endif
    
    // initial downsampling (Src -> Mip1)
    {
        const uint2 swizzledPixelPos = groupOriginPixelPos + swizzledThreadId;
        const uint2 srcPos = swizzledPixelPos * 2;
        const float depth0 = SourceMip[srcPos];
        const float depth1 = SourceMip[srcPos + uint2(1, 0)];
        const float depth2 = SourceMip[srcPos + uint2(0, 1)];
        const float depth3 = SourceMip[srcPos + uint2(1, 1)];
        
        const float maxDepth = max(max(depth0, depth1), max(depth2, depth3));
#if !USE_ATOMIC_MAX
        groupResultsFlat[groupThreadIndex] = maxDepth;
#else
        InterlockedMax(groupResultsFlatUint[(groupThreadIndex / 4) * 4], asuint(maxDepth));
#endif
        Mip1[swizzledPixelPos] = maxDepth;
    }
    
    // for use in the loop
    RWTexture2D<float> mips[MAX_MIP - 1] =
    {
        Mip2,
        Mip3,
        Mip4,
        Mip5,
    };
    
    [unroll(MAX_MIP - 1)]
#if !USE_ATOMIC_MAX
    for (uint mip = 1, ldsIndex = groupThreadIndex * 4, offsetInterval = 1;
        mip < MAX_MIP;
        mip++, ldsIndex *= 4, offsetInterval *= 4)
#else
    for (uint mip = 1, ldsIndex = groupThreadIndex * 4, offsetInterval = 16;
        mip < MAX_MIP;
        mip++, ldsIndex *= 4, offsetInterval *= 4)
#endif
    {
        GroupMemoryBarrierWithGroupSync(); // groupsync is important. non-synced barrier causes weird lds race conditions.
        
        const bool threadActive = ldsIndex < TOTAL_THREAD_COUNT;
        [branch]
        if (threadActive)
        {
#if !USE_ATOMIC_MAX
            const float depth0 = groupResultsFlat[ldsIndex];
            const float depth1 = groupResultsFlat[ldsIndex + offsetInterval];
            const float depth2 = groupResultsFlat[ldsIndex + offsetInterval * 2];
            const float depth3 = groupResultsFlat[ldsIndex + offsetInterval * 3];
            
            const float maxDepth = max(max(depth0, depth1), max(depth2, depth3));
            groupResultsFlat[ldsIndex] = maxDepth;
#else
            const float maxDepth = asfloat(groupResultsFlatUint[ldsIndex]);
            InterlockedMax(groupResultsFlatUint[(ldsIndex / offsetInterval) * offsetInterval], asuint(maxDepth));
#endif
            mips[mip - 1][(groupOriginPixelPos / (1 << mip)) + swizzledThreadId] = maxDepth;
        }
    }
}

[numthreads(NUM_THREADS_XY, NUM_THREADS_XY, 1)]
void cs_stretch_swizzle(const uint groupThreadIndex : SV_GroupIndex, const uint3 groupId : SV_GroupID)
{
    const uint2 groupOriginPixelPos = groupId.xy * NUM_THREADS_XY;
    const uint2 swizzledThreadId = DecodeMortonIndex(groupThreadIndex);
    const uint2 swizzledPixelPos = groupOriginPixelPos + swizzledThreadId;
    
    const uint2 srcPos = swizzledPixelPos * 2;
    const float depth0 = SourceMip[srcPos];
    const float depth1 = SourceMip[srcPos + uint2(1, 0)];
    const float depth2 = SourceMip[srcPos + uint2(0, 1)];
    const float depth3 = SourceMip[srcPos + uint2(1, 1)];
    
    float maxDepth = max(max(depth0, depth1), max(depth2, depth3));
    
    const bool2 isSourceMipSizeOdd = isodd(SourceMipSize);
    
    [branch]
    if (isSourceMipSizeOdd.x)
    {
        maxDepth = max(maxDepth, SourceMip[srcPos + uint2(2, 0)]);
        maxDepth = max(maxDepth, SourceMip[srcPos + uint2(2, 1)]);
        
        [branch]
        if (isSourceMipSizeOdd.y)
        {
            maxDepth = max(maxDepth, SourceMip[srcPos + uint2(2, 2)]);
        }
    }
    
    [branch]
    if (isSourceMipSizeOdd.y)
    {
        maxDepth = max(maxDepth, SourceMip[srcPos + uint2(0, 2)]);
        maxDepth = max(maxDepth, SourceMip[srcPos + uint2(1, 2)]);
    }
    
    Mip1[swizzledPixelPos] = maxDepth;
}
