// ============================================================================
// VoidColonySky.fx — 虚空聚落天空着色器
// 红黑风格亚空间末日背景：深邃漩涡 + 能量裂流 + 暗物质星云 + 虚空之眼
// 全屏 CustomSky 渲染，单 DrawCall
// ============================================================================

sampler uImage0 : register(s0);

texture uNoiseTex;
sampler noiseSamp = sampler_state
{
    texture = <uNoiseTex>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float uTime;
float intensity;       // 整体强度 0~1
float aspectRatio;     // 屏幕宽高比

float3 voidCore;       // 中心深黑色
float3 fireColor1;     // 火焰亮色 (橙黄)
float3 fireColor2;     // 火焰中色 (暗红)
float3 nebulaColor;    // 星云色 (暗紫/蓝紫)

// ---- 辅助函数 ----
#define PI 3.14159265
#define TAU 6.28318530

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

// 值噪声
float valueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + float2(1, 0));
    float c = hash21(i + float2(0, 1));
    float d = hash21(i + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// 分形噪声
float fbm(float2 p, int octaves)
{
    float value = 0.0;
    float amp = 0.5;
    float freq = 1.0;
    for (int i = 0; i < octaves; i++)
    {
        value += amp * valueNoise(p * freq);
        freq *= 2.13;
        amp *= 0.49;
        p += float2(1.7, 9.2);
    }
    return value;
}

// 域扭曲 fbm — 产生有机的漩涡/火焰形态
float warpedFbm(float2 p, float time)
{
    float2 q = float2(
        fbm(p + float2(0.0, 0.0), 4),
        fbm(p + float2(5.2, 1.3), 4));
    float2 r = float2(
        fbm(p + 4.0 * q + float2(1.7, 9.2) + time * 0.08, 4),
        fbm(p + 4.0 * q + float2(8.3, 2.8) + time * 0.06, 4));
    return fbm(p + 3.5 * r, 4);
}

// ---- A. 深邃漩涡背景 ----
// 多层螺旋结构，红黑交织，形成宇宙漩涡
float3 cosmicVortex(float2 centered, float dist, float angle, float time)
{
    // 螺旋角扭曲 — 距离越远旋转越多
    float spiralAngle = angle + dist * 4.5 - time * 0.12;
    float spiralAngle2 = angle - dist * 3.2 + time * 0.08;

    // 极坐标采样 UV
    float normA = (spiralAngle + PI) / TAU;
    float normA2 = (spiralAngle2 + PI) / TAU;

    // 第一层漩涡臂 — 大尺度结构
    float2 armUV = float2(normA * 2.0, dist * 1.8 - time * 0.03);
    float2 warp = tex2D(noiseSamp, frac(armUV * 0.7 + time * 0.02)).rg * 0.5;
    float arm1 = tex2D(noiseSamp, frac(float2(
        normA * 3.0 + warp.x,
        dist * 2.5 + warp.y - time * 0.04
    ))).r;

    // 第二层漩涡臂 — 反向旋转
    float2 armUV2 = float2(normA2 * 2.5, dist * 2.0 + time * 0.025);
    float2 warp2 = tex2D(noiseSamp, frac(armUV2 * 0.6 + 0.3)).gb * 0.4;
    float arm2 = tex2D(noiseSamp, frac(float2(
        normA2 * 2.0 - warp2.x,
        dist * 1.5 + warp2.y + time * 0.035
    ))).g;

    // 混合漩涡层
    float vortex = max(arm1 * 0.65, arm2 * 0.55);
    vortex = pow(abs(vortex), 1.4);

    // 径向衰减 — 中心暗，中段亮（漩涡臂主体），外围渐暗
    float radialMask = smoothstep(0.0, 0.25, dist) * smoothstep(1.4, 0.5, dist);
    vortex *= radialMask;

    // 漩涡颜色：从暗红到黑
    float3 color = lerp(voidCore, fireColor2 * 0.6, vortex);
    color += fireColor2 * 0.15 * vortex * smoothstep(0.5, 0.2, dist);

    return color;
}

// ---- B. 能量裂流/火焰触手 ----
// 从中心向外延伸的炽热能量流，模拟亚空间能量泄漏
float3 energyTendrils(float2 centered, float dist, float angle, float time)
{
    // 使用域扭曲产生有机的触手形态
    float2 uv = centered * 1.5;

    // 主能量场 — 域扭曲 FBM
    float energy = warpedFbm(uv + float2(time * 0.03, -time * 0.02), time);

    // 将能量场塑形为从中心发射的射线状
    float normAngle = (angle + PI) / TAU;

    // 射线遮罩：只在特定角度方向显示强烈能量
    float2 rayUV = float2(normAngle * 5.0, time * 0.08);
    float rayMask = tex2D(noiseSamp, frac(rayUV)).r;
    rayMask = smoothstep(0.45, 0.75, rayMask);

    // 径向强度分布
    float radialFade = smoothstep(0.0, 0.08, dist) * smoothstep(1.2, 0.15, dist);

    // 将能量值锐化为触手状纹理
    float tendril = energy * rayMask * radialFade;
    tendril = smoothstep(0.3, 0.65, tendril);

    // 中心区域特别强的能量流
    float2 centralUV = float2(normAngle * 3.0 + time * 0.15, dist * 3.0 - time * 0.2);
    float2 cWarp = tex2D(noiseSamp, frac(centralUV * 0.5)).rg * 0.6;
    float centralFlow = tex2D(noiseSamp, frac(float2(
        normAngle * 4.0 + cWarp.x + time * 0.1,
        dist * 4.0 + cWarp.y - time * 0.15
    ))).r;
    centralFlow = pow(abs(centralFlow), 2.0) * smoothstep(0.6, 0.0, dist) * 1.5;

    float totalEnergy = tendril + centralFlow;

    // 颜色梯度：核心亮黄/橙 → 边缘暗红
    float3 color = lerp(fireColor2, fireColor1, pow(abs(totalEnergy), 1.5));
    color *= totalEnergy;

    // HDR bloom 模拟 — 强能量区额外提亮
    color += fireColor1 * 0.4 * pow(abs(centralFlow), 2.5);

    return color;
}

// ---- C. 虚空之眼 ----
// 中央的黑暗核心，周围环绕炽热的吸积环
float3 voidEye(float2 centered, float dist, float angle, float time)
{
    // 吸积环参数
    float eyeRadius = 0.12;
    float ringWidth = 0.06;
    float ringDist = abs(dist - eyeRadius);

    // 吸积环 — 极其明亮的边缘
    float ring = exp(-ringDist * ringDist / (ringWidth * ringWidth));

    // 环上的纹理 — 旋转的物质流
    float normA = (angle + PI) / TAU;
    float2 ringUV = float2(normA * 8.0 + time * 0.6, ringDist * 20.0);
    float ringTex = tex2D(noiseSamp, frac(ringUV)).r;
    ring *= 0.5 + ringTex * 0.5;

    // 环颜色：超亮橙白
    float3 ringColor = lerp(fireColor1, float3(1.0, 0.9, 0.7), ring * 0.6) * ring * 2.5;

    // 中央黑暗 — 完全黑色的虚空核心
    float darkness = smoothstep(eyeRadius * 0.8, eyeRadius * 0.3, dist);

    // 引力透镜效果 — 环内侧有亮边
    float lensEdge = smoothstep(eyeRadius * 1.2, eyeRadius * 0.9, dist)
                   * smoothstep(eyeRadius * 0.4, eyeRadius * 0.7, dist);
    float3 lensColor = fireColor1 * lensEdge * 0.8;

    float3 color = ringColor + lensColor;
    // 黑洞内部压暗一切
    color *= (1.0 - darkness);

    return color;
}

// ---- D. 闪电裂痕 ----
// 亚空间屏障的裂缝，表现为闪电/裂纹效果
float lightningCracks(float2 centered, float dist, float angle, float time)
{
    float cracks = 0.0;

    // 多条裂缝
    for (int i = 0; i < 5; i++)
    {
        float id = (float) i;
        float h1 = hash11(id * 3.731 + 0.5);
        float h2 = hash11(id * 7.137 + 1.3);

        // 裂缝角度和径向范围
        float crackAngle = h1 * TAU + time * (0.02 + h2 * 0.03);
        float2 crackDir = float2(cos(crackAngle), sin(crackAngle));

        // 沿裂缝方向的投影距离
        float projDist = dot(centered, crackDir);
        // 垂直距离
        float perpDist = abs(dot(centered, float2(-crackDir.y, crackDir.x)));

        // 裂缝路径的弯曲 — 用噪声扭曲
        float bend = valueNoise(float2(projDist * 8.0 + id * 5.0, time * 0.3 + id)) * 0.06;
        perpDist = abs(perpDist - bend);

        // 裂缝宽度（中心粗，两端细）
        float crackMask = smoothstep(0.7, 0.0, abs(projDist)) * smoothstep(0.15, 0.25, dist);
        float crackWidth = 0.003 + crackMask * 0.006;

        // 裂缝本体
        float crack = exp(-perpDist * perpDist / (crackWidth * crackWidth));

        // 分支
        float branchSeed = projDist * 15.0 + id * 3.0;
        float branchVal = valueNoise(float2(branchSeed, time * 0.5));
        if (branchVal > 0.65)
        {
            float branchAngle = crackAngle + (branchVal - 0.65) * 8.0;
            float2 branchDir = float2(cos(branchAngle), sin(branchAngle));
            float branchPerp = abs(dot(centered - crackDir * projDist, float2(-branchDir.y, branchDir.x)));
            crack += exp(-branchPerp * branchPerp / (0.002 * 0.002)) * 0.4 * crackMask;
        }

        // 闪烁 — 随机闪灭
        float flicker = smoothstep(-0.2, 0.3, sin(time * (3.0 + h1 * 4.0) + id * 2.7));
        crack *= flicker;

        cracks += crack * crackMask;
    }

    return saturate(cracks);
}

// ---- E. 星空背景 ----
// 散布的细小星点，在暗区域闪烁
float stars(float2 uv, float time)
{
    float starField = 0.0;

    // 多层星空，不同密度
    for (int layer = 0; layer < 3; layer++)
    {
        float scale = 20.0 + (float) layer * 15.0;
        float2 cell = floor(uv * scale);
        float2 cellUV = frac(uv * scale);

        float h = hash21(cell + (float) layer * 100.0);

        // 只有部分格子有星星
        if (h > 0.85)
        {
            // 星星在格子内的偏移位置
            float2 starPos = hash22(cell + (float) layer * 50.0) * 0.6 + 0.2;
            float starDist = length(cellUV - starPos);

            // 星星亮度和大小
            float brightness = (h - 0.85) / 0.15;
            float starSize = 0.01 + brightness * 0.02;
            float star = exp(-starDist * starDist / (starSize * starSize));

            // 闪烁
            float twinkle = 0.6 + 0.4 * sin(time * (1.0 + hash11(h * 100.0) * 3.0) + h * TAU);
            star *= twinkle * brightness;

            starField += star;
        }
    }

    return saturate(starField);
}

// ---- F. 紫蓝星云 ----
// 外围区域的暗淡星云，给背景增添深度和色彩层次
float3 nebulaGlow(float2 centered, float dist, float time)
{
    // 星云分布 — 主要在外围
    float2 nebUV = centered * 0.8 + float2(time * 0.01, -time * 0.008);
    float neb1 = fbm(nebUV * 3.0, 5);
    float neb2 = fbm(nebUV * 2.0 + float2(3.7, 1.2), 4);

    // 外围遮罩
    float outerMask = smoothstep(0.3, 0.9, dist) * smoothstep(1.5, 0.8, dist);

    // 星云密度
    float nebDensity = neb1 * 0.6 + neb2 * 0.4;
    nebDensity = smoothstep(0.3, 0.7, nebDensity) * outerMask;

    // 多色星云：紫色和蓝紫色区域
    float colorVar = fbm(centered * 2.0 + 10.0, 3);
    float3 nebColor1 = nebulaColor;
    float3 nebColor2 = float3(0.08, 0.04, 0.18); // 深紫蓝
    float3 nebCol = lerp(nebColor1, nebColor2, colorVar);

    return nebCol * nebDensity * 0.5;
}

// ============================================================================
// 主像素着色器
// ============================================================================
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    // 归一化到 [-1, 1]，校正宽高比
    float2 centered = coords * 2.0 - 1.0;
    centered.x *= aspectRatio;

    float dist = length(centered);
    float angle = atan2(centered.y, centered.x);
    float time = uTime;

    // ======== 逐层合成 ========

    // 1. 基底：纯黑虚空
    float3 color = float3(0.01, 0.005, 0.015);

    // 2. 星空（最底层，在暗区域可见）
    float starVal = stars(coords, time);
    float3 starColor = lerp(float3(0.7, 0.6, 0.8), float3(1.0, 0.85, 0.7), hash21(floor(coords * 30.0)));
    color += starColor * starVal * 0.25 * smoothstep(0.3, 0.8, dist);

    // 3. 紫蓝星云
    color += nebulaGlow(centered, dist, time);

    // 4. 宇宙漩涡
    color += cosmicVortex(centered, dist, angle, time);

    // 5. 能量裂流
    color += energyTendrils(centered, dist, angle, time);

    // 6. 闪电裂痕
    float cracks = lightningCracks(centered, dist, angle, time);
    float3 crackColor = lerp(fireColor1, float3(1.0, 0.7, 0.4), cracks);
    color += crackColor * cracks * 0.7;

    // 7. 虚空之眼（最上层，中央黑洞）
    float3 eye = voidEye(centered, dist, angle, time);
    // 中央黑洞遮挡一切
    float eyeDarkness = smoothstep(0.12, 0.04, dist);
    color = color * (1.0 - eyeDarkness * 0.95) + eye;

    // ======== 全局后处理 ========

    // 暗角效果 — 四角压暗
    float vignette = 1.0 - smoothstep(0.5, 1.5, dist);
    color *= vignette;

    // 微弱的整体脉动
    float globalPulse = 0.92 + 0.08 * sin(time * 0.5);
    color *= globalPulse;

    // 色调映射（简化 ACES）
    color = color / (color + 0.8);

    // 应用强度
    color *= intensity;

    return float4(color, intensity);
}

technique Technique1
{
    pass VoidColonySkyPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
