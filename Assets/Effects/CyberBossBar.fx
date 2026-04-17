// ============================================================================
// CyberBossBar.fx — 赛博朋克2077风格Boss血条着色器
// 弧形band + 管状明暗 + 高光条 + 分段 + 末端辉光
// ============================================================================

sampler uImage0 : register(s0);

float uTime;
float uLifeRatio;
float uHitFlash;
float2 uBarSize;

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 uv = coords;

    float3 darkRed = float3(0.40, 0.02, 0.02);
    float3 coreRed = float3(0.84, 0.10, 0.06);
    float3 hot = float3(1.0, 0.42, 0.28);

    //弧形band中心：中间上凸两端下沉
    float dx = uv.x - 0.5;
    float bandY = 0.5 - dx * dx * 0.45;
    float dist = abs(uv.y - bandY);

    //band遮罩含管状明暗
    float band = smoothstep(0.28, 0.10, dist);

    //分段
    float seg = frac(uv.x * 20.0);
    float gap = step(0.025, seg) * step(seg, 0.975);

    //填充
    float fill = step(uv.x, uLifeRatio);

    //高光条偏向band上沿
    float relY = uv.y - bandY;
    float spec = saturate(1.0 - abs(relY + 0.065) * 16.0) * 0.38 * band;

    //填充色
    float hitGlow = uHitFlash * band * 0.25;
    float3 barCol = coreRed * band * gap + hot * gap * (spec + hitGlow);

    //空区暗色轮廓
    float3 emptyCol = darkRed * gap * band * 0.05;

    float3 color = lerp(emptyCol, barCol, fill);
    float a = lerp(gap * band * 0.025, gap * band + spec * 0.1, fill);

    //末端辉光
    float endG = saturate(1.0 - abs(uv.x - uLifeRatio) * 18.0) * 0.25 * fill * band;
    color += hot * endG;
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
