using CalamityOverhaul.Common;
using InnoVault.Actors;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.Architectures.SignalTowers
{
    /// <summary>
    /// 信号塔红色闪电Actor
    /// 整道闪电完全由SignalTowerLightning.fx在像素着色器里程序生成
    /// Actor本身只负责位置、生命周期与混合模式切换，Position设为画布左上角
    /// 画布在世界像素空间内为BoltWidthPx x BoltHeightPx，底部中点正好锚在信号塔顶
    /// </summary>
    internal class SignalTowerLightningActor : Actor
    {
        /// <summary>闪电画布宽度px</summary>
        public const int BoltWidthPx = 820;
        /// <summary>闪电画布高度px，要远高于信号塔自身以营造从天而降的气势</summary>
        public const int BoltHeightPx = 2600;

        /// <summary>本次闪电生存帧数，Spawn时写入</summary>
        [SyncVar]
        public int InitLifeFrames;
        /// <summary>本次闪电随机种子，Spawn时写入</summary>
        [SyncVar]
        public float InitSeed;

        private int maxLife;
        private int life;

        public override void OnSpawn(params object[] args) {
            DrawLayer = ActorDrawLayer.AfterTiles;
            Width = BoltWidthPx;
            Height = BoltHeightPx;
            //扩展绘制范围到画布外一圈，保证冲击点爆闪外延也不被裁
            DrawExtendMode = Math.Max(BoltWidthPx, BoltHeightPx);
            maxLife = InitLifeFrames > 0 ? InitLifeFrames : 50;
            life = maxLife;
        }

        public override void AI() {
            if (!VoidColony.Active) {
                ActorLoader.KillActor(WhoAmI);
                return;
            }
            Velocity = Vector2.Zero;
            life--;
            if (life <= 0) {
                ActorLoader.KillActor(WhoAmI);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, ref Color drawColor) {
            Effect shader = EffectLoader.SignalTowerLightning?.Value;
            if (shader == null || maxLife <= 0) return false;

            float lifeProgress = 1f - life / (float)maxLife;

            shader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["lifeProgress"]?.SetValue(MathHelper.Clamp(lifeProgress, 0f, 1f));
            shader.Parameters["intensity"]?.SetValue(1f);
            shader.Parameters["seed"]?.SetValue(InitSeed);
            shader.Parameters["texelSize"]?.SetValue(new Vector2(1f / BoltWidthPx, 1f / BoltHeightPx));
            shader.Parameters["aspect"]?.SetValue(BoltWidthPx / (float)BoltHeightPx);

            Vector2 drawPos = Position - Main.screenPosition;

            //切到加色混合+immediate模式以应用shader，叠加在建筑之上不会抹黑背景
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);

            Texture2D px = TextureAssets.MagicPixel.Value;
            Rectangle dest = new((int)drawPos.X, (int)drawPos.Y, BoltWidthPx, BoltHeightPx);
            spriteBatch.Draw(px, dest, Color.White);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
