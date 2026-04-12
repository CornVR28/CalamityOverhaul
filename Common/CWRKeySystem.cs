using Terraria.ModLoader;

namespace CalamityOverhaul.Common
{
    internal class CWRKeySystem : ICWRLoader
    {
        public static ModKeybind HeavenfallLongbowSkillKey { get; private set; }
        public static ModKeybind InfinitePickSkillKey { get; private set; }
        public static ModKeybind Murasama_TriggerKey { get; private set; }
        public static ModKeybind Murasama_DownKey { get; private set; }
        public static ModKeybind ADS_Key { get; private set; }
        public static ModKeybind QuestLog_Key { get; private set; }
        public static ModKeybind QuestManager_Key { get; private set; }
        public static ModKeybind Halibut_Domain { get; private set; }
        public static ModKeybind Halibut_Clone { get; private set; }
        public static ModKeybind Halibut_Restart { get; private set; }
        public static ModKeybind Halibut_Superposition { get; private set; }
        public static ModKeybind Halibut_Teleport { get; private set; }
        public static ModKeybind Halibut_UIControl { get; private set; }
        public static ModKeybind Halibut_Skill_L { get; private set; }
        public static ModKeybind Halibut_Skill_R { get; private set; }
        public static ModKeybind WeponSkill_Q { get; private set; }
        public static ModKeybind WeponSkill_R { get; private set; }
        public static ModKeybind AriaofTheCosmos_Q { get; private set; }
        public static ModKeybind AriaofTheCosmos_R { get; private set; }
        public static ModKeybind JusticeUnveiled { get; private set; }
        public static ModKeybind EmblemOfDread_Dash { get; private set; }
        public static ModKeybind EyeOfSingularity_QuantumLeap { get; private set; }
        public static ModKeybind HackTime_Toggle { get; private set; }
        public static ModKeybind CyberBanish_Key { get; private set; }
        public static ModKeybind CyberFreeze_Key { get; private set; }
        public static ModKeybind CyberwareSkill_Key { get; private set; }

        void ICWRLoader.LoadData() {
            Mod mod = CWRMod.Instance;
            ADS_Key = KeybindLoader.RegisterKeybind(mod, "ADS_Key", "Z");
            QuestLog_Key = KeybindLoader.RegisterKeybind(mod, "QuestLog_Key", "L");
            QuestManager_Key = KeybindLoader.RegisterKeybind(mod, "QuestManager_Key", "K");
            HeavenfallLongbowSkillKey = KeybindLoader.RegisterKeybind(mod, "HeavenfallLongbowSkillKey", "Q");
            InfinitePickSkillKey = KeybindLoader.RegisterKeybind(mod, "InfinitePickSkillKey", "C");
            Murasama_TriggerKey = KeybindLoader.RegisterKeybind(mod, "Murasama_TriggerKey", "F");
            Murasama_DownKey = KeybindLoader.RegisterKeybind(mod, "Murasama_DownKey", "X");
            Halibut_Domain = KeybindLoader.RegisterKeybind(mod, "Halibut_Domain", "Q");
            Halibut_Clone = KeybindLoader.RegisterKeybind(mod, "Halibut_Clone", "J");
            Halibut_Restart = KeybindLoader.RegisterKeybind(mod, "Halibut_Restart", "H");
            Halibut_Superposition = KeybindLoader.RegisterKeybind(mod, "Halibut_Superposition", "F");
            Halibut_Teleport = KeybindLoader.RegisterKeybind(mod, "Halibut_Teleport", "G");
            Halibut_UIControl = KeybindLoader.RegisterKeybind(mod, "Halibut_UIControl", "M");
            Halibut_Skill_L = KeybindLoader.RegisterKeybind(mod, "Halibut_Skill_L", "Q");
            Halibut_Skill_R = KeybindLoader.RegisterKeybind(mod, "Halibut_Skill_R", "E");
            WeponSkill_Q = KeybindLoader.RegisterKeybind(mod, "WeponSkill_Q", "Q");
            WeponSkill_R = KeybindLoader.RegisterKeybind(mod, "WeponSkill_R", "R");
            JusticeUnveiled = KeybindLoader.RegisterKeybind(mod, "JusticeUnveiled", "W");
            EmblemOfDread_Dash = KeybindLoader.RegisterKeybind(mod, "EmblemOfDread_Dash", "V");
            EyeOfSingularity_QuantumLeap = KeybindLoader.RegisterKeybind(mod, "EyeOfSingularity_QuantumLeap", "B");
            HackTime_Toggle = KeybindLoader.RegisterKeybind(mod, "HackTime_Toggle", "N");
            CyberBanish_Key = KeybindLoader.RegisterKeybind(mod, "CyberBanish_Key", "Y");
            CyberFreeze_Key = KeybindLoader.RegisterKeybind(mod, "CyberFreeze_Key", "U");
            CyberwareSkill_Key = KeybindLoader.RegisterKeybind(mod, "CyberwareSkill_Key", "V");
        }

        void ICWRLoader.UnLoadData() {
            ADS_Key = null;
            QuestLog_Key = null;
            QuestManager_Key = null;
            HeavenfallLongbowSkillKey = null;
            InfinitePickSkillKey = null;
            Murasama_TriggerKey = null;
            Murasama_DownKey = null;
            Halibut_Domain = null;
            Halibut_Clone = null;
            Halibut_Restart = null;
            Halibut_Superposition = null;
            Halibut_Teleport = null;
            Halibut_UIControl = null;
            Halibut_Skill_L = null;
            Halibut_Skill_R = null;
            WeponSkill_Q = null;
            WeponSkill_R = null;
            JusticeUnveiled = null;
            EmblemOfDread_Dash = null;
            EyeOfSingularity_QuantumLeap = null;
            HackTime_Toggle = null;
            CyberBanish_Key = null;
            CyberFreeze_Key = null;
            CyberwareSkill_Key = null;
        }
    }
}
