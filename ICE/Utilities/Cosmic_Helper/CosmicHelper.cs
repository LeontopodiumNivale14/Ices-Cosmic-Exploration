using Dalamud.Interface.Textures;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ICE.Enums;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ICE.Utilities;

public static unsafe partial class CosmicHelper
{

    public static readonly List<uint> Ranks = [1, 2, 3, 4];
    public static readonly List<uint> ARankIds = [4, 5, 6];

    public static readonly List<uint> CrafterJobList = [8, 9, 10, 11, 12, 13, 14, 15];
    public static readonly List<uint> GatheringJobList = [16, 17, 18];


    /// <summary>
    /// Currently contains all the WKSMissionLotterySpecialCond values that are weather based
    /// MAKE SURE. TO UPDATE THIS. COME NEW MOON
    /// </summary>
    public static readonly HashSet<uint> WeatherSelection = new() { 13, 14, 15, 16, 23, 24 };

    public static List<int> GreyIconList = new List<int>() { 91031, 91032, 91033, 91034, 91035, 91036, 91037, 91038, 91039, 91040, 91041 };
    public static Dictionary<CosmicWeather, int> WeatherIds = new()
    {
        [CosmicWeather.UmbralWind] = 60219,
        [CosmicWeather.MoonDust] = 60222,
        [CosmicWeather.Clouds] = 60203,
        [CosmicWeather.Rain] = 60207,
        [CosmicWeather.ClearSkies] = 60201,
        [CosmicWeather.FairSkies] = 60202,
    };

    public static readonly int MinimumLevel = 10;
    public static readonly int MaximumLevel = Player.MaxLevel;

    public static readonly int MaxRelicLevel = 17;

    #region Dictionaries

    public class CraftingInfo
    {
        public uint ItemId { get; set; } 
        public uint Durability { get; set; }
        public uint Quality { get; set; }
        public uint Progress { get; set; }
        public int RequiredAmount { get; set; } 
        public uint RecipeId { get; set; }
        public bool ExpertCraft { get; set; } = false;
        public Dictionary<uint, int> RequiredItems { get; set; } = new();
    }

    /// <summary>
    /// Some things to note that I didn't realize until after I really dug into the sheet a bit more/cleaned this up. <br />
    /// Sheet is: WKSMissionUnit <br />
    /// <b>- Row 0:</b> Mission Name <br />
    /// <b>- Row 2:</b> JobId attached to the quest (so 8 is CRP, 9 is BSM, etc.) <br />
    /// <b>- Row 3:</b> 2nd Required job??? <br />
    /// <b>- Row 4:</b> 3rd Required job??? <br />
    /// <b>- Row 5:</b> Bool → Is it a critical mission? <br />
    /// <b>- Row 6:</b> Rank → D = 1 | C = 2 | B = 3 | 4 = A-1 | 5 = A-2 | 6 = A-3 <br />
    /// <b>- Row 7:</b> Mission time limit (seconds) <br />
    /// <b>- Row 18:</b> Recipe # → Corresponds to the RecipeID
    /// </summary>
    public class CosmicInfo
    {
        // - - - Crafter Specific - - - //
        public Dictionary<ushort, CraftingInfo> Crafts_Main { get; set; } = new();
        public Dictionary<ushort, CraftingInfo> Crafts_Pre { get; set; } = new();
        public bool IsExpert { get; set; } = false;

        // - - - BTN | MIN Specific - - - //
        public Dictionary<uint, int> Gathering_Min { get; set; } = new();

        // - - - FSH Specific - - - //
        public int Fish_AmountRequired { get; set; } = 0;
        public int Fish_VarietyAmount { get; set; } = 0;
        public List<string> Fish_Presets { get; set; } = new();

        // - - - Map Related - - - // 
        public Vector2 MapPosition { get; set; } = new();
        public int Radius { get; set; } = 0;
        public uint TerritoryId { get; set; }
        public uint MarkerId { get; set; }

        // - - - Exp Modifier Section - - - // 

        public uint ExpModifier_1 { get; set; } = 0;
        public uint ExpModifier_2 { get; set; } = 0;
        public uint ExpModifier_3 { get; set; } = 0;

        // - - - Universal Info - - - //
        public string Name { get; set; }
        public List<uint> Jobs { get; set; } = new();
        public uint ToDoId { get; set; } = 0;
        public uint Rank { get; set; } = 1;
        public uint Level { get; set; } = 0;
        public MissionAttributes Attributes { get; set; }
        public CosmicWeather Weather { get; set; }
        public uint StartTime { get; set; }
        public uint EndTime { get; set; }
        public uint ClassScore { get; set; } = 0;
        public uint CosmoCredit { get; set; } = 0;
        public uint LunarCredit { get; set; } = 0;
        public uint RewardItem { get; set; } = 0;
        public uint RewardItemAmount { get; set; } = 0;
        public uint DronebitReward { get; set; } = 0;
        public HashSet<uint> PreviousMissions { get; set; } = new();
        public Dictionary<int, int> RelicXpInfo { get; set; } = new();
        public uint BronzeScore { get; set; } = 0;
        public uint SilverScore { get; set; } = 0;
        public uint GoldScore { get; set; } = 0;
    }

    public static Dictionary<uint, CosmicInfo> SheetMissionDict = new();

    public class GatheringInfo
    {
        public Dictionary<uint, int> MinGatherItems = [];
    }

    public static Dictionary<uint, GatheringInfo> GatheringItemDict = new();

    public static Dictionary<uint, ISharedImmediateTexture> GreyTexture = new Dictionary<uint, ISharedImmediateTexture>();

    public static Dictionary<uint, ISharedImmediateTexture> JobIconDict = new Dictionary<uint, ISharedImmediateTexture>();
    public static Dictionary<CosmicWeather, ISharedImmediateTexture> WeatherIconDict = new();

    public static Dictionary<uint, uint> MissionScoreDict = new(); // MissionID -> Score

    // Load the CSV file
    public static void LoadMissionScores()
    {
        MissionScoreDict.Clear();

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "ICE.Resources.MissionScores.csv";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            PluginLog.Error($"Failed to find embedded CSV: {resourceName}");
            return;
        }

        using var reader = new StreamReader(stream);
        bool headerSkipped = false;
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (!headerSkipped)
            {
                headerSkipped = true;
                continue; // Skip header
            }

            var parts = line.Split(',');
            if (parts.Length >= 4 &&
                uint.TryParse(parts[0].Trim(), out uint missionId) &&
                uint.TryParse(parts[3].Trim(), out uint score))
            {
                MissionScoreDict[missionId] = score;
            }
        }
    }

    public class GatherItemInfo
    {
        public HashSet<uint> itemIds { get; set; } = new();
        public uint Type { get; set; } = 0;
    }
    public static Dictionary<string, GatherItemInfo> GatheringItems = new();

    public class XPType
    {
        public int CurrentXP { get; set; }
        public int NeededXP { get; set; }
    }

    public static Dictionary<uint, List<uint>> MissionUnlock = new()
    {
        [499] = new() { 82, 397 },
        [500] = new() { 217, 397 },
        [501] = new() { 262, 397 },
        [505] = new() { 37, 442 },
        [506] = new() { 127, 442 },
        [507] = new() { 307, 442 },
        [510] = new() { 172, 487 },
        [511] = new() { 352, 487 }
    };

    #endregion
}