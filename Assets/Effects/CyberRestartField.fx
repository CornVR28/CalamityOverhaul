// ============================================================================
// CyberRestartField.fx 赛博空间领域级"系统重启"演出
// 单 pass 处理四阶段：撕裂(黑墙裂缝)→收缩(向心数据流+引力涡)→奇点(红黑核心裂缝+引力透镜)→炸裂(放射激波)
// 通过四个阶段权重 tearK/collapseK/singularityK/burstK 串联演出，配合极坐标噪声+脊线噪声构造高频细节
// 期望调用方：以领域中心为原点绘制一个 2*领域半径 大小的方形 quad，加法混合，Texture[1]=noise 网格图
// ============================================================================

sampler uImage0 : register(s0);
sampler noiseTex : register(s1);

float uTime;          //累计动画时间
float tearK;          //撕裂阶段权重 0..1
float collapseK;      //收缩阶段权重 0..1
float singularityK;   //奇点阶段权重 0..1
float burstK;         //炸裂阶段权重 0..1
float crackSeed;      //本次重启随机种子
float globalAlpha;    //整体淡入淡出

static const float PI = 3.14159265;
static const float TAU = 6.28318530;

float hash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

//沿径向叠加多频脊线噪声，得到流动感纹理
float ridge(float2 uv)
{
    float a = tex2D(noiseTex, uv).r;
    return 1.0 - abs(a * 2.0 - 1.0);
}

//黑墙裂缝（极坐标角度切槽 + 噪声扰动 + 径向衰减）
//返回 (核心红, 黑墙轮廓, 末端高光)
float3 cracks(float2 centered, float dist, float angle)
{
    const float CrackCount = 7.0;
    float ang01 = angle / TAU + 0.5;
    float slot = ang01 * CrackCount + crackSeed * 11.0;
    float idx = floor(slot);
    float f = frac(slot) - 0.5;//[-0.5,0.5] 槽内偏移
    float seed = hash11(idx + crackSeed * 17.0);

    //每条裂缝独立生长延迟
    float delay = seed * 0.55;
    float local = saturate((tearK - delay) / max(0.0001, 1.0 - delay));
    float grow = 1.0 - pow(1.0 - local, 2.6);
    //收缩阶段被吸回中心
    float retract = 1.0 - collapseK;
    retract *= retract;
    float reach = grow * retract;

    //裂缝沿径向折线扰动
    float jn = tex2D(noiseTex, float2(dist * 2.4 - uTime * 0.45, seed * 1.3)).g;
    float curve = (jn - 0.5) * 0.10 * sin(dist * 11.0 + seed * 9.0);
    float lateral = f * (0.10 + 0.18 * sin(dist * 5.0 + seed * 6.0)) - curve;

    //径向半宽：靠近中心收窄、靠近外端略宽再收尾
    float taper = sin(saturate(dist / max(reach, 0.001)) * PI);//端点 0、中段 1
    float half = (0.012 + 0.045 * taper) * (0.6 + 0.4 * grow);

    //长度限制
    float withinLen = smoothstep(reach, reach - 0.04, dist);
    float coreMask = smoothstep(half, 0.0, abs(lateral)) * withinLen;
    //黑墙：比 core 更宽、更慢衰减
    float wallMask = smoothstep(half * 3.4, 0.0, abs(lateral)) * withinLen;
    wallMask = max(wallMask - coreMask * 0.85, 0.0);

    //末端高光
    float tipBand = smoothstep(reach * 0.92, reach, dist) * (1.0 - smoothstep(reach, reach + 0.04, dist));
    float tipPulse = 0.5 + 0.5 * sin(uTime * 9.0 + seed * 13.0);
    float tipMask = tipBand * coreMask * (0.4 + 0.6 * tipPulse);

    return float3(coreMask, wallMask, tipMask) * (1.0 - smoothstep(0.95, 1.05, dist));
}

//收缩阶段：径向向内的数据流细带 + 螺旋渐暗涡
float3 collapseStreams(float dist, float angle)
{
    float w = collapseK;
    if (w <= 0.0) return 0.0;

    //螺旋角：让径向条带跟随角度产生螺线感，同时随时间向内推进
    float spin = angle * 3.0 + uTime * 1.6 + crackSeed * 5.0;
    //径向相位：dist 越大相位越落后，模拟"外缘起步、向内追赶"
    float radPhase = dist * 14.0 - uTime * 6.0 * (0.4 + 0.8 * w);
    float ribbon = ridge(float2(spin * 0.18, radPhase * 0.05));
    ribbon = pow(saturate(ribbon), 2.4);

    //径向条带衰减：center 处归零（已被奇点吃掉）、外缘随阶段进度回收
    float outerLimit = lerp(1.05, 0.05, pow(w, 1.5));//收缩末段把外径压到接近核心
    float radial = smoothstep(0.02, 0.10, dist) * (1.0 - smoothstep(outerLimit - 0.10, outerLimit, dist));

    //角向高频细栅
    float fine = tex2D(noiseTex, float2(spin * 0.30, dist * 2.6 + uTime * 0.3)).b;
    fine = smoothstep(0.55, 0.95, fine);

    float streams = ribbon * radial * (0.55 + 0.55 * fine) * w;

    //引力涡环：单条粗黑环线沿外径收缩
    float ringR = lerp(0.92, 0.05, pow(w, 1.3));
    float ringDist = abs(dist - ringR);
    float ringMask = smoothstep(0.045, 0.0, ringDist) * w;

    return float3(streams, ringMask, 0.0);
}

//奇点核心：竖直窄裂口 + 内部抖动 + 引力透镜暗环
float3 singularityCore(float2 centered, float dist)
{
    float w = singularityK;
    if (w <= 0.0) return 0.0;

    //核心整体尺寸：随 w 起伏、随 burstK 急速塌缩
    float pulse = 0.85 + 0.15 * sin(uTime * 12.0 + crackSeed * 9.0);
    float coreSize = lerp(0.04, 0.085, w) * pulse;
    float burstShrink = 1.0 - burstK;
    coreSize *= burstShrink;

    //垂直窄裂口的有向距离场：x 越小越靠裂缝中线、y 决定裂口长度
    float halfH = coreSize * 1.4;
    float halfW = coreSize * 0.18;
    //裂口轻微抖动
    float jx = (tex2D(noiseTex, float2(centered.y * 4.0 + uTime * 0.7, crackSeed)).r - 0.5) * 0.012;
    float dx = abs(centered.x - jx);
    float dy = abs(centered.y);
    float slitX = smoothstep(halfW, 0.0, dx);
    float slitY = smoothstep(halfH, halfH * 0.85, dy);//端点更柔
    float slit = slitX * (1.0 - smoothstep(halfH * 1.0, halfH * 1.15, dy));

    //核心黑色椭圆基底（软）
    float2 ellip = centered / float2(coreSize * 0.55, coreSize * 1.4);
    float core = 1.0 - smoothstep(0.85, 1.05, length(ellip));

    //外缘引力透镜暗环
    float lensR = coreSize * 2.6;
    float lensDist = abs(dist - lensR);
    float lens = smoothstep(coreSize * 1.0, 0.0, lensDist) * w;

    //横向短闪偶发
    float jitter = sin(uTime * 18.0 + crackSeed * 23.0);
    float crossBand = step(0.55, jitter) * smoothstep(coreSize * 4.0, coreSize * 1.0, dist) * smoothstep(0.012, 0.0, abs(centered.y));

    //x: 黑底 y: 红裂口 z: 透镜环
    return float3(core * w, slit * w + crossBand * 0.6, lens);
}

//炸裂阶段：放射激波 + 同心冲击环
float3 burstField(float dist, float angle)
{
    float w = burstK;
    if (w <= 0.0) return 0.0;

    //放射纹：高频角向脊线乘径向能量曲线
    float rays = ridge(float2(angle * 1.6 / TAU + crackSeed, dist * 0.25 - uTime * 2.0));
    rays = pow(saturate(rays), 1.8);
    //径向能量在 0.05~1.0 之间从 1 到 0 平滑下降，并随 burstK 平移
    float radial = smoothstep(0.0, 0.15, dist) * (1.0 - smoothstep(0.7 + 0.3 * w, 1.0 + 0.3 * w, dist));
    float rayMask = rays * radial * w;

    //冲击环：从 0.1 扩张到 1.0
    float ringR = lerp(0.05, 1.0, smoothstep(0.0, 1.0, w));
    float ringDist = abs(dist - ringR);
    float ringW = 0.04 + 0.05 * (1.0 - w);
    float ring = smoothstep(ringW, 0.0, ringDist) * (1.0 - w * 0.6);

    //中心白热盘：在炸裂前 30% 达到峰值
    float flash = 0.0;
    if (w < 0.35) flash = pow(1.0 - w / 0.35, 1.4);
    float flashMask = flash * (1.0 - smoothstep(0.0, 0.18, dist));

    return float3(rayMask, ring, flashMask);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    //把 [0,1] 映射到 [-1,1]，并求极坐标
    float2 centered = coords * 2.0 - 1.0;
    float dist = length(centered);
    float angle = atan2(centered.y, centered.x);
    if (dist > 1.05) return 0.0;

    //四阶段累积色
    float3 cracksRGB = cracks(centered, dist, angle);
    float3 collapseRGB = collapseStreams(dist, angle);
    float3 coreRGB = singularityCore(centered, dist);
    float3 burstRGB = burstField(dist, angle);

    //调色：核心暗红 #d8221c，黑墙 #1a0306，热边 #ff7a4f，冷暗 #050103
    float3 hotRed = float3(1.0, 0.40, 0.22);
    float3 coreRed = float3(0.85, 0.10, 0.10);
    float3 wall = float3(0.05, 0.01, 0.02);
    float3 white = float3(1.0, 0.92, 0.78);

    float3 col = 0.0;
    //裂缝：黑墙先压暗，再叠红芯，最后叠末端高光
    col += wall * cracksRGB.y * 1.2;
    col += coreRed * cracksRGB.x * 1.0;
    col += hotRed * cracksRGB.z * 1.4;

    //收缩：暗红流条 + 黑环
    col += coreRed * collapseRGB.x * 0.85;
    col += hotRed * collapseRGB.x * 0.35;
    col += wall * collapseRGB.y * 1.6;

    //奇点：黑底压底 + 红裂芯 + 透镜暗环
    col -= wall * coreRGB.x * 1.4;//相当于在加法混合下抑制亮度，制造"黑洞感"
    col += coreRed * coreRGB.y * 1.3;
    col += hotRed * coreRGB.y * 0.5;
    col += wall * coreRGB.z * 0.8;

    //炸裂：白热放射 + 暖环 + 中心闪
    col += hotRed * burstRGB.x * 0.85;
    col += white * burstRGB.x * 0.35;
    col += white * burstRGB.y * 0.85;
    col += hotRed * burstRGB.y * 0.55;
    col += white * burstRGB.z * 1.0;

    //领域整体红雾：四阶段权重之和的弱底色，让"领域被点燃"
    float ambient = saturate(tearK * 0.25 + collapseK * 0.4 + singularityK * 0.45 + burstK * 0.35);
    col += coreRed * ambient * 0.10 * (1.0 - dist * 0.8);

    //外缘羽化
    float fade = 1.0 - smoothstep(0.95, 1.05, dist);
    col *= fade * globalAlpha;

    //加法混合下使用 RGB 强度作为 alpha
    float a = saturate(max(max(col.r, col.g), col.b)) * globalAlpha;
    return float4(col, a) * vertexColor;
}

technique Technique1
{
    pass CyberRestartFieldPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
