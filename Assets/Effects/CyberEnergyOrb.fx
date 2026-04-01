// ============================================================================
// CyberEnergyOrb.fx — 赛博能量球着色器
// 多层噪声扰动 + Colormap 映射 + 菲涅尔边缘辉光 + 数字脉冲
// 对一张灰度纹理（如SoftGlow）应用，产生高质感能量球效果
// ============================================================================

float uTime;
float fadeAlpha;
float3 coreColor;       // 最亮的内核色
float3 glowColor;       // 中间辉光色
float3 auraColor;       // 边缘外层色
float orbScale;          // 能量球的脉动缩放

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
    
    // 以纹理中心为原点的极坐标
    float2 center = uv - 0.5;
    float dist = length(center);          // 0=中心 0.5=边缘
    float angle = atan2(center.y, center.x);
    
    // 基础灰度形状（圆形衰减）
    float baseTex = tex2D(baseSamp, uv).r;
    
    // ============================================================
    // A. 多层噪声扰动 —— 能量表面流动
    // ============================================================
    // 低频大尺度涌动
    float2 noiseUV1 = float2(
        dist * 2.0 + uTime * 0.3,
        angle * 0.318 + uTime * 0.15  // 0.318 ≈ 1/π
    );
    float n1 = tex2D(noiseSamp, frac(noiseUV1)).r;
    
    // 中频细节纹理
    float2 noiseUV2 = float2(
        dist * 4.0 - uTime * 0.5 + 0.37,
        angle * 0.637 - uTime * 0.25  // 0.637 ≈ 2/π
    );
    float n2 = tex2D(noiseSamp, frac(noiseUV2)).g;
    
    // 高频微细结构
    float2 noiseUV3 = float2(
        dist * 8.0 + uTime * 0.8,
        angle * 1.27 + uTime * 0.4
    );
    float n3 = tex2D(noiseSamp, frac(noiseUV3)).b;
    
    // 混合能量强度场
    float energyField = n1 * 0.5 + n2 * 0.3 + n3 * 0.2;
    
    // ============================================================
    // B. Colormap 映射 —— 径向距离+能量场决定颜色
    // ============================================================
    // 将dist映射到colormap参数：中心最亮(1)，边缘最暗(0)
    // 收紧边界让球体更实心
    float radialGrad = 1.0 - smoothstep(0.0, 0.32, dist);
    // 实心核心区域：dist < 0.18 的区域强制接近1
    float solidCore = 1.0 - smoothstep(0.0, 0.2, dist);
    radialGrad = max(radialGrad, solidCore);
    
    // 用能量场扰动radialGrad，产生活的表面
    float cmapInput = saturate(radialGrad + (energyField - 0.5) * 0.35 * radialGrad);
    
    // 三段式colormap: aura -> glow -> core -> white
    float3 color;
    if (cmapInput < 0.35)
    {
        float t = cmapInput / 0.35;
        color = lerp(auraColor * 0.5, glowColor, t);
    }
    else if (cmapInput < 0.7)
    {
        float t = (cmapInput - 0.35) / 0.35;
        color = lerp(glowColor, coreColor, t);
    }
    else
    {
        float t = (cmapInput - 0.7) / 0.3;
        color = lerp(coreColor, float3(1.0, 0.97, 0.93), t * t);
    }
    
    // ============================================================
    // C. 菲涅尔边缘辉光 —— 边缘高亮环
    // ============================================================
    float fresnelInner = 1.0 - smoothstep(0.15, 0.30, dist);
    float fresnelRing = smoothstep(0.20, 0.28, dist) * (1.0 - smoothstep(0.28, 0.35, dist));
    float3 fresnelColor = glowColor * fresnelRing * 1.5;
    
    // ============================================================
    // D. 数字脉冲纹 —— 赛博科幻质感
    // ============================================================
    // 同心环脉冲
    float ringPulse = sin(dist * 40.0 - uTime * 6.0) * 0.5 + 0.5;
    ringPulse = pow(ringPulse, 8.0); // 锐化成细线
    ringPulse *= smoothstep(0.32, 0.10, dist) * 0.15; // 仅在球体内部可见
    
    // 径向射线（微弱的能量脉络）
    float rayAngle = frac(angle * 2.546 + uTime * 0.5);
    float rays = pow(abs(sin(rayAngle * 3.14159 * 6.0)), 20.0);
    rays *= smoothstep(0.30, 0.15, dist) * 0.1;
    
    // ============================================================
    // E. 表面明暗变化 —— 伪3D球体光照
    // ============================================================
    // 模拟从左上方来的光照
    float2 lightDir = float2(-0.4, -0.5);
    float lightDot = dot(normalize(center), normalize(lightDir));
    float lighting = 0.7 + 0.3 * lightDot;
    
    // ============================================================
    // 合成
    // ============================================================
    float3 finalColor = color * lighting;
    finalColor += fresnelColor;
    finalColor += coreColor * ringPulse;
    finalColor += glowColor * rays;
    
    // 核心超亮（高能中心的白热感）——加大范围和强度
    float coreHot = pow(saturate(1.0 - dist / 0.16), 2.5);
    finalColor += float3(1.0, 0.98, 0.95) * coreHot * 0.8;
    
    // alpha：实心球体 + 边缘硬截断
    float alpha = saturate(radialGrad * 1.5);
    // 硬边缘截断：dist > 0.32 快速衰减到0
    alpha *= 1.0 - smoothstep(0.28, 0.34, dist);
    // 边缘补充一层薄薄的bloom
    alpha += fresnelRing * 0.4;
    alpha = saturate(alpha) * fadeAlpha;
    
    return float4(finalColor * alpha, alpha) * input.Color;
}

technique Technique1
{
    pass CyberEnergyOrbPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
