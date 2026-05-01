// ============================================================================
// CyberReform.fx — 赛博空间数据块聚拢重现着色器（瞬移技能终点演出）
// 黑墙数据方块从外围向中心聚拢 → 高光快闪 → 玩家在残留余烬中重现
// 与 CyberRiftSlash 的撕裂段对应，构成"劈裂 → 重现"两段式演出
// 通过 CyberReformProj 以单矩形 quad 渲染（uv 范围 0~1，中心 0.5,0.5）
// ============================================================================

sampler uImage0 : register(s0);
sampler noiseSamp : register(s1);

float uTime;
float fadeAlpha;        //整体透明度 0~1
float reformProgress;   //聚拢进度 0~1（0=方块在最外侧, 1=完成聚拢于中心）
float snapPulse;        //"咔嗒"重现闪光强度 0~1
float dissipate;        //后段消散进度 0~1（重现完成后由外向内逐层熄灭）
float seed;             //本实例随机种子

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoords : TEXCOORD0;
};

float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

//极坐标 wedge 形数据片：从外向内沿固定角度收缩，形成"楔形数据块"
float wedgeBlock(float2 p, float radial, float angle, float blockAng, float thickness, float headRadius)
{
    //极坐标
    float r = length(p);
    float a = atan2(p.y, p.x);
    //归一化角度差
    float da = abs(fmod(a - angle + 3.14159265, 6.2831853) - 3.14159265);
    //角向遮罩（保持角宽度恒定）
    float angleMask = 1.0 - smoothstep(blockAng * 0.5 - 0.012, blockAng * 0.5, da);
    //径向遮罩：内端=headRadius, 外端=headRadius+thickness
    float inner = headRadius;
    float outer = headRadius + thickness;
    float radialMask = step(inner, r) * step(r, outer);
    //径向边缘软化（前端硬边突出，尾端柔和）
    float frontEdge = smoothstep(inner - 0.01, inner + 0.005, r);
    float backEdge = smoothstep(outer + 0.01, outer - 0.005, r);
    return angleMask * radialMask * frontEdge * backEdge;
}

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    //以中心为 0,0 的单位坐标
    float2 p = input.TexCoords - float2(0.5, 0.5);
    float r = length(p);
    float a = atan2(p.y, p.x);

    //圆形剪裁，超出整体最大半径直接 0
    if (r > 0.5) return float4(0, 0, 0, 0);

    // ============================================================
    // A. 数据块聚拢——围绕中心的 16 个楔形数据块，从外向内收拢
    // ============================================================
    const int BLOCK_COUNT = 16;
    float blockSum = 0.0;
    float edgeSum = 0.0;
    float blockAng = 6.2831853 / BLOCK_COUNT * 0.78;  //块占角宽（保留间隙）

    //聚拢曲线：早期块在外围（headRadius=0.45），后期收拢到 0.05
    //使用 ease-in-out 让"聚拢"动作更有冲击感
    float t = saturate(reformProgress);
    float ease = t * t * (3.0 - 2.0 * t);
    float headRadius = lerp(0.46, 0.06, ease);

    //块厚度：聚拢中保持，临近完成时压缩
    float thickness = lerp(0.10, 0.04, ease);

    [unroll]
    for (int i = 0; i < BLOCK_COUNT; i++)
    {
        float fi = (float)i;
        //每块基础角度
        float angle = (fi / (float)BLOCK_COUNT) * 6.2831853;
        //每块独立小抖动（让聚拢不机械）
        float jitter = (hash21(float2(fi, seed * 7.3)) - 0.5) * 0.10;
        //每块独立"前后错峰"延迟（数据真实感）
        float lag = hash21(float2(fi, seed * 3.7 + 1.0));
        float laggedT = saturate((t - lag * 0.18) / 0.82);
        float laggedEase = laggedT * laggedT * (3.0 - 2.0 * laggedT);
        float radius = lerp(0.46, 0.06, laggedEase);

        //该块本体
        float block = wedgeBlock(p, r, angle + jitter, blockAng, thickness, radius);
        //数字闪烁（块独立闪烁）
        float bf = 0.55 + 0.45 * sin(uTime * 14.0 + fi * 1.7 + seed * 5.0);
        block *= bf;

        //块前缘亮边（硬光）
        float frontGlow = step(radius, r) * step(r, radius + 0.012);
        float frontAng = 1.0 - smoothstep(blockAng * 0.4, blockAng * 0.55,
            abs(fmod(a - angle - jitter + 3.14159265, 6.2831853) - 3.14159265));

        blockSum += block;
        edgeSum += frontGlow * frontAng;
    }
    blockSum = saturate(blockSum);
    edgeSum = saturate(edgeSum);

    // ============================================================
    // B. 中心吸入暗核——表示"维度凹陷"的负空间
    // ============================================================
    //聚拢初期中心是真空，临近完成时变亮
    float darkCore = 1.0 - smoothstep(0.0, 0.08, r);
    darkCore *= (1.0 - ease) * 0.55;

    // ============================================================
    // C. SNAP 闪光——重现瞬间整圈白热爆发
    // ============================================================
    //径向高光（中心强、边缘柔）
    float snapRadial = (1.0 - smoothstep(0.0, 0.30, r));
    //径向"咔哒"环（重现瞬间从内向外冲出的能量环）
    float snapRingT = saturate(snapPulse);
    float snapRingR = 0.05 + snapRingT * 0.45;
    float snapRingMask = 1.0 - smoothstep(0.012, 0.04, abs(r - snapRingR));
    //合成闪光
    float snap = (snapRadial * 0.85 + snapRingMask * 0.95) * snapPulse;

    // ============================================================
    // D. 余烬碎片——重现后空间残留的细小漂浮像素
    // ============================================================
    //极坐标网格随机方块
    float gAng = floor(a * 12.0 / 6.2831853);
    float gR = floor(r * 14.0);
    float ts = floor(uTime * 16.0);
    float gh = hash21(float2(gAng + ts * 3.7, gR + seed * 5.0));
    float embers = step(0.86, gh) * gh;
    embers *= smoothstep(0.05, 0.40, r) * (1.0 - smoothstep(0.40, 0.50, r));
    //仅在 SNAP 之后出现
    embers *= snapPulse * 0.7;

    // ============================================================
    // E. 噪声扰动数据片——黑墙背景"沙漠"
    // ============================================================
    float n = tex2D(noiseSamp, frac(float2(p.x * 4.0 + uTime * 0.8, p.y * 4.0 + seed))).r;
    float bgGrain = (n - 0.5) * 0.25;
    bgGrain *= smoothstep(0.0, 0.45, r) * (1.0 - smoothstep(0.42, 0.5, r));

    // ============================================================
    // F. 后段消散：由外向内，让外围块先"塌缩"
    // ============================================================
    float dissMask = 1.0 - smoothstep(0.5 - dissipate * 0.5, 0.55 - dissipate * 0.5, r);
    blockSum *= dissMask;
    edgeSum *= dissMask;
    embers *= dissMask;

    // ============================================================
    // 颜色合成
    // ============================================================
    float3 cBlockBody  = float3(0.06, 0.020, 0.030);   //黑墙块体（深黑红）
    float3 cBlockEdge  = float3(1.00, 0.42, 0.15);     //块边橙红高亮
    float3 cDarkCore   = float3(0.04, 0.005, 0.015);   //中心暗红
    float3 cSnapHot    = float3(1.00, 0.92, 0.78);     //SNAP白热
    float3 cSnapWarm   = float3(1.00, 0.52, 0.20);     //SNAP橙红外焰
    float3 cEmber      = float3(1.00, 0.55, 0.20);     //余烬橙
    float3 cBgGrain    = float3(0.65, 0.08, 0.05);     //背景颗粒红

    //块"实体"：黑墙暗块 + 强亮的橙红前缘
    float3 col = float3(0, 0, 0);
    col += cBlockBody * blockSum;
    col += cBlockEdge * blockSum * 0.65;          //块整体染上一层橙红光
    col += cBlockEdge * edgeSum * 1.6;            //硬亮前缘
    col += cDarkCore * darkCore;
    col += cSnapHot * snap;
    col += cSnapWarm * snap * 0.6;
    col += cEmber * embers;
    col += cBgGrain * bgGrain * 0.6;

    //alpha：块体 + 闪光 + 余烬
    float alpha = saturate(
        blockSum * 0.95
        + edgeSum * 1.0
        + darkCore * 0.7
        + snap * 0.95
        + embers * 0.7
        + abs(bgGrain) * 0.5
    );
    alpha *= fadeAlpha;

    return float4(col * alpha, alpha) * input.Color;
}

technique Technique1
{
    pass ReformPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
