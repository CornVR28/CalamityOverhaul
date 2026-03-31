//骇客时间屏幕后处理着色器
//风格参考赛博朋克2077骇入扫描模式
//整体暗色基底配合冷色调青绿滤镜，干净可读，不含扫描线和故障特效
//边缘正交网格框架加暗角，中心区域保持清晰

sampler uImage0 : register(s0);

//效果强度(0到1)
float intensity;
//动画时间
float uTime;
//暗角强度
float vignetteStrength;
//色调偏移强度
float tintStrength;

//平滑脉冲函数
float pulse(float x, float center, float width)
{
    float d = abs(x - center);
    return smoothstep(width, 0.0, d);
}

float4 HackTimePass(float2 coords : TEXCOORD0) : COLOR0
{
    float4 original = tex2D(uImage0, coords);
    if (intensity < 0.001)
        return original;

    float effectStr = intensity;

    //========================================
    //1.场景压暗与去饱和
    //========================================
    float3 color = original.rgb;

    //提取亮度
    float lum = dot(color, float3(0.299, 0.587, 0.114));

    //去饱和，保留一定原始色彩
    float desatAmount = 0.45 * effectStr * tintStrength;
    float3 gray = float3(lum, lum, lum);
    color = lerp(color, gray, desatAmount);

    //施加冷青色调
    //目标色：暗蓝绿，不过分鲜艳
    float3 tealTint = float3(lum * 0.65, lum * 0.88, lum * 0.92);
    float tintMix = 0.35 * effectStr * tintStrength;
    color = lerp(color, tealTint, tintMix);

    //整体压暗到约40%到60%亮度
    float dimFactor = lerp(1.0, 0.48, effectStr * 0.85);
    color *= dimFactor;

    //========================================
    //2.暗角效果
    //========================================
    float2 vc = (coords - 0.5) * 2.0;
    float vDist = dot(vc, vc);
    float vignette = 1.0 - vDist * vignetteStrength * effectStr;
    vignette = saturate(vignette);
    //暗角区域额外加深青色氛围
    float edgeDark = smoothstep(0.3, 1.2, vDist);
    color *= lerp(1.0, vignette, 0.8);

    //========================================
    //3.边缘色差分离(仅在画面边缘生效)
    //========================================
    float edgeMask = smoothstep(0.2, 0.8, vDist);
    float chromaOffset = 0.003 * effectStr * edgeMask;
    float2 toEdge = normalize(vc + 0.001);
    float2 aberDir = toEdge * chromaOffset;
    float rCA = tex2D(uImage0, coords + aberDir).r;
    float bCA = tex2D(uImage0, coords - aberDir).b;
    //仅在边缘区域混入色差
    color.r = lerp(color.r, rCA * dimFactor * vignette, edgeMask * 0.6);
    color.b = lerp(color.b, bCA * dimFactor * vignette, edgeMask * 0.6);

    //========================================
    //4.科技感边缘框架（正交网格+矩形边框线）
    //========================================
    float breathe = 0.5 + 0.5 * sin(uTime * 1.2);

    //在屏幕边缘区域绘制正交细线网格（frac保证0-1，无负值问题）
    float gridMask = smoothstep(0.4, 0.85, vDist) * effectStr;
    float gridSpacing = 40.0;
    float gx = frac(coords.x * gridSpacing);
    float gy = frac(coords.y * gridSpacing);
    float gridLineX = smoothstep(0.47, 0.5, gx) * smoothstep(0.53, 0.5, gx);
    float gridLineY = smoothstep(0.47, 0.5, gy) * smoothstep(0.53, 0.5, gy);
    float grid = max(gridLineX, gridLineY);
    color += float3(0.12, 0.65, 0.7) * grid * gridMask * 0.1 * (0.6 + 0.4 * breathe);

    //矩形边框线：外框
    float borderW = 0.004;
    float outerFrame = max(
        max(pulse(coords.y, 0.035, borderW), pulse(coords.y, 0.965, borderW)),
        max(pulse(coords.x, 0.035, borderW), pulse(coords.x, 0.965, borderW))
    );
    //矩形边框线：内框（更细）
    float innerFrame = max(
        max(pulse(coords.y, 0.065, borderW * 0.5), pulse(coords.y, 0.935, borderW * 0.5)),
        max(pulse(coords.x, 0.055, borderW * 0.5), pulse(coords.x, 0.945, borderW * 0.5))
    );
    float frameAlpha = effectStr * 0.3 * (0.7 + 0.3 * breathe);
    float3 frameColor = float3(0.15, 0.75, 0.8);
    color += frameColor * (outerFrame * frameAlpha + innerFrame * frameAlpha * 0.4);

    //========================================
    //5.四角装饰光点
    //========================================
    //在四个角落附近添加微弱的青色辉光
    float2 corners[4] = {
        float2(0.08, 0.08),
        float2(0.92, 0.08),
        float2(0.08, 0.92),
        float2(0.92, 0.92)
    };

    for (int i = 0; i < 4; i++)
    {
        float cDist = length(coords - corners[i]);
        float cGlow = smoothstep(0.15, 0.0, cDist);
        //每个角的脉冲相位略有不同
        float cPulse = 0.5 + 0.5 * sin(uTime * 1.5 + i * 1.57);
        float cAlpha = cGlow * 0.08 * cPulse * effectStr;
        color += float3(0.1, 0.7, 0.75) * cAlpha;
    }

    //========================================
    //6.上下边框细线装饰
    //========================================
    //顶部和底部各一条极细的青色分界线
    float topLine = pulse(coords.y, 0.03, 0.004) * effectStr * 0.3;
    float botLine = pulse(coords.y, 0.97, 0.004) * effectStr * 0.3;
    //线条上叠加流动效果
    float flowOffset = frac(coords.x * 3.0 + uTime * 0.3);
    float flowMask = smoothstep(0.0, 0.1, flowOffset) * smoothstep(0.7, 0.6, flowOffset);
    topLine *= (0.5 + 0.5 * flowMask);
    botLine *= (0.5 + 0.5 * flowMask);

    color += float3(0.2, 0.85, 0.9) * (topLine + botLine);

    //========================================
    //7.整体对比度微调
    //========================================
    //略微提升中间调的对比度，使画面更有科技感
    float3 midpoint = float3(0.25, 0.28, 0.30);
    color = midpoint + (color - midpoint) * lerp(1.0, 1.15, effectStr * 0.5);

    //确保颜色不溢出
    color = saturate(color);

    return float4(color, original.a);
}

technique Technique1
{
    pass HackTimeScreenPass
    {
        PixelShader = compile ps_3_0 HackTimePass();
    }
}
