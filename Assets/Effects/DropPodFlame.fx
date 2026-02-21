matrix transformMatrix;
float globalTime;
float heatIntensity;

texture uNoise;
sampler2D noiseTex = sampler_state
{
    texture = <uNoise>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

struct VertexShaderInput
{
    float4 Position : POSITION;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Color = input.Color;
    output.TexCoords = input.TexCoords;
    output.Position = mul(input.Position, transformMatrix);
    return output;
}

float QuadraticBump(float x)
{
    return x * (4.0 - x * 4.0);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TexCoords;

    // 噪声采样，双层不同频率叠加模拟火焰翻滚
    float noise1 = tex2D(noiseTex, coords * float2(1.0, 1.5) + float2(globalTime * -3.6, 0.0)).r;
    float noise2 = tex2D(noiseTex, coords * float2(0.8, 2.0) + float2(globalTime * -4.8, 0.3)).r;
    float combinedNoise = (noise1 + noise2) * 0.5;

    // 边缘衰减：Y方向抛物线，中间最亮两侧渐隐
    float edgeFade = QuadraticBump(coords.y);

    // X方向衰减：越远离起点越暗，模拟火焰尾部消散
    float tailFade = pow(saturate(1.0 - coords.x), 1.5);

    // 白热核心：根部中央最强
    float coreIntensity = pow(edgeFade * tailFade, 3.0) * combinedNoise;
    float whiteHotBrightness = coreIntensity * lerp(0.8, 3.5, heatIntensity);

    // 基础火焰颜色
    float4 baseColor = input.Color * edgeFade * tailFade * 1.8;

    // 颜色扰动：减蓝绿让火焰偏橙红
    float distortion = tex2D(noiseTex, coords * float2(0.6, 1.8) + float2(globalTime * -2.4, 0.2)).r;
    baseColor.rgb -= float3(0.02, 0.08, 0.15) * distortion * baseColor.a;

    // 最终：基础颜色 + 白热高光
    return baseColor + whiteHotBrightness * baseColor.a;
}

technique Technique1
{
    pass DropPodFlamePass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
