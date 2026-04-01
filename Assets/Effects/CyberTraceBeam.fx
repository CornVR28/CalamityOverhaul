// ============================================================================
// CyberTraceBeam.fx — 赛博追踪能量光束着色器
// 蓝/黄/青三色主题 · 流动能量纹理 · 科幻光球头部
// Trail条带渲染，配合 CyberTraceBeamProj 使用
// ============================================================================

float4x4 transformMatrix;
float uTime;
float fadeAlpha;        // 整体透明度 0~1.4
float headProgress;     // 头部在拖尾上的位置 0~1（along轴）
float3 coreColor;       // 主题内层亮色（由C#端按主题传入）
float3 glowColor;       // 主题中层辉光色
float3 auraColor;       // 主题外层光晕色
float trailLength;      // 拖尾可见长度 0~1（从头部往后）

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

struct VSInput
{
    float4 Position : POSITION0;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

struct PSInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoords : TEXCOORD0;
};

PSInput VertexShaderFunction(VSInput v)
{
    PSInput o;
    o.Position = mul(v.Position, transformMatrix);
    o.Color = v.Color;
    o.TexCoords = v.TexCoords;
    return o;
}

// 简单哈希函数
float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float2 uv = input.TexCoords;
    float along = uv.x;          // 0=起点（尾部） 1=末端（头部）
    float cross_ = uv.y;         // 0=上边 1=下边
    float crossDist = abs(cross_ - 0.5) * 2.0; // 0=中心 1=边缘

    // ---- 拖尾可见遮罩：头部所在位置向后延伸 trailLength ----
    float tailStart = headProgress - trailLength;
    float visMask = smoothstep(tailStart - 0.03, tailStart + 0.05, along)
                  * smoothstep(headProgress + 0.03, headProgress - 0.01, along);
    if (visMask < 0.001)
        return float4(0, 0, 0, 0);

    // 离头部的相对距离 0=头部 1=尾端
    float distFromHead = saturate((headProgress - along) / max(trailLength, 0.01));

    // ---- 噪声采样 ----
    float n1 = tex2D(noiseSamp, frac(float2(along * 4.0 + uTime * 1.5, cross_ * 0.8))).r;
    float n2 = tex2D(noiseSamp, frac(float2(along * 8.0 - uTime * 2.2, cross_ * 1.5 + 0.4))).g;
    float n3 = tex2D(noiseSamp, frac(float2(along * 2.5 + uTime * 0.8, cross_ * 3.0 + 0.7))).b;

    // ============================================================
    // A. 白热能量核心 —— 光束中心最亮的通道
    // ============================================================
    float coreWidth = 0.08 + n1 * 0.04;
    // 核心随离头部距离逐渐变窄
    coreWidth *= lerp(1.0, 0.3, distFromHead);
    float core = 1.0 - smoothstep(0.0, coreWidth, crossDist);
    core = pow(saturate(core), 1.2);
    // 流动闪烁
    float corePulse = 0.85 + 0.15 * sin(uTime * 18.0 + along * 40.0);
    core *= corePulse;

    // ============================================================
    // B. 内层辉光 —— 主题色明亮层
    // ============================================================
    float innerW = 0.25 + n2 * 0.08;
    innerW *= lerp(1.0, 0.5, distFromHead);
    float inner = 1.0 - smoothstep(coreWidth * 0.4, innerW, crossDist);
    inner *= 0.7;

    // ============================================================
    // C. 外层光晕 —— 柔和的主题色扩散
    // ============================================================
    float outerFade = 1.0 - smoothstep(0.15, 0.95, crossDist);
    outerFade *= lerp(0.4, 0.1, distFromHead);

    // ============================================================
    // D. 纵向能量流纹 —— 沿光束方向的流动条纹
    // ============================================================
    float streamUV1 = frac(along * 10.0 - uTime * 4.0);
    float stream1 = smoothstep(0.0, 0.06, streamUV1) * smoothstep(0.22, 0.10, streamUV1);
    float streamUV2 = frac(along * 16.0 - uTime * 6.5 + 0.33);
    float stream2 = smoothstep(0.0, 0.04, streamUV2) * smoothstep(0.15, 0.07, streamUV2);
    float streams = (stream1 + stream2 * 0.6) * (1.0 - crossDist * 0.7) * 0.35;
    streams *= lerp(1.0, 0.2, distFromHead);

    // ============================================================
    // E. 微弱数字网格 —— 赛博科幻质感
    // ============================================================
    float gridX = frac(along * 30.0);
    float gridY = frac(cross_ * 6.0);
    float gridLine = step(gridX, 0.04) + step(gridY, 0.06);
    gridLine = saturate(gridLine);
    // 网格闪烁（只有部分格子亮）
    float cellID = floor(along * 30.0) + floor(cross_ * 6.0) * 37.0;
    float cellFlicker = hash21(float2(cellID, floor(uTime * 5.0)));
    gridLine *= step(0.82, cellFlicker) * 0.15;
    gridLine *= (1.0 - crossDist * 0.6);
    gridLine *= lerp(0.8, 0.1, distFromHead);

    // ============================================================
    // F. 头部光球 —— 圆形高亮辐射
    // ============================================================
    float headDist = abs(along - headProgress);
    float headOrb = 1.0 - smoothstep(0.0, 0.05, headDist);
    headOrb *= (1.0 - crossDist * 0.6);
    // 光球呼吸脉冲
    float orbPulse = 0.8 + 0.2 * sin(uTime * 12.0);
    headOrb *= orbPulse;
    // 光球外圈柔和光晕
    float headGlow = 1.0 - smoothstep(0.02, 0.12, headDist);
    headGlow *= (1.0 - crossDist * 0.8) * 0.4;

    // ============================================================
    // G. 边缘腐蚀 —— 噪声驱动的有机能量边界
    // ============================================================
    float edgeNoise = n2 * 0.20 + n3 * 0.15;
    float edgeMask = 1.0 - smoothstep(0.45 - edgeNoise, 0.92, crossDist);

    // ============================================================
    // H. 尾端渐隐 —— 拖尾末端平滑消失
    // ============================================================
    float tailFade = smoothstep(0.0, 0.25, 1.0 - distFromHead);

    // ============================================================
    // 颜色合成
    // ============================================================
    float3 cWhite = float3(1.0, 0.97, 0.92);

    float3 color = float3(0, 0, 0);
    color += cWhite     * core;                              // A: 白热核心
    color += coreColor  * inner;                             // B: 主题内层
    color += glowColor  * outerFade;                         // C: 主题外层
    color += glowColor  * streams;                           // D: 能量流纹
    color += coreColor  * gridLine;                          // E: 数字网格
    color += cWhite     * headOrb * 0.8;                     // F: 光球核心
    color += coreColor  * headGlow;                          // F: 光球辉光
    color += auraColor  * headGlow * 0.3;                    // F: 光球光晕

    float alpha = saturate(
        edgeMask
        + core * 0.6
        + headOrb * 0.5
        + streams * 0.2
        + gridLine * 0.1
    );
    alpha *= fadeAlpha * visMask * tailFade;

    return float4(color * alpha, alpha) * input.Color;
}

technique Technique1
{
    pass CyberTraceBeamPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
