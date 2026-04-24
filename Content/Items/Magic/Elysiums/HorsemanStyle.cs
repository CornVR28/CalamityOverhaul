using Terraria.ID;

namespace CalamityOverhaul.Content.Items.Magic.Elysiums
{
    internal sealed class HorsemanStyle
    {
        public required string Name { get; init; }
        public required Color PrimaryColor { get; init; }
        public required Color SecondaryColor { get; init; }
        public required Color TextColor { get; init; }
        public required int DustType { get; init; }
        public required float OrbitRadiusX { get; init; }
        public required float OrbitRadiusY { get; init; }
        public required float AngularSpeed { get; init; }
        public required float EntryHeight { get; init; }
        public required float EntryRadius { get; init; }
        public required float IntroDuration { get; init; }
        public required float ScaleMin { get; init; }
        public required float ScaleMax { get; init; }
        public required float GlowScale { get; init; }
        public required float TrailOpacity { get; init; }
    }

    internal static class HorsemanCatalog
    {
        private static readonly HorsemanStyle[] Styles = [
            new() {
                Name = "瘟疫",
                PrimaryColor = new Color(150, 235, 110),
                SecondaryColor = new Color(75, 145, 55),
                TextColor = new Color(188, 255, 138),
                DustType = DustID.CorruptGibs,
                OrbitRadiusX = 224f,
                OrbitRadiusY = 124f,
                AngularSpeed = 0.92f,
                EntryHeight = 360f,
                EntryRadius = 290f,
                IntroDuration = 34f,
                ScaleMin = 0.86f,
                ScaleMax = 1.08f,
                GlowScale = 1.25f,
                TrailOpacity = 0.20f,
            },
            new() {
                Name = "战争",
                PrimaryColor = new Color(255, 105, 88),
                SecondaryColor = new Color(255, 178, 118),
                TextColor = new Color(255, 155, 125),
                DustType = DustID.Torch,
                OrbitRadiusX = 252f,
                OrbitRadiusY = 106f,
                AngularSpeed = 1.38f,
                EntryHeight = 420f,
                EntryRadius = 340f,
                IntroDuration = 28f,
                ScaleMin = 0.94f,
                ScaleMax = 1.15f,
                GlowScale = 1.38f,
                TrailOpacity = 0.26f,
            },
            new() {
                Name = "饥荒",
                PrimaryColor = new Color(255, 220, 122),
                SecondaryColor = new Color(171, 128, 62),
                TextColor = new Color(255, 228, 172),
                DustType = DustID.GoldFlame,
                OrbitRadiusX = 196f,
                OrbitRadiusY = 164f,
                AngularSpeed = 0.74f,
                EntryHeight = 300f,
                EntryRadius = 270f,
                IntroDuration = 38f,
                ScaleMin = 0.82f,
                ScaleMax = 1.03f,
                GlowScale = 1.18f,
                TrailOpacity = 0.18f,
            },
            new() {
                Name = "死亡",
                PrimaryColor = new Color(232, 232, 232),
                SecondaryColor = new Color(148, 148, 170),
                TextColor = new Color(245, 245, 255),
                DustType = DustID.Smoke,
                OrbitRadiusX = 284f,
                OrbitRadiusY = 148f,
                AngularSpeed = 0.58f,
                EntryHeight = 470f,
                EntryRadius = 390f,
                IntroDuration = 46f,
                ScaleMin = 0.92f,
                ScaleMax = 1.24f,
                GlowScale = 1.5f,
                TrailOpacity = 0.30f,
            }
        ];

        public static HorsemanStyle Get(int horsemanIndex) {
            if (horsemanIndex < 0 || horsemanIndex >= Styles.Length) {
                return Styles[0];
            }

            return Styles[horsemanIndex];
        }
    }
}