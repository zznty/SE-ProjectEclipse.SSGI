#ifndef LIB_COLOR
#define LIB_COLOR

float3 rgb_to_srgb(float3 rgb)
{
    return (rgb <= 0.0031308) ? rgb * 12.92 : (pow(abs(rgb), 0.4166666667) * 1.055 - 0.055);
}

float3 srgb_to_rgb(float3 srgb)
{
    return (srgb <= 0.04045) ? srgb * 0.0773993808 : pow((abs(srgb) + 0.055) * 0.9478672986, 2.4);
}

float3 hsv_to_rgb(float3 hsv)
{
    float4 K = float4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0f - K.www);
    return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
}

float3 rgb_to_hsv(float3 rgb)
{
    float4 K = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
    float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 hsv_offset_to_hsv(float3 hsvOffset)
{
    return saturate(float3(hsvOffset.x, hsvOffset.y + 0.8, hsvOffset.z + 0.45));
}

float3 hsv_offset_to_rgb(float3 hsvOffset)
{
    return hsv_to_rgb(saturate(float3(hsvOffset.x, hsvOffset.y + 0.8, hsvOffset.z + 0.45)));
}

float RemoveMetalnessFromColoring(float metalness, float coloring)
{
    const float threshold = 0.4;
    const float thresholdMultiply = 0.5;
    return coloring * saturate(1.0 - saturate(metalness - threshold) / ((1.0 - threshold) * thresholdMultiply));
}

float3 ColorizeGray(float3 baseColor, float3 coloringHSV, float coloringFactor)
{
    static const float3 magic_values = float3(0, 204.0 / 255.0, 0); // is required
    
    if (coloringFactor > 0)
    {
        coloringHSV += magic_values;
        
        float3 hsv = rgb_to_hsv(rgb_to_srgb(baseColor));
        hsv.xy = 0;
        float3 finalHsv = saturate(hsv + coloringHSV);

        baseColor = lerp(baseColor, srgb_to_rgb(hsv_to_rgb(finalHsv)), coloringFactor);
    }
    return baseColor;
}

float3 ColorShift(float3 rgb, float3 hsv_shift, float rate)
{
    float3 shiftedHSV = rgb_to_hsv(rgb) + hsv_shift;
	
    shiftedHSV.x = shiftedHSV.x % 1.0f;

    if (shiftedHSV.x < 0) 
        shiftedHSV.x += 1.0f;

    shiftedHSV.y = clamp(shiftedHSV.y, 0, 1.0f);
    shiftedHSV.z = clamp(shiftedHSV.z, 0, 1.0f);

    return lerp(rgb, hsv_to_rgb(shiftedHSV), rate);
}

float luminance(float3 color)
{
    return dot(color, float3(0.2126f, 0.7152f, 0.0722f));
}

float4 unpack_color_shift_hsv(int4 colorshift)
{
    int hue = (colorshift.w << 8) | (0xFF & colorshift.z);
    return float4(hue, colorshift.y, colorshift.x, 0) / float4(360, 100, 100, 1);
}

#endif
