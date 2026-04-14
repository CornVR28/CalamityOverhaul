using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityOverhaul.Content.ADV.Extras
{
    public abstract class CGEntry : VaultType<CGEntry>
    {
        private static readonly Dictionary<string, CGEntry> _entries = [];
        public static IReadOnlyCollection<CGEntry> AllEntries => _entries.Values;

        //CG显示名称
        public LocalizedText DisplayName { get; protected set; }
        //排序权重，值越小越靠前
        public virtual int SortOrder => 0;
        //所属分组（用于标签页内的分类筛选，可选）
        public virtual string Group => "";

        //判断CG是否已解锁
        public virtual bool IsUnlocked => false;

        //获取缩略图纹理（网格中显示的小图）
        public virtual Texture2D GetThumbnail() => null;

        //获取全屏查看的大图纹理
        public virtual Texture2D GetFullImage() => GetThumbnail();

        //获取缩略图源矩形（用于图集裁切，默认整张纹理）
        public virtual Rectangle? GetThumbnailSourceRect(Texture2D texture) {
            return texture?.Frame();
        }

        //获取大图源矩形
        public virtual Rectangle? GetFullImageSourceRect(Texture2D texture) {
            return texture?.Frame();
        }

        public static CGEntry GetEntry(string id) => _entries.TryGetValue(id, out var e) ? e : null;

        protected sealed override void VaultRegister() {
            Instances.Add(this);
            _entries.TryAdd(Name, this);
        }

        public override void VaultSetup() {
            DisplayName ??= ModContent.GetInstance<CWRMod>()
                .GetLocalization($"CGEntry.{Name}.DisplayName", () => Name);
        }

        public override void Unload() {
            _entries.Clear();
        }
    }
}
