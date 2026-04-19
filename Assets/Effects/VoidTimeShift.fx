//虚空聚落时空叠加着色器
//过去滤镜：复古褪色胶片+冷蓝废墟感，克制的噪点与扫描线保证画面可读性
//切换演出：约半秒的RGB色散撕裂+中线亮带，瞬发冲击但不长时间影响视线

sampler uImage0 : register(s0);

//过去滤镜强度0到1
float filterIntensity;
//切换演出强度0到1的钟形曲线
float transitionStrength;
//动画时间
float uTime;

//廉价伪随机哈希，用于低幅度胶片颗粒
float hash12(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float4 MainPS(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords;

    //切换演出的采样位移：高速水平信号错位带，仅在transitionStrength显著时出现
    if (transitionStrength > 0.01)
    {
        //条带遮罩，取高带顶部的窄区
        float bandY = frac(uv.y * 4.0 + uTime * 0.8);
        float bandMask = smoothstep(0.85, 1.0, bandY) * transitionStrength;
        //随时间步进的整行偏移，幅度受钟形曲线控制
        float shift = (hash12(float2(floor(uv.y * 60.0), floor(uTime * 30.0))) - 0.5) * 0.03;
        uv.x += shift * bandMask;
    }

    float4 original = tex2D(uImage0, uv);
    float3 color = original.rgb;

    //========================================
    //1.过去滤镜：褪色冷蓝+琥珀暖部+轻微vignette+低噪点
    //========================================
    if (filterIntensity > 0.001)
    {
        float fi = filterIntensity;

        //亮度
        float lum = dot(color, float3(0.299, 0.587, 0.114));

        //去饱和，保留约45%原色，避免完全灰掉失去阅读线索
        float3 gray = float3(lum, lum, lum);
        color = lerp(color, gray, 0.55 * fi);

        //三档色调：暗部偏死寂冷蓝，中间调轻灰，亮部偏褪色琥珀，复古胶片感
        float3 shadowTint = float3(0.14, 0.18, 0.24);
        float3 midTint = float3(0.32, 0.34, 0.33);
        float3 highlightTint = float3(0.68, 0.62, 0.50);
        float3 tone;
        if (lum < 0.45)
            tone = lerp(shadowTint, midTint, lum / 0.45);
        else
            tone = lerp(midTint, highlightTint, saturate((lum - 0.45) / 0.55));
        //色调混合幅度克制
        color = lerp(color, tone * (lum + 0.18), 0.35 * fi);

        //轻微压暗
        color *= lerp(1.0, 0.82, fi);

        //柔和vignette，幅度低于HackTime
        float2 vc = (coords - 0.5) * 2.0;
        float vd = dot(vc, vc);
        float vignette = 1.0 - vd * 0.35 * fi;
        color *= saturate(vignette);

        //极轻微胶片颗粒，幅度0.015，每秒约18步刷新避免纯静态
        float grainTime = floor(uTime * 18.0);
        float grain = hash12(coords * 640.0 + grainTime) - 0.5;
        color += grain * 0.015 * fi;

        //疏朗扫描线，振幅很小仅用于胶片质感暗示
        float scan = sin(coords.y * 600.0) * 0.5 + 0.5;
        color -= scan * 0.008 * fi;

        //轻度对比度压缩模拟"蒙尘"
        color = lerp(float3(0.28, 0.30, 0.32), color, lerp(1.0, 0.92, fi));
    }

    //========================================
    //2.切换演出：中线RGB色散+亮度爆闪+中间裂缝
    //========================================
    if (transitionStrength > 0.01)
    {
        float ts = transitionStrength;

        //水平RGB色散，越靠近中线越强
        float bandCenterDist = abs(coords.y - 0.5);
        float bandFall = smoothstep(0.5, 0.0, bandCenterDist);
        float caOffset = 0.012 * ts * bandFall;
        float r = tex2D(uImage0, uv + float2(caOffset, 0)).r;
        float b = tex2D(uImage0, uv - float2(caOffset, 0)).b;
        color.r = lerp(color.r, r, ts * bandFall * 0.7);
        color.b = lerp(color.b, b, ts * bandFall * 0.7);

        //亮度短促一闪
        color += float3(0.08, 0.09, 0.10) * ts;

        //中间水平裂缝亮带
        float seam = smoothstep(0.008, 0.0, bandCenterDist) * ts;
        color += float3(0.25, 0.22, 0.18) * seam;
    }

    color = saturate(color);
    return float4(color, original.a);
}

technique Technique1
{
    pass VoidTimeShiftPass
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
