//=============================================================================
//VoidSuction.fx — 虚空吸入演出全屏滤镜
//独立于传送门本体，负责：径向失真、色差加剧、黑闪覆盖
//=============================================================================

sampler uImage0 : register(s0);
sampler noiseTex : register(s1);

float uTime;             //累计时间（秒）
float suctionProgress;   //吸入进度0-1
float blackFlash;        //黑闪覆盖0-1

float2 focusCenter;      //吸入焦点（世界坐标）
float2 screenPosition;   //屏幕左上角（世界坐标）
float2 worldViewSize;    //缩放修正后的世界可视范围

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 original = tex2D(uImage0, coords);

    if (suctionProgress < 0.001 && blackFlash < 0.001)
        return original;

    //世界坐标
    float2 worldPos = screenPosition + worldViewSize * coords;
    float2 toFocus = focusCenter - worldPos;
    float dist = length(toFocus);
    float2 dir = dist > 0.1 ? toFocus / dist : float2(0, 0);

    float effectRadius = max(worldViewSize.x, worldViewSize.y) * 0.8;
    float normDist = saturate(dist / max(effectRadius, 1.0));
    float falloff = saturate(1.0 - normDist * 0.6);

    //径向吸入失真
    float pullStr = suctionProgress * suctionProgress * 0.025 * falloff;
    float2 pullOffset = dir * pullStr * worldViewSize / max(worldViewSize.x, 1.0);
    float2 distortedCoords = clamp(coords + pullOffset, 0.002, 0.998);

    float4 distorted = tex2D(uImage0, distortedCoords);

    //色差分离（径向方向）
    float2 caDir = dir;
    float caStr = suctionProgress * 12.0 / max(worldViewSize.x, 1.0);
    float2 caOffset = caDir * caStr;
    distorted.r = tex2D(uImage0, clamp(distortedCoords + caOffset, 0.002, 0.998)).r;
    distorted.b = tex2D(uImage0, clamp(distortedCoords - caOffset * 0.6, 0.002, 0.998)).b;

    //暗角加深（吸入时视野收窄感）
    float vignette = normDist * normDist;
    float vignetteStr = suctionProgress * 0.5;
    distorted.rgb *= 1.0 - vignette * vignetteStr;

    //黑闪覆盖
    distorted.rgb *= saturate(1.0 - blackFlash);

    return distorted;
}

technique Technique1
{
    pass VoidSuctionPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
