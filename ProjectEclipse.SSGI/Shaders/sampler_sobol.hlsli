#ifndef SAMPLER_SOBOL
#define SAMPLER_SOBOL

#include "constants.hlsli"

// sobol quasi-random generator with owen scrambling
// https://www.shadertoy.com/view/sd2Xzm
// https://www.jcgt.org/published/0009/04/01/

struct SobolOwenSampler
{
    static uint2 Sobol(uint index)
    {
        uint2 p = 0u;
        uint2 d = 0x80000000u;

        for (; index != 0u; index >>= 1u)
        {
            if ((index & 1u) != 0u)
            {
                p ^= d;
            }

            d.x >>= 1u;
            d.y ^= d.y >> 1u;
        }
    
        return p;
    }

    static uint OwenHash(uint x, uint seed)
    {
        x ^= x * 0x3d20adeau;
        x += seed;
        x *= (seed >> 16) | 1u;
        x ^= x * 0x05526c56u;
        x ^= x * 0x53a22864u;
        return x;
    }

    static uint2 OwenHash2(uint2 x, uint2 seed)
    {
        x ^= x * 0x3d20adeau;
        x += seed;
        x *= (seed >> 16) | 1u;
        x ^= x * 0x05526c56u;
        x ^= x * 0x53a22864u;
        return x;
    }

    static uint OwenScramble(uint p, uint seed)
    {
        p = reversebits(p);
        p = OwenHash(p, seed);
        return reversebits(p);
    }

    static uint2 OwenScramble2(uint2 p, uint2 seed)
    {
        p = reversebits(p);
        p = OwenHash2(p, seed);
        return reversebits(p);
    }

    uint2 seed;
    uint iteration;
    
    void Init(const uint startIteration, const uint2 rand2)
    {
        seed = rand2;
        iteration = startIteration;
    }
    
    float2 Next()
    {
        uint2 ip = Sobol(iteration);
        iteration++;
        
        //ip.x = OwenScramble(ip.x, seed.x);
        //ip.y = OwenScramble(ip.y, seed.y);
        ip = OwenScramble2(ip, seed);
        
        float2 p = float2(ip) / float(0xffffffffu);
        return p;
    }
};

#endif
