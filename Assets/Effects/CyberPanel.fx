// ============================================================================
// CyberPanel.fx — 赛博朋克2077风格UI面板背景着色器
// 程序化生成深色面板背景：六角网格 + 噪声表面 + CRT扫描线 + 扫掠光带
// + 暗角 + 面板分段线 + 微故障 —— 应用于对话框面板矩形，AlphaBlend模式
// ============================================================================

sampler uImage0 : register(s0);

float uTime;        // 全局动画时间（单调递增）
float uAlpha;       // 面板整体透明度 (0~1)
float2 uResolution; // 面板像素尺寸（用于精确纹理密度）

// ── 程序化噪声工具 ──
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

// 2D value noise（带双线性插值）
float valueNoise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f); // smoothstep

    float a = hash21(i);
    float b = hash21(i + float2(1.0, 0.0));
    float c = hash21(i + float2(0.0, 1.0));
    float d = hash21(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// ── 六角网格 ──
// 返回: x=到最近六角中心距离, y=六角单元格ID的hash
float2 hexGrid(float2 p, float scale) {
    p *= scale;
    float2 r = float2(1.0, 1.7320508); // (1, sqrt(3))
    float2 h = r * 0.5;

    float2 a = fmod(p, r) - h;
    float2 b = fmod(p - h, r) - h;

    float2 g;
    if (dot(a, a) < dot(b, b))
        g = a;
    else
        g = b;

    float dist = max(abs(g.x), abs(g.y * 0.5773 + abs(g.x) * 0.5)); // hex distance
    float2 cellId = p - g;
    float cellHash = hash21(cellId);

    return float2(dist, cellHash);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 pixelPos = coords * uResolution;

    // ═══ 1. 基础渐变背景（深海蓝黑） ═══
    float3 bgTop = float3(0.028, 0.035, 0.072);
    float3 bgBot = float3(0.015, 0.022, 0.048);
    float3 bg = lerp(bgTop, bgBot, coords.y);

    // ═══ 2. 噪声表面变化（让面板有不均匀的"材质"感） ═══
    float n1 = valueNoise(pixelPos * 0.08);
    float n2 = valueNoise(pixelPos * 0.035 + 100.0);
    float surfaceVar = (n1 * 0.6 + n2 * 0.4); // 0~1
    // 低频明暗起伏 —— 模拟金属面板表面的不均匀反射
    bg *= 0.78 + surfaceVar * 0.44;
    // 叠加色彩变化（偏青/偏蓝交替）
    bg += float3(-0.008, 0.006, 0.014) * (surfaceVar - 0.5) * 1.0;

    // ═══ 3. 六角网格叠层（CP2077的标志性HUD纹理） ═══
    float hexScale = 0.065; // 网格密度
    float2 hexUV = pixelPos * float2(1.0, 1.0);
    float2 hex = hexGrid(hexUV, hexScale);
    float hexDist = hex.x;
    float hexCell = hex.y;

    // 六角边缘线（亮线，纹理骨架）
    float hexEdge = 1.0 - smoothstep(0.36, 0.44, hexDist);
    float hexLine = smoothstep(0.40, 0.43, hexDist) * (1.0 - smoothstep(0.43, 0.50, hexDist));
    // 六角网格线 —— 明显的青色骨架
    bg += float3(0.025, 0.060, 0.10) * hexLine * 0.85;
    // 六角单元格内部的明暗差异（填充感更强）
    float cellFill = step(0.65, hexCell) * 0.22;
    bg += float3(0.015, 0.038, 0.065) * cellFill * hexEdge;

    // 少量六角单元格有脉动高亮（随时间缓慢闪烁）
    float cellPulse = sin(uTime * 1.5 + hexCell * 40.0) * 0.5 + 0.5;
    float activeCells = step(0.82, hexCell); // ~18%的格子活跃
    bg += float3(0.015, 0.042, 0.075) * activeCells * cellPulse * hexEdge * 0.85;

    // ═══ 4. CRT扫描线（每3像素一条，更粗更可见） ═══
    float scanline = frac(pixelPos.y / 3.0);
    float scanDark = smoothstep(0.0, 0.18, scanline) * smoothstep(1.0, 0.82, scanline);
    bg *= 0.82 + 0.18 * scanDark;

    // ═══ 5. 面板分段线（水平+竖向薄线，划分HUD区域） ═══
    // 水平分段线 —— 每约60像素一条暗沟 + 下方高光反射
    float hSeg = frac(pixelPos.y / 60.0);
    float hLine = 1.0 - smoothstep(0.0, 0.025, hSeg);
    float hReflect = smoothstep(0.025, 0.06, hSeg) * (1.0 - smoothstep(0.06, 0.10, hSeg));
    bg *= 1.0 - hLine * 0.50; // 暗沟（更深）
    bg += float3(0.018, 0.038, 0.065) * hReflect * 0.40; // 反光（更亮）

    // 竖向分段线 —— 在x=30%和x=75%处有竖向分割
    float vLine1 = 1.0 - smoothstep(0.0, 0.005, abs(coords.x - 0.28));
    float vLine2 = 1.0 - smoothstep(0.0, 0.005, abs(coords.x - 0.76));
    bg += float3(0.015, 0.035, 0.060) * (vLine1 + vLine2) * 0.35;

    // ═══ 6. 扫掠光带（缓慢纵向扫过的全息光束） ═══
    float sweepPos = frac(uTime * 0.07);
    float sweepDist = coords.y - sweepPos;
    if (sweepDist < -0.5) sweepDist += 1.0;
    if (sweepDist > 0.5) sweepDist -= 1.0;
    float sweepCore = exp(-abs(sweepDist) * 30.0);
    float sweepGlow = exp(-abs(sweepDist) * 10.0);
    bg += float3(0.010, 0.045, 0.090) * sweepCore * 0.6;
    bg += float3(0.005, 0.018, 0.038) * sweepGlow * 0.25;
    // 扫掠经过时六角网格增亮
    bg += float3(0.012, 0.035, 0.060) * sweepGlow * hexLine * 2.5;

    // ═══ 7. 暗角渐变 ═══
    float2 vig = coords * 2.0 - 1.0;
    float vigMask = 1.0 - dot(vig * float2(0.45, 0.55), vig * float2(0.45, 0.55));
    vigMask = saturate(vigMask);
    vigMask = vigMask * 0.35 + 0.65;
    bg *= vigMask;

    // ═══ 8. 微故障条纹（间歇性闪烁水平亮条） ═══
    float g1 = sin(uTime * 5.7);
    float g2 = sin(uTime * 11.3);
    float g3 = sin(uTime * 3.1);
    float glitchTrigger = g1 * g2 * g3;
    if (abs(glitchTrigger) > 0.88)
    {
        float seed = floor(uTime * 25.0);
        float gy = frac(sin(seed * 127.1 + 311.7) * 43758.5453);
        float gd = abs(coords.y - gy);
        float gm = 1.0 - smoothstep(0.0, 0.012, gd);
        bg += float3(0.04, 0.09, 0.15) * gm * 0.6;
        // 故障时色偏（RGB位移模拟）
        float shift = gm * 0.006;
        bg.r += shift * 0.8;
        bg.b -= shift * 0.4;
    }

    // ═══ 9. 顶部高光渐变 ═══
    float topGrad = 1.0 - smoothstep(0.0, 0.12, coords.y);
    bg += float3(0.012, 0.022, 0.040) * topGrad * 0.3;

    return float4(bg * uAlpha, uAlpha) * vertexColor;
}

technique Technique1
{
    pass CyberPanelPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
