// ============================================================================
// CybCourseLoading.fx — 超梦接入加载界面背景着色器
// 赛博朋克2077风格：黑底 + 霓虹黄几何框架 + 双环旋转指示器
// 全程序化，不含扫描线，低视觉疲劳，高辨识度
// ============================================================================

float uTime;
float uProgress;     //0..1 加载进度
float uAspectRatio;  //屏幕宽高比

#define YELLOW float3(0.965, 0.773, 0.094)
#define CYAN   float3(0.000, 0.898, 1.000)
#define BASE   float3(0.007, 0.010, 0.022)

//水平细线SDF（UV空间）
float hLine(float y, float yCurr, float halfThick)
{
    return smoothstep(halfThick, 0.0, abs(yCurr - y));
}

//垂直细线SDF（UV空间）
float vLine(float x, float xCurr, float halfThick)
{
    return smoothstep(halfThick, 0.0, abs(xCurr - x));
}

float4 PSCybCourseLoading(float2 uv : TEXCOORD0) : COLOR0
{
    float3 col = BASE;

    // ====================================================================
    // Layer 1 — 左侧双竖线：主线+副线，赛博朋克标志性边栏
    // ====================================================================
    col += YELLOW * vLine(0.013, uv.x, 0.0012);
    col += YELLOW * vLine(0.023, uv.x, 0.0007) * 0.32;

    // ====================================================================
    // Layer 2 — 标题区水平横线（顶部+底部，从左栏延伸到页面中间偏右）
    // ====================================================================
    float hl_top = hLine(0.082, uv.y, 0.0013) * step(0.025, uv.x) * step(uv.x, 0.460);
    col += YELLOW * hl_top * 0.82;

    float hl_bot = hLine(0.858, uv.y, 0.0010) * step(0.025, uv.x) * step(uv.x, 0.560);
    col += YELLOW * hl_bot * 0.52;

    // ====================================================================
    // Layer 3 — 进度条（y≈0.908）：深色轨道 + 黄色填充 + 前沿辉光
    // ====================================================================
    float barY    = 0.908;
    float barHalf = 0.009;
    float barDist = abs(uv.y - barY);

    //深色轨道
    col += YELLOW * smoothstep(barHalf + 0.004, barHalf, barDist) * 0.09;

    //填充
    if (uv.x <= uProgress)
    {
        float fill = smoothstep(barHalf, 0.0, barDist);
        fill = fill * fill;
        col = lerp(col, YELLOW, fill * 0.93);
        //前沿辉光
        col += YELLOW * exp(-(uProgress - uv.x) * 58.0) * fill * 0.68;
    }

    //轨道边框线（上下各一条）
    col += YELLOW * smoothstep(0.0014, 0.0, abs(barDist - barHalf)) * 0.40;

    // ====================================================================
    // Layer 4 — 四角L形标注框（赛博朋克最典型的UI元素）
    // ====================================================================
    float cS = 0.048; //L臂长度
    float cT = 0.0028;//线宽（UV空间，Y方向）
    float cM = 0.022; //距屏幕边缘距离
    float corner = 0.0;

    //左上
    corner += hLine(cM,     uv.y, cT) * step(cM,       uv.x) * step(uv.x, cM + cS);
    corner += vLine(cM,     uv.x, cT) * step(cM,       uv.y) * step(uv.y, cM + cS);
    //右上
    corner += hLine(cM,     uv.y, cT) * step(1.0-cM-cS, uv.x) * step(uv.x, 1.0-cM);
    corner += vLine(1.0-cM, uv.x, cT) * step(cM,        uv.y) * step(uv.y, cM + cS);
    //左下
    corner += hLine(1.0-cM, uv.y, cT) * step(cM,        uv.x) * step(uv.x, cM + cS);
    corner += vLine(cM,     uv.x, cT) * step(1.0-cM-cS, uv.y) * step(uv.y, 1.0-cM);
    //右下
    corner += hLine(1.0-cM, uv.y, cT) * step(1.0-cM-cS, uv.x) * step(uv.x, 1.0-cM);
    corner += vLine(1.0-cM, uv.x, cT) * step(1.0-cM-cS, uv.y) * step(uv.y, 1.0-cM);

    col += YELLOW * saturate(corner) * 0.90;

    // ====================================================================
    // Layer 5 — 中心旋转指示器（极坐标，宽高比修正使其显示为正圆）
    //   外圆：4段黄色弧（顺时针）
    //   内圆：3段青色弧（逆时针）
    //   中心：黄色脉冲点
    // ====================================================================
    float2 spinCenter = float2(0.500, 0.462);
    float2 rel = uv - spinCenter;
    rel.x *= uAspectRatio;
    float spinR = length(rel);
    float spinA = atan2(rel.y, rel.x);

    //外层4弧
    float outerArc = smoothstep(0.0030, 0.0, abs(spinR - 0.063));
    outerArc *= smoothstep(-0.30, 0.20, sin(spinA * 4.0 - uTime * 2.8));
    col += YELLOW * outerArc * 0.88;

    //内层3弧（青色，逆转）
    float innerArc = smoothstep(0.0022, 0.0, abs(spinR - 0.040));
    innerArc *= smoothstep(-0.30, 0.20, sin(spinA * 3.0 + uTime * 3.6));
    col += CYAN * innerArc * 0.62;

    //中心点（心跳脉冲）
    float pulse = 0.58 + 0.42 * abs(sin(uTime * 2.3));
    col += YELLOW * smoothstep(0.008, 0.0, spinR) * pulse;

    // ====================================================================
    // Post — 轻微暗角（强化屏幕中心聚焦感）
    // ====================================================================
    float2 vigUV = uv - 0.5;
    float vig = 1.0 - dot(vigUV, vigUV) * 2.0;
    col *= saturate(vig * 0.5 + 0.58);

    return float4(saturate(col), 1.0);
}

technique CybCourseLoading
{
    pass P0
    {
        PixelShader = compile ps_3_0 PSCybCourseLoading();
    }
}
