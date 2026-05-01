// ============================================================================
// CyberReform.fx —— 赛博空间体素重组/分解共用着色器
// 风格对齐：CyberspaceField 的领域边缘方格栅栏——干净红边描边块 + 暗红内填
// 单矩形 quad 上渲染规则像素网格，每格按"出生位置 → 归位"动画
// 通过 direction 区分演出方向：+1 = 向心重组（终点）, -1 = 离心分解（起点）
// ============================================================================

sampler uImage0 : register(s0);
sampler noiseSamp : register(s1);

float uTime;
float fadeAlpha;        //整体透明度 0~1
float reformProgress;   //演出进度 0~1
float snapPulse;        //"咔嗒"重现/解构闪光强度 0~1
float dissipate;        //后段消散进度 0~1
float seed;             //本实例随机种子
float direction;        //+1 = reform(向心), -1 = decompose(离心)

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

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float2 p = input.TexCoords - float2(0.5, 0.5);
    float r = length(p);

    //圆形剪裁
    if (r > 0.55) return float4(0, 0, 0, 0);

    //=========================================================
    //目标网格：18x18，每格归属于一个固定的 cell 索引
    //=========================================================
    const float CELL = 18.0;
    float2 cellIdx = floor(input.TexCoords * CELL);
    float2 cellCenterUV = (cellIdx + 0.5) / CELL;
    float2 cellP = cellCenterUV - float2(0.5, 0.5);

    //轮廓蒙版（人形：高瘦椭圆）
    float2 ellip = cellP / float2(0.30, 0.42);
    float ellipDist = dot(ellip, ellip);
    float silhouette = step(ellipDist, 1.0);

    //外环装饰：稀疏点亮一些环带格子，丰富剪影外的视觉
    float ringR = length(cellP);
    float inRing = step(0.32, ringR) * step(ringR, 0.50);
    float ringHash = hash21(cellIdx + float2(seed * 7.7, 17.3));
    float isRingCell = inRing * step(0.82, ringHash);

    float belongs = saturate(silhouette + isRingCell);
    if (belongs < 0.5) {
        belongs = 0.0;
    }

    //=========================================================
    //每格独立时序：lag 控制先后差
    //=========================================================
    float lag = hash21(cellIdx + float2(seed * 3.7, 1.7)) * 0.40;
    float laggedT = saturate((reformProgress - lag) / max(1.0 - lag, 0.001));
    float ease = laggedT * laggedT * (3.0 - 2.0 * laggedT);

    //=========================================================
    //飞行偏移：每格随机径向 + 切向方向，让动画有"数据飞入/飞散"的方向感
    //=========================================================
    float2 radialDir = (ringR < 0.001) ? float2(1.0, 0.0) : (cellP / ringR);
    float2 tangent = float2(-radialDir.y, radialDir.x);
    float jitter = (hash21(cellIdx + float2(seed * 5.1, 9.7)) - 0.5) * 1.0;
    float2 flyDir = normalize(radialDir + tangent * jitter);
    float flyDistMax = 0.30 + hash21(cellIdx + float2(seed * 11.0, 27.0)) * 0.20;

    //direction = +1 (reform): 起始远离目标，归位时回到目标
    //direction = -1 (decompose): 起始在目标，飞远离散
    float offsetMag = (direction > 0.0) ? (1.0 - ease) * flyDistMax : ease * flyDistMax;
    float2 currentCellPos = cellP + flyDir * offsetMag;

    //=========================================================
    //当前像素在该格"飞到位置"内的局部坐标 [0,1]
    //=========================================================
    float2 toCell = (p - currentCellPos) * CELL + 0.5;
    float inside = step(0.0, toCell.x) * step(toCell.x, 1.0)
                 * step(0.0, toCell.y) * step(toCell.y, 1.0);
    if (inside < 0.5) inside = 0.0;

    //=========================================================
    //每格的"激活态"：是否该格当前可见
    //通过 lag 控制先后；direction=+1 时，进度推进过 lag 才激活；direction=-1 时反之
    //=========================================================
    float activeStrength;
    if (direction > 0.0) {
        //reform: laggedT 推进时格子越来越实
        activeStrength = smoothstep(0.0, 0.18, laggedT) * (1.0 - smoothstep(1.05, 1.25, laggedT));
    }
    else {
        //decompose: 0 时全实，逐渐淡出
        activeStrength = (1.0 - smoothstep(0.55, 1.0, laggedT));
    }

    //=========================================================
    //单格图案：红边描边方块 + 内部暗红渐变
    //完全不使用字形/字模，与领域边缘栅栏视觉对齐
    //=========================================================
    //到格子边缘的距离（局部坐标 [0,1] 内）
    float bx = min(toCell.x, 1.0 - toCell.x);
    float by = min(toCell.y, 1.0 - toCell.y);
    float borderDist = min(bx, by);

    //描边线：靠近边的部分高亮
    //调整边宽 ~12% 让线粗细可见
    float borderLine = 1.0 - smoothstep(0.05, 0.13, borderDist);
    //内填渐变：从中心暗到边缘略亮
    float interior = smoothstep(0.10, 0.50, borderDist);
    interior = (1.0 - interior) * 0.35 + interior * 0.55;
    //内填区域：避开描边的部分
    float interiorMask = step(0.13, borderDist);
    //描边硬边：避免太软
    float borderMask = step(borderDist, 0.13);

    //角节点强调：四角小亮点（与领域栅格节点呼应）
    float node = (1.0 - smoothstep(0.0, 0.10, bx)) * (1.0 - smoothstep(0.0, 0.10, by));

    //每格随机"亮档"：少数格子比其他更亮（对应领域里的"激活节点"）
    float lvl = hash21(cellIdx + float2(seed * 1.3, 41.0));
    float isHot = step(0.78, lvl);
    float hotPulse = 0.7 + 0.3 * sin(uTime * 6.0 + lvl * 12.0);

    //归位 / 解构 边缘 SNAP（reform 临归位/decompose 刚撕开）
    float settleFlash = 0.0;
    if (direction > 0.0) {
        settleFlash = smoothstep(0.85, 1.0, laggedT) * (1.0 - smoothstep(1.0, 1.10, laggedT));
    }
    else {
        settleFlash = (1.0 - smoothstep(0.0, 0.18, laggedT));
    }

    //=========================================================
    //外缘消散（仅 reform 后期外圈先熄灭）
    //=========================================================
    float dissMask = 1.0;
    if (direction > 0.0) {
        dissMask = 1.0 - smoothstep(0.50 - dissipate * 0.50, 0.55 - dissipate * 0.50, ringR);
    }

    //=========================================================
    //中心 SNAP 闪光：玩家在中心实体化
    //=========================================================
    float snapRadial = 1.0 - smoothstep(0.0, 0.30, r);
    //倾斜十字撕裂带
    float angle1 = 0.18;
    float angle2 = 1.5708 + 0.10;
    float2 d1 = float2(cos(angle1), sin(angle1));
    float2 d2 = float2(cos(angle2), sin(angle2));
    float band1 = exp(-pow(dot(p, float2(-d1.y, d1.x)) * 28.0, 2.0))
                * smoothstep(0.40, 0.0, abs(dot(p, d1)));
    float band2 = exp(-pow(dot(p, float2(-d2.y, d2.x)) * 32.0, 2.0))
                * smoothstep(0.45, 0.0, abs(dot(p, d2)));
    float snap = (snapRadial * 0.7 + (band1 + band2 * 0.85) * 0.9) * snapPulse;

    //=========================================================
    //合成像素强度
    //=========================================================
    float showStrength = belongs * inside * activeStrength * dissMask;
    //描边亮度（主视觉）
    float borderAmt = showStrength * borderLine
                    * (1.0 + isHot * 0.6 + settleFlash * 1.2 + (isHot * (hotPulse - 0.7)) * 1.3);
    //内填亮度（暗红填充）
    float interiorAmt = showStrength * interior * interiorMask
                      * (0.55 + isHot * 0.4 + settleFlash * 0.6);
    //角节点
    float nodeAmt = showStrength * node * (0.6 + isHot * 0.6 + settleFlash * 1.2);

    //=========================================================
    //颜色（与领域边缘对齐：暗红 / 血红 / 鲜红 / 白热）
    //=========================================================
    float3 cInteriorDark = float3(0.18, 0.02, 0.04);  //内填暗红
    float3 cBorder       = float3(0.95, 0.18, 0.12);  //描边鲜红
    float3 cBorderHot    = float3(1.00, 0.45, 0.22);  //热块描边
    float3 cNode         = float3(1.00, 0.55, 0.30);  //角节点橙
    float3 cSnapHot      = float3(1.00, 0.92, 0.78);
    float3 cSnapWarm     = float3(1.00, 0.50, 0.20);

    float3 col = float3(0, 0, 0);
    col += cInteriorDark * interiorAmt * 1.4;
    col += lerp(cBorder, cBorderHot, isHot * 0.7 + settleFlash * 0.6) * borderAmt;
    col += cNode * nodeAmt * 0.7;
    col += cSnapHot * snap;
    col += cSnapWarm * snap * 0.55;

    //alpha：描边为主，内填、角节点、SNAP 辅助
    float alpha = saturate(
          borderAmt * 0.95
        + interiorAmt * 0.65
        + nodeAmt * 0.75
        + snap * 0.95
    );
    alpha *= fadeAlpha;

    return float4(col * alpha, alpha) * input.Color;
}

technique Technique1
{
    pass CyberReformPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
