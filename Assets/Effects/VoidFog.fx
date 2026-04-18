// ============================================================================
// VoidFog.fx — 虚空聚落程序化雾气着色器
// 多层随机雾气效果，红色主题
// 使用多层噪声驱动，模拟深度视差的大气雾效
// ============================================================================

sampler uImage0 : register(s0);

float uTime;
float uIntensity;       // 整体强度 0~1
float uAspectRatio;     // 屏幕宽高比
float2 uScreenPos;      // 归一化世界相机位置（用于视差）
float uFogDensity;      // 全局雾气浓度乘数

// ======================== Hash / Noise ========================

float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float vnoise(float2 p)
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

// 分形噪声，octave 间旋转避免轴向伪影
float fbm(float2 p, int oct)
{
    float v = 0.0;
    float a = 0.5;
    float2x2 rot = float2x2(0.8, -0.6, 0.6, 0.8); // ~37° 旋转
    for (int i = 0; i < oct; i++)
    {
        v += a * vnoise(p);
        p = mul(rot, p) * 2.0;
        a *= 0.5;
    }
    return v;
}

// 域弯曲 fbm — 产生更有机的扭曲形态
float warpedFbm(float2 p, float t)
{
    float2 q = float2(
        fbm(p + float2(0.0, 0.0), 3),
        fbm(p + float2(5.2, 1.3), 3)
    );
    float2 r = float2(
        fbm(p + 3.0 * q + float2(1.7, 9.2) + t * 0.05, 3),
        fbm(p + 3.0 * q + float2(8.3, 2.8) + t * 0.04, 3)
    );
    return fbm(p + 2.5 * r, 3);
}

// ======================== 单层雾气 ========================
// depth: 虚拟深度 (0=近, 1=远)
// 近处层移动更快、更浓、更大
// 远处层移动更慢、更淡、更细碎

struct FogLayer
{
    float alpha;
    float3 color;
};

FogLayer computeFogLayer(
    float2 uv,
    float depth,
    float time,
    float2 camOffset,
    float aspectRatio)
{
    FogLayer result;

    // 视差：近层受相机影响大，远层小
    float parallax = lerp(0.4, 0.05, depth);
    float2 worldUV = uv * float2(aspectRatio, 1.0);
    worldUV += camOffset * parallax;

    // 每层独特的缩放和偏移
    float scale = lerp(2.5, 5.5, depth);  // 远处更细碎
    float speed = lerp(0.035, 0.012, depth); // 近处流动更快

    float2 fogUV = worldUV * scale;
    fogUV.x += time * speed;
    fogUV.y += sin(time * speed * 0.7 + depth * 6.28) * 0.15;

    // 使用域弯曲 fbm 产生有机形态
    float n = warpedFbm(fogUV + depth * 37.7, time * 0.3);

    // 附加一层大尺度调制，产生雾气团块感
    float largeMod = fbm(worldUV * 1.2 + float2(depth * 13.0, time * 0.008), 3);
    largeMod = smoothstep(0.3, 0.7, largeMod);

    // 高度梯度：雾气在屏幕下半部分更浓
    float heightGrad = smoothstep(0.05, 0.65, uv.y); // 上方淡出
    heightGrad *= smoothstep(1.0, 0.75, uv.y);       // 最底部略微减弱

    // 组合雾气密度
    float density = n * largeMod * heightGrad;

    // 锐化边缘，让雾气有明显的团状而非均匀
    float threshold = lerp(0.22, 0.32, depth);
    float softness = lerp(0.15, 0.22, depth);
    density = smoothstep(threshold, threshold + softness, density);

    // 近处雾更浓
    float maxAlpha = lerp(0.38, 0.14, depth);
    result.alpha = density * maxAlpha;

    // 颜色：深层暗红偏紫，近层亮红偏橙
    float3 deepColor  = lerp(float3(0.12, 0.02, 0.04), float3(0.08, 0.015, 0.06), depth);
    float3 brightColor = lerp(float3(0.55, 0.08, 0.04), float3(0.35, 0.05, 0.10), depth);

    // 噪声驱动的颜色变化
    float colorVar = fbm(fogUV * 0.6 + float2(depth * 20.0, 0.0), 2);
    result.color = lerp(deepColor, brightColor, saturate(density * 1.8 + colorVar * 0.3));

    // 雾气边缘的微弱辉光（用 Additive 概念来加亮边缘）
    float edgeGlow = smoothstep(threshold - 0.05, threshold + 0.02, n * largeMod * heightGrad);
    edgeGlow -= density;
    edgeGlow = max(edgeGlow, 0.0);
    result.color += float3(0.3, 0.04, 0.02) * edgeGlow * 2.0;

    return result;
}

// ======================== 主像素着色器 ========================

float4 PSVoidFog(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float t = uTime;
    float2 uv = coords;

    // 累积所有雾气层
    float3 fogColor = float3(0, 0, 0);
    float fogAlpha = 0.0;

    // 6 层雾气，从远到近叠加
    // 远处层先绘制（alpha 预乘合成）
    [unroll]
    for (int i = 5; i >= 0; i--)
    {
        float depth = (float)i / 5.0; // 0=最近, 1=最远

        FogLayer layer = computeFogLayer(uv, depth, t, uScreenPos, uAspectRatio);

        // Alpha compositing: over operator
        float a = layer.alpha * uFogDensity;
        fogColor = fogColor * (1.0 - a) + layer.color * a;
        fogAlpha = fogAlpha * (1.0 - a) + a;
    }

    // 屏幕边缘柔和渐隐（暗角效果）
    float2 vc = (uv - 0.5) * 2.0;
    vc.x *= uAspectRatio * 0.5;
    float vignette = 1.0 - dot(vc, vc) * 0.15;
    vignette = saturate(vignette);

    fogAlpha *= vignette * uIntensity;
    fogColor *= vignette;

    // 添加微弱的整体底色，让虚空有一种弥漫的红色气氛
    float ambientFog = smoothstep(0.1, 0.7, uv.y) * 0.06 * uIntensity;
    fogColor += float3(0.15, 0.02, 0.03) * ambientFog;
    fogAlpha += ambientFog;

    return float4(fogColor, saturate(fogAlpha));
}

technique VoidFog
{
    pass P0
    {
        PixelShader = compile ps_3_0 PSVoidFog();
    }
}
