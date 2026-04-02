// ============================================================================
// CyberEnergyOrb.fx — 赛博能量球着色器
// 多层噪声扰动 + Colormap 映射 + 菲涅尔边缘辉光 + 数字脉冲
// 对一张灰度纹理（如SoftGlow）应用，产生高质感能量球效果
// 支持领域超驱模式：黑墙故障风格 + 白热金/品红配色
// ============================================================================

float uTime;
float fadeAlpha;
float3 coreColor;       // 最亮的内核色
float3 glowColor;       // 中间辉光色
float3 auraColor;       // 边缘外层色
float orbScale;          // 能量球的脉动缩放

// ---- 超驱参数 ----
float overdriveAmount;   // 0=正常 1=完全超驱
float glitchBurst;       // 0-1 间歇性故障爆发强度
float3 odCoreColor;      // 超驱核心色（白热金）
float3 odGlowColor;      // 超驱辉光色（品红）
float3 odAuraColor;      // 超驱光晕色（深品红）

texture uNoiseTex;
sampler noiseSamp = sampler_state
{
    texture = <uNoiseTex>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

// SpriteBatch 自动将纹理绑定到 register(s0)
sampler baseSamp : register(s0);

// 简单哈希
float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

struct PSInput
{
    float4 Position : POSITION0;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float2 uv = input.TexCoords;
    
    // 超驱色彩混合
    float od = overdriveAmount;
    float3 effCore = lerp(coreColor, odCoreColor, od);
    float3 effGlow = lerp(glowColor, odGlowColor, od);
    float3 effAura = lerp(auraColor, odAuraColor, od);

    // 以纹理中心为原点的极坐标
    float2 center = uv - 0.5;
    float dist = length(center);          // 0=中心 0.5=边缘
    float angle = atan2(center.y, center.x);
    
    // 基础灰度形状（圆形衰减）
    float baseTex = tex2D(baseSamp, uv).r;
    
    // ============================================================
    // A. 多层噪声扰动 —— 能量表面流动
    // ============================================================
    float timeMultiplier = 1.0 + od * 0.8; // 超驱时流动加快
    float2 noiseUV1 = float2(
        dist * 2.0 + uTime * 0.3 * timeMultiplier,
        angle * 0.318 + uTime * 0.15 * timeMultiplier
    );
    float n1 = tex2D(noiseSamp, frac(noiseUV1)).r;
    
    float2 noiseUV2 = float2(
        dist * 4.0 - uTime * 0.5 * timeMultiplier + 0.37,
        angle * 0.637 - uTime * 0.25 * timeMultiplier
    );
    float n2 = tex2D(noiseSamp, frac(noiseUV2)).g;
    
    float2 noiseUV3 = float2(
        dist * 8.0 + uTime * 0.8 * timeMultiplier,
        angle * 1.27 + uTime * 0.4 * timeMultiplier
    );
    float n3 = tex2D(noiseSamp, frac(noiseUV3)).b;
    
    float energyField = n1 * 0.5 + n2 * 0.3 + n3 * 0.2;
    
    // ============================================================
    // B. Colormap 映射 —— 径向距离+能量场决定颜色
    // ============================================================
    float radialGrad = 1.0 - smoothstep(0.0, 0.32, dist);
    float solidCore = 1.0 - smoothstep(0.0, 0.2, dist);
    radialGrad = max(radialGrad, solidCore);
    
    float cmapInput = saturate(radialGrad + (energyField - 0.5) * 0.35 * radialGrad);
    
    // 超驱时能量场扰动更强
    cmapInput = saturate(cmapInput + od * (energyField - 0.5) * 0.2);
    
    float3 color;
    if (cmapInput < 0.35)
    {
        float t = cmapInput / 0.35;
        color = lerp(effAura * 0.5, effGlow, t);
    }
    else if (cmapInput < 0.7)
    {
        float t = (cmapInput - 0.35) / 0.35;
        color = lerp(effGlow, effCore, t);
    }
    else
    {
        float t = (cmapInput - 0.7) / 0.3;
        color = lerp(effCore, float3(1.0, 0.97, 0.93), t * t);
    }
    
    // ============================================================
    // C. 菲涅尔边缘辉光 —— 边缘高亮环
    // ============================================================
    float fresnelInner = 1.0 - smoothstep(0.15, 0.30, dist);
    float fresnelRing = smoothstep(0.20, 0.28, dist) * (1.0 - smoothstep(0.28, 0.35, dist));
    float3 fresnelColor = effGlow * fresnelRing * (1.5 + od * 1.0);
    
    // ============================================================
    // D. 数字脉冲纹 —— 赛博科幻质感
    // ============================================================
    float pulseSpeed = 6.0 + od * 4.0;
    float ringPulse = sin(dist * 40.0 - uTime * pulseSpeed) * 0.5 + 0.5;
    ringPulse = pow(ringPulse, 8.0);
    ringPulse *= smoothstep(0.32, 0.10, dist) * (0.15 + od * 0.1);
    
    float rayAngle = frac(angle * 2.546 + uTime * (0.5 + od * 0.3));
    float rays = pow(abs(sin(rayAngle * 3.14159 * 6.0)), 20.0);
    rays *= smoothstep(0.30, 0.15, dist) * (0.1 + od * 0.08);
    
    // ============================================================
    // E. 表面明暗变化 —— 伪3D球体光照
    // ============================================================
    float2 lightDir = float2(-0.4, -0.5);
    float lightDot = dot(normalize(center), normalize(lightDir));
    float lighting = 0.7 + 0.3 * lightDot;
    
    // ============================================================
    // 合成
    // ============================================================
    float3 finalColor = color * lighting;
    finalColor += fresnelColor;
    finalColor += effCore * ringPulse;
    finalColor += effGlow * rays;
    
    float coreHot = pow(saturate(1.0 - dist / 0.16), 2.5);
    finalColor += float3(1.0, 0.98, 0.95) * coreHot * (0.8 + od * 0.3);
    
    // alpha
    float alpha = saturate(radialGrad * 1.5);
    alpha *= 1.0 - smoothstep(0.28, 0.34, dist);
    alpha += fresnelRing * 0.4;
    alpha = saturate(alpha) * fadeAlpha;

    // ============================================================
    // F. 超驱故障叠加
    // ============================================================
    if (od > 0.01)
    {
        float burst = glitchBurst;

        // F-1. RGB通道分离（对基础纹理重采样）
        float splitDist = od * (0.008 + burst * 0.025);
        float splitAngle = uTime * 2.5 + burst * 5.0;
        float2 splitDir = float2(cos(splitAngle), sin(splitAngle)) * splitDist;
        float rChan = tex2D(baseSamp, uv + splitDir).r;
        float bChan = tex2D(baseSamp, uv - splitDir).r;
        // 将分离通道混入颜色
        float3 rgbSplit;
        rgbSplit.r = finalColor.r * (0.7 + rChan * 0.5);
        rgbSplit.g = finalColor.g;
        rgbSplit.b = finalColor.b * (0.7 + bChan * 0.5);
        finalColor = lerp(finalColor, rgbSplit, od * 0.6);

        // F-2. 方块腐蚀
        float2 blockUV = floor(uv * (15.0 + burst * 10.0)) / (15.0 + burst * 10.0);
        float blockID2 = hash21(blockUV + float2(floor(uTime * 8.0), 0.0));
        float blockThresh = 0.90 - burst * 0.25;
        float blockOn = step(blockThresh, blockID2);
        finalColor += float3(1.0, 0.92, 0.8) * blockOn * od * (0.25 + burst * 0.5);
        alpha += blockOn * od * 0.08;

        // F-3. 扫描线干扰
        float scanlineOD = frac(uv.y * 60.0 + uTime * 1.5);
        scanlineOD = step(0.94, scanlineOD);
        finalColor += effCore * scanlineOD * od * 0.4;

        // F-4. 全局闪烁
        float flickOD = hash21(float2(floor(uTime * 20.0), 9.3));
        float flickMag = burst * (flickOD - 0.3) * 1.5;
        finalColor *= 1.0 + flickMag;

        // F-5. 超驱菲涅尔增强（品红外环）
        float odRing = smoothstep(0.22, 0.30, dist) * (1.0 - smoothstep(0.30, 0.38, dist));
        finalColor += effGlow * odRing * od * (0.6 + burst * 0.8);
        alpha += odRing * od * 0.15;

        // F-6. alpha增强
        alpha *= 1.0 + od * 0.15;
    }
    
    return float4(finalColor * alpha, alpha) * input.Color;
}

technique Technique1
{
    pass CyberEnergyOrbPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
