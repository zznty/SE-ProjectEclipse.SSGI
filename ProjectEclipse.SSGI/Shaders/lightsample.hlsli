#ifndef LIB_LIGHTSAMPLE
#define LIB_LIGHTSAMPLE

#include "constants.hlsli"

struct LightSample
{
    uint LightIndex;
    
    // for primitive area lights: position of the sampled point relative to the center of the light primitive
    // NOT just a unit vector!! it also contains distance from light center info
    // this makes reprojection and shifting between different pixels easier, probably.
    
    // for environment lights (sunlight): direction from the surface to the sampled point, IN SUN-LOCAL SPACE!!!!!#@qpf@#$!
    // doesnt change for different surfaces because the light is infinitely far away
    float3 PointDir;
    
    void InitInvalid()
    {
        LightIndex = LIGHT_SAMPLE_INVALID_ID; // not used, just set something
        PointDir = 0; // not used, just set something
    }
    
    bool IsValid()
    {
        return LightIndex != LIGHT_SAMPLE_INVALID_ID;
    }
    
    bool IsSunSample()
    {
        return LightIndex == LIGHT_SAMPLE_SUN_ID;
    }
};

#endif
