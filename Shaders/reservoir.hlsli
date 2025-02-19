#ifndef LIB_RESERVOIR
#define LIB_RESERVOIR

#include "random.hlsli"
#include "lightsample.hlsli"

struct Reservoir
{
    LightSample Sample;
    
    // while streaming: total weights of all encountered samples
    // after normalization: weight of the selected sample
    float WeightSum;
    
    // # of samples this reservoir has seen so far
    // (when merging 2 reservoirs, newRes.M = res1.M + res2.M)
    uint M;
    
    // store visibility for reuse
    bool Visibility;
    
    void Init()
    {
        Sample.InitInvalid();
        WeightSum = 0;
        M = 0;
        Visibility = false;
    }
   
#define IGNORE_INVALID true
    
    bool AddSample(LightSample s, float w, inout uint rand)
    {
#if IGNORE_INVALID
        if (!s.IsValid())
            return false;
#endif
        WeightSum += w;
        M++;
        bool selected = NextFloat(rand) < w / WeightSum;
        if (selected)
        {
            Sample = s;
        }
        return selected;
    }
    
    bool Merge(Reservoir res, float w, inout uint rand)
    {
#if IGNORE_INVALID
        if (!res.Sample.IsValid())
            return false;
#endif
        WeightSum += w;
        M += res.M;
        bool selected = NextFloat(rand) < w / WeightSum;
        if (selected)
        {
            Sample = res.Sample;
        }
        return selected;
    }
    
    // https://research.nvidia.com/sites/default/files/pubs/2020-07_Spatiotemporal-reservoir-resampling/ReSTIR.pdf#equation.2.6
    // (1 / targetPdf) * (1 / M) * wSum
    // => (1 / (targetPdf * M)) * wSum
    // => wSum / (targetPdf * M)
    void Normalize(float candidateTargetPdf, uint M, float weightMultiplier = 1.0)
    {
        float divisor = candidateTargetPdf * M;
        WeightSum = divisor == 0 ? 0 : (WeightSum * weightMultiplier) / divisor;
    }
};

Reservoir CreateEmptyReservoir()
{
    Reservoir res;
    res.Sample.InitInvalid();
    res.WeightSum = 0;
    res.M = 0;
    res.Visibility = false;
    return res;
}

#endif
