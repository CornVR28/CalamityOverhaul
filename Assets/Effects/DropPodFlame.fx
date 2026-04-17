// ============================================================================
// DropPodFlame.fx — 空降仓科幻能量尾焰着色器 (ps_3_0)
// 高亮度等离子喷射：白蓝核心 → 青蓝中段 → 橙色消散
// 多层FBM噪声 · 湍流UV扭曲 · 高斯核心光柱 · 能量脉冲
// 配合InnoVault Trail条带渲染使用
// ============================================================================

float4x4 transformMatrix;
float globalTime;
float heatIntensity;   // 再入灼烧强度 0~1

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

struct VSInput
{
    float4 Position : POSITION0;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

struct PSInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoords : TEXCOORD0;
};

PSInput VertexShaderFunction(VSInput v)
{
    PSInput o;
    o.Position = mul(v.Position, transformMatrix);
    o.TexCoords = v.TexCoords;
    o.Color = v.Color;
    return o;
}

// ---- FBM 噪声（4层叠加）ps_3_0 允许循环内纹理采样 ----
float fbm(float2 uv)
{
    float value = 0.0;
    float amplitude = 0.5;
    float2 offset = float2(37.0, 17.0);
    for (int i = 0; i < 4; i++)
    {
        value += amplitude * tex2D(noiseTex, uv).r;
        uv = uv * 2.17 + offset;
        amplitude *= 0.48;
    }
    return value;
}

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float2 uv = input.TexCoords;
    // uv.x: 0=喷口根部, 1=火焰远端
    // uv.y: 0~1 横截面（0.5=中心）
    float progress = uv.x;
    float crossDist = abs(uv.y - 0.5) * 2.0; // 0=中心, 1=边缘

    // ---- 湍流UV扭曲——让火焰形态更有机 ----
    float2 turbUV = uv * float2(1.2, 2.5) + float2(-globalTime * 4.5, globalTime * 0.3);
    float turbulence = (fbm(turbUV) - 0.5) * 0.12 * progress;
    float2 distortedUV = uv + float2(turbulence, turbulence * 0.6);

    // ---- 多层FBM噪声——营造丰富的等离子体翻滚质感 ----
    float n1 = fbm(distortedUV * float2(1.0, 1.8) + float2(-globalTime * 4.0, 0.0));
    float n2 = fbm(distortedUV * float2(0.7, 2.5) + float2(-globalTime * 5.5, 0.4));
    float n3 = tex2D(noiseTex, distortedUV * float2(2.5, 4.0) + float2(-globalTime * 7.0, 0.15)).r;
    float combinedNoise = n1 * 0.45 + n2 * 0.35 + n3 * 0.20;

    // ---- 横截面衰减：根部锐利、远端柔和 ----
    float edgeSharpness = lerp(2.8, 1.0, progress);
    float edgeFade = pow(saturate(1.0 - crossDist), edgeSharpness);

    // ---- 纵向衰减：能量沿尾焰渐消 ----
    float tailFade = pow(saturate(1.0 - progress), 1.3);

    // ---- 高斯核心光柱——中央较亮的等离子束 ----
    float coreWidth = lerp(0.15, 0.03, progress);
    float coreBrightness = exp(-crossDist * crossDist / (coreWidth * coreWidth * 2.0));

    // ---- 科幻能量配色 ----
    float3 whiteCore  = float3(0.90, 0.93, 1.00);  // 淡蓝白核心
    float3 blueInner  = float3(0.25, 0.65, 1.00);  // 亮青蓝
    float3 cyanMid    = float3(0.08, 0.35, 0.85);  // 电光蓝
    float3 warmOuter  = float3(0.80, 0.40, 0.10);  // 橙黄消散

    // 纵向颜色分布
    float3 flameColor;
    if (progress < 0.10)
        flameColor = lerp(whiteCore, blueInner, progress / 0.10);
    else if (progress < 0.40)
        flameColor = lerp(blueInner, cyanMid, (progress - 0.10) / 0.30);
    else
        flameColor = lerp(cyanMid, warmOuter, (progress - 0.40) / 0.60);

    // 与顶点色融合——根部由着色器主导，远端由顶点色主导
    float3 vertexRGB = input.Color.rgb;
    flameColor = lerp(flameColor, vertexRGB * 1.1, saturate(progress * 1.5));

    // 横截面着色：中心略亮带蓝调，边缘保留主色
    float3 centerTint = flameColor * 0.4 + blueInner * 0.6;
    flameColor = lerp(centerTint, flameColor, saturate(crossDist * 2.0));

    // ---- 亮度组合 ----
    float baseBrightness = edgeFade * tailFade * combinedNoise;
    float coreGlow = coreBrightness * tailFade;

    // 热强度增幅——大幅降低，避免Additive双绘过曝
    float heatBoost = 1.0 + heatIntensity * 0.4;
    // 能量脉动
    float pulse = 1.0 + sin(globalTime * 28.0 + progress * 12.0) * 0.06;

    // ---- 能量丝缕——边缘的明亮细节 ----
    float wispSample = tex2D(noiseTex, uv * float2(3.5, 7.0) + float2(-globalTime * 6.0, globalTime * 0.4)).r;
    float wispMask = smoothstep(0.62, 0.78, wispSample) * edgeFade * tailFade * 0.35;

    // ---- 外层蓝色光晕——让边缘有柔和的辉光扩散 ----
    float haloFade = exp(-crossDist * crossDist / 0.32) * tailFade * 0.15;
    float3 haloColor = cyanMid * haloFade;

    // ---- 核心色：不再用纯白，用偏蓝白的火焰色 ----
    float3 coreColor = lerp(whiteCore, blueInner, 0.3);

    // ---- 最终合成——整体压低亮度，让颜色层次可见 ----
    float3 finalColor = flameColor * baseBrightness * 0.9 * heatBoost * pulse
                       + coreColor * coreGlow * 0.5 * heatBoost
                       + flameColor * wispMask * 0.6
                       + haloColor;

    float alpha = saturate(baseBrightness * 0.8 + coreGlow * 0.3 + wispMask * 0.2 + haloFade)
                * input.Color.a;

    return float4(finalColor * alpha, alpha);
}

technique Technique1
{
    pass DropPodFlamePass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
