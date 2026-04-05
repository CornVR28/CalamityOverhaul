//=============================================================================
//AccretionDisk.fx - 中央天体着色器（黑洞/能量核心）
//特性: 引力透镜扭曲、多层等离子体旋转、事件视界暗区、
//      光子环、日冕辐射、临边增亮、能量脉动
//=============================================================================

sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
texture noiseTexture;
sampler noiseTex = sampler_state
{
    texture = <noiseTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

matrix transformMatrix;
float uTime;
float rotationSpeed;
float innerRadius;
float outerRadius;
float2 centerPos;
float brightness;
float distortionStrength;

//三层渐变颜色
float4 innerColor;
float4 midColor;
float4 outerColor;

struct VertexShaderInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoords : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoords : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Color = input.Color;
    output.TexCoords = input.TexCoords;
    output.Position = mul(input.Position, transformMatrix);
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TexCoords;
    float2 center = float2(0.5, 0.5);
    float2 toCenter = coords - center;

    float dist = length(toCenter);
    float angle = atan2(toCenter.y, toCenter.x);

    //=== 引力透镜扭曲 ===
    //模拟光线被强引力场弯曲
    float lensStr = 0.025;
    float2 lensed = toCenter;
    if (dist > 0.001)
    {
        float lensFactor = lensStr / (dist * dist + 0.008);
        lensed = toCenter * (1.0 + lensFactor);
    }
    float lDist = length(lensed);
    float lAngle = atan2(lensed.y, lensed.x);

    //=== 三层差分速率等离子体旋转 ===
    //模拟等离子体在不同深度以不同速率旋转
    float rA1 = lAngle + uTime * rotationSpeed * 1.0;
    float rA2 = lAngle + uTime * rotationSpeed * 0.55 + 2.094; //120度偏移
    float rA3 = lAngle + uTime * rotationSpeed * 1.7 + 4.189;  //240度偏移

    float2 pUV1 = float2(cos(rA1), sin(rA1)) * lDist * 2.0;
    float2 pUV2 = float2(cos(rA2), sin(rA2)) * lDist * 3.5;
    float2 pUV3 = float2(cos(rA3), sin(rA3)) * lDist * 6.0;

    float4 p1 = tex2D(noiseTex, pUV1 + float2(uTime * 0.04, uTime * 0.025));
    float4 p2 = tex2D(noiseTex, pUV2 + float2(-uTime * 0.035, uTime * 0.05));
    float4 p3 = tex2D(noiseTex, pUV3 + float2(uTime * 0.07, -uTime * 0.03));

    //等离子体强度合成
    float plasma = p1.r * 0.48 + p2.g * 0.32 + p3.b * 0.20;

    //=== 事件视界暗区 ===
    //极端引力导致光线无法逃逸的核心暗区
    float eventH = 0.055;
    float horizonMask = smoothstep(eventH - 0.015, eventH + 0.04, dist);

    //=== 光子球/光子环 ===
    //光线在临界轨道上的积累形成极亮的环
    float photonR = eventH + 0.035;
    float photonRing = exp(-pow((dist - photonR) * 45.0, 2.0));
    float3 photonCol = lerp(innerColor.rgb, float3(1.0, 0.97, 0.93), 0.65);

    //=== 温度梯度颜色 ===
    float sphereR = outerRadius * 0.42;
    float normD = saturate((dist - eventH) / (sphereR - eventH));
    float4 baseColor;
    if (normD < 0.35)
    {
        baseColor = lerp(innerColor * 1.6, midColor, normD / 0.35);
    }
    else
    {
        baseColor = lerp(midColor, outerColor, (normD - 0.35) / 0.65);
    }

    //=== 日冕效果 ===
    //高温等离子体大气层的散射辐射
    float corona = exp(-dist * 3.8) * plasma;
    float3 coronaCol = innerColor.rgb * 1.6;

    //=== 双频能量脉动 ===
    float ePulse1 = sin(uTime * 2.5 + dist * 12.0) * 0.18 + 0.82;
    float ePulse2 = sin(uTime * 4.2 - dist * 18.0 + plasma * 5.0) * 0.12 + 0.88;

    //=== 放射状射线 ===
    //磁场约束下的高能粒子束
    float rays = sin(angle * 8.0 + uTime * 1.5 + plasma * 4.0);
    rays = pow(abs(rays), 3.5) * 0.35;

    //=== 球体遮罩 ===
    float sphereMask = 1.0 - smoothstep(sphereR - 0.04, sphereR + 0.015, dist);

    //=== 最终合成 ===
    float intensity = sphereMask * plasma * horizonMask * ePulse1 * ePulse2;
    intensity *= brightness;

    float4 finalColor;
    finalColor.rgb = baseColor.rgb * intensity;

    //日冕辐射
    finalColor.rgb += coronaCol * corona * brightness * 0.9;

    //光子环
    finalColor.rgb += photonCol * photonRing * brightness * 3.5;

    //放射状射线
    finalColor.rgb += innerColor.rgb * rays * sphereMask * brightness * 0.45;

    //临边增亮（球体边缘因视线穿过更多大气而更亮）
    float limbSq = max(1.0 - pow(dist / sphereR, 2.0), 0.0);
    float limbBright = (1.0 - sqrt(limbSq)) * sphereMask;
    finalColor.rgb += innerColor.rgb * limbBright * 0.55 * brightness;

    //核心白热光
    float coreGlow = pow(saturate(1.0 - dist / eventH), 2.5) * (1.0 - horizonMask);
    finalColor.rgb += float3(1.0, 0.96, 0.91) * coreGlow * brightness * 0.25;

    //外部散射光晕
    float outerGlw = exp(-dist * 2.2) * 0.28;
    finalColor.rgb += midColor.rgb * outerGlw * brightness;

    //alpha通道
    finalColor.a = saturate(intensity + corona * 0.5 + photonRing * 0.7 + outerGlw) * input.Color.a;

    //等离子体闪烁
    float flicker = p1.g * p2.r;
    finalColor.rgb *= 0.88 + flicker * 0.24;

    return finalColor * input.Color;
}

technique Technique1
{
    pass AccretionDiskPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
