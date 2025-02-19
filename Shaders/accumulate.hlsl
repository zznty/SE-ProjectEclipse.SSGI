cbuffer Constants : register(b0)
{
    uint FrameIndex;
}

Texture2D historyTexture : register(t0);
Texture2D currentFrame : register(t1);

void main(float4 position : SV_Position, out float4 target1 : SV_Target0, out float4 target2 : SV_Target1)
{
    //if (FrameIndex > 20)
    //{
    //    target1 = historyTexture[position.xy];
    //    target2 = target1;
    //    return;
    //}
    
    const float weight = 1.0 / float(FrameIndex + 1);
    const float4 history = FrameIndex == 0 ? 0 : (historyTexture[position.xy] * (1 - weight));
    const float4 color = currentFrame[position.xy] * weight;
    
    target1 = history + color;
    target2 = target1;
}
