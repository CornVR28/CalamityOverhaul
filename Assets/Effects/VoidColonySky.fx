// ============================================================================
// VoidColonySky.fx — 虚空聚落天空着色器
// 红黑风格亚空间末日背景：深邃漩涡 + 域扭曲火焰 + 暗物质星云 + 虚空之眼
// 全屏 CustomSky 渲染，单 DrawCall
// 所有效果使用笛卡尔坐标噪声，避免 atan2 角度接缝
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

// 旋转2D坐标（用于漩涡效果，避免极坐标）
float2 rotate2D(float2 p, float a)
{
    float s = sin(a);
    float c = cos(a);
    return float2(p.x * c - p.y * s, p.x * s + p.y * c);
}

// ---- A. 深邃漩涡背景 ----
// 使用笛卡尔坐标旋转采样，完全避免 atan2 接缝
float3 cosmicVortex(float2 centered, float dist, float time)
{
    // 对坐标施加距离相关的旋转 — 产生螺旋效果
    float2 spiral1 = rotate2D(centered, dist * 4.5 - time * 0.12);
    float2 spiral2 = rotate2D(centered, -dist * 3.2 + time * 0.08);

    // 第一层漩涡臂 — 直接用旋转后的笛卡尔坐标采样
    float2 armUV1 = spiral1 * 0.8 + float2(time * 0.02, -time * 0.015);
    float2 warp1 = tex2D(noiseSamp, frac(armUV1 * 0.4 + 0.5)).rg * 0.5;
    float arm1 = tex2D(noiseSamp, frac(spiral1 * 0.6 + warp1 + 0.5)).r;

    // 第二层漩涡臂 — 反向旋转
    float2 armUV2 = spiral2 * 0.7 + float2(-time * 0.018, time * 0.012);
    float2 warp2 = tex2D(noiseSamp, frac(armUV2 * 0.35 + 0.3)).gb * 0.4;
    float arm2 = tex2D(noiseSamp, frac(spiral2 * 0.5 + warp2 + 0.5)).g;

    // 第三层 — 更细的纹理
    float2 spiral3 = rotate2D(centered, dist * 6.0 + time * 0.05);
    float arm3 = tex2D(noiseSamp, frac(spiral3 * 1.0 + 0.5)).b;

    // 混合漩涡层
    float vortex = arm1 * 0.45 + arm2 * 0.35 + arm3 * 0.2;
    vortex = pow(abs(vortex), 1.3);

    // 径向衰减 — 中心暗，中段亮，外围渐暗
    float radialMask = smoothstep(0.0, 0.2, dist) * smoothstep(1.5, 0.45, dist);
    vortex *= radialMask;

    // 漩涡颜色：从暗红到黑
    float3 color = lerp(voidCore, fireColor2 * 0.7, vortex);
    color += fireColor2 * 0.2 * vortex * smoothstep(0.5, 0.15, dist);

    return color;
}

// ---- B. 域扭曲火焰 ----
// 纯笛卡尔坐标的有机火焰/能量，无方向性光束
float3 warpedFlames(float2 centered, float dist, float time)
{
    // 多层域扭曲 — 产生有机的火焰形态
    float2 uv1 = centered * 1.2 + float2(time * 0.025, -time * 0.02);
    float flame1 = warpedFbm(uv1, time);

    // 第二层 — 不同频率和相位
    float2 uv2 = centered * 0.8 + float2(-time * 0.015, time * 0.03);
    float flame2 = warpedFbm(uv2 + float2(7.3, 2.1), time * 0.7);

    // 混合并塑形
    float flame = flame1 * 0.6 + flame2 * 0.4;

    // 提取高亮区域作为火焰纹理
    float firePattern = smoothstep(0.35, 0.7, flame);

    // 径向遮罩 — 中心到中段最亮，外围衰减
    float radialMask = smoothstep(0.0, 0.1, dist) * smoothstep(1.1, 0.2, dist);
    firePattern *= radialMask;

    // 额外的中心区域炽热层
    float2 uvCenter = centered * 2.0 + float2(time * 0.04, -time * 0.035);
    float centerFlame = warpedFbm(uvCenter + float2(3.1, 8.7), time * 1.2);
    centerFlame = smoothstep(0.4, 0.75, centerFlame);
    centerFlame *= smoothstep(0.5, 0.05, dist); // 只在中心区域

    float totalFlame = firePattern + centerFlame * 0.8;
    totalFlame = saturate(totalFlame);

    // 颜色梯度：暗红底 → 亮橙黄高光
    float3 color = lerp(fireColor2 * 0.5, fireColor1, pow(abs(totalFlame), 1.8));
    color *= totalFlame;

    // 极亮区域额外泛白
    color += float3(1.0, 0.8, 0.5) * pow(abs(centerFlame), 3.0) * 0.3;

    return color;
}

// ---- C. 虚空之眼 ----
// 中央黑暗核心 + 炽热吸积环（使用笛卡尔旋转采样）
float3 voidEye(float2 centered, float dist, float time)
{
    // 吸积环参数
    float eyeRadius = 0.12;
    float ringWidth = 0.055;
    float ringDist = abs(dist - eyeRadius);

    // 吸积环 — 高斯辉光
    float ring = exp(-ringDist * ringDist / (ringWidth * ringWidth));

    // 环上纹理 — 用旋转坐标采样，无 atan2
    float2 ringCoord = rotate2D(centered, time * 0.6);
    float2 ringUV = ringCoord * 4.0 + 0.5;
    float ringTex = tex2D(noiseSamp, frac(ringUV)).r;

    // 第二层环纹理
    float2 ringCoord2 = rotate2D(centered, -time * 0.35);
    float ringTex2 = tex2D(noiseSamp, frac(ringCoord2 * 6.0 + 0.3)).g;

    ring *= 0.4 + ringTex * 0.35 + ringTex2 * 0.25;

    // 环颜色：超亮橙白
    float3 ringColor = lerp(fireColor1, float3(1.0, 0.9, 0.7), ring * 0.5) * ring * 2.0;

    // 中央黑暗
    float darkness = smoothstep(eyeRadius * 0.8, eyeRadius * 0.25, dist);

    // 引力透镜 — 内侧亮边
    float lensEdge = smoothstep(eyeRadius * 1.2, eyeRadius * 0.85, dist)
                   * smoothstep(eyeRadius * 0.35, eyeRadius * 0.65, dist);
    float3 lensColor = fireColor1 * lensEdge * 0.6;

    float3 color = ringColor + lensColor;
    color *= (1.0 - darkness);

    return color;
}

// ---- D. 星空背景 ----
float stars(float2 uv, float time)
{
    float starField = 0.0;

    for (int layer = 0; layer < 3; layer++)
    {
        float scale = 20.0 + (float) layer * 15.0;
        float2 cell = floor(uv * scale);
        float2 cellUV = frac(uv * scale);

        float h = hash21(cell + (float) layer * 100.0);

        if (h > 0.85)
        {
            float2 starPos = hash22(cell + (float) layer * 50.0) * 0.6 + 0.2;
            float starDist = length(cellUV - starPos);

            float brightness = (h - 0.85) / 0.15;
            float starSize = 0.01 + brightness * 0.02;
            float star = exp(-starDist * starDist / (starSize * starSize));

            float twinkle = 0.6 + 0.4 * sin(time * (1.0 + hash11(h * 100.0) * 3.0) + h * TAU);
            star *= twinkle * brightness;

            starField += star;
        }
    }

    return saturate(starField);
}

// ---- E. 紫蓝星云 ----
float3 nebulaGlow(float2 centered, float dist, float time)
{
    float2 nebUV = centered * 0.8 + float2(time * 0.01, -time * 0.008);
    float neb1 = fbm(nebUV * 3.0, 5);
    float neb2 = fbm(nebUV * 2.0 + float2(3.7, 1.2), 4);

    float outerMask = smoothstep(0.3, 0.9, dist) * smoothstep(1.5, 0.8, dist);

    float nebDensity = neb1 * 0.6 + neb2 * 0.4;
    nebDensity = smoothstep(0.3, 0.7, nebDensity) * outerMask;

    float colorVar = fbm(centered * 2.0 + 10.0, 3);
    float3 nebColor1 = nebulaColor;
    float3 nebColor2 = float3(0.08, 0.04, 0.18);
    float3 nebCol = lerp(nebColor1, nebColor2, colorVar);

    return nebCol * nebDensity * 0.5;
}

// ---- F. 细微能量丝 ----
// 在中远距离区域用噪声产生柔和的丝状能量纹理，纯装饰
float3 energyWisps(float2 centered, float dist, float time)
{
    // 缓慢旋转采样
    float2 rc1 = rotate2D(centered, time * 0.04) * 2.0;
    float2 rc2 = rotate2D(centered, -time * 0.03) * 1.5;

    // 拉伸采样产生丝状效果
    float w1 = tex2D(noiseSamp, frac(float2(rc1.x * 0.3, rc1.y * 1.5) + 0.5)).r;
    float w2 = tex2D(noiseSamp, frac(float2(rc2.x * 1.5, rc2.y * 0.3) + 0.3)).g;

    // 噪声域扭曲
    float2 warpUV = centered * 1.0 + float2(time * 0.02, -time * 0.015);
    float warpVal = fbm(warpUV * 3.0, 3);

    float wisps = w1 * w2;
    wisps = smoothstep(0.15, 0.45, wisps + warpVal * 0.2);

    // 遮罩 — 中间环带最明显
    float mask = smoothstep(0.15, 0.35, dist) * smoothstep(0.9, 0.5, dist);
    wisps *= mask;

    // 淡红色调
    float3 color = lerp(fireColor2 * 0.3, fireColor1 * 0.4, wisps);
    color *= wisps * 0.5;

    return color;
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
    color += cosmicVortex(centered, dist, time);

    // 5. 域扭曲火焰
    color += warpedFlames(centered, dist, time);

    // 6. 细微能量丝
    color += energyWisps(centered, dist, time);

    // 7. 虚空之眼（最上层，中央黑洞）
    float3 eye = voidEye(centered, dist, time);
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
