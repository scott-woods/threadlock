sampler s0;
float4 _outlineColor; // 1,1,1,1
float2 _textureSize;


struct VertexShaderOutput
{
    float4 Position : POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};


float4 mainPixel(VertexShaderOutput input) : COLOR
{
    float4 centerColor = tex2D(s0, input.TextureCoordinates);
    
    float2 texSize = 1.0f / _textureSize;
    
    if (centerColor.a == 0.0f)
    {
        bool isEdge = false;
        
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                
                float4 sampleColor = tex2D(s0, input.TextureCoordinates + texSize * float2(x, y));
                if (sampleColor.a != 0.0f)
                {
                    isEdge = true;
                    break;
                }
            }
            if (isEdge)
                break;
        }
        
        if (isEdge)
        {
            return _outlineColor;
        }

    }
    
    return centerColor;
}

technique SpriteOutline
{
    pass P0
    {
        PixelShader = compile ps_3_0 mainPixel();
    }
};