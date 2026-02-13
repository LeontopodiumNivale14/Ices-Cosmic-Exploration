using System;
using System.IO;
using ICE.OldYamlConfig;
using ICE.Utilities.Cosmic_Helper;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ICE.ConfigFiles;

public static class ConfigMigration
{
    public static void MigrateFromOldYaml(Config newConfig)
    {
        // Check if old YAML exists
        if (!File.Exists(MissionConfigs.ConfigPath))
        {
            IceLogging.Info("No old YAML config found, skipping migration");
            return;
        }

        try
        {
            IceLogging.Info($"Found old YAML config at: {0} {MissionConfigs.ConfigPath}");
            IceLogging.Info("Starting migration to new EzConfig system...");

            // Load old config with proper naming convention
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var yaml = File.ReadAllText(MissionConfigs.ConfigPath);
            var oldConfig = deserializer.Deserialize<MissionConfigs>(yaml);

            // Log how many mission configs were found
            IceLogging.Info($"Found {oldConfig.MissionConfig?.Count ?? 0} mission configurations to migrate");
            IceLogging.Info($"Found {oldConfig.GatherProfiles?.Count ?? 0} gather profiles to migrate");
            IceLogging.Info($"Found {oldConfig.ScoreKeeper?.Count ?? 0} score keeper entries to migrate");

            // Migrate all sections
            MigrateSafetySettings(oldConfig);
            MigrateMainWindow(oldConfig);
            MigrateOverlay(oldConfig);
            MigrateMissionSettings(oldConfig);
            MigrateTableSettings(oldConfig);
            MigrateRepairSettings(oldConfig);
            MigrateGatheringSettings(oldConfig);
            MigrateGambaSettings(oldConfig);
            MigrateMiscSettings(oldConfig);
            MigrateRelicSettings(oldConfig);
            MigrateShoppingSettings(oldConfig);
            MigrateDebugSettings(oldConfig);

            // Save the migrated config
            newConfig.Save();

            // Log migration success
            IceLogging.Info($"Migrated {newConfig.MissionConfig?.Count ?? 0} mission configurations");
            IceLogging.Info($"Migrated {newConfig.GatherProfiles?.Count ?? 0} gather profiles");
            IceLogging.Info($"Migrated {newConfig.ScoreKeeper?.Count ?? 0} score keeper entries");

            // Backup old config
            BackupOldConfig();

            IceLogging.Info("Migration completed successfully!");
            C.OldConfigMigrateV1 = true;
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to migrate config: {ex}");
            PluginLog.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    private static void MigrateSafetySettings(MissionConfigs old)
    {
        C.StopOnAbort = old.StopOnAbort;
        C.RejectUnknownYesno = old.RejectUnknownYesno;
        C.DelayGrabMission = old.DelayGrabMission;
        C.DelayIncrease = old.DelayIncrease;
        C.DelayCraft = old.DelayCraft;
        C.DelayCraftIncrease = old.DelayCraftIncrease;
        C.AnimationLockAbandon = old.AnimationLockAbandon;
        C.JumpIfStuck = old.JumpIfStuck;
    }

    private static void MigrateMainWindow(MissionConfigs old)
    {
        C.ShowInfoButton = old.ShowInfoButton;
        C.MiddleColumnWidth = old.MiddleColumnWidth;
        C.SelectedJob = old.SelectedJob;
        C.XPRelicGrind = old.XPRelicGrind;
        C.XPLeveling_Mode = old.XPLeveling_Mode;
        C.XPRelicIgnoreManual = old.XPRelicIgnoreManual;
        C.XPRelicOnlyEnabled = old.XPRelicOnlyEnabled;
        C.ShowCritical = old.ShowCritical;
        C.ShowSequential = old.ShowSequential;
        C.ShowWeather = old.ShowWeather;
        C.ShowTimeRestricted = old.ShowTimeRestricted;
        C.ShowClassA = old.ShowClassA;
        C.ShowClassB = old.ShowClassB;
        C.ShowClassC = old.ShowClassC;
        C.ShowClassD = old.ShowClassD;
    }

    private static void MigrateOverlay(MissionConfigs old)
    {
        C.ShowOverlay = old.ShowOverlay;
        C.ShowSeconds = old.ShowSeconds;
        C.ShowTotalScore = old.ShowTotalScore;
        C.ShowExpBars = old.ShowExpBars;
    }

    private static void MigrateMissionSettings(MissionConfigs old)
    {
        C.OnlyGrabMission_Debug = old.OnlyGrabMission;
        C.TargetLevel = old.TargetLevel;
        C.StopWhenLevel = old.StopWhenLevel;
        C.StopOnceHitCosmoCredits = old.StopOnceHitCosmoCredits;
        C.CosmoCreditsCap = old.CosmoCreditsCap;
        C.StopOnceHitLunarCredits = old.StopOnceHitLunarCredits;
        C.LunarCreditsCap = old.LunarCreditsCap;
        C.StopOnceHitCosmicScore = old.StopOnceHitCosmicScore;
        C.CosmicScoreCap = old.CosmicScoreCap;
        C.StopOnceRelicFinished = old.StopOnceRelicFinished;
        C.SequenceMissionPriority = old.SequenceMissionPriority;
        C.WeatherMissionPriority = old.WeatherMissionPriority;
        C.TimedMissionPriority = old.TimedMissionPriority;

        if (old.MissionConfig != null)
        {
            C.MissionConfig = old.MissionConfig;
            IceLogging.Info($"Migrating {old.MissionConfig.Count} mission configurations");

            // Validate and fix null entries
            var nullKeys = C.MissionConfig.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
            foreach (var key in nullKeys)
            {
                IceLogging.Warning($"Found null MissionSettings for mission {key}, creating new instance");
                C.MissionConfig[key] = new Config.MissionSettings();
            }

            if (nullKeys.Count > 0)
            {
                IceLogging.Info($"Fixed {nullKeys.Count} null mission configurations");
            }
        }

        C.GrindAllProvisionals = old.GrindAllProvisionals;

        if (old.JobPrio != null)
            C.JobPrio = old.JobPrio;

        C.AutoSelectMoon = old.AutoSelectMoon;
        C.ShowSinusMissions = old.ShowSinusMissions;
        C.ShowPhaennaMissions = old.ShowPhaennaMissions;
        C.RemoveAfterGold = old.RemoveAfterGold;
        C.ShowExtraMissionInfo = old.ShowExtraMissionInfo;

        // Deep copy dictionaries
        if (old.ScoreKeeper != null)
        {
            C.ScoreKeeper = old.ScoreKeeper;
            IceLogging.Info($"Migrating {old.ScoreKeeper.Count} score keeper entries");
        }

        if (old.MissionConfig != null)
        {
            C.MissionConfig = old.MissionConfig;
            IceLogging.Info($"Migrating {old.MissionConfig.Count} mission configurations");
        }
    }

    private static void MigrateTableSettings(MissionConfigs old)
    {
        C.TableSortOption = old.TableSortOption;
        C.HideUnsupportedMissions = old.HideUnsupportedMissions;
        C.AutoPickCurrentJob = old.AutoPickCurrentJob;
        C.ShowCompletionWindow = old.ShowCompletionWindow;
        C.ShowCompletionOnlyJob = old.ShowCompletionOnlyJob;
        C.ShowSelectedJobOnly = old.ShowSelectedJobOnly;
        C.ShowCompletion_MissingGold = old.ShowCompletion_MissingGold;
        C.ShowManualMode = old.ShowManualMode;
        C.Auto_ShowTokens = old.Auto_ShowTokens;
        C.Show_StopWhen = old.Show_StopWhen;
        C.Show_GatheringProfile = old.Show_GatheringProfile;
        C.Show_MissionPriority = old.Show_MissionPriority;
        C.Show_MiscSettings = old.Show_MiscSettings;
        C.Show_HubActivities = old.Show_HubActivities;
    }

    private static void MigrateRepairSettings(MissionConfigs old)
    {
        C.SelfRepairGather = old.SelfRepairGather;
        C.SelfRepairCrafter = old.SelfRepairCrafter;
        C.RepairAtVendor = old.RepairAtVendor;
        C.RepairPercent = old.RepairPercent;
        C.SelfSpiritbondGather = old.SelfSpiritbondGather;
    }

    private static void MigrateGatheringSettings(MissionConfigs old)
    {
        C.SelectedGatherIndex = old.SelectedGatherIndex;
        C.UseGatheringFood = old.UseGatheringFood;
        C.GatheringFood = old.GatheringFood;
        C.AutoCordial = old.AutoCordial;
        C.inverseCordialPrio = old.inverseCordialPrio;
        C.CordialMinGp = old.CordialMinGp;
        C.UseOnFisher = old.UseOnFisher;
        C.PreventOvercap = old.PreventOvercap;
        C.UseOnlyInMission = old.UseOnlyInMission;

        // Deep copy gather settings
        if (old.GatherSettings != null)
        {
            C.GatherSettings = old.GatherSettings;
            IceLogging.Info($"Migrating {old.GatherSettings.Count} gather settings");
        }

        if (old.GatherProfiles != null)
        {
            C.GatherProfiles = old.GatherProfiles;
            IceLogging.Info($"Migrating {old.GatherProfiles.Count} gather profiles");
        }
    }

    private static void MigrateGambaSettings(MissionConfigs old)
    {
        // Deep copy gamba items
        if (old.GambaItemWeights != null)
        {
            C.GambaItemWeights = old.GambaItemWeights;
            IceLogging.Info($"Migrating {old.GambaItemWeights.Count} gamba item weights");
        }

        C.GambaEnabled = old.GambaEnabled;
        C.GambaPreferSmallerWheel = old.GambaPreferSmallerWheel;
        C.GambaCreditsMinimum = old.GambaCreditsMinimum;
        C.GambaDelay = old.GambaDelay;
        C.GambaBetweenRuns = old.GambaBetweenRuns;
        C.GambaAtAmount = old.GambaAtAmount;
    }

    private static void MigrateMiscSettings(MissionConfigs old)
    {
        C.MoonSprint = old.MoonSprint;
        C.MountId = old.MountId;
        C.MountName = old.MountName;
        C.MountRadius = old.MountRadius;
        C.DismountRadius = old.DismountRadius;
        C.UseMountOutsideMission = old.UseMountOutsideMission;
        C.UseMountInMission = old.UseMountInMission;
        C.LeftColumnWidth = old.LeftColumnWidth;
        C.PlaySoundAlert = old.PlaySoundAlert;
        C.SoundVolume = old.SoundVolume;
        C.TimeHistoryLimit = old.TimeHistoryLimit;
        C.RemoveStellarStatus = old.RemoveStellarStatus;
        C.ShowSPM = old.ShowSPM;
        C.StartUponEnterMoon = old.StartUponEnterMoon;
        C.PersonalReturnSpot = old.PersonalReturnSpot;

        // Deep copy dictionaries
        if (old.CrafterLocations != null)
        {
            C.CrafterLocations = old.CrafterLocations;
            IceLogging.Info($"Migrating {old.CrafterLocations.Count} crafter locations");
        }

        if (old.PostMissionCommands != null)
        {
            C.PostMissionCommands = old.PostMissionCommands;
            IceLogging.Info($"Migrating {old.PostMissionCommands.Count} post mission commands");
        }
    }

    private static void MigrateRelicSettings(MissionConfigs old)
    {
        C.TurninRelic = old.TurninRelic;

        // Deep copy relic jobs
        if (old.RelicJobs != null)
            C.RelicJobs = old.RelicJobs;

        C.FarmAllRelics = old.FarmAllRelics;
        C.Stop_AllRelicsComplete = old.Stop_AllRelicsComplete;
    }

    private static void MigrateShoppingSettings(MissionConfigs old)
    {
        // Deep copy shopping lists
        if (old.CosmoShopping != null)
        {
            C.CosmoShopping = old.CosmoShopping;
            IceLogging.Info($"Migrating {old.CosmoShopping.Count} cosmo shopping items");
        }

        if (old.CosmoShoppingOrder != null)
            C.CosmoShoppingOrder = old.CosmoShoppingOrder;

        C.BuyItems = old.BuyItems;
        C.CosmoBuyAtAmount = old.CosmoBuyAtAmount;
    }

    private static void MigrateDebugSettings(MissionConfigs old)
    {
        C.FailsafeRecipeSelect = old.FailsafeRecipeSelect;
        C.UseDummyXp = old.UseDummyXp;

        // Deep copy dummy XP
        if (old.DummyXP != null)
            C.DummyXP = old.DummyXP;

        C.PictoColor_Circle = old.PictoColor_Circle;
        C.PictoColor_Dot = old.PictoColor_Dot;
        C.PictoColor_Cone = old.PictoColor_Cone;
        C.UseDummyRanks = old.UseDummyRanks;
        C.ShowDummyA = old.ShowDummyA;
        C.ShowDummyB = old.ShowDummyB;
        C.ShowDummyC = old.ShowDummyC;
        C.ShowDummyD = old.ShowDummyD;
        C.DisablePathfindingToRedAlert = old.DisablePathfindingToRedAlert;
        C.ShowDebugGatherInfo = old.ShowDebugGatherInfo;
        C.AuthorName = old.AuthorName;
        C.CustomRoutePath = old.CustomRoutePath;
        C.DisableHudClipping = old.DisableHudClipping;
        C.HighlightVisibleMissions = old.HighlightVisibleMissions;
    }

    private static void BackupOldConfig()
    {
        try
        {
            var backupPath = MissionConfigs.ConfigPath + ".backup";
            File.Copy(MissionConfigs.ConfigPath, backupPath, overwrite: true);

            // Optionally delete the old config after backing up
            // File.Delete(MissionConfigs.ConfigPath);

            IceLogging.Info($"Old config backed up to: {backupPath}");
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to backup old config: {ex}");
        }
    }
}