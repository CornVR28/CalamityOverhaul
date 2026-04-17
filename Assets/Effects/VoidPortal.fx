//=============================================================================
// VoidPortal.fx — 虚空裂隙全屏后处理着色器
// 世界坐标系驱动，噪声纹理采样，多层视觉叠加
// 层级：UV扭曲 → 色差分离 → 背景压暗 → 虚空内部 → 多阶能量边缘
//       → 辐射裂纹 → 径向脉冲 → 能量微粒
// 不使用网格/像素化——纯粹的虚空撕裂感
//=============================================================================

sampler uImage0 : register(s0);
sampler noiseTex : register(s1);

float uTime;             //着色器累计时间（秒）
float intensity;         //全局强度0-1（淡入淡出）
float expandProgress;    //裂隙展开进度0-1
float riftHalfHeight;    //裂隙半高（世界像素）
float riftMaxWidth;      //裂隙最大半宽（世界像素）
float dimStrength;       //背景压暗强度
float energyPower;       //能量辉光强度
float crackSeed;         //裂缝随机种子

float2 riftCenter;       //裂隙中心（世界坐标）
float2 screenPosition;   //屏幕左上角（世界坐标）
float2 worldViewSize;    //缩放修正后的世界可视范围

// ---- 工具函数 ----

float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// 双层噪声纹理采样
float layeredNoise(float2 uv, float timeOff)
{
    float n1 = tex2D(noiseTex, frac(uv * 0.37 + float2(timeOff * 0.013, timeOff * 0.009))).r;
    float n2 = tex2D(noiseTex, frac(uv * 1.13 + float2(timeOff * -0.017, timeOff * 0.021))).g;
    float n3 = tex2D(noiseTex, frac(uv * 2.71 + float2(timeOff * 0.031, timeOff * -0.011))).b;
    return n1 * 0.5 + n2 * 0.35 + n3 * 0.15;
}

//=============================================================================
// 裂隙形状函数
// 返回到裂隙边界的有符号距离（负=内部），以及裂隙中心线X偏移
// 在世界坐标中运算
//=============================================================================
float riftSDF(float2 relPos, out float centerLineX, out float yNorm)
{
    float wProg = pow(saturate(expandProgress), 1.5);
    float hProg = pow(saturate(expandProgress), 0.5);

    float halfH = riftHalfHeight * hProg;
    float maxW = riftMaxWidth * wProg;

    // Y方向归一化
    yNorm = clamp(relPos.y / max(halfH, 0.1), -1.0, 1.0);
    float tipDist = max(abs(relPos.y) - halfH, 0.0);

    // 裂隙中心线路径（锯齿偏移，采样噪声纹理）
    float2 pathUV = frac(float2(
        relPos.y * 0.002 + crackSeed * 0.1,
        uTime * 0.04 + crackSeed * 0.3));
    float pathNoise = tex2D(noiseTex, pathUV).r;
    centerLineX = (pathNoise - 0.5) * maxW * 0.35;

    // 宽度包络：中心最宽，尖端收窄（幂函数产生尖锐尖端）
    float envelope = 1.0 - pow(abs(yNorm), 1.5);

    // 噪声调制宽度（不稳定脉动边缘）
    float2 widthUV = frac(float2(
        relPos.y * 0.005 + uTime * 0.02 + crackSeed * 0.7,
        uTime * 0.015 + crackSeed * 0.2));
    float widthNoise = tex2D(noiseTex, widthUV).g;
    float localWidth = maxW * envelope * (0.45 + 0.55 * widthNoise);

    // 撕裂锯齿（高频噪声使边缘参差不齐）
    float2 tearUV = frac(float2(
        relPos.y * 0.015 + uTime * 0.008 + crackSeed,
        uTime * 0.006));
    float tearNoise = tex2D(noiseTex, tearUV).b;
    localWidth *= (0.75 + 0.25 * step(0.3, tearNoise));

    // 有符号距离
    float xDist = abs(relPos.x - centerLineX) - localWidth;
    return max(xDist, tipDist * 0.6);
}

//=============================================================================
// 主像素着色器
//=============================================================================
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 original = tex2D(uImage0, coords);

    if (intensity < 0.001 || expandProgress < 0.001)
        return original;

    // ================================================================
    // 世界坐标计算
    // ================================================================
    float2 worldPos = screenPosition + worldViewSize * coords;
    float2 screenUV = worldViewSize * coords;
    float2 relPos = worldPos - riftCenter;
    float worldDist = length(relPos);

    float effectRadius = (riftHalfHeight + riftMaxWidth) * saturate(expandProgress) * 1.8;
    float normDist = saturate(worldDist / max(effectRadius, 1.0));

    // 裂隙 SDF
    float centerLineX, yNorm;
    float sd = riftSDF(relPos, centerLineX, yNorm);

    // ================================================================
    // 第一层：UV扭曲 — 现实被虚空吸入撕裂
    // ================================================================
    float2 distUV1 = frac(screenUV * 0.0004 + float2(uTime * 0.018, uTime * 0.013));
    float2 warpDisp = tex2D(noiseTex, distUV1).rg * 2.0 - 1.0;

    // 扭曲强度：靠近裂隙边缘最强
    float edgeFactor = saturate(1.0 - max(sd, 0.0) / max(riftMaxWidth * expandProgress * 0.5, 1.0));
    float warpStr = intensity * 0.004 * (0.3 + edgeFactor * 1.5) * saturate(expandProgress);

    // 向心吸入分量
    float2 pullDir = riftCenter - worldPos;
    float pullLen = length(pullDir);
    pullDir = pullLen > 0.1 ? pullDir / pullLen : float2(0, 0);
    float pullStr = intensity * 0.002 * saturate(1.0 - normDist) * saturate(expandProgress);
    pullStr = pullStr * pullStr;

    float2 warpOffset = (warpDisp * warpStr + pullDir * pullStr) * worldViewSize / max(worldViewSize.x, 1.0);
    float2 warpedCoords = clamp(coords + warpOffset, 0.002, 0.998);
    original = tex2D(uImage0, warpedCoords);

    // ================================================================
    // 第二层：色差分离
    // ================================================================
    float2 edgeDir = relPos / max(worldDist, 0.1);
    float caWorldPx = edgeFactor * 4.0 * intensity;
    float2 caOffset = edgeDir * caWorldPx / worldViewSize;
    original.r = tex2D(uImage0, warpedCoords + caOffset).r;
    original.b = tex2D(uImage0, warpedCoords - caOffset * 0.65).b;

    // ================================================================
    // 第三层：背景压暗 — 虚空压迫的黑暗蔓延
    // ================================================================
    float dimRange = effectRadius * 2.5;
    float dimFactor = saturate(1.0 - worldDist / max(dimRange, 1.0));
    dimFactor = dimFactor * dimFactor;
    float dimPulse = 0.88 + 0.12 * sin(uTime * 1.6);
    float targetDim = lerp(0.45, 0.15, dimFactor * 0.4);
    float actualDim = lerp(1.0, targetDim, intensity * dimStrength * dimPulse);
    original.rgb *= actualDim;

    // 去饱和（虚空侵蚀色彩）
    float lum = dot(original.rgb, float3(0.299, 0.587, 0.114));
    float3 gray = float3(lum, lum, lum);
    float desatAmount = 0.4 * intensity * dimFactor;
    original.rgb = lerp(original.rgb, gray, desatAmount);

    // 三阶暗红色映射
    float3 shadowVoid = float3(0.06, 0.005, 0.015);
    float3 midCrimson = float3(0.35, 0.025, 0.04);
    float3 highEmber  = float3(0.65, 0.12, 0.06);
    float3 redMap;
    if (lum < 0.25)
        redMap = lerp(shadowVoid, midCrimson, lum / 0.25);
    else
        redMap = lerp(midCrimson, highEmber, saturate((lum - 0.25) / 0.75));
    float redTintStr = 0.3 * intensity * dimFactor;
    original.rgb = lerp(original.rgb, redMap * (lum * 0.6 + 0.4), redTintStr);

    // ================================================================
    // 第四层：虚空内部 — 深邃的黑暗，有机的流动纹理
    // ================================================================
    float edgeThick = riftMaxWidth * saturate(expandProgress) * 0.15;
    float insideFactor = saturate(-sd / max(edgeThick * 5.0, 1.0));

    // 虚空涡流纹理（采样真实噪声纹理，多频叠加）
    float2 voidUV1 = frac(relPos * 0.003 + float2(uTime * 0.008, -uTime * 0.012));
    float voidN1 = tex2D(noiseTex, voidUV1).r;
    float2 voidUV2 = frac(relPos * 0.008 + float2(sin(uTime * 0.15) * 0.3, cos(uTime * 0.12) * 0.3));
    float voidN2 = tex2D(noiseTex, voidUV2).g;
    float voidTex = voidN1 * 0.6 + voidN2 * 0.4;

    // 几乎纯黑，深处有暗红微光涟漪
    float3 voidColor = float3(0.008, 0.002, 0.004) * voidTex;
    float deepPulse = pow(saturate(voidN2), 4.0);
    voidColor += float3(0.06, 0.006, 0.003) * deepPulse;

    // 虚空核心的微弱旋涡暗示
    float vortexAngle = atan2(relPos.y, relPos.x - centerLineX);
    float vortexN = tex2D(noiseTex, frac(float2(vortexAngle * 0.1, worldDist * 0.001 + uTime * 0.02))).r;
    voidColor += float3(0.03, 0.003, 0.008) * vortexN * insideFactor;

    original.rgb = lerp(original.rgb, voidColor, insideFactor);

    // ================================================================
    // 第五层：多阶能量边缘 — 白热核心→赤红辉光→暗红远辉
    // ================================================================
    float absSD = max(abs(sd), 0.0);

    // 内层：白热炽烈（裂隙正边缘）
    float innerEdge = exp(-absSD / max(edgeThick * 0.15, 0.1));
    // 中层：赤红辉光
    float midGlow = exp(-absSD / max(edgeThick * 1.5, 0.1));
    // 外层：暗红远距辉光
    float outerGlow = exp(-absSD / max(edgeThick * 5.0, 0.1));

    // 高频闪烁（不稳定能量逸散）
    float2 flickUV = frac(float2(relPos.y * 0.008 + uTime * 0.5, relPos.x * 0.005 + uTime * 0.2));
    float flickNoise = tex2D(noiseTex, flickUV).r;
    float flicker1 = 0.55 + 0.45 * flickNoise;

    float2 flickUV2 = frac(float2(relPos.x * 0.004 + uTime * 0.12, relPos.y * 0.003));
    float flicker2 = 0.7 + 0.3 * tex2D(noiseTex, flickUV2).g;

    // 边缘能量纹理
    float eNoise = layeredNoise(relPos * 0.004, uTime);

    float domainBreathe = 0.9 + 0.1 * sin(uTime * 0.7);

    // 白热核心
    float3 hotCore = float3(1.0, 0.55, 0.25) * innerEdge * energyPower * flicker1 * 2.2;
    // 赤红辉光带
    float3 redGlow = float3(0.9, 0.07, 0.03) * midGlow * energyPower * eNoise * flicker2;
    // 暗红远辉光
    float3 deepRedGlow = float3(0.35, 0.015, 0.008) * outerGlow * energyPower * 0.5;

    float3 edgeEnergy = (hotCore + redGlow + deepRedGlow) * saturate(expandProgress) * domainBreathe;

    // ================================================================
    // 第六层：辐射裂纹 — 从裂隙向外延伸的能量裂纹
    // ================================================================
    float angle = atan2(relPos.y, relPos.x);
    float normAngle = (angle + 3.14159) / 6.28318;

    float2 crackUV = frac(float2(
        normAngle * 3.0 + uTime * 0.03 + crackSeed * 0.5,
        worldDist * 0.002 - uTime * 0.02));
    float crackNoise = tex2D(noiseTex, crackUV).r;

    // 锐利裂纹形态（窄带通滤波）
    float crack = smoothstep(0.40, 0.48, crackNoise) * smoothstep(0.62, 0.52, crackNoise);

    // 裂纹只在靠近裂隙的区域出现
    float crackRange = saturate(1.0 - normDist * 1.3);
    crackRange *= crackRange;
    float crackPulse = 0.5 + 0.5 * sin(uTime * 1.3 + hash21(floor(crackUV * 10.0)) * 6.28318);
    float crackTotal = crack * crackRange * crackPulse * 1.5;

    float3 crackColor = float3(0.95, 0.18, 0.08);

    // ================================================================
    // 第七层：径向脉冲环 — 从裂隙中心向外扩散的能量波
    // ================================================================
    float pulseDistortion = tex2D(noiseTex, frac(screenUV * 0.0006 + uTime * 0.005)).r * 15.0;
    float basePhaseDist = worldDist + pulseDistortion;
    float pulse1 = pow(saturate(sin((basePhaseDist - uTime * 55.0) * 0.010) * 0.5 + 0.5), 7.0);
    float pulse2 = pow(saturate(sin((basePhaseDist - uTime * 35.0) * 0.007) * 0.5 + 0.5), 9.0);
    float pulse = (pulse1 * 0.55 + pulse2 * 0.45);
    pulse *= saturate(1.0 - normDist * 0.7) * 0.2;

    float3 pulseColor = float3(0.8, 0.1, 0.06);

    // ================================================================
    // 第八层：能量微粒 — 虚空中漂浮的发光碎片
    // ================================================================
    float2 particleUV = frac(screenUV * 0.004 + float2(uTime * 0.02, -uTime * 0.018));
    float particleNoise = tex2D(noiseTex, particleUV).r;
    float particle = smoothstep(0.90, 0.96, particleNoise);
    float particleSeed = floor(particleNoise * 50.0);
    float particlePulse = 0.5 + 0.5 * sin(uTime * 2.5 + particleSeed * 2.9);
    particle *= particlePulse * saturate(1.0 - normDist) * 0.55;

    float3 particleColor = float3(1.0, 0.35, 0.18);

    // ================================================================
    // 合成加法层
    // ================================================================
    float3 additive = float3(0, 0, 0);
    additive += edgeEnergy;
    additive += crackColor * crackTotal;
    additive += pulseColor * pulse;
    additive += particleColor * particle;

    // ================================================================
    // 最终合成
    // ================================================================
    float3 finalColor = original.rgb + additive * intensity * domainBreathe;

    return float4(finalColor, original.a);
}

technique Technique1
{
    pass VoidPortalPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
