// ============================================================================
// KingSlimeBloodWing.fx —— 史莱姆王皇室血光凝胶翼着色器
// 设计目标：
//   1. 用 Wing.png 的 alpha 作为"羽毛形状骨架"，shader 负责染血+凝胶演出。
//   2. 颜色风格：深紫红 → 鲜血红 → 高光金白；纯有机噪声形态——绝不出现周期带状/扫描线。
//   3. 扑翅瞬间触发"血光脉冲"——羽根处径向血浆爆发，向羽尖外推。
//   4. 暴怒模式（mode>0.5）整体压暗、流光更急、加深紫调。
// ============================================================================

sampler uImage0 : register(s0);

float uTime;             // 全局时间
float intensity;         // 主强度 0~1（与 alpha 放大相关）
float extension;         // 翅膀展开度 0~1（0=收拢, 1=完全展开）
float flapPhase;         // 当前扑翅相位 [-π, π]
float flapEnergy;        // 当前扑翅能量峰值 0~1（每次扑翅刷新一次脉冲）
float enragedMix;        // 暴怒混入度 0~1
float isFalling;         // 是否处于砸地下落（>=0.5 表示是；用于改变流光方向）
float seed;              // 实例化扰动种子
float2 texelSize;        // 1/纹理尺寸

float3 bloodCore;        // 血色核心 (亮红)
float3 bloodEdge;        // 血色边缘 (暗紫红)
float3 bloodHighlight;   // 高光 (亮金白)

// ----- 哈希 / 噪声 -----
float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = hash(i);
    float b = hash(i + float2(1, 0));
    float c = hash(i + float2(0, 1));
    float d = hash(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// 多层 fbm 用于"凝胶/血浆流体"质感
float fbm(float2 p)
{
    float v = 0.0;
    float amp = 0.55;
    [unroll]
    for (int i = 0; i < 4; i++)
    {
        v += amp * noise(p);
        p *= 2.05;
        amp *= 0.55;
    }
    return v;
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    //========================================
    // 取样原翅膀 mask；alpha 决定形状
    //========================================
    float4 src = tex2D(uImage0, coords);
    if (src.a < 0.01)
        return float4(0, 0, 0, 0);

    // 灰度密度——白色羽毛中心 -> 1，边缘半透 -> 较低
    float density = max(max(src.r, src.g), src.b);
    // alpha 当作"羽毛骨架强度"
    float skeleton = src.a;

    //========================================
    // UV 工作坐标
    //   coords.x ∈ [0,1]，0 在羽根、1 在羽尖
    //   coords.y ∈ [0,1]，0 在翅膀上沿、1 在下沿
    //========================================
    float radial = coords.x;
    float along  = coords.y;

    //========================================
    // 1) 血液流动渐变——沿羽毛径向流动 fbm，朝翼尖去
    //    使用低频、慢漂的 fbm 染色，非周期带状
    //========================================
    float flowSpeed = 0.85 + enragedMix * 0.7 + flapEnergy * 1.5;
    // 砸地下落时让血流"向后甩"
    float flowDir = isFalling >= 0.5 ? -1.0 : 1.0;
    float flowOffset = uTime * flowSpeed * flowDir + seed * 1.7;

    float2 flowUV = float2(radial * 3.6 - flowOffset, along * 2.4 + uTime * 0.25);
    float plasma = fbm(flowUV);
    plasma = saturate(plasma * 1.25 - 0.15);

    //========================================
    // 2) 凝胶湿润反光——双层 fbm 叠加生成不规则亮斑
    //    彻底替代任何 "frac(...) -> smoothstep" 形成的周期高光带
    //========================================
    float sheenScroll = uTime * (0.45 + flapEnergy * 0.7);
    float2 sheenUV1 = float2(radial * 3.5 - sheenScroll * 1.1,
                             along  * 5.5 + sheenScroll * 0.40 + seed * 1.3);
    float sheen1 = fbm(sheenUV1);

    float2 sheenUV2 = float2(radial * 6.2 + sheenScroll * 0.8,
                             along  * 3.5 - sheenScroll * 0.55 + seed * 2.1);
    float sheen2 = fbm(sheenUV2);

    // 阈值化——只取 fbm 的高亮峰，让反光呈现"湿润斑块"而非全屏漫射
    float wetSheen = saturate((sheen1 * 0.62 + sheen2 * 0.38) * 1.65 - 0.62) * skeleton;

    //========================================
    // 3) 暗血静脉（保留——这是有机条纹，不是规则扫描线）
    //========================================
    float vein = fbm(float2(radial * 8.0 + seed * 3.7, along * 14.0));
    vein = saturate(vein * 1.15 - 0.35);
    float veinMask = smoothstep(0.55, 0.85, vein);

    //========================================
    // 4) 翼下血液汇集——下沿与翼尖区域偏暗（"血池/血滴汇流"）
    //    用 along 的 smoothstep + fbm 扰动制造不均匀边缘，没有带状结构
    //========================================
    float poolMask = smoothstep(0.55, 0.95, along);
    poolMask *= 0.55 + 0.45 * fbm(float2(radial * 7.0 + seed * 4.3,
                                         along  * 4.0 + uTime * 0.30));
    poolMask *= smoothstep(0.45, 0.95, radial);

    //========================================
    // 5) 扑翅脉冲——以根部为圆心向外扩散的环形血爆（事件型，非周期）
    //========================================
    float pulseRing = 0.0;
    if (flapEnergy > 0.01)
    {
        float ringCenter = 1.0 - flapEnergy;
        float ringWidth  = 0.18;
        pulseRing = smoothstep(ringWidth, 0.0, abs(radial - ringCenter)) * flapEnergy;
    }

    //========================================
    // 6) 收拢可见性——extension 低时仅保留羽根附近
    //========================================
    float visibility = saturate(extension * 1.15);
    float foldMask   = smoothstep(0.0, 0.55, radial);
    visibility *= lerp(foldMask, 1.0, saturate(extension));

    //========================================
    // 颜色合成
    //========================================
    // 基础血色：暗紫红 -> 鲜血红
    float3 baseColor = lerp(bloodEdge, bloodCore, saturate(plasma * 1.1));
    // 暴怒时整体偏深紫黑 + 鲜血亮血
    float3 enragedTint = lerp(bloodEdge * 0.55, bloodCore * 1.05, saturate(plasma + 0.10));
    baseColor = lerp(baseColor, enragedTint, enragedMix);

    float3 col = baseColor * (0.72 + 0.55 * density);

    // 凝胶湿润反光叠加（不规则斑块）
    col += bloodHighlight * wetSheen * (0.85 + flapEnergy * 0.7);

    // 暗静脉染色——降低亮度并偏暗紫
    col = lerp(col, bloodEdge * 0.35, veinMask * 0.55);

    // 翼下血液汇集——加深、偏暗血红
    col = lerp(col, bloodEdge * 0.45, poolMask * 0.55);

    // 扑翅脉冲：白热血光向外推
    col += lerp(bloodCore, bloodHighlight, pulseRing) * pulseRing * 1.4;

    //========================================
    // 边缘血雾光晕（fresnel 风格）
    //   关键改动：不再用 sin(uTime*4 + radial*8) 这种"沿径向带条纹"的脉冲，
    //   改为整翼统一的呼吸脉冲——只跟时间和 seed 走，无 radial 依赖。
    //========================================
    float aL = tex2D(uImage0, coords + float2(-texelSize.x * 1.5, 0)).a;
    float aR = tex2D(uImage0, coords + float2( texelSize.x * 1.5, 0)).a;
    float aU = tex2D(uImage0, coords + float2(0, -texelSize.y * 1.5)).a;
    float aD = tex2D(uImage0, coords + float2(0,  texelSize.y * 1.5)).a;
    float edge = saturate(1.0 - (aL + aR + aU + aD) * 0.25);
    edge = smoothstep(0.0, 0.65, edge);

    float edgeBreath = 0.55 + 0.30 * sin(uTime * 3.2 + seed * 4.0);
    col += lerp(bloodCore, bloodHighlight, edgeBreath) * edge * 0.50 * (0.55 + flapEnergy * 0.7);

    //========================================
    // 输出 alpha
    //========================================
    float outAlpha = skeleton * visibility * vertexColor.a * intensity;
    outAlpha = saturate(outAlpha + pulseRing * 0.30 * skeleton);

    // 与 vertexColor RGB 调色
    col *= vertexColor.rgb;

    // 预乘 alpha 输出（与 BlendState.AlphaBlend 配合）
    return float4(col * outAlpha, outAlpha);
}

technique Technique1
{
    pass KingSlimeBloodWingPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
