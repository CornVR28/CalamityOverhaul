using CalamityOverhaul.Content.HackTimes.Targets;
using System;
using Terraria;

namespace CalamityOverhaul.Content.HackTimes
{
    /// <summary>
    /// NPC 扫描数据实现
    /// <br/>同时承担 <see cref="IHackTarget"/> 抽象，把 NPC 的"被骇入"行为下沉到本类
    /// </summary>
    internal class NpcScannable : IHackTarget
    {
        public int NpcIndex { get; }

        public NpcScannable(int npcIndex) {
            NpcIndex = npcIndex;
        }

        #region IScannable

        public Vector2 WorldCenter {
            get {
                if (NpcIndex < 0 || NpcIndex >= Main.maxNPCs) return Vector2.Zero;
                return Main.npc[NpcIndex].Center;
            }
        }

        public bool IsValid {
            get {
                if (NpcIndex < 0 || NpcIndex >= Main.maxNPCs) return false;
                return Main.npc[NpcIndex].active;
            }
        }

        public bool IsHackable => true;

        public int ScanRowCount => 6;

        public void BuildScanData(string[] labels, string[] values, Color[] colors) {
            if (NpcIndex < 0 || NpcIndex >= Main.maxNPCs) return;
            NPC npc = Main.npc[NpcIndex];
            if (!npc.active) return;

            //TYPE
            labels[0] = HackTime.TypeLabel.Value;
            if (npc.boss) {
                values[0] = HackTime.BossClass.Value;
                colors[0] = HackTheme.Danger;
            }
            else if (npc.lifeMax > 5000) {
                values[0] = HackTime.EliteUnit.Value;
                colors[0] = HackTheme.Uploading;
            }
            else {
                values[0] = HackTime.HostileEntity.Value;
                colors[0] = HackTheme.TextBright;
            }

            //THREAT
            int threatScore = (int)(npc.damage * 0.5f + npc.lifeMax * 0.01f + npc.defense);
            labels[1] = HackTime.ThreatLabel.Value;
            if (threatScore > 500) {
                values[1] = HackTime.ThreatExtreme.Value;
                colors[1] = HackTheme.Danger;
            }
            else if (threatScore > 200) {
                values[1] = HackTime.ThreatHigh.Value;
                colors[1] = HackTheme.Uploading;
            }
            else if (threatScore > 80) {
                values[1] = HackTime.ThreatModerate.Value;
                colors[1] = HackTheme.AccentAlt;
            }
            else {
                values[1] = HackTime.ThreatLow.Value;
                colors[1] = HackTheme.Accent;
            }

            //HP
            labels[2] = "HP";
            values[2] = $"{npc.life:N0} / {npc.lifeMax:N0}";
            float hpPct = (float)npc.life / Math.Max(npc.lifeMax, 1);
            colors[2] = hpPct > 0.5f ? HackTheme.Accent
                : hpPct > 0.25f ? HackTheme.Uploading : HackTheme.Danger;

            //DEF
            labels[3] = HackTime.DefLabel.Value;
            values[3] = $"{npc.defense}";
            colors[3] = HackTheme.TextBright;

            //DMG
            labels[4] = HackTime.DmgLabel.Value;
            values[4] = $"{npc.damage}";
            colors[4] = HackTheme.TextBright;

            //KB.RES
            labels[5] = HackTime.KbResLabel.Value;
            values[5] = $"{npc.knockBackResist:F2}";
            colors[5] = npc.knockBackResist >= 0.9f ? HackTheme.Danger
                : npc.knockBackResist >= 0.5f ? HackTheme.Uploading : HackTheme.TextBright;
        }

        #endregion

        #region IHackTarget

        public HackTargetType TargetType => HackTargetType.Get<NpcTargetType>();

        public Vector2 LockFrameHalfSize {
            get {
                if (!IsValid) return Vector2.Zero;
                NPC npc = Main.npc[NpcIndex];
                return new Vector2(
                    Math.Max(npc.width, 32) * 0.6f + 28f,
                    Math.Max(npc.height, 32) * 0.6f + 28f);
            }
        }

        public string LockFrameTitle => IsValid ? Main.npc[NpcIndex].FullName : string.Empty;

        public bool TryGetLockFrameStatus(out string text, out Color color) {
            text = null;
            color = default;
            if (!IsValid) return false;
            NPC npc = Main.npc[NpcIndex];
            if (npc.lifeMax <= 0) return false;

            float hpPct = (float)npc.life / npc.lifeMax;
            text = HackTime.HpFormat.Format((int)(hpPct * 100));
            color = hpPct > 0.5f ? HackTheme.AccentAlt
                : hpPct > 0.25f ? HackTheme.Uploading : HackTheme.Danger;
            return true;
        }

        public bool ApplyHack(QuickHackDef hack, Player caster) {
            //NPC 协议走效果追踪器，由其管理 OnApply→OnTick→OnRemove 生命周期
            int casterIndex = caster?.whoAmI ?? Main.myPlayer;
            return HackEffectTracker.ApplyNpcEffect(hack, NpcIndex, casterIndex) != null;
        }

        public bool TargetEquals(IHackTarget other) {
            return other is NpcScannable n && n.NpcIndex == NpcIndex;
        }

        #endregion
    }
}
