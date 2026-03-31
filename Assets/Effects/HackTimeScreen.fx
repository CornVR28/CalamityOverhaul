//骇客时间屏幕后处理着色器
//赛博朋克2077骇入扫描模式——明亮冷青风格
//高对比度、饱和的冷青色调，画面清晰亮眼
//动态扫描线、边缘辉光、科技装饰框架

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
    //1.色调处理：冷青映射（保持明亮）
    //========================================
    float3 color = original.rgb;

    //提取亮度
    float lum = dot(color, float3(0.299, 0.587, 0.114));

    //适度去饱和，保留更多原始色彩
    float desatAmount = 0.30 * effectStr * tintStrength;
    float3 gray = float3(lum, lum, lum);
    color = lerp(color, gray, desatAmount);

    //冷青色调映射：亮区偏青白，暗区偏深蓝
    float3 shadowTint = float3(0.06, 0.14, 0.22);  //暗部：深海蓝
    float3 midTint = float3(0.15, 0.55, 0.60);      //中间调：冷青
    float3 highlightTint = float3(0.60, 0.92, 0.95); //亮部：青白
    //三档混合
    float3 toneMap;
    if (lum < 0.35)
        toneMap = lerp(shadowTint, midTint, lum / 0.35);
    else
        toneMap = lerp(midTint, highlightTint, saturate((lum - 0.35) / 0.65));
    float tintMix = 0.38 * effectStr * tintStrength;
    color = lerp(color, toneMap * (lum + 0.15), tintMix);

    //轻微压暗（保持70-80%亮度，而非以前的48%）
    float dimFactor = lerp(1.0, 0.72, effectStr * 0.7);
    color *= dimFactor;

    //亮部增益：让原本明亮的区域保持醒目
    float lumBoost = smoothstep(0.4, 0.8, lum);
    color += float3(0.04, 0.12, 0.13) * lumBoost * effectStr * 0.5;

    //========================================
    //2.暗角效果（更柔和，中心更亮）
    //========================================
    float2 vc = (coords - 0.5) * 2.0;
    float vDist = dot(vc, vc);
    float vignette = 1.0 - vDist * vignetteStrength * effectStr * 0.65;
    vignette = saturate(vignette);
    //暗角区域带青色氛围光
    float edgeDark = smoothstep(0.3, 1.2, vDist);
    color *= lerp(1.0, vignette, 0.6);
    //边缘青色氛围补偿（不让边缘变纯黑）
    color += float3(0.02, 0.06, 0.07) * edgeDark * effectStr;

    //========================================
    //3.边缘色差分离
    //========================================
    float edgeMask = smoothstep(0.2, 0.8, vDist);
    float chromaOffset = 0.0035 * effectStr * edgeMask;
    float2 toEdge = normalize(vc + 0.001);
    float2 aberDir = toEdge * chromaOffset;
    float rCA = tex2D(uImage0, coords + aberDir).r;
    float bCA = tex2D(uImage0, coords - aberDir).b;
    color.r = lerp(color.r, rCA * dimFactor * vignette, edgeMask * 0.5);
    color.b = lerp(color.b, bCA * dimFactor * vignette, edgeMask * 0.5);

    //========================================
    //4.动态水平扫描线（微弱CRT感）
    //========================================
    //细扫描线：每隔几行有微弱暗纹
    float scanLine = frac(coords.y * 300.0);
    float scanDark = smoothstep(0.48, 0.5, scanLine) * smoothstep(0.52, 0.5, scanLine);
    color -= float3(0.008, 0.012, 0.014) * scanDark * effectStr;

    //移动扫描高亮带：从上到下周期扫过
    float scanSweep = frac(uTime * 0.15);
    float scanBand = pulse(coords.y, scanSweep, 0.015) * effectStr * 0.08;
    color += float3(0.15, 0.6, 0.65) * scanBand;

    //========================================
    //5.科技感边缘框架（更亮更醒目）
    //========================================
    float breathe = 0.5 + 0.5 * sin(uTime * 1.2);

    //边缘网格
    float gridMask = smoothstep(0.45, 0.9, vDist) * effectStr;
    float gridSpacing = 40.0;
    float gx = frac(coords.x * gridSpacing);
    float gy = frac(coords.y * gridSpacing);
    float gridLineX = smoothstep(0.47, 0.5, gx) * smoothstep(0.53, 0.5, gx);
    float gridLineY = smoothstep(0.47, 0.5, gy) * smoothstep(0.53, 0.5, gy);
    float grid = max(gridLineX, gridLineY);
    color += float3(0.10, 0.55, 0.60) * grid * gridMask * 0.15 * (0.5 + 0.5 * breathe);

    //矩形边框线：外框（更亮）
    float borderW = 0.005;
    float outerFrame = max(
        max(pulse(coords.y, 0.035, borderW), pulse(coords.y, 0.965, borderW)),
        max(pulse(coords.x, 0.035, borderW), pulse(coords.x, 0.965, borderW))
    );
    //内框
    float innerFrame = max(
        max(pulse(coords.y, 0.065, borderW * 0.5), pulse(coords.y, 0.935, borderW * 0.5)),
        max(pulse(coords.x, 0.055, borderW * 0.5), pulse(coords.x, 0.945, borderW * 0.5))
    );
    float frameAlpha = effectStr * 0.45 * (0.6 + 0.4 * breathe);
    float3 frameColor = float3(0.20, 0.85, 0.90);
    color += frameColor * (outerFrame * frameAlpha + innerFrame * frameAlpha * 0.5);

    //外框辉光扩散（在框线周围外扩发光）
    float outerGlow = max(
        max(pulse(coords.y, 0.035, borderW * 4.0), pulse(coords.y, 0.965, borderW * 4.0)),
        max(pulse(coords.x, 0.035, borderW * 4.0), pulse(coords.x, 0.965, borderW * 4.0))
    );
    color += float3(0.05, 0.20, 0.22) * outerGlow * effectStr * 0.3 * (0.7 + 0.3 * breathe);

    //========================================
    //6.四角装饰光点（更明亮）
    //========================================
    float2 corners[4] = {
        float2(0.08, 0.08),
        float2(0.92, 0.08),
        float2(0.08, 0.92),
        float2(0.92, 0.92)
    };

    for (int i = 0; i < 4; i++)
    {
        float cDist = length(coords - corners[i]);
        float cGlow = smoothstep(0.12, 0.0, cDist);
        float cPulse = 0.5 + 0.5 * sin(uTime * 1.5 + i * 1.57);
        float cAlpha = cGlow * 0.14 * cPulse * effectStr;
        color += float3(0.15, 0.80, 0.85) * cAlpha;
    }

    //========================================
    //7.上下边框流动线（更亮的青色）
    //========================================
    float topLine = pulse(coords.y, 0.03, 0.005) * effectStr * 0.45;
    float botLine = pulse(coords.y, 0.97, 0.005) * effectStr * 0.45;
    float flowOffset = frac(coords.x * 3.0 + uTime * 0.3);
    float flowMask = smoothstep(0.0, 0.1, flowOffset) * smoothstep(0.7, 0.6, flowOffset);
    topLine *= (0.4 + 0.6 * flowMask);
    botLine *= (0.4 + 0.6 * flowMask);
    color += float3(0.25, 0.90, 0.95) * (topLine + botLine);

    //========================================
    //8.整体对比度与色彩增益
    //========================================
    //提升对比度，让画面更锐利
    float3 midpoint = float3(0.30, 0.34, 0.36);
    color = midpoint + (color - midpoint) * lerp(1.0, 1.25, effectStr * 0.5);

    //微弱的全局青色叠加（让整体色调统一而醒目）
    color += float3(0.01, 0.03, 0.035) * effectStr;

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
