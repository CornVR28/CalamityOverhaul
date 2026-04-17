//=============================================================================
// NeutronWarp.fx — 中子星程序化扭曲位移图生成器
// ps_3_0 — 替代CPU端33-133层暴力叠绘，单Pass生成扭曲贡献
// 输出格式: R=位移方向(0-1→0-2π), G=位移强度, A=混合权重
// 4种技术: GravitationalVortex / ShockwaveRing / RelativisticJet / GravitationalLens
//=============================================================================

float uTime;
float uIntensity;
float uProgress;
float uRadius;
float uRotation;

#define PI  3.14159265
#define TAU 6.28318530

// ---- 伪随机哈希 ----
float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// ---- 值噪声 ----
float valueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = hash21(i);
    float b = hash21(i + float2(1.0, 0.0));
    float c = hash21(i + float2(0.0, 1.0));
    float d = hash21(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// ---- 分形噪声 ----
float fbm2(float2 p)
{
    float v = 0.0;
    float amp = 0.5;
    float2 shift = float2(17.3, 31.7);
    for (int i = 0; i < 3; i++)
    {
        v += valueNoise(p) * amp;
        p = p * 2.17 + shift;
        amp *= 0.5;
    }
    return v;
}

//=============================================================================
// 技术A: 重力漩涡 (Gravitational Vortex)
// 中子星爆炸 — 引力场造成的空间扭曲漩涡
// 替代: NeutronExplode(33层) / EXNeutronExplode(133层)
//=============================================================================
float4 GravitationalVortexPS(float2 uv : TEXCOORD0) : COLOR0
{
    float2 centered = uv - 0.5;
    float dist = length(centered);
    float angle = atan2(centered.y, centered.x);
    float normDist = dist / max(uRadius, 0.001);

    if (normDist > 1.4)
        return float4(0, 0, 0, 0);

    // 引力势能衰减：中子星级别的致密引力场
    float gravCore = exp(-normDist * normDist * 2.0);

    // 爱因斯坦环残影（特定半径处的光线汇聚增亮）
    float einsteinRing = exp(-pow(abs(normDist - 0.7), 2.0) * 30.0) * 0.3;

    // 差分旋转：内快外慢（角动量守恒）
    float angularVelocity = 1.0 / (normDist + 0.12);
    float swirl = angularVelocity * 0.5 * sin(uTime * 2.0 + normDist * 4.0);

    // 湍流磁场噪声
    float noise = fbm2(centered * 5.0 + float2(uTime * 0.4, uTime * 0.3));
    float noiseOffset = (noise - 0.5) * 0.4;

    // 引力脉冲波（从中心向外传播）
    float pulse = sin(normDist * 15.0 - uTime * 8.0) * 0.12;
    pulse *= exp(-normDist * 4.0);

    // 位移方向
    float direction = angle + swirl + noiseOffset + uRotation;
    direction = frac(direction / TAU + 0.5);

    // 位移强度
    float magnitude = (gravCore + einsteinRing + pulse) * uProgress * uIntensity;
    magnitude = saturate(magnitude);

    // 柔和边缘衰减
    float alpha = smoothstep(1.4, 0.6, normDist) * uProgress;

    return float4(direction, magnitude, 0, saturate(alpha));
}

//=============================================================================
// 技术B: 冲击波环 (Shockwave Ring)
// 爆炸扩散的环形扭曲
// 替代: NeutronExplosionRanged / EXNeutronExplosionRanged / NeutronExplosionRogue
//=============================================================================
float4 ShockwaveRingPS(float2 uv : TEXCOORD0) : COLOR0
{
    float2 centered = uv - 0.5;
    float dist = length(centered);
    float angle = atan2(centered.y, centered.x);
    float normDist = dist / max(uRadius, 0.001);

    if (normDist > 1.8)
        return float4(0, 0, 0, 0);

    // 冲击波前沿
    float ringPos = uProgress * 1.3;
    float ringWidth = 0.08 + uProgress * 0.06;
    float ring = exp(-pow((normDist - ringPos) / ringWidth, 2.0));

    // 后方稀疏波拖尾
    float trail = exp(-max(normDist - ringPos, 0.0) * 6.0) * 0.25;
    trail *= (1.0 - uProgress);

    // 中心残余引力坍缩
    float residual = exp(-normDist * normDist * 8.0) * 0.2 * (1.0 - uProgress);

    // 环形噪声扰动
    float edgeNoise = valueNoise(float2(angle * 2.0 / TAU, uTime * 1.5));
    float noiseModulate = 0.8 + edgeNoise * 0.4;

    // 径向向外
    float direction = frac(angle / TAU + 0.5);

    // 强度
    float magnitude = (ring * noiseModulate + trail + residual) * uIntensity;
    magnitude = saturate(magnitude);

    float alpha = (ring + trail * 0.5 + residual) * smoothstep(1.8, 1.0, normDist);

    return float4(direction, magnitude, 0, saturate(alpha));
}

//=============================================================================
// 技术C: 相对论性喷流 (Relativistic Jet)
// 中子星极轴的物质喷射柱形扭曲
// 替代: NeutronWandExplode(33层 scale(0.1, 21))
//=============================================================================
float4 RelativisticJetPS(float2 uv : TEXCOORD0) : COLOR0
{
    float2 centered = uv - 0.5;

    // 磁力管约束: 高斯横截面
    float lateralDist = abs(centered.x);
    float lateralFalloff = exp(-lateralDist * lateralDist * 30.0);

    // 开尔文-亥姆霍兹不稳定性：喷流边界摆动
    float khInstability = sin(centered.y * 6.0 + uTime * 5.0) * 0.15 * lateralDist;

    // 内部扭结不稳定性
    float kinkInstability = sin(centered.y * 15.0 - uTime * 8.0) * 0.08;

    // 磁场重联闪烁
    float reconnection = valueNoise(float2(centered.y * 3.0 + 0.5, uTime * 2.0));
    reconnection = smoothstep(0.4, 0.7, reconnection) * 0.3;

    // 喷流动力
    float jetPower = lateralFalloff * uIntensity * (1.0 + reconnection);

    // 湍流
    float turb = fbm2(centered * float2(8.0, 2.5) + uTime * float2(0.3, 1.8));
    float turbOffset = (turb - 0.5) * 0.3;

    // 方向：沿轴 + 扰动
    float direction = PI * 0.5 + khInstability + kinkInstability + turbOffset;
    direction = frac(direction / TAU + 0.5);

    // 强度
    float magnitude = jetPower * uProgress;
    magnitude = saturate(magnitude);

    float alpha = lateralFalloff * uProgress * 0.9;

    return float4(direction, magnitude, 0, saturate(alpha));
}

//=============================================================================
// 技术D: 引力透镜 (Gravitational Lens)
// 飞行弹幕的紧凑引力弯曲
// 替代: NeutronGlaiveBeam(3层) / NeutronBullet(3层)
//=============================================================================
float4 GravitationalLensPS(float2 uv : TEXCOORD0) : COLOR0
{
    float2 centered = uv - 0.5;
    float dist = length(centered);
    float angle = atan2(centered.y, centered.x);
    float normDist = dist / max(uRadius, 0.001);

    if (normDist > 1.8)
        return float4(0, 0, 0, 0);

    // 广义相对论偏转 ∝ M/r²
    float deflection = uIntensity / (normDist * normDist + 0.15);
    deflection *= smoothstep(1.8, 0.2, normDist);

    // 爱因斯坦环增亮
    float einsteinR = 0.5;
    float eRing = exp(-pow(abs(normDist - einsteinR) * 5.0, 2.0)) * 0.25;

    // 径向向内
    float inwardAngle = angle + PI;
    float direction = frac(inwardAngle / TAU + 0.5);

    // 强度
    float magnitude = (deflection + eRing) * uProgress;
    magnitude = saturate(magnitude);

    float alpha = smoothstep(1.8, 0.4, normDist) * uProgress;

    return float4(direction, magnitude, 0, saturate(alpha));
}

//=============================================================================
// 技术定义
//=============================================================================
technique GravitationalVortex
{
    pass P0
    {
        PixelShader = compile ps_3_0 GravitationalVortexPS();
    }
}

technique ShockwaveRing
{
    pass P0
    {
        PixelShader = compile ps_3_0 ShockwaveRingPS();
    }
}

technique RelativisticJet
{
    pass P0
    {
        PixelShader = compile ps_3_0 RelativisticJetPS();
    }
}

technique GravitationalLens
{
    pass P0
    {
        PixelShader = compile ps_3_0 GravitationalLensPS();
    }
}
