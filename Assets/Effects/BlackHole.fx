//=============================================================================
//BlackHole.fx - 黑洞着色器
//EventHorizon技术: AlphaBlend模式绘制事件视界黑暗区域
//Accretion技术: Additive模式绘制吸积环+光子环+引力透镜
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
float eventHorizonRadius;
float diskInnerRadius;
float diskOuterRadius;
float brightness;
float dopplerStrength;
float distortionStrength;

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

//=== 事件视界像素着色器 ===
//输出纯黑色+alpha遮罩，配合AlphaBlend吞噬背景光
float4 EventHorizonPS(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TexCoords;
    float2 center = float2(0.5, 0.5);
    float dist = length(coords - center);

    float ehR = eventHorizonRadius;

    //事件视界核心：完全不透明黑色
    float coreAlpha = 1.0 - smoothstep(ehR * 0.5, ehR * 1.1, dist);

    //外围引力暗化（模拟光线弯曲吸收）
    float gravDarken = exp(-max(dist - ehR, 0.0) * 8.0) * 0.3;

    float totalAlpha = saturate(coreAlpha + gravDarken);

    return float4(0, 0, 0, totalAlpha * input.Color.a);
}

//=== 吸积效应像素着色器 ===
//光子环+吸积盘+引力透镜光效，中心遮罩为零
float4 AccretionPS(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TexCoords;
    float2 center = float2(0.5, 0.5);
    float2 toCenter = coords - center;
    float dist = length(toCenter);
    float angle = atan2(toCenter.y, toCenter.x);

    float ehR = eventHorizonRadius;

    //事件视界遮罩（中心不发光）
    float horizonMask = smoothstep(ehR * 0.85, ehR * 1.4, dist);

    //=== 引力透镜UV扭曲 ===
    float lensF = distortionStrength * ehR / max(dist - ehR * 0.7, 0.008);
    lensF = min(lensF, 2.5);
    float2 lensedTC = toCenter * (1.0 + lensF * 0.012);
    float lensedDist = length(lensedTC);
    float lensedAngle = atan2(lensedTC.y, lensedTC.x);

    //=== 光子环（~1.5倍史瓦西半径的极亮薄环）===
    float photonR = ehR * 1.55;
    float photonD = abs(dist - photonR);
    float photonRing = exp(-photonD * photonD * 1500.0) * 2.5;
    //光子环微弱闪烁
    float pFlicker = tex2D(noiseTex, float2(angle / 6.283 + uTime * 0.25, 0.5 + uTime * 0.08)).r;
    photonRing *= (0.8 + pFlicker * 0.2);

    //=== 吸积盘 ===
    float diskNorm = saturate((lensedDist - diskInnerRadius) / (diskOuterRadius - diskInnerRadius));
    float diskMask = smoothstep(diskInnerRadius - 0.02, diskInnerRadius + 0.03, lensedDist);
    diskMask *= 1.0 - smoothstep(diskOuterRadius - 0.04, diskOuterRadius + 0.02, lensedDist);

    //开普勒差分旋转
    float keplerV = rotationSpeed * pow(max(lensedDist + 0.02, 0.02), -1.5);
    float diskAngle = lensedAngle + uTime * keplerV;

    //旋转UV采样噪声
    float2 diskUV = float2(cos(diskAngle) * lensedDist, sin(diskAngle) * lensedDist);

    float4 n1 = tex2D(noiseTex, diskUV * 2.0 + float2(uTime * 0.04, uTime * 0.025));
    float4 n2 = tex2D(noiseTex, diskUV * 5.5 + float2(-uTime * 0.05, uTime * 0.06));
    float4 n3 = tex2D(noiseTex, diskUV * 11.0 + float2(uTime * 0.07, -uTime * 0.04));

    //湍流（范围0.45~0.95）
    float turb = n1.r * 0.45 + n2.g * 0.35 + n3.b * 0.20;
    turb = turb * 0.5 + 0.45;

    //螺旋臂（3臂，适度缠绕）
    float spiral = sin(diskAngle * 3.0 - lensedDist * 30.0 + uTime * 0.5);
    spiral = spiral * 0.5 + 0.5;
    spiral = 0.5 + spiral * 0.5;

    //纤维丝细节
    float filament = sin(diskAngle * 7.0 + lensedDist * 40.0 + turb * 6.0);
    filament = filament * 0.5 + 0.5;
    filament = 0.65 + filament * 0.35;

    //多普勒增亮（接近侧更亮）
    float doppler = 1.0 + cos(angle + uTime * rotationSpeed * 0.12) * dopplerStrength;

    //温度梯度颜色（白热→橙→紫红）
    float4 diskColor;
    if (diskNorm < 0.25)
    {
        float4 hotWhite = float4(1.0, 0.95, 0.88, 1.0);
        diskColor = lerp(hotWhite, innerColor, diskNorm / 0.25);
    }
    else if (diskNorm < 0.6)
    {
        diskColor = lerp(innerColor, midColor, (diskNorm - 0.25) / 0.35);
    }
    else
    {
        diskColor = lerp(midColor, outerColor, (diskNorm - 0.6) / 0.4);
    }

    //径向亮度衰减（内圈热，外圈冷）
    float radialBright = 1.0 + (1.0 - diskNorm) * 0.6;

    //引力红移衰减（靠近视界变暗）
    float redshiftF = smoothstep(ehR, ehR + 0.1, lensedDist);

    //盘面合算
    float diskBright = diskMask * turb * spiral * filament * radialBright * doppler * redshiftF;

    //=== 内缘高温边界层 ===
    float innerEdgeDist = max(lensedDist - diskInnerRadius, 0.0);
    float innerEdge = exp(-innerEdgeDist * innerEdgeDist * 500.0);
    innerEdge *= smoothstep(ehR, diskInnerRadius, lensedDist);

    //=== 爱因斯坦环辉光（引力透镜亮环）===
    float einsteinD = abs(dist - ehR * 1.15);
    float einsteinRing = exp(-einsteinD * einsteinD * 600.0) * 1.2;

    //=== 喷流暗示（两极方向微弱能量束）===
    float jetCos = cos(angle);
    float jetMask = exp(-jetCos * jetCos * 6.0);
    jetMask *= exp(-max(dist - ehR, 0.0) * 4.0);
    jetMask *= 0.35;

    //=== 外围光晕 ===
    float outerHalo = exp(-max(dist - diskOuterRadius, 0.0) * 5.0) * 0.12;

    //=== 最终合成 ===
    float4 finalColor = float4(0, 0, 0, 0);

    //吸积盘
    finalColor.rgb += diskColor.rgb * diskBright * brightness;

    //光子环（白热色）
    float3 photonCol = lerp(innerColor.rgb, float3(1, 1, 1), 0.7);
    finalColor.rgb += photonCol * photonRing * brightness * horizonMask;

    //爱因斯坦环
    float3 einsteinCol = lerp(float3(0.7, 0.85, 1.0), innerColor.rgb, 0.3);
    finalColor.rgb += einsteinCol * einsteinRing * brightness * 0.5;

    //内缘高温
    finalColor.rgb += innerColor.rgb * innerEdge * brightness * horizonMask;

    //喷流
    finalColor.rgb += float3(0.5, 0.6, 1.0) * jetMask * brightness;

    //外围光晕
    finalColor.rgb += outerColor.rgb * outerHalo * brightness;

    //遮罩事件视界中心
    finalColor.rgb *= horizonMask;

    //alpha
    finalColor.a = saturate(diskBright * 1.5 + photonRing * 0.3 + einsteinRing * 0.3 + innerEdge + jetMask + outerHalo);
    finalColor.a *= horizonMask;
    finalColor.a *= input.Color.a;

    return finalColor * input.Color;
}

technique EventHorizon
{
    pass EventHorizonPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 EventHorizonPS();
    }
}

technique Accretion
{
    pass AccretionPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 AccretionPS();
    }
}
