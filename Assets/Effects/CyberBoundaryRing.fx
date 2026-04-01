// ============================================================================
// CyberBoundaryRing.fx — 赛博空间领域常驻边界环
// 从CyberShockwave特化而来，仅保留外环+外侧碎片拖尾
// 去除内侧压缩波余波，避免与CyberspaceField全屏着色器叠加冲突
// ============================================================================

sampler uImage0 : register(s0);
sampler noiseTex : register(s1);

float uTime;
float ringProgress;     //环在归一化空间中的位置
float ringThickness;    //环厚度（归一化）
float fadeAlpha;        //整体淡出 0~1

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    float2 centered = coords * 2.0 - 1.0;
    float dist = length(centered);
    float angle = atan2(centered.y, centered.x);
    float normAngle = (angle + 3.14159) / 6.28318;

    // ---- 噪声扰动环边缘 ----
    float n1 = tex2D(noiseTex, float2(normAngle * 4.0 + uTime * 3.0, ringProgress * 2.0)).r;
    float n2 = tex2D(noiseTex, float2(normAngle * 7.0 - uTime * 2.0, ringProgress + 0.5)).g;
    float noiseDisp = (n1 * 0.6 + n2 * 0.4 - 0.5) * 0.07;

    float adjDist = dist + noiseDisp;

    // ---- 主环形遮罩（纯外环，无内侧增亮）----
    float ringDist = abs(adjDist - ringProgress);
    float ringMask = 1.0 - smoothstep(0.0, ringThickness, ringDist);

    // ---- 外侧数字碎片拖尾 ----
    float trailing = smoothstep(ringProgress + ringThickness * 2.0, ringProgress, adjDist);
    trailing *= trailing;
    float trailNoise = tex2D(noiseTex, float2(normAngle * 16.0 + uTime * 1.5, dist * 3.0)).r;
    trailing *= step(0.42, trailNoise) * 0.5;

    // ---- 内侧硬切：完全丢弃环内像素 ----
    float innerCut = smoothstep(ringProgress - ringThickness * 0.3, ringProgress, adjDist);
    ringMask *= innerCut;

    // ---- 颜色 ----
    float3 coreRed   = float3(0.85, 0.08, 0.06);
    float3 hotEdge   = float3(1.0, 0.50, 0.30);
    float3 darkTrail = float3(0.45, 0.025, 0.035);

    float brightness = ringMask * 0.7;
    float3 ringColor = lerp(coreRed, hotEdge, ringMask * 0.35) * brightness;

    //环上微观数字纹理
    float gridA = frac(normAngle * 48.0);
    float gridR = frac(dist * 28.0);
    float grid = smoothstep(0.03, 0.0, min(gridA, 1.0 - gridA));
    grid += smoothstep(0.015, 0.0, min(gridR, 1.0 - gridR));
    grid = saturate(grid);
    ringColor += float3(0.22, 0.035, 0.02) * grid * ringMask * 0.25;

    float3 trailColor = darkTrail * trailing;

    float3 finalColor = (ringColor + trailColor) * fadeAlpha;
    float alpha = saturate(brightness + trailing * 0.6) * fadeAlpha;

    return float4(finalColor * alpha, alpha) * vertexColor;
}

technique Technique1
{
    pass CyberBoundaryRingPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
