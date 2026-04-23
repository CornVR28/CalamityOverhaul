// ============================================================================
// BrimstoneDialogueBox.fx 至尊灾厄硫磺火对话框专属着色器
// 核心视觉:黑曜熔岩底板 + 底部升腾火焰墙 + 顶部倒悬火舌
// 熔岩裂纹发光脉络 + 游离余烬 + 热浪畸变 + 脉动火焰边框 + 暗角
// 中央水平带刻意压暗,保证文字可读性
// AlphaBlend模式,shader自行控制alpha形状
// ============================================================================

sampler uImage0 : register(s0);

float uTime;
float uAlpha;
float2 uResolution;
float uEdgePad;
float uInfernoPulse;    //0~1火焰整体强度脉动(由CPU提供)

// ============================================================================
// 基础工具函数
// ============================================================================
#define PI 3.14159265
#define TAU 6.28318530

float hash11(float p) {
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

float hash21(float2 p) {
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 hash22(float2 p) {
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac(float2((p3.x + p3.y) * p3.z, (p3.x + p3.z) * p3.y));
}

float valueNoise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + float2(1.0, 0.0));
    float c = hash21(i + float2(0.0, 1.0));
    float d = hash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

//四阶fbm
float fbm4(float2 p) {
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++) {
        v += a * valueNoise(p);
        p = p * 2.11 + float2(1.7, 9.2);
        a *= 0.5;
    }
    return v;
}

//域扭曲fbm,制造有机火焰形态
float warpedFbm(float2 p, float t) {
    float2 q = float2(fbm4(p), fbm4(p + float2(5.2, 1.3)));
    float2 r = float2(fbm4(p + 3.0 * q + float2(1.7, 9.2) + t * 0.35),
                      fbm4(p + 3.0 * q + float2(8.3, 2.8) - t * 0.28));
    return fbm4(p + 3.5 * r);
}

//简易Worley式距离场,用于熔岩裂纹
float worley(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    float minD = 1.0;
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            float2 g = float2(x, y);
            float2 o = hash22(i + g);
            float2 r = g + o - f;
            float d = dot(r, r);
            minD = min(minD, d);
        }
    }
    return sqrt(minD);
}

// ============================================================================
// 主色板
// ============================================================================
static const float3 COL_VOID   = float3(0.010, 0.004, 0.004); //虚空黑
static const float3 COL_ASH    = float3(0.055, 0.020, 0.015); //灰烬深褐
static const float3 COL_MAGMA  = float3(0.220, 0.055, 0.025); //熔岩暗红
static const float3 COL_EMBER  = float3(0.900, 0.340, 0.110); //余烬橙
static const float3 COL_FLAME  = float3(1.000, 0.620, 0.200); //火焰亮橙
static const float3 COL_WHITE  = float3(1.000, 0.900, 0.720); //火心白

// ============================================================================
// 火焰场:输入归一化到[0,1]的(u,v),vAlong=0=焰根 1=焰尖
// 返回火焰强度(0~1)
// ============================================================================
float flameField(float2 uv, float vAlong, float t, float seedOffset) {
    //向火焰顶部的uv流动(减少y坐标使纹样向上漂移)
    float2 puv = uv * float2(2.2, 2.6);
    puv.y -= t * 1.35;
    puv.x += sin(t * 0.6 + uv.y * 3.0) * 0.12;
    puv += seedOffset;

    //域扭曲湍流
    float n = warpedFbm(puv, t);
    //纵向衰减:根部饱和,尖端消散
    float taper = smoothstep(1.0, 0.05, vAlong);
    //焰尖稀疏化
    float flicker = n * taper;
    //中心聚焦,边缘减弱(水平方向两端压暗,焰墙更聚集)
    float xFall = 1.0 - pow(abs(uv.x * 2.0 - 1.0), 2.2);
    flicker *= 0.5 + xFall * 0.5;
    return saturate(pow(flicker, 1.35) * 2.1);
}

// ============================================================================
// 主片段
// ============================================================================
float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 pixelPos = coords * uResolution;
    float2 innerMin = float2(uEdgePad, uEdgePad);
    float2 innerMax = uResolution - float2(uEdgePad, uEdgePad);
    float2 innerSize = innerMax - innerMin;
    float2 center = uResolution * 0.5;
    float2 halfSize = innerSize * 0.5;

    //圆角矩形SDF
    float2 dd = abs(pixelPos - center) - halfSize;
    float cornerR = 8.0;
    float panelSDF = length(max(dd, 0.0)) + min(max(dd.x, dd.y), 0.0) - cornerR;

    //面板外廓alpha
    float edgeAlpha = 1.0 - smoothstep(-1.0, 2.0, panelSDF);
    if (panelSDF > uEdgePad + 6.0) return float4(0, 0, 0, 0);

    //归一化内部UV
    float2 uv = saturate((pixelPos - innerMin) / innerSize);
    float t = uTime;

    // ============================================================
    // 1 黑曜熔岩底色
    // 纵向由中央深渊向上下略亮,水平方向从边缘向中心压暗一点
    // 这样中央仍偏暗,保证文字区可读
    // ============================================================
    float vCenter = abs(uv.y - 0.5) * 2.0;
    float3 bg = lerp(COL_ASH * 0.6, COL_VOID, 1.0 - vCenter * vCenter);
    //底部更热一点,顶部更冷
    bg = lerp(bg, COL_MAGMA * 0.35, pow(uv.y, 1.8) * 0.55);
    bg = lerp(bg, COL_ASH * 0.25, pow(1.0 - uv.y, 2.2) * 0.35);

    // ============================================================
    // 2 熔岩裂纹:Worley裂纹底纹,会随uTime缓慢呼吸
    // 在整个面板下层弥漫,中央区更弱防止干扰文字
    // ============================================================
    float2 crackUV = (pixelPos + float2(0, t * 4.0)) * 0.020;
    float crackD = worley(crackUV);
    float crack = 1.0 - smoothstep(0.0, 0.06, crackD);
    //呼吸
    float crackBreath = sin(t * 1.6 + crackUV.x * 0.8) * 0.5 + 0.5;
    crack *= 0.55 + crackBreath * 0.45;
    //中央水平带裂纹亮度压低
    float midBand = 1.0 - smoothstep(0.15, 0.55, vCenter);
    crack *= lerp(1.0, 0.35, midBand);
    bg += COL_MAGMA * crack * 0.55;
    bg += COL_EMBER * crack * crackBreath * 0.18;

    // ============================================================
    // 3 底部升腾火焰墙:主视觉焦点
    // 使用底部局部UV;火焰只在下部约45%高度区绘制
    // ============================================================
    float bottomBand = smoothstep(0.38, 1.0, uv.y); //[0,1] 底部强
    float vBottom = (uv.y - 0.38) / 0.62; //重新归一
    if (bottomBand > 0.001) {
        float f1 = flameField(float2(uv.x, vBottom), 1.0 - vBottom, t, 0.0);
        float f2 = flameField(float2(uv.x + 0.17, vBottom * 1.08), 1.0 - vBottom, t * 1.15 + 1.7, 3.3);
        float fMain = max(f1, f2 * 0.85);

        //颜色分层:根部熔岩橙,中段亮橙,尖端火心白
        float3 flameCol = lerp(COL_MAGMA * 1.2, COL_EMBER, saturate(fMain * 1.2));
        flameCol = lerp(flameCol, COL_FLAME, saturate((fMain - 0.35) * 1.6));
        flameCol = lerp(flameCol, COL_WHITE, saturate((fMain - 0.78) * 2.5));

        float intensity = fMain * bottomBand * (0.85 + uInfernoPulse * 0.35);
        bg += flameCol * intensity;

        //根部辉光带(底边热光晕)
        float rootGlow = pow(saturate(uv.y - 0.72) / 0.28, 2.0);
        bg += COL_MAGMA * rootGlow * (0.45 + uInfernoPulse * 0.35);
    }

    // ============================================================
    // 4 顶部倒悬火舌:弱化版,制造对称压迫感
    // 只覆盖上部约25%高度
    // ============================================================
    float topBand = 1.0 - smoothstep(0.0, 0.28, uv.y);
    if (topBand > 0.001) {
        float vTop = 1.0 - uv.y / 0.28;
        float tf = flameField(float2(uv.x * 1.15, vTop), 1.0 - vTop, t * 0.9 + 5.0, 7.7);
        float3 flameCol = lerp(COL_ASH * 0.8, COL_EMBER, saturate(tf));
        flameCol = lerp(flameCol, COL_FLAME, saturate((tf - 0.45) * 1.5));
        float intensity = tf * topBand * (0.55 + uInfernoPulse * 0.25);
        bg += flameCol * intensity * 0.75;
    }

    // ============================================================
    // 5 热浪畸变纹路(水平方向flame-like条纹,柔和)
    // ============================================================
    float heat = sin((pixelPos.y + sin(pixelPos.x * 0.045 + t * 1.8) * 6.0) * 0.12 + t * 2.5);
    heat = heat * 0.5 + 0.5;
    heat = pow(heat, 4.0);
    //只在上下两侧附近显示,中央区几乎不扰动
    float heatBand = smoothstep(0.3, 0.0, uv.y) + smoothstep(0.7, 1.0, uv.y);
    bg += COL_EMBER * heat * heatBand * 0.06;

    // ============================================================
    // 6 游离余烬粒子(shader内,双层)
    // ============================================================
    //第一层:较多小粒子,缓慢上升
    float2 g1 = floor(pixelPos / 34.0);
    float s1 = hash21(g1);
    float life1 = frac(s1 * 5.97 + t * (0.25 + s1 * 0.20));
    float2 p1 = (g1 + 0.5) * 34.0 + (hash22(g1) - 0.5) * 26.0;
    p1.y -= life1 * 55.0;
    float d1 = length(pixelPos - p1);
    float sz1 = 1.4 + s1 * 1.6;
    float em1 = (1.0 - smoothstep(0.0, sz1, d1)) * sin(life1 * PI) * step(0.55, s1);
    bg += COL_FLAME * em1 * 0.8;
    //外辉
    float eg1 = exp(-d1 * 0.18) * sin(life1 * PI) * step(0.55, s1);
    bg += COL_EMBER * eg1 * 0.25;

    //第二层:稀疏大粒子
    float2 g2 = floor(pixelPos / 72.0);
    float s2 = hash21(g2 + 71.0);
    float life2 = frac(s2 * 9.13 + t * (0.14 + s2 * 0.10));
    float2 p2 = (g2 + 0.5) * 72.0 + (hash22(g2 + 13.0) - 0.5) * 50.0;
    p2.y -= life2 * 85.0;
    float d2 = length(pixelPos - p2);
    float sz2 = 2.0 + s2 * 2.6;
    float em2 = (1.0 - smoothstep(0.0, sz2, d2)) * sin(life2 * PI) * step(0.70, s2);
    bg += COL_WHITE * em2 * 0.55;
    float eg2 = exp(-d2 * 0.08) * sin(life2 * PI) * step(0.70, s2);
    bg += COL_EMBER * eg2 * 0.22;

    // ============================================================
    // 7 中央文字区轻度压暗:提升文字可读性
    // 以"烬光晕"形式压暗,而非硬切
    // ============================================================
    float textBandMask = smoothstep(0.18, 0.42, vCenter); //中央=0 外侧=1
    textBandMask = 1.0 - textBandMask;
    //中央再额外压暗25%
    bg *= lerp(1.0, 0.72, textBandMask);
    //同时在中央叠一个微弱暗红余温,保留氛围
    bg += COL_MAGMA * textBandMask * 0.035;

    // ============================================================
    // 8 脉动火焰边框:内缘发光 + 外缘暖光
    // ============================================================
    //内缘:距离板边向内12px以内发光
    float innerDist = max(-panelSDF - 0.0, 0.0);
    float rim = exp(-innerDist * 0.32);
    float rimPulse = 0.75 + sin(t * 2.0) * 0.25;
    bg += COL_EMBER * rim * 0.55 * rimPulse;
    bg += COL_FLAME * rim * rim * 0.35 * rimPulse;

    //距离边缘4px以内的"焰线"
    float edgeLine = exp(-innerDist * innerDist * 0.6);
    bg += COL_WHITE * edgeLine * 0.18 * rimPulse;

    //沿着边缘的爬行火焰:UV坐标取到沿周长运动的参数
    //用panelSDF等值线的梯度方向作近似;这里用uv.x+uv.y简化制造爬行
    float crawl = sin((uv.x * 6.0 + uv.y * 6.0 + t * 1.6) * TAU) * 0.5 + 0.5;
    bg += COL_EMBER * rim * crawl * 0.25;

    // ============================================================
    // 9 浮雕斜面:从左上打光的金属厚度感
    // ============================================================
    float bevelW = 10.0;
    float bevelMask = saturate(-panelSDF / bevelW);
    bevelMask = 1.0 - bevelMask;
    bevelMask *= bevelMask;
    float2 lightDir = normalize(float2(0.6, -0.7));
    float2 edgeN = normalize(pixelPos - center + 0.0001);
    float bevelLight = dot(edgeN, lightDir) * 0.5 + 0.5;
    bg += lerp(COL_VOID, COL_EMBER * 0.85, bevelLight) * bevelMask * 0.45;
    float glint = bevelMask * pow(saturate(bevelLight), 10.0);
    bg += COL_WHITE * glint * 0.35;

    //内凹凹槽
    float grooveD = abs(-panelSDF - bevelW);
    float groove = exp(-grooveD * grooveD * 0.18) * 0.18;
    bg -= float3(0.025, 0.015, 0.012) * groove;

    // ============================================================
    // 10 暗角与扫掠光带
    // ============================================================
    //全域向上的扫掠,模拟火焰风暴偶尔卷过
    float swPhase = frac(t * 0.035);
    float swDist = uv.y - (1.0 - swPhase);
    if (swDist < -0.5) swDist += 1.0;
    if (swDist > 0.5) swDist -= 1.0;
    float sweep = exp(-abs(swDist) * 8.0);
    bg += COL_EMBER * sweep * 0.08;

    //暗角
    float2 vig = uv * 2.0 - 1.0;
    float vigStr = dot(vig * float2(0.45, 0.55), vig * float2(0.45, 0.55));
    bg *= saturate(1.0 - vigStr) * 0.35 + 0.65;

    // ============================================================
    // 11 顶部薄反光
    // ============================================================
    float topRef = 1.0 - smoothstep(0.0, 0.10, uv.y);
    bg += COL_EMBER * topRef * 0.05;

    // ============================================================
    // 12 细颗粒噪点,模拟余烬尘埃
    // ============================================================
    float dust = hash21(pixelPos + t * 40.0) * 0.05;
    bg *= 1.0 - dust * 0.6;

    // ============================================================
    // 输出
    // ============================================================
    float fa = uAlpha * edgeAlpha;
    //让火焰亮部略微突破alpha(避免高光被alpha裁掉)
    float emitBoost = saturate((max(bg.r, max(bg.g, bg.b)) - 0.6) * 0.8);
    fa = saturate(fa + emitBoost * edgeAlpha * 0.25);
    return float4(bg * fa, fa) * vertexColor;
}

technique Technique1
{
    pass BrimstoneDialogueBoxPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
