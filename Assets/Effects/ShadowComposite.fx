//=============================================================================
//ShadowComposite.fx - 阴影合成着色器
//对每个屏幕像素，在像素空间(各向同性)中计算到光源的角度和距离，
//采样1D阴影图，输出该光源的光照贡献
//角度计算与Shadow1DMap一致: 都在各向同性空间(Tile空间≡像素空间)
//=============================================================================

texture shadowMapTex;
sampler shadowMapSampler = sampler_state
{
    texture = <shadowMapTex>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    AddressU = wrap;
    AddressV = clamp;
};

//光源在屏幕中的位置(像素坐标)
float2 lightScreenPos;
//光源半径(像素)
float radiusPixels;
//屏幕尺寸(像素)
float2 screenSize;
//光源颜色(RGB, 0~1)
float3 lightColor;
//光源强度
float lightIntensity;
//软阴影偏移角(弧度)
float softAngle;

static const float TWO_PI = 6.28318530718;

float4 CompositePS(float2 coords : TEXCOORD0) : COLOR0
{
    //当前像素在屏幕中的像素坐标(各向同性空间)
    float2 pixelPos = coords * screenSize;
    float2 diff = pixelPos - lightScreenPos;

    float dist = length(diff);
    float normDist = dist / radiusPixels;

    //超出光源范围
    if (normDist > 1.0)
        return float4(0, 0, 0, 0);

    //计算角度(与Shadow1DMap一致，都在各向同性空间中)
    float angle = atan2(diff.y, diff.x);
    if (angle < 0.0)
        angle += TWO_PI;
    float angleUV = angle / TWO_PI;

    //软阴影PCF: 5次采样(展开循环避免gradient指令问题)
    float shadow = 0.0;
    float occDist;
    float sa;

    //中心采样
    occDist = tex2D(shadowMapSampler, float2(angleUV, 0.5)).r;
    shadow += (normDist < occDist) ? 1.0 : saturate((occDist - normDist + 0.03) / 0.06);

    //+softAngle
    sa = frac(angleUV + softAngle / TWO_PI);
    occDist = tex2D(shadowMapSampler, float2(sa, 0.5)).r;
    shadow += (normDist < occDist) ? 1.0 : saturate((occDist - normDist + 0.03) / 0.06);

    //-softAngle
    sa = frac(angleUV - softAngle / TWO_PI);
    occDist = tex2D(shadowMapSampler, float2(sa, 0.5)).r;
    shadow += (normDist < occDist) ? 1.0 : saturate((occDist - normDist + 0.03) / 0.06);

    //+halfSoftAngle
    sa = frac(angleUV + softAngle * 0.5 / TWO_PI);
    occDist = tex2D(shadowMapSampler, float2(sa, 0.5)).r;
    shadow += (normDist < occDist) ? 1.0 : saturate((occDist - normDist + 0.03) / 0.06);

    //-halfSoftAngle
    sa = frac(angleUV - softAngle * 0.5 / TWO_PI);
    occDist = tex2D(shadowMapSampler, float2(sa, 0.5)).r;
    shadow += (normDist < occDist) ? 1.0 : saturate((occDist - normDist + 0.03) / 0.06);

    shadow /= 5.0;

    //光照衰减: 平滑二次衰减
    float atten = 1.0 - normDist;
    atten = atten * atten;

    float3 result = lightColor * shadow * atten * lightIntensity;
    return float4(result, 1.0);
}

technique Composite
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 CompositePS();
    }
};
