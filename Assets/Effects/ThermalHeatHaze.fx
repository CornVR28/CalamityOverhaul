// ============================================================================
// ThermalHeatHaze.fx — 热能发电机局部热浪扭曲后处理 (ps_3_0)
// 以发电机为中心的小范围上升热浪 · 噪声驱动UV偏移 · 暖色调偏移
// 全屏四边形渲染，采样已绘制完的场景纹理
// ============================================================================

sampler2D screenTex : register(s0);

#define MAX_SOURCES 8

float2 screenSize;                     // 屏幕尺寸（像素）
float2 hazeCenters[MAX_SOURCES];       // 各热源中心（归一化屏幕坐标 0~1）
float  hazeIntensities[MAX_SOURCES];   // 各热源扭曲强度 0~1
int    sourceCount;                    // 当前活跃热源数量
float  globalTime;                     // 动画时间

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

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    if (sourceCount <= 0)
        return tex2D(screenTex, coords);

    float aspect = screenSize.x / screenSize.y;

    // ---- 双层噪声（与热源位置无关，只采样一次） ----
    float2 nUV1 = coords * float2(6.0, 14.0) + float2(globalTime * 0.25, -globalTime * 1.2);
    float wave1 = tex2D(noiseTex, nUV1).r - 0.5;

    float2 nUV2 = coords * float2(14.0, 28.0) + float2(-globalTime * 0.4, -globalTime * 2.5);
    float wave2 = tex2D(noiseTex, nUV2).g - 0.5;

    float combined = wave1 * 0.65 + wave2 * 0.35;

    // ---- 累加所有热源的贡献 ----
    float2 totalOffset = float2(0, 0);
    float totalWarmth = 0;
    float totalFlicker = 0;

    for (int i = 0; i < sourceCount; i++) {
        float intensity = hazeIntensities[i];
        if (intensity < 0.005)
            continue;

        float2 center = hazeCenters[i];
        float2 delta = coords - center;
        float2 corrected = float2(delta.x * aspect, delta.y);
        float dist = length(corrected);

        // 上升偏置——热浪只往上走
        float upBias = saturate(1.0 - (coords.y - center.y) * 3.0);
        upBias = upBias * upBias;

        // 水平展宽
        float horizontalSpread = saturate(1.0 - abs(delta.x) * aspect * 5.0);
        horizontalSpread = horizontalSpread * horizontalSpread;
        upBias *= lerp(0.4, 1.0, horizontalSpread);

        // 径向衰减
        float radius = 0.06 + intensity * 0.14;
        float falloff = exp(-dist * dist / (radius * radius + 0.001)) * upBias;

        // 累加UV偏移
        float strength = intensity * 0.018;
        totalOffset += float2(
            combined * strength * falloff * 0.35,
            combined * strength * falloff
        );

        totalWarmth += falloff * intensity * 0.12;
        totalFlicker += wave1 * 0.06 * falloff * intensity;
    }

    // 防止多源叠加过度
    totalOffset = clamp(totalOffset, -0.04, 0.04);
    totalWarmth = min(totalWarmth, 0.25);
    totalFlicker = clamp(totalFlicker, -0.08, 0.08);

    // 无贡献直接返回
    if (abs(totalOffset.x) + abs(totalOffset.y) < 0.0001)
        return tex2D(screenTex, coords);

    // ---- 采样扭曲后的屏幕颜色 ----
    float2 distortedUV = clamp(coords + totalOffset, 0.001, 0.999);
    float4 color = tex2D(screenTex, distortedUV);

    // ---- 暖色调偏移 ----
    color.r += totalWarmth * 0.5;
    color.g += totalWarmth * 0.2;
    color.b -= totalWarmth * 0.1;

    // ---- 热浪明暗闪烁 ----
    color.rgb += totalFlicker;

    return color;
}

technique Technique1
{
    pass ThermalHazePass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
