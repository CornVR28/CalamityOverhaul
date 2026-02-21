using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Scenarios.Draedons.AcheronProtocols.Machines.DropPodScens
{
    /// <summary>
    /// 空降仓演出屏幕叠加效果——仅负责绘制屏幕空间的速度线等叠加特效，
    /// 空降仓主体、尾焰、灼烧光效等已移至 <see cref="DropPodActor"/> 在世界坐标中绘制
    /// </summary>
    internal class DropPodDrawSystem : ModSystem
    {
        //由DropPodActor同步过来的数据
        private static int dropTimer;
        private static float reentryHeat;

        /// <summary>
        /// 由 <see cref="DropPodActor"/> 每帧调用，同步坠落计时和灼烧强度
        /// </summary>
        internal static void SyncDropTimer(int timer, float heat) {
            dropTimer = timer;
            reentryHeat = heat;
        }

        public override void PostUpdateEverything() {
            if (!DropPodWorld.Active) {
                dropTimer = 0;
                reentryHeat = 0f;
            }
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch) {
            if (!DropPodWorld.Active) return;
            if (dropTimer <= 0) return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;
            if (!player.TryGetOverride<DropPodPlayer>(out var dpPlayer) || dpPlayer == null || !dpPlayer.DropPodActive) return;

            //绘制前景速度线（屏幕空间叠加）
            DrawSpeedLines(spriteBatch);
        }

        /// <summary>
        /// 速度线——模拟极速下坠时的速度感，从中心向外辐射
        /// </summary>
        private static void DrawSpeedLines(SpriteBatch sb) {
            Texture2D px = VaultAsset.placeholder2.Value;
            float speedAlpha = MathHelper.Clamp(dropTimer / 120f, 0f, 0.6f);

            int lineCount = (int)(20 + reentryHeat * 30);
            int seed = dropTimer / 3;
            for (int i = 0; i < lineCount; i++) {
                int lineSeed = seed * 13 + i * 7919;
                float hash1 = MathF.Abs(MathF.Sin(lineSeed * 0.127f));
                float hash2 = MathF.Abs(MathF.Sin(lineSeed * 0.283f));
                float hash3 = MathF.Abs(MathF.Sin(lineSeed * 0.419f));

                float angle = hash1 * MathHelper.TwoPi;
                float dist = 80f + hash2 * (Main.screenWidth * 0.4f);
                float lineLength = 20f + hash3 * 60f;
                float lineWidth = 1f + hash3 * 1.5f;

                Vector2 center = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
                Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                Vector2 lineStart = center + dir * dist;
                Vector2 lineEnd = lineStart + dir * lineLength;

                float lineAlpha = speedAlpha * (0.3f + hash2 * 0.5f);
                Color lineColor = Color.Lerp(
                    new Color(200, 220, 255),
                    new Color(255, 180, 100),
                    reentryHeat * hash3) * lineAlpha;

                Vector2 diff = lineEnd - lineStart;
                float rot = diff.ToRotation();
                float len = diff.Length();
                sb.Draw(px, lineStart, new Rectangle(0, 0, 1, 1), lineColor, rot,
                    new Vector2(0, 0.5f), new Vector2(len, lineWidth), SpriteEffects.None, 0f);
            }
        }

        public override void Unload() {
            dropTimer = 0;
            reentryHeat = 0f;
        }
    }
}
