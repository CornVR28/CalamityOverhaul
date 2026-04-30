// ============================================================================
// OceanWaterBlob.fx — 海洋水滴/水花/水头精灵着色器
// 程序化折射 · 动态焦散 · 自适应泡沫边 · 生物荧光内核
// 用于 SpriteBatch 批量渲染水滴、泡沫、头部水球等水状粒子
// ============================================================================

float uTime;
float foamThreshold;     // 泡沫边触发阈值 0~1（基于 base alpha）
float refractStrength;   // 折射偏移强度 0~0.1
float coreShimmerSpeed;  // 内核闪烁速度
float3 foamColor;        // 泡沫高光色
float3 bioColor;         // 生物荧光高光色
float3 highlightColor;   // 顶点高光色

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

// SpriteBatch 自动绑定纹理至 register(s0)
sampler baseSamp : register(s0);

float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

struct PSInput
{
    float4 Position : POSITION0;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float2 uv = input.TexCoords;

    // ========= 噪声驱动的折射偏移 =========
    // 在 UV 上施加微小扰动，使水滴外形像被折射的水团
    float2 nUV = float2(uv.x * 2.4 + uTime * 0.55, uv.y * 2.4 - uTime * 0.45);
    float n1 = tex2D(noiseSamp, frac(nUV)).r;
    float n2 = tex2D(noiseSamp, frac(nUV * 1.7 + 0.31)).g;
    float n3 = tex2D(noiseSamp, frac(nUV * 0.9 + 0.71)).b;

    float2 refractDir = float2(n1 - 0.5, n2 - 0.5) * refractStrength;
    float4 baseC = tex2D(baseSamp, uv + refractDir);
    float baseAlpha = baseC.a;
    if (baseAlpha < 0.005) return float4(0, 0, 0, 0);

    // ========= 焦散闪光（高频亮点） =========
    float caustic = pow(saturate(n1 * n2), 4.0) * 5.0;
    float causticPulse = 0.6 + 0.4 * sin(uTime * coreShimmerSpeed + (uv.x + uv.y) * 22.0);
    caustic *= causticPulse;

    // ========= 内核（生物荧光，越接近不透明区域越亮） =========
    float coreT = smoothstep(0.55, 1.0, baseAlpha);
    float coreShimmer = 0.55 + 0.45 * sin(uTime * coreShimmerSpeed * 1.3 + n3 * 8.0);
    float core = coreT * coreShimmer;

    // ========= 泡沫边（在中-低 alpha 区域出现） =========
    //   * 噪声触发让泡沫呈"气泡"分布而非纯环形
    float foamRing = smoothstep(foamThreshold, foamThreshold + 0.20, baseAlpha)
                   * smoothstep(0.92, 0.55, baseAlpha);
    float foamMask = foamRing * saturate(0.35 + n1 * 0.85);
    foamMask = saturate(foamMask);

    // ========= 顶点高光（左上方光照感） =========
    float2 lightDir = normalize(float2(-0.4, -0.55));
    float2 centered = uv - 0.5;
    float lightDot = saturate(dot(normalize(centered + 0.0001), lightDir));
    float topHighlight = pow(lightDot, 3.0) * coreT * 0.5;

    // ========= 颜色合成 =========
    float3 baseTint = input.Color.rgb;

    float3 col = baseTint * baseAlpha;
    col += bioColor       * core * 0.7;
    col += highlightColor * topHighlight;
    col += foamColor      * foamMask * 1.25;
    col += foamColor      * caustic * coreT * 0.40;

    // 输出 alpha：在 baseAlpha 基础上叠加泡沫贡献
    float outA = baseAlpha * input.Color.a;
    outA += foamMask * input.Color.a * 0.45;
    outA = saturate(outA);

    // 输出预乘色，配合上层 BlendState（Additive 或 AlphaBlend 都能正确呈现）
    return float4(col * input.Color.a, outA);
}

technique Technique1
{
    pass OceanWaterBlobPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
