// ============================================================================
// CyberPanel.fx v2 — SHPC赛博朋克対話框面板着色器
// 深暗紫底色 + 六角蜂窝结构边框(非矩形alpha遮罩) + 蓝色能量流高光
// 边缘溢出 + 故障闪烁 + CRT扫描线 + 扫掠光 + 暗角
// AlphaBlend模式 —— C#绘制扩展矩形，shader控制alpha形状
// ============================================================================

sampler uImage0 : register(s0);

float uTime;        // 全局动画时间（单调递增）
float uAlpha;       // 面板整体透明度 (0~1)
float2 uResolution; // 包含边距的完整绘制区域像素尺寸
float uEdgePad;     // 面板向四周扩展的像素数（用于六角溢出）

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
    f = f * f * (3.0 - 2.0 * f);

    float a = hash21(i);
    float b = hash21(i + float2(1.0, 0.0));
    float c = hash21(i + float2(0.0, 1.0));
    float d = hash21(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// floor-based mod（正确处理负值，等价于GLSL mod）
float2 hmod(float2 a, float2 b) { return a - b * floor(a / b); }

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 pixelPos = coords * uResolution;

    // ═══ 面板SDF（正值=在内部，负值=在外部） ═══
    float2 innerMin = float2(uEdgePad, uEdgePad);
    float2 innerMax = uResolution - float2(uEdgePad, uEdgePad);
    float panelSDF = min(
        min(pixelPos.x - innerMin.x, innerMax.x - pixelPos.x),
        min(pixelPos.y - innerMin.y, innerMax.y - pixelPos.y)
    );

    // 远离面板的像素直接丢弃
    if (panelSDF < -(uEdgePad + 2.0)) return float4(0, 0, 0, 0);

    // ═══ 六角网格（内联计算，需要cellCenter） ═══
    float hexScale = 0.065;
    float2 hexUV = pixelPos;
    // 微弱UV扰动（让网格呼吸/扭曲）
    float distortT = uTime * 0.4;
    float2 ds = pixelPos * 0.008;
    float ddxV = valueNoise(ds + float2(distortT, 0.0)) - 0.5;
    float ddyV = valueNoise(ds + float2(0.0, distortT * 0.7)) - 0.5;
    hexUV += float2(ddxV, ddyV) * 2.5;

    float2 hp = hexUV * hexScale;
    float2 hr = float2(1.0, 1.7320508);
    float2 hh = hr * 0.5;
    float2 ha = hmod(hp, hr) - hh;
    float2 hb = hmod(hp - hh, hr) - hh;
    float2 hg;
    if (dot(ha, ha) < dot(hb, hb))
        hg = ha;
    else
        hg = hb;

    float hexDist = max(abs(hg.x), abs(hg.y * 0.5773 + abs(hg.x) * 0.5));
    float2 cellId = hp - hg;
    float2 cellCenterPx = cellId / hexScale;
    float cellHash = hash21(cellId);
    float cellHash2 = hash21(cellId + float2(7.13, 3.71));

    float hexEdge = 1.0 - smoothstep(0.36, 0.44, hexDist);
    float hexLine = smoothstep(0.38, 0.42, hexDist) * (1.0 - smoothstep(0.42, 0.50, hexDist));

    // ═══ 单元格中心SDF（决定边缘alpha遮罩） ═══
    float cellSDF = min(
        min(cellCenterPx.x - innerMin.x, innerMax.x - cellCenterPx.x),
        min(cellCenterPx.y - innerMin.y, innerMax.y - cellCenterPx.y)
    );

    // 每个六角格随机"外伸"量（部分格子向外生长，形成蜂窝轮廓）
    float protrusion = (cellHash - 0.35) * 28.0;
    // ~20%的格子有动态伸缩动画
    float protAnim = sin(uTime * 1.2 + cellHash2 * 30.0) * 0.5 + 0.5;
    protrusion += step(0.8, cellHash2) * protAnim * 8.0;

    float effectiveBound = cellSDF + protrusion;

    // ═══ Alpha遮罩（非矩形六角蜂窝轮廓） ═══
    float cellAlpha;
    if (panelSDF > 18.0) {
        cellAlpha = 1.0;
    } else {
        cellAlpha = smoothstep(-2.0, 2.0, effectiveBound);
        // 确保面板内部仍保持不透明
        cellAlpha = max(cellAlpha, smoothstep(10.0, 18.0, panelSDF));
    }

    if (cellAlpha < 0.01) return float4(0, 0, 0, 0);

    // 内面板坐标（0-1），用于渐变/暗角等效果
    float2 innerSize = innerMax - innerMin;
    float2 innerCoords = saturate((pixelPos - innerMin) / innerSize);

    // ═══ 1. 基础渐变背景（深暗紫色） ═══
    float3 bgTop = float3(0.045, 0.018, 0.078);
    float3 bgBot = float3(0.020, 0.010, 0.048);
    float3 bg = lerp(bgTop, bgBot, innerCoords.y);

    // ═══ 2. 噪声表面（紫色调金属质感） ═══
    float n1 = valueNoise(pixelPos * 0.08);
    float n2 = valueNoise(pixelPos * 0.035 + 100.0);
    float surfVar = n1 * 0.6 + n2 * 0.4;
    bg *= 0.78 + surfVar * 0.44;
    bg += float3(0.010, -0.004, 0.016) * (surfVar - 0.5);

    // ═══ 3. 六角网格叠层（紫色骨架 + 蓝色能量流高光） ═══
    // 能量流方向波
    float2 flowDir = normalize(float2(1.0, 0.8));
    float flowPhase = dot(pixelPos * 0.012, flowDir) - uTime * 1.8;
    float flowWave = sin(flowPhase) * 0.5 + 0.5;
    float flowWave2 = sin(flowPhase * 0.6 + 1.2) * 0.5 + 0.5;
    float flowIntensity = flowWave * 0.7 + flowWave2 * 0.3;

    // 脉冲扫描波
    float2 ctr = uResolution * 0.5;
    float dfc = length(pixelPos - ctr);
    float md = length(uResolution * 0.5);
    float pp = dfc / md * 6.2832 - uTime * 2.0;
    float pulseScan = pow(max(sin(pp), 0.0), 4.0);

    // 六角网格线 — 紫色基底 + 蓝色能量流
    float lineGlow = 0.85 + flowIntensity * 0.5 + pulseScan * 0.4;
    bg += float3(0.050, 0.020, 0.100) * hexLine * lineGlow * 0.7;
    bg += float3(0.015, 0.050, 0.120) * hexLine * lineGlow * flowIntensity;

    // 六角单元格填充
    float cellFill = step(0.55, cellHash) * 0.25;
    bg += float3(0.030, 0.012, 0.060) * cellFill * hexEdge;
    bg += float3(0.008, 0.025, 0.055) * flowIntensity * step(0.4, cellHash2) * 0.18 * hexEdge;

    // 活跃格子脉动
    float cellPulse = sin(uTime * 1.8 + cellHash * 40.0) * 0.5 + 0.5;
    float activeCells = step(0.72, cellHash);
    float activeGlow = cellPulse * (0.7 + pulseScan * 0.6);
    bg += float3(0.015, 0.025, 0.085) * activeCells * activeGlow * hexEdge;

    // 能量节点高亮
    float nodeCell = step(0.93, cellHash);
    float nodeGlow = 0.6 + sin(uTime * 3.0 + cellHash2 * 20.0) * 0.4;
    bg += float3(0.010, 0.040, 0.110) * nodeCell * nodeGlow * hexEdge;

    // ═══ 4. 蜂窝边框辉光（边界处六角线条发光） ═══
    float borderProx = exp(-abs(cellSDF) * 0.06);
    // 紫色基底辉光
    bg += float3(0.055, 0.022, 0.110) * hexLine * borderProx * 2.0;
    // 蓝色能量流在边框上的叠加
    bg += float3(0.012, 0.045, 0.120) * hexLine * borderProx * flowIntensity * 1.5;
    // 有效边界处的锐利高光带
    float edgeHL = smoothstep(3.0, 0.0, abs(effectiveBound)) * hexEdge;
    bg += float3(0.035, 0.055, 0.140) * edgeHL;

    // ═══ 5. CRT扫描线 ═══
    float scanline = frac(pixelPos.y / 3.0);
    float scanDark = smoothstep(0.0, 0.18, scanline) * smoothstep(1.0, 0.82, scanline);
    bg *= 0.82 + 0.18 * scanDark;

    // ═══ 6. 面板分段线（紫色调） ═══
    float hSeg = frac(pixelPos.y / 60.0);
    float hLineSeg = 1.0 - smoothstep(0.0, 0.025, hSeg);
    float hReflect = smoothstep(0.025, 0.06, hSeg) * (1.0 - smoothstep(0.06, 0.10, hSeg));
    bg *= 1.0 - hLineSeg * 0.50;
    bg += float3(0.020, 0.012, 0.045) * hReflect * 0.40;

    float vLine1 = 1.0 - smoothstep(0.0, 0.005, abs(innerCoords.x - 0.28));
    float vLine2 = 1.0 - smoothstep(0.0, 0.005, abs(innerCoords.x - 0.76));
    bg += float3(0.018, 0.012, 0.050) * (vLine1 + vLine2) * 0.35;

    // ═══ 7. 扫掠光带（紫色全息光束） ═══
    float sweepPos = frac(uTime * 0.07);
    float sd = innerCoords.y - sweepPos;
    if (sd < -0.5) sd += 1.0;
    if (sd > 0.5) sd -= 1.0;
    float sweepCore = exp(-abs(sd) * 30.0);
    float sweepGlw = exp(-abs(sd) * 10.0);
    bg += float3(0.028, 0.015, 0.070) * sweepCore * 0.6;
    bg += float3(0.010, 0.012, 0.035) * sweepGlw * 0.25;
    // 扫掠经过时增亮六角网格
    bg += float3(0.008, 0.025, 0.060) * sweepGlw * hexLine * (1.5 + flowIntensity * 1.5);

    // ═══ 8. 暗角渐变 ═══
    float2 vig = innerCoords * 2.0 - 1.0;
    float vigMask = 1.0 - dot(vig * float2(0.45, 0.55), vig * float2(0.45, 0.55));
    vigMask = saturate(vigMask) * 0.35 + 0.65;
    bg *= vigMask;

    // ═══ 9. 故障闪烁（内部微故障 + 边缘强故障） ═══
    float edgeZone = smoothstep(15.0, 0.0, panelSDF) * step(-uEdgePad, panelSDF);
    float g1 = sin(uTime * 5.7);
    float g2 = sin(uTime * 11.3);
    float g3 = sin(uTime * 3.1);
    float glitchTrigger = g1 * g2 * g3;
    if (abs(glitchTrigger) > 0.85)
    {
        float seed = floor(uTime * 25.0);
        float gy = frac(sin(seed * 127.1 + 311.7) * 43758.5453);
        float gd = abs(innerCoords.y - gy);
        float gm = 1.0 - smoothstep(0.0, 0.015, gd);
        // 内部微故障（紫色调）
        bg += float3(0.025, 0.015, 0.065) * gm * 0.5;
        bg.r += gm * 0.005;
        bg.b -= gm * 0.003;
        // 边缘强故障（RGB色散 + alpha闪烁）
        if (edgeZone > 0.1)
        {
            float eg = gm * edgeZone;
            bg.r += eg * 0.08;
            bg.g -= eg * 0.03;
            bg.b += eg * 0.05;
            cellAlpha *= 1.0 - eg * 0.4 * step(0.5, cellHash2);
        }
    }

    // ═══ 10. 顶部高光 ═══
    float topGrad = 1.0 - smoothstep(0.0, 0.12, innerCoords.y);
    bg += float3(0.018, 0.010, 0.035) * topGrad * 0.3;

    float fa = uAlpha * cellAlpha;
    return float4(bg * fa, fa) * vertexColor;
}

technique Technique1
{
    pass CyberPanelPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
