using System;
using System.Collections.Generic;
using System.Text;

namespace ICE.Utilities.Cosmic_Helper;

public static partial class CosmicHelper
{
    public class CriticalInfo
    {
        public Vector3 RawLocation { get; set; }
        public Vector2 MapInfo { get; set; }
        public uint NpcSelection { get; set; }
    }

    #region Sinus

    private static CriticalInfo AstromagneticStorm1α = new()
    {
        RawLocation = new Vector3(176.24f, 9.40f, 560.07f),
        MapInfo = new Vector2(24.7f, 32.5f),
        NpcSelection = 0,
    };
    private static CriticalInfo AstromagneticStorm1β = new()
    {
        RawLocation = new Vector3(-91.58f, 19.32f, -241.99f),
        MapInfo = new Vector2(19.7f, 16.6f),
        NpcSelection = 1,
    };
    private static CriticalInfo AstromagneticStorm2α = new()
    {
        RawLocation = new Vector3(-72.76f, 51.00f, 768.64f),
        MapInfo = new Vector2(19.9f, 36.9f),
        NpcSelection = 0,
    };
    private static CriticalInfo AstromagneticStorm2β = new()
    {
        RawLocation = new Vector3(-464.50f, 37.89f, -69.89f),
        MapInfo = new Vector2(12.2f, 20.1f),
        NpcSelection = 1,
    };

    private static CriticalInfo MeteorShower1α = new()
    {
        RawLocation = new Vector3(-219.93f, 24.16f, 209.98f),
        MapInfo = new(17.1f, 25.7f),
        NpcSelection = 0,
    };
    private static CriticalInfo MeteorShower1β = new()
    {
        RawLocation = new Vector3(34.86f, 34.38f, -349.75f),
        MapInfo = new(22.2f, 14.5f),
        NpcSelection = 1,
    };
    private static CriticalInfo MeteorShower2α = new()
    {
        RawLocation = new Vector3(845.90f, -58.44f, -390.45f),
        MapInfo = new(38.1f, 13.9f),
        NpcSelection = 0,
    };
    private static CriticalInfo MeteorShower2β = new()
    {
        RawLocation = new Vector3(497.36f, -115.32f, -845.65f),
        MapInfo = new Vector2(31.3f, 4.4f),
        NpcSelection = 1,
    };

    private static CriticalInfo SporingMist1α = new()
    {
        RawLocation = new Vector3(539.43f, 36.38f, 49.89f),
        MapInfo = new Vector2(32.3f, 22.5f),
        NpcSelection = 0,
    };
    private static CriticalInfo SporingMist1β = new()
    {
        RawLocation = new Vector3(654.32f, 52.00f, 100.13f),
        MapInfo = new Vector2(34.6f, 23.5f),
        NpcSelection = 1,
    };
    private static CriticalInfo SporingMist2α = new()
    {
        RawLocation = new Vector3(379.62f, 51.39f, 704.14f),
        MapInfo = new Vector2(29.1f, 35.6f),
        NpcSelection = 0,
    };
    private static CriticalInfo SporingMist2β = new()
    {
        RawLocation = new Vector3(99.66f, 18.11f, -209.80f),
        MapInfo = new Vector2(23.5f, 17.3f),
        NpcSelection = 1,
    };

    #endregion

    #region Phaenna

    // Phaenna
    private static CriticalInfo Thunderstorms1α = new()
    {
        RawLocation = new Vector3(417.57f, 52.00f, -445.41f),
        MapInfo = new Vector2(29.9f, 12.6f),
        NpcSelection = 0,
    };
    private static CriticalInfo Thunderstorms1β = new()
    {
        RawLocation = new Vector3(432.76f, 54.13f, -169.80f),
        MapInfo = new Vector2(30.1f, 18.1f),
        NpcSelection = 1,
    };
    private static CriticalInfo Thunderstorms2α = new()
    {
        RawLocation = new Vector3(169.79f, 41.00f, -210.79f),
        MapInfo = new Vector2(24.9f, 17.3f),
        NpcSelection = 0,
    };
    private static CriticalInfo Thunderstorms2β = new()
    {
        RawLocation = new Vector3(-615.30f, 8.26f, -515.45f),
        MapInfo = new Vector2(9.2f, 11.2f),
        NpcSelection = 1,
    };

    private static CriticalInfo AnnealingWinds1α = new()
    {
        RawLocation = new Vector3(239.77f, 133.83f, -704.44f),
        MapInfo = new Vector2(26.3f, 7.4f),
        NpcSelection = 0,
    };
    private static CriticalInfo AnnealingWinds1β = new()
    {
        RawLocation = new Vector3(-506.27f, -8.42f, -751.29f),
        MapInfo = new Vector2(11.2f, 6.2f),
        NpcSelection = 1,
    };
    private static CriticalInfo AnnealingWinds2α = new()
    {
        RawLocation = new Vector3(410.29f, 18.90f, 25.14f),
        MapInfo = new Vector2(29.6f, 22.2f),
        NpcSelection = 1,
    };
    private static CriticalInfo AnnealingWinds2β = new()
    {
        RawLocation = new Vector3(10.10f, 7.98f, 339.70f),
        MapInfo = new Vector2(21.7f, 28.3f),
        NpcSelection = 0,
    };

    private static CriticalInfo GlassRain1α = new()
    {
        RawLocation = new Vector3(407.15f, -229.45f, 224.76f),
        MapInfo = new Vector2(29.3f, 25.6f),
        NpcSelection = 0,
    };
    private static CriticalInfo GlassRain1β = new()
    {
        RawLocation = new Vector3(544.42f, -251.07f, 634.55f),
        MapInfo = new Vector2(32.5f, 34.2f),
        NpcSelection = 1,
    };
    private static CriticalInfo GlassRain2α = new()
    {
        RawLocation = new Vector3(148.96f, -9.99f, 487.46f),
        MapInfo = new Vector2(24.6f, 31.4f),
        NpcSelection = 0,
    };
    private static CriticalInfo GlassRain2β = new()
    {
        RawLocation = new Vector3(-488.32f, 25.05f, 35.65f),
        MapInfo = new Vector2(11.8f, 22.3f),
        NpcSelection = 1,
    };

    #endregion

    #region Oizys

    private static CriticalInfo GravitationAnom1α = new()
    {
        RawLocation = new Vector3(77.05f, -58.69f, -475.25f),
        MapInfo = new Vector2(22.9f, 11.9f),
        NpcSelection = 0,
    };
    private static CriticalInfo GravitationAnom1β = new()
    {
        RawLocation = new Vector3(-189.71f, -0.07f, -61.36f),
        MapInfo = new Vector2(17.6f, 20.1f),
        NpcSelection = 1,
    };

    private static CriticalInfo GaleForce1α = new()
    {
        MapInfo = new(33.2f, 12.9f),
        RawLocation = new(584.29f, -60.42f, -429.47f),
        NpcSelection = 0,
    };
    private static CriticalInfo GaleForce1β = new()
    {
        MapInfo = new(24.1f, 20.2f),
        RawLocation = new(125.37f, 0.64f, -68.72f),
        NpcSelection = 1,
    };
    private static CriticalInfo GaleForce2α = new()
    {
        MapInfo = new(8.2f, 12.3f),
        RawLocation = new(-669.31f, -88.50f, -453.18f),
        NpcSelection = 0,
    };
    private static CriticalInfo GaleForce2β = new()
    {
        MapInfo = new(19.0f, 20.5f),
        RawLocation = new(-127.96f, 0.27f, -50.00f),
        NpcSelection = 1,
    };

    private static CriticalInfo BubbleBloom1α = new()
    {
        MapInfo = new(10.1f, 24.6f),
        RawLocation = new(-572.10f, 22.70f, 156.11f),
        NpcSelection = 0,
    };
    private static CriticalInfo BubbleBloom1β = new()
    {
        MapInfo = new(14.1f, 39),
        RawLocation = new(-369.71f, 104.98f, 876.68f),
        NpcSelection = 1,
    };

    #endregion

    public static Dictionary<uint, CriticalInfo> CriticalLocations = new();

    public static void UpdateCriticalWeather()
    {
        // Sinus - Astromagnetic Storm 1/α
        AddKeys(AstromagneticStorm1α, 518, 522, 530);
        AddKeys(AstromagneticStorm1β, 537, 543);

        // Astromagnetic Storm 2/β
        AddKeys(AstromagneticStorm2α, 512, 521, 527);
        AddKeys(AstromagneticStorm2β, 533, 536, 542);

        // Meteor Showers 1/α
        AddKeys(MeteorShower1α, 515, 524, 538);
        AddKeys(MeteorShower1β, 519, 523);

        // Meteor Showers 2/β
        AddKeys(MeteorShower2α, 516, 520, 525);
        AddKeys(MeteorShower2β, 531, 534, 539);

        // Sporing Mist 1/α
        AddKeys(SporingMist1α, 517, 532);
        AddKeys(SporingMist1β, 514, 529, 541);

        // Sporing Mist 2/β
        AddKeys(SporingMist2α, 513, 526, 528);
        AddKeys(SporingMist2β, 535, 540, 544);

        // Phaenna - Thunderstorm 1/α
        AddKeys(Thunderstorms1α, 1007, 1019, 1025);
        AddKeys(Thunderstorms1β, 1016, 1022);

        // Thunderstorm 2/β
        AddKeys(Thunderstorms2α, 1017, 1023, 1034);
        AddKeys(Thunderstorms2β, 1010, 1026, 1031);

        // Annealing Winds 1/α
        AddKeys(AnnealingWinds1α, 1028, 1032, 1037);
        AddKeys(AnnealingWinds1β, 1011, 1020, 1035);

        AddKeys(AnnealingWinds2α, 1013, 1029, 1038);
        AddKeys(AnnealingWinds2β, 1008, 1036);

        // Glass Rain 1/α
        AddKeys(GlassRain1α, 1009, 1014, 1021);
        AddKeys(GlassRain1β, 1030, 1039);

        // Glass Rain 2/β
        AddKeys(GlassRain2α, 1015, 1018, 1033);
        AddKeys(GlassRain2β, 1012, 1024, 1027);

        AddKeys(GravitationAnom1α, 1350, 1356);
        AddKeys(GravitationAnom1β, 1352, 1354, 1358);

        AddKeys(GaleForce1α, 1353, 1357, 1363);
        AddKeys(GaleForce1β, 1349, 1360, 1369);
        AddKeys(GaleForce2α, 1351, 1365);
        AddKeys(GaleForce2β, 1359, 1361, 1367);

        AddKeys(BubbleBloom1α, 1348, 1362, 1366);
        AddKeys(BubbleBloom1β, 1355, 1364, 1368);
    }

    private static void AddKeys(CriticalInfo location, params uint[] keys)
    {
        foreach (var key in keys)
            CriticalLocations[key] = location;
    }
}
