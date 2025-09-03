using ECommons.Automation;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Ui;
using ICE.Ui.DebugWindowTabs;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ECommons.GenericHelpers;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskMissionFind
    {
        private static uint MissionId = 0;
        private static uint? currentClassJob => PlayerHelper.GetClassJobId();
        private static bool isGatherer => currentClassJob >= 16 && currentClassJob <= 18;

        private static uint LockedOutCounter = 0;
        public static HashSet<uint> BlacklistedMission = [];

        private static IEnumerable<CosmicMission> CriticalMissions => C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Type == MissionType.Critical && x.Enabled);
        private static IEnumerable<CosmicMission> WeatherMissions => C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Type == MissionType.Weather && x.Enabled).Where(x => !BlacklistedMission.Contains(x.Id));
        private static IEnumerable<CosmicMission> TimedMissions => C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Type == MissionType.Timed && x.Enabled).Where(x => !BlacklistedMission.Contains(x.Id));
        private static IEnumerable<CosmicMission> SequenceMissions => C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Enabled).Where(x => x.Type == MissionType.Sequential && C.Missions.Any(y => y.PreviousMissionId == x.Id)); // might be bad logic but should work and these fields arent used rn anyway
        private static IEnumerable<CosmicMission> StandardMissions => C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Type == MissionType.Standard && x.Enabled);
        private static IEnumerable<CosmicMission> A2Missions => C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Type == MissionType.Standard && CosmicHelper.MissionInfoDict[x.Id].Rank == 5 && x.Enabled);
        private static IEnumerable<CosmicMission> A1Missions => C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Type == MissionType.Standard && CosmicHelper.MissionInfoDict[x.Id].Rank == 4 && x.Enabled);
        private static IEnumerable<CosmicMission> BMissions => C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Type == MissionType.Standard && CosmicHelper.MissionInfoDict[x.Id].Rank == 3 && x.Enabled);
        private static IEnumerable<CosmicMission> CMissions => C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Type == MissionType.Standard && CosmicHelper.MissionInfoDict[x.Id].Rank == 2 && x.Enabled);
        private static IEnumerable<CosmicMission> DMissions => C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Type == MissionType.Standard && CosmicHelper.MissionInfoDict[x.Id].Rank == 1 && x.Enabled);

        private static bool HasCritical => CriticalMissions.Any();
        private static bool HasWeather => WeatherMissions.Any();
        private static bool HasTimed => TimedMissions.Any();
        private static bool HasSequence => SequenceMissions.Any();
        private static bool HasStandard => StandardMissions.Any();
        private static bool HasA2 => A2Missions.Any();
        private static bool HasA1 => A1Missions.Any();
        private static bool HasB => BMissions.Any();
        private static bool HasC => CMissions.Any();
        private static bool HasD => DMissions.Any();

        public static void Enqueue()
        {
            // 
            if (SchedulerMain.AnimationLockAbandonState)
            {
                SchedulerMain.State = IceState.AnimationLock;
                return;
            }

            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            // Checks to see if you've hit the following:
            // Cosmocredits
            // Lunarcredits
            // Cosmo score
            // Target Level
            // Stopping after the current mission
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

            if (C.StopOnceHitCosmoCredits)
            {
                if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
                {
                    if (hud.CosmoCredit >= C.CosmoCreditsCap)
                    {
                        DuoLog.Information($"Stopping the plugin as you have {hud.CosmoCredit} Cosmocredits.");
                        SchedulerMain.DisablePlugin();
                        return;
                    }
                }
            }

            if (C.StopOnceHitLunarCredits)
            {
                if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
                {
                    if (hud.LunarCredit >= C.LunarCreditsCap)
                    {
                        DuoLog.Information($"Stopping the plugin as you have {hud.LunarCredit} Lunar Credits");
                        SchedulerMain.DisablePlugin();
                        return;
                    }
                }
            }

            if (C.StopOnceHitCosmicScore)
            {
                var (classScore, _, _, _) = MissionHandler.GetCosmicClassScores();
                if (classScore >= C.CosmicScoreCap)
                {
                    DuoLog.Information($"Stopping the plugin as you have {classScore} Cosmic Score");
                    SchedulerMain.DisablePlugin();
                    return;
                }
            }

            if (Player.Level >= C.TargetLevel && C.StopWhenLevel)
            {
                DuoLog.Information($"Stopping the plugin as you have reached level {C.TargetLevel}");
                SchedulerMain.DisablePlugin();
                return;
            }
            if (SchedulerMain.StopBeforeGrab)
            {
                SchedulerMain.DisablePlugin();
                return;
            }

            if (!SchedulerMain.State.HasFlag(IceState.GrabMission))
            {
                // Don't know how we're here, but returning back to the start to make sure we're on the right path.
                // this... hmm... shouldn't be here I feel. Might be wrong. Not sure I like this
                return;
            }

            // Checking to see if you even HAVE any missions enabled to begin with.
            if (!(HasCritical || HasWeather || HasTimed || HasSequence || HasStandard))
            {
                // Esentially checking to make sure you don't have relic XP Grind here, due to wanting to automatically select the "best mission"
                if (!(C.XPRelicGrind && !C.XPRelicOnlyEnabled))
                {
                    DuoLog.Error($"No missions enabled for {Svc.ClientState.LocalPlayer?.ClassJob.Value.Name}. Did you forget to set me up?");
                    SchedulerMain.DisablePlugin();
                    return;
                }
            }

            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            // All checks have been done, time to actually initiate the code
            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

            P.TaskManager.Enqueue(TaskRepair.GatherCheck, "Checking for repairs");
            P.TaskManager.Enqueue(TaskSpiritbond.TryExtractMateria, "Checking for materia");

            P.TaskManager.Enqueue(UpdateValues, "Updating Task Mission Values");
            P.TaskManager.Enqueue(OpenMissionFinder, "Opening the Mission finder");
            if (C.XPRelicGrind)
            {
                P.TaskManager.Enqueue(BasicMissionButton, "Selecting Basic Missions");
                P.TaskManager.Enqueue(FindBestRelicXP, "Selecting Best Relic XP Mission");
                P.TaskManager.Enqueue(FindResetMission, "Checking Reset for Relic XP");
            }
            else
            {
                if (HasCritical)
                {
                    P.TaskManager.Enqueue(CriticalButton, "Selecting Critical Mission");
                    P.TaskManager.Enqueue(FindCriticalMission, "Checking to see if critical mission available");
                }
                if (HasWeather || HasTimed || HasSequence) // Skip Checks if enabled mission doesn't have weather, timed or sequence?
                {
                    P.TaskManager.Enqueue(WeatherButton, "Selecting Weather");
                    P.TaskManager.Enqueue(FindWeatherMission, "Checking to see if weather mission avaialable");
                }
                if (HasStandard)
                {
                    P.TaskManager.Enqueue(BasicMissionButton, "Selecting Basic Missions");
                    P.TaskManager.Enqueue(FindBasicMission, "Finding Basic Mission");
                    P.TaskManager.Enqueue(FindResetMission, "Checking for abandon mission");
                }
            }
            P.TaskManager.Enqueue(GrabMission, "Grabbing the mission");
            DelayMission();
            P.TaskManager.Enqueue(AbandonMission, "Checking to see if need to leave mission");
            P.TaskManager.Enqueue(async () =>
            {
                if (EzThrottler.Throttle("UI", 250))
                {
                    if (SchedulerMain.Abandon == true)
                    {
                        SchedulerMain.Abandon = false;
                        return;
                    }
                    else if (CosmicHelper.CurrentLunarMission != 0)
                    {
                        UpdateStateFlags();
                        return;
                    }
                }
            });
        }

        internal static void UpdateStateFlags()
        {
            CosmicMission? mission = C.Missions.SingleOrDefault(x => x.Id == CosmicHelper.CurrentLunarMission);
            if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Critical))
            {
                Dalamud.Game.ClientState.Objects.Types.IGameObject? gameobject = Utils.TryGetObjectCollectionPoint();
                if (gameobject is not null)
                    SchedulerMain.NearestCollectionPoint = gameobject.Position;
                else
                    SchedulerMain.NearestCollectionPoint = null;
            }
            if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Craft))
                SchedulerMain.State |= IceState.Craft;
            if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Gather))
            {
                SchedulerMain.State |= IceState.Gather;
                SchedulerMain.InitialGatheringItemMultiplier = mission.GatherSetting.InitialGatheringItemMultiplier;
                SchedulerMain.GathererBuffsUsed = [];
                uint missionNodeSetId = CosmicHelper.MissionInfoDict[CosmicHelper.CurrentLunarMission].NodeSet;
                List<GatheringUtil.GathNodeInfo> missionNode = [.. GatheringUtil.MoonNodeInfoList.Where(x => x.NodeSet == missionNodeSetId)];

                if (mission.GatherSetting.Pathfinding == 0 && missionNodeSetId != SchedulerMain.PreviousNodeSetId)
                {
                    SchedulerMain.CurrentNodeSet = missionNode;
                    SchedulerMain.CurrentIndex = 0;
                    SchedulerMain.PreviousNodeSetId = missionNodeSetId;
                }
                else if (mission.GatherSetting.Pathfinding == 1)
                {
                    var pathfinder = new GatheringPathfinder();
                    SchedulerMain.CurrentNodeSet = [.. pathfinder.SolveOpenEndedTSP(Player.Position, missionNode)];
                    SchedulerMain.CurrentIndex = 0;
                    SchedulerMain.PreviousNodeSetId = missionNodeSetId;
                }
                else if (mission.GatherSetting.Pathfinding == 2 && missionNodeSetId != SchedulerMain.PreviousNodeSetId)
                {
                    var pathfinder = new GatheringPathfinder();
                    SchedulerMain.CurrentNodeSet = [.. pathfinder.SolveCyclicalTSP(missionNode, mission.GatherSetting.TSPCycleSize)];
                    SchedulerMain.CurrentIndex = 0;
                    SchedulerMain.PreviousNodeSetId = missionNodeSetId;
                }
                SchedulerMain.NodesVisited = 0;
            }
            if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Fish))
                SchedulerMain.State |= IceState.Fish;
            if (mission.ManualMode || C.OnlyGrabMission)
                SchedulerMain.State |= IceState.ManualMode;
            SchedulerMain.State |= IceState.ExecutingMission;
            SchedulerMain.State &= ~IceState.GrabMission;
        }

        internal static bool? UpdateValues()
        {
            SchedulerMain.Abandon = false;
            SchedulerMain.MissionName = string.Empty;
            MissionId = 0;

            return true;
        }

        // Buttons to call, debating on just making these voids but. To the same degree there's no true call on 
        internal unsafe static bool? CriticalButton()
        {
            if (EzThrottler.Throttle("WKSUIButton", 250))
                if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    x.CriticalMissions();
                    return true;
                }
            return false;
        }
        internal unsafe static bool? WeatherButton()
        {
            if (EzThrottler.Throttle("WKSUIButton", 250))
                if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    x.ProvisionalMissions();
                    return true;
                }
            return false;
        }
        internal unsafe static bool? BasicMissionButton()
        {
            if (MissionId != 0)
            {
                return true;
            }
            if (EzThrottler.Throttle("WKSUIButton", 250))
                if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    x.BasicMissions();
                    return true;
                }
            return false;
        }

        internal unsafe static bool? OpenMissionFinder()
        {
            if (TryGetAddonMaster<WKSMission>("WKSMission", out var mission) && mission.IsAddonReady)
            {
                return true;
            }

            if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
            {
                if (EzThrottler.Throttle("OpenMissionFinder"))
                    hud.Mission();
            }

            return false;
        }

        internal unsafe static bool? FindCriticalMission()
        {
            if (EzThrottler.Throttle("FindCriticalMission"))
            {
                if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    x.ProvisionalMissions();
                    var currentClassJob = PlayerHelper.GetClassJobId();
                    foreach (var m in x.StellerMissions)
                    {
                        var criticalMissionEntry = C.Missions.Where(x => x.Enabled && x.JobId == currentClassJob).FirstOrDefault(e => e.Id == m.MissionId);

                        if (criticalMissionEntry == default)
                        {
                            IceLogging.Debug($"[Critical Mission] Critical mission entry is default. Which means id: {criticalMissionEntry}");
                            continue;
                        }

                        IceLogging.Debug($"Mission Name: {m.Name} | MissionId: {criticalMissionEntry.Id} has been found. Setting value for sending");
                        SelectMission(m);
                        break;
                    }
                }

                if (MissionId == 0)
                    IceLogging.Debug("No mission was found under weather, continuing on");
                return true;
            }
            return false;
        }

        internal unsafe static bool? FindWeatherMission()
        {
            if (MissionId != 0)
            {
                IceLogging.Debug("[Weather Mission] You already have a mission found, skipping finding weather mission");
                return true;
            }
            if (EzThrottler.Throttle("FindWeatherMission"))
            {
                if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    x.ProvisionalMissions();
                    var currentClassJob = PlayerHelper.GetClassJobId();

                    var weatherIds = C.Missions.Where(x => x.Type == MissionType.Weather).Select(w => w.Id).ToHashSet();
                    var sequenceIds = C.Missions.Where(x => x.Type == MissionType.Sequential).Select(s => s.Id).ToHashSet();
                    var timedIds = C.Missions.Where(x => x.Type == MissionType.Timed).Select(t => t.Id).ToHashSet();

                    var weatherMissions = x.StellerMissions.Where(m => !timedIds.Contains(m.MissionId) && !sequenceIds.Contains(m.MissionId)); // missions that aren't sequence or timed
                    var timedMissions = x.StellerMissions.Where(m => !weatherIds.Contains(m.MissionId) && !sequenceIds.Contains(m.MissionId)); // missions that aren't sequence or weather
                    var sequenceMissions = x.StellerMissions.Where(m => !weatherIds.Contains(m.MissionId) && !timedIds.Contains(m.MissionId)); // missions that aren't timed or weather

                    // Order of the default priority is Sequence -> Timed -> Weather
                    var priorityMissions = new List<(int prio, IEnumerable<WKSMission.StellarMissions> missions)>
                    {
                        (C.SequenceMissionPriority, sequenceMissions),
                        (C.TimedMissionPriority, timedMissions),
                        (C.WeatherMissionPriority, weatherMissions)
                    };

                    var sortedMissions = priorityMissions
                        .OrderBy(p => p.prio)
                        .SelectMany(p => p.missions)
                        .ToArray();

                    foreach (var m in sortedMissions)
                    {
                        var weatherMissionEntry = C.Missions.Where(x => x.Enabled && (x.JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob)).FirstOrDefault(e => e.Id == m.MissionId && CosmicHelper.MissionInfoDict[e.Id].JobId == currentClassJob);

                        if (weatherMissionEntry == default)
                        {
                            IceLogging.Debug($"Weather mission entry is default. Which means id: {weatherMissionEntry}");
                            continue;
                        }
                        IceLogging.Debug($"Mission Name: {m.Name} | MissionId: {weatherMissionEntry.Id} has been found. Setting value for sending");
                        SelectMission(m);
                        break;
                    }
                }

                if (MissionId == 0)
                    IceLogging.Debug("No mission was found under weather.");
                return true;
            }
            return false;
        }

        private static void SelectMission(WKSMission.StellarMissions m)
        {
            m.Select();
            SchedulerMain.MissionName = m.Name;
            MissionId = m.MissionId;
        }

        internal unsafe static bool? FindBasicMission()
        {
            if (EzThrottler.Throttle("Selecting Basic Mission"))
            {
                IceLogging.Debug($"Mission Name: {SchedulerMain.MissionName} | MissionId: {MissionId}");
                if (MissionId != 0)
                {
                    IceLogging.Debug("You already have a mission found, skipping finding a basic mission");
                    return true;
                }

                if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    foreach (var m in x.StellerMissions)
                    {
                        var basicMissionEntry = C.Missions.Where(x => x.Enabled && (x.JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob)).FirstOrDefault(e => e.Id == m.MissionId);

                        if (basicMissionEntry == default)
                            continue;

                        if (EzThrottler.Throttle("[Reset Mission Finder] Selecting Basic Mission"))
                        {
                            IceLogging.Debug($"Mission Name: {basicMissionEntry.Name} | MissionId: {basicMissionEntry.Id} has been found. Setting values for sending");
                            SelectMission(m);
                            break;
                        }
                    }

                    if (MissionId == 0)
                        IceLogging.Debug("No mission was found under basic missions.");
                    return true;
                }
            }
            return false;
        }

        internal unsafe static bool? FindBestRelicXP()
        {
            if (EzThrottler.Throttle("Selecting Best Mission for XP"))
            {
                IceLogging.Debug($"Mission Name: {SchedulerMain.MissionName} | MissionId: {MissionId}");
                if (MissionId != 0)
                {
                    IceLogging.Debug("You already have a mission found, skipping finding a basic mission");
                    return true;
                }

                if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    var DRankMissions = C.Missions.Where(x => CosmicHelper.MissionInfoDict[x.Id].JobId == currentClassJob || CosmicHelper.MissionInfoDict[x.Id].JobId2 == currentClassJob).Where(x => x.Type == MissionType.Standard && CosmicHelper.MissionInfoDict[x.Id].Rank == 1);
                    List<uint> MissionIds = new List<uint>();
                    foreach (var entry in DRankMissions)
                    {
                        IceLogging.Debug($"Adding {entry.Id}");
                        MissionIds.Add(entry.Id);
                    }
                    bool CorrectTab = false;
                    foreach (var m in x.StellerMissions)
                    {
                        if (MissionIds.Contains(m.MissionId))
                        {
                            IceLogging.Debug($"Found: {m.MissionId}");
                            CorrectTab = true;
                            break;
                        }
                    }

                    if (!CorrectTab)
                    {
                        if (EzThrottler.Throttle("Tabbing over to the basic missions"))
                            x.BasicMissions();
                    }
                    else
                    {
                        var bestMissionXp = FindRelicMission();
                        if (bestMissionXp != null)
                        {
                            foreach (var m in x.StellerMissions)
                            {
                                if (m.MissionId != bestMissionXp)
                                    continue;
                                else
                                {
                                    if (EzThrottler.Throttle($"Best mission is found [Mission XP]"))
                                    {
                                        IceLogging.Debug($"Best XP Mission to find: {bestMissionXp}");
                                        IceLogging.Debug($"Currently selecting mission from ui: {m.MissionId} | Name: {m.Name}");
                                        SelectMission(m);
                                        break;
                                    }
                                }
                            }
                        }
                        if (MissionId == 0)
                            IceLogging.Debug("No mission was found to be optimal for xp. Continuing on");
                        return true;
                    }
                }
            }
            return false;
        }

        internal unsafe static bool? FindResetMission()
        {
            if (EzThrottler.Throttle("FindResetMission"))
            {
                IceLogging.Debug($"Mission Name: {SchedulerMain.MissionName} | MissionId {MissionId}");
                if (MissionId != 0)
                {
                    IceLogging.Debug("You already have a mission found, skipping finding a basic mission.");
                    return true;
                }

                if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    IceLogging.Debug("Found mission was false");
                    var currentClassJob = PlayerHelper.GetClassJobId();


                    if (!x.StellerMissions.Any(x => CosmicHelper.MissionInfoDict[x.MissionId].JobId == currentClassJob)) //Tryin to reroll but on wrong job list
                    {
                        if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
                        {
                            if (EzThrottler.Throttle("Opening Mission Hud"))
                            {
                                hud.Mission();
                                Task.Delay(200);
                                hud.Mission();
                            }
                        }
                        IceLogging.Debug("Wrong class mission list, Restarting");
                        return false;
                    }

                    int A2 = x.StellerMissions.Where(x => CosmicHelper.MissionInfoDict[x.MissionId].Rank == 5).Count();
                    int A1 = x.StellerMissions.Where(x => CosmicHelper.MissionInfoDict[x.MissionId].Rank == 4).Count();
                    var missionRanks = new List<(bool hasMission, uint rank)>
                    {
                        (A2 !=0 && HasA2, 5),
                        (A2 == 0 && HasA2 || HasA1, 4),
                        (HasB, 3),
                        (HasC, 2),
                        (HasD, 1),
                    }
                            .Where(x => x.hasMission)
                            .Select(x => x.rank)
                            .ToArray();

                    if (missionRanks.Length == 0)
                    {
                        IceLogging.Debug("No Standard Mission is Selected, nothing to reroll");
                        return true;
                    }

                    var rankToReset = missionRanks.Max();

                    Random rng = new Random();

                    var missions = x.StellerMissions
                        .GroupBy(m => CosmicHelper.MissionInfoDict[m.MissionId].Rank) // Group By Rank
                        .SelectMany(g => g.OrderBy(m => rng.Next())) // Reorder inside each group randomly
                        .ToArray();

                    foreach (var m in missions)
                    {
                        var missionEntry = CosmicHelper.MissionInfoDict.FirstOrDefault(e => e.Key == m.MissionId);

                        if (missionEntry.Value == null)
                            continue;

                        IceLogging.Debug($"Mission: {m.Name} | Mission rank: {missionEntry.Value.Rank} | Rank to reset: {rankToReset}");
                        if (missionEntry.Value.Rank == rankToReset)
                        {
                            IceLogging.Debug($"Setting SchedulerMain.MissionName = {m.Name}");
                            m.Select();
                            SchedulerMain.MissionName = m.Name;
                            MissionId = missionEntry.Key;
                            SchedulerMain.Abandon = true;

                            IceLogging.Debug($"Mission Name: {SchedulerMain.MissionName}");

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private class XPType
        {
            public int CurrentXP { get; set; }
            public int NeededXP { get; set; }
        }

        private class Reward
        {
            public int Tier { get; set; }
            public float Amount { get; set; }
        }

        private class MissionInfo
        {
            public uint Id { get; set; }
            public string Name { get; set; }
            public List<Reward> ExpReward { get; set; }
        }

        internal unsafe static int? FindRelicMission()
        {
            uint? currentJobId = PlayerHelper.GetClassJobId().Value;
            var wksManager = WKSManager.Instance();
            if (wksManager == null || wksManager->ResearchModule == null || !wksManager->ResearchModule->IsLoaded)
                return null;

            var job = currentJobId;
            var toolClassId = (byte)(job - 7);
            var stage = wksManager->ResearchModule->CurrentStages[toolClassId - 1];
            var nextstate = wksManager->ResearchModule->UnlockedStages[toolClassId - 1];

            if (Svc.Data.GetExcelSheet<WKSCosmoToolClass>().TryGetRow(toolClassId, out var row))
            {

            }

            Dictionary<int, XPType> XPTable = new Dictionary<int, XPType>();

            if (!MissionHud.UseXPDebugger)
            {
                for (byte type = 1; type <= 5; type++)
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
                foreach (var entry in MissionHud.DummyXPTest)
                {
                    if (entry.Value.NeededXP != 0)
                    {
                        XPTable[entry.Key] = new XPType()
                        {
                            CurrentXP = entry.Value.CurrentXP,
                            NeededXP = entry.Value.NeededXP,
                        };
                    }
                }
            }

            var urgencies = new Dictionary<int, float>();
            for (int i = 0; i < XPTable.Count; i++)
            {
                var bar = XPTable[i + 1];
                urgencies[i + 1] = (bar.NeededXP > 0) ? 1f - ((float)bar.CurrentXP / bar.NeededXP) : 0f;
                IceLogging.Debug($"XP Type: {i+1} | Urgency: {urgencies[i + 1]}");
            }

            Dictionary<uint, CosmicHelper.MissionListInfo> currentlyAvailable = new();

            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                foreach (var mission in x.StellerMissions)
                {
                    if (CosmicHelper.MissionInfoDict.ContainsKey(mission.MissionId))
                    {
                        uint rank = CosmicHelper.MissionInfoDict[mission.MissionId].Rank;
                        uint minLevel = 0;
                        if (rank == 1)
                            minLevel = 10;
                        else if (rank == 2)
                            minLevel = 50;
                        else if (rank == 3)
                            minLevel = 90;
                        else // 
                            minLevel = 100;

                        if (minLevel <= PlayerHelper.GetLevel())
                        {
                            var entry = CosmicHelper.MissionInfoDict[mission.MissionId];
                            currentlyAvailable.TryAdd(mission.MissionId, entry);
                        }
                    }
                }
            }
            IceLogging.Info($"Amount of available missions: {currentlyAvailable.Count}");

            var rewardsDict = new Dictionary<uint, Dictionary<int, float>>();

            foreach (var mission in currentlyAvailable)
            {
                var missionConfig = C.Missions.Where(x => x.Id == mission.Key).FirstOrDefault();
                if (missionConfig != null)
                {
                    var missionEntry = CosmicHelper.MissionInfoDict.Where(x => x.Key == mission.Key).FirstOrDefault();

                    bool IgnoreManual = C.XPRelicIgnoreManual && missionConfig.ManualMode;
                    bool IgnoreNotEnabled = C.XPRelicOnlyEnabled && !missionConfig.Enabled;
                    bool GatheringCollectable = (missionEntry.Value.Attributes.HasFlag(MissionAttributes.Collectables) || missionEntry.Value.Attributes.HasFlag(MissionAttributes.ReducedItems))&&
                                  missionEntry.Value.Attributes.HasFlag(MissionAttributes.Gather) &&
                                  !missionEntry.Value.Attributes.HasFlag(MissionAttributes.Craft);

                    IceLogging.Debug($" - - - - - - - ");
                    IceLogging.Debug($"MissionID: {mission.Key} \n " +
                                     $"\"Ignore Manual Mode:\" {C.XPRelicIgnoreManual} | Mission Manual Enabled: {missionConfig.ManualMode} | Skipping? {IgnoreManual} \n" +
                                     $"Only Enabled Mode: {C.XPRelicOnlyEnabled} | Mission is Enabled: {missionConfig.Enabled} | Skipping? {IgnoreNotEnabled}");

                    if (GatheringCollectable)
                        continue;
                    if (IgnoreManual)
                        continue;
                    if (IgnoreNotEnabled)
                        continue;
                    else
                    {
                        var rewardMap = new Dictionary<int, float>();
                        foreach (var reward in mission.Value.ExperienceRewards)
                        {
                            IceLogging.Info($"Adding the following xp to {mission.Key} | Type: {reward.Type} | Amount: {reward.Amount}");
                            rewardMap[reward.Type] = reward.Amount;
                        }
                        rewardsDict[mission.Key] = rewardMap;
                    }
                }
            }

            IceLogging.Debug($"Reward Dictionary Count: {rewardsDict.Count}");

            int bestIndex = -1;
            float bestScore = float.NegativeInfinity;

            foreach (var kvp in rewardsDict)
            {
                int i = (int)kvp.Key;
                var reward = kvp.Value;
                float score = 0f;
                IceLogging.Info($"Currently checking mission: {i}");

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

                if (score > bestScore)
                {
                    IceLogging.Debug($"New Best Score: {score} (was {bestScore})", "[Relic XP Finder]");
                    IceLogging.Debug($"Mission Number: {i}");
                    bestScore = score;
                    bestIndex = i;
                }
            }

            IceLogging.Info($"Best relic xp has been completed. Best Relic Mission: {bestIndex}");
            return (bestIndex > 0) ? bestIndex : null;
        }

        internal unsafe static bool? GrabMission()
        {
            if (EzThrottler.Throttle("GrabMission", 250))
            {
                IceLogging.Debug($"[Grabbing Mission] Mission Name: {SchedulerMain.MissionName} | MissionId {MissionId}");
                CosmicHelper.MissionListInfo mission = CosmicHelper.MissionInfoDict[MissionId];
                float distance = mission.MarkerId != 0 ? Vector2.Distance(new Vector2(Player.Position.X, Player.Position.Z), new Vector2(mission.X, mission.Y)) : 0;
                if (SchedulerMain.Abandon == false && mission.Attributes.HasFlag(MissionAttributes.Gather) && !mission.Attributes.HasFlag(MissionAttributes.Critical) && distance > mission.Radius)
                {
                    SchedulerMain.State |= IceState.Waiting;
                    IceLogging.Debug($"Distance to marker: {distance} | Radius: {mission.Radius}");
                    if (PlayerHelper.IsPlayerNotBusy())
                        ActionManager.Instance()->UseAction(ActionType.GeneralAction, 9);
                    P.Navmesh.PathfindAndMoveTo(GatheringUtil.MoonNodeInfoList.Where(x => x.NodeSet == mission.NodeSet).OrderBy(x => PlayerHelper.GetDistanceToPlayer(x.Position)).First().LandZone, false);
                    return true;
                }
                else if (SchedulerMain.Abandon == false && mission.Attributes.HasFlag(MissionAttributes.Gather) && C.Missions.SingleOrDefault(x => x.Id == MissionId).GatherSetting.MinimumGP > PlayerHelper.GetGp())
                {
                    SchedulerMain.State |= IceState.Waiting;
                    return true;
                }
                else if (TryGetAddonMaster<SelectYesno>("SelectYesno", out var select) && select.IsAddonReady)
                {
                    string[] commenceStrings = ["選択したミッションを開始します。よろしいですか？", "Commence selected mission?", "Ausgewählte Mission wird gestartet.Fortfahren?", "Commencer la mission sélectionnée ?", "确定要开始此任务吗？", "선택한 임무를 시작하시겠습니까?"];
                    if (commenceStrings.Any(select.Text.Contains) || !C.RejectUnknownYesno)
                    {
                        IceLogging.Debug($"Expected Commence window: {select.Text}");
                        select.Yes();
                    }
                    else
                    {
                        IceLogging.Error($"[Grabbing Mission] Unexpected Commence window: {select.Text}");
                        select.No();
                    }
                    return false;
                }
                else if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    if (!CosmicHelper.MissionInfoDict.ContainsKey(MissionId))
                    {
                        IceLogging.Debug($"No values were found for mission id {MissionId}... which is odd. Stopping the process");
                        if (!HasStandard && (HasWeather || HasTimed))
                        {
                            SchedulerMain.State |= IceState.Waiting;
                            return true;
                        }
                    }
                    else
                        ECommons.Automation.Callback.Fire(x.Base, true, 13, MissionId); // Firing off to initiate quest
                }
                else if (!AddonHelper.IsAddonActive("WKSMission"))
                {
                    if (CosmicHelper.CurrentLunarMission == 0 && CosmicHelper.MissionInfoDict[MissionId].Time != 0) // If Mission is Locked
                    {
                        BlacklistedMission.Add(MissionId);
                        SchedulerMain.State = IceState.GrabMission;
                        P.TaskManager.Abort();
                    }
                    return true;
                }
            }
            return false;
        }

        internal unsafe static bool? AbandonMission()
        {
            if (SchedulerMain.Abandon == false || CosmicHelper.CurrentLunarMission == 0)
                return true;
            else if (EzThrottler.Throttle("AbandonMission", 250))
            {
                if (TryGetAddonMaster<SelectYesno>("SelectYesno", out var select) && select.IsAddonReady)
                {
                    string[] abandonStrings = ["受注中のミッションを破棄します。", "Abandon mission?", "Aktuelle Mission abbrechen?", "Êtes-vous sûre de vouloir abandonner la mission en cours ?", "确定要放弃已领取的任务吗？", "수락한 임무를 포기하시겠습니까?"];
                    if (abandonStrings.Any(select.Text.Contains) || !C.RejectUnknownYesno)
                    {
                        IceLogging.Debug($"[Abandoning Mission] Expected Abandon window: {select.Text}");
                        select.Yes();
                        return true;
                    }
                    else
                    {
                        IceLogging.Error($"[Abandoning Mission] Unexpected Abandon window: {select.Text}");
                        select.No();
                        return false;
                    }
                }
                if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var addon) && addon.IsAddonReady)
                {
                    IceLogging.Debug("[Abandoning Mission] Attempting Abandon.");
                    addon.Abandon();
                }
                else if (TryGetAddonMaster<WKSHud>("WKSHud", out var SpaceHud) && SpaceHud.IsAddonReady)
                {
                    IceLogging.Debug("[Abandoning Mission] WKSMissionInformation missing. Attempting opening.");
                    SpaceHud.Mission();
                }
            }
            return false;
        }

        public static void WaitForNonStandard()
        {
            if (!PlayerHelper.IsInCosmicZone())
                return;

            if (P.Navmesh.IsRunning())
                return;

            if (CosmicHelper.MissionInfoDict[MissionId].Attributes.HasFlag(MissionAttributes.Gather) && C.Missions.SingleOrDefault(x => x.Id == MissionId).GatherSetting.MinimumGP > PlayerHelper.GetGp())
                return;

            if (HasStandard)
                SchedulerMain.State &= ~IceState.Waiting;

            uint currentWeatherId = WeatherForecastHandler.GetCurrentWeatherId();
            bool isUmbralWind = currentWeatherId == 49;
            bool isMoonDust = currentWeatherId == 148;
            if ((isUmbralWind || isMoonDust) && HasWeather)
            {
                bool hasCorrectWeather = WeatherMissions
                    .Any(x => (CosmicHelper.MissionInfoDict[x.Id].Weather == CosmicWeather.UmbralWind && isUmbralWind) || (CosmicHelper.MissionInfoDict[x.Id].Weather == CosmicWeather.MoonDust && isMoonDust));
                if (hasCorrectWeather)
                    SchedulerMain.State &= ~IceState.Waiting;
            }

            //bool isSporingMist = currentWeatherId == 197;
            //bool isAstromagneticStorms = currentWeatherId == 149 || currentWeatherId == 196;
            //bool isMeteoricShower = currentWeatherId == 194 || currentWeatherId == 195;
            //if ((isSporingMist || isAstromagneticStorms || isMeteoricShower) && hasCritical)
            //{
            //    //Cannot Check for Umbral Weather For Critical
            //    SchedulerMain.State = IceState.GrabMission;
            //}

            (var currentTimedBonus, var nextTimedBonus) = PlayerHandlers.GetTimedJob();
            if (currentTimedBonus.Value != null && HasTimed)
            {

                List<uint> jobIds = [.. currentTimedBonus.Value
                    .Select(name => MainWindowV2.jobOptions.FirstOrDefault(job => job.Name == name))
                    .Where(job => job != default)
                    .Select(job => job.Id)];

                if (jobIds.Any(job => job == currentClassJob)) // Might still be the problem child... need to check upon re-write of this nightmare of a nest
                {
                    bool hasMissionAtThisTime = TimedMissions
                        .Any(mission => currentTimedBonus.Key.start == 2 * (CosmicHelper.MissionInfoDict[mission.Id].Time - 1));
                    if (hasMissionAtThisTime)
                        SchedulerMain.State &= ~IceState.Waiting;
                }
            }
        }

        internal static void DelayMission()
        {
            if (C.DelayGrabMission)
                P.TaskManager.EnqueueDelay(C.DelayIncrease);
        }
    }
}
