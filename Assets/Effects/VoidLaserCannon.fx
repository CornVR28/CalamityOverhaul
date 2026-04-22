//虚空聚落巨型激光炮台专用着色器
//渲染一条极具压迫感的高能能量柱
//特点：
//1. 白热炽热核心(亮到曝白)+紫红等离子层+深靛蓝外晕三层色温梯度
//2. 沿光束方向高速流动的湍流光纹，强化能量流速感
//3. 横向呼吸脉动+纵向等离子谐振，制造高能共振视觉
//4. 电浆撕裂亮斑点缀(随机闪白)，模拟高能粒子电离
//5. 核心处有色散(RGB分离)锐边，强调能量密度
//6. 长方向末端柔和衰减，避免硬截断，突出远距离威慑

sampler uImage0 : register(s0); //承载激光贴图(白色占位即可)
sampler uImage1 : register(s1); //扰动噪声纹理

float uTime;
float uOpacity;          //整体可见度0~1
float uIntensity;        //主强度(蓄能/发射/收束的阶段强度)
float uCoreIntensity;    //核心亮度(高频脉动)
float uPulseSpeed;
float uDistortionStrength;
float uBeamLength;
float uBeamWidth;
float uPhaseBlend;       //0=蓄能预警，1=全功率发射

//色温梯度
static const float3 CoreWhite    = float3(1.0, 0.98, 1.0);   //炽白核心
static const float3 HotPink      = float3(1.0, 0.55, 0.95);  //高温粉紫
static const float3 MagentaPlasma = float3(0.85, 0.2, 1.0);  //等离子洋红
static const float3 DeepViolet   = float3(0.35, 0.1, 0.9);   //深紫
static const float3 VoidIndigo   = float3(0.08, 0.02, 0.45); //虚空靛蓝
static const float3 CherenkovRim = float3(0.3, 0.7, 1.0);    //切伦科夫边缘光

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
    output.Position = input.Position;
    output.Color = input.Color;
    output.TexCoords = input.TexCoords;
    return output;
}

//解析哈希噪声
float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float valueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash(i);
    float b = hash(i + float2(1, 0));
    float c = hash(i + float2(0, 1));
    float d = hash(i + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float fbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++)
    {
        v += a * valueNoise(p);
        p *= 2.1;
        a *= 0.5;
    }
    return v;
}

//光束末端软衰减：距离源头越远越柔，避免硬边
float tailFalloff(float along)
{
    //前半段完整强度，后段平滑衰减
    return 1.0 - smoothstep(0.65, 1.0, along);
}

//沿光束方向的呼吸脉动：多频率叠加
float beamPulse(float along, float t)
{
    float p1 = sin(along * 22.0 - t * uPulseSpeed * 2.2) * 0.5 + 0.5;
    float p2 = sin(along * 7.0  - t * uPulseSpeed * 0.9 + 1.3) * 0.5 + 0.5;
    return 0.55 + p1 * 0.35 + p2 * 0.15;
}

//高能电浆撕裂亮斑(随机沿光束闪白)
float plasmaFlash(float along, float distCenter, float t)
{
    float seed = floor(along * 45.0 + t * 12.0);
    float r = hash(float2(seed, 7.7));
    //绝大多数位置无闪烁，只有极少数强闪
    float flash = smoothstep(0.93, 1.0, r);
    //闪烁位置沿束偏斜概率更靠核心
    flash *= 1.0 - smoothstep(0.0, 0.55, distCenter);
    return flash;
}

//RGB色散：核心出现锐利细线
float3 chromaticCore(float distCenter, float along, float t)
{
    float r = 1.0 - smoothstep(0.0, 0.04, abs(distCenter - 0.0));
    float g = 1.0 - smoothstep(0.0, 0.05, abs(distCenter - 0.012));
    float b = 1.0 - smoothstep(0.0, 0.05, abs(distCenter + 0.012));
    float rim = pow(beamPulse(along, t), 1.5);
    return float3(r, g, b) * rim;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoords;
    float along = uv.x;                          //0=枪口，1=末端
    float distCenter = abs(uv.y - 0.5) * 2.0;   //0=中心，1=外缘

    //纹理扰动：沿光束高速流动的湍流
    float2 flowUV = float2(along * 3.0 - uTime * 2.5, uv.y * 2.0);
    float flow = tex2D(uImage1, flowUV).r;
    float2 flowUV2 = float2(along * 8.0 - uTime * 5.5, uv.y * 3.5 + uTime * 0.3);
    float flow2 = tex2D(uImage1, flowUV2).g;

    //湍流位移：扭曲distCenter让能量柱呈现流动不规则感
    float turbulence = (flow * 0.55 + flow2 * 0.45 - 0.5) * uDistortionStrength;
    float warpedDist = saturate(distCenter + turbulence * 0.18);

    //核心锐利层(极窄，贡献炽白)
    float core = 1.0 - smoothstep(0.0, 0.08, warpedDist);
    core = pow(core, 1.2);

    //主能量层(等离子粉紫)
    float mid = 1.0 - smoothstep(0.05, 0.32, warpedDist);
    mid = pow(mid, 1.8);

    //外辉层(洋红深紫)
    float outer = 1.0 - smoothstep(0.18, 0.55, warpedDist);
    outer = pow(outer, 2.2);

    //边缘切伦科夫辉光(蓝色环状，内低外高再消散)
    float cherenkov = smoothstep(0.28, 0.55, warpedDist) * smoothstep(0.85, 0.55, warpedDist);
    float cherenkovWave = sin(along * 30.0 - uTime * 4.5) * 0.35 + 0.65;
    cherenkov *= cherenkovWave;

    //横向呼吸脉动
    float pulse = beamPulse(along, uTime);
    //蓄能阶段核心更尖锐、更窄：通过phaseBlend缩窄mid和outer
    float phaseWidth = lerp(0.6, 1.0, uPhaseBlend);
    mid *= phaseWidth;
    outer *= phaseWidth;

    //沿光束fbm条纹：制造能量流线
    float streak = fbm(float2(along * 12.0 - uTime * 4.0, uv.y * 5.0));
    streak = pow(streak, 2.0);
    float streakGate = 1.0 - smoothstep(0.05, 0.45, warpedDist);

    //电浆撕裂亮斑
    float flashSpot = plasmaFlash(along, warpedDist, uTime);

    //RGB色散核心
    float3 chroma = chromaticCore(warpedDist, along, uTime);

    //末端衰减
    float tail = tailFalloff(along);

    //==== 颜色合成 ====
    float3 color = float3(0, 0, 0);

    //外层最宽的靛蓝基底
    color += VoidIndigo * outer * 0.8;
    //中层紫+洋红
    float3 midColor = lerp(DeepViolet, MagentaPlasma, pulse);
    color += midColor * mid * 1.3;
    //切伦科夫蓝环
    color += CherenkovRim * cherenkov * 0.9;
    //热粉过渡
    color += HotPink * mid * outer * 0.4;
    //能量流线条纹(沿束白粉高光)
    color += lerp(HotPink, CoreWhite, streak) * streak * streakGate * 0.55;
    //炽白核心(被uCoreIntensity直接放大，让它能超过1造成泛光)
    color += CoreWhite * core * (1.6 + uCoreIntensity * 1.8) * pulse;
    //RGB色散核心补强
    color += chroma * uCoreIntensity * 0.9;
    //电浆撕裂白光
    color += CoreWhite * flashSpot * 3.0;

    //核心超亮溢出(让核心区域"烫出画面")
    float bloom = pow(1.0 - warpedDist, 14.0);
    color += CoreWhite * bloom * (1.8 + uCoreIntensity);

    //==== 强度合成 ====
    float intensity = 0.0;
    intensity += core * (1.2 + uCoreIntensity);
    intensity += mid * 0.9;
    intensity += outer * 0.55;
    intensity += cherenkov * 0.5;
    intensity += streak * streakGate * 0.4;
    intensity += flashSpot * 2.5;
    intensity *= pulse;
    intensity *= uIntensity;
    intensity *= tail;

    //末端颜色也要同步衰减
    color *= tail;

    float alpha = saturate(intensity * uOpacity * input.Color.a);
    color *= input.Color.rgb;

    return float4(color, alpha);
}

//简化通道(低端/降级)
float4 SimplePixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoords;
    float along = uv.x;
    float distCenter = abs(uv.y - 0.5) * 2.0;

    float core = 1.0 - smoothstep(0.0, 0.1, distCenter);
    core = pow(core, 1.5);
    float mid = 1.0 - smoothstep(0.08, 0.38, distCenter);
    mid = pow(mid, 2.0);
    float outer = 1.0 - smoothstep(0.25, 0.55, distCenter);
    outer = pow(outer, 2.5);

    float pulse = sin(along * 14.0 - uTime * uPulseSpeed * 1.4) * 0.3 + 0.7;

    float3 color = VoidIndigo * outer * 0.8;
    color += lerp(DeepViolet, MagentaPlasma, pulse) * mid * 1.2;
    color += CoreWhite * core * (1.4 + uCoreIntensity) * pulse;

    float intensity = (core * 1.3 + mid * 0.7 + outer * 0.4) * pulse * uIntensity * tailFalloff(along);

    return float4(color * input.Color.rgb, saturate(intensity * uOpacity * input.Color.a));
}

technique Technique1
{
    pass VoidLaserPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }

    pass SimpleVoidLaserPass
    {
        PixelShader = compile ps_2_0 SimplePixelShaderFunction();
    }
}
