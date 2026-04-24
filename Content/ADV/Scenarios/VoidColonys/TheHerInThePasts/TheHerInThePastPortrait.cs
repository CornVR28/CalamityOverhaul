using CalamityOverhaul.Common;
using CalamityOverhaul.Content.ADV.Scenarios.Shepel;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace CalamityOverhaul.Content.ADV.Scenarios.VoidColonys.TheHerInThePasts
{
    /// <summary>
    /// 过去的她——双角色立绘，统一承载在硫磺火对话框上
    /// 通过Role切换显示硫火女巫雕像或SHPC立绘，女巫带着色进度与像素剥落
    /// SHPC带表情切换与故障扭曲效果
    /// </summary>
    internal class TheHerInThePastPortrait : FullBodyPortraitBase
    {
        public enum Role { Witch, SHPC }

        public override string PortraitKey => "TheHerInThePast";

        protected override float FadeInDuration => 45f;
        protected override float FadeOutDuration => 60f;

        public Role CurrentRole { get; private set; } = Role.Witch;
        public ShepelFullBodyPortrait.Face SHPCFace { get; private set; } = ShepelFullBodyPortrait.Face.Blank;
        public bool smile { get; set; } = false;
        public float Coloration => colorationT;
        public bool IsDissolving => dissolving;

        //女巫着色进度0到1
        private float colorationT;
        //像素剥落进度
        private float dissolveT;
        private bool dissolving;
        private int localTimer;

        //SHPC故障扭曲
        private float glitchTimer;
        private float glitchIntensity;
        private float glitchTimeAccum;

        public void SwitchTo(Role role) => CurrentRole = role;

        public void SetColoration(float value) {
            colorationT = MathHelper.Clamp(value, 0f, 1f);
        }

        public void SetSHPCFace(ShepelFullBodyPortrait.Face face) {
            SHPCFace = face;
        }

        /// <summary>
        /// 触发SHPC故障扭曲
        /// </summary>
        public void TriggerGlitch(float intensity = 0.6f, float duration = 0.7f) {
            glitchIntensity = MathHelper.Clamp(intensity, 0f, 1f);
            glitchTimer = MathF.Max(glitchTimer, duration * 60f);
        }

        /// <summary>
        /// 开始女巫像素剥落消散
        /// </summary>
        public void StartPixelDissolve() {
            if (dissolving) return;
            dissolving = true;
            dissolveT = 0f;
            BlockDialogueClose = true;
            EnterCustomPhase();
        }

        protected override void OnInitialize() {
            scale = 1.2f;
            colorationT = 0f;
            dissolveT = 0f;
            dissolving = false;
            localTimer = 0;
            glitchTimer = 0f;
            glitchIntensity = 0f;
            glitchTimeAccum = 0f;
        }

        protected override void OnUpdate() {
            localTimer++;
            if (glitchTimer > 0f) {
                glitchTimer--;
                glitchTimeAccum += 0.016f;
                if (glitchTimer <= 0f) {
                    glitchTimer = 0f;
                    glitchIntensity = 0f;
                }
            }
        }

        protected override void OnCustomPhaseUpdate() {
            localTimer++;
            dissolveT = MathF.Min(1f, dissolveT + 1f / 180f);
            CurrentFade = MathHelper.Clamp(1f - dissolveT, 0f, 1f);
            if (dissolveT >= 1f) {
                BlockDialogueClose = false;
                ForceDeactivate();
            }
        }

        protected override void OnDraw(SpriteBatch spriteBatch, float alpha) {
            if (CurrentRole == Role.Witch) {
                DrawWitch(spriteBatch, alpha);
            }
            else {
                DrawSHPC(spriteBatch, alpha);
            }
        }

        private void DrawWitch(SpriteBatch spriteBatch, float alpha) {
            float scale2 = scale * 1.16f;
            Texture2D portrait = ADVAsset.Lain;
            if (portrait == null || portrait.IsDisposed) return;

            Rectangle rectangle = new Rectangle(0, 0, portrait.Width, portrait.Height);
            position = OwnerDialogue.GetPanelRect().Top() + new Vector2(-160, -portrait.Height + 90) * scale2;

            Color stone = Color.White;
            Color vivid = Color.White;
            Color blended = Color.Lerp(stone, vivid, colorationT) * alpha;

            spriteBatch.Draw(portrait, position, rectangle, blended, rotation, Vector2.Zero, scale2, SpriteEffects.None, 0f);

            if (smile) {
                portrait = ADVAsset.Lain_smile;
                position.X += 38 * scale2;
                spriteBatch.Draw(portrait, position, rectangle, blended, rotation, Vector2.Zero, scale2, SpriteEffects.None, 0f);
            }
        }

        private void DrawSHPC(SpriteBatch spriteBatch, float alpha) {
            Texture2D portrait = ADVAsset.Shepel;
            if (portrait == null || portrait.IsDisposed) return;

            Rectangle rectangle = new Rectangle(0, 0, portrait.Width, portrait.Height);
            position = OwnerDialogue.GetPanelRect().Top() + new Vector2(-160, -portrait.Height + 100) * scale;

            Color color = Color.White * alpha;
            bool useGlitch = glitchTimer > 0f && glitchIntensity > 0f && EffectLoader.ShepelGlitch?.Value != null;

            if (useGlitch) {
                Effect effect = EffectLoader.ShepelGlitch.Value;
                effect.Parameters["uTime"]?.SetValue(glitchTimeAccum);
                effect.Parameters["uIntensity"]?.SetValue(glitchIntensity);

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.AnisotropicClamp, DepthStencilState.None,
                    RasterizerState.CullNone, effect, Main.UIScaleMatrix);
            }

            spriteBatch.Draw(portrait, position, rectangle, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            Vector2 facePos = position + new Vector2(18 * scale, 0);

            Texture2D faceTexture = SHPCFace switch {
                ShepelFullBodyPortrait.Face.Blank => ADVAsset.Shepel_Blank,
                ShepelFullBodyPortrait.Face.Happy => ADVAsset.Shepel_Happy,
                ShepelFullBodyPortrait.Face.Pain => ADVAsset.Shepel_Pain,
                ShepelFullBodyPortrait.Face.Sad => ADVAsset.Shepel_Sad,
                ShepelFullBodyPortrait.Face.Serious => ADVAsset.Shepel_Serious,
                ShepelFullBodyPortrait.Face.Shocked => ADVAsset.Shepel_Shocked,
                ShepelFullBodyPortrait.Face.Sleep => ADVAsset.Shepel_Sleep,
                ShepelFullBodyPortrait.Face.Smirk => ADVAsset.Shepel_Smirk,
                _ => null
            };

            if (faceTexture != null) {
                spriteBatch.Draw(faceTexture, facePos, null, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            if (useGlitch) {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.AnisotropicClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Main.UIScaleMatrix);
            }
        }
    }
}
