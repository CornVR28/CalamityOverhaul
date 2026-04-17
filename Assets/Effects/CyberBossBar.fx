// ============================================================================
// CyberBossBar.fx — 赛博朋克2077风格Boss血条着色器
// 多层辉光叠加 + 内发光边缘 + 扫描线噪波 + 能量脉冲
// ============================================================================

sampler uImage0 : register(s0);

float uTime;
float uLifeRatio;
float uHitFlash;
float2 uBarSize;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 uv = coords;

    //血条核心色阶
    float3 deepRed = float3(0.55, 0.04, 0.04);
    float3 coreRed = float3(0.88, 0.12, 0.08);
    float3 hotWhite = float3(1.0, 0.55, 0.40);

    //20段分隔，间隙更窄更精致
    float seg = frac(uv.x * 20.0);
    float gap = step(0.03, seg) * step(seg, 0.97);

    //填充区
    float fill = step(uv.x, uLifeRatio);

    //纵向内发光：边缘亮中间略暗，模拟管状光源
    float ey = uv.y * (1.0 - uv.y) * 4.0;
    float tubeLit = 0.55 + ey * 0.45;

    //顶部锐利高光条，模拟玻璃反光
    float specular = saturate(1.0 - abs(uv.y - 0.18) * 12.0) * 0.6;

    //扫描线干扰纹
    float scanPx = frac(uv.y * uBarSize.y * 0.5);
    float scan = 0.92 + scanPx * 0.08;

    //填充区最终颜色
    float3 barCol = coreRed * tubeLit * scan * gap;
    barCol += hotWhite * specular * gap;

    //受击高亮
    barCol += hotWhite * uHitFlash * gap * 0.5;

    //空区暗色轮廓
    float3 emptyCol = deepRed * gap * ey * 0.12;

    float3 color = lerp(emptyCol, barCol, fill);
    float a = lerp(gap * ey * 0.08, saturate(gap * tubeLit + specular * 0.3), fill);

    //末端辉光
    float endD = abs(uv.x - uLifeRatio);
    float endG = saturate(1.0 - endD * 30.0) * 0.45 * fill;
    color += hotWhite * endG * ey;
    a = saturate(a + endG);

    return float4(color * a, a) * vertexColor;
}

technique Technique1
{
    pass CyberBossBarPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
