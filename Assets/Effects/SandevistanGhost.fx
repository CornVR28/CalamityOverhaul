sampler uImage0 : register(s0);
float glowIntensity;

float4 SandevistanPass(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);

    //透明像素直接跳过
    if (color.a < 0.01)
        return float4(0, 0, 0, 0);

    //RT内容为预乘alpha格式，还原为实际颜色以正确处理
    float3 actual = color.rgb / max(color.a, 0.001);

    //基于亮度的辉光增强
    float luminance = dot(actual, float3(0.299, 0.587, 0.114));
    actual *= (1.0 + glowIntensity * luminance);

    //半透明边缘区域柔和光晕，模拟能量扩散
    float edgeFactor = smoothstep(0.0, 0.15, color.a) * (1.0 - smoothstep(0.15, 0.5, color.a));
    actual += actual * edgeFactor * 0.3;

    //重新预乘alpha输出
    return float4(actual * color.a, color.a);
}

technique Technique1
{
    pass SandevistanGhostPass
    {
        PixelShader = compile ps_2_0 SandevistanPass();
    }
}
