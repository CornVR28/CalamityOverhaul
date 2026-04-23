using CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.GlitchWraith;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// 屏幕中央目标锁定框渲染
    /// <br/>在骇入模式锁定NPC时，围绕目标绘制赛博风准星框、十字线、旋转环、文字标签
    /// </summary>
    internal static class HackTargetFrame
    {
        public static void Draw(SpriteBatch sb, float timer) {
            float camProg = HackTime.CameraProgress;
            if (camProg < 0.01f) return;

            Texture2D px = CWRAsset.Placeholder_White?.Value;
            if (px == null) return;

            float alpha = HackTime.Intensity * camProg;
            Vector2 center = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);

            float baseHalfW;
            float baseHalfH;
            string targetName = "";
            string hpStr = null;
            Color hpColor = default;

            int selIdx = HackTime.SelectedTargetIndex;
            if (selIdx >= 0 && selIdx < Main.maxNPCs) {
                NPC npc = Main.npc[selIdx];
                if (!npc.active) return;

                baseHalfW = Math.Max(npc.width, 32) * 0.6f + 28f;
                baseHalfH = Math.Max(npc.height, 32) * 0.6f + 28f;
                targetName = npc.FullName;
                if (npc.lifeMax > 0) {
                    float hpPct = (float)npc.life / npc.lifeMax;
                    hpStr = HackTime.HpFormat.Format((int)(hpPct * 100));
                    hpColor = hpPct > 0.5f ? HackTheme.AccentAlt
                        : hpPct > 0.25f ? HackTheme.Uploading : HackTheme.Danger;
                }
            }
            else if (HackTime.CurrentScanTarget is TileScannable tileScan && tileScan.IsValid) {
                //物块扫描目标的锁定框
                Vector2 wc = tileScan.WorldCenter;
                int tx = (int)(wc.X / 16f);
                int ty = (int)(wc.Y / 16f);
                Rectangle bounds = TileScannable.GetTileWorldBounds(tx, ty);
                baseHalfW = Math.Max(bounds.Width, 32) * 0.6f + 28f;
                baseHalfH = Math.Max(bounds.Height, 32) * 0.6f + 28f;

                //物块名称和分类
                Tile tile = Main.tile[tx, ty];
                if (tile.HasTile) {
                    int tileType = tile.TileType;
                    targetName = TileScannable.GetTileName(tx, ty, tileType);
                    hpStr = TileScannable.GetTileClass(tileType);
                    hpColor = TileScannable.GetTileClassColor(tileType);
                }
            }
            else if (HackTime.CurrentScanTarget is IHackableTurret turret && turret.IsValid) {
                //炮台锁定框：尺寸取底座贴图实际宽高
                var actor = turret.AsActor;
                baseHalfW = Math.Max(actor.Width, 32) * 0.45f + 24f;
                baseHalfH = Math.Max(actor.Height, 32) * 0.45f + 24f;
                targetName = GetTurretDisplayName(turret);
                if (turret.IsCircuitDisabled) {
                    int seconds = turret.CircuitDisabledFrames / 60 + 1;
                    hpStr = HackTime.TurretScanCircuit.Value + ": " + seconds + "s";
                    hpColor = HackTheme.Uploading;
                }
                else {
                    hpStr = HackTime.TurretScanCircuitOnline.Value;
                    hpColor = HackTheme.AccentAlt;
                }
            }
            else if (HackTime.CurrentScanTarget is IHackableSignalTower signalTower && signalTower.IsValid) {
                //信号塔锁定框：以Actor贴图宽高收缩，避免庞大矩形框吃屏
                var actor = signalTower.AsActor;
                baseHalfW = Math.Max(actor.Width, 32) * 0.40f + 22f;
                baseHalfH = Math.Max(actor.Height, 32) * 0.32f + 22f;
                targetName = HackTime.SignalTowerScanName.Value;
                hpStr = HackTime.SignalTowerScanStatusOnline.Value;
                hpColor = HackTheme.AccentAlt;
            }
            else if (HackTime.CurrentScanTarget is GlitchWraithActor wraith && wraith.Active) {
                //灵异目标锁定框：尺寸取Actor贴图宽高，外放一点以贴合身形
                baseHalfW = Math.Max(wraith.Width, 32) * 0.6f + 30f;
                baseHalfH = Math.Max(wraith.Height, 32) * 0.6f + 30f;
                targetName = HackTime.WraithScanNameValue.Value;
                hpStr = HackTime.WraithScanIntegrityValue.Value;
                hpColor = HackTheme.Contagion;
            }
            else {
                return;
            }

            float ease = EaseOutCubic(camProg);
            float expand = 1f + (1f - ease) * 0.8f;
            float halfW = baseHalfW * expand;
            float halfH = baseHalfH * expand;

            int armLen = (int)(24f * ease);
            if (armLen < 2) return;
            Color frameColor = HackTheme.Accent * (alpha * 0.75f);
            Color dimColor = HackTheme.Accent * (alpha * 0.25f);

            //四角方括号
            DrawFrameBracket(sb, px, center, -halfW, -halfH, armLen, frameColor);
            DrawFrameBracket(sb, px, center, halfW, -halfH, armLen, frameColor);
            DrawFrameBracket(sb, px, center, -halfW, halfH, armLen, frameColor);
            DrawFrameBracket(sb, px, center, halfW, halfH, armLen, frameColor);

            //四角辉光
            Texture2D glow = CWRAsset.SoftGlow?.Value;
            if (glow != null && ease > 0.3f) {
                Color cg = HackTheme.Accent * (alpha * 0.08f * ease);
                cg.A = 0;
                Vector2[] corners = {
                    center + new Vector2(-halfW, -halfH),
                    center + new Vector2(halfW, -halfH),
                    center + new Vector2(-halfW, halfH),
                    center + new Vector2(halfW, halfH)
                };
                foreach (var c in corners)
                    sb.Draw(glow, c, null, cg, 0, glow.Size() / 2, 0.1f, SpriteEffects.None, 0);
            }

            //边缘刻度标记
            if (ease > 0.5f) {
                float tickAlpha = (ease - 0.5f) * 2f * alpha * 0.35f;
                Color tickColor = HackTheme.Border * tickAlpha;
                int tickCount = 6;
                for (int i = 1; i < tickCount; i++) {
                    float t = (float)i / tickCount;
                    float tx = MathHelper.Lerp(center.X - halfW, center.X + halfW, t);
                    float ty = MathHelper.Lerp(center.Y - halfH, center.Y + halfH, t);
                    sb.Draw(px, new Rectangle((int)tx, (int)(center.Y - halfH), 1, 5),
                        new Rectangle(0, 0, 1, 1), tickColor);
                    sb.Draw(px, new Rectangle((int)tx, (int)(center.Y + halfH - 5), 1, 5),
                        new Rectangle(0, 0, 1, 1), tickColor);
                    sb.Draw(px, new Rectangle((int)(center.X - halfW), (int)ty, 5, 1),
                        new Rectangle(0, 0, 1, 1), tickColor);
                    sb.Draw(px, new Rectangle((int)(center.X + halfW - 5), (int)ty, 5, 1),
                        new Rectangle(0, 0, 1, 1), tickColor);
                }
            }

            //中心十字
            float crossLen = 8f * ease;
            sb.Draw(px, new Rectangle((int)(center.X - crossLen), (int)center.Y, (int)(crossLen * 2), 1),
                new Rectangle(0, 0, 1, 1), dimColor);
            sb.Draw(px, new Rectangle((int)center.X, (int)(center.Y - crossLen), 1, (int)(crossLen * 2)),
                new Rectangle(0, 0, 1, 1), dimColor);

            //旋转环指示器（用4条短斜线模拟）
            if (ease > 0.6f) {
                float ringAlpha = (ease - 0.6f) / 0.4f * alpha * 0.2f;
                float rot = timer * 0.5f;
                float ringR = Math.Max(halfW, halfH) + 12f;
                Color ringCol = HackTheme.Accent * ringAlpha;
                for (int a = 0; a < 4; a++) {
                    float angle = rot + a * MathHelper.PiOver2;
                    Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
                    Vector2 p1 = center + dir * (ringR - 6);
                    Vector2 p2 = center + dir * (ringR + 6);
                    DrawLine(sb, px, p1, p2, 1.5f, ringCol);
                }
            }

            //文字标签
            if (ease > 0.4f) {
                float labelAlpha = (ease - 0.4f) / 0.6f * alpha;

                string label = HackTime.TargetLocked.Value;
                Vector2 labelSize = FontAssets.MouseText.Value.MeasureString(label) * 0.34f;
                Vector2 labelPos = new(center.X - labelSize.X * 0.5f, center.Y - halfH - 22f);
                Utils.DrawBorderString(sb, label, labelPos, HackTheme.Accent * (labelAlpha * 0.7f), 0.34f);

                if (!string.IsNullOrEmpty(targetName)) {
                    Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(targetName) * 0.36f;
                    Vector2 namePos = new(center.X - nameSize.X * 0.5f, center.Y + halfH + 8f);
                    Utils.DrawBorderString(sb, targetName, namePos, HackTheme.TextBright * (labelAlpha * 0.6f), 0.36f);
                }

                //状态信息（NPC为生命值百分比，物块为分类）
                if (hpStr != null) {
                    Vector2 hpSize = FontAssets.MouseText.Value.MeasureString(hpStr) * 0.26f;
                    Vector2 hpPos = new(center.X - hpSize.X * 0.5f, center.Y + halfH + 30f);
                    Utils.DrawBorderString(sb, hpStr, hpPos, hpColor * (labelAlpha * 0.45f), 0.26f);
                }
            }

            //水平扫描线
            float scanT = timer * 0.6f % 1f;
            float scanY = center.Y - halfH + scanT * halfH * 2;
            float scanFade = 1f - Math.Abs(scanT - 0.5f) * 2f;
            sb.Draw(px, new Rectangle((int)(center.X - halfW), (int)scanY, (int)(halfW * 2), 1),
                new Rectangle(0, 0, 1, 1), HackTheme.Accent * (alpha * 0.14f * scanFade));
            //扫描线辉光
            if (glow != null) {
                Color sg = HackTheme.Accent * (alpha * 0.04f * scanFade);
                sg.A = 0;
                sb.Draw(glow, new Vector2(center.X, scanY), null, sg, 0,
                    glow.Size() / 2, new Vector2(halfW / 20f, 0.02f), SpriteEffects.None, 0);
            }
        }

        private static void DrawFrameBracket(SpriteBatch sb, Texture2D px,
            Vector2 center, float ox, float oy, int armLen, Color color) {
            int cx = (int)(center.X + ox);
            int cy = (int)(center.Y + oy);
            int dirH = ox < 0 ? 1 : -1;
            int dirV = oy < 0 ? 1 : -1;

            int hx = dirH > 0 ? cx : cx - armLen;
            sb.Draw(px, new Rectangle(hx, cy, armLen, 2), new Rectangle(0, 0, 1, 1), color);

            int vy = dirV > 0 ? cy : cy - armLen;
            sb.Draw(px, new Rectangle(cx, vy, 2, armLen), new Rectangle(0, 0, 1, 1), color);
        }

        private static void DrawLine(SpriteBatch sb, Texture2D px, Vector2 start, Vector2 end, float thickness, Color color) {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 1f) return;
            sb.Draw(px, start, new Rectangle(0, 0, 1, 1), color, diff.ToRotation(),
                Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        private static float EaseOutCubic(float t) {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        /// <summary>返回炮台锁定框上的目标名，按具体炮台类型选用对应本地化</summary>
        private static string GetTurretDisplayName(IHackableTurret turret) {
            return turret switch {
                ADV.Scenarios.VoidColonys.Architectures.LaserCannons.LaserCannonTurretActor
                    => HackTime.TurretScanLaserName.Value,
                ADV.Scenarios.VoidColonys.Architectures.GatlinTurrets.GatlinTurretActor
                    => HackTime.TurretScanGatlinName.Value,
                _ => HackTime.TurretScanName.Value,
            };
        }
    }
}
