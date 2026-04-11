//=============================================================================
//ShadowComposite.fx - 阴影合成着色器
//对每个屏幕像素，计算到光源的角度和距离，采样1D阴影图，
//判定阴影并叠加光照衰减、颜色，支持多光源Additive累加
//=============================================================================

//1D阴影图纹理(由Shadow1DMap生成)
texture shadowMapTex;
sampler shadowMapSampler = sampler_state
{
    texture = <shadowMapTex>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    AddressU = wrap;
    AddressV = clamp;
};

//光源在屏幕UV中的位置 (0~1)
float2 lightScreenUV;
//光源半径(屏幕UV空间)
float lightRadiusScreenUV;
//屏幕宽高比修正 (width/height)
float aspectRatio;
//光源颜色
float4 lightColor;
//光源强度
float lightIntensity;
//软阴影采样半角(弧度)，越大阴影越软
float softShadowAngle;
//软阴影采样数(必须为奇数，如5,7,9)
float softShadowSamples;

static const float PI = 3.14159265359;
static const float TWO_PI = 6.28318530718;

float4 CompositePS(float2 coords : TEXCOORD0) : COLOR0
{
    //修正宽高比后的方向向量
    float2 diff = coords - lightScreenUV;
    diff.x *= aspectRatio;

    float dist = length(diff);
    float normalizedDist = dist / lightRadiusScreenUV;

    //超过光源范围，无光照
    if (normalizedDist > 1.0)
        return float4(0, 0, 0, 0);

    //计算角度 (0~1 映射到 0~2PI)
    float angle = atan2(diff.y, diff.x);
    if (angle < 0.0)
        angle += TWO_PI;
    float angleUV = angle / TWO_PI;

    //软阴影: 在角度方向上多次采样取平均
    float shadow = 0.0;
    float halfAngle = softShadowAngle;
    float sampleCount = softShadowSamples;
    float stepAngle = halfAngle * 2.0 / max(sampleCount - 1.0, 1.0);

    for (int i = 0; i < 9; i++)
    {
        if ((float)i >= sampleCount)
            break;

        float offsetAngle = -halfAngle + stepAngle * (float)i;
        float sampleAngleUV = angleUV + offsetAngle / TWO_PI;

        //wrap角度
        sampleAngleUV = frac(sampleAngleUV);

        float occDist = tex2D(shadowMapSampler, float2(sampleAngleUV, 0.5)).r;

        if (normalizedDist < occDist)
            shadow += 1.0;
        else
        {
            //半影渐变: 在遮挡边缘做平滑过渡
            float penumbra = saturate((occDist - normalizedDist + 0.02) / 0.04);
            shadow += penumbra;
        }
    }
    shadow /= sampleCount;

    //光照衰减: 平滑二次衰减
    float attenuation = 1.0 - normalizedDist;
    attenuation = attenuation * attenuation;

    //最终光照贡献
    float3 finalLight = lightColor.rgb * shadow * attenuation * lightIntensity;
    return float4(finalLight, shadow * attenuation * lightIntensity);
}

technique Composite
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 CompositePS();
    }
};
