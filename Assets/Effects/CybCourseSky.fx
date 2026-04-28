// ============================================================================
// CybCourseSky.fx — 超梦沉浸空间天空着色器
// 赛博朋克风格数字深空夜景，全程序化无外部纹理依赖
// 深蓝黑渐变 + 极光状大气带 + 星云薄雾 + 稀疏星辰 + 地平线城市余光
// 不含扫描线，低视觉疲劳，高可读性，科幻感强
// ============================================================================

float uTime;
float uIntensity;
float uAspectRatio;

#define TAU 6.28318530

// ==================== Hash / Noise ====================

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

// 两八度轻量fbm，用于星云与极光扰动
float fbm2(float2 p)
{
    return vnoise(p) * 0.625 + vnoise(p * 2.1 + float2(3.7, 8.1)) * 0.375;
}

// 单层星辰，返回该UV处的亮度
float starLayer(float2 uv, float scale, float seed)
{
    float2 id  = floor(uv * scale);
    float2 sub = frac(uv * scale) - 0.5;
    float  h   = hash21(id + seed);
    if (h < 0.80) return 0.0;
    h = (h - 0.80) / 0.20;
    float2 off = (hash22(id + seed * 1.37) - 0.5) * 0.38;
    float  d   = length(sub - off);
    float  tw  = sin(uTime * (1.0 + h * 2.2) + hash11(h + seed) * TAU) * 0.15 + 0.85;
    return h * h * tw * smoothstep(0.09, 0.0, d);
}

// ==================== 主函数 ====================

float4 PSCybCourseSky(float2 uv : TEXCOORD0) : COLOR0
{
    // 宽高比修正UV，用于水平方向敏感的效果（极光波形、星云）
    float2 uvW = float2(uv.x * uAspectRatio, uv.y);
    float  t   = uTime * 0.065;

    // ================================================================
    // Layer 1 — 基础渐变天空
    // 顶部深空黑蓝 → 底部略亮的午夜蓝
    // ================================================================
    float3 topCol = float3(0.016, 0.022, 0.052);
    float3 botCol = float3(0.026, 0.048, 0.095);
    float3 col    = lerp(topCol, botCol, pow(saturate(uv.y), 0.5));

    // ================================================================
    // Layer 2 — 极光带
    // 两条正弦弯曲的水平光带，颜色极淡，避免喧宾夺主
    // 第一条：青色调（y≈0.36）
    // 第二条：蓝紫调（y≈0.62）
    // ================================================================
    float w1 = sin(uvW.x * 2.4 + t * 1.15) * 0.032
             + sin(uvW.x * 5.3 - t * 0.72) * 0.014;
    float b1 = exp(-pow(abs(uv.y - 0.36 - w1) * 30.0, 1.7));
    col += float3(0.04, 0.26, 0.38) * b1 * 0.085;

    float w2 = sin(uvW.x * 1.9 - t * 0.88) * 0.028
             + sin(uvW.x * 4.7 + t * 0.58) * 0.011;
    float b2 = exp(-pow(abs(uv.y - 0.62 - w2) * 24.0, 1.5));
    col += float3(0.06, 0.12, 0.34) * b2 * 0.065;

    // ================================================================
    // Layer 3 — 星云薄雾
    // 极低不透明度的fbm纹理，注入蓝调层次感，不遮挡任何内容
    // ================================================================
    float2 nebUV = uvW * float2(0.65, 1.05) + float2(t * 0.022, 0.0);
    float  neb   = fbm2(nebUV * 1.25);
    col += float3(0.025, 0.07, 0.16) * neb * 0.065;

    // ================================================================
    // Layer 4 — 星辰（三层：细密/稠密/稀疏偏亮）
    // 全部偏蓝白色调，避免暖色系干扰科幻氛围
    // ================================================================
    col += float3(0.76, 0.87, 1.00) * starLayer(uv, 22.0,  0.0) * 0.48;
    col += float3(0.70, 0.82, 1.00) * starLayer(uv, 50.0, 27.3) * 0.40;
    col += float3(0.86, 0.92, 1.00) * starLayer(uv,  9.0,  6.8) * 0.58;

    // ================================================================
    // Layer 5 — 地平线城市余光
    // 天空底部隐约可见的大气辉光，暗示地平线外的赛博都市
    // 冷蓝为主色（霓虹与屏幕漫反射），极微弱暖橙（废热残余）
    // ================================================================
    float horizon = smoothstep(0.42, 1.0, uv.y);
    col += float3(0.038, 0.115, 0.230) * horizon * 0.32;
    col += float3(0.085, 0.050, 0.018) * horizon * 0.09;

    // ================================================================
    // 最终输出，淡入淡出由uIntensity控制
    // ================================================================
    col *= uIntensity;
    return float4(saturate(col), 1.0);
}

technique CybCourseSky
{
    pass P0
    {
        PixelShader = compile ps_3_0 PSCybCourseSky();
    }
}
