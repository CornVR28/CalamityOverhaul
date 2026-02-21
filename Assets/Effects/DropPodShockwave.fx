float globalTime;
float shockwaveIntensity;
float ringRadius;    // 0~1 当前环半径占比
float ringThickness; // 环的厚度
float squishY;       // Y轴压缩比 <1 产生透视椭圆

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

struct PixelInput
{
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

float4 PixelShaderFunction(PixelInput input) : COLOR0
{
    float2 uv = input.TexCoords;
    
    // 将UV映射到 -1~1 的空间，Y方向反向补偿透视压缩
    float2 centered = uv * 2.0 - 1.0;
    centered.y /= squishY;
    float dist = length(centered);
    
    // 角度用于噪声采样
    float angle = atan2(centered.y, centered.x);
    float normalizedAngle = (angle + 3.14159) / 6.28318;
    
    // 环形遮罩——以ringRadius为中心，ringThickness为宽度
    float ringDist = abs(dist - ringRadius);
    float ringMask = 1.0 - smoothstep(0.0, ringThickness, ringDist);
    
    // 噪声扰动——沿环周方向采样，制造不均匀的压缩气流感
    float noise1 = tex2D(noiseTex, float2(normalizedAngle * 3.0 + globalTime * 2.0, dist * 2.0)).r;
    float noise2 = tex2D(noiseTex, float2(normalizedAngle * 5.0 - globalTime * 1.5, dist * 1.5 + 0.3)).r;
    float combinedNoise = (noise1 + noise2) * 0.5;
    
    // 环内侧（压缩面）更亮，外侧渐隐
    float innerBias = smoothstep(ringRadius, ringRadius - ringThickness * 0.5, dist);
    
    // 最终亮度
    float brightness = ringMask * combinedNoise * shockwaveIntensity;
    
    // 白热核心——环的最内侧边缘最亮
    float coreBrightness = ringMask * innerBias * shockwaveIntensity * 1.5;
    
    // 合并基础颜色
    float4 baseColor = input.Color * brightness * 1.6;
    
    // 添加白热高光
    float4 coreColor = float4(1.0, 0.95, 0.85, 1.0) * coreBrightness * baseColor.a;
    
    return baseColor + coreColor;
}

technique Technique1
{
    pass DropPodShockwavePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
