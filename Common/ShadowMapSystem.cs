using InnoVault.RenderHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityOverhaul.Common
{
    /// <summary>
    /// 光源数据，描述一个点光源的属性
    /// </summary>
    public struct ShadowLightSource
    {
        /// <summary>
        /// 光源世界坐标(像素)
        /// </summary>
        public Vector2 WorldPosition;
        /// <summary>
        /// 光源半径(像素)
        /// </summary>
        public float Radius;
        /// <summary>
        /// 光源颜色
        /// </summary>
        public Color LightColor;
        /// <summary>
        /// 光源强度 0~1+
        /// </summary>
        public float Intensity;
        /// <summary>
        /// 软阴影采样半角(弧度)，0=硬阴影，越大越软，建议0.02~0.08
        /// </summary>
        public float SoftShadowAngle;

        public ShadowLightSource(Vector2 worldPos, float radius, Color color, float intensity = 1f, float softAngle = 0.03f) {
            WorldPosition = worldPos;
            Radius = radius;
            LightColor = color;
            Intensity = intensity;
            SoftShadowAngle = softAngle;
        }
    }

    /// <summary>
    /// 1D Shadow Map 实时阴影系统
    /// 使用极坐标展开+列归约生成每个光源的1D阴影图，
    /// 然后在合成Pass中对每个像素做距离比较，实现遮挡阴影。
    /// 多光源通过Additive Blend累加光照贡献到同一张RT。
    /// </summary>
    public class ShadowMapSystem : RenderHandle
    {
        //===着色器资源===
        private static Effect _shadow1DMapShader;
        private static Effect _shadowCompositeShader;

        //===渲染目标===
        //遮挡图: 每像素=1Tile，白色=实心
        private static Texture2D _occlusionMap;
        private static Color[] _occlusionData;
        private static int _occMapWidth, _occMapHeight;

        //1D阴影图: 宽度=角度分辨率，高度=1
        private static RenderTarget2D _shadow1DTarget;

        //光照累加RT: 全屏分辨率，所有光源的光照贡献累加于此
        private static RenderTarget2D _lightAccumTarget;

        //全屏白色纹理(用于绘制全屏quad)
        private static Texture2D _pixelTexture;

        //===配置===
        /// <summary>
        /// 1D阴影图的角度分辨率，越高越精细，建议360~720
        /// </summary>
        public static int AngleResolution = 360;
        /// <summary>
        /// 射线步进最大次数，越高遮挡精度越好但GPU开销越大
        /// </summary>
        public static int MaxRaySteps = 96;
        /// <summary>
        /// 软阴影默认采样数(奇数)
        /// </summary>
        public static int SoftShadowSamples = 5;
        /// <summary>
        /// 全局阴影开关
        /// </summary>
        public static bool Enabled = false;
        /// <summary>
        /// 环境暗度 0=不暗化 1=全黑 建议0.85~0.95
        /// </summary>
        public static float AmbientDarkness = 0.92f;
        /// <summary>
        /// 是否自动收集屏幕内的发光Tile作为光源
        /// </summary>
        public static bool AutoCollectTileLights = true;
        /// <summary>
        /// 是否自动为玩家添加微弱光源
        /// </summary>
        public static bool AutoPlayerLight = true;
        /// <summary>
        /// 玩家默认光源半径(像素)
        /// </summary>
        public static float PlayerLightRadius = 200f;
        /// <summary>
        /// 玩家默认光源强度
        /// </summary>
        public static float PlayerLightIntensity = 0.6f;
        /// <summary>
        /// 玩家默认光源颜色
        /// </summary>
        public static Color PlayerLightColor = new(220, 210, 180);
        /// <summary>
        /// 火把类光源默认半径(像素)
        /// </summary>
        public static float TorchLightRadius = 320f;
        /// <summary>
        /// 火把类光源默认强度
        /// </summary>
        public static float TorchLightIntensity = 1.0f;
        /// <summary>
        /// 自动收集的Tile光源最大数量，防止性能爆炸
        /// </summary>
        public static int MaxAutoLights = 30;
        /// <summary>
        /// Tile光源扫描间隔(每N帧扫描一次，降低CPU开销)
        /// </summary>
        public static int TileScanInterval = 3;

        //===光源列表===
        private static readonly List<ShadowLightSource> _lightSources = [];
        //缓存的Tile光源(降低扫描频率用)
        private static readonly List<ShadowLightSource> _cachedTileLights = [];
        private static int _tileScanTimer;
        //当前帧Tile范围
        private static int _tileStartX, _tileStartY;

        //让阴影系统在后处理中较早执行
        public override float Weight => 0.5f;

        public override void Load() {
            if (Main.dedServ) return;
            Main.OnResolutionChanged += HandleResolutionChanged;
        }

        public override void Unload() {
            Main.OnResolutionChanged -= HandleResolutionChanged;
            DisposeResources();
            _shadow1DMapShader = null;
            _shadowCompositeShader = null;
            _pixelTexture = null;
        }

        /// <summary>
        /// 外部调用: 注册一个光源，每帧需要重新注册
        /// </summary>
        public static void AddLight(ShadowLightSource light) {
            _lightSources.Add(light);
        }

        /// <summary>
        /// 外部调用: 快速添加光源
        /// </summary>
        public static void AddLight(Vector2 worldPos, float radius, Color color, float intensity = 1f) {
            _lightSources.Add(new ShadowLightSource(worldPos, radius, color, intensity));
        }

        /// <summary>
        /// 自动收集玩家光源和屏幕内发光Tile
        /// </summary>
        private static void CollectAutoLights() {
            //玩家光源
            if (AutoPlayerLight && Main.LocalPlayer != null && Main.LocalPlayer.active && !Main.LocalPlayer.dead) {
                _lightSources.Add(new ShadowLightSource(
                    Main.LocalPlayer.Center,
                    PlayerLightRadius,
                    PlayerLightColor,
                    PlayerLightIntensity,
                    0.05f
                ));
            }

            //Tile光源: 降频扫描，使用缓存
            if (AutoCollectTileLights) {
                _tileScanTimer++;
                if (_tileScanTimer >= TileScanInterval || _cachedTileLights.Count == 0) {
                    _tileScanTimer = 0;
                    ScanTileLights();
                }
                _lightSources.AddRange(_cachedTileLights);
            }
        }

        /// <summary>
        /// 扫描屏幕内发光Tile，按亮度排序取前N个
        /// </summary>
        private static void ScanTileLights() {
            _cachedTileLights.Clear();

            int startX = (int)(Main.screenPosition.X / 16f) - 2;
            int startY = (int)(Main.screenPosition.Y / 16f) - 2;
            int endX = startX + Main.screenWidth / 16 + 4;
            int endY = startY + Main.screenHeight / 16 + 4;

            startX = Math.Max(0, startX);
            startY = Math.Max(0, startY);
            endX = Math.Min(Main.maxTilesX - 1, endX);
            endY = Math.Min(Main.maxTilesY - 1, endY);

            //每隔几格扫描一次减少计算量，发光物通常至少占1~2格
            for (int x = startX; x <= endX; x++) {
                for (int y = startY; y <= endY; y++) {
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile) continue;

                    int type = tile.TileType;
                    if (!Main.tileLighted[type]) continue;

                    //获取该Tile的光照颜色
                    float r = 0f, g = 0f, b = 0f;
                    GetTileLightColor(x, y, type, ref r, ref g, ref b);

                    //跳过不发光或极弱光源
                    float brightness = r + g + b;
                    if (brightness < 0.1f) continue;

                    //避免重复: 多格物件只取左上角
                    if (!IsTileOrigin(x, y, tile)) continue;

                    //根据亮度推算半径和强度
                    float radius = TorchLightRadius * Math.Min(brightness / 1.5f, 1.5f);
                    float intensity = TorchLightIntensity * Math.Min(brightness / 1.0f, 1.2f);

                    //颜色从光照值转换
                    Color color = new((int)(Math.Min(r / brightness, 1f) * 255),
                                      (int)(Math.Min(g / brightness, 1f) * 255),
                                      (int)(Math.Min(b / brightness, 1f) * 255));

                    Vector2 worldPos = new(x * 16f + 8f, y * 16f + 8f);

                    _cachedTileLights.Add(new ShadowLightSource(worldPos, radius, color, intensity, 0.02f));

                    if (_cachedTileLights.Count >= MaxAutoLights) return;
                }
            }
        }

        /// <summary>
        /// 获取Tile发光颜色，处理原版常见发光Tile和ModTile
        /// </summary>
        private static void GetTileLightColor(int i, int j, int type, ref float r, ref float g, ref float b) {
            //先尝试ModTile
            ModTile modTile = TileLoader.GetTile(type);
            if (modTile != null) {
                modTile.ModifyLight(i, j, ref r, ref g, ref b);
                return;
            }

            //原版常见发光Tile的默认光照，通过Lighting.GetColor近似
            switch (type) {
                case TileID.Torches:
                    r = 1.0f; g = 0.95f; b = 0.8f;
                    break;
                case TileID.Campfire:
                    r = 1.1f; g = 0.95f; b = 0.55f;
                    break;
                case TileID.Candles:
                case TileID.PeaceCandle:
                case TileID.WaterCandle:
                case TileID.Candelabras:
                    r = 0.8f; g = 0.75f; b = 0.6f;
                    break;
                case TileID.Chandeliers:
                    r = 0.95f; g = 0.9f; b = 0.75f;
                    break;
                case TileID.HangingLanterns:
                case TileID.ChineseLanterns:
                    r = 0.85f; g = 0.8f; b = 0.65f;
                    break;
                case TileID.Lamps:
                    r = 0.9f; g = 0.85f; b = 0.7f;
                    break;
                case TileID.FireflyinaBottle:
                    r = 0.5f; g = 0.7f; b = 0.3f;
                    break;
                case TileID.LightningBuginaBottle:
                    r = 0.5f; g = 0.4f; b = 0.8f;
                    break;
                default:
                    //回退: 用Lighting.GetColor近似
                    Color c = Lighting.GetColor(i, j);
                    r = c.R / 255f;
                    g = c.G / 255f;
                    b = c.B / 255f;
                    break;
            }
        }

        /// <summary>
        /// 判断是否是多格物件的主格(左上角)，避免同一物件注册多个光源
        /// </summary>
        private static bool IsTileOrigin(int x, int y, Tile tile) {
            int frameX = tile.TileFrameX;
            int frameY = tile.TileFrameY;
            //火把等1x1物件总是origin
            if (frameX == 0 && frameY == 0) return true;
            //多格物件: 只认frameX和frameY都是其周期起点的格子
            //大多数发光Tile是 18px 间隔
            return (frameX % 18 == 0 || frameX == 0) && (frameY % 18 == 0 || frameY == 0)
                && frameX < 18 && frameY < 18;
        }

        public override void EndCaptureDraw(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            if (!Enabled || Main.gameMenu) return;

            //自动收集光源
            CollectAutoLights();

            if (_lightSources.Count == 0) {
                //没有光源但开启了阴影，绘制全黑遮罩
                DrawFullDarkness(sb, gd, screenSwap);
                _lightSources.Clear();
                return;
            }

            EnsureResources(gd);
            LoadShadersIfNeeded();

            if (_shadow1DMapShader == null || _shadowCompositeShader == null) {
                _lightSources.Clear();
                return;
            }

            //Step1: CPU生成遮挡图
            UpdateOcclusionMap();

            //Step2: 清空光照累加RT
            gd.SetRenderTarget(_lightAccumTarget);
            gd.Clear(Color.Black);
            gd.SetRenderTarget(null);

            //Step3: 对每个光源生成1D阴影图并累加光照
            foreach (var light in _lightSources) {
                RenderLightShadow(sb, gd, light);
            }

            //Step4: 合成到主屏幕——反转光照为阴影遮罩
            ApplyToScreen(sb, gd, screenSwap);

            _lightSources.Clear();
        }

        /// <summary>
        /// 没有光源时的全屏暗化
        /// </summary>
        private void DrawFullDarkness(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            EnsurePixelTexture(gd);

            gd.SetRenderTarget(screenSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();

            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(screenSwap, Vector2.Zero, Color.White);
            //叠加暗化层
            sb.Draw(_pixelTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.Black * AmbientDarkness);
            sb.End();
        }

        /// <summary>
        /// CPU端生成Tile遮挡图
        /// </summary>
        private void UpdateOcclusionMap() {
            //计算可见Tile范围(加padding)
            int padding = 8;
            _tileStartX = (int)(Main.screenPosition.X / 16f) - padding;
            _tileStartY = (int)(Main.screenPosition.Y / 16f) - padding;
            int newW = Main.screenWidth / 16 + padding * 2 + 2;
            int newH = Main.screenHeight / 16 + padding * 2 + 2;

            //尺寸变化时重建
            if (_occlusionMap == null || newW != _occMapWidth || newH != _occMapHeight) {
                _occlusionMap?.Dispose();
                _occMapWidth = newW;
                _occMapHeight = newH;
                _occlusionMap = new Texture2D(Main.graphics.GraphicsDevice, _occMapWidth, _occMapHeight);
                _occlusionData = new Color[_occMapWidth * _occMapHeight];
            }

            //填充遮挡数据
            for (int y = 0; y < _occMapHeight; y++) {
                for (int x = 0; x < _occMapWidth; x++) {
                    int tileX = _tileStartX + x;
                    int tileY = _tileStartY + y;

                    bool solid = false;
                    if (tileX >= 0 && tileX < Main.maxTilesX && tileY >= 0 && tileY < Main.maxTilesY) {
                        Tile tile = Main.tile[tileX, tileY];
                        solid = tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                    }

                    _occlusionData[y * _occMapWidth + x] = solid ? Color.White : Color.Black;
                }
            }

            _occlusionMap.SetData(_occlusionData);
        }

        /// <summary>
        /// 对单个光源: 生成1D阴影图 → 合成光照到累加RT
        /// </summary>
        private void RenderLightShadow(SpriteBatch sb, GraphicsDevice gd, ShadowLightSource light) {
            //光源在遮挡图中的UV坐标
            float lightTileX = light.WorldPosition.X / 16f - _tileStartX;
            float lightTileY = light.WorldPosition.Y / 16f - _tileStartY;
            float lightUVx = lightTileX / _occMapWidth;
            float lightUVy = lightTileY / _occMapHeight;

            //光源半径转遮挡图UV空间
            float radiusTiles = light.Radius / 16f;
            //取较大维度做归一化
            float lightRadiusUV = radiusTiles / Math.Max(_occMapWidth, _occMapHeight);

            //===Pass1: 生成1D阴影图===
            gd.SetRenderTarget(_shadow1DTarget);
            gd.Clear(Color.White); //默认无遮挡

            _shadow1DMapShader.Parameters["occlusionMap"]?.SetValue(_occlusionMap);
            _shadow1DMapShader.Parameters["lightPosUV"]?.SetValue(new Vector2(lightUVx, lightUVy));
            _shadow1DMapShader.Parameters["mapSize"]?.SetValue(new Vector2(_occMapWidth, _occMapHeight));
            _shadow1DMapShader.Parameters["lightRadiusUV"]?.SetValue(lightRadiusUV);
            _shadow1DMapShader.Parameters["angleResolution"]?.SetValue((float)AngleResolution);
            _shadow1DMapShader.Parameters["maxSteps"]?.SetValue((float)MaxRaySteps);

            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            _shadow1DMapShader.CurrentTechnique.Passes[0].Apply();
            EnsurePixelTexture(gd);
            sb.Draw(_pixelTexture, new Rectangle(0, 0, AngleResolution, 1), Color.White);
            sb.End();

            //===Pass2: 合成光照到累加RT===
            gd.SetRenderTarget(_lightAccumTarget);
            //Additive混合，多光源光照叠加

            //光源屏幕UV
            Vector2 lightScreenPos = light.WorldPosition - Main.screenPosition;
            float lightScreenUVx = lightScreenPos.X / Main.screenWidth;
            float lightScreenUVy = lightScreenPos.Y / Main.screenHeight;
            float aspect = (float)Main.screenWidth / Main.screenHeight;

            //光源半径转屏幕UV
            float lightRadiusScreenUV = light.Radius / Main.screenHeight;

            _shadowCompositeShader.Parameters["shadowMapTex"]?.SetValue(_shadow1DTarget);
            _shadowCompositeShader.Parameters["lightScreenUV"]?.SetValue(new Vector2(lightScreenUVx, lightScreenUVy));
            _shadowCompositeShader.Parameters["lightRadiusScreenUV"]?.SetValue(lightRadiusScreenUV);
            _shadowCompositeShader.Parameters["aspectRatio"]?.SetValue(aspect);
            _shadowCompositeShader.Parameters["lightColor"]?.SetValue(light.LightColor.ToVector4());
            _shadowCompositeShader.Parameters["lightIntensity"]?.SetValue(light.Intensity);
            _shadowCompositeShader.Parameters["softShadowAngle"]?.SetValue(light.SoftShadowAngle);
            _shadowCompositeShader.Parameters["softShadowSamples"]?.SetValue((float)SoftShadowSamples);

            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            _shadowCompositeShader.CurrentTechnique.Passes[0].Apply();
            sb.Draw(_pixelTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);
            sb.End();
        }

        /// <summary>
        /// 将光照累加RT合成到主屏幕: 光照区域亮，无光照区域暗
        /// </summary>
        private void ApplyToScreen(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            //先保存当前屏幕到swap
            gd.SetRenderTarget(screenSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();

            //绘制回主屏幕: 原画面 × (环境光 + 光照贡献)
            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            //先画原画面
            sb.Draw(screenSwap, Vector2.Zero, Color.White);
            sb.End();

            //叠加暗化层
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            sb.Draw(_pixelTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                Color.Black * AmbientDarkness);
            sb.End();

            //叠加光照(Additive): 光照区域"加亮"回来
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            sb.Draw(_lightAccumTarget, Vector2.Zero, Color.White);
            sb.End();
        }

        private void EnsureResources(GraphicsDevice gd) {
            //1D阴影图RT
            if (_shadow1DTarget == null || _shadow1DTarget.IsDisposed
                || _shadow1DTarget.Width != AngleResolution) {
                _shadow1DTarget?.Dispose();
                _shadow1DTarget = new RenderTarget2D(gd, AngleResolution, 1,
                    false, SurfaceFormat.Color, DepthFormat.None);
            }

            //光照累加RT
            if (_lightAccumTarget == null || _lightAccumTarget.IsDisposed
                || _lightAccumTarget.Width != Main.screenWidth || _lightAccumTarget.Height != Main.screenHeight) {
                _lightAccumTarget?.Dispose();
                _lightAccumTarget = new RenderTarget2D(gd, Main.screenWidth, Main.screenHeight,
                    false, SurfaceFormat.Color, DepthFormat.None);
            }

            EnsurePixelTexture(gd);
        }

        private void EnsurePixelTexture(GraphicsDevice gd) {
            if (_pixelTexture == null || _pixelTexture.IsDisposed) {
                _pixelTexture = new Texture2D(gd, 1, 1);
                _pixelTexture.SetData([Color.White]);
            }
        }

        private void LoadShadersIfNeeded() {
            if (_shadow1DMapShader != null && _shadowCompositeShader != null) return;

            try {
                _shadow1DMapShader = ModContent.Request<Effect>(
                    CWRConstant.Effects + "Shadow1DMap", AssetRequestMode.ImmediateLoad).Value;
                _shadowCompositeShader = ModContent.Request<Effect>(
                    CWRConstant.Effects + "ShadowComposite", AssetRequestMode.ImmediateLoad).Value;
            }
            catch (Exception e) {
                Enabled = false;
                CWRMod.Instance?.Logger.Error("阴影着色器加载失败: " + e.Message);
            }
        }

        private static void HandleResolutionChanged(Vector2 newSize) {
            _lightAccumTarget?.Dispose();
            _lightAccumTarget = null;
        }

        private static void DisposeResources() {
            _occlusionMap?.Dispose();
            _occlusionMap = null;
            _occlusionData = null;
            _shadow1DTarget?.Dispose();
            _shadow1DTarget = null;
            _lightAccumTarget?.Dispose();
            _lightAccumTarget = null;
            _pixelTexture?.Dispose();
            _pixelTexture = null;
        }
    }
}
