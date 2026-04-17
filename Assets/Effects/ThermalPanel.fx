// ============================================================================
// ThermalPanel.fx — 热力发电机面板着色器
// 工业金属底板 + 热能反应渐变 + 锈蚀噪声纹理 + 扫描线
// + 燃烧脉冲 + 热浪光效 + 暗角 + 角落标记
// uTemperature: 温度比例 0~1，控制整体暖色偏移和热辉光
// uBurnIntensity: 燃烧强度 0~1，控制火焰脉冲和底部光晕
// ============================================================================

sampler uImage0 : register(s0);

float uTime;
float uAlpha;
float2 uResolution;
float uEdgePad;
float uTemperature;
float uBurnIntensity;

// ─── 噪声工具 ───
float hash21(float2 p) {
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
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

float fbm2(float2 p) {
    float v = 0.0;
    v += valueNoise(p) * 0.5;
    v += valueNoise(p * 2.03 + 17.0) * 0.25;
    v += valueNoise(p * 4.01 + 43.0) * 0.125;
    return v / 0.875;
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 pixelPos = coords * uResolution;
    float2 innerMin = float2(uEdgePad, uEdgePad);
    float2 innerMax = uResolution - float2(uEdgePad, uEdgePad);
    float2 innerSize = innerMax - innerMin;

    // ═══ 圆角矩形SDF ═══
    float2 center = uResolution * 0.5;
    float2 halfSize = innerSize * 0.5;
    float2 d = abs(pixelPos - center) - halfSize;
    float cornerR = 5.0;
    float panelSDF = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - cornerR;

    if (panelSDF > uEdgePad + 2.0) return float4(0, 0, 0, 0);

    float edgeAlpha = 1.0 - smoothstep(-1.0, 2.5, panelSDF);
    if (edgeAlpha < 0.01) return float4(0, 0, 0, 0);

    float2 uv = saturate((pixelPos - innerMin) / innerSize);
    float t = uTemperature;
    float burn = uBurnIntensity;

    // ═══ 1. 底色渐变（冷工业→热锻造） ═══
    float3 coldTop = float3(0.045, 0.032, 0.028);
    float3 coldBot = float3(0.020, 0.015, 0.012);
    float3 hotTop  = float3(0.088, 0.042, 0.022);
    float3 hotBot  = float3(0.055, 0.028, 0.016);

    float3 top = lerp(coldTop, hotTop, t);
    float3 bot = lerp(coldBot, hotBot, t);
    float3 bg = lerp(top, bot, uv.y);

    // ═══ 2. 拉丝金属纹理 ═══
    float brush = valueNoise(pixelPos * float2(0.05, 0.16));
    float brushCoarse = valueNoise(pixelPos * 0.022 + 150.0);
    bg *= 0.74 + (brush * 0.55 + brushCoarse * 0.28) * 0.52;

    // 热色偏移
    float3 warmTint = float3(0.018, 0.006, -0.004);
    bg += warmTint * t * (brush - 0.5);

    // ═══ 3. 锈蚀噪声花纹 ═══
    float rust = fbm2(pixelPos * 0.018 + float2(uTime * 0.2, uTime * 0.08));
    float3 rustColor = float3(0.14, 0.06, 0.025);
    bg += rustColor * rust * 0.10 * (0.3 + t * 0.7);

    // ═══ 4. 热能脉络网（两条水平脉冲线） ═══
    float circuitAccum = 0.0;
    float lineY1 = innerMin.y + innerSize.y * 0.35;
    float lineY2 = innerMin.y + innerSize.y * 0.72;
    float distY1 = abs(pixelPos.y - lineY1);
    float distY2 = abs(pixelPos.y - lineY2);
    float line1 = 1.0 - smoothstep(0.0, 2.5, distY1);
    float line2 = 1.0 - smoothstep(0.0, 2.5, distY2);
    float pulse1 = sin((pixelPos.x * 0.025 - uTime * 1.5)) * 0.5 + 0.5;
    float pulse2 = sin((pixelPos.x * 0.025 + uTime * 1.2)) * 0.5 + 0.5;
    circuitAccum += line1 * pulse1 + line2 * pulse2;

    float3 pulseColor = lerp(float3(0.12, 0.06, 0.03), float3(0.35, 0.14, 0.06), t);
    bg += pulseColor * circuitAccum * 0.18 * (0.4 + t * 0.6);

    // ═══ 5. 底部热浪光晕（受温度和燃烧强度影响） ═══
    float heatGlow = pow(max(uv.y, 0.0), 2.5) * t;
    float3 heatColor = float3(0.45, 0.18, 0.06);
    bg += heatColor * heatGlow * 0.35;

    // 燃烧脉冲
    float burnPulse = sin(uTime * 3.5) * 0.5 + 0.5;
    float burnPulse2 = sin(uTime * 5.8 + 1.7) * 0.5 + 0.5;
    float3 burnColor = float3(0.55, 0.22, 0.06);
    bg += burnColor * burn * burnPulse * 0.10;
    bg += float3(0.3, 0.08, 0.02) * burn * burnPulse2 * 0.06 * (1.0 - uv.y);

    // ═══ 6. 网格线 ═══
    float gridSize = 42.0;
    float gx = abs(frac(pixelPos.x / gridSize) - 0.5) * 2.0;
    float gy = abs(frac(pixelPos.y / gridSize) - 0.5) * 2.0;
    float gridLineX = 1.0 - smoothstep(0.0, 0.028, 1.0 - gx);
    float gridLineY = 1.0 - smoothstep(0.0, 0.028, 1.0 - gy);
    float gridLine = max(gridLineX, gridLineY);
    float gridCross = gridLineX * gridLineY;

    float3 gridColor = lerp(float3(0.06, 0.04, 0.03), float3(0.16, 0.08, 0.04), t);
    bg += gridColor * gridLine * 0.07;
    bg += gridColor * gridCross * 0.15;

    // ═══ 7. 扫描线 ═══
    float scanPos = frac(uTime * 0.035) * innerSize.y;
    float scanDist = abs((pixelPos.y - innerMin.y) - scanPos);
    float scanLine = exp(-scanDist * 0.12);
    float3 scanColor = lerp(float3(0.25, 0.12, 0.06), float3(0.55, 0.25, 0.10), t);
    bg += scanColor * scanLine * 0.14;

    // 第二扫描线（反向，更弱）
    float scanPos2 = frac(uTime * 0.022 + 0.5) * innerSize.y;
    float scanDist2 = abs((pixelPos.y - innerMin.y) - scanPos2);
    bg += scanColor * 0.5 * exp(-scanDist2 * 0.18) * 0.08;

    // ═══ 8. CRT水平线 ═══
    float crtLine = abs(frac(pixelPos.y * 0.25) - 0.5) * 2.0;
    crtLine = smoothstep(0.4, 0.5, crtLine);
    bg *= 1.0 - crtLine * 0.04;

    // ═══ 9. 边框辉光 ═══
    float edgeDist = -panelSDF;
    float edgeGlowStr = exp(-edgeDist * 0.22);
    float3 edgeColor = lerp(float3(0.28, 0.14, 0.07), float3(0.50, 0.22, 0.08), t + burn * 0.3);
    bg += edgeColor * edgeGlowStr * (0.25 + t * 0.45 + burn * 0.15);

    // 内框细线
    float innerEdge = exp(-abs(edgeDist - 10.0) * 0.6);
    bg += edgeColor * innerEdge * 0.12;

    // ═══ 10. 角落标记 ═══
    float markerLen = 18.0;
    float markerW = 2.5;
    float markers = 0.0;

    // 左上
    float2 tl = innerMin + 8.0;
    markers += step(abs(pixelPos.x - tl.x), markerLen) * step(abs(pixelPos.y - tl.y), markerW);
    markers += step(abs(pixelPos.y - tl.y), markerLen) * step(abs(pixelPos.x - tl.x), markerW);
    // 右上
    float2 tr = float2(innerMax.x - 8.0, innerMin.y + 8.0);
    markers += step(abs(pixelPos.x - tr.x), markerLen) * step(abs(pixelPos.y - tr.y), markerW);
    markers += step(abs(pixelPos.y - tr.y), markerLen) * step(abs(pixelPos.x - tr.x), markerW);
    // 左下
    float2 bl = float2(innerMin.x + 8.0, innerMax.y - 8.0);
    markers += step(abs(pixelPos.x - bl.x), markerLen) * step(abs(pixelPos.y - bl.y), markerW);
    markers += step(abs(pixelPos.y - bl.y), markerLen) * step(abs(pixelPos.x - bl.x), markerW);
    // 右下
    float2 br = innerMax - 8.0;
    markers += step(abs(pixelPos.x - br.x), markerLen) * step(abs(pixelPos.y - br.y), markerW);
    markers += step(abs(pixelPos.y - br.y), markerLen) * step(abs(pixelPos.x - br.x), markerW);

    float3 markerColor = lerp(float3(0.30, 0.15, 0.08), float3(0.55, 0.25, 0.10), t);
    bg += markerColor * saturate(markers) * 0.35;

    // ═══ 11. 暗角 ═══
    float2 vigUV = uv * 2.0 - 1.0;
    float vig = dot(vigUV, vigUV);
    bg *= 1.0 - vig * 0.22;

    // ═══ 12. 顶部反光条 ═══
    float topHighlight = 1.0 - smoothstep(0.0, 0.06, uv.y);
    bg += float3(0.15, 0.10, 0.06) * topHighlight * 0.12;

    float alpha = edgeAlpha * uAlpha;
    return float4(bg * alpha, alpha);
}

technique Technique1
{
    pass ThermalPanelPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
