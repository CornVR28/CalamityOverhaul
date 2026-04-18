// ============================================================================
// VoidFog.fx 虚空聚落雾气着色器
// 在世界空间中生成多层滚动柏林噪声，模拟漂浮于亚空间中的不规则雾团
// 雾团跟随世界坐标，玩家移动时不会"贴在屏幕上"，看起来像真正存在于世界中
// ============================================================================

sampler uImage0 : register(s0);//占位白纹理
sampler uNoise  : register(s1);//PerlinNoise 噪声纹理 wrap

float  uTime;//累积时间
float  uIntensity;//整体淡入淡出 0~1
float  uDensity;//密度倍率
float2 uScreenSize;//屏幕像素尺寸
float2 uWorldOffset;//Main.screenPosition 世界坐标
float2 uViewScale;//1/zoom 修正
float4 uFogColorLow;//稀薄雾色
float4 uFogColorHi;//浓密雾色

float4 PSFog(float2 coords : TEXCOORD0, float4 vertColor : COLOR0) : COLOR0
{
    //还原像素对应的世界坐标，并归一化到噪声纹理空间
    float2 worldPx = uWorldOffset + coords * uScreenSize * uViewScale;
    float2 baseUV = worldPx / 2048.0;

    float t = uTime;

    //三层不同尺度与方向的噪声叠加，制造翻腾感
    float n1 = tex2D(uNoise, baseUV * 1.0  + float2( t * 0.012, t * 0.004)).r;
    float n2 = tex2D(uNoise, baseUV * 2.7  + float2(-t * 0.020, t * 0.013)).r;
    float n3 = tex2D(uNoise, baseUV * 5.3  + float2( t * 0.030,-t * 0.022)).r;

    float density = n1 * 0.55 + n2 * 0.30 + n3 * 0.15;

    //收紧曲线得到不规则的浓淡块
    density = smoothstep(0.30, 0.85, density);

    //大尺度低频噪声充当宏观分布，让某些区域几乎无雾
    float patch = tex2D(uNoise, baseUV * 0.35 + float2(t * 0.005, t * 0.002)).r;
    patch = smoothstep(0.20, 0.78, patch);
    density *= lerp(0.20, 1.20, patch);

    density *= uDensity * uIntensity;
    density = saturate(density);

    //厚处偏向浓色
    float3 col = lerp(uFogColorLow.rgb, uFogColorHi.rgb, density * density);

    //稀疏微光颗粒，让雾里偶尔闪烁紫色亚空间余晖
    float glint = tex2D(uNoise, baseUV * 12.0 - float2(t * 0.040, t * 0.025)).r;
    glint = smoothstep(0.94, 0.995, glint) * density;
    col += float3(0.45, 0.18, 0.55) * glint * 0.7;

    return float4(col, density);
}

technique VoidFog
{
    pass P0
    {
        PixelShader = compile ps_3_0 PSFog();
    }
}
