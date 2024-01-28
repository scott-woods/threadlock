Texture2D tex;
sampler textureSampler = sampler_state
{
    Texture = <tex>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};
float2 textureSize;

float4 texturePointSmooth(float2 uv)
{
    float2 pixel = 1.0 / textureSize;
    uv -= pixel * 0.5;
    float2 uv_pixels = uv * textureSize;
    float2 delta_pixel = frac(uv_pixels) - 0.5;

    float2 ddxy = abs(ddx(uv_pixels)) + abs(ddy(uv_pixels));

    float2 mip = log2(ddxy) - 0.5;
    mip = clamp(mip, 0.0, 1.0); // ps_3_0 does not support min for float2 directly

    float lod = min(mip.x, mip.y);
    
    // SampleLevel is not available in ps_3_0, use Tex2Dlod instead
    float4 result = tex2Dlod(textureSampler, float4(uv + (clamp(delta_pixel / ddxy, 0.0, 1.0) - delta_pixel) * pixel, 0, lod));
    return result;
}

float4 MainPS(float2 UV : TEXCOORD0) : SV_Target
{
    float4 Texture = texturePointSmooth(UV);
    return Texture.rgba;
}

technique JitterFree
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
};