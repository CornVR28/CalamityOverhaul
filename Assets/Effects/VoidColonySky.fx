// ============================================================================
// VoidColonySky.fx — 虚空聚落天空着色器
// 亚空间末日风格全屏天空背景
// 红黑旋涡漩涡 + 能量裂隙流 + 星云 + 星辰 + 中心暗域
// 全程序化生成，无需外部纹理依赖
// ============================================================================

sampler uImage0 : register(s0);

float uTime;
float uIntensity;       // 整体强度 0~1（淡入淡出）
float uAspectRatio;     // 屏幕宽高比 screenWidth / screenHeight

#define PI  3.14159265
#define TAU 6.28318530

// ======================== Hash Functions ========================

float hash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.11369, 0.13787));
    p3 += dot(p3, p3.yzx + 19.19);
    return frac(float2((p3.x + p3.y) * p3.z, (p3.x + p3.z) * p3.y));
}

// ======================== Noise Primitives ========================

float vnoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f); // Hermite smooth

    float a = hash21(i);
    float b = hash21(i + float2(1, 0));
    float c = hash21(i + float2(0, 1));
    float d = hash21(i + float2(1, 1));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// 分形噪声，octave间旋转避免轴向伪影
float fbm(float2 p, int oct)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < oct; i++)
    {
        v += a * vnoise(p);
        // 旋转 ~37° 并放大频率
        p = float2(p.x * 0.8 - p.y * 0.6, p.x * 0.6 + p.y * 0.8) * 2.0;
        a *= 0.5;
    }
    return v;
}

// 脊噪声 — 产生锐利的山脊/裂缝图案
float ridgedFbm(float2 p, int oct)
{
    float v = 0.0;
    float a = 0.5;
    float prev = 1.0;
    for (int i = 0; i < oct; i++)
    {
        float n = 1.0 - abs(vnoise(p) * 2.0 - 1.0);
        n = n * n;
        n *= prev; // 级联权重，让深层细节依附于浅层结构
        prev = n;
        v += a * n;
        p = float2(p.x * 0.8 - p.y * 0.6, p.x * 0.6 + p.y * 0.8) * 2.0;
        a *= 0.5;
    }
    return v;
}

// 域弯曲fbm — 产生有机的扭曲图案
float warpedFbm(float2 p, float t)
{
    float2 q = float2(
        fbm(p + float2(0.0, 0.0), 3),
        fbm(p + float2(5.2, 1.3), 3)
    );
    float2 r = float2(
        fbm(p + 3.5 * q + float2(1.7, 9.2) + t * 0.1, 3),
        fbm(p + 3.5 * q + float2(8.3, 2.8) + t * 0.08, 3)
    );
    return fbm(p + 3.0 * r, 3);
}

// ======================== Star Field ========================

float starField(float2 uv, float scale, float time)
{
    float2 id = floor(uv * scale);
    float2 sub = frac(uv * scale);

    float2 starPos = hash22(id);
    float brightness = hash21(id + 137.0);

    // 只有部分格子有星星
    if (brightness < 0.72)
        return 0.0;
    brightness = (brightness - 0.72) / 0.28; // remap to 0~1

    float d = length(sub - starPos);
    float star = smoothstep(0.045, 0.0, d) * brightness;

    // 闪烁
    float twinkle = sin(hash11(id.x * 31.0 + id.y * 57.0) * TAU
        + time * (1.5 + hash11(id.x * 13.0) * 2.5));
    star *= 0.55 + 0.45 * twinkle;

    return star;
}

// ======================== Main Pixel Shader ========================

float4 PSVoidColonySky(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float t = uTime;
    float2 uv = coords;

    // 中心偏移并校正宽高比，使漩涡为圆形
    float2 c = (uv - float2(0.5, 0.48)) * float2(uAspectRatio, 1.0);
    float dist = length(c);
    float angle = atan2(c.y, c.x);

    // ====================================================================
    // Layer 1: 深渊底色 — 接近纯黑，微带暗红
    // ====================================================================
    float3 col = float3(0.01, 0.003, 0.008);
    // 远处略微提亮
    col += float3(0.012, 0.004, 0.008) * smoothstep(0.0, 1.0, dist);

    // ====================================================================
    // Layer 2: 漩涡旋臂 — 深红色旋转云层
    // 对数螺旋：角度随距离偏移产生旋臂结构
    // ====================================================================
    float spiralA = angle + dist * 4.2 - t * 0.18;
    float spiralB = angle - dist * 2.8 + t * 0.13; // 反向旋转层

    // 双旋臂（主）
    float mainArm = pow(max(sin(spiralA * 2.0) * 0.5 + 0.5, 0.0), 1.8);
    // 三旋臂（次，较淡）
    float secArm = pow(max(sin(spiralB * 3.0) * 0.5 + 0.5, 0.0), 2.5) * 0.3;
    float armBase = mainArm + secArm;

    // 在螺旋坐标系中采样流动噪声
    // 使用旋转后的笛卡尔坐标代替 angle/TAU，避免 atan2 在左侧 ±π 处的不连续接缝
    float va = t * 0.15;
    float vcos = cos(va);
    float vsin = sin(va);
    float2 rotC = float2(c.x * vcos - c.y * vsin, c.x * vsin + c.y * vcos);
    float2 vortexUV = float2(rotC.x * 1.8 + t * 0.022, rotC.y * 1.8 + dist * 0.8 - t * 0.045);
    float vNoise = fbm(vortexUV * 5.0, 4);
    float vDetail = fbm(vortexUV * 11.0 + 47.0, 3) * 0.25;
    float vortex = armBase * (vNoise + vDetail);

    // 径向衰减：中心暗洞和极远处淡出
    float vortexFade = smoothstep(0.04, 0.22, dist) * smoothstep(1.5, 0.35, dist);
    vortex *= vortexFade;

    // 漩涡着色：暗红 → 亮橙红
    float3 vortexCol = lerp(
        float3(0.28, 0.03, 0.015),
        float3(0.85, 0.2, 0.04),
        saturate(vortex * 2.2)
    );
    col += vortexCol * vortex;

    // ====================================================================
    // Layer 3: 能量裂隙 — 明亮的橙黄色裂缝/流
    // 使用脊噪声产生锐利裂缝纹理，沿螺旋方向旋转
    // ====================================================================
    float2 riftUV = c * 2.2;
    // 螺旋扭转
    float sw = dist * 3.2 - t * 0.25;
    float cs = cos(sw);
    float sn = sin(sw);
    riftUV = float2(riftUV.x * cs - riftUV.y * sn, riftUV.x * sn + riftUV.y * cs);

    float ridge = ridgedFbm(riftUV * 2.8, 4);

    // 锐利阈值产生裂缝形态
    float rift = smoothstep(0.55, 0.78, ridge);
    float riftCore = smoothstep(0.68, 0.9, ridge);

    // 裂隙沿旋臂更密集
    rift *= (0.35 + 0.65 * armBase);
    riftCore *= (0.3 + 0.7 * armBase);

    // 径向衰减
    float riftFade = smoothstep(0.025, 0.16, dist) * smoothstep(1.1, 0.2, dist);
    rift *= riftFade;
    riftCore *= riftFade;

    // 裂隙发光晕散（宽且柔和的底光）
    float riftGlow = smoothstep(0.38, 0.7, ridge) * riftFade * armBase * 0.35;
    col += float3(0.5, 0.08, 0.02) * riftGlow;

    // 裂隙本体色：橙边 → 黄白核心
    float3 riftCol = lerp(
        float3(1.0, 0.38, 0.05),
        float3(1.0, 0.82, 0.38),
        riftCore
    );
    col += riftCol * rift * 1.4;

    // ====================================================================
    // Layer 4: 域弯曲能量流 — 大尺度有机扭曲流
    // 补充旋涡中的宽大能量带（概念图中从中心向外延伸的火焰流）
    // ====================================================================
    float2 streamUV = c * 1.2 + float2(t * 0.03, -t * 0.02);
    float stream = warpedFbm(streamUV, t * 0.6);
    float streamEdge = smoothstep(0.48, 0.6, stream);
    float streamBright = smoothstep(0.56, 0.72, stream);

    float streamFade = smoothstep(0.06, 0.2, dist) * smoothstep(1.3, 0.3, dist);
    streamEdge *= streamFade * 0.5;
    streamBright *= streamFade * 0.4;

    col += float3(0.7, 0.15, 0.03) * streamEdge;
    col += float3(1.0, 0.55, 0.12) * streamBright;

    // ====================================================================
    // Layer 5: 星云 — 紫蓝色弥漫云
    // ====================================================================
    float neb1 = fbm(c * 1.6 + float2(100.0, 50.0) + t * 0.008, 4);
    neb1 = smoothstep(0.32, 0.68, neb1);
    float nebZone = smoothstep(0.3, 0.85, dist); // 边缘更浓
    col += float3(0.12, 0.04, 0.22) * neb1 * nebZone * 0.5;

    // 第二层星云：偏红紫
    float neb2 = fbm(c * 1.1 + float2(50.0, 80.0) - t * 0.006, 3);
    neb2 = smoothstep(0.38, 0.65, neb2);
    col += float3(0.18, 0.03, 0.12) * neb2 * nebZone * 0.35;

    // ====================================================================
    // Layer 6: 星辰 — 多层程序化星空
    // ====================================================================
    float s1 = starField(c + 200.0, 28.0, t);
    float s2 = starField(c + 500.0, 55.0, t);
    float s3 = starField(c + 800.0, 95.0, t);

    // 明亮区域压制星星
    float sDim = 1.0 - saturate(vortex * 3.5 + rift * 5.0 + streamEdge * 3.0 + neb1 * 1.5);
    sDim *= smoothstep(0.05, 0.2, dist); // 中心无星

    float3 starCol = float3(1.0, 0.92, 0.85) * s1 * 0.75
        + float3(0.92, 0.85, 1.0) * s2 * 0.45
        + float3(0.85, 0.88, 1.0) * s3 * 0.25;
    col += starCol * sDim;

    // ====================================================================
    // Layer 7: 中心暗域 + 边缘辉光环
    // ====================================================================
    float centerDark = smoothstep(0.0, 0.13, dist);
    col *= centerDark;

    // 暗域边缘的灼热辉光环
    float glowRing = exp(-(dist - 0.16) * (dist - 0.16) * 55.0) * 0.38;
    col += float3(0.9, 0.28, 0.06) * glowRing;

    // 极中心微弱的深红余辉
    float coreGlow = exp(-dist * dist * 80.0) * 0.08;
    col += float3(0.4, 0.05, 0.02) * coreGlow;

    // ====================================================================
    // Post: 柔和暗角 + 输出
    // ====================================================================
    float vignette = smoothstep(1.3, 0.45, dist);
    vignette = max(vignette, 0.12);
    col *= vignette;

    col *= uIntensity;

    return float4(saturate(col), 1.0);
}

technique VoidColonySky
{
    pass P0
    {
        PixelShader = compile ps_3_0 PSVoidColonySky();
    }
}
