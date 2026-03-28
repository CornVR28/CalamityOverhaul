using System.Reflection;
using Terraria.ModLoader.IO;

namespace CalamityOverhaul.Content.ADV
{
    /// <summary>
    /// ADV数据模块基类。每个剧情线/功能模块继承此类，定义自己的存档字段。
    /// 模块会被ADVSave自动发现和管理，无需手动注册。
    /// </summary>
    public abstract class ADVDataModule
    {
        /// <summary>
        /// 存档中的唯一标识键。一旦发布后不可更改，否则会导致旧存档数据丢失。
        /// </summary>
        public abstract string SaveKey { get; }

        /// <summary>
        /// 将本模块的所有公共字段保存到TagCompound
        /// </summary>
        public TagCompound SaveFields() {
            TagCompound tag = [];
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields) {
                if (field.FieldType == typeof(bool)) {
                    tag[field.Name] = field.GetValue(this);
                }
                else if (field.FieldType == typeof(int)) {
                    tag[field.Name] = field.GetValue(this);
                }
            }
            return tag;
        }

        /// <summary>
        /// 从TagCompound加载本模块的所有公共字段
        /// </summary>
        public void LoadFields(TagCompound tag) {
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields) {
                if (field.FieldType == typeof(bool)) {
                    if (tag.TryGet(field.Name, out bool value)) {
                        field.SetValue(this, value);
                    }
                }
                else if (field.FieldType == typeof(int)) {
                    if (tag.TryGet(field.Name, out int value)) {
                        field.SetValue(this, value);
                    }
                }
            }
        }
    }
}
