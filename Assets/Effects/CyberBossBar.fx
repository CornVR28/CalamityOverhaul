// ============================================================================
// CyberBossBar.fx — 赛博朋克2077风格Boss血条着色器
// 分段血条泛光 + 扫描线质感 + 边缘辉光 + 受击脉冲
// ============================================================================

sampler uImage0 : register(s0);

float uTime;
float uLifeRatio;      //当前生命比例0~1
float uHitFlash;       //受击闪烁强度0~1
float2 uBarSize;       //血条像素尺寸

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 uv = coords;

    //基色
    float3 coreRed = float3(0.82, 0.11, 0.11);
    float3 hotRed = float3(1.0, 0.30, 0.18);
    float3 dimRed = float3(0.18, 0.03, 0.03);

    //分段遮罩
    float segX = frac(uv.x * 20.0);
    float segMask = step(0.04, segX) * step(segX, 0.96);

    //填充遮罩
    float fillMask = step(uv.x, uLifeRatio);

    //扫描线（用像素坐标）
    float scan = 0.90 + 0.10 * frac(uv.y * uBarSize.y * 0.5);

    //纵向边缘柔化
    float edgeY = smoothstep(0.0, 0.2, uv.y) * smoothstep(0.0, 0.2, 1.0 - uv.y);

    //填充区颜色
    float3 barColor = coreRed * segMask * scan * edgeY;

    //顶部高光
    float topHL = saturate(0.35 - uv.y) * 0.8;
    barColor += hotRed * topHL * segMask;

    //末端辉光
    float dist = abs(uv.x - uLifeRatio);
    float endGlow = saturate(1.0 - dist * 35.0) * 0.5;
    barColor += hotRed * endGlow * edgeY;

    //受击脉冲
    barColor += hotRed * uHitFlash * segMask * 0.4;

    //组合
    float3 empty = dimRed * segMask * edgeY * 0.2;
    float3 finalColor = lerp(empty, barColor, fillMask);

    float alpha = saturate(fillMask * segMask * edgeY + endGlow * 0.3 + uHitFlash * 0.15);
    alpha = max(alpha, segMask * edgeY * 0.15);

    return float4(finalColor * alpha, alpha) * vertexColor;
}

technique Technique1
{
    pass CyberBossBarPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
