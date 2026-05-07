// ============================================================================
// KingSlimeBloodWing.fx —— 史莱姆王皇室血光凝胶翼着色器
// 重写说明：彻底移除所有 hash/noise/fbm，全部改用解析 sin/cos。
// 根本原因：hash 函数在纹理随 Boss 移动/缩放时会产生逐帧 UV 格点跳变，
// 表现为内部纹路的高频闪烁。sin/cos 是带限函数，任何缩放都不会产生混叠。
// ============================================================================

sampler uImage0 : register(s0);

float uTime;
float intensity;
float extension;
float flapStrength;
float flapPhase;
float flapEnergy;
float enragedMix;
float isFalling;
float seed;
float2 texelSize;   // 保留声明兼容外部传参，不在 shader 内使用

float3 bloodCore;
float3 bloodEdge;
float3 bloodHighlight;

// UV 形变：纯 sin 波，无任何 hash/noise，彻底杜绝形变本身带来的边缘闪烁
float2 DeformUV(float2 uv)
{
    float r = uv.x;

    // 鞭梢波：羽尖滞后羽根，扑翅时拖出弧线
    float whipAmp = (flapStrength * r * r * 0.06 + flapEnergy * r * r * 0.03);
    float whipY   = sin(flapPhase * 1.3 - r * 3.5 + seed * 0.7) * whipAmp;

    // 重力垂：strength 低时下沿向下偏移
    float sag  = ((1.0 - flapStrength) * 0.035 + (1.0 - extension) * 0.012)
                 * smoothstep(0.15, 0.90, r)
                 * smoothstep(-0.1, 0.5, uv.y - 0.5);

    // 砸地后掠：整翼向尖端方向拉伸
    float fallingX = isFalling * 0.018 * smoothstep(0.10, 0.90, r);

    return uv + float2(fallingX, whipY + sag);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 deformedUV = DeformUV(coords);
    float4 src = tex2D(uImage0, deformedUV);
    if (src.a < 0.01)
        return float4(0, 0, 0, 0);

    float density  = max(max(src.r, src.g), src.b);
    float skeleton = src.a;

    // 颜色计算全部用原坐标，颜色流动不跟着形变走
    float radial = coords.x;
    float along  = coords.y;

    // 流速：enragedMix 和 flapEnergy 影响幅度小，避免速度突变引起跳帧感
    float flowSpeed = 0.50 + enragedMix * 0.25;
    float flowDir   = lerp(1.0, -1.0, saturate(isFalling));
    float t         = uTime * flowSpeed * flowDir;

    // 1) 血浆底色：两层低频 sin 叠加，沿翼展方向缓慢流动
    //    频率均 ≤ 3，在任何缩放下都不会产生混叠
    float p1 = sin(radial * 2.5 - t        + along * 1.0 + seed      ) * 0.5 + 0.5;
    float p2 = sin(radial * 1.4 - t * 0.65 + along * 1.6 + seed * 2.1) * 0.5 + 0.5;
    float plasma = p1 * 0.58 + p2 * 0.42;

    // 2) 湿润反光：两层 sin 相乘产生斑块感，频率低，无混叠
    float st  = uTime * 0.28;
    float sh1 = sin(radial * 1.8 + along * 2.1 - st        + seed * 1.5) * 0.5 + 0.5;
    float sh2 = sin(radial * 1.1 - along * 1.5 + st * 0.55 + seed * 0.8) * 0.5 + 0.5;
    float wetSheen = pow(saturate(sh1 * sh2 * 2.2 - 0.3), 2.0) * skeleton;

    // 3) 翼膜脉络：abs-sin 产生分叉线条，模拟翼脉走向
    //    两组方向相交得到网状纹，不是平行条纹
    float v1 = 1.0 - abs(sin(along * 3.2 + radial * 1.8 + seed * 2.8));
    float v2 = 1.0 - abs(sin(along * 4.5 - radial * 1.1 + seed * 1.6));
    float veinMask = smoothstep(0.80, 0.96, v1 * v2);

    // 4) 翼下血液汇集：纯几何渐变 + 单层低频 sin 扰动
    float poolBase = smoothstep(0.58, 0.95, along) * smoothstep(0.40, 0.88, radial);
    float poolMod  = sin(radial * 2.2 + uTime * 0.22 + seed * 2.5) * 0.5 + 0.5;
    float poolMask = poolBase * (0.65 + 0.35 * poolMod);

    // 5) 扑翅脉冲环：以羽根为起点向外扩散
    float pulseRing = 0.0;
    if (flapEnergy > 0.01)
    {
        float ringCenter = 1.0 - flapEnergy;
        pulseRing = smoothstep(0.18, 0.0, abs(radial - ringCenter)) * flapEnergy;
    }

    // 6) 收拢可见性
    float visibility = saturate(extension * 1.15);
    visibility *= lerp(smoothstep(0.0, 0.55, radial), 1.0, saturate(extension));

    // 颜色合成
    float3 baseColor   = lerp(bloodEdge, bloodCore, saturate(plasma * 1.1));
    float3 enragedTint = lerp(bloodEdge * 0.55, bloodCore * 1.05, saturate(plasma + 0.10));
    baseColor = lerp(baseColor, enragedTint, enragedMix);

    float3 col = baseColor * (0.75 + 0.42 * density);
    col += bloodHighlight * wetSheen * (0.80 + flapEnergy * 0.45);
    col  = lerp(col, bloodEdge * 0.35, veinMask * 0.45);
    col  = lerp(col, bloodEdge * 0.48, poolMask * 0.40);
    col += lerp(bloodCore, bloodHighlight, pulseRing) * pulseRing * 1.25;

    // 边缘光晕：用 alpha 自身做 fresnel，避免采样邻域像素（在缩放时会产生错误边界）
    float edgeFresnel  = pow(1.0 - skeleton, 1.8) * skeleton;
    edgeFresnel        = smoothstep(0.0, 0.22, edgeFresnel);
    float edgeBreath   = 0.55 + 0.22 * sin(uTime * 1.6 + seed * 2.1);
    col += lerp(bloodCore, bloodHighlight, edgeBreath) * edgeFresnel * 0.40;

    float outAlpha = skeleton * visibility * vertexColor.a * intensity;
    outAlpha = saturate(outAlpha + pulseRing * 0.22 * skeleton);

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
