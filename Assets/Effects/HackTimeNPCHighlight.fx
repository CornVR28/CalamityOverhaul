//骇客时间NPC高亮着色器
//选中NPC：红色赛博滤镜+描边辉光+扫描线
//悬停NPC：冷青淡化滤镜+描边辉光

sampler uImage0 : register(s0);

//纹素尺寸(1/宽, 1/高)
float2 texelSize;
//效果强度(0到1)
float intensity;
//选中状态(1=选中, 0=悬停)
float isSelected;
//动画时间
float uTime;

float4 HackNPCPass(float2 coords : TEXCOORD0, float4 smpColor : COLOR0) : COLOR0
{
    float4 texColor = tex2D(uImage0, coords);
    float effectStr = intensity;

    //========================================
    //1.邻域采样——边缘检测
    //========================================
    float stp = 2.0;
    float a_r  = tex2D(uImage0, coords + float2( texelSize.x * stp, 0)).a;
    float a_l  = tex2D(uImage0, coords + float2(-texelSize.x * stp, 0)).a;
    float a_u  = tex2D(uImage0, coords + float2(0,  texelSize.y * stp)).a;
    float a_d  = tex2D(uImage0, coords + float2(0, -texelSize.y * stp)).a;
    float a_ru = tex2D(uImage0, coords + texelSize * stp).a;
    float a_ld = tex2D(uImage0, coords - texelSize * stp).a;
    float a_lu = tex2D(uImage0, coords + float2(-texelSize.x, texelSize.y) * stp).a;
    float a_rd = tex2D(uImage0, coords + float2(texelSize.x, -texelSize.y) * stp).a;

    float neighborMax = max(max(max(a_r, a_l), max(a_u, a_d)),
                            max(max(a_ru, a_ld), max(a_lu, a_rd)));
    float neighborAvg = (a_r + a_l + a_u + a_d + a_ru + a_ld + a_lu + a_rd) / 8.0;

    float pulse = 0.7 + 0.3 * sin(uTime * 3.5);

    //========================================
    //2.外描边：透明像素但有不透明邻居
    //========================================
    if (texColor.a < 0.1 && neighborMax > 0.3)
    {
        float3 outlineColor = isSelected > 0.5
            ? float3(1.0, 0.12, 0.08)
            : float3(0.08, 0.75, 0.85);
        float glowStr = neighborMax * effectStr * 0.7 * pulse;
        //预乘alpha输出
        return float4(outlineColor * glowStr, glowStr);
    }

    //完全透明直接丢弃
    if (texColor.a < 0.01)
        return float4(0, 0, 0, 0);

    //========================================
    //3.应用顶点着色(环境光照)
    //========================================
    float4 color = texColor * smpColor;

    //内边缘因子：邻居平均alpha低=靠近边缘
    float edgeFactor = 1.0 - neighborAvg;
    edgeFactor = saturate(edgeFactor);
    edgeFactor = smoothstep(0.0, 0.4, edgeFactor);

    float lum = dot(color.rgb, float3(0.299, 0.587, 0.114));
    float3 result = color.rgb;

    //========================================
    //4.选中状态：红色赛博滤镜
    //========================================
    if (isSelected > 0.5)
    {
        //色调映射：向红/橙色偏移
        float3 cyberTint = float3(1.0, 0.22, 0.15);
        result = lerp(result, lum * cyberTint * 1.5, effectStr * 0.4);

        //高亮区域增益
        float highMask = smoothstep(0.35, 0.75, lum);
        result += float3(0.18, 0.05, 0.02) * highMask * effectStr;

        //内边缘辉光：亮红脉冲
        result += float3(1.0, 0.18, 0.12) * edgeFactor * effectStr * 0.5 * pulse;

        //水平扫描线（从上到下缓慢移动）
        float scanY = frac(coords.y * 50.0 + uTime * 1.8);
        float scanMask = smoothstep(0.47, 0.5, scanY) * smoothstep(0.53, 0.5, scanY);
        result += float3(0.1, 0.025, 0.015) * scanMask * effectStr * 0.3;
    }
    //========================================
    //5.悬停状态：冷青微光滤镜
    //========================================
    else
    {
        //色调映射：向冷青偏移
        float3 cyberTint = float3(0.22, 0.82, 0.88);
        result = lerp(result, lum * cyberTint * 1.3, effectStr * 0.22);

        //内边缘辉光：柔和青色脉冲
        result += float3(0.12, 0.8, 0.85) * edgeFactor * effectStr * 0.3 * pulse;
    }

    result = saturate(result);
    return float4(result, color.a);
}

technique Technique1
{
    pass HackNPCHighlightPass
    {
        PixelShader = compile ps_3_0 HackNPCPass();
    }
}
