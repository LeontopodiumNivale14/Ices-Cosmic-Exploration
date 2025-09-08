using Dalamud.Interface.Textures;
using ECommons.GameHelpers;
using ICE.Enums;
using System.Collections.Generic;

namespace ICE.Utilities;

public static unsafe partial class CosmicHelper
{

    public static readonly HashSet<uint> Ranks = [1, 2, 3, 4];
    public static readonly HashSet<uint> ARankIds = [4, 5, 6];

    public static readonly HashSet<int> CrafterJobList = [8, 9, 10, 11, 12, 13, 14, 15];
    public static readonly HashSet<int> GatheringJobList = [16, 17, 18];

    public static readonly HashSet<int> WeatherMissionList = [30, 31, 32,];
    public static readonly HashSet<int> TimedMissionList = [40, 43,];
    public static readonly HashSet<int> CriticalMissions = [512, 513, 514,];

    public static List<int> GreyIconList = new List<int>() { 91031, 91032, 91033, 91034, 91035, 91036, 91037, 91038, 91039, 91040, 91041 };

    public static readonly int MinimumLevel = 10;
    public static readonly int MaximumLevel = Player.MaxLevel;

    #region Dictionaries

    /// <summary>
    /// Some things to note that I didn't realize until after I really dug into the sheet a bit more/cleaned this up. <br />
    /// Sheet is: WKSMissionUnit <br />
    /// <b>- Row 0:</b> Mission Name <br />
    /// <b>- Row 2:</b> JobId attached to the quest (so 9 is CRP, 10 is BSM, etc.) <br />
    /// <b>- Row 3:</b> 2nd Required job??? <br />
    /// <b>- Row 4:</b> 3rd Required job??? <br />
    /// <b>- Row 5:</b> Bool → Is it a critical mission? <br />
    /// <b>- Row 6:</b> Rank → D = 1 | C = 2 | B = 3 | 4 = A-1 | 5 = A-2 | 6 = A-3 <br />
    /// <b>- Row 7:</b> Mission time limit (seconds) <br />
    /// <b>- Row 18:</b> Recipe # → Corresponds to the RecipeID
    /// </summary>
    public class MissionListInfo
    {
        public string Name { get; set; }
        public uint JobId { get; set; }
        public uint JobId2 { get; set; } = 0;
        public uint JobId3 { get; set; } = 0;
        public uint ToDoSlot { get; set; }
        public uint Rank { get; set; }
        public MissionAttributes Attributes { get; set; }
        public uint TimeLimit { get; set; }
        public uint StartTime { get; set; }
        public uint EndTime { get; set; }
        public CosmicWeather Weather { get; set; }
        public uint RecipeId { get; set; } = 0;
        public uint BronzeRequirement { get; set; }
        public uint SilverRequirement { get; set; }
        public uint GoldRequirement { get; set; }
        public uint CosmoCredit { get; set; }
        public uint LunarCredit { get; set; }
        public uint PreviousMissionID { get; set; }
        public uint MarkerId { get; set; }
        public uint TerritoryId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Radius { get; set; }
        public uint NodeSet { get; set; }
        public List<(int Type, int Amount)> ExperienceRewards { get; set; }
    }

    public static Dictionary<uint, MissionListInfo> MissionInfoDict = [];
    public class MoonRecipieInfo
    {
        public Dictionary<ushort, int> MainCraftsDict = [];
        public bool PreCrafts { get; set; } = false;
        public Dictionary<ushort, int> PreCraftDict = [];
    }

    public static Dictionary<uint, MoonRecipieInfo> MoonRecipies = [];

    public class GatheringInfo
    {
        public Dictionary<uint, int> MinGatherItems = [];
    }

    public static Dictionary<uint, GatheringInfo> GatheringItemDict = new();

    public static Dictionary<uint, ISharedImmediateTexture> GreyTexture = new Dictionary<uint, ISharedImmediateTexture>();

    public static Dictionary<uint, ISharedImmediateTexture> JobIconDict = new Dictionary<uint, ISharedImmediateTexture>();

    #endregion
}