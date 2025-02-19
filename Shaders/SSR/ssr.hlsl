#include "../math.hlsli"
#include "brdf_ggx.hlsli"
#include "common.hlsli"
#include "trace_hiz.hlsli"
#include "brdf_unity.hlsli"

bool TraceSSRayNaive(float2 uvStart, SSRInput input, float3 rayDir, out float2 uvHit)
{
    // use naive ray marching:
    // move (1 / maxsteps) * maxWorldDist * rayDir * viewdist
    // transform to uv and check if raydepth > pixeldepth (0 to view dist)
    // break and return if depthDiff < maxThickness
    
    static const float MAX_DIST = 50; // world distance meters
    static const uint MAX_STEPS = 1500;
    static const float MAX_THICKNESS = MAX_DIST / MAX_STEPS * 2;
    const float2 FRAME_SIZE = GetScreenSize();
    
    float3 rayStepInView = rayDir * MAX_DIST / MAX_STEPS;
    float3 rayPosInView = input.PositionView;
    
    int hitIndex = -1;
    bool texPosValid = true;
    for (uint i = 0; i < MAX_STEPS && texPosValid; i++)
    {
        float3 texPos = ViewToTexAndLinearDepth(rayPosInView);
        texPosValid = all(saturate(texPos.xy) == texPos.xy);
        
        float pixelDepth = compute_depth(DepthBuffer.SampleLevel(PointSampler, texPos.xy, 0));
        float depthDiff = texPos.z - pixelDepth;
        [branch]
        if (depthDiff >= 0 && depthDiff < MAX_THICKNESS)
        {
            float3 normal = LoadNormalInViewSpace(texPos.xy);
            if (dot(normal, -rayDir) > 0)
            {
                hitIndex = i;
                break;
            }
        }
        
        rayPosInView += rayStepInView;
    }
    
    uvHit = ViewToTex(rayPosInView);
    return hitIndex != -1;
}

#define lengthSq(x) dot(x, x)
#define distanceSq(x, y) lengthSq(x - y)
#define diff(x, y) abs(x - y)

static const float MAX_DIST = 50; // world distance meters
static const uint MAX_STEPS = 500;
static const float MAX_THICKNESS = MAX_DIST / MAX_STEPS * 1.5;

void swap(inout float x, inout float y)
{
    float temp = x;
    x = y;
    y = temp;
}

float invlerp(float x, float y, float v)
{
    return (v - x) / (y - x);
}

float2 invlerp(float2 x, float2 y, float2 v)
{
    return (v - x) / (y - x);
}

bool TraceSSRayBinarySearch(float2 uvStart, SSRInput input, float3 rayDir, out float2 uvHit)
{
    const float nearPlane = ProjMatrix._43;
    const float reflect_dot_camera = dot(rayDir, compute_screen_ray(0.5));
    const uint2 frameSize = ScreenSize;
    const float stepRate = 1.0;
    
    const float rayLength = (input.PositionView.z + rayDir.z * MAX_DIST) > nearPlane ? (nearPlane - -input.PositionView.z) / -rayDir.z : MAX_DIST;
    const float3 rayStartView = input.PositionView + input.NormalView * 0.05;
    const float3 rayEndView = rayStartView + rayDir * rayLength;
    
    const float4 rayStartClip = mul(float4(rayStartView, 1), ProjMatrix); // h0
    const float4 rayEndClip = mul(float4(rayEndView, 1), ProjMatrix); // h1
    
    float k0 = 1.0 / rayStartClip.w; // perspective divisor's reciprocal (1 / zDepth)
    float k1 = 1.0 / rayEndClip.w;
    
    float q0 = rayStartClip.z;
    float q1 = rayEndClip.z;
    
    float2 p0 = (rayStartClip.xy * k0 * float2(0.5, -0.5)) + 0.5; // could just use uvStart
    float2 p1 = (rayEndClip.xy * k1 * float2(0.5, -0.5)) + 0.5;
    
    // clamp end pos to [0, 1] if it's outside
    bool2 oob = saturate(p1) != p1;
    if (any(oob))
    {
        float2 alpha = oob ? ((p1 - ((p1 > 1) ? 1 : 0)) / (p1 - p0)) : 0;
        float alphaMax = max(alpha.x, alpha.y);
        
        k1 = lerp(k1, k0, alphaMax);
        q1 = lerp(q1, q0, alphaMax);
        p1 = lerp(p1, p0, alphaMax);
    }
    
    p0 *= frameSize;
    p1 *= frameSize;
    
    p1 = (distanceSq(p0, p1) < (0.01 * 0.01)) ? p0 + 0.01 : p1;
    
    float2 delta = p1 - p0;
    
    bool isRayVertical = false;
    if (abs(delta.x) < abs(delta.y))
    {
        isRayVertical = true;
        delta = delta.yx;
    }
    
    const uint maxSteps = min(ceil(length(delta)), max(frameSize.x, frameSize.y)) / stepRate;
    const float maxThickness = max(abs(compute_depth(q1 * k1) - -rayStartView.z) / maxSteps, 0.03);
    
    float stepDirection = sign(delta.x);
    float invdx = stepDirection / delta.x;
    
    float dk = (k1 - k0) * invdx;
    float dq = (q1 - q0) * invdx;
    float2 dp = float2(stepDirection, invdx * delta.y);
    const float dko = dk;
    const float dqo = dq;
    const float2 dpo = dp;
    
    delta = isRayVertical ? delta.yx : delta;
    dp = isRayVertical ? dp.yx : dp;
    
    dk *= stepRate;
    dq *= stepRate;
    dp *= stepRate;
    
    float k = k0;
    float q = q0;
    float2 p = p0;
    
    const float3 viewRay = compute_screen_ray(0.5);
    
    bool hit = false;
    float depthDiff;
    float pixelDepth;
    for (uint i = 0; i < maxSteps; i++)
    {
        k += dk;
        q += dq;
        p += dp;
        
        float rawPixelDepth = DepthBuffer[p];
        pixelDepth = compute_depth(rawPixelDepth);
        
        float rayDepth = compute_depth(q * k);
        depthDiff = rayDepth - pixelDepth;
        
        if (depthDiff > 0)
        {
            //float3 normal = LoadNormalInViewSpacePixel(p);
            //if (dot(normal, -input.RayReflectDirView) > 0)
            {
                //hit = true;
                break;
            }
        }
    }
    
    for (i = 0; i < 10 && all(clamp(p, 0, frameSize) == p); i++)
    {
        dk *= 0.5;
        dq *= 0.5;
        dp *= 0.5;
        
        k = k - dk * sign(depthDiff);
        q = q - dq * sign(depthDiff);
        p = p - dp * sign(depthDiff);
        
        pixelDepth = compute_depth(DepthBuffer[p]);
        float rayDepth = compute_depth(q * k);
        depthDiff = rayDepth - pixelDepth;
    
        float3 normal = LoadNormalInViewSpacePixel(p);
    }
    
    if (abs(depthDiff) < 0.1)
    {
        hit = true;
    }
    
    uvHit = round(p) / frameSize;
    return hit;
}

float3 main(float4 position : SV_Position, float2 uv : TEXCOORD0) : SV_Target0
{
    const uint2 pixelPos = uint2(position.xy);
    float3 pixelColor = FrameBuffer[pixelPos];
    SSRInput input = LoadSSRInput(pixelPos);
    
#if REFLECTION_ONLY
    pixelColor = 0;
    input.Color = 1;
#endif
    
    [branch]
    if (!input.IsForeground /*|| input.Gloss < REFLECT_GLOSS_THRESHOLD*/)
    {
        return pixelColor;
    }
    
    float3 reflectDir = reflect(input.RayDirView, input.NormalView);
    
    float2 uvHit;
    float hitConfidence = TraceRayHiZ(uv, input, reflectDir, uvHit);
    
    if (hitConfidence != 0)
    {
        const float3 hitColor = FrameBuffer.SampleLevel(LinearSampler, uvHit, 0).xyz;
        const float3 brdfSpecular = Brdf(1 - input.Gloss, input.Metalness, input.Color, -input.RayDirView, reflectDir, input.NormalView);
        const float pdf = GetGGXMirrorPdf(input);
        pixelColor += hitColor * hitConfidence * (brdfSpecular / pdf) * IndirectLightMulti;
    }
    
    return pixelColor;
}
