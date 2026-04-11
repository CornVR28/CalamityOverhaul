//=============================================================================
//Shadow1DMap.fx - 1D阴影图生成着色器
//将以光源为中心的遮挡图在极坐标空间中展开，
//对每个角度方向做列归约(取最小距离)，输出1D阴影图
//=============================================================================

//遮挡图纹理，每像素=1个Tile，白色=实心遮挡，黑色=空气
texture occlusionMap;
sampler occlusionSampler = sampler_state
{
    texture = <occlusionMap>;
    magfilter = POINT;
    minfilter = POINT;
    AddressU = clamp;
    AddressV = clamp;
};

//光源在遮挡图中的UV坐标 (0~1)
float2 lightPosUV;
//遮挡图尺寸(像素)
float2 mapSize;
//光源半径(以Tile为单位，归一化到UV空间)
float lightRadiusUV;
//角度分辨率 (输出纹理宽度，如360或720)
float angleResolution;
//射线步进次数
float maxSteps;

//输出: 高度为1的纹理，宽度=angleResolution
//每个像素x对应一个角度，r通道存储归一化距离(0~1, 相对于lightRadius)
float4 GenerateShadowMap(float2 coords : TEXCOORD0) : COLOR0
{
    float angle = coords.x * 6.28318530718; //0~2PI

    float2 dir = float2(cos(angle), sin(angle));

    float stepSize = lightRadiusUV / maxSteps;
    float minDist = 1.0; //默认无遮挡，距离=1(最大)

    for (int i = 1; i <= 96; i++)
    {
        float dist = stepSize * (float)i;
        float2 samplePos = lightPosUV + dir * dist;

        //越界检查
        if (samplePos.x < 0.0 || samplePos.x > 1.0 || samplePos.y < 0.0 || samplePos.y > 1.0)
            break;

        float occlusion = tex2D(occlusionSampler, samplePos).r;
        if (occlusion > 0.5)
        {
            minDist = dist / lightRadiusUV;
            break;
        }
    }

    return float4(minDist, minDist, minDist, 1.0);
}

technique GenerateShadow
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 GenerateShadowMap();
    }
};
