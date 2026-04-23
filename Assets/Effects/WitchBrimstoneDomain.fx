// ============================================================================
// WitchBrimstoneDomain.fx — 硫火女巫留影鬼域
// 主题：被焚毁经文的灰烬回响，吊挂着一只未合拢的凝视之眼
// 视觉组成：
//   A 深渊暗炉底色，带缓慢翻涌的岩浆暗流
//   B 核心凝视瞳孔（椭圆长瞳缝，带冷硫磺火辉光）
//   C 从中心辐射而出的破碎裂纹（像烧裂的玻璃，硫火光从缝隙渗出）
//   D 悬浮的焚毁经文残片（在中圈漂浮，点状燃烧）
//   E 垂直上升的幽火细柱（像残魂）
//   F 四向烙印封印（十字刻在最外层的焦灰符印）
//   G 向上飘散的灰烬余烬
//   H 外缘硫磺烟雾消散
// 单DrawCall一次性绘制所有层
// ============================================================================

sampler uImage0 : register(s0);

texture uNoiseTex;
sampler noiseSamp = sampler_state
{
    texture = <uNoiseTex>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float uTime;
float fadeAlpha;        //整体透明度0到1
float expandProgress;   //鬼域展开进度0到1
float dissolveProgress; //剥落消散进度0到1，1时鬼域即将完全熄灭
float pulseIntensity;   //核心瞳孔呼吸强度
float3 coreColor;       //核心硫火亮橙
float3 midColor;        //中层暗红
float3 edgeColor;       //边缘焦紫黑
float3 voidColor;       //虚空底色

#define PI 3.14159265
#define TAU 6.28318530

//散列与噪声
float hash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float valueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + float2(1, 0));
    float c = hash21(i + float2(0, 1));
    float d = hash21(i + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float fbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++)
    {
        v += a * valueNoise(p);
        p = p * 2.13 + float2(1.7, 9.2);
        a *= 0.5;
    }
    return v;
}

//二维旋转
float2 rot2(float2 p, float a)
{
    float s = sin(a);
    float c = cos(a);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

//到直线段距离
float segDist(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h);
}

// ---- A 暗炉底色 ----
float3 abyssBase(float2 centered, float dist, float time)
{
    //岩浆缓流，方向近似切向 + 轻微径向
    float2 flowUV = centered * 1.4 + float2(time * 0.05, -time * 0.07);
    float magma = fbm(flowUV);
    magma = smoothstep(0.35, 0.75, magma);

    //中心越黑、外缘渐进到焦色
    float depth = smoothstep(0.0, 0.9, dist);
    float3 base = lerp(voidColor * 0.35, edgeColor * 0.45, depth);
    base = lerp(base, midColor * 0.55, magma * 0.45 * (1.0 - depth * 0.5));
    return base;
}

// ---- B 核心凝视瞳孔 ----
//椭圆竖缝瞳孔，带呼吸脉动
float witchEye(float2 centered, float time, float pulse, out float iris)
{
    //眼球整体大小受expandProgress缩放，这里默认单位参考0.18
    float2 p = centered;
    //横向拉扁，形成眼球
    float2 eyeball = float2(p.x / 1.2, p.y);
    float eyeR = length(eyeball);
    float ballMask = 1.0 - smoothstep(0.13, 0.16, eyeR);

    //呼吸：瞳孔宽度随时间变化
    float breath = 0.6 + 0.4 * sin(time * 1.3) * pulse;
    float2 pup = float2(p.x / (0.035 + breath * 0.02), p.y / 0.12);
    float pupR = length(pup);
    iris = 1.0 - smoothstep(0.9, 1.0, pupR);

    //瞳孔内侧的纵向光芒
    float slit = exp(-pow(p.x / 0.01, 2.0)) * exp(-pow(p.y / 0.14, 2.0));

    //虹膜高亮
    float halo = (1.0 - smoothstep(0.07, 0.14, eyeR)) * ballMask;
    halo *= 0.4 + 0.6 * sin(time * 2.0 + eyeR * 40.0) * 0.3 + 0.7;

    //合成RGB：深黑眼球 + 硫火虹膜 + 核心亮斑
    float3 col = voidColor * ballMask * 1.2;
    col += midColor * halo * 0.6;
    col += coreColor * iris * 1.4;
    col += float3(1.0, 0.9, 0.55) * slit * 1.6;
    return col;
}

// ---- C 辐射裂纹 ----
//以中心为原点的放射状不规则裂纹，内部透出硫火
float radialCracks(float2 centered, float dist, float angle, float time)
{
    //每条裂纹是一条从某半径开始、角度近似固定、末端分叉的折线
    //这里用简化方式：对角度进行量化取样，形成若干主轴
    float a = angle;
    float ringFade = smoothstep(0.12, 0.2, dist) * (1.0 - smoothstep(0.55, 0.78, dist));
    if (ringFade <= 0.001) return 0.0;

    //主裂纹：12道
    float crack = 0.0;
    for (int i = 0; i < 12; i++)
    {
        float id = (float)i;
        //主角度 + 微小摇晃
        float baseAng = id * (TAU / 12.0) + 0.13;
        //给每条裂纹一个偏心扭动
        float jitter = sin(dist * 12.0 + id * 2.3 + time * 0.6) * 0.08;
        float diff = a - (baseAng + jitter);
        //角度折叠到[-PI,PI]
        diff = diff - TAU * floor((diff + PI) / TAU);
        //每条裂纹的可见半径范围
        float life = 0.5 + 0.5 * sin(time * 0.4 + id * 1.7);
        float rStart = 0.1 + hash11(id * 3.17) * 0.04;
        float rEnd = 0.55 + 0.1 * life;
        float rMask = smoothstep(rStart, rStart + 0.03, dist) *
                      (1.0 - smoothstep(rEnd - 0.08, rEnd, dist));
        //裂纹宽度随半径收窄
        float width = 0.004 + 0.01 * (1.0 - saturate((dist - rStart) / max(rEnd - rStart, 0.01)));
        float streak = exp(-(diff * diff) / (width * width)) * rMask;
        crack = max(crack, streak);
    }

    //在裂纹上叠加闪烁
    float flick = 0.65 + 0.35 * sin(time * 4.0 + dist * 30.0);
    return crack * ringFade * flick;
}

// ---- D 焚毁经文残片 ----
//中圈漂浮的小型燃烧方块，代表从经卷上飞落的残片
float burningScriptures(float2 centered, float time)
{
    float total = 0.0;
    //12片残片
    for (int i = 0; i < 12; i++)
    {
        float id = (float)i;
        float h1 = hash11(id * 1.93);
        float h2 = hash11(id * 4.71);
        float h3 = hash11(id * 7.19);

        //角度围绕圆周分布，附加缓慢旋转
        float ang = id * (TAU / 12.0) + time * (0.05 + h1 * 0.08) * (h2 > 0.5 ? 1.0 : -1.0);
        float r = 0.33 + h3 * 0.08 + sin(time * 0.6 + id * 1.3) * 0.015;

        //残片所处位置
        float2 pos = float2(cos(ang), sin(ang)) * r;
        //残片随身携带的局部坐标系，带自旋
        float spin = time * (0.8 + h1 * 1.4) + id * 0.7;
        float2 local = rot2(centered - pos, spin);
        //拉长成小纸片
        float2 rect = local / float2(0.018, 0.012);
        //矩形SDF近似
        float2 q = abs(rect) - 1.0;
        float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
        float paper = 1.0 - smoothstep(0.0, 0.35, d);

        //残片中央的燃烧核心
        float burn = exp(-dot(local, local) * 9000.0);

        //生命周期：残片会周期性燃烧殆尽再点燃
        float life = frac(time * (0.18 + h2 * 0.12) + h3);
        float alive = sin(life * PI);

        total += (paper * 0.5 + burn * 1.2) * alive;
    }
    return saturate(total);
}

// ---- E 垂直幽火 ----
//从下方升起的细长火柱，代表残魂
float risingSoulFlames(float2 centered, float time)
{
    //将坐标映射到圆内：仅在垂直轴附近+随机分布
    float total = 0.0;
    for (int i = 0; i < 10; i++)
    {
        float id = (float)i;
        float h1 = hash11(id * 2.17);
        float h2 = hash11(id * 5.73);
        //起始底部位置（圆下半部）
        float xBase = (h1 - 0.5) * 1.4;
        float life = frac(time * (0.25 + h2 * 0.2) + h1 * 3.1);
        //y从底部升起
        float yTop = 0.75 - life * 1.35;
        //火柱局部坐标
        float2 local = float2(centered.x - xBase - sin(life * PI * 2.0 + id) * 0.03,
                              centered.y - yTop);
        //高斯拉高：竖向窄，纵向长
        float width = 0.012 * (1.0 - life * 0.5);
        float height = 0.14;
        float gauss = exp(-pow(local.x / width, 2.0)) *
                      exp(-pow(max(local.y, 0.0) / height, 2.0));
        //生命淡入淡出
        gauss *= sin(life * PI);
        //限制只在鬼域内
        float r = length(float2(xBase, yTop));
        gauss *= 1.0 - smoothstep(0.7, 0.85, r);
        total += gauss;
    }
    return saturate(total);
}

// ---- F 四向烙印封印 ----
//四个方位的十字焦印，象征钉在鬼域边缘的封印
float brandSeals(float2 centered, float dist, float angle, float time)
{
    float result = 0.0;
    //在半径0.68的位置上布置4个封印
    float sealR = 0.68;
    for (int i = 0; i < 4; i++)
    {
        float id = (float)i;
        //角度正对上下左右，并随时间缓慢顺时针偏移
        float ang = id * (PI * 0.5) + time * 0.03;
        float2 sealPos = float2(cos(ang), sin(ang)) * sealR;
        //局部坐标需要跟随角度旋转，让十字朝向中心
        float2 local = rot2(centered - sealPos, -ang - PI * 0.5);
        //绘制十字：两条正交线段
        float l1 = exp(-pow(local.x / 0.003, 2.0)) * step(abs(local.y), 0.04);
        float l2 = exp(-pow(local.y / 0.003, 2.0)) * step(abs(local.x), 0.04);
        //外圈小环
        float ringR = length(local);
        float ring = exp(-pow((ringR - 0.045) / 0.004, 2.0));
        //脉动
        float pulse = 0.6 + 0.4 * sin(time * 2.5 + id * 1.9);
        result += (l1 + l2 + ring * 0.7) * pulse;
    }
    return saturate(result);
}

// ---- G 飘散灰烬 ----
float floatingAsh(float2 centered, float time)
{
    float total = 0.0;
    for (int i = 0; i < 26; i++)
    {
        float id = (float)i;
        float h1 = hash11(id * 1.13);
        float h2 = hash11(id * 3.37);
        float h3 = hash11(id * 7.91);

        float ang = h1 * TAU;
        float baseR = 0.15 + h2 * 0.6;
        float life = frac(time * (0.2 + h3 * 0.25) + h1 * 2.0);

        //粒子会从某半径逐渐向上升并向外飘
        float2 basePos = float2(cos(ang), sin(ang)) * baseR;
        basePos.y -= life * 0.35;
        basePos.x += sin(life * TAU + h2 * 6.0) * 0.05;

        float r = length(centered - basePos);
        float scale = 0.004 + h3 * 0.003;
        float puff = exp(-r * r / (scale * scale));
        puff *= sin(life * PI);
        total += puff;
    }
    return saturate(total);
}

// ---- H 外缘烟雾 ----
float outerSmoke(float2 centered, float dist, float time)
{
    //仅在最外环可见，噪声驱动慢速翻滚
    float2 uv = centered * 2.2 + float2(time * 0.05, time * 0.08);
    float n = fbm(uv);
    float mask = smoothstep(0.6, 0.95, dist);
    return smoothstep(0.35, 0.8, n) * mask;
}

// ============================================================
// 主像素着色
// ============================================================
float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    //归一化到[-1,1]
    float2 centered = coords * 2.0 - 1.0;
    float dist = length(centered);
    float angle = atan2(centered.y, centered.x);

    //域外直接裁掉
    float edgeFade = 1.0 - smoothstep(0.88, 1.02, dist);
    if (edgeFade <= 0.001)
        return float4(0, 0, 0, 0);

    float time = uTime;
    float expand = saturate(expandProgress);
    float dissolve = saturate(dissolveProgress);

    //扩张时整体缩放剪裁：未展开的位置裁成透明
    float expandMask = 1.0 - smoothstep(expand, expand + 0.08, dist);

    //A 底色
    float3 finalColor = abyssBase(centered, dist, time);

    //B 核心瞳孔
    float iris;
    float3 eyeCol = witchEye(centered, time, pulseIntensity, iris);
    //瞳孔仅在展开超过一半后浮现
    float eyeReveal = smoothstep(0.45, 0.85, expand);
    finalColor += eyeCol * eyeReveal * (1.0 - dissolve * 0.6);

    //C 裂纹
    float cracks = radialCracks(centered, dist, angle, time);
    //裂纹颜色：中到核心间插值，内部透亮
    float3 crackCol = lerp(midColor, coreColor, saturate(cracks * 1.4));
    finalColor += crackCol * cracks * (0.8 + pulseIntensity * 0.2);

    //D 经文残片
    float scrip = burningScriptures(centered, time);
    float3 scripCol = lerp(midColor, float3(1.0, 0.85, 0.5), 0.6);
    finalColor += scripCol * scrip * 0.85;

    //E 幽火
    float souls = risingSoulFlames(centered, time);
    //幽火颜色偏冷硫火，加一点青灰
    float3 soulCol = lerp(coreColor, float3(1.0, 0.95, 0.7), 0.4);
    finalColor += soulCol * souls;

    //F 封印
    float brands = brandSeals(centered, dist, angle, time);
    float brandReveal = smoothstep(0.4, 0.9, expand);
    float3 brandCol = lerp(coreColor, float3(1.0, 0.9, 0.55), 0.3);
    finalColor += brandCol * brands * 0.8 * brandReveal;

    //G 灰烬
    float ash = floatingAsh(centered, time);
    float3 ashCol = lerp(edgeColor, coreColor, 0.35);
    finalColor += ashCol * ash * 0.8;

    //H 烟雾
    float smoke = outerSmoke(centered, dist, time);
    finalColor += edgeColor * smoke * 0.6;

    //--------------- 消散阶段：整体向黑化去饱和 ---------------
    //像素块化地变黑（与WitchStatueActor中的剥落呼应）
    if (dissolve > 0.01)
    {
        float charFade = smoothstep(0.0, 1.0, dissolve);
        finalColor = lerp(finalColor, voidColor * 0.3, charFade * 0.8);
    }

    //--------------- 透明度合成 ---------------
    float alpha = 0.0;

    //基础雾填充
    float fill = lerp(0.05, 0.22, smoothstep(0.9, 0.0, dist));
    alpha += fill;

    //元素贡献
    alpha += iris * eyeReveal * 0.9;
    alpha += cracks * 0.85;
    alpha += scrip * 0.7;
    alpha += souls * 0.8;
    alpha += brands * 0.6 * brandReveal;
    alpha += ash * 0.6;
    alpha += smoke * 0.35;

    alpha = saturate(alpha);
    alpha *= edgeFade * fadeAlpha * expand * expandMask;
    //消散时整体透明度下降
    alpha *= 1.0 - dissolve * 0.75;

    return float4(finalColor * alpha, alpha) * vertexColor;
}

technique Technique1
{
    pass WitchBrimstoneDomainPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
