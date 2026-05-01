// ============================================================================
// CyberRiftSlash.fx —— 赛博空间数据走廊着色器（瞬移传输通道）
// 玩家化为像素数据块沿走廊从起点流向终点
// 整个走廊以"黑墙AI"为母题：硬边像素格 + 二进制小字形 + 数据流脉冲 + 色差撕裂
// ============================================================================

float4x4 transformMatrix;
float uTime;
float fadeAlpha;        //总体透明度 0~1
float visibleStart;     //可见区间起点 0~1（尾收时上升）
float visibleEnd;       //可见区间终点 0~1（头延伸时上升）
float glitchSeed;       //本实例随机种子
float impactPulse;      //命中目标的脉冲强度 0~1
float corridorLength;   //走廊像素长度，用于动态调整像素格密度（防止短/长距离形变）

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

struct VSInput
{
    float4 Position : POSITION0;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};

struct PSInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoords : TEXCOORD0;
};

PSInput VertexShaderFunction(VSInput v)
{
    PSInput o;
    o.Position = mul(v.Position, transformMatrix);
    o.Color = v.Color;
    o.TexCoords = v.TexCoords;
    return o;
}

//哈希工具
float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float hash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

//每个 4x6 子像素格的内置字形蒙版，实现伪二进制字符
//用一个 4x6=24 位伪查找表近似（用 hash 当字模索引）
float glyphMask(float2 cellUV, float glyphId)
{
    //cellUV 期望在 0~1
    //把 cell 内部分成 4x6 子像素
    float2 sub = floor(cellUV * float2(4.0, 6.0));
    //字形哈希：每个子像素一个开关
    float h = hash21(sub + float2(glyphId * 13.7, glyphId * 7.3));
    //保留中央列概率更高，让字形看起来更像字符竖笔
    float colWeight = (sub.x == 1.0 || sub.x == 2.0) ? 0.42 : 0.62;
    return step(colWeight, h);
}

float4 PixelShaderFunction(PSInput input) : COLOR0
{
    float2 uv = input.TexCoords;
    float along = uv.x;
    float cross_ = uv.y;
    float crossDist = abs(cross_ - 0.5) * 2.0;

    //可见区间裁剪（带轻微羽化）
    float headMask = smoothstep(visibleEnd + 0.025, visibleEnd - 0.015, along);
    float tailMask = smoothstep(visibleStart - 0.015, visibleStart + 0.025, along);
    float visMask = headMask * tailMask;
    if (visMask < 0.001)
        return float4(0, 0, 0, 0);

    //=========================================================
    //走廊像素格定义：长度自适应密度，让像素始终保持~6~10px
    //corridorLength 来自 CPU 端，单位世界像素
    //=========================================================
    float colCount = clamp(corridorLength / 14.0, 16.0, 64.0);
    float rowCount = 7.0; //横向 7 行，中行=主轴

    //把 uv 映射到格子坐标
    float2 gridIdx = floor(float2(along * colCount, cross_ * rowCount));
    float2 gridFrac = frac(float2(along * colCount, cross_ * rowCount));

    //中心行索引
    float midRow = floor(rowCount * 0.5);
    float rowDist = abs(gridIdx.y - midRow); //距离中行的行数

    //=========================================================
    //数据流脉冲：从起点往终点扫一道亮带（让走廊"运行"起来）
    //=========================================================
    //每帧的扫描位置（在 0~1.5 之间循环，超出 1 后逐渐淡出）
    float flowPhase = frac(uTime * 0.45 + glitchSeed * 0.31);
    float flowDist = abs(along - flowPhase);
    float flowPulse = exp(-flowDist * flowDist * 18.0);
    //叠加第二条反相扫描，制造"双向吞吐"的赛博节拍
    float flowPhase2 = frac(uTime * 0.27 + 0.5 + glitchSeed * 0.13);
    float flowDist2 = abs(along - flowPhase2);
    flowPulse += exp(-flowDist2 * flowDist2 * 28.0) * 0.5;

    //=========================================================
    //每个像素格的状态：通过 hash 决定开关、亮度、颜色档
    //=========================================================
    float timeBucket = floor(uTime * 9.0); //每秒约 9 次刷新
    float hCell = hash21(gridIdx + float2(timeBucket * 1.7, glitchSeed * 11.3));
    //中心行更可能"亮"，外侧行更可能"暗"——模拟数据沿主轴密集传输
    float onThreshold = lerp(0.18, 0.78, rowDist / (rowCount * 0.5));
    float cellOn = step(onThreshold, hCell);

    //每个亮格的亮度档：黑墙暗块 / 中亮 / 高亮
    float hLevel = hash21(gridIdx + float2(timeBucket * 0.7 + 9.0, glitchSeed * 5.1));
    float levelHot = step(0.78, hLevel);    //热块
    float levelMid = step(0.40, hLevel) * (1.0 - levelHot); //中亮
    //levelDim = !hot && !mid（默认黑墙暗块）

    //数据流脉冲增加亮度档：脉冲扫过时所有暗块也提一档
    float pulseBoost = saturate(flowPulse * 1.4);

    //=========================================================
    //每格内的字形蒙版（伪二进制字符）——让每个格子看起来是个数据块
    //=========================================================
    float glyphId = hash21(gridIdx + float2(timeBucket * 0.31, glitchSeed * 3.7));
    float glyph = glyphMask(gridFrac, glyphId);
    //字形小边距（让格子之间留缝，呈现像素阵列感）
    float gutter = step(0.08, gridFrac.x) * step(gridFrac.x, 0.92)
                 * step(0.10, gridFrac.y) * step(gridFrac.y, 0.90);

    //=========================================================
    //中央主轴白热脊线：稳定不熄灭，标识数据走廊主干
    //=========================================================
    float spineCore = 1.0 - smoothstep(0.0, 0.085, crossDist);
    spineCore = pow(saturate(spineCore), 1.4);
    //微弱高频抖动避免死板
    float spineFlicker = 0.85 + 0.15 * sin(uTime * 32.0 + along * 90.0 + glitchSeed * 7.0);
    spineCore *= spineFlicker;

    //=========================================================
    //色差撕裂：左右两侧偏移采样产生霓虹分裂感
    //=========================================================
    float chromaOffset = 0.04 * (0.4 + flowPulse * 0.8);
    float crossL = cross_ - chromaOffset / rowCount;
    float crossR = cross_ + chromaOffset / rowCount;
    float idxLY = floor(crossL * rowCount);
    float idxRY = floor(crossR * rowCount);
    float hCellL = hash21(float2(gridIdx.x, idxLY) + float2(timeBucket * 1.7, glitchSeed * 11.3));
    float hCellR = hash21(float2(gridIdx.x, idxRY) + float2(timeBucket * 1.7, glitchSeed * 11.3));
    float onL = step(onThreshold, hCellL);
    float onR = step(onThreshold, hCellR);

    //=========================================================
    //边缘碎屑：可见末端附近溢出零散像素
    //=========================================================
    float edgeAlong = min(along - visibleStart, visibleEnd - along);
    float edgeFringe = smoothstep(0.06, 0.0, edgeAlong);
    float fringeHash = hash21(gridIdx + float2(timeBucket * 2.7 + 17.0, glitchSeed * 8.1));
    float fringe = step(0.66, fringeHash) * edgeFringe;

    //=========================================================
    //外缘渐隐遮罩（避免走廊边界硬切）
    //=========================================================
    float corridorMask = 1.0 - smoothstep(0.62, 0.98, crossDist);

    //=========================================================
    //尖端能量爆发：当前 visibleEnd 处冲一发硬光
    //=========================================================
    float tipDist = abs(along - visibleEnd);
    float tipFlare = 1.0 - smoothstep(0.0, 0.04, tipDist);
    tipFlare *= (1.0 - crossDist * 0.5);
    float tipPulse = 0.5 + 0.5 * sin(uTime * 38.0 + glitchSeed * 9.0);
    tipFlare *= 0.5 + 0.5 * tipPulse;

    //命中冲击全段提亮
    float impactGlow = impactPulse * (1.0 - crossDist * 0.3);

    //=========================================================
    //合成像素数据块本体强度
    //=========================================================
    //有效像素 = 开关 * 字形 * 间距
    float pixelCore = cellOn * glyph * gutter * corridorMask;
    //热块强度
    float pixelHot = pixelCore * (levelHot + pulseBoost * 0.4);
    //中亮强度
    float pixelMid = pixelCore * (levelMid + pulseBoost * 0.25);
    //暗块强度（黑墙底）
    float pixelDim = pixelCore * (1.0 - levelHot) * 0.55;

    //色差侧影叠加（在像素核心轻微外偏）
    float pixelChromaR = onR * glyph * gutter * corridorMask * 0.35;
    float pixelChromaL = onL * glyph * gutter * corridorMask * 0.35;

    //=========================================================
    //颜色合成（黑墙暗红 + 鲜橙红 + 白热脊线 + 色差青/红）
    //=========================================================
    float3 cBlackWall = float3(0.10, 0.02, 0.04);   //黑墙暗块
    float3 cMidOrange = float3(1.00, 0.42, 0.12);   //中亮橙
    float3 cHotEmber  = float3(1.00, 0.78, 0.45);   //热块橙红
    float3 cWhiteHot  = float3(1.00, 0.96, 0.86);   //白热脊
    float3 cCyanShift = float3(0.20, 0.85, 1.00);   //色差青（偏冷）
    float3 cRedShift  = float3(1.00, 0.20, 0.30);   //色差红
    float3 cTip       = float3(1.00, 0.85, 0.60);
    float3 cImpact    = float3(1.00, 0.92, 0.78);

    float3 color = float3(0, 0, 0);
    //黑墙底块
    color += cBlackWall * pixelDim * 1.6;
    //中亮像素
    color += cMidOrange * pixelMid * 1.1;
    //热块（脉冲带过会"白"一阵）
    color += cHotEmber * pixelHot * (1.0 + pulseBoost * 0.6);
    //白热脊线
    color += cWhiteHot * spineCore;
    //色差侧影
    color += cRedShift  * pixelChromaR * 0.55;
    color += cCyanShift * pixelChromaL * 0.45;
    //尖端 + 命中冲击
    color += cTip * tipFlare * 1.1;
    color += cImpact * impactGlow * 0.9;
    //碎屑
    color += cMidOrange * fringe * 0.7;

    //alpha：核心数据块叠加脊线、碎屑、冲击
    float alpha = saturate(
          pixelCore * 0.85
        + spineCore * 0.95
        + tipFlare * 0.7
        + impactGlow * 0.6
        + fringe * 0.55
    );
    alpha *= fadeAlpha * visMask;

    return float4(color * alpha, alpha) * input.Color;
}

technique Technique1
{
    pass CyberRiftSlashPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
