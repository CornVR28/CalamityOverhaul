// ============================================================================
// CyberspaceField.fx — 赛博空间领域着色器 v3
// 双层架构：场景压暗+去饱和+红染 → 加法叠加赛博特效
// 风格：赛博朋克2077黑墙AI，深红色系，方形栅格边缘，强压迫感
// 修正：screenSize 传入缩放修正后的世界可视范围，领域锚定于世界
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
float2 worldViewSize;   // 缩放修正后的世界可视范围（非屏幕像素尺寸）
float gridSize;         // 栅格单元边长（世界像素）

// 域内实体扫描圆环
int entityCount;        // 域内实体数量 (最大32)
float4 entities[32];    // (centerX, centerY, ringRadius, seed)

// ---- 工具函数 ----

float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// 多八度噪声采样（用低开销的双层混合替代循环）
float layeredNoise(float2 uv, float timeOff)
{
    float n1 = tex2D(noiseTex, frac(uv * 0.37 + float2(timeOff * 0.013, timeOff * 0.009))).r;
    float n2 = tex2D(noiseTex, frac(uv * 1.13 + float2(timeOff * -0.017, timeOff * 0.021))).g;
    float n3 = tex2D(noiseTex, frac(uv * 2.71 + float2(timeOff * 0.031, timeOff * -0.011))).b;
    return n1 * 0.5 + n2 * 0.35 + n3 * 0.15;
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 original = tex2D(uImage0, coords);

    if (intensity < 0.001 || expandProgress < 0.001)
        return original;

    // ================================================================
    // 世界坐标计算（缩放感知）
    // ================================================================
    float2 worldPos = screenPosition + worldViewSize * coords;
    float2 relPos = worldPos - setPoint;
    float worldDist = length(relPos);
    float effectiveRadius = radius * expandProgress;

    // ================================================================
    // 网格基础
    // ================================================================
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
    // 域外溢出光晕
    // ================================================================
    if (!inside)
    {
        float overDist = (cellDist - edgeBound) / (gridSize * 5.0);
        float outerGlow = saturate(1.0 - overDist);
        outerGlow *= outerGlow;
        outerGlow *= intensity;

        if (outerGlow < 0.005)
            return original;

        original.rgb *= lerp(1.0, 0.85, outerGlow);
        original.rgb += float3(0.14, 0.015, 0.02) * outerGlow * 0.4;

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
    // 色差偏移量转为UV空间：世界像素偏移 / 世界可视范围
    float caWorldPx = edgeFactor * 2.5 * intensity;
    float2 caOffset = edgeDir * caWorldPx / worldViewSize;

    original.r = tex2D(uImage0, coords + caOffset).r;
    original.b = tex2D(uImage0, coords - caOffset).b;

    // ================================================================
    // 第二层：场景压暗 + 去饱和 + 红染（核心压迫感来源）
    // ================================================================
    float targetDim = lerp(0.55, 0.25, centerFactor * 0.3);
    float dimFactor = lerp(1.0, targetDim, intensity * dimStrength);
    float3 processed = original.rgb * dimFactor;

    float lum = dot(processed, float3(0.299, 0.587, 0.114));
    float3 gray = float3(lum, lum, lum);
    processed = lerp(processed, gray, 0.50 * intensity);

    float3 redShift = float3(lum * 1.15, lum * 0.50, lum * 0.52);
    processed = lerp(processed, redShift, 0.40 * intensity);

    float vignette = 1.0 - normDist * normDist * 0.25;
    processed *= lerp(1.0, vignette, intensity);

    // ================================================================
    // 第三层：加法叠加赛博特效（全面丰富质感）
    // ================================================================

    // --- A. 底层数字暗流（多八度噪声场）---
    // 域内始终有微妙的深红流动纹理，避免大面积"空洞"感
    float2 fieldUV = worldPos / (gridSize * 12.0);
    float fieldNoise = layeredNoise(fieldUV, uTime);
    float digitalField = smoothstep(0.3, 0.7, fieldNoise);
    digitalField *= lerp(0.12, 0.05, edgeFactor);

    // --- B. 栅格结构线（噪声调制亮度，模拟电路板走线）---
    float2 cellLocal = frac(relPos / gridSize);
    float bx = min(cellLocal.x, 1.0 - cellLocal.x);
    float by = min(cellLocal.y, 1.0 - cellLocal.y);
    float borderDist = min(bx, by);

    // 每条边独立噪声亮度，模拟"活跃/休眠"电路
    float traceNoiseH = tex2D(noiseTex, frac(float2(cellIdx.x * 0.13, cellIdx.y * 0.09 + uTime * 0.01))).r;
    float traceNoiseV = tex2D(noiseTex, frac(float2(cellIdx.y * 0.11, cellIdx.x * 0.07 + uTime * 0.012))).g;
    float traceBright = (by < bx) ? traceNoiseH : traceNoiseV;
    traceBright = smoothstep(0.2, 0.8, traceBright);

    float gridLine = 1.0 - smoothstep(0.0, 0.05 + edgeFactor * 0.02, borderDist);
    // 边缘区域所有线都亮，内部区域随噪声明灭
    float gridOpacity = lerp(traceBright * 0.25, 1.0, edgeFactor);
    gridLine *= gridOpacity;
    gridLine *= 0.7 + 0.3 * breathe;

    // --- C. 栅格交叉节点亮点 ---
    float nodeX = 1.0 - smoothstep(0.0, 0.08, bx);
    float nodeY = 1.0 - smoothstep(0.0, 0.08, by);
    float node = nodeX * nodeY;
    float nodePulse = 0.5 + 0.5 * sin(uTime * 2.2 + cellRand * 6.28);
    node *= lerp(nodePulse * 0.15, 0.6, edgeFactor);

    // --- D. 水平扫描线（细密横纹，缓慢下移）---
    float scanPhase = worldPos.y * 0.015 - uTime * 0.55;
    float scanLine = pow(saturate(sin(scanPhase * 6.28318) * 0.5 + 0.5), 10.0);
    scanLine *= lerp(0.15, 0.3, centerFactor);

    // --- F. 主扫描条（约16秒周期从上至下扫过的高亮光带）---
    float sweepCycle = frac(uTime * 0.06);
    float sweepWorldY = setPoint.y + lerp(-effectiveRadius, effectiveRadius, sweepCycle);
    float sweepDist = abs(worldPos.y - sweepWorldY);
    float sweepBar = saturate(1.0 - sweepDist / (gridSize * 1.5));
    sweepBar = sweepBar * sweepBar * 0.55;
    sweepBar *= step(worldDist, effectiveRadius * 0.95);

    // --- G. 垂直数据流（部分列Matrix式下落，带渐变拖尾）---
    float dColIdx = floor(worldPos.x / (gridSize * 2.0));
    float colRand = hash21(float2(dColIdx, 7.77));
    float streamActive = step(0.55, colRand);
    float streamSpeed = 0.15 + colRand * 0.2;
    float streamPhase = frac(worldPos.y * 0.003 - uTime * streamSpeed);
    // 带拖尾的渐变（头亮尾暗）
    float streamHead = smoothstep(0.0, 0.05, streamPhase) * smoothstep(0.3, 0.08, streamPhase);
    float streamTail = pow(saturate(1.0 - streamPhase / 0.3), 3.0) * 0.4;
    float dataStream = (streamHead + streamTail) * streamActive * (1.0 - edgeFactor) * 0.30;

    // --- H. 径向脉冲环（从中心向外扩散的能量波纹，噪声扰动）---
    float pulseDistortion = tex2D(noiseTex, frac(worldPos * 0.001 + uTime * 0.008)).r * 15.0;
    float pulsePhase = (worldDist + pulseDistortion - uTime * 75.0) * 0.02;
    float pulse = pow(saturate(sin(pulsePhase) * 0.5 + 0.5), 16.0);
    pulse *= saturate(1.0 - normDist * 0.65) * 0.25;

    // --- I. 边缘强光区 ---
    float edgeGlow = smoothstep(0.72, 1.0, normDist);
    float edgePulse = 0.55 + 0.45 * sin(uTime * 1.8 + cellRand * 6.28318);
    edgeGlow *= edgePulse;

    // --- J. 水平故障撕裂（取代廉价的全单元闪烁）---
    // 随机的水平错位线段，模拟数字传输故障
    float tearLine = floor(worldPos.y / 3.0);           // 每3像素一行
    float tearTime = floor(uTime * 4.0);                // 每0.25秒重新随机
    float tearRand = hash21(float2(tearLine, tearTime));
    bool tearActive = tearRand > 0.992;
    // 撕裂偏移：固定世界像素量 / 可视范围 → 缩放安全的UV偏移
    float tearShift = (tearRand - 0.5) * 6.0 / worldViewSize.x;
    float tearBright = 0.0;
    if (tearActive && edgeFactor < 0.5)
    {
        float4 tearSample = tex2D(uImage0, float2(coords.x + tearShift, coords.y));
        tearBright = dot(tearSample.rgb, float3(0.333, 0.333, 0.333)) * 0.4;
    }

    // --- K. 实体扫描圆环（标记域内实体位置与大小）---
    float entityRingTotal = 0;
    float entityScanTotal = 0;
    [loop]
    for (int e = 0; e < entityCount; e++)
    {
        float2 eCenter = entities[e].xy;
        float eRadius = entities[e].z;
        float eSeed = entities[e].w;

        float2 toEntity = worldPos - eCenter;
        float eDist = length(toEntity);
        float ringDist = abs(eDist - eRadius);

        // 主圆环：锐利的薄环线
        float ring = 1.0 - smoothstep(0.0, 2.0, ringDist);

        // 12段分节（HUD风格分段环）
        float eAngle = atan2(toEntity.y, toEntity.x);
        float segFrac = frac(eAngle * 1.9099);  // 12 segments over 2pi
        float segGap = smoothstep(0.03, 0.08, min(segFrac, 1.0 - segFrac));
        ring *= lerp(1.0, segGap, 0.45);

        // 外晕：柔和扩散光
        float halo = 1.0 - smoothstep(0.0, 8.0, ringDist);
        halo *= halo * 0.2;

        // 旋转扫描弧：~60°高亮弧段绕环旋转
        float scanAngle = uTime * 1.8 + eSeed * 6.28318;
        float angleDiff = abs(frac((eAngle - scanAngle) / 6.28318 + 0.5) - 0.5) * 2.0;
        float scan = smoothstep(0.17, 0.0, angleDiff);

        // 呼吸脉冲
        float ePulse = 0.7 + 0.3 * sin(uTime * 2.5 + eSeed * 12.56);

        entityRingTotal += (ring * 0.65 + halo) * ePulse;
        entityScanTotal += ring * scan * ePulse;
    }

    // --- 赛博色彩面板 ---
    float3 cDeepRed   = float3(0.65, 0.05, 0.07);
    float3 cBrightRed = float3(1.0,  0.12, 0.08);
    float3 cHotRed    = float3(1.0,  0.30, 0.20);
    float3 cDimRed    = float3(0.40, 0.03, 0.04);
    float3 cWhiteRed  = float3(1.0,  0.55, 0.45);
    float3 cNodeColor = float3(0.90, 0.20, 0.15);

    // --- 合成加法层 ---
    float3 additive = float3(0, 0, 0);
    additive += cDimRed     * digitalField;          // A: 底层暗流
    additive += cDeepRed    * gridLine * 0.75;       // B: 栅格走线
    additive += cNodeColor  * node;                  // C: 节点亮点
    additive += cBrightRed  * scanLine;              // D: 扫描线
    additive += cWhiteRed   * sweepBar;              // F: 主扫描条
    additive += cDimRed     * dataStream;            // G: 垂直数据流
    additive += cBrightRed  * pulse;                 // H: 径向脉冲环
    additive += cHotRed     * edgeGlow * 0.65;       // I: 边缘强光
    additive += cBrightRed  * tearBright;            // J: 故障撕裂
    additive += cBrightRed  * entityRingTotal;       // K: 实体扫描环
    additive += cWhiteRed   * entityScanTotal * 0.5; // K: 扫描弧高亮

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
