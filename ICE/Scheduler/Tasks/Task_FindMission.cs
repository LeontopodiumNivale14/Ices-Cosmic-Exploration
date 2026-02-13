using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.Cosmic;
using ICE.Utilities.Cosmic_Helper;
using ICE.Utilities.GatheringHelper;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ICE.Ui.MainUi.ModeSelect.modeSelect_TableInfo;
using static ICE.Utilities.CosmicHelper;

namespace ICE.Scheduler.Tasks
{
    internal static class Task_FindMission
    { 
        /// <summary>
        /// List of all available critical missions
        /// </summary>
        private static List<uint> CriticalMissions = new List<uint>();
        /// <summary>
        /// List of all the available<br></br>
        /// -> Timed <br></br>
        /// -> Weather <br></br>
        /// -> Sequence <br></br>
        /// </summary>
        private static List<uint> WeatherMissions = new List<uint>();
        private static List<uint> TimedMissions = new List<uint>();
        private static List<uint> SequenceMissions = new List<uint>();
        private static List<uint> ExARankMissions = new List<uint>();
        private static List<uint> ARankMissions = new List<uint>();
        private static List<uint> BRankMissions = new List<uint>();
        private static List<uint> CRankMissions = new List<uint>();
        private static List<uint> DRankMissions = new List<uint>();

        private static int SpecialMissionCount = 0;
        private static int BasicMissionCount = 0;

        private static List<Vector3> fishingPath = new List<Vector3>();
        private static GatheringUtil.FisherSpotInfo fishingEntry = new();

        private static readonly Random _random = new Random();
        private static uint missionToAbandon = 0;

        private static int timeoutAmount = 0;
        private static int maxTimeout = 10;

        private static Vector3 fishingHoleLoc = Vector3.Zero;

        public static void Enqueue()
        {
            IceLogging.Info("Starting the find mission queue", "[Task Find Mission]");
            fishingHoleLoc = Vector3.Zero; // this is here to make sure when we're finding a mission, the queue for the random fishing hole gets reset
            // P.TaskManager.Enqueue(RefreshMissionUi, "Refreshing Mission UI");
            P.TaskManager.Enqueue(OpenMissionUi, "Opening it on proper class");
            if (C.XPRelicGrind)
            {
                P.TaskManager.Enqueue(() => OpenTab("ExpCheck"), "Opening Standard tab for relic grind");
            }
            else
            {
                P.TaskManager.Enqueue(RefreshSelectedMissions, "Refreshing the list of viable missions");
                P.TaskManager.Enqueue(TabTasksCheck, "Checking which tabs to check for missions");
            }
        }
        public static bool? OpenMissionUi()
        {
            if (GenericHelpers.TryGetAddonMaster<Talk>("Talk", out var talkUi) && talkUi.IsAddonReady)
            {
                if (EzThrottler.Throttle("Closing the talk"))
                    talkUi.Click();

                return false;
            }

            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var hud) && hud.IsAddonReady)
            {
                IceLogging.Info("Starting the find mission queue", "[Task: Find Mission | Open Mission Ui]");
                return true;
            }
            else
            {
                if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var moonHud) && moonHud.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Opening the mission ui"))
                    {
                        IceLogging.Info("Opening the moon mission selection hud");
                        moonHud.Mission();
                    }
                }
            }

            return false;
        }
        public static bool? RefreshSelectedMissions()
        {
            CriticalMissions.Clear();
            WeatherMissions.Clear();
            TimedMissions.Clear();
            SequenceMissions.Clear();
            ExARankMissions.Clear();
            ARankMissions.Clear();
            BRankMissions.Clear();
            CRankMissions.Clear();
            DRankMissions.Clear();
            SpecialMissionCount = 0;
            BasicMissionCount = 0;

            uint currentJobId = (uint)Mission_Settings.StartJob;
            IceLogging.Debug($"Show all provisionals: {C.GrindAllProvisionals}");

            bool specialMode = C.XPLeveling_Mode || C.XPRelicGrind;

            if (!specialMode)
            {
                // Sorting out prosionals first
                foreach (var jobId in C.JobPrio)
                {
                    if (!C.GrindAllProvisionals)
                    {
                        if (jobId != currentJobId)
                            continue;
                    }

                    foreach (var m in CosmicHelper.SheetMissionDict.Where(x => x.Value.Jobs.Contains(jobId)))
                    {
                        bool provisional = m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential)
                                        || m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed)
                                        || m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather);

                        if (!provisional)
                            continue;

                        if (C.MissionConfig.TryGetValue(m.Key, out var config) && config.Enabled)
                        {
                            if (m.Value.TerritoryId != Player.Territory.RowId)
                            {
                                IceLogging.Debug($"Skipping: [{m.Key}] due to being in a different zone");
                                IceLogging.Debug($"Current Zone: {Player.Territory} | Mission Zone: {m.Value.TerritoryId}");
                                continue;
                            }

                            IceLogging.Debug($"Found the provisional mission: [{m.Key}] [{m.Value.Name}].");
                            if (m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential) && !SequenceMissions.Contains(m.Key))
                                SequenceMissions.Add(m.Key);
                            else if (m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed) && !TimedMissions.Contains(m.Key))
                                TimedMissions.Add(m.Key);
                            else if (m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather) && !WeatherMissions.Contains(m.Key))
                                WeatherMissions.Add(m.Key);

                            SpecialMissionCount += 1;
                        }
                    }
                }
            }

            foreach (var mission in C.MissionConfig)
            {
                var enabled = mission.Value.Enabled;

                if (C.XPRelicGrind)
                {
                    if (!enabled && C.XPRelicOnlyEnabled)
                        continue;
                }
                else if (C.XPLeveling_Mode)
                {
                    if (!CosmicHelper.QuickLevelList.Contains(mission.Key))
                        continue;
                }
                else if (!enabled)
                    continue;

                var missionId = mission.Key;
                if (CosmicHelper.SheetMissionDict.TryGetValue(missionId, out var missionInfo))
                {
                    List<uint> missionJobs = new();
                    missionJobs = missionInfo.Jobs;

                    // Territory Check, cause people seem to also be forgetting this
                    if (missionInfo.TerritoryId != Player.Territory.RowId)
                        continue;

                    var attribute = missionInfo.Attributes;
                    bool provisional = missionInfo.Attributes.HasFlag(MissionAttributes.ProvisionalSequential)
                                    || missionInfo.Attributes.HasFlag(MissionAttributes.ProvisionalTimed)
                                    || missionInfo.Attributes.HasFlag(MissionAttributes.ProvisionalWeather);

                    if (!provisional)
                    {
                        if (!missionJobs.Contains(currentJobId))
                            continue;

                        // Alright, mission was double checked to make sure it was enabled
                        // And also checked to make sure that the current job is on the mission, time to actually add it to the mission info

                        if (C.XPLeveling_Mode)
                        {
                            var playerLevel = Player.Level;
                            var missionLevel = missionInfo.Level;

                            // Short end of it all, making sure to see what tier the player should be doing
                            // Taking the players level and making sure it matches to the tier
                            // 90+ = 90
                            // 50-89 = 50
                            // 10-49 = 10
                            int playerTier = playerLevel >= 90 ? 90 : playerLevel >= 50 ? 50 : 10;

                            if (missionLevel != playerTier)
                                continue;
                        }

                        if (missionInfo.Attributes.HasFlag(MissionAttributes.Critical))
                            CriticalMissions.Add(missionId);
                        else if (missionInfo.Rank == 5)
                        {
                            ExARankMissions.Add(missionId);
                            BasicMissionCount += 1;
                        }
                        else if (missionInfo.Rank == 4)
                        {
                            ARankMissions.Add(missionId);
                            BasicMissionCount += 1;
                        }
                        else if (missionInfo.Rank == 3)
                        {
                            BRankMissions.Add(missionId);
                            BasicMissionCount += 1;
                        }
                        else if (missionInfo.Rank == 2)
                        {
                            CRankMissions.Add(missionId);
                            BasicMissionCount += 1;
                        }
                        else if (missionInfo.Rank == 1)
                        {
                            DRankMissions.Add(missionId);
                            BasicMissionCount += 1;
                        }
                    }
                }
                else
                {
                    IceLogging.Error($"We're somehow missing a mission from the sheets??? MissionID: {missionId}\n" +
                                     $"Please let me know if this happens");
                }
            }

            IceLogging.Info($"Mission count has been updated to the following for JobId {currentJobId}: \n" +
                $"Critical Count: {CriticalMissions.Count}\n" +
                $"Sequence Count: {SequenceMissions.Count}\n" +
                $"Timed Count: {TimedMissions.Count}\n" +
                $"Weather Count: {WeatherMissions.Count}\n" +
                $"A-EX Rank: {ExARankMissions.Count} \n" +
                $"A Rank: {ARankMissions.Count} \n" +
                $"B Rank: {BRankMissions.Count} \n" +
                $"C Rank: {CRankMissions.Count} \n" +
                $"D Rank: {DRankMissions.Count} \n" +
                $"Total Critical Missions: {CriticalMissions.Count} \n" +
                $"Total Special Missions: {SpecialMissionCount} \n" +
                $"Total Basic Missions: {BasicMissionCount} \n");

            return true;
        }
        public static bool? TabTasksCheck()
        {
            bool hasCritical = CriticalMissions.Count > 0;
            bool hasSpecial = SpecialMissionCount > 0;
            bool hasBasic = BasicMissionCount > 0;

            if (!(hasCritical || hasSpecial || hasBasic))
            {
                IceLogging.Debug("You have... no active missions and you're not on relic grinding mode. Disabling this for now");
                SchedulerMain.State = IceState.Idle;
                SchedulerMain.DisablePlugin();
            }

            if (hasCritical)
                P.TaskManager.Enqueue(() => OpenTab("Critical"), "Opening the critical tab for missions");
            if (hasSpecial)
                P.TaskManager.Enqueue(() => OpenTab("Provisional"), "Opening the provisional tab for missions");
            if (hasBasic)
                P.TaskManager.Enqueue(() => OpenTab("Standard"), "Opening the standard mission tab");

            return true;
        }
        public static bool? OpenTab(string type)
        {
            if (CosmicHelper.CurrentLunarMission != 0)
            {
                IceLogging.Info($"Mission has been accepted. No reason to open tab: {type}");
                return true;
            }

            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                switch (type)
                {
                    case "Critical":
                        {
                            x.CriticalMissions();
                            P.TaskManager.InsertMulti
                            (
                                new(() => FrameDelay(8), "Delaying 8 frames for tab"),
                                new(CheckCritical, "Checking to see if current missions match up with the critical")
                            );
                            break;
                        }
                    case "Provisional":
                        {
                            x.ProvisionalMissions();
                            P.TaskManager.InsertMulti
                            (
                                new(() => FrameDelay(8), "Delaying 8 frames for tab"),
                                new(CheckAllProvisional, "Checking to see if any provisional missions exist")
                            );
                            break;
                        }
                    case "ExpCheck":
                        {
                            x.BasicMissions();
                            P.TaskManager.InsertMulti
                            (
                                new(() => FrameDelay(8), "Delaying 8 frames for the tab"),
                                new(SelectProperClass, "Selecting the proper class"),
                                new(() => CheckExp(), "Checking Exp Missions")
                            );
                            break;
                        }
                    case "Reset":
                        {
                            x.BasicMissions();
                            P.TaskManager.InsertMulti
                            (
                                new(() => FrameDelay(8), "Throwing a delay in to make sure you're on the right tab"),
                                new(SelectProperClass, "Selecting the proper class"),
                                new(() => FindReroll(), "Finding->Accepting next reroll")
                            );
                            break;
                        }
                    default:
                        {
                            x.BasicMissions();
                            P.TaskManager.InsertMulti
                            (
                                new(() => FrameDelay(8), "Delaying 8 frames for tab"),
                                new(SelectProperClass, "Selecting the proper class"),
                                new(CheckStandard, "Checking the standard missions for any potentional missions")
                            );
                            break;
                        }
                }

                return true;
            }
            else
            {
                if (EzThrottler.Throttle("Opening the mission ui"))
                {
                    if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var moonHud) && moonHud.IsAddonReady)
                        moonHud.Mission();
                }
            }

            return false;
        }
        public static bool? SelectProperClass()
        {
            var jobTab = (int)Mission_Settings.StartJob - 8;
            IceLogging.Debug($"Should be going to job tab: {jobTab}");
            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var missionInfo) && missionInfo.IsAddonReady)
            {
                foreach (var mission in missionInfo.StellerMissions)
                {
                    var missionSheet = CosmicHelper.SheetMissionDict[mission.MissionId];
                    bool isCritical = missionSheet.Attributes.HasFlag(MissionAttributes.Critical);
                    bool isProvisional = missionSheet.Attributes.HasFlag(MissionAttributes.ProvisionalSequential)
                        || missionSheet.Attributes.HasFlag(MissionAttributes.ProvisionalTimed)
                        || missionSheet.Attributes.HasFlag(MissionAttributes.ProvisionalWeather);

                    if (isCritical || isProvisional)
                    {
                        if (EzThrottler.Throttle("We're on the wrong tab... changing"))
                            missionInfo.BasicMissions();

                        return false;
                    }

                    if (!missionSheet.Jobs.Contains((uint)Mission_Settings.StartJob))
                    {
                        // We're not in the correct tab, going to return false and make sure we select the proper one
                        if (EzThrottler.Throttle("Selecting the right job tab", 1000))
                        {
                            string jobString = string.Join(", ", CosmicHelper.SheetMissionDict[mission.MissionId].Jobs);

                            IceLogging.Debug($"Selecting proper job tab. Expecting: {jobTab} | Job: {(int)Mission_Settings.StartJob} | On job page: {jobString} | MissionID: {mission.MissionId}");
                            missionInfo.SelectClass[jobTab].Select();
                        }

                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                // if we've gotten this far, that means we're in the right area, returning true
                return true;
            }
            else
            {
                if (EzThrottler.Throttle("Opening the mission ui"))
                {
                    if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var moonHud) && moonHud.IsAddonReady)
                        moonHud.Mission();
                }
            }
            return false;
        }
        public static bool? CheckCritical()
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                foreach (var mission in x.StellerMissions)
                {
                    if (CriticalMissions.Contains(mission.MissionId))
                    {
                        mission.Select();
                        InsertGrabMission(mission.MissionId);
                        IceLogging.Info("Going to \" Insert Grab Mission Task\" next", "[Task: Find Mission | Check Critical]");
                        return true;
                    }
                }

                IceLogging.Info("No mission was found under the critical tab, continuing onto the next", "[Critical Mission Check]");
                return true;
            }

            return false;
        }
        public static bool? CheckStandard()
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                Dictionary<uint, int> visibleCount = new()
                {
                    [100] = 0,
                    [90] = 0,
                    [50] = 0,
                    [10] = 0,
                };

                foreach (var mission in x.StellerMissions)
                {
                    var level = CosmicHelper.SheetMissionDict[mission.MissionId].Level;
                    visibleCount[level] += 1;
                }

                List<string> RankPriority = new() { "ExA", "A", "B", "C", "D" };

                foreach (var rankType in RankPriority)
                {
                    // Get the appropriate HashSet for this rank
                    List<uint> MissionList = rankType switch
                    {
                        "ExA" => ExARankMissions,
                        "A" => ARankMissions,
                        "B" => BRankMissions,
                        "C" => CRankMissions,
                        "D" => DRankMissions,
                        _ => new List<uint>()
                    };

                    // Skip if no missions configured for this rank
                    if (MissionList.Count == 0)
                        continue;

                    // Look for missions of this rank type
                    foreach (var mission in x.StellerMissions.Where(m => MissionList.Contains(m.MissionId)))
                    {
                        mission.Select();
                        InsertGrabMission(mission.MissionId);
                        IceLogging.Debug($"Mission was found!: {mission.MissionId}. Activating it/inserting stack to queue up mission grab");
                        return true; // Found and processed a mission
                    }
                }

                if (C.XPLeveling_Mode)
                {


                    if (BRankMissions.Count > 0 && visibleCount[90] == 0)
                    {
                        IceLogging.Debug("We're missing B Rank missions to farm");
                        IceLogging.Debug("Searching for viable mission to level");
                        // We're expectd to have B ranks for rankings, so going to do the standard check to see what we can do...
                        if (visibleCount[50] > 0)
                        {
                            IceLogging.Debug("Searching viable C Rank Missions");
                            // Good, we have visible C ranks missions. Going to aim to just complete one of these that we're missing
                            var mission = x.StellerMissions
                                .Where(m => CosmicHelper.Unlock_MissionList.Contains(m.MissionId))
                                .Where(m => CosmicHelper.SheetMissionDict[m.MissionId].Level == 50)
                                .Where(m => !MissionCompleted(m.MissionId))
                                .FirstOrDefault();

                            if (mission != null)
                            {
                                mission.Select();
                                InsertGrabMission(mission.MissionId);
                                IceLogging.Debug($"Mission was found that needs completed to unlock the lv. 90 missions");
                                return true;
                            }
                        }
                        else if (visibleCount[10] > 0)
                        {
                            IceLogging.Debug("Searching Viable D rank Missions");
                            // This should always be true, but reguardless *-JUST-* in case
                            // This is on the off chance this is their first time stepping foot in here.
                            var mission = x.StellerMissions
                                .Where(m => CosmicHelper.Unlock_MissionList.Contains(m.MissionId))
                                .Where(m => CosmicHelper.SheetMissionDict[m.MissionId].Level == 10)
                                .Where(m => !MissionCompleted(m.MissionId))
                                .FirstOrDefault();

                            if (mission != null)
                            {
                                mission.Select();
                                InsertGrabMission(mission.MissionId);
                                IceLogging.Debug($"Mission was found that needs completed to unlock the lv. 50 missions");
                                return true;
                            }
                        }
                    }
                    else if (CRankMissions.Count > 0 && visibleCount[50] == 0)
                    {
                        if (visibleCount[10] > 0)
                        {
                            // This should always be true, but reguardless *-JUST-* in case
                            // This is on the off chance this is their first time stepping foot in here.
                            var mission = x.StellerMissions
                                .Where(m => CosmicHelper.Unlock_MissionList.Contains(m.MissionId))
                                .Where(m => CosmicHelper.SheetMissionDict[m.MissionId].Level == 10)
                                .Where(m => !MissionCompleted(m.MissionId))
                                .FirstOrDefault();

                            if (mission != null)
                            {
                                mission.Select();
                                InsertGrabMission(mission.MissionId);
                                IceLogging.Debug($"Mission was found that needs completed to unlock the lv. 50 missions");
                                return true;
                            }
                        }
                    }
                }

                // No mission was found, time to kick in Mr. Resetti
                IceLogging.Debug("No mission was found in standard", "[FindMission: CheckStandard]");
                P.TaskManager.Insert(() => CheckReroll(), "Checking Re-Roll Status");
                IceLogging.Debug("Initating the check reroll", "[FindMission: CheckStandard]");
                return true;
            }

            return false;
        }
        private static unsafe bool? CheckExp()
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                var bestIndex = FindBestRelicMission();

                if (bestIndex > 0)
                {
                    IceLogging.Debug($"A mission was found for the xp grind: {bestIndex}.");
                    var selectedMission = x.StellerMissions.Where(x => x.MissionId == bestIndex).FirstOrDefault();

                    if (selectedMission != null)
                    {
                        selectedMission.Select();
                        InsertGrabMission(selectedMission.MissionId);
                        return true;
                    }
                }
                else
                {
                    IceLogging.Debug("\n" +
                                     "Somehow, you manage to find absolutely no missions. That's actually impressive.\n" +
                                     "You might have one of the following issues:\n" +
                                     "1: Ignore Manual Mode is enabled, and you have all missions set to manual mode.\n" +
                                     "2: Only Enabled Missions is on, and you have a very limited pool of missions that somehow missed the mark\n" +
                                     "If it's 2, then this should go on to re-roll for you (assuming that you have ATLEAST 1 mission enabled somewhere...\n" +
                                     "Checking this now", "[Xp Grind]");

                    if (C.XPRelicOnlyEnabled && BasicMissionCount != 0)
                    {
                        IceLogging.Debug($"Only relic grind was enabled. Continuing to re-roll mission now");
                        HashSet<uint> EnabledMissions = new();
                        foreach (var mission in C.MissionConfig.Where(x => x.Value.Enabled && SheetMissionDict[x.Key].Jobs.Contains((uint)Player.Job)))
                        {
                            EnabledMissions.Add(mission.Key);
                        }


                        string exARankStr = string.Join(", ", ExARankMissions);
                        string aRankStr = string.Join(", ", ARankMissions);
                        string bRankStr = string.Join(", ", BRankMissions);
                        string cRankStr = string.Join(", ", CRankMissions);
                        string dRankStr = string.Join(", ", DRankMissions);
                        string enabledStr = string.Join(", ", EnabledMissions);

                        IceLogging.Debug($"ExA Rank: {exARankStr}");
                        IceLogging.Debug($"A Rank: {aRankStr}");
                        IceLogging.Debug($"B Rank: {bRankStr}");
                        IceLogging.Debug($"C Rank: {cRankStr}");
                        IceLogging.Debug($"D Rank: {dRankStr}");
                        IceLogging.Debug($"Enabled Missions: {enabledStr}");
                        IceLogging.Debug($"Planet: {Player.Territory}");
                        P.TaskManager.Insert(() => FindReroll(), "Finding Reroll mission for Relic Grind");
                        return true;
                    }
                    else
                    {
                        IceLogging.Error($"Okay, something is wrong. Stopping the process", "[Relic Grind]");
                        SchedulerMain.DisablePlugin();
                    }
                }
            }

            return false;
        }
        public static unsafe uint? FindBestRelicMission()
        {
            string tip = "[Relic XP Finder]";

            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                var maxStage = CosmicHelper.MaxRelicLevel;

                var wksManager = WKSManager.Instance();
                if (wksManager == null || wksManager->ResearchModule == null || !wksManager->ResearchModule->IsLoaded)
                    return null;

                var job = (uint)Player.Job;
                var toolClassId = (byte)(job - 7);
                var stage = wksManager->ResearchModule->CurrentStages[toolClassId - 1];
                var nextstate = wksManager->ResearchModule->UnlockedStages[toolClassId - 1];

                if (Svc.Data.GetExcelSheet<WKSCosmoToolClass>().TryGetRow(toolClassId, out var row)) { }

                Dictionary<int, CosmicHelper.XPType> XPTable = new Dictionary<int, CosmicHelper.XPType>();

                if (C.UseDummyXp)
                {
                    XPTable = C.DummyXP;
                }
                else
                {
                    if (stage != maxStage)
                    {
                        for (byte type = 1; type < 7; type++)
                        {
                            if (!wksManager->ResearchModule->IsTypeAvailable(toolClassId, type))
                                break;

                            var neededXP = wksManager->ResearchModule->GetNeededAnalysis(toolClassId, type);

                            var currentXp = wksManager->ResearchModule->GetCurrentAnalysis(toolClassId, type);
                            var requiredXp = neededXP - currentXp;
                            if (!XPTable.ContainsKey(type))
                            {
                                XPTable[type] = new XPType()
                                {
                                    CurrentXP = currentXp,
                                    NeededXP = neededXP,
                                };
                            }
                        }
                    }
                    else
                    {
                        for (byte type = 1; type < 7; type++)
                        {
                            if (!wksManager->ResearchModule->IsTypeAvailable(toolClassId, type))
                                break;

                            var maxXP = wksManager->ResearchModule->GetMaxAnalysis(toolClassId, type);

                            var currentXp = wksManager->ResearchModule->GetCurrentAnalysis(toolClassId, type);
                            var requiredXp = maxXP - currentXp;
                            if (!XPTable.ContainsKey(type))
                            {
                                XPTable[type] = new XPType()
                                {
                                    CurrentXP = currentXp,
                                    NeededXP = maxXP,
                                };
                            }
                        }
                    }
                }

                var urgencies = new Dictionary<int, float>();
                for (int i = 0; i < XPTable.Count; i++)
                {
                    var bar = XPTable[i + 1];
                    urgencies[i + 1] = bar.NeededXP > 0 ? 1f - (float)bar.CurrentXP / bar.NeededXP : 0f;
                    IceLogging.Debug($"XP Type: {i+1} | Urgency: {urgencies[i + 1]}", tip);
                }

                Dictionary<uint, Dictionary<int, float>> rewardMissions = new();
                foreach (var availMission in x.StellerMissions)
                {
                    var id = availMission.MissionId;
                    if (CosmicHelper.SheetMissionDict.TryGetValue(id, out var mission))
                    {
                        var missionConfig = C.MissionConfig[id];

                        int minLevel = 10;
                        var rank = mission.Rank;
                        switch (rank)
                        {
                            case 1:
                                minLevel = 10;
                                break;
                            case 2:
                                minLevel = 50;
                                break;
                            case 3:
                                minLevel = 90;
                                break;
                            case 4:
                            case 5:
                            case 6:
                                minLevel = 100;
                                break;
                            default:
                                minLevel = 10;
                                break;
                        }

                        bool properLevel = Player.Level >= minLevel;
                        bool IgnoreManual = C.XPRelicIgnoreManual && missionConfig.ManualMode;
                        bool IgnoreNotEnabled = C.XPRelicOnlyEnabled && !missionConfig.Enabled;
                        bool unSupported = UnsupportedMissions.Ids.Contains(id);
                        bool dualClass = mission.Jobs.Count == 2;
                        bool skipDualClass = false;
                        if (dualClass)
                        {
                            skipDualClass = !(Player.GetLevel((Job)mission.Jobs[0]) >= 100 && Player.GetLevel((Job)mission.Jobs[1]) >= 100);
                        }

                        PlayerHelper.UpdateHasManip();
                        var jobId = mission.Jobs.Where(x => CosmicHelper.CrafterJobList.Contains(x)).FirstOrDefault();

                        bool manipUnlocked = PlayerHelper.ManipClassInfo.TryGetValue(jobId, out var manipInfo) && manipInfo.HasUnlocked;
                        bool isManipReq = mission.Attributes.HasFlag(MissionAttributes.ExpertCraft);

                        IceLogging.Debug($"[Mission: {id}]" +
                                         $"Is proper Level: {properLevel}\n | Mission Level: {minLevel} | Player Level: {Player.Level} \n" +
                                         $"Ignoring cause of manual? {IgnoreManual}\n" +
                                         $"Ignoring cuase of not enabled: {IgnoreNotEnabled}\n" +
                                         $"Ignoring because of not supported: {unSupported}\n" +
                                         $"Is Manipulation required: {isManipReq}\n" +
                                         $"Is Manipluation even unlocked: {manipUnlocked}\n", tip);

                        if (!properLevel) continue;
                        if (IgnoreManual) continue;
                        if (IgnoreNotEnabled) continue;
                        if (unSupported) continue;
                        if (isManipReq && !manipUnlocked) continue;
                        if (skipDualClass) continue;

                        Dictionary<int, float> rewardDict = new();
                        foreach (var reward in mission.RelicXpInfo.OrderBy(x => x.Key))
                        {
                            rewardDict[reward.Key] = reward.Value;
                        }
                        rewardMissions[id] = rewardDict;
                        IceLogging.Debug($"Adding {id} to the potentional missions for relic xp");
                    }
                }

                uint? bestMissionId = null;
                float bestScore = float.NegativeInfinity;

                foreach (var kvp in rewardMissions)
                {
                    uint missionId = kvp.Key;
                    var reward = kvp.Value;
                    float score = 0f;
                    IceLogging.Debug($"Currently checking mission: {missionId}");

                    foreach (var rewardEntry in reward)
                    {
                        IceLogging.Debug($"Checking for value: {rewardEntry.Key}");
                        if (urgencies.TryGetValue(rewardEntry.Key, out var urgency))
                        {
                            IceLogging.Info($"Checking urgency for: {rewardEntry.Key}");
                            float contribution = urgency * rewardEntry.Value;

                            // Only add positive contributions (high urgency rewards)
                            if (contribution > 0)
                            {
                                score += contribution;
                                IceLogging.Info($"Adding positive score: {contribution}");
                            }
                            else
                            {
                                IceLogging.Debug($"Skipping negative score: {contribution}");
                            }
                        }
                    }

                    // Compare this mission's score with the current best
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMissionId = missionId;
                        IceLogging.Info($"New best mission: {missionId} with score: {score}");
                    }
                }

                return bestMissionId;
            }
            else
            {
                return null;
            }
        }
        public static unsafe bool? CheckAllProvisional()
        {
            IceLogging.Debug($"Weather Mission Count: {WeatherMissions.Count}");
            IceLogging.Debug($"Sequence Mission Count: {SequenceMissions.Count}");
            IceLogging.Debug($"Timed Mission Count: {TimedMissions.Count}");

            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                foreach (var missionType in C.MissionPrio)
                {
                    List<uint> missionIds = missionType switch
                    {
                        ProvisionalTypes.ProvisionalWeather => WeatherMissions,
                        ProvisionalTypes.ProvisionalSequential => SequenceMissions,
                        ProvisionalTypes.ProvisionalTimed => TimedMissions,
                        _ => new List<uint>()
                    };

                    if (missionIds.Count == 0)
                        continue;

                    foreach (var id in missionIds)
                    {
                        var mission = x.StellerMissions.Where(x => x.MissionId == id).FirstOrDefault();
                        if (mission != null)
                        {
                            mission.Select();
                            InsertGrabMission(mission.MissionId);
                            return true;
                        }
                    }
                }
            }

            return true;
        }
        public static bool? FindReroll()
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                List<uint> AExRank = new List<uint>();
                List<uint> ARank = new List<uint>();
                List<uint> BRank = new List<uint>();
                List<uint> CRank = new List<uint>();
                List<uint> DRank = new List<uint>();

                // Track mission appearance counts
                foreach (var mission in x.StellerMissions)
                {
                    var missionId = mission.MissionId;

                    // Increment appearance count
                    if (!Mission_Settings.missionAppearanceCounts.ContainsKey(missionId))
                        Mission_Settings.missionAppearanceCounts[missionId] = 0;
                    Mission_Settings.missionAppearanceCounts[missionId]++;

                    var rank = CosmicHelper.SheetMissionDict[missionId].Rank;
                    switch (rank)
                    {
                        case 5: AExRank.Add(missionId); break;
                        case 4: ARank.Add(missionId); break;
                        case 3: BRank.Add(missionId); break;
                        case 2: CRank.Add(missionId); break;
                        case 1:
                        default: DRank.Add(missionId); break;
                    }
                }

                bool CheckARanks = (ExARankMissions.Count > 0 || ARankMissions.Count > 0) && (AExRank.Count > 0 || ARank.Count > 0);
                bool CheckBRanks = (BRankMissions.Count > 0 && BRank.Count > 0);
                bool CheckCRanks = (CRankMissions.Count > 0 && CRank.Count > 0);
                bool CheckDRanks = (DRankMissions.Count > 0 && DRank.Count > 0);

                var random = new Random();
                void ShuffleList<T>(List<T> list, Random rnd)
                {
                    for (int i = list.Count - 1; i > 0; i--)
                    {
                        int j = rnd.Next(i + 1);
                        (list[i], list[j]) = (list[j], list[i]);
                    }
                }

                ShuffleList(AExRank, random);
                ShuffleList(ARank, random);
                ShuffleList(BRank, random);
                ShuffleList(CRank, random);
                ShuffleList(DRank, random);

                // small function to find a frequent mission that might be locking us
                uint FindFrequentMission(List<uint> missionList, int threshold = 3)
                {
                    foreach (var missionId in missionList)
                    {
                        if (CosmicHelper.SheetMissionDict.TryGetValue(missionId, out var mission))
                        {
                            if (mission.Jobs.Count == 2)
                            {
                                if (!(Player.GetLevel((Job)mission.Jobs[0]) >= 100 && Player.GetLevel((Job)mission.Jobs[1]) >= 100))
                                    continue;
                            }
                        }

                        if (Mission_Settings.missionAppearanceCounts.TryGetValue(missionId, out int count) && count >= threshold)
                        {
                            return missionId;
                        }
                    }
                    return 0;
                }

                if (CheckARanks)
                {
                    // Check for frequent missions in A/AEx ranks first
                    uint frequentAEx = FindFrequentMission(AExRank, Mission_Settings.rerollThreshold);
                    uint frequentA = FindFrequentMission(ARank, Mission_Settings.rerollThreshold);

                    if (AExRank.Count > 2)
                    {
                        if (frequentAEx != 0)
                        {
                            missionToAbandon = frequentAEx;
                            IceLogging.Debug($"Abandoning frequently appearing AEX mission (appeared {Mission_Settings.missionAppearanceCounts[frequentAEx]} times)");
                            Mission_Settings.previouslyAbandoned = 5;
                        }
                        else
                        {
                            IceLogging.Debug($"Only AEX Rank missions are available. Forcing an AEX rank to be accepted");
                            missionToAbandon = AExRank.First();
                            Mission_Settings.previouslyAbandoned = 5;
                        }
                    }
                    else if (ARank.Count > 2)
                    {
                        if (frequentA != 0)
                        {
                            missionToAbandon = frequentA;
                            IceLogging.Debug($"Abandoning frequently appearing A mission (appeared {Mission_Settings.missionAppearanceCounts[frequentA]} times)");
                            Mission_Settings.previouslyAbandoned = 4;
                        }
                        else
                        {
                            IceLogging.Debug($"Only A Rank missions are available. Forcing an A rank to be accepted");
                            missionToAbandon = ARank.First();
                            Mission_Settings.previouslyAbandoned = 4;
                        }
                    }
                    else
                    {
                        if (Mission_Settings.previouslyAbandoned == 5)
                        {
                            if (frequentA != 0)
                            {
                                missionToAbandon = frequentA;
                                IceLogging.Debug($"Abandoning frequently appearing A mission (appeared {Mission_Settings.missionAppearanceCounts[frequentA]} times)");
                                Mission_Settings.previouslyAbandoned = 4;
                            }
                            else
                            {
                                missionToAbandon = ARank.First();
                                IceLogging.Debug($"Abandoning Rank 4 Mission.");
                                Mission_Settings.previouslyAbandoned = 4;
                            }
                        }
                        else if (Mission_Settings.previouslyAbandoned == 4)
                        {
                            if (frequentAEx != 0)
                            {
                                missionToAbandon = frequentAEx;
                                IceLogging.Debug($"Abandoning frequently appearing AEX mission (appeared {Mission_Settings.missionAppearanceCounts[frequentAEx]} times)");
                                Mission_Settings.previouslyAbandoned = 5;
                            }
                            else
                            {
                                missionToAbandon = AExRank.First();
                                IceLogging.Debug($"Abandoning Rank 5 Mission");
                                Mission_Settings.previouslyAbandoned = 5;
                            }
                        }
                        else
                        {
                            missionToAbandon = ARank.First();
                            IceLogging.Debug($"Starting off w/ abandoning an A rank");
                            Mission_Settings.previouslyAbandoned = 4;
                        }
                    }
                }
                else if (CheckBRanks)
                {
                    uint frequentB = FindFrequentMission(BRank, Mission_Settings.rerollThreshold);
                    if (frequentB != 0)
                    {
                        missionToAbandon = frequentB;
                        IceLogging.Debug($"Abandoning frequently appearing B mission (appeared {Mission_Settings.missionAppearanceCounts[frequentB]} times)");
                    }
                    else
                    {
                        missionToAbandon = BRank.First();
                    }
                    Mission_Settings.previouslyAbandoned = 3;
                }
                else if (CheckCRanks)
                {
                    uint frequentC = FindFrequentMission(CRank, Mission_Settings.rerollThreshold);
                    if (frequentC != 0)
                    {
                        missionToAbandon = frequentC;
                        IceLogging.Debug($"Abandoning frequently appearing C mission (appeared {Mission_Settings.missionAppearanceCounts[frequentC]} times)");
                    }
                    else
                    {
                        missionToAbandon = CRank.First();
                    }
                    Mission_Settings.previouslyAbandoned = 2;
                }
                else if (CheckDRanks)
                {
                    uint frequentD = FindFrequentMission(DRank, Mission_Settings.rerollThreshold);
                    if (frequentD != 0)
                    {
                        missionToAbandon = frequentD;
                        IceLogging.Debug($"Abandoning frequently appearing D mission (appeared {Mission_Settings.missionAppearanceCounts[frequentD]} times)");
                    }
                    else
                    {
                        missionToAbandon = DRank.First();
                    }
                    Mission_Settings.previouslyAbandoned = 1;
                }
                else if (C.XPLeveling_Mode)
                {
                    // If we've gotten this far, that means we need to find a mission to reroll for us to properly level
                    if (BRankMissions.Count > 0)
                    {
                        IceLogging.Debug("Leveling mode is active. Need to find a valid C or D Rank mission");
                        var mission = x.StellerMissions.Where(m => CosmicHelper.SheetMissionDict[m.MissionId].Level == 50).FirstOrDefault();

                        if (mission != null)
                        {
                            missionToAbandon = mission.MissionId;
                        }
                        else
                        {
                            mission = x.StellerMissions.Where(m => CosmicHelper.SheetMissionDict[m.MissionId].Level == 10).FirstOrDefault();

                            if (mission != null)
                                missionToAbandon = mission.MissionId;
                        }
                    }
                    else if (CRankMissions.Count > 0)
                    {
                        var mission = x.StellerMissions.Where(m => CosmicHelper.SheetMissionDict[m.MissionId].Level == 10).FirstOrDefault();
                        if (mission != null)
                            missionToAbandon = mission.MissionId;
                    }
                }

                if (missionToAbandon != 0)
                {
                    var abandonMission = x.StellerMissions.First(m => m.MissionId == missionToAbandon);
                    abandonMission.Select();
                    P.TaskManager.Insert(() => GrabMission(missionToAbandon, true), "Going to abandon mission now");
                    IceLogging.Debug($"Attempting to abandon mission ID: {missionToAbandon} (Rank: {CosmicHelper.SheetMissionDict[missionToAbandon].Rank})");
                    Mission_Settings.missionAppearanceCounts[missionToAbandon] = 0;

                    return true;
                }
                else
                {
                    IceLogging.Debug("No valid missions found to abandon");
                    return false;
                }
            }

            return false;
        }
        public static void InsertGrabMission(uint missionId)
        {
            P.TaskManager.InsertMulti(
                new(() => ChangeJob(missionId), "Changing jobs for standard mission"),
                new(() => Navmesh_MoveToMission(missionId), "Checking if movement is necessary", Utils.TaskConfig),
                new(() => GrabMission(missionId), "Selecting mission for grabbing")
            );
        }
        private static unsafe void InitiateMission(uint missionId)
        {
            var WKSInstance = WKSManager.Instance();
            WKSInstance->MissionModule->InitiateMission((ushort)missionId);
        }
        private static bool? GrabMission(uint missionId, bool reroll = false)
        {
            if (CosmicHelper.CurrentLunarMission != 0)
            {
                Mission_Settings.ResetNodeCounter();

                if (reroll)
                {
                    SchedulerMain.State = IceState.AbandonMission;
                    Task_AbandonMission.ForceAbandon = true;
                }
                else
                {
                    SchedulerMain.State = IceState.ExecutingMission;
                    Task_AbandonMission.ForceAbandon = false;
                }
                Mission_Settings.nodeTotal = 0;
                P.TaskManager.Tasks.Clear();
                timeoutAmount = 0;
                IceLogging.Debug($"State upon exiting: {SchedulerMain.State}");
                return true;
            }
            else
            {
                IceLogging.Verbose($"Selecting the mission: {missionId}");
                if (EzThrottler.Throttle("Selecting said mission (or attempting to)", 1000))
                {
                    InitiateMission(missionId);
                    timeoutAmount += 1;
                    IceLogging.Debug($"Are we expected to reroll? {reroll}", "[Grab Mission]");
                }

                if (timeoutAmount >= maxTimeout)
                {
                    IceLogging.Debug("We seem to have grabbed an invalid mission, so we just gonna reset it all");
                    SchedulerMain.State = IceState.GrabMission;
                    P.TaskManager.Tasks.Clear();
                    timeoutAmount = 0;

                    return true;
                }
            }

            return false;
        }
        public static unsafe bool? Navmesh_MoveToMission(uint missionId)
        {
            ThrottleMessage("Currently in a navmesh movement");

            var missionEntry = CosmicHelper.SheetMissionDict[missionId];
            var missionConfig = C.MissionConfig[missionId];
            var currentJob = (uint)Player.Job;

            if (!missionEntry.Jobs.Contains(currentJob))
            {
                IceLogging.Error("Somehow, we've managed to get a job that isn't suppose to be an option for our jobs?? Resetting the whole process and going to try again.\n" +
                                 "If this continues on multiple times in a row, let me know.");
                SchedulerMain.State = IceState.GrabMission;
                P.TaskManager.Tasks.Clear();
                return true;
            }

            if (missionConfig.ManualMode || UnsupportedMissions.Ids.Contains(missionId))
            {
                // TODO: Remove the extra 2 here until I can fix pathfinding thing
                // This is here to make sure that you don't need to be in the area for moving. Mainly cause nodes aren't mapped out yet and it's expecting to map to that area...
                IceLogging.Info("Found a mission that is either in Manual mode, or unsupported. Continuing on", "[FindMission: NavmeshMoveTo]");
                return true;
            }
            else if (!P.Navmesh.Installed)
            {
                IceLogging.Error("HEY. YOU DIDN'T READ THE INFO PAGE DID YOU HUH. Navmesh isn't installed.... sooo... yeah this is unfort. Read the info page on the main page. I ain't going to hold your hand on this one");
                return true;
            }
            else if (missionEntry.Attributes.HasFlag(MissionAttributes.Gather))
            {
                // Mission was found to be a gathering or critical mission, seeing if you're within range of it
                Vector2 PlayerPos = new Vector2(Player.Position.X, Player.Position.Z);
                Vector2 MapCenter = missionEntry.MapPosition;
                var missionTerritory = missionEntry.TerritoryId;
                var mapId = missionEntry.MapPosition;
                var gatherInfo = GatheringRouteLoader.GetRoute(missionTerritory, mapId);

                if (gatherInfo.Count == 0)
                {
                    IceLogging.Info("HEY. This gathering location hasn't been set to gather, and should honestly be set to a manual state. Cause things are about to bug out. If it's a new area please let me know o/");
                    return true;
                }

                Vector3 closestNode = gatherInfo[0].LandZone;

                if (!P.Navmesh.IsRunning())
                {
                    foreach (var node in gatherInfo)
                    {
                        if (Player.DistanceTo(node.Position) < 5)
                        {
                            IceLogging.Debug("We're close to a gathering node. So continuing on.");
                            return true;
                        }
                    }
                }
                
                if (!Task_NavmeshMove.Task_NavTo(closestNode).Value)
                {
                    return false;
                }
            }
            else if (missionEntry.Attributes.HasFlag(MissionAttributes.Fish))
            {
                // Just a way to handle fishing missions specifically (that isn't under the critical unbrella).
                // Need to generate a path to the pre-set spot per fishing hole
                // Then need to add the last path to it once the base has been generated, to make sure that you're facing to the fishing hole properly.
                var location = missionEntry.MapPosition;
                var territory = missionEntry.TerritoryId;
                var fishingHole = GatheringUtil.MoonFishingLocations[territory][location];

                if (fishingHole == null || fishingHole.Count == 0)
                {
                    IceLogging.Info("We've seemed to have ran into a problem with the fishing hole... either it's missing spots, or it doesn't exist. Please report back to me on this with logs leading up to this");
                    IceLogging.Info($"Mission ID: {missionId} | Map Position: {missionEntry.MapPosition} | Moon Territory: {missionEntry.TerritoryId}");
                }

                if (!P.Navmesh.IsRunning())
                {
                    foreach (var fishSpot in fishingHole)
                    {
                        if (Player.DistanceTo(fishSpot.FishingSpot) < 3)
                        {
                            IceLogging.Info("We're currently at a fishing spot! Continuing onto facing position -> grabbing the mission");
                            fishingHoleLoc = Vector3.Zero;
                            return true;
                        }
                    }
                }

                if (fishingHoleLoc == Vector3.Zero)
                {
                    var _random = new Random();
                    var randomIndex = _random.Next(fishingHole.Count);
                    if (EzThrottler.Throttle("Setting destination"))
                    {
                        IceLogging.Debug($"Random number generator said we're going to the following fishing hole #: {randomIndex}");
                        fishingHoleLoc = fishingHole[randomIndex].FishingSpot;
                    }
                }
                else
                {
                    if (!Task_NavmeshMove.Task_NavTo(fishingHoleLoc).Value)
                    {
                        if (EzThrottler.Throttle("Waitin for nav to finish"))
                        {
                            IceLogging.Debug($"Waiting for navmesh to get to: {fishingHoleLoc}\n" +
                                             $"Current distance: {Player.DistanceTo(fishingHoleLoc)}\n" +
                                             $"Currently at: {Player.Position:N2}");
                        }
                        return false;
                    }
                    else
                    {
                        fishingHoleLoc = Vector3.Zero;
                    }
                }
            }
            else if (C.PersonalReturnSpot)
            {


                if (missionEntry.Attributes.HasFlag(MissionAttributes.Critical))
                {
                    IceLogging.Debug("We ideally don't want to return back to a spot if it's a critical. So we're going to stay put");
                    return true;
                }
                else
                {
                    var territory = Player.Territory.RowId;
                    if (C.CrafterLocations.TryGetValue(territory, out var location))
                    {
                        if (!Task_NavmeshMove.Task_NavTo(location).Value)
                        {
                            if (EzThrottler.Throttle("Log message", 1000))
                                IceLogging.Debug("Moving to crafting spot");

                            return false;
                        }
                        else
                        {
                            IceLogging.Debug("We're at the spot for crafter location!");
                            return true;
                        }
                    }
                    else
                    {
                        IceLogging.Debug("No location is set for this place, so continuing on");
                        return true;
                    }
                }
            }
            else
            {
                IceLogging.Debug("Mission was not a gathering or critical mission. Navmesh moving was not necessary. Moving onto next step", "[Task_FindMission: Navmesh]");
                return true;
            }

            return false;
        }
        private static bool? CheckReroll()
        {
            if (SchedulerMain.State == IceState.ExecutingMission)
            {
                IceLogging.Debug("No reason to reroll, you found a proper mission");
            }
            else
            {
                IceLogging.Debug("No mission was found, time for rerolling!", "[Check Reroll]");

                P.TaskManager.Insert(() => OpenTab("Reset"), "Opening tab to the reset mission");
                IceLogging.Debug("Task for re-roll thrown in", "[Check Reroll]");
            }
            return true;
        }
        public static bool? FrameDelay(int amount)
        {
            P.TaskManager.InsertDelay(amount, true);
            return true;

        }
        private static bool? ChangeJob(uint missionId)
        {
            IceLogging.Debug($"Starting to change job");

            var mission = CosmicHelper.SheetMissionDict[missionId];
            var jobId = mission.Jobs.First();

            if ((uint)Player.Job == jobId)
                return true;
            else
            {
                if (EzThrottler.Throttle("Swapping to job for mission"))
                {
                    GearsetHandler.TaskClassChange((Job)jobId);
                    IceLogging.Debug($"Swapping to job: {jobId}");
                }
                return false;
            }
        }
        private static void ThrottleMessage(string s)
        {
            if (EzThrottler.Throttle($"{s} _ message", 2000))
            {
                IceLogging.Debug(s);
            }
        }
        private static void MinimumRequirementsMet()
        {
            // Just jotting down notes here for requirements to unlock new missions
            // A Rank Requirements: Lv. 100 | 5 unique class D missions | 3 B missions with gold star
            // B Rank Requirements: Lv. 90 | 4 unique class D missions
            // C Rank Requirements: Lv. 50 | 3 unique class D missions
        }
        private static unsafe bool MissionCompleted(uint id)
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return true;

            var manager = (WKSManagerCustom*)managerPtr;
            var isCompleted = manager->IsMissionCompleted(id);

            return isCompleted;
        }
    }
}
