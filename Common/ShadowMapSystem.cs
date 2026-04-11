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
    /// 1D Shadow Map 实时阴影系统<br/>
    /// Pass1: 对每个光源，在Tile空间沿360个方向射线步进遮挡图，生成1D阴影图<br/>
    /// Pass2: 对每个屏幕像素，在像素空间计算到光源的角度和距离，采样1D阴影图判定遮挡<br/>
    /// 多光源Additive累加到光照RT，最终用Multiply混合: 场景色 × (环境光 + 光照)
    /// </summary>
    public class ShadowMapSystem : RenderHandle
    {
        //===着色器===
        private static Effect _shadow1DMapShader;
        private static Effect _shadowCompositeShader;

        //===渲染资源===
        private static Texture2D _occlusionMap;
        private static Color[] _occlusionData;
        private static int _occMapWidth, _occMapHeight;
        private static RenderTarget2D _shadow1DTarget;
        private static RenderTarget2D _lightAccumTarget;
        private static Texture2D _pixelTexture;

        //Multiply混合状态: dst.rgb = src.rgb × dst.rgb
        private static BlendState _multiplyBlend;

        //===配置===
        /// <summary>
        /// 1D阴影图角度分辨率，越高越精细(360~720)
        /// </summary>
        public static int AngleResolution = 512;
        /// <summary>
        /// 全局阴影开关
        /// </summary>
        public static bool Enabled = false;
        /// <summary>
        /// 环境光强度 0=全黑 1=全亮 建议0.05~0.15
        /// </summary>
        public static float AmbientLight = 0.08f;
        /// <summary>
        /// 是否自动收集屏幕内发光Tile
        /// </summary>
        public static bool AutoCollectTileLights = true;
        /// <summary>
        /// 是否自动为玩家添加微弱光源
        /// </summary>
        public static bool AutoPlayerLight = true;
        /// <summary>
        /// 玩家默认光源半径(像素)
        /// </summary>
        public static float PlayerLightRadius = 250f;
        /// <summary>
        /// 玩家默认光源强度
        /// </summary>
        public static float PlayerLightIntensity = 0.7f;
        /// <summary>
        /// 玩家默认光源颜色
        /// </summary>
        public static Color PlayerLightColor = new(220, 210, 180);
        /// <summary>
        /// 火把类光源默认半径(像素)
        /// </summary>
        public static float TorchLightRadius = 350f;
        /// <summary>
        /// 火把类光源默认强度
        /// </summary>
        public static float TorchLightIntensity = 1.0f;
        /// <summary>
        /// 自动光源最大数量
        /// </summary>
        public static int MaxAutoLights = 40;
        /// <summary>
        /// Tile光源扫描间隔(帧)
        /// </summary>
        public static int TileScanInterval = 4;

        //===运行时状态===
        private static readonly List<ShadowLightSource> _lightSources = [];
        private static readonly List<ShadowLightSource> _cachedTileLights = [];
        private static int _tileScanTimer;
        private static int _tileStartX, _tileStartY;

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
            _multiplyBlend?.Dispose();
            _multiplyBlend = null;
        }

        /// <summary>
        /// 注册一个光源，每帧需要重新注册
        /// </summary>
        public static void AddLight(ShadowLightSource light) {
            _lightSources.Add(light);
        }

        /// <summary>
        /// 快速添加光源
        /// </summary>
        public static void AddLight(Vector2 worldPos, float radius, Color color, float intensity = 1f) {
            _lightSources.Add(new ShadowLightSource(worldPos, radius, color, intensity));
        }

        #region 自动光源收集

        private static void CollectAutoLights() {
            if (AutoPlayerLight && Main.LocalPlayer is { active: true, dead: false }) {
                _lightSources.Add(new ShadowLightSource(
                    Main.LocalPlayer.Center, PlayerLightRadius,
                    PlayerLightColor, PlayerLightIntensity, 0.05f));
            }

            if (!AutoCollectTileLights) return;
            _tileScanTimer++;
            if (_tileScanTimer >= TileScanInterval || _cachedTileLights.Count == 0) {
                _tileScanTimer = 0;
                ScanTileLights();
            }
            _lightSources.AddRange(_cachedTileLights);
        }

        /// <summary>
        /// 扫描屏幕内所有发光Tile
        /// </summary>
        private static void ScanTileLights() {
            _cachedTileLights.Clear();

            int startX = (int)(Main.screenPosition.X / 16f) - 3;
            int startY = (int)(Main.screenPosition.Y / 16f) - 3;
            int endX = startX + Main.screenWidth / 16 + 6;
            int endY = startY + Main.screenHeight / 16 + 6;

            startX = Math.Max(0, startX);
            startY = Math.Max(0, startY);
            endX = Math.Min(Main.maxTilesX - 1, endX);
            endY = Math.Min(Main.maxTilesY - 1, endY);

            //用于去重: 记录已添加光源的Tile位置(量化到2x2格)
            HashSet<long> addedPositions = [];

            for (int x = startX; x <= endX; x++) {
                for (int y = startY; y <= endY; y++) {
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile) continue;

                    int type = tile.TileType;
                    if (!Main.tileLighted[type]) continue;

                    //2x2网格去重: 防止相邻格添加重复光源
                    long key = ((long)(x / 2) << 32) | (uint)(y / 2);
                    if (!addedPositions.Add(key)) continue;

                    float r = 0f, g = 0f, b = 0f;
                    GetTileLightColor(x, y, type, ref r, ref g, ref b);

                    float brightness = r + g + b;
                    if (brightness < 0.15f) continue;

                    float radius = TorchLightRadius * MathHelper.Clamp(brightness / 1.5f, 0.5f, 1.5f);
                    float intensity = TorchLightIntensity * MathHelper.Clamp(brightness / 1.2f, 0.4f, 1.3f);

                    //从光照RGB推算颜色(归一化)
                    float maxC = Math.Max(r, Math.Max(g, b));
                    Color color = new(
                        (int)(r / maxC * 255),
                        (int)(g / maxC * 255),
                        (int)(b / maxC * 255));

                    Vector2 worldPos = new(x * 16f + 8f, y * 16f + 8f);
                    _cachedTileLights.Add(new ShadowLightSource(worldPos, radius, color, intensity, 0.03f));

                    if (_cachedTileLights.Count >= MaxAutoLights) return;
                }
            }
        }

        private static void GetTileLightColor(int i, int j, int type, ref float r, ref float g, ref float b) {
            ModTile modTile = TileLoader.GetTile(type);
            if (modTile != null) {
                modTile.ModifyLight(i, j, ref r, ref g, ref b);
                return;
            }

            //原版常见发光Tile
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
                case TileID.DiscoBall:
                    r = 0.7f; g = 0.7f; b = 0.7f;
                    break;
                default:
                    //用Lighting系统近似获取该格光照
                    Color c = Lighting.GetColor(i, j);
                    r = c.R / 255f;
                    g = c.G / 255f;
                    b = c.B / 255f;
                    break;
            }
        }

        #endregion

        #region 渲染管线

        public override void EndCaptureDraw(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            if (!Enabled || Main.gameMenu) return;

            CollectAutoLights();

            if (_lightSources.Count == 0) {
                //无光源: 全屏用Multiply暗化到环境光水平
                ApplyDarknessOnly(sb, gd, screenSwap);
                return;
            }

            EnsureResources(gd);
            LoadShadersIfNeeded();

            if (_shadow1DMapShader == null || _shadowCompositeShader == null) {
                _lightSources.Clear();
                return;
            }

            //1.CPU生成遮挡图
            UpdateOcclusionMap();

            //2.清空光照累加RT为环境光
            byte ambient = (byte)(MathHelper.Clamp(AmbientLight, 0f, 1f) * 255);
            gd.SetRenderTarget(_lightAccumTarget);
            gd.Clear(new Color(ambient, ambient, ambient));

            //3.逐光源: 生成1D阴影图 → Additive合成到光照RT
            foreach (var light in _lightSources) {
                RenderLightShadow(sb, gd, light);
            }

            //4.Multiply合成: 场景色 × 光照图
            ApplyLightMap(sb, gd, screenSwap);

            _lightSources.Clear();
        }

        /// <summary>
        /// 对单个光源: Pass1生成1D阴影图, Pass2合成光照
        /// </summary>
        private void RenderLightShadow(SpriteBatch sb, GraphicsDevice gd, ShadowLightSource light) {
            //光源在遮挡图中的UV(0~1)
            float lightTileX = light.WorldPosition.X / 16f - _tileStartX;
            float lightTileY = light.WorldPosition.Y / 16f - _tileStartY;
            float lightUVx = lightTileX / _occMapWidth;
            float lightUVy = lightTileY / _occMapHeight;

            float radiusTiles = light.Radius / 16f;

            //===Pass1: 1D阴影图===
            gd.SetRenderTarget(_shadow1DTarget);
            gd.Clear(Color.White);

            _shadow1DMapShader.Parameters["occlusionMap"]?.SetValue(_occlusionMap);
            _shadow1DMapShader.Parameters["lightPosUV"]?.SetValue(new Vector2(lightUVx, lightUVy));
            _shadow1DMapShader.Parameters["mapSize"]?.SetValue(new Vector2(_occMapWidth, _occMapHeight));
            _shadow1DMapShader.Parameters["radiusTiles"]?.SetValue(radiusTiles);

            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            _shadow1DMapShader.CurrentTechnique.Passes[0].Apply();
            sb.Draw(_pixelTexture, new Rectangle(0, 0, AngleResolution, 1), Color.White);
            sb.End();

            //===Pass2: 合成到光照累加RT===
            gd.SetRenderTarget(_lightAccumTarget);

            //光源在屏幕中的像素坐标
            Vector2 lightScreenPos = light.WorldPosition - Main.screenPosition;

            _shadowCompositeShader.Parameters["shadowMapTex"]?.SetValue(_shadow1DTarget);
            _shadowCompositeShader.Parameters["lightScreenPos"]?.SetValue(lightScreenPos);
            _shadowCompositeShader.Parameters["radiusPixels"]?.SetValue(light.Radius);
            _shadowCompositeShader.Parameters["screenSize"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            _shadowCompositeShader.Parameters["lightColor"]?.SetValue(light.LightColor.ToVector3());
            _shadowCompositeShader.Parameters["lightIntensity"]?.SetValue(light.Intensity);
            _shadowCompositeShader.Parameters["softAngle"]?.SetValue(light.SoftShadowAngle);

            sb.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            _shadowCompositeShader.CurrentTechnique.Passes[0].Apply();
            sb.Draw(_pixelTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);
            sb.End();
        }

        /// <summary>
        /// Multiply合成: 场景色 × 光照图
        /// </summary>
        private void ApplyLightMap(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            //备份场景到swap
            gd.SetRenderTarget(screenSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();

            //先将光照图绘制到screenTarget
            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            sb.Draw(_lightAccumTarget, Vector2.Zero, Color.White);
            sb.End();

            //Multiply: 将场景(swap)乘上光照图(已在screenTarget中)
            //BlendState: result = src × dst = swap.rgb × lightMap.rgb
            sb.Begin(SpriteSortMode.Deferred, _multiplyBlend);
            sb.Draw(screenSwap, Vector2.Zero, Color.White);
            sb.End();
        }

        /// <summary>
        /// 无光源时的纯暗化
        /// </summary>
        private void ApplyDarknessOnly(SpriteBatch sb, GraphicsDevice gd, RenderTarget2D screenSwap) {
            _lightSources.Clear();
            EnsureResources(gd);

            byte ambient = (byte)(MathHelper.Clamp(AmbientLight, 0f, 1f) * 255);
            Color ambientColor = new(ambient, ambient, ambient);

            //备份场景到swap
            gd.SetRenderTarget(screenSwap);
            gd.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            sb.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            sb.End();

            //screenTarget填充环境光色
            gd.SetRenderTarget(Main.screenTarget);
            gd.Clear(ambientColor);

            //Multiply: scene × ambient
            sb.Begin(SpriteSortMode.Deferred, _multiplyBlend);
            sb.Draw(screenSwap, Vector2.Zero, Color.White);
            sb.End();
        }

        #endregion

        #region 遮挡图生成

        private void UpdateOcclusionMap() {
            int padding = 10;
            _tileStartX = (int)(Main.screenPosition.X / 16f) - padding;
            _tileStartY = (int)(Main.screenPosition.Y / 16f) - padding;
            int newW = Main.screenWidth / 16 + padding * 2 + 2;
            int newH = Main.screenHeight / 16 + padding * 2 + 2;

            if (_occlusionMap == null || newW != _occMapWidth || newH != _occMapHeight) {
                _occlusionMap?.Dispose();
                _occMapWidth = newW;
                _occMapHeight = newH;
                _occlusionMap = new Texture2D(Main.graphics.GraphicsDevice, _occMapWidth, _occMapHeight);
                _occlusionData = new Color[_occMapWidth * _occMapHeight];
            }

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

        #endregion

        #region 资源管理

        private void EnsureResources(GraphicsDevice gd) {
            if (_shadow1DTarget == null || _shadow1DTarget.IsDisposed
                || _shadow1DTarget.Width != AngleResolution) {
                _shadow1DTarget?.Dispose();
                _shadow1DTarget = new RenderTarget2D(gd, AngleResolution, 1,
                    false, SurfaceFormat.Color, DepthFormat.None);
            }

            if (_lightAccumTarget == null || _lightAccumTarget.IsDisposed
                || _lightAccumTarget.Width != Main.screenWidth || _lightAccumTarget.Height != Main.screenHeight) {
                _lightAccumTarget?.Dispose();
                _lightAccumTarget = new RenderTarget2D(gd, Main.screenWidth, Main.screenHeight,
                    false, SurfaceFormat.Color, DepthFormat.None);
            }

            if (_pixelTexture == null || _pixelTexture.IsDisposed) {
                _pixelTexture = new Texture2D(gd, 1, 1);
                _pixelTexture.SetData([Color.White]);
            }

            if (_multiplyBlend == null || _multiplyBlend.IsDisposed) {
                _multiplyBlend = new BlendState {
                    ColorSourceBlend = Blend.DestinationColor,
                    ColorDestinationBlend = Blend.Zero,
                    AlphaSourceBlend = Blend.DestinationColor,
                    AlphaDestinationBlend = Blend.Zero
                };
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

        #endregion
    }
}
