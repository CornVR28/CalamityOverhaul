// ============================================================================
// ThermalHeatHaze.fx — 热能发电机局部热浪扭曲后处理 (ps_3_0)
// 以发电机为中心的小范围上升热浪 · 噪声驱动UV偏移 · 暖色调偏移
// 全屏四边形渲染，采样已绘制完的场景纹理
// ============================================================================

sampler2D screenTex : register(s0);

float2 screenSize;       // 屏幕尺寸（像素）
float2 hazeCenter;       // 热源中心（归一化屏幕坐标 0~1）
float  hazeIntensity;    // 扭曲强度 0~1，由温度比决定
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
    // 强度极低直接返回原色
    if (hazeIntensity < 0.005)
        return tex2D(screenTex, coords);

    // ---- 从热源中心到当前像素的方向和距离 ----
    float2 delta = coords - hazeCenter;
    float aspect = screenSize.x / screenSize.y;
    float2 corrected = float2(delta.x * aspect, delta.y);
    float dist = length(corrected);

    // ---- 上升偏置——热浪只往上走，下方几乎无效果 ----
    // coords.y < hazeCenter.y 表示在热源上方
    float upBias = saturate(1.0 - (coords.y - hazeCenter.y) * 5.0);
    upBias = upBias * upBias;

    // 水平方向也有轻微展开，模拟热气柱展宽
    float horizontalSpread = saturate(1.0 - abs(delta.x) * aspect * 12.0);
    horizontalSpread = horizontalSpread * horizontalSpread;
    upBias *= lerp(0.3, 1.0, horizontalSpread);

    // ---- 紧凑的径向衰减 ----
    float radius = 0.03 + hazeIntensity * 0.07;
    float falloff = exp(-dist * dist / (radius * radius + 0.001)) * upBias;

    // ---- 双层噪声驱动的上升波纹 ----
    // 低频：大尺度的热空气翻涌
    float2 nUV1 = coords * float2(6.0, 14.0) + float2(globalTime * 0.25, -globalTime * 1.2);
    float wave1 = tex2D(noiseTex, nUV1).r - 0.5;

    // 高频：细小的闪烁扰动
    float2 nUV2 = coords * float2(14.0, 28.0) + float2(-globalTime * 0.4, -globalTime * 2.5);
    float wave2 = tex2D(noiseTex, nUV2).g - 0.5;

    float combined = wave1 * 0.65 + wave2 * 0.35;

    // ---- UV偏移——以垂直方向为主（热空气上升） ----
    float strength = hazeIntensity * 0.006;
    float2 offset = float2(
        combined * strength * falloff * 0.3,   // 水平微弱
        combined * strength * falloff           // 垂直为主
    );

    // ---- 采样扭曲后的屏幕颜色 ----
    float2 distortedUV = clamp(coords + offset, 0.001, 0.999);
    float4 color = tex2D(screenTex, distortedUV);

    // ---- 暖色调偏移——靠近热源越暖 ----
    float warmth = falloff * hazeIntensity * 0.06;
    color.r += warmth * 0.45;
    color.g += warmth * 0.18;
    color.b -= warmth * 0.08;

    // ---- 微弱的热浪明暗闪烁 ----
    float flicker = wave1 * 0.03 * falloff * hazeIntensity;
    color.rgb += flicker;

    return color;
}

technique Technique1
{
    pass ThermalHazePass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
