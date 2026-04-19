// ============================================================================
// CyberBossBar.fx 赛博朋克2077风格敌人血条(精简版 ps_2_0)
// 平行四边形 / 刻度 / 扫描线 / 威胁色渐变 / 受击白闪
// ============================================================================

sampler uImage0 : register(s0);

float uTime;
float uLifeRatio;
float uHitFlash;
float2 uBarSize;

float4 PixelShaderFunction(float2 uv : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    //平行四边形边界：上边右移下边左移
    float skew = 0.06;
    float leftEdge  = skew * (1.0 - uv.y);
    float rightEdge = 1.0 - skew * uv.y;
    float inside = step(leftEdge, uv.x) * step(uv.x, rightEdge);

    float t = (uv.x - leftEdge) / (rightEdge - leftEdge);

    //威胁色渐变(红→琥珀黄)
    float3 high = float3(1.00, 0.82, 0.08);
    float3 low  = float3(1.00, 0.12, 0.16);
    float3 barCol = lerp(low, high, smoothstep(0.15, 0.85, uLifeRatio));

    //填充遮罩
    float filled = step(t, uLifeRatio);

    //管状渐变(中心亮)
    float dy = uv.y - 0.5;
    float vert = 1.0 - dy * dy * 2.0;

    //扫描线
    float scan = 1.0 - step(0.5, frac(uv.y * uBarSize.y * 0.6)) * 0.15;

    //刻度：每10%一条
    float tickPos = abs(frac(t * 10.0) - 0.5);
    float tick = step(0.46, tickPos);

    //领先缘亮
    float lead = saturate(1.0 - abs(t - uLifeRatio) * 80.0) * filled;

    //沿斜边的左右亮线(平行四边形侧缘)
    float edgeLeft  = smoothstep(0.012, 0.0, (uv.x - leftEdge));
    float edgeRight = smoothstep(0.012, 0.0, (rightEdge - uv.x));
    float sideEdge = (edgeLeft + edgeRight) * inside;

    //空区底色
    float3 emptyCol = float3(0.08, 0.04, 0.02);

    //填充着色
    float3 fillCol = barCol * vert * scan;
    fillCol += barCol * lead * 1.8;
    fillCol = lerp(fillCol, float3(1.0, 0.95, 0.8), uHitFlash * 0.45);

    float3 color = lerp(emptyCol, fillCol, filled);
    color -= tick * 0.18;
    color += barCol * sideEdge * 0.9;

    return float4(color, inside) * vertexColor;
}

technique Technique1
{
    pass CyberBossBarPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
