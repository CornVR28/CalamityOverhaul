using CalamityOverhaul.Common;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.Banish;
using CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces.DomainFreeze;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace CalamityOverhaul.Content.LegendWeapon.SHPCLegend.Cyberspaces
{
    /// <summary>
    /// 赛博空间渲染器。
    /// <br/>在 DrawAfterTiles 时机执行世界层后处理（压暗+去饱和+红染+加法赛博特效），
    /// <br/>位于物块图层之上、玩家和其他实体图层之下，随后叠加边缘光晕环
    /// </summary>
    internal class CyberspaceRender : RenderHandle
    {
        private const int MaxEntities = 32;
        private static readonly Vector4[] entityBuffer = new Vector4[MaxEntities];

        [VaultLoaden(CWRConstant.Masking + "Noise2")]
        private static Asset<Texture2D> noise2;
        [VaultLoaden(CWRConstant.Masking + "SoftGlow")]
        private static Asset<Texture2D> softGlow;

        public override void UpdateBySystem(int index) {
            if (Main.gameMenu) {
                Cyberspace.Reset();
                CyberBanish.Reset();
                CyberDomainFreeze.Reset();
                return;
            }

            Cyberspace.Update();
            CyberBanish.Update();
            CyberDomainFreeze.Update();
        }

        public override void DrawNPCsOverTiles(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            if (Main.gameMenu) {
                return;
            }

            //仅按视觉强度判定，确保Active翻转后收缩动画仍能完整播完
            if (Cyberspace.Intensity < 0.001f) {
                return;
            }

            ApplyFullScreenShader(spriteBatch, graphicsDevice, screenSwap);

            DrawBoundaryShockwaveRing(spriteBatch);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
                DepthStencilState.None, RasterizerState.CullNone, null,
                Main.GameViewMatrix.TransformationMatrix);
            DrawEdgeGlowRing(spriteBatch);
            spriteBatch.End();
        }

        private static void ApplyFullScreenShader(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            Effect shader = EffectLoader.CyberspaceField?.Value;
            Texture2D noiseTex = noise2?.Value;
            if (shader == null || noiseTex == null) return;
            if (screenSwap == null || screenSwap.IsDisposed) return;
            if (Main.screenTarget == null || Main.screenTarget.IsDisposed) return;

            // 将当前屏幕内容复制到交换缓冲
            graphicsDevice.SetRenderTarget(screenSwap);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            spriteBatch.End();

            // 设置着色器参数
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Vector2 screenPixels = Main.ScreenSize.ToVector2();
            Vector2 worldViewSize = screenPixels / zoom;
            Vector2 worldViewOrigin = Main.screenPosition
                + screenPixels * (Vector2.One - Vector2.One / zoom) * 0.5f;

            shader.Parameters["uTime"]?.SetValue(Cyberspace.EffectTime);
            shader.Parameters["radius"]?.SetValue(Cyberspace.Radius);
            shader.Parameters["intensity"]?.SetValue(Cyberspace.Intensity);
            shader.Parameters["expandProgress"]?.SetValue(Cyberspace.ExpandProgress);
            shader.Parameters["dimStrength"]?.SetValue(Cyberspace.DimStrength);
            Vector2 domainCenter = Main.LocalPlayer.Center;
            float effectiveRadius = Cyberspace.Radius * Cyberspace.ExpandProgress;
            shader.Parameters["setPoint"]?.SetValue(domainCenter);
            shader.Parameters["screenPosition"]?.SetValue(worldViewOrigin);
            shader.Parameters["worldViewSize"]?.SetValue(worldViewSize);
            shader.Parameters["gridSize"]?.SetValue(Cyberspace.GridSize);

            // 收集域内实体数据
            int entityCount = CollectEntitiesInDomain(domainCenter, effectiveRadius);
            shader.Parameters["entityCount"]?.SetValue(entityCount);
            if (entityCount > 0) {
                shader.Parameters["entities"]?.SetValue(entityBuffer);
            }

            // 应用着色器并绘制回主屏幕
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            graphicsDevice.Textures[1] = noiseTex;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            shader.CurrentTechnique.Passes[0].Apply();
            spriteBatch.Draw(screenSwap, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        private static void DrawEdgeGlowRing(SpriteBatch spriteBatch) {
            Texture2D glowTex = softGlow?.Value;
            if (glowTex == null || Cyberspace.Intensity < 0.01f) return;

            //逐层绘制边缘光晕（包含收缩中的层）
            for (int layer = 0; layer < Cyberspace.RenderLayerCount; layer++) {
                float expand = Cyberspace.GetLayerExpand(layer);
                if (expand < 0.1f) continue;
                DrawSingleEdgeGlowRing(spriteBatch, glowTex, layer, expand);
            }
        }

        private static void DrawSingleEdgeGlowRing(SpriteBatch spriteBatch, Texture2D glowTex,
            int layer, float expand) {
            Vector2 center = Main.LocalPlayer.Center;
            float r = Cyberspace.GetLayerRadius(layer) * expand;
            float gs = Cyberspace.GridSize;
            float time = Cyberspace.EffectTime;
            float effectIntensity = Cyberspace.Intensity;

            if (r < gs * 2) return;

            int numSteps = Math.Clamp((int)(MathHelper.TwoPi * r / (gs * 0.6f)), 48, 280);
            float prevSnapX = float.NaN;
            float prevSnapY = float.NaN;
            float screenW = Main.screenWidth;
            float screenH = Main.screenHeight;
            float margin = gs * 4;
            Vector2 glowOrigin = new Vector2(glowTex.Width * 0.5f, glowTex.Height * 0.5f);
            float glowScale = gs * 3.0f / glowTex.Width;

            for (int i = 0; i < numSteps; i++) {
                float angle = i * MathHelper.TwoPi / numSteps;
                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);

                float wx = center.X + cos * (r + gs * 1.2f);
                float wy = center.Y + sin * (r + gs * 1.2f);

                float relX = wx - center.X;
                float relY = wy - center.Y;
                float snapX = MathF.Floor(relX / gs) * gs + gs * 0.5f;
                float snapY = MathF.Floor(relY / gs) * gs + gs * 0.5f;

                if (snapX == prevSnapX && snapY == prevSnapY) continue;
                prevSnapX = snapX;
                prevSnapY = snapY;

                float cellWorldX = center.X + snapX;
                float cellWorldY = center.Y + snapY;

                float screenX = cellWorldX - Main.screenPosition.X;
                float screenY = cellWorldY - Main.screenPosition.Y;
                if (screenX < -margin || screenX > screenW + margin ||
                    screenY < -margin || screenY > screenH + margin) continue;

                float cellHash = MathF.Abs(MathF.Sin(snapX * 0.137f + snapY * 0.251f));
                float pulse = 0.3f + 0.7f * MathF.Sin(time * 1.8f + cellHash * MathF.PI * 2f);
                pulse = MathF.Max(pulse, 0f);
                //外层亮度递增
                float layerMult = 1f + layer * 0.25f;
                float alpha = pulse * effectIntensity * 0.4f * layerMult;

                Color glowColor = GetLayerGlowColor(layer, alpha);

                spriteBatch.Draw(glowTex, new Vector2(screenX, screenY), null, glowColor,
                    0f, glowOrigin, glowScale, SpriteEffects.None, 0f);
            }
        }

        private static Color GetLayerGlowColor(int layer, float alpha) {
            return layer switch {
                0 => new Color(0.80f * alpha, 0.05f * alpha, 0.04f * alpha, 0f),
                1 => new Color(0.90f * alpha, 0.10f * alpha, 0.06f * alpha, 0f),
                _ => new Color(1.0f * alpha, 0.18f * alpha, 0.08f * alpha, 0f),
            };
        }

        /// <summary>
        /// 在每层领域边界绘制常驻边界环——使用专用CyberBoundaryRing着色器
        /// <br/>逐层绘制，呼吸脉动带层间时间偏移，颜色随层数递升
        /// </summary>
        private static void DrawBoundaryShockwaveRing(SpriteBatch spriteBatch) {
            Effect shader = EffectLoader.CyberBoundaryRing?.Value;
            if (shader == null) return;
            if (CWRAsset.Placeholder_White?.Value == null) return;
            if (CWRAsset.Extra_193?.Value == null) return;
            if (Cyberspace.Intensity < 0.02f) return;

            Texture2D canvas = CWRAsset.Placeholder_White.Value;
            Texture2D noise = CWRAsset.Extra_193.Value;
            Vector2 center = Main.LocalPlayer.Center;
            Vector2 drawPos = center - Main.screenPosition;

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,
                null, Main.GameViewMatrix.TransformationMatrix);
            Main.graphics.GraphicsDevice.Textures[1] = noise;
            Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

            for (int layer = 0; layer < Cyberspace.RenderLayerCount; layer++) {
                float expand = Cyberspace.GetLayerExpand(layer);
                if (expand < 0.3f) continue;

                float time = Cyberspace.EffectTime * 0.75f;
                float layerRadius = Cyberspace.GetLayerRadius(layer);
                float effectiveRadius = layerRadius * expand;
                float quadHalf = effectiveRadius * 1.1f;
                float ringPos = effectiveRadius / quadHalf;
                float thickness = 0.15f + 0.012f * MathF.Sin(time * 0.8f + 1.2f + layer * 2.1f);

                //每层用不同的时间偏移，避免同步呼吸
                shader.Parameters["uTime"]?.SetValue(time + layer * 7.3f);
                shader.Parameters["ringProgress"]?.SetValue(ringPos);
                shader.Parameters["ringThickness"]?.SetValue(thickness);
                shader.Parameters["fadeAlpha"]?.SetValue(Cyberspace.Intensity);
                shader.CurrentTechnique.Passes[0].Apply();

                float drawDiameter = quadHalf * 2f * 0.8f;
                Color ringTint = GetLayerRingTint(layer);
                spriteBatch.Draw(canvas, drawPos, null, ringTint,
                    0f, canvas.Size() * 0.5f, new Vector2(drawDiameter, drawDiameter),
                    SpriteEffects.None, 0f);
            }

            spriteBatch.End();
        }

        private static Color GetLayerRingTint(int layer) {
            return layer switch {
                0 => new Color(1f, 0.80f, 0.65f),
                1 => new Color(1f, 0.65f, 0.50f),
                _ => new Color(1f, 0.50f, 0.35f),
            };
        }

        /// <summary>
        /// 收集域内活跃NPC，将位置和大小写入 entityBuffer
        /// 返回收集到的实体数量
        /// </summary>
        private static int CollectEntitiesInDomain(Vector2 domainCenter, float effectiveRadius) {
            int count = 0;
            float radiusSq = effectiveRadius * effectiveRadius;

            for (int i = 0; i < Main.maxNPCs && count < MaxEntities; i++) {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage) continue;

                Vector2 npcCenter = npc.Center;
                float dx = npcCenter.X - domainCenter.X;
                float dy = npcCenter.Y - domainCenter.Y;
                if (dx * dx + dy * dy > radiusSq) continue;

                float ringRadius = Math.Max(npc.width, npc.height) * 0.8f + 10f;
                float seed = (i * 0.137f) % 1f;
                entityBuffer[count] = new Vector4(npcCenter.X, npcCenter.Y, ringRadius, seed);
                count++;
            }

            // 清零未使用的槽位
            for (int i = count; i < MaxEntities; i++) {
                entityBuffer[i] = Vector4.Zero;
            }

            return count;
        }
    }
}
