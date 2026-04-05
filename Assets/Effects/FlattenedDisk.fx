//=============================================================================
//FlattenedDisk.fx - 高级吸积盘着色器
//特性: 开普勒差分旋转、多普勒增亮、多尺度湍流噪声、
//      螺旋密度波、光子环、Rayleigh-Taylor不稳定性边缘
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
float flattenRatio;
float2 centerPos;
float brightness;
float distortionStrength;
float pulseIntensity;
float dopplerStrength; //多普勒增亮强度

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

    //压扁Y轴模拟3D倾斜视角
    float2 flatTC = toCenter;
    flatTC.y /= max(flattenRatio, 0.01);

    //极坐标
    float dist = length(flatTC);
    float angle = atan2(flatTC.y, flatTC.x);

    //盘面半径参数
    float iR = 0.08;
    float oR = 0.47;
    float normDist = saturate((dist - iR) / (oR - iR));

    //=== 开普勒差分旋转 ===
    //角速度 ~ r^(-3/2)，内圈转速远大于外圈
    float keplerV = rotationSpeed * pow(max(dist + 0.03, 0.03), -1.5);
    float rotAngle = angle + uTime * keplerV;

    //=== 多尺度噪声湍流 ===
    float2 rotUV = float2(cos(rotAngle) * dist, sin(rotAngle) * dist);

    //大尺度结构
    float4 n1 = tex2D(noiseTex, rotUV * 2.0 + float2(uTime * 0.05, uTime * 0.02));
    //中尺度细节
    float4 n2 = tex2D(noiseTex, rotUV * 5.0 + float2(-uTime * 0.04, uTime * 0.07));
    //小尺度湍流
    float4 n3 = tex2D(noiseTex, rotUV * 11.0 + float2(uTime * 0.09, -uTime * 0.05));
    //扭曲驱动层
    float4 n4 = tex2D(noiseTex, rotUV * 1.3 + float2(uTime * 0.025, uTime * 0.025));

    //多层噪声合成
    float turb = n1.r * 0.42 + n2.g * 0.30 + n3.b * 0.16 + n4.r * 0.12;

    //噪声驱动的UV偏移扭曲
    float2 distortOff = (n4.xy - 0.5) * distortionStrength * (1.0 - normDist * 0.4);
    float4 nDetail = tex2D(noiseTex, (rotUV + distortOff) * 7.0 + float2(uTime * 0.07, -uTime * 0.03));
    turb = turb * 0.78 + nDetail.r * 0.22;

    //=== 螺旋密度波 ===
    float spiralArms = 2.0;
    float spiralTight = 8.0;
    float spiral = sin(rotAngle * spiralArms - dist * spiralTight * 28.0 + uTime * 0.6);
    spiral = spiral * 0.5 + 0.5;
    spiral = lerp(0.55, 1.0, pow(spiral, 0.65));

    //=== 盘面遮罩（正确的环形：内->外渐变） ===
    float diskMask = smoothstep(iR - 0.01, iR + 0.1, dist);
    diskMask *= 1.0 - smoothstep(oR - 0.1, oR + 0.02, dist);

    //=== 多普勒增亮 ===
    //旋转方向上朝向观察者的一侧更亮
    float dopplerPhase = cos(angle + uTime * rotationSpeed * 0.15 + 1.57);
    float doppler = 1.0 + dopplerPhase * dopplerStrength * (1.0 - normDist * 0.3);

    //=== 温度梯度颜色（内热外冷） ===
    float cT = saturate(normDist);
    float4 baseColor;
    if (cT < 0.25)
    {
        baseColor = lerp(innerColor * 1.4, midColor, cT * 4.0);
    }
    else if (cT < 0.65)
    {
        baseColor = lerp(midColor, outerColor, (cT - 0.25) * 2.5);
    }
    else
    {
        baseColor = outerColor * (1.0 - (cT - 0.65) * 0.72);
    }

    //=== 纤维丝状结构 ===
    float filaments = sin(rotAngle * 14.0 + dist * 65.0 + turb * 10.0);
    filaments = filaments * 0.5 + 0.5;
    filaments = pow(filaments, 1.4) * 0.35 + 0.65;

    //=== 径向密度条纹 ===
    float bands = sin(dist * 45.0 - uTime * 1.2 + turb * 5.0);
    bands = bands * 0.5 + 0.5;
    bands = bands * 0.4 + 0.6;

    //=== 脉动效果 ===
    float pulse = 1.0 + sin(uTime * 2.0 + dist * 6.0) * pulseIntensity;

    //=== 最终强度合成 ===
    float intensity = diskMask * turb * spiral * filaments * bands * doppler * pulse;
    intensity *= brightness;

    //径向亮度衰减（内圈更亮）
    float radBright = pow(max(1.0 - smoothstep(iR, oR, dist), 0.0), 1.2);

    //=== 3D倾斜边缘光照 ===
    float edgeY = abs(toCenter.y * flattenRatio) / max(dist, 0.001);
    float edgeLit = (1.0 - saturate(edgeY * 2.0)) * diskMask * 0.2;
    float3 edgeCol = lerp(midColor.rgb, float3(1, 1, 1), 0.35) * edgeLit;

    //=== 最终颜色合成 ===
    float4 finalColor;
    finalColor.rgb = baseColor.rgb * intensity * (1.0 + radBright * 0.65) + edgeCol;
    finalColor.a = saturate(intensity * 1.3) * input.Color.a;

    //=== 光子环（最内稳定轨道的高亮环） ===
    float photonRing = exp(-pow((dist - iR) * 28.0, 2.0));
    float3 photonCol = lerp(innerColor.rgb, float3(1, 1, 1), 0.55);
    finalColor.rgb += photonCol * photonRing * brightness * 2.2 * doppler;

    //=== 内缘辐射光晕 ===
    float innerGlow = pow(saturate(1.0 - dist / (iR + 0.02)), 3.5);
    finalColor.rgb += innerColor.rgb * innerGlow * 1.8;

    //=== 外缘散射光晕 ===
    float outerHalo = exp(-pow((dist - oR) * 9.0, 2.0)) * 0.1;
    finalColor.rgb += outerColor.rgb * outerHalo;

    //=== 热点闪烁（物质团块随机增亮） ===
    float flicker = n1.g * n2.b;
    finalColor.rgb *= 0.87 + flicker * 0.26;

    //=== Rayleigh-Taylor不稳定性（内边缘扰动手指状突起） ===
    float rtInstab = sin(angle * 16.0 + uTime * 3.0 + turb * 5.0);
    rtInstab = rtInstab * 0.5 + 0.5;
    float rtMask = exp(-pow((dist - iR - 0.05) * 18.0, 2.0));
    finalColor.rgb += innerColor.rgb * rtInstab * rtMask * 0.5 * brightness;

    return finalColor * input.Color;
}

technique Technique1
{
    pass FlattenedDiskPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
