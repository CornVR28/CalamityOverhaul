sampler uImage0 : register(s0);

//效果强度（0=无效果，1=完整效果）
float intensity;
//时间，用于动态扫描线和噪声
float uTime;
//色差偏移量
float chromaticOffset;
//暗角强度
float vignetteStrength;
//以玩家为中心的屏幕归一化坐标（0.5, 0.5 = 屏幕中央）
float2 playerCenter;

//简单的伪随机hash
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float4 SandevistanScreenPass(float2 coords : TEXCOORD0) : COLOR0
{
    float4 original = tex2D(uImage0, coords);
    if (intensity < 0.001)
        return original;

    // === 1. 色差分离（Chromatic Aberration）===
    //从玩家中心向外的方向做RGB偏移，赛博朋克标志性效果
    float2 toEdge = coords - playerCenter;
    float distFromCenter = length(toEdge);
    float2 aberrationDir = normalize(toEdge + 0.001) * chromaticOffset * intensity;

    float r = tex2D(uImage0, coords + aberrationDir).r;
    float g = original.g;
    float b = tex2D(uImage0, coords - aberrationDir).b;
    float a = original.a;

    float3 aberrated = float3(r, g, b);

    // === 2. 青色调去饱和（Cyan Desaturation）===
    //提取亮度，混合到青色调的去饱和
    float lum = dot(aberrated, float3(0.299, 0.587, 0.114));
    //目标：低饱和度的冷青色调
    float3 coldTint = float3(lum * 0.7, lum * 0.95, lum * 1.0);
    float desatAmount = 0.4 * intensity;
    float3 tinted = lerp(aberrated, coldTint, desatAmount);

    // === 3. 暗角（Vignette）===
    //从屏幕边缘向中心的渐变遮罩
    float2 vignetteCoords = (coords - 0.5) * 2.0;
    float vignetteDist = dot(vignetteCoords, vignetteCoords);
    float vignette = 1.0 - vignetteDist * vignetteStrength * intensity;
    vignette = saturate(vignette);
    tinted *= vignette;

    // === 4. 扫描线（Scanlines）===
    //水平扫描线 + 缓慢滚动，模拟赛博朋克义体接口的视觉噪声
    float scanline = sin((coords.y * 800.0) + uTime * 3.0) * 0.5 + 0.5;
    scanline = lerp(1.0, scanline, 0.06 * intensity);
    tinted *= scanline;

    // === 5. 细微噪声（Digital Noise）===
    //高频数字噪点，模拟神经接口干扰
    float noise = hash(coords * 500.0 + uTime * 7.0);
    noise = lerp(1.0, noise, 0.04 * intensity);
    tinted *= noise;

    // === 6. 青色边缘高光（Edge Highlight）===
    //在暗角边缘添加淡青色辉光
    float edgeGlow = smoothstep(0.4, 0.9, vignetteDist) * intensity * 0.15;
    tinted += float3(0.0, edgeGlow * 0.8, edgeGlow);

    return float4(tinted, a);
}

technique Technique1
{
    pass SandevistanScreenPass
    {
        PixelShader = compile ps_3_0 SandevistanScreenPass();
    }
}
