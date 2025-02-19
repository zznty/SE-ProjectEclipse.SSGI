#ifndef LIB_SAMPLERS
#define LIB_SAMPLERS

SamplerState DefaultSampler        : register(s0);
// point
// linear
SamplerState TextureSampler        : register(s3);
// shadowmap
SamplerState AlphamaskSampler      : register(s5);
SamplerState TextureSamplerAniso4x : register(s10);

#define AlphamaskSampler TextureSampler // keen uses TextureSampler for sampling alphamask textures... why does AlphamaskSampler even exist then??

#endif
