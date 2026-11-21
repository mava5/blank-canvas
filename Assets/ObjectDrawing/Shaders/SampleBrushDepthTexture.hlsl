
#ifndef SAMPLE_BRUSH_DEPTH_INCLUDED
#define SAMPLE_BRUSH_DEPTH_INCLUDED

void SampleBrushDepth_float(
    float2 UV,
    UnityTexture2D DepthTexture,
    UnitySamplerState Sampler,
    float IsOrthographic,
    float ZNear,
    float ZFar,
    out float LinearDepth,
    out float RawDepth)
{
    RawDepth = DepthTexture.Sample(Sampler, UV).r;
    
    if (IsOrthographic > 0.5f)
    {
        LinearDepth = RawDepth;
    }
    else
    {
        RawDepth = 1.0 - RawDepth;
        float linearEye = (ZNear * ZFar) / (ZFar - RawDepth * (ZFar - ZNear));
        LinearDepth = saturate(1.0 - (linearEye - ZNear) / (ZFar - ZNear));
    }
}

void SampleBrushDepth_half(
    float2 UV,
    UnityTexture2D DepthTexture,
    UnitySamplerState Sampler,
    float IsOrthographic,
    float ZNear,
    float ZFar,
    out float LinearDepth,
    out float RawDepth)
{
    RawDepth = DepthTexture.Sample(Sampler, UV).r;
    
    if (IsOrthographic > 0.5f)
    {
        LinearDepth = RawDepth;
    }
    else
    {
        RawDepth = 1.0 - RawDepth;
        float linearEye = (ZNear * ZFar) / (ZFar - RawDepth * (ZFar - ZNear));
        LinearDepth = saturate(1.0 - (linearEye - ZNear) / (ZFar - ZNear));
    }
}

#endif
