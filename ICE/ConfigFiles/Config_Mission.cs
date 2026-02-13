using ICE.OldYamlConfig;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ICE.ConfigFiles;

public partial class Config
{
    public bool OnlyGrabMission_Debug { get; set; } = false;
    public int TargetLevel { get; set; } = 10;
    public bool StopWhenLevel { get; set; } = false;
    public bool StopOnceHitCosmoCredits { get; set; } = false;
    public int CosmoCreditsCap { get; set; } = 30000;
    public bool StopOnceHitLunarCredits { get; set; } = false;
    public int LunarCreditsCap { get; set; } = 10000;
    public bool StopOnceHitCosmicScore { get; set; } = false;
    public int CosmicScoreCap { get; set; } = 500000;
    public bool StopOnceRelicFinished { get; set; } = false;
    public byte SequenceMissionPriority { get; set; } = 1;
    public byte WeatherMissionPriority { get; set; } = 2;
    public byte TimedMissionPriority { get; set; } = 3;
    public List<ProvisionalTypes> MissionPrio { get; set; } = new()
    {
        ProvisionalTypes.ProvisionalWeather,
        ProvisionalTypes.ProvisionalSequential,
        ProvisionalTypes.ProvisionalTimed
    };
    public bool GrindAllProvisionals { get; set; } = true;
    public List<uint> JobPrio { get; set; } = new()
    {
        8, 9, 10, 11, 12, 13, 14, 15,  // Crafters: CRP, BSM, ARM, GSM, LTW, WVR, ALC, CUL
        16, 17, 18                     // Gatherers: MIN, BTN, FSH
    };
    public bool AutoSelectMoon { get; set; } = true;
    public bool ShowSinusMissions { get; set; } = true;
    public bool ShowPhaennaMissions { get; set; } = true;
    public bool ShowOizysMissions { get; set; } = true;
    public bool RemoveAfterGold { get; set; } = false;
    public bool ShowExtraMissionInfo { get; set; } = true;
    public bool Artisan_RaphaelForce { get; set; } = true;
    public bool Artisan_RaphaelMaster { get; set; } = false;
    public Dictionary<uint, uint> ScoreKeeper { get; set; } = new();

    public Dictionary<uint, MissionSettings> MissionConfig { get; set; } = new();
    public Dictionary<string, List<uint>> Mission_Playlist { get; set; } = new();

    public class MissionSettings
    {
        public bool Enabled { get; set; } = false;
        public bool ManualMode { get; set; } = false;

        public int GProfileId { get; set; } = 0;
        public bool AutoTurnin { get; set; } = true;
        public bool TurninGold { get; set; } = false;
        public bool TurninSilver { get; set; } = false;
        public bool TurninBronze { get; set; } = false;
        public bool Use_BuildinPreset { get; set; } = false;
        public string AutoHookPresetName { get; set; } = string.Empty;
        public double BestTime { get; set; } = double.MaxValue;
        public double AverageTime { get; set; } = 0;
        public double AverageBronzeTime { get; set; } = 0;
        public double AverageSilverTime { get; set; } = 0;
        public double AverageGoldTime { get; set; } = 0;
        public double AverageCriticalTime { get; set; } = 0;
        public int TotalCompletions { get; set; } = 0;
        public int BronzeCompletion { get; set; } = 0;
        public int SilverCompletions { get; set; } = 0;
        public int GoldCompletions { get; set; } = 0;
        public int CriticalCompletions { get; set; } = 0;
        public int FailedCounters { get; set; } = 0;
        public List<TurninData> TurninRecords { get; set; } = new();
        // Old References to time below for migration
        [YamlIgnore]
        public List<double> Times { get; set; } = new();

        public class TurninData
        {
            public double Time { get; set; }
            public TurninState State { get; set; }
        }
    }
}
