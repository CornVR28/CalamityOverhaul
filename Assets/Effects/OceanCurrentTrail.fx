// ============================================================================
// OceanCurrentTrail.fx — 海洋洪流核心拖尾着色器
// 域扭曲噪声卷流 + 表面焦散 + 流动条纹 + 边缘泡沫带 + 头部加亮
// 与 OceanCurrent.cs 的 IPrimitiveDrawable.Trail 配合使用
// ============================================================================

float4x4 transformMatrix;
float uTime;
float fadeAlpha;        // 整体淡出 0~1（用于飞溅/消散平滑过渡）
float pulse;            // 0~1 呼吸脉动
float speedRatio;       // 0~1 速度归一化（拉伸条纹强度）
float foamDensity;      // 0~1 泡沫密度（越高越湍急）
float3 deepColor;       // 深海色（中心深色）
float3 shallowColor;    // 浅层色（边缘渐变）
float3 foamColor;       // 泡沫高光色
float3 bioColor;        // 生物荧光高光色

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

texture uFlowTex;
sampler flowSamp = sampler_state
{
    texture = <uFlowTex>;
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
    float along = input.TexCoords.x;            // 0=头部, 1=尾端
    float cross_ = input.TexCoords.y;           // 0..1 横向
    float crossDist = abs(cross_ - 0.5) * 2.0;  // 0=中心, 1=边缘

    // ============================================================
    // A. 域扭曲（Domain Warp） —— 用 Flow 纹理偏移采样 UV
    //    模拟流体漩涡卷动，让噪声纹理"流"起来
    // ============================================================
    float2 flowUV = frac(float2(along * 1.4 - uTime * 0.55, cross_ * 1.6 + uTime * 0.15));
    float2 flow = (tex2D(flowSamp, flowUV).rg - 0.5) * 0.22;
    flow.x += sin(along * 12.0 + uTime * 3.0) * 0.025;

    // ============================================================
    // B. 多频噪声组合（高低频水团结构）
    // ============================================================
    float n1 = tex2D(noiseSamp, frac(float2(along * 3.0 + flow.x - uTime * 0.95, cross_ * 1.5 + flow.y))).r;
    float n2 = tex2D(noiseSamp, frac(float2(along * 6.5 - uTime * 1.35 + flow.x * 0.5, cross_ * 2.5 + 0.31 + flow.y * 0.5))).g;
    float n3 = tex2D(noiseSamp, frac(float2(along * 13.0 + uTime * 1.8, cross_ * 4.2 + 0.12))).b;
    float waterField = n1 * 0.55 + n2 * 0.30 + n3 * 0.15;

    // ============================================================
    // C. 截面厚度 —— 噪声让边缘呈不规则水绺
    // ============================================================
    float thickness = 1.0 - crossDist;
    thickness *= 0.74 + waterField * 0.50;
    thickness = saturate(thickness);

    // ============================================================
    // D. 内核（亮白能量芯） —— 头部更宽，尾端收窄
    // ============================================================
    float coreWidth = 0.34 + n1 * 0.10;
    coreWidth *= lerp(1.20, 0.45, along);     // 头宽尾窄
    float core = saturate(1.0 - smoothstep(0.0, coreWidth, crossDist));
    core = pow(core, 1.4);
    core *= 0.72 + 0.28 * pulse;

    // ============================================================
    // E. 焦散波光（Voronoi 风格 min(n2,n3) + 沿轴亮带）
    // ============================================================
    float caustic = min(n2, n3);
    caustic = pow(caustic, 0.4) * 1.3;
    float causticBand = sin(along * 24.0 - uTime * 8.5 + waterField * 7.0) * 0.5 + 0.5;
    caustic = saturate(caustic * (0.35 + causticBand * 0.65));
    caustic *= (1.0 - crossDist * 0.55);

    // ============================================================
    // F. 流动条纹（沿轴推进的水流条带，速度越快越明显）
    // ============================================================
    float stripe1 = frac(along * 8.0 - uTime * 3.0 + waterField * 0.5);
    float streamA = smoothstep(0.0, 0.10, stripe1) * smoothstep(0.42, 0.20, stripe1);
    float stripe2 = frac(along * 14.0 - uTime * 4.5 + n2 * 0.4);
    float streamB = smoothstep(0.0, 0.06, stripe2) * smoothstep(0.20, 0.10, stripe2);
    float streamStripes = (streamA + streamB * 0.5) * (1.0 - crossDist * 0.45) * speedRatio;

    // ============================================================
    // G. 边缘泡沫带 —— 噪声触发的高对比白色泡沫
    //     比单纯的 alpha 衰减更具有"沸腾"感
    // ============================================================
    float foamCore = smoothstep(0.50, 0.92, crossDist + waterField * 0.20);
    foamCore *= smoothstep(1.05, 0.62, crossDist);
    float foamBubbles = step(0.55 - foamDensity * 0.15, n1 + n2 * 0.55) * foamCore;
    float foamMask = saturate(foamBubbles + foamCore * 0.32);
    // 让泡沫沿水流推进（动态而非静止）
    float foamFlick = 0.65 + 0.35 * sin(uTime * 7.0 + n3 * 18.0 + along * 10.0);
    foamMask *= foamFlick;

    // ============================================================
    // H. 头部圆饼亮区 + 头部光环
    // ============================================================
    float headCap = 1.0 - smoothstep(0.0, 0.07, along);
    headCap *= 1.0 - crossDist * 0.55;
    float headRim = (1.0 - smoothstep(0.05, 0.18, along)) * (1.0 - crossDist * 0.7) * 0.65;

    // ============================================================
    // I. 尾端淡出
    // ============================================================
    float tailFade = 1.0 - smoothstep(0.72, 1.0, along);

    // ============================================================
    // 颜色合成
    // ============================================================
    float depth = 1.0 - crossDist;
    float3 bodyColor = lerp(shallowColor, deepColor, saturate(depth * 0.85 + waterField * 0.10));

    float3 col = bodyColor * thickness * (0.62 + waterField * 0.40);
    col += bioColor    * core * 0.95;
    col += float3(1.0, 1.0, 1.0) * core * 0.40;
    col += foamColor   * caustic * 0.55;
    col += foamColor   * streamStripes * 0.60;
    col += foamColor   * foamMask * 1.15;
    col += float3(1.0, 1.0, 1.0) * headCap * 0.75;
    col += bioColor    * headRim;

    float alpha = thickness * 0.85;
    alpha += core * 0.80;
    alpha += foamMask * 0.55;
    alpha += caustic * 0.18;
    alpha += headCap * 0.50;
    alpha = saturate(alpha) * tailFade * fadeAlpha;

    return float4(col * alpha, alpha) * input.Color;
}

technique Technique1
{
    pass OceanCurrentTrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
