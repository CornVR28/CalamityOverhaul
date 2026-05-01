// ============================================================================
// CyberRiftSlash.fx — 赛博空间维度数据裂缝着色器（瞬移技能专用）
// 黑墙数据撕裂：白炽核 + 鲜橙红中层 + 暗红黑墙余烬 + 平行幽灵线
// 强调"力量感与干脆感"——快速延伸/全亮闪烁/急速尾缩三段动画
// Trail条带渲染，配合 CyberRiftSlashProj 使用
// ============================================================================

float4x4 transformMatrix;
float uTime;
float fadeAlpha;        //整体透明度 0~1
float visibleStart;     //可见段起点 0~1（尾收缩时上升）
float visibleEnd;       //可见段终点 0~1（头延伸时上升）
float glitchSeed;       //本实例随机种子
float impactPulse;      //冲击脉冲强度（命中目标瞬间提亮，0~1）

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

float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float2 uv = input.TexCoords;
    float along = uv.x;
    float cross_ = uv.y;
    float crossDist = abs(cross_ - 0.5) * 2.0;

    // ---- 可见区域遮罩 ----
    float headMask = smoothstep(visibleEnd + 0.04, visibleEnd - 0.02, along);
    float tailMask = smoothstep(visibleStart - 0.02, visibleStart + 0.04, along);
    float visMask = headMask * tailMask;
    if (visMask < 0.001)
        return float4(0, 0, 0, 0);

    // ---- 噪声采样 ----
    float n1 = tex2D(noiseSamp, frac(float2(along * 3.5 + uTime * 1.4, cross_ * 0.6 + glitchSeed))).r;
    float n2 = tex2D(noiseSamp, frac(float2(along * 8.0 - uTime * 2.3, cross_ * 1.4 + 0.37))).g;
    float n3 = tex2D(noiseSamp, frac(float2(along * 2.0 + uTime * 0.7, cross_ * 2.8 + 0.61))).b;

    // ============================================================
    // A. 核心裂缝——白热维度撕裂芯（撕开现实的高能缝隙）
    // ============================================================
    float coreW = 0.085 + n1 * 0.05;
    float core = 1.0 - smoothstep(0.0, coreW, crossDist);
    core = pow(saturate(core), 1.2);
    //核心持续高频闪烁，营造能量过载感
    float coreFlicker = 0.78 + 0.22 * sin(uTime * 28.0 + along * 60.0 + glitchSeed * 11.0);
    core *= coreFlicker;

    // ============================================================
    // B. 中层鲜橙红辉光——主色调（区别于赛博领域底层的深红，更亮更饱和）
    // ============================================================
    float midW = 0.30 + n2 * 0.10;
    float mid = 1.0 - smoothstep(coreW * 0.5, midW, crossDist);
    mid = pow(saturate(mid), 1.15) * 0.78;

    // ============================================================
    // C. 外层暗红黑墙余烬光晕
    // ============================================================
    float outer = 1.0 - smoothstep(0.18, 0.95, crossDist);
    outer *= 0.32;

    // ============================================================
    // D. 平行幽灵线——表现"维度裂缝"的多层位错感
    // ============================================================
    //上下两条平行的次级亮带，距中线约 0.22 处
    float ghostUpper = 1.0 - smoothstep(0.0, 0.05, abs(cross_ - 0.5 - 0.22));
    float ghostLower = 1.0 - smoothstep(0.0, 0.05, abs(cross_ - 0.5 + 0.22));
    float ghostMod = 0.5 + 0.5 * sin(along * 70.0 + uTime * 18.0 + glitchSeed * 6.0);
    float ghost = (ghostUpper + ghostLower) * ghostMod * 0.55;
    //向尖端集中（让幽灵线在头部更明显）
    ghost *= smoothstep(0.0, 0.4, along);

    // ============================================================
    // E. 数字数据方块——黑墙入侵的破碎数据流
    // ============================================================
    //大方块层（沿裂缝主轴硬切的块）
    float bx = floor(along * 22.0 + glitchSeed * 5.0);
    float by = floor(cross_ * 5.0);
    float blockTime = floor(uTime * 12.0);
    float bHash = hash21(float2(bx + blockTime * 7.1, by + glitchSeed * 13.0));
    float blockOn = step(0.55, bHash);
    float blockFill = bHash * blockOn;
    //硬边矩形内边距
    float bxFrac = frac(along * 22.0 + glitchSeed * 5.0);
    float byFrac = frac(cross_ * 5.0);
    float blockEdge = step(0.07, bxFrac) * step(bxFrac, 0.93)
                    * step(0.09, byFrac) * step(byFrac, 0.91);
    blockFill *= blockEdge;

    //小碎片层
    float sbx = floor(along * 56.0);
    float sby = floor(cross_ * 10.0);
    float sTime = floor(uTime * 18.0);
    float sHash = hash21(float2(sbx + sTime * 11.3, sby + glitchSeed * 7.7));
    float subBlock = step(0.74, sHash) * sHash;
    subBlock *= (1.0 - crossDist * 0.55);

    // ============================================================
    // F. 沿裂缝快速流动的数据条纹（强调"劈"的方向感）
    // ============================================================
    float streamUV = frac(along * 14.0 - uTime * 6.0 + glitchSeed * 4.0);
    float stream = smoothstep(0.0, 0.06, streamUV) * smoothstep(0.32, 0.10, streamUV);
    stream *= (1.0 - crossDist * 0.7) * 0.55;

    // ============================================================
    // G. 边缘腐蚀——撕裂边界的不规则噪声
    // ============================================================
    float edgeNoise = n2 * 0.20 + n3 * 0.20;
    float edgeMask = 1.0 - smoothstep(0.48 - edgeNoise, 0.94, crossDist);

    // ============================================================
    // H. 尖端能量爆发——强调"劈砍"的尖锐感（沿延伸前端集中能量）
    // ============================================================
    float tipDist = abs(along - visibleEnd);
    float tipFlare = 1.0 - smoothstep(0.0, 0.05, tipDist);
    tipFlare *= (1.0 - crossDist * 0.6);
    float tipPulse = 0.5 + 0.5 * sin(uTime * 36.0 + glitchSeed * 9.0);
    tipFlare *= 0.55 + 0.45 * tipPulse;

    //冲击脉冲：命中目标瞬间整段亮起一次
    float impactGlow = impactPulse * (1.0 - crossDist * 0.4) * 0.7;

    // ============================================================
    // 颜色合成——突出橙红高亮主题，与领域底色形成层次差
    // ============================================================
    float3 cWhiteHot   = float3(1.00, 0.96, 0.88);
    float3 cBrightOrng = float3(1.00, 0.45, 0.15);   //鲜橙红中层
    float3 cBrightRed  = float3(1.00, 0.18, 0.08);   //鲜红辅助
    float3 cDarkRed    = float3(0.30, 0.020, 0.030); //黑墙余烬
    float3 cBlockOrng  = float3(0.95, 0.32, 0.10);   //数据方块橙
    float3 cSubBlock   = float3(1.00, 0.55, 0.25);
    float3 cStream     = float3(1.00, 0.40, 0.18);
    float3 cTip        = float3(1.00, 0.70, 0.45);
    float3 cGhost      = float3(1.00, 0.30, 0.12);
    float3 cImpact     = float3(1.00, 0.80, 0.55);

    float3 color = float3(0, 0, 0);
    color += cWhiteHot   * core;                            // A
    color += cBrightOrng * mid * 0.7;                       // B 主橙
    color += cBrightRed  * mid * 0.35;                      // B 红色辅助
    color += cDarkRed    * outer;                           // C
    color += cGhost      * ghost;                           // D 平行幽灵线
    color += cBlockOrng  * blockFill * edgeMask * 0.65;     // E 大方块
    color += cSubBlock   * subBlock * 0.40;                 // E 小碎片
    color += cStream     * stream;                          // F 数据流
    color += cTip        * tipFlare;                        // H 尖端
    color += cImpact     * impactGlow;                      // 命中冲击

    float alpha = saturate(
        edgeMask
        + core * 0.6
        + ghost * 0.4
        + blockFill * 0.30
        + subBlock * 0.18
        + stream * 0.20
        + tipFlare * 0.30
        + impactGlow * 0.45
    );
    alpha *= fadeAlpha * visMask;

    return float4(color * alpha, alpha) * input.Color;
}

technique Technique1
{
    pass RiftSlashPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
