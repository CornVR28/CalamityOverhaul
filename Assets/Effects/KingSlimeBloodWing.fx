// ============================================================================
// KingSlimeBloodWing.fx —— 史莱姆王皇室血光凝胶翼着色器
// 设计目标：
//   1. 用 Wing.png 的 alpha 作为"羽毛形状骨架"，shader 负责染血+凝胶演出。
//   2. 颜色风格：深紫红 → 鲜血红 → 高光金白；纯有机噪声形态——绝不出现周期带状/扫描线。
//   3. 扑翅瞬间触发"血光脉冲"——羽根处径向血浆爆发，向羽尖外推。
//   4. 暴怒模式整体压暗、流光更急、加深紫调（enragedMix 控制）。
//   5. 通过 UV 位移让翅膀产生类布料/类血膜的自然受力形变——绝非纸板。
//      具体形变包括：
//        a. 沿径向传播的"鞭梢"波——羽尖滞后于羽根，扑翅时尾巴会带出弧线
//        b. 重力垂——下沿在 strength 低时下垂，让收翅瞬间像被风带着捋下来
//        c. 风扑微颤——低频 fbm 微小位移，给静止翅膀一点"血膜在呼吸"的味道
// ============================================================================

sampler uImage0 : register(s0);

float uTime;             // 全局时间
float intensity;         // 主强度 0~1
float extension;         // 翅膀展开度 0~1
float flapStrength;      // 用力扑翅程度 0~1（控制 UV 位移的总幅度）
float flapPhase;         // 当前扑翅相位 [-π, π]
float flapEnergy;        // 当前扑翅能量峰值 0~1（事件型，每次扑翅 reset 到 1 后衰减）
float enragedMix;        // 暴怒混入度 0~1
float isFalling;         // 砸地下落混入度 0~1（连续，shader 内 lerp 血流方向）
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

// ============================================================================
// UV 形变——根据当前扑翅状态，把采样坐标做一次"软变形"再读纹理。
// 这是让翅膀脱离"纸板感"的核心：羽尖会带波，下沿会重力垂，无风时膜片轻颤。
//   uv     : 原始 0~1 翅膀 UV
//   返回值 : 位移后的 UV（仍在 0~1 附近，最大约 ±0.04 偏移）
// ============================================================================
float2 DeformUV(float2 uv)
{
    // 沿羽方向 0..1（root → tip）
    float r = uv.x;
    // 厚度方向中点为 0.5
    float c = uv.y - 0.5;

    //========================================
    // a) 鞭梢波——以羽根为锚点向羽尖传播，尖端滞后产生弧形扫尾
    //    幅度 ∝ flapStrength × radial²，让根稳、尖飘
    //    频率与 flapPhase 解耦，速度自驱（不会因为 strength=0 就冻结）
    //========================================
    float whipPhase = flapPhase * 1.4 - r * 4.5 + seed * 0.8;
    float whipAmp = flapStrength * (r * r) * 0.085 + flapEnergy * (r * r) * 0.05;
    float2 whipDisp = float2(0, sin(whipPhase) * whipAmp);

    //========================================
    // b) 重力垂——strength 低时下沿往下挪，模拟无升力时的"软塌"
    //    展开度低（收拢中）也加重重力垂感觉，让收翅看起来不是平移而是"软下来"
    //========================================
    float sag = (1.0 - flapStrength) * 0.55 + (1.0 - extension) * 0.20;
    // 只在远离根部的位置施加，让根部稳定
    sag *= smoothstep(0.20, 0.95, r);
    // c<0 上半页 几乎不动；c>0 下半页 下垂
    float belowFactor = smoothstep(-0.15, 0.45, c);
    float2 sagDisp = float2(0, sag * belowFactor * 0.05);

    //========================================
    // c) 风扑微颤——低频 fbm 给膜片"轻颤"的呼吸感
    //    哪怕完全静止时也保留少量颤动，让翅膀始终是"活的"
    //========================================
    float breathStrength = lerp(0.006, 0.018, flapStrength);
    float2 breath = float2(
        noise(float2(r * 3.0, uTime * 0.55 + seed * 1.7)) - 0.5,
        noise(float2(r * 3.0 + 7.3, uTime * 0.55 + seed * 1.7 + 11.1)) - 0.5
    ) * breathStrength * smoothstep(0.10, 0.85, r);

    //========================================
    // d) 砸地后掠——isFalling 时把整体 UV 向羽根方向"拉伸"，制造绷紧感
    //    实现：在 falling 时把采样位置稍微向 r 增大方向偏移，让整翼贴出线条美感
    //========================================
    float fallingPull = isFalling * 0.025 * smoothstep(0.10, 0.95, r);
    float2 fallingDisp = float2(fallingPull, 0);

    return uv + whipDisp + sagDisp + breath + fallingDisp;
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    //========================================
    // 用形变后的 UV 取样翅膀骨架
    //========================================
    float2 deformedUV = DeformUV(coords);
    float4 src = tex2D(uImage0, deformedUV);
    if (src.a < 0.01)
        return float4(0, 0, 0, 0);

    float density = max(max(src.r, src.g), src.b);
    float skeleton = src.a;

    //========================================
    // 染色用 UV 仍然采用原坐标——颜色流动不跟着位移走，避免"血色一起飘"造成晃眼
    //========================================
    float radial = coords.x;
    float along  = coords.y;

    //========================================
    // 1) 血液流动渐变——fbm 染色
    //    flowDir 以 isFalling 在 [+1, -1] lerp，过渡平滑（中点速度趋零，自然反向）
    //========================================
    float flowSpeed = 0.85 + enragedMix * 0.7 + flapEnergy * 1.5;
    float flowDir = lerp(1.0, -1.0, saturate(isFalling));
    float flowOffset = uTime * flowSpeed * flowDir + seed * 1.7;

    float2 flowUV = float2(radial * 3.6 - flowOffset, along * 2.4 + uTime * 0.25);
    float plasma = saturate(fbm(flowUV) * 1.25 - 0.15);

    //========================================
    // 2) 凝胶湿润反光——fbm 双层叠加阈值化的"湿润斑块"，无周期带状
    //========================================
    float sheenScroll = uTime * (0.45 + flapEnergy * 0.7);
    float2 sheenUV1 = float2(radial * 3.5 - sheenScroll * 1.1,
                             along  * 5.5 + sheenScroll * 0.40 + seed * 1.3);
    float sheen1 = fbm(sheenUV1);
    float2 sheenUV2 = float2(radial * 6.2 + sheenScroll * 0.8,
                             along  * 3.5 - sheenScroll * 0.55 + seed * 2.1);
    float sheen2 = fbm(sheenUV2);
    float wetSheen = saturate((sheen1 * 0.62 + sheen2 * 0.38) * 1.65 - 0.62) * skeleton;

    //========================================
    // 3) 暗血静脉
    //========================================
    float vein = fbm(float2(radial * 8.0 + seed * 3.7, along * 14.0));
    vein = saturate(vein * 1.15 - 0.35);
    float veinMask = smoothstep(0.55, 0.85, vein);

    //========================================
    // 4) 翼下血液汇集
    //========================================
    float poolMask = smoothstep(0.55, 0.95, along);
    poolMask *= 0.55 + 0.45 * fbm(float2(radial * 7.0 + seed * 4.3,
                                         along  * 4.0 + uTime * 0.30));
    poolMask *= smoothstep(0.45, 0.95, radial);

    //========================================
    // 5) 扑翅脉冲——以根部为圆心向外扩散的环形血爆
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
    float3 baseColor = lerp(bloodEdge, bloodCore, saturate(plasma * 1.1));
    float3 enragedTint = lerp(bloodEdge * 0.55, bloodCore * 1.05, saturate(plasma + 0.10));
    baseColor = lerp(baseColor, enragedTint, enragedMix);

    float3 col = baseColor * (0.72 + 0.55 * density);

    col += bloodHighlight * wetSheen * (0.85 + flapEnergy * 0.7);
    col = lerp(col, bloodEdge * 0.35, veinMask * 0.55);
    col = lerp(col, bloodEdge * 0.45, poolMask * 0.55);
    col += lerp(bloodCore, bloodHighlight, pulseRing) * pulseRing * 1.4;

    //========================================
    // 边缘血雾光晕（fresnel）——边缘 alpha 也用形变后的 UV 取样，让羽缘随膜片微动
    //========================================
    float aL = tex2D(uImage0, deformedUV + float2(-texelSize.x * 1.5, 0)).a;
    float aR = tex2D(uImage0, deformedUV + float2( texelSize.x * 1.5, 0)).a;
    float aU = tex2D(uImage0, deformedUV + float2(0, -texelSize.y * 1.5)).a;
    float aD = tex2D(uImage0, deformedUV + float2(0,  texelSize.y * 1.5)).a;
    float edge = saturate(1.0 - (aL + aR + aU + aD) * 0.25);
    edge = smoothstep(0.0, 0.65, edge);

    float edgeBreath = 0.55 + 0.30 * sin(uTime * 3.2 + seed * 4.0);
    col += lerp(bloodCore, bloodHighlight, edgeBreath) * edge * 0.50 * (0.55 + flapEnergy * 0.7);

    //========================================
    // 输出 alpha
    //========================================
    float outAlpha = skeleton * visibility * vertexColor.a * intensity;
    outAlpha = saturate(outAlpha + pulseRing * 0.30 * skeleton);

    col *= vertexColor.rgb;

    return float4(col * outAlpha, outAlpha);
}

technique Technique1
{
    pass KingSlimeBloodWingPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
