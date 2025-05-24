using ECommons.Automation;
using ICE.Ui;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommons.GameHelpers;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ECommons.GenericHelpers;

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
            if (SchedulerMain.AnimationLockAbandonState)
            {
                SchedulerMain.State = IceState.AnimationLock;
                return;
            }
            if (C.StopOnceHitCosmoCredits)
            {
                if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
                {
                    if (hud.CosmoCredit >= C.CosmoCreditsCap)
                    {
                        IceLogging.Debug($"[SchedulerMain] Stopping the plugin as you have {hud.CosmoCredit} Cosmocredits");
                        Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                        {
                            Message = $"[ICE] Stopping the plugin as you have {hud.CosmoCredit} Cosmocredits.",
                            Type = Dalamud.Game.Text.XivChatType.ErrorMessage,
                        });
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
                        IceLogging.Debug($"[SchedulerMain] Stopping the plugin as you have {hud.LunarCredit} Lunar Credits");
                        Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                        {
                            Message = $"[ICE] Stopping the plugin as you have {hud.LunarCredit} Lunar Credits",
                            Type = Dalamud.Game.Text.XivChatType.ErrorMessage,
                        });
                        SchedulerMain.DisablePlugin();
                        return;
                    }
                }
            }

            if (Player.Level >= C.TargetLevel && C.StopWhenLevel)
            {
                SchedulerMain.StopBeforeGrab = true;
                IceLogging.Debug($"StopWhenLevel: Stopped at target level {Player.Level}");
            }
            if (SchedulerMain.StopBeforeGrab)
            {
                SchedulerMain.DisablePlugin();
                return;
            }

            if (!SchedulerMain.State.HasFlag(IceState.GrabMission))
                return;

            if (!(HasCritical || HasWeather || HasTimed || HasSequence || HasStandard))
            {
                IceLogging.Error("[SchedulerMain] No missions available, stopping the plugin");
                Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                {
                    Message = $"[ICE] No missions enabled for {Svc.ClientState.LocalPlayer?.ClassJob.Value.Name}. Did you forget to set me up?",
                    Type = Dalamud.Game.Text.XivChatType.ErrorMessage,
                });
                SchedulerMain.DisablePlugin();
                return;
            }

            P.TaskManager.Enqueue(TaskRepair.GatherCheck, "Checking for repairs");
            P.TaskManager.Enqueue(TaskSpiritbond.TryExtractMateria, "Checking for materia");

            P.TaskManager.Enqueue(UpdateValues, "Updating Task Mission Values");
            P.TaskManager.Enqueue(OpenMissionFinder, "Opening the Mission finder");
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

            if (EzThrottler.Throttle("OpenMissionFinder"))
                if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
                {
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

                        IceLogging.Debug($"[Critical Mission] Mission Name: {m.Name} | MissionId: {criticalMissionEntry.Id} has been found. Setting value for sending", true);
                        SelectMission(m);
                        break;
                    }
                }

                if (MissionId == 0)
                    IceLogging.Debug("[Critical Mission] No mission was found under weather, continuing on", true);
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

                    var weatherMissions = x.StellerMissions.Where(m => !timedIds.Contains(m.MissionId) && !sequenceIds.Contains(m.MissionId));
                    var timedMissions = x.StellerMissions.Where(m => !weatherIds.Contains(m.MissionId) && !sequenceIds.Contains(m.MissionId));
                    var sequenceMissions = x.StellerMissions.Where(m => !weatherIds.Contains(m.MissionId) && !timedIds.Contains(m.MissionId));

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
                            IceLogging.Debug($"[Weather Mission] Weather mission entry is default. Which means id: {weatherMissionEntry}", true);
                            continue;
                        }
                        IceLogging.Debug($"[Weather Mission] Mission Name: {m.Name} | MissionId: {weatherMissionEntry.Id} has been found. Setting value for sending", true);
                        SelectMission(m);
                        break;
                    }
                }

                if (MissionId == 0)
                    IceLogging.Debug("[Weather Mission] No mission was found under weather.", true);
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
                IceLogging.Debug($"[Basic Mission] Mission Name: {SchedulerMain.MissionName} | MissionId: {MissionId}", true);
                if (MissionId != 0)
                {
                    IceLogging.Debug("[Basic Mission] You already have a mission found, skipping finding a basic mission", true);
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
                            IceLogging.Debug($"Mission Name: {basicMissionEntry.Name} | MissionId: {basicMissionEntry.Id} has been found. Setting values for sending", true);
                            SelectMission(m);
                            break;
                        }
                    }

                    if (MissionId == 0)
                        IceLogging.Debug("[Basic Mission] No mission was found under basic missions.", true);
                    return true;
                }
            }
            return false;
        }

        internal unsafe static bool? FindResetMission()
        {
            if (EzThrottler.Throttle("FindResetMission"))
            {
                IceLogging.Debug($"[Reset Mission Finder] Mission Name: {SchedulerMain.MissionName} | MissionId {MissionId}", true);
                if (MissionId != 0)
                {
                    IceLogging.Debug("[Reset Mission Finder] You already have a mission found, skipping finding a basic mission.", true);
                    return true;
                }

                if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    IceLogging.Debug("[Reset Mission Finder] Found mission was false", true);
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
                        IceLogging.Debug("[Reset Mission Finder] Wrong class mission list, Restarting", true);
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
                        IceLogging.Debug("[Reset Mission Finder] No Standard Mission is Selected, nothing to reroll", true);
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

                        IceLogging.Debug($"[Reset Mission Finder] Mission: {m.Name} | Mission rank: {missionEntry.Value.Rank} | Rank to reset: {rankToReset}", true);
                        if (missionEntry.Value.Rank == rankToReset)
                        {
                            IceLogging.Debug($"[Reset Mission Finder] Setting SchedulerMain.MissionName = {m.Name}", true);
                            m.Select();
                            SchedulerMain.MissionName = m.Name;
                            MissionId = missionEntry.Key;
                            SchedulerMain.Abandon = true;

                            IceLogging.Debug($"[Reset Mission Finder] Mission Name: {SchedulerMain.MissionName}", true);

                            return true;
                        }
                    }
                }
            }
            return false;
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
                    IceLogging.Debug($"[ICE] Distance to marker: {distance} | Radius: {mission.Radius}", true);
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
                    string[] commenceStrings = ["選択したミッションを開始します。よろしいですか？", "Commence selected mission?", "Ausgewählte Mission wird gestartet.Fortfahren?", "Commencer la mission sélectionnée ?"];
                    if (commenceStrings.Any(select.Text.Contains) || !C.RejectUnknownYesno)
                    {
                        IceLogging.Debug($"[Grabbing Mission] Expected Commence window: {select.Text}", true);
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
                        Callback.Fire(x.Base, true, 13, MissionId); // Firing off to initiate quest
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
                    string[] abandonStrings = ["受注中のミッションを破棄します。よろしいですか？", "Abandon mission?", "Aktuelle Mission abbrechen?", "Êtes-vous sûr de vouloir abandonner la mission en cours ?"];
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
                    .Select(name => MainWindow.jobOptions.FirstOrDefault(job => job.Name == name))
                    .Where(job => job != default)
                    .Select(job => job.Id - 1)]; // Because MainWindow.jobOptions Id is slightly off :(

                if (jobIds.Any(job => job == currentClassJob))
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
