sampler noiseTexture : register(s1);

float globalTime;
float heatIntensity;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float QuadraticBump(float x)
{
    return x * (4.0 - x * 4.0);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates.xy;

    // 修正纹理畸变
    coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;

    // 噪声采样，沿X轴滚动模拟火焰向下流动
    float noise1 = tex2D(noiseTexture, coords * float2(1.0, 1.5) + float2(globalTime * -3.6, 0.0));
    float noise2 = tex2D(noiseTexture, coords * float2(0.8, 2.0) + float2(globalTime * -4.8, 0.3));
    float combinedNoise = (noise1 + noise2) * 0.5;

    // 边缘衰减：Y方向使用抛物线，中间最亮两侧渐隐
    float edgeFade = QuadraticBump(coords.y);

    // X方向衰减：越远离起点(x=0)越暗，模拟火焰尾部消散
    float tailFade = pow(1.0 - coords.x, 1.5);

    // 白热核心：在火焰根部(x接近0)且中央(y接近0.5)最强
    float coreIntensity = pow(edgeFade * tailFade, 3.0) * combinedNoise;
    float whiteHotBrightness = coreIntensity * lerp(0.8, 3.5, heatIntensity);

    // 基础火焰颜色，由顶点颜色和边缘衰减决定
    float4 baseColor = input.Color * edgeFade * tailFade * 1.8;

    // 添加颜色扰动：减去一些蓝绿分量，让火焰偏橙红
    float distortion = tex2D(noiseTexture, coords * float2(0.6, 1.8) + float2(globalTime * -2.4, 0.2));
    baseColor.rgb -= float3(0.02, 0.08, 0.15) * distortion * baseColor.a;

    // 最终输出：基础颜色 + 白热高光
    return baseColor + whiteHotBrightness * baseColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
