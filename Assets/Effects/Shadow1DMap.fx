//=============================================================================
//Shadow1DMap.fx - 1D阴影图生成着色器
//从光源沿每个角度方向在Tile空间中做射线步进，
//找到第一个遮挡物并存储归一化距离(distTiles / radiusTiles)
//关键: 步进在Tile空间中(各向同性)，UV采样通过uvPerTile转换
//=============================================================================

texture occlusionMap;
sampler occlusionSampler = sampler_state
{
    texture = <occlusionMap>;
    magfilter = POINT;
    minfilter = POINT;
    AddressU = clamp;
    AddressV = clamp;
};

//光源在遮挡图中的UV坐标(0~1)
float2 lightPosUV;
//遮挡图尺寸(Tile数)
float2 mapSize;
//光源半径(Tile单位)
float radiusTiles;

static const float TWO_PI = 6.28318530718;

float4 GenerateShadowMap(float2 coords : TEXCOORD0) : COLOR0
{
    float angle = coords.x * TWO_PI;

    //在Tile空间中的方向(各向同性，1单位=1Tile)
    float2 dirTile = float2(cos(angle), sin(angle));

    //每个Tile对应的UV偏移量(非方形修正的关键)
    float2 uvPerTile = 1.0 / mapSize;

    float stepTiles = radiusTiles / 64.0;
    float minDist = 1.0;

    for (int i = 1; i <= 64; i++)
    {
        float d = stepTiles * (float)i;
        //在Tile空间沿方向步进d个Tile，转换为UV坐标采样
        float2 sampleUV = lightPosUV + dirTile * d * uvPerTile;

        if (sampleUV.x < 0.0 || sampleUV.x > 1.0 || sampleUV.y < 0.0 || sampleUV.y > 1.0)
            break;

        //使用tex2Dlod避免循环内gradient指令问题
        float occ = tex2Dlod(occlusionSampler, float4(sampleUV, 0, 0)).r;
        if (occ > 0.5)
        {
            minDist = d / radiusTiles;
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
