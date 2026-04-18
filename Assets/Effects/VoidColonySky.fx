// ============================================================================
// VoidColonySky.fx — 虚空聚落天空着色器
// 亚空间末日风格全屏天空背景
// 多层视差旋涡 + 能量裂隙 + 雷电 + 星云 + 星辰 + 中心炽热火眼
// 全程序化生成，无需外部纹理依赖
// ============================================================================

sampler uImage0 : register(s0);

float uTime;
float uIntensity;       //整体强度0~1，淡入淡出
float uAspectRatio;     //屏幕宽高比screenWidth/screenHeight

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

// ======================== Helpers ========================

//二维旋转
float2 rot2(float2 p, float a)
{
    float c = cos(a);
    float s = sin(a);
    return float2(p.x * c - p.y * s, p.x * s + p.y * c);
}

//雷电脉冲，沿放射方向产生分支闪电
//返回0~1强度，t用于触发与衰减
float lightningBolt(float2 p, float seed, float time)
{
    //每条闪电按周期触发，周期内只活跃很短时间
    float period = 1.4;
    float phase = time / period + seed;
    float idx = floor(phase);
    float local = frac(phase);
    //每条闪电随机寿命窗口
    float life = smoothstep(0.0, 0.06, local) * smoothstep(0.4, 0.18, local);
    if (life < 0.001)
        return 0.0;

    //每次触发随机抖动方向，使闪电不固定
    float jitter = (hash11(idx * 17.7 + seed * 3.3) - 0.5) * 0.6;
    p = rot2(p, jitter);

    //沿x轴拉长，沿y轴变窄，制作放射状细长闪电
    float ang = atan2(p.y, p.x);
    float r = length(p);
    //沿径向方向偏移采样，产生抖动折线
    float n = ridgedFbm(float2(r * 6.0 + idx * 11.3, seed * 7.0), 4);
    float jag = (n - 0.5) * 0.18;
    float bolt = exp(-pow(abs(ang + jag) * (3.5 + 12.0 * (1.0 - r)), 2.0));
    //径向衰减，越往外越淡
    bolt *= smoothstep(0.04, 0.08, r) * smoothstep(0.95, 0.3, r);
    return bolt * life;
}

// ======================== Main Pixel Shader ========================

float4 PSVoidColonySky(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float t = uTime;
    float2 uv = coords;

    //中心偏移并校正宽高比，使漩涡为圆形，中心略偏上对应概念图构图
    float2 c = (uv - float2(0.5, 0.46)) * float2(uAspectRatio, 1.0);
    float dist = length(c);
    float angle = atan2(c.y, c.x);
    //深度因子，0为远景1为近景，用于做大气透视
    float depth = saturate(1.0 - dist * 0.85);

    // ====================================================================
    //Layer 0: 深渊底色，墨黑微带血色
    // ====================================================================
    float3 col = float3(0.006, 0.002, 0.004);

    // ====================================================================
    //Layer 1: 远景星云背景，紫蓝幽冷的弥漫云团，慢速流动
    //定位为最深的一层，提供基础氛围与色彩对比
    // ====================================================================
    float2 farUV = c * 0.85 + float2(t * 0.012, -t * 0.008);
    float farNeb = fbm(farUV * 1.6 + 17.0, 4);
    farNeb = smoothstep(0.35, 0.85, farNeb);
    float farZone = smoothstep(0.18, 0.95, dist);
    //深紫到冷蓝
    float3 farCol = lerp(float3(0.10, 0.03, 0.18), float3(0.04, 0.06, 0.20), farNeb);
    col += farCol * farNeb * farZone * 0.55;

    //远处微弱的暗红弥漫，模拟血色霞光
    float farRed = fbm(c * 1.0 + float2(70.0, 30.0) - t * 0.005, 3);
    farRed = smoothstep(0.4, 0.75, farRed);
    col += float3(0.16, 0.02, 0.05) * farRed * farZone * 0.45;

    // ====================================================================
    //Layer 2: 远视差星辰，最暗最密，用于推开纵深
    // ====================================================================
    float starsFar = starField(c * 1.0 + 800.0, 110.0, t) * 0.55;
    float3 starsFarCol = float3(0.78, 0.82, 1.0);

    // ====================================================================
    //Layer 3: 中景反向旋转云层，深红基调，提供旋涡背板
    // ====================================================================
    float midRot = -t * 0.07;
    float2 midC = rot2(c, midRot);
    float2 midUV = float2(midC.x * 1.4, midC.y * 1.4 + dist * 0.5);
    float midSpiral = angle - dist * 2.6 + t * 0.11;
    float midArmMask = pow(max(sin(midSpiral * 2.0) * 0.5 + 0.5, 0.0), 1.4);
    float midNoise = fbm(midUV * 3.6 - t * 0.04, 4);
    float midClouds = midArmMask * midNoise;
    float midFade = smoothstep(0.05, 0.28, dist) * smoothstep(1.5, 0.45, dist);
    midClouds *= midFade;
    //中景色：暗血红到玫红
    float3 midCol = lerp(float3(0.18, 0.02, 0.02), float3(0.55, 0.10, 0.08), saturate(midClouds * 1.8));
    col += midCol * midClouds * 1.1;

    // ====================================================================
    //Layer 4: 主漩涡旋臂，对数螺旋两组叠加，富含细节噪声
    // ====================================================================
    float spiralA = angle + dist * 4.4 - t * 0.20;
    float spiralB = angle - dist * 3.0 + t * 0.14;
    float mainArm = pow(max(sin(spiralA * 2.0) * 0.5 + 0.5, 0.0), 1.6);
    float secArm = pow(max(sin(spiralB * 3.0) * 0.5 + 0.5, 0.0), 2.4) * 0.45;
    float armBase = mainArm + secArm;

    float2 rotC = rot2(c, t * 0.16);
    float2 vortexUV = float2(rotC.x * 1.9 + t * 0.025, rotC.y * 1.9 + dist * 0.9 - t * 0.05);
    float vNoise = fbm(vortexUV * 5.5, 4);
    float vDetail = fbm(vortexUV * 12.0 + 47.0, 3) * 0.28;
    float vortex = armBase * (vNoise + vDetail);

    float vortexFade = smoothstep(0.05, 0.22, dist) * smoothstep(1.5, 0.35, dist);
    vortex *= vortexFade;

    //漩涡渐变着色：暗赤到炽橙
    float3 vortexCol = lerp(
        float3(0.32, 0.04, 0.018),
        float3(0.95, 0.28, 0.06),
        saturate(vortex * 2.2)
    );
    col += vortexCol * vortex * 1.05;

    // ====================================================================
    //Layer 5: 域弯曲能量流，宽大有机扭曲带，强化旋涡的能量流向
    // ====================================================================
    float2 streamUV = c * 1.15 + float2(t * 0.03, -t * 0.02);
    float stream = warpedFbm(streamUV, t * 0.6);
    float streamEdge = smoothstep(0.46, 0.6, stream);
    float streamBright = smoothstep(0.56, 0.74, stream);
    float streamFade = smoothstep(0.06, 0.22, dist) * smoothstep(1.3, 0.3, dist);
    streamEdge *= streamFade * 0.55;
    streamBright *= streamFade * 0.45;
    col += float3(0.78, 0.18, 0.04) * streamEdge;
    col += float3(1.0, 0.6, 0.14) * streamBright;

    // ====================================================================
    //Layer 6: 锐利能量裂隙，对应概念图旋臂中的明亮缝光
    // ====================================================================
    float2 riftUV = c * 2.2;
    riftUV = rot2(riftUV, dist * 3.2 - t * 0.25);
    float ridge = ridgedFbm(riftUV * 2.8, 4);
    float rift = smoothstep(0.56, 0.78, ridge);
    float riftCore = smoothstep(0.7, 0.92, ridge);
    rift *= (0.32 + 0.68 * armBase);
    riftCore *= (0.28 + 0.72 * armBase);
    float riftFade = smoothstep(0.03, 0.16, dist) * smoothstep(1.1, 0.22, dist);
    rift *= riftFade;
    riftCore *= riftFade;

    float riftGlow = smoothstep(0.38, 0.7, ridge) * riftFade * armBase * 0.42;
    col += float3(0.55, 0.10, 0.02) * riftGlow;
    float3 riftCol = lerp(float3(1.0, 0.42, 0.06), float3(1.0, 0.88, 0.45), riftCore);
    col += riftCol * rift * 1.45;

    // ====================================================================
    //Layer 7: 放射雷电，从核心向外随机抖动闪烁，强化体积感
    // ====================================================================
    float bolt = 0.0;
    bolt += lightningBolt(c, 0.13, t * 1.3);
    bolt += lightningBolt(rot2(c, 1.05), 0.41, t * 1.3 + 0.6);
    bolt += lightningBolt(rot2(c, -1.27), 0.77, t * 1.3 + 1.1);
    bolt += lightningBolt(rot2(c, 2.3), 0.29, t * 1.3 + 1.7);
    bolt += lightningBolt(rot2(c, -2.55), 0.92, t * 1.3 + 2.3);
    bolt = saturate(bolt);
    //雷电核心炽白，外围橙红辉散
    col += float3(0.7, 0.18, 0.05) * bolt * 0.9;
    col += float3(1.0, 0.85, 0.6) * pow(bolt, 3.0) * 1.6;

    // ====================================================================
    //Layer 8: 中景视差星辰，中等亮度
    // ====================================================================
    float starsMid = starField(c * 1.0 + 400.0, 60.0, t) * 0.7;
    float3 starsMidCol = float3(0.95, 0.92, 1.0);

    // ====================================================================
    //Layer 9: 近景视差星辰，更亮更稀，伴轻微闪烁
    // ====================================================================
    float starsNear = starField(c * 1.0 + 100.0, 32.0, t) * 0.95;
    float3 starsNearCol = float3(1.0, 0.95, 0.86);

    //综合星辰，被亮区压制，中心区域无星
    float starMask = 1.0 - saturate(vortex * 4.0 + rift * 5.0 + streamEdge * 3.0
        + bolt * 6.0 + midClouds * 1.8);
    starMask *= smoothstep(0.06, 0.22, dist);
    col += (starsFarCol * starsFar + starsMidCol * starsMid + starsNearCol * starsNear) * starMask;

    // ====================================================================
    //Layer 10: 中心炽热火眼，对应概念图中央竖直状的烈焰核心
    //不再是纯黑暗洞，而是亮白炽核外披橙红
    // ====================================================================
    //核心位置略微跳动模拟燃烧脉动
    float pulse = 0.92 + 0.08 * sin(t * 4.7) + 0.04 * sin(t * 11.3 + 1.7);
    //先做圆形发光底
    float coreA = exp(-dist * dist * 240.0);
    //再叠加竖直拉长的火焰形态，对应概念图中竖向劈裂的火眼
    float2 fc = c * float2(2.4, 0.85);
    float fAng = atan2(fc.y, fc.x);
    float fR = length(fc);
    float flameNoise = fbm(float2(fAng * 1.8, fR * 5.0 - t * 1.2), 4);
    float flameShape = exp(-fR * fR * 35.0) * (0.55 + 0.55 * flameNoise);
    float flameMid = exp(-fR * fR * 90.0) * (0.7 + 0.4 * flameNoise);
    float flameHot = exp(-fR * fR * 260.0);

    float3 coreCol = float3(0.0, 0.0, 0.0);
    coreCol += float3(0.55, 0.08, 0.02) * flameShape * 1.4;
    coreCol += float3(1.0, 0.45, 0.10) * flameMid * 1.6;
    coreCol += float3(1.0, 0.92, 0.65) * flameHot * 2.4;
    coreCol += float3(0.95, 0.30, 0.06) * coreA * 0.55;
    coreCol *= pulse;
    col += coreCol;

    //火眼外缘的灼热环，分隔核心和外圈漩涡
    float ringR = 0.165;
    float glowRing = exp(-(dist - ringR) * (dist - ringR) * 70.0) * 0.55;
    col += float3(1.0, 0.35, 0.08) * glowRing * pulse;
    //更远处的暗赤光晕
    float outerHalo = exp(-(dist - 0.32) * (dist - 0.32) * 9.0) * 0.18;
    col += float3(0.5, 0.06, 0.02) * outerHalo;

    // ====================================================================
    //Layer 11: 大气透视雾，越靠近水平边缘越偏冷蓝紫，模拟空间深度
    // ====================================================================
    float3 fogCol = float3(0.06, 0.03, 0.10);
    float fogAmt = 1.0 - depth;
    fogAmt = pow(fogAmt, 2.2) * 0.35;
    col = lerp(col, fogCol, fogAmt);

    // ====================================================================
    //Layer 12: 漂浮尘埃，用细噪声做体积粒子的暗示
    // ====================================================================
    float dust = fbm(c * 12.0 + float2(t * 0.05, -t * 0.04), 3);
    dust = smoothstep(0.62, 0.86, dust) * 0.18;
    dust *= smoothstep(0.08, 0.4, dist) * smoothstep(1.4, 0.4, dist);
    col += float3(0.45, 0.18, 0.08) * dust;

    // ====================================================================
    //Post: 暗角 + 色彩调整 + 输出
    // ====================================================================
    //中心保护：避免雾和暗角吃掉火眼亮度
    float vignette = smoothstep(1.35, 0.4, dist);
    vignette = lerp(vignette, 1.0, exp(-dist * dist * 28.0));
    vignette = max(vignette, 0.1);
    col *= vignette;

    //轻微对比度提升，让火光更醒目
    col = pow(max(col, 0.0), 0.95);

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
