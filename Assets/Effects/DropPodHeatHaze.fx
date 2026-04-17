// ============================================================================
// DropPodHeatHaze.fx — 空降仓屏幕空间热浪扭曲后处理 (ps_3_0)
// 以仓体为中心的径向热浪波纹 · 噪声驱动UV偏移 · 大气闪烁
// 全屏四边形渲染，采样已绘制完的场景纹理
// ============================================================================

sampler2D screenTex : register(s0); // 当前场景画面

float2 screenSize;       // 屏幕尺寸（像素）
float2 hazeCenter;       // 热源中心（归一化屏幕坐标 0~1）
float  hazeIntensity;    // 扭曲强度 0~1
float  globalTime;       // 动画时间

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
    // 如果强度极低直接返回原色，避免不必要的采样
    if (hazeIntensity < 0.005)
        return tex2D(screenTex, coords);

    // ---- 从热源中心到当前像素的方向和距离 ----
    float2 delta = coords - hazeCenter;
    // 修正宽高比，使热浪以圆形扩散
    float aspect = screenSize.x / screenSize.y;
    float2 corrected = float2(delta.x * aspect, delta.y);
    float dist = length(corrected);

    // ---- 衰减范围——近处强、远处弱 ----
    // 有效半径约 0.6 屏幕对角线，偏上方（火焰区域）更强
    float verticalBias = saturate(1.0 - (coords.y - hazeCenter.y) * 1.5);
    verticalBias = lerp(0.3, 1.0, verticalBias);
    float falloff = exp(-dist * dist / (0.15 * hazeIntensity + 0.02)) * verticalBias;

    // ---- 多层噪声驱动的波纹 ----
    // 低频大波纹
    float2 noiseUV1 = coords * 3.0 + float2(globalTime * 0.8, globalTime * 0.5);
    float wave1 = tex2D(noiseTex, noiseUV1).r - 0.5;

    // 中频热浪
    float2 noiseUV2 = coords * 6.0 + float2(-globalTime * 1.2, globalTime * 0.9);
    float wave2 = tex2D(noiseTex, noiseUV2).g - 0.5;

    // 高频闪烁
    float2 noiseUV3 = coords * 12.0 + float2(globalTime * 2.0, -globalTime * 0.7);
    float wave3 = tex2D(noiseTex, noiseUV3).r - 0.5;

    float combinedWave = wave1 * 0.5 + wave2 * 0.35 + wave3 * 0.15;

    // ---- 径向波纹——以热源为中心的同心环状闪烁 ----
    float radialWave = sin(dist * 40.0 - globalTime * 6.0) * 0.3;
    radialWave *= exp(-dist * 5.0); // 径向波快速衰减

    // ---- UV偏移计算 ----
    float distortionStrength = hazeIntensity * 0.025;
    float2 offset = float2(
        (combinedWave + radialWave) * distortionStrength * falloff,
        (combinedWave * 0.7 + radialWave * 0.5) * distortionStrength * falloff
    );

    // 偏移方向叠加径向分量——热浪沿径向扩散
    float2 radialDir = normalize(delta + 0.0001);
    offset += radialDir * combinedWave * distortionStrength * falloff * 0.3;

    // ---- 采样扭曲后的屏幕颜色 ----
    float2 distortedUV = coords + offset;
    // 钳制UV防止采样出界
    distortedUV = clamp(distortedUV, 0.001, 0.999);
    float4 color = tex2D(screenTex, distortedUV);

    // ---- 微弱的热浪色调偏移——近处略偏暖 ----
    float warmShift = falloff * hazeIntensity * 0.08;
    color.r += warmShift * 0.5;
    color.g += warmShift * 0.2;
    color.b -= warmShift * 0.1;

    return color;
}

technique Technique1
{
    pass DropPodHeatHazePass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
