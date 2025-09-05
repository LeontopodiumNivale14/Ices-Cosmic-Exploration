using System.Collections.Generic;
using System.Text.Json.Serialization;
using ECommons.Configuration;

namespace ICE
{
    public class Configuration : IEzConfig
    {
        [JsonIgnore]
        public const int CurrentConfigVersion = 1;

        // Missions the user has enabled
        public List<CosmicMission> Missions { get; set; } = [];

        // Safety settings
        public bool StopOnAbort { get; set; } = true;
        public bool RejectUnknownYesno { get; set; } = true;
        public bool DelayGrabMission { get; set; } = true;
        public bool DelayCraft { get; set; } = true;
        public int DelayIncrease { get; set; } = 500;
        public int DelayCraftIncrease { get; set; } = 2500;
        public int PossiblyStuck = 0;
        public bool AnimationLockAbandon { get; set; } = true;
#if DEBUG
        public bool FailsafeRecipeSelect = false;
#endif
        // Main Window Settings
        public uint SelectedJob = 8;

        public bool showCritical = true;
        public bool showSequential = true;
        public bool showWeather = true;
        public bool showTimeRestricted = true;
        public bool showClassA = true;
        public bool showClassB = true;
        public bool showClassC = true;
        public bool showClassD = true;

        // Mission settings
        public bool OnlyGrabMission { get; set; } = false;
        public bool StopOnceHitCosmoCredits { get; set; } = false;
        public bool StopOnceHitLunarCredits { get; set; } = false;
        public bool StopOnceHitCosmicScore { get; set; } = false;
        public int CosmoCreditsCap { get; set; } = 30000;
        public int LunarCreditsCap { get; set; } = 10000;
        public int CosmicScoreCap { get; set; } = 500000;
        public byte SequenceMissionPriority { get; set; } = 1;
        public byte WeatherMissionPriority { get; set; } = 2;
        public byte TimedMissionPriority { get; set; } = 3;
        public bool ShowNotes { get; set; } = true;
        public bool IncreaseMiddleColumn { get; set; } = true;

        public int TargetLevel { get; set; } = 10;
        public bool StopWhenLevel { get; set; } = false;

        // Overlay settings
        public bool ShowOverlay { get; set; } = false;
        public bool ShowSeconds { get; set; } = false;
        public bool ShowExpBars { get; set; } = false;

        // Table settings
        public bool HideUnsupportedMissions { get; set; } = false;
        public bool AutoPickCurrentJob { get; set; } = false;
        public int TableSortOption = 0;
        public bool ShowExpColums { get; set; } = true;
        public bool ShowCreditsColumn { get; set; } = true;

        // Gathering Settings
        public bool SelfRepairGather { get; set; } = true;
        public bool SelfSpiritbondGather { get; set; } = true;
        public int RepairPercent { get; set; } = 50;
        public int SelectedGatherIndex { get; set; } = 0;
        public List<GatherBuffProfile> GatherSettings { get; set; } = new()
        {
            new GatherBuffProfile { Id = 0, Name = "Default"},
        };
        public bool AutoCordial { get; set; } = false;
        public bool inverseCordialPrio { get; set; } = false;
        public int CordialMinGp { get; set; } = 0;
        public bool UseOnFisher { get; set; } = false;
        public bool PreventOvercap { get; set; } = false;
        public bool UseOnlyInMission { get; set; } = false;

        // Gamba settings
        public List<Gamba> GambaItemWeights { get; set; } = new();
        public bool GambaEnabled { get; set; } = false;
        public bool GambaPreferSmallerWheel { get; set; } = false;
        public int GambaCreditsMinimum { get; set; } = 0;
        public int GambaDelay { get; set; } = 250;

        // Misc settings
        public bool EnableAutoSprint { get; set; } = true;

        public bool XPRelicGrind { get; set; } = false;
        public bool XPRelicIgnoreManual { get; set; } = false;
        public bool XPRelicOnlyEnabled { get; set; } = false;

        public void Save()
        {
            EzConfig.Save();
        }
    }

    public class CosmicMission
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public MissionType Type { get; set; } = MissionType.Standard;
        public bool Enabled { get; set; } = false;
        public uint PreviousMissionId { get; set; } = 0;
        public uint JobId { get; set; }
        public bool TurnInSilver { get; set; } = false;
        public bool TurnInGold { get; set; } = false;
        public bool TurnInASAP { get; set; } = false;
        public bool ManualMode { get; set; } = false;
        public int GatherSettingId { get; set; } = 0;
        [JsonIgnore]
        public GatherBuffProfile GatherSetting => C.GatherSettings.FirstOrDefault(x => x.Id == GatherSettingId)
                                          ?? C.GatherSettings[0]; // fallback to default
        public string TurnInMode;
    }

    public class Gamba
    {
        public uint ItemId { get; set; }
        public int Weight { get; set; } = 0;
        public GambaType Type { get; set; }
    }

    public class GatherBuffs
    {
        public bool BoonIncrease2 { get; set; } = false;
        public int BoonIncrease2Gp { get; set; } = 100;
        public int BoonIncrease2MaxUse { get; set; } = -1;
        public bool BoonIncrease1 { get; set; } = false;
        public int BoonIncrease1Gp { get; set; } = 50;
        public int BoonIncrease1MaxUse { get; set; } = -1;
        public bool TidingsBool { get; set; } = false;
        public int TidingsGp { get; set; } = 200;
        public int TidingsMaxUse { get; set; } = -1;
        public bool YieldII { get; set; } = false;
        public int YieldIIGp { get; set; } = 500;
        public int YieldIIMaxUse { get; set; } = -1;
        public bool YieldI { get; set; } = false;
        public int YieldIGp { get; set; } = 400;
        public int YieldIMaxUse { get; set; } = -1;
        public bool BountifulYieldII { get; set; } = false;
        public int BountifulYieldIIGp { get; set; } = 100;
        public int BountifulYieldIIMaxUse { get; set; } = -1;
        public int BountifulMinItem {  get; set; } = 4;
        public bool BonusIntegrity { get; set; } = false;
        public int BonusIntegrityGp { get; set; } = 300;
        public int BonusIntegrityMaxUse { get; set; } = -1;
    }

    public class GatherBuffProfile
    {
        public int Id { get; set; } // Index being set for quick reference
        public string Name { get; set; } = ""; // Name, moreso for Ui
        public int Pathfinding { get; set; } = 1;
        public int TSPCycleSize { get; set; } = 20;
        public int MinimumGP { get; set; } = -1;
        public int InitialGatheringItemMultiplier { get; set; } = 1;
        public GatherBuffs Buffs { get; set; } = new();
    }

    public enum MissionType
    {
        Standard = 0,
        Sequential = 1,
        Weather = 2,
        Timed = 3,
        Critical = 4
    }

    public enum GambaType
    {
        Mount = 0,
        Emote = 1,
        Minion = 2,
        Outfit = 3,
        Accessory = 4,
        Orchestrion = 5,
        Housing = 6,
        Dye = 7,
        Other = 8,
        Materia = 9,
    }
}