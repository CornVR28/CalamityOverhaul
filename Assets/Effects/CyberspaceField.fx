// ============================================================================
// CyberspaceField.fx — 赛博空间领域着色器 v2
// 双层架构：场景压暗+去饱和+红染 → 加法叠加赛博特效
// 风格：赛博朋克2077黑墙AI，深红色系，方形栅格边缘，强压迫感
// ============================================================================

sampler uImage0 : register(s0);
sampler noiseTex : register(s1);

float uTime;
float radius;           // 领域半径（世界像素）
float intensity;        // 0-1 效果强度（淡入淡出）
float expandProgress;   // 0-1 展开进度
float dimStrength;      // 压暗强度 (0=不压暗, 1=最大压暗)
float2 setPoint;        // 领域中心（世界坐标）
float2 screenPosition;  // 屏幕左上角（世界坐标）
float2 screenSize;      // 屏幕尺寸（像素）
float gridSize;         // 栅格单元边长（世界像素）

// ---- 工具函数 ----

float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 original = tex2D(uImage0, coords);

    if (intensity < 0.001 || expandProgress < 0.001)
        return original;

    // ================================================================
    // 世界坐标与网格基础计算
    // ================================================================
    float2 worldPos = screenPosition + screenSize * coords;
    float2 relPos = worldPos - setPoint;
    float worldDist = length(relPos);
    float effectiveRadius = radius * expandProgress;

    float2 cellIdx = floor(relPos / gridSize);
    float2 cellCenter = (cellIdx + 0.5) * gridSize;
    float cellDist = length(cellCenter);
    float cellRand = hash21(cellIdx);

    // 噪声驱动不规则边缘
    float2 noiseUV = frac(cellIdx * 0.07 + float2(uTime * 0.02, uTime * 0.015));
    float edgeNoise = tex2D(noiseTex, noiseUV).r;
    float radiusOffset = (edgeNoise - 0.5) * gridSize * 4.5;

    // 边界区呼吸闪烁
    float breathe = sin(uTime * 0.8 + cellRand * 6.28318) * 0.5 + 0.5;
    float flickerRange = gridSize * 3.0;
    if (abs(cellDist - effectiveRadius) < flickerRange)
        radiusOffset += (breathe - 0.5) * gridSize * 1.5;

    float edgeBound = effectiveRadius + radiusOffset;
    bool inside = cellDist < edgeBound;

    // ================================================================
    // 域外溢出光晕 —— 边界外渗透红光 + 隐约栅格暗影
    // ================================================================
    if (!inside)
    {
        float overDist = (cellDist - edgeBound) / (gridSize * 5.0);
        float outerGlow = saturate(1.0 - overDist);
        outerGlow *= outerGlow;
        outerGlow *= intensity;

        if (outerGlow < 0.005)
            return original;

        // 轻微压暗 + 红光渗透
        original.rgb *= lerp(1.0, 0.85, outerGlow);
        original.rgb += float3(0.14, 0.015, 0.02) * outerGlow * 0.4;

        // 外围栅格暗影
        float2 outerCell = frac(relPos / gridSize);
        float ob = min(min(outerCell.x, 1.0 - outerCell.x), min(outerCell.y, 1.0 - outerCell.y));
        float outerGrid = 1.0 - smoothstep(0.0, 0.04, ob);
        original.rgb += float3(0.25, 0.02, 0.03) * outerGrid * outerGlow * 0.3;

        return float4(original.rgb, original.a);
    }

    // ================================================================
    // 内部归一化坐标
    // ================================================================
    float normDist = saturate(cellDist / effectiveRadius);
    float edgeFactor = smoothstep(0.7, 1.0, normDist);
    float centerFactor = 1.0 - normDist;

    // ================================================================
    // 第一层：色差分离（边缘区域，增强数字侵入感）
    // ================================================================
    float2 edgeDir = normalize(relPos + 0.001);
    float caStrength = edgeFactor * 0.004 * intensity;
    float2 caOffset = edgeDir * caStrength;

    original.r = tex2D(uImage0, coords + caOffset).r;
    original.b = tex2D(uImage0, coords - caOffset).b;

    // ================================================================
    // 第二层：场景压暗 + 去饱和 + 红染（核心压迫感来源）
    // ================================================================

    // 压暗：边缘≈55%亮度，中心≈25%亮度
    float targetDim = lerp(0.55, 0.25, centerFactor * 0.3);
    float dimFactor = lerp(1.0, targetDim, intensity * dimStrength);
    float3 processed = original.rgb * dimFactor;

    // 去饱和（将画面推向灰阶）
    float lum = dot(processed, float3(0.299, 0.587, 0.114));
    float3 gray = float3(lum, lum, lum);
    processed = lerp(processed, gray, 0.50 * intensity);

    // 红色调染（灰阶基础上叠加暗红色调）
    float3 redShift = float3(lum * 1.15, lum * 0.50, lum * 0.52);
    processed = lerp(processed, redShift, 0.40 * intensity);

    // 域内暗角——距边缘越近越暗，增加封闭压迫感
    float vignette = 1.0 - normDist * normDist * 0.25;
    processed *= lerp(1.0, vignette, intensity);

    // ================================================================
    // 第三层：加法叠加赛博特效
    // 在压暗后的画面上，红色发光线条/环/条纹会极为醒目
    // ================================================================

    // --- 栅格结构线（边缘区域清晰发光，中心极淡）---
    float2 cellLocal = frac(relPos / gridSize);
    float bx = min(cellLocal.x, 1.0 - cellLocal.x);
    float by = min(cellLocal.y, 1.0 - cellLocal.y);
    float borderDist = min(bx, by);
    float gridLine = 1.0 - smoothstep(0.0, 0.05 + edgeFactor * 0.02, borderDist);
    gridLine *= lerp(0.10, 1.0, edgeFactor);
    gridLine *= 0.7 + 0.3 * breathe;

    // --- 水平扫描线（细密横纹，缓慢下移）---
    float scanPhase = worldPos.y * 0.015 - uTime * 0.55;
    float scanLine = pow(saturate(sin(scanPhase * 6.28318) * 0.5 + 0.5), 10.0);
    scanLine *= lerp(0.2, 0.4, centerFactor);

    // --- 主扫描条（约16秒周期从上至下扫过的高亮光带）---
    float sweepCycle = frac(uTime * 0.06);
    float sweepWorldY = setPoint.y + lerp(-effectiveRadius, effectiveRadius, sweepCycle);
    float sweepDist = abs(worldPos.y - sweepWorldY);
    float sweepBar = saturate(1.0 - sweepDist / (gridSize * 1.5));
    sweepBar = sweepBar * sweepBar * 0.55;
    sweepBar *= step(worldDist, effectiveRadius * 0.95);

    // --- 垂直数据流（部分列出现Matrix式下落的光流）---
    float colIdx = floor(worldPos.x / (gridSize * 2.0));
    float colRand = hash21(float2(colIdx, 7.77));
    float streamPhase = frac(worldPos.y * 0.003 - uTime * (0.15 + colRand * 0.2));
    float dataStream = pow(streamPhase, 5.0) * step(0.60, colRand) * 0.35;
    dataStream *= (1.0 - edgeFactor);

    // --- 水平数据传输条（间歇出现的水平滑动亮条）---
    float barRow = floor(worldPos.y / (gridSize * 4.0));
    float barRand = hash21(float2(barRow, 13.37));
    float barPhase = frac(worldPos.x * 0.0008 + uTime * (0.15 + barRand * 0.2));
    float barLen = 0.08 + barRand * 0.15;
    float hBar = smoothstep(0, barLen * 0.15, barPhase) * smoothstep(barLen, barLen * 0.85, barPhase);
    hBar *= step(0.78, barRand) * 0.30;
    hBar *= (1.0 - edgeFactor);

    // --- 径向脉冲环（从中心向外扩散的能量波纹）---
    float pulsePhase = (worldDist - uTime * 75.0) * 0.02;
    float pulse = pow(saturate(sin(pulsePhase) * 0.5 + 0.5), 16.0);
    pulse *= saturate(1.0 - normDist * 0.65) * 0.30;

    // --- 边缘强光区（边界区域强烈的发光呼吸）---
    float edgeGlow = smoothstep(0.72, 1.0, normDist);
    float edgePulse = 0.55 + 0.45 * sin(uTime * 1.8 + cellRand * 6.28318);
    edgeGlow *= edgePulse;
    // 边缘单元闪烁块
    float edgeFlicker = step(0.92, hash21(cellIdx + floor(uTime * 6.0))) * edgeFactor;

    // --- 数字故障（随机单元瞬间亮起）---
    float glitchPhase = floor(uTime * 5.0);
    float glitchVal = hash21(cellIdx * 17.31 + glitchPhase);
    float glitch = step(0.975, glitchVal) * (1.0 - edgeFactor) * 0.6;

    // --- 赛博色彩面板 ---
    float3 cDeepRed   = float3(0.65, 0.05, 0.07);
    float3 cBrightRed = float3(1.0,  0.12, 0.08);
    float3 cHotRed    = float3(1.0,  0.30, 0.20);
    float3 cDimRed    = float3(0.40, 0.03, 0.04);
    float3 cWhiteRed  = float3(1.0,  0.55, 0.45);

    // --- 合成加法层 ---
    float3 additive = float3(0, 0, 0);
    additive += cDeepRed   * gridLine    * 0.75;
    additive += cBrightRed * scanLine;
    additive += cWhiteRed  * sweepBar;
    additive += cDimRed    * dataStream;
    additive += cDimRed    * hBar;
    additive += cBrightRed * pulse;
    additive += cHotRed    * edgeGlow    * 0.65;
    additive += cBrightRed * edgeFlicker * 0.50;
    additive += cBrightRed * glitch;

    // ================================================================
    // 最终合成
    // ================================================================
    float3 finalColor = processed + additive * intensity;

    return float4(finalColor, original.a);
}

technique Technique1
{
    pass CyberspacePass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
