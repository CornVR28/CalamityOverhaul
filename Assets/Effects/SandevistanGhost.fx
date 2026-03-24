sampler uImage0 : register(s0);
float4 tintColor;
float glowIntensity;

float4 SandevistanPass(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);

    //透明像素直接跳过
    if (color.a < 0.01)
        return float4(0, 0, 0, 0);

    //计算亮度
    float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));

    //将原始颜色向青色调偏移
    float3 tinted = lerp(color.rgb, luminance * tintColor.rgb, 0.7);

    //根据亮度添加辉光，模拟Sandevistan的能量辉光效果
    tinted += tintColor.rgb * glowIntensity * luminance;

    //边缘增强 - 半透明区域产生柔和光晕
    float edgeGlow = smoothstep(0.0, 0.3, color.a) * (1.0 - smoothstep(0.3, 0.8, color.a));
    tinted += tintColor.rgb * edgeGlow * 0.2;

    return float4(tinted, color.a * 0.85);
}

technique Technique1
{
    pass SandevistanGhostPass
    {
        PixelShader = compile ps_2_0 SandevistanPass();
    }
}
