using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.Cosmic_Helper;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using MissionRank = FFXIVClientStructs.FFXIV.Client.Game.WKS.WKSMissionModule.MissionRank;

namespace ICE.Scheduler.Tasks
{
    internal static class Task_CheckScore
    {
        public static void Enqueue()
        {
            var Id = CosmicHelper.CurrentLunarMission;
            var mission = CosmicHelper.SheetMissionDict[Id];

            var jobs = mission.Jobs;

            if (CosmicHelper.CrafterJobList.Any(x => jobs.Contains(x)) && CosmicHelper.GatheringJobList.Any(x => jobs.Contains(x)))
            {
                P.TaskManager.Enqueue(() => DualClass(), "Checking dual class score");
                P.TaskManager.Enqueue(() => SchedulerMain.State = IceState.DualClass, "Setting state to dual class");
            }
            else if (CosmicHelper.CrafterJobList.Any(x => jobs.Contains(x)))
            {
                IceLogging.Info("Currently on a crafting job, checking for crafting scoring", "Task: Score Check");
                P.TaskManager.Enqueue(() => SchedulerMain.State = IceState.Craft);
                P.TaskManager.Enqueue(() => Craft_V2(), "Checking for crafting score mission");
            }
            else if (CosmicHelper.GatheringJobList.Any(x => jobs.Contains(x)))
            {
                var jobId = Player.Job;

                IceLogging.Info($"Currently on a gathering job {jobId}");
                if (jobId == (Job)18)
                {
                    P.TaskManager.Enqueue(() => SchedulerMain.State = IceState.Fish);
                    P.TaskManager.Enqueue(() => Fish(), "Checking fishing missions for score");
                }
                else
                {
                    P.TaskManager.Enqueue(() => SchedulerMain.State = IceState.Gather);
                    P.TaskManager.Enqueue(() => Gather_V2(), "Checking the gathering score");
                }
            }
        }

        public static unsafe bool? Fish()
        {
            string tag = "[Task_Check Score: Fish]";
            var currentMission = CosmicHelper.CurrentLunarMission;

            if (EzThrottler.Throttle("Fish Score Check Throttle"))
                IceLogging.Verbose($"Score check for fish was initialized. Checking for minimum requirements: [{currentMission}]", tag);

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var missionInfo) && missionInfo.IsAddonReady)
            {
                if (CosmicHelper.SheetMissionDict.TryGetValue(currentMission, out var sheetInfo))
                {
                    var rank = CurrentRank();
                    var currentScore = CosmicHandler.GetScore();

                    if (rank == MissionRank.Failed)
                    {
                        if (EzThrottler.Throttle("Timed out message"))
                            IceLogging.Debug("Mission is either timed out, or out of resources. So going to force a turnin", tag);
                        SchedulerMain.State = IceState.AbandonMission;
                        P.TaskManager.Tasks.Clear();
                        return true;
                    }
                    else if (sheetInfo.IsCritical)
                    {
                        IceLogging.Verbose($"We need to check to see if we have the minimum amount of items for the critical, checking now", tag);
                        foreach (var fishItem in sheetInfo.Gathering_Min)
                        {
                            if (PlayerHelper.GetItemCount(fishItem.Key, out var amount))
                            {
                                if (amount < fishItem.Value)
                                {
                                    IceLogging.Debug("We've found a fish that we're still missing!\n" +
                                        $"ItemID: {fishItem.Key}. We need: {fishItem.Value}. We have: {amount}", tag);

                                    return true;
                                }
                            }
                        }
                    }
                    else if (rank < MissionRank.Bronze)
                    {
                        if (EzThrottler.Throttle("Bronze Check"))
                            IceLogging.Debug("We still haven't even met the bronze threshold for turning in, going to just check back", tag);

                        return true;
                    }

                    if (EzThrottler.Throttle("Score Check"))
                        IceLogging.Verbose("We have atleast met the bronze threshold, checking to see where to go from there", tag);

                    if (rank != MissionRank.None || sheetInfo.Attributes.HasFlag(MissionAttributes.Critical))
                    {
                        if (sheetInfo.Attributes.HasFlag(MissionAttributes.Score_TimeRemaining))
                        {
                            IceLogging.Debug("We're in a mission where we're just meeting the minimum score. Turning in", tag);
                            SchedulerMain.State = IceState.TurninMission;
                            P.TaskManager.Tasks.Clear();

                            return true;
                        }
                        else
                        {
                            bool shouldTurnin = false;

                            if (sheetInfo.Attributes.HasFlag(MissionAttributes.Critical))
                            {
                                IceLogging.Verbose("We're in a critical mission, this needs to just be turned in", tag);
                                shouldTurnin = true;
                            }
                            else if (CosmicHandler.IsMissionTimedOut())
                            {
                                IceLogging.Verbose("We seem to be timed out of our current mission. But we've hit a minimum threshold of score, so we're just going to turnin", tag);
                                shouldTurnin = true;
                            }
                            else if (Mission_Settings.Mode == ModeSelect.LevelMode && rank >= MissionRank.Bronze)
                            {
                                IceLogging.Debug("We're in Leveling Mode, and we only need a bronze. So we're setting turnin to true");
                                shouldTurnin = true;
                            }
                            else
                            {
                                var config = C.MissionConfig[currentMission];

                                if (sheetInfo.IsMaster)
                                {
                                    shouldTurnin = (config.TurninGoal is TurninState.Gold && rank >= MissionRank.Gold)
                                        || (config.TurninGoal is TurninState.Master_Score && rank >= MissionRank.Gold && currentScore >= config.Master_Score)
                                        || (config.TurninGoal is TurninState.TimeExpired && rank >= MissionRank.Gold && CosmicHandler.IsMissionTimedOut());
                                }
                                else
                                {
                                    shouldTurnin = (config.TurninGoal is TurninState.Gold && rank >= MissionRank.Gold) 
                                        || (config.TurninGoal is TurninState.Silver && rank >= MissionRank.Silver) 
                                        || (config.TurninGoal is TurninState.Bronze && rank >= MissionRank.Bronze);
                                }
                            }

                            if (shouldTurnin)
                            {
                                IceLogging.Info("The threshold for scoring was met. Time to turnin", tag);
                                SchedulerMain.State = IceState.TurninMission;
                                // P.AutoHook.Ah_State(false);
                                P.TaskManager.Tasks.Clear();

                                return true;
                            }
                            else
                            {
                                if (EzThrottler.Throttle("Score Report"))
                                {
                                    var config = C.MissionConfig[currentMission];

                                    IceLogging.Debug("We're still going for a score/not met threshold.\n" +
                                        $"Rank: {rank.ToString()}\n" +
                                        $"Turnin Rank: {config.TurninGoal.ToString()}", tag);

                                    if (sheetInfo.IsMaster && config.TurninGoal == TurninState.Master_Score)
                                    {
                                        IceLogging.Verbose($"Current Score: {currentScore} | Turnin Goal: {config.Master_Score}", tag);
                                    }
                                }
                                return true;
                            }
                        }
                    }
                    else
                    {
                        IceLogging.Error($"Hey, it seems something slipped through the cracks. If you could report to me this it would be great\n" +
                            $"ID: {currentMission}\n" +
                            $"Name: {sheetInfo.Name}\n" +
                            $"Missed the score ranking but slipped through", tag);

                        return true;
                    }
                }
            }
            else if (GenericHelpers.TryGetAddonMaster<WKSHud>(out var moonHud) && moonHud.IsAddonReady)
            {
                if (EzThrottler.Throttle("Opening the moon hud", 1000))
                {
                    moonHud.Mission();
                    IceLogging.Info("Hud wasn't visible. Opening it", "[Score Check]");
                }
            }

            return false;
        }
        public static bool? Craft_V2()
        {
            string tag = "[Check Score: Craft]";

            var currentScore = CosmicHandler.GetScore();
            var rank = CurrentRank();
            var currentMission = CosmicHelper.CurrentLunarMission;

            if (rank == MissionRank.Failed)
            {
                IceLogging.Debug("Mission is either timed out, or out of resources. So going to force a turnin", tag);
                SchedulerMain.State = IceState.AbandonMission;
                P.TaskManager.Tasks.Clear();
                return true;
            }

            if (Svc.Condition[ConditionFlag.ExecutingGatheringAction])
            {
                return false;
            }

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>(out var missionInfo) && missionInfo.IsAddonReady)
            {
                var id = CosmicHelper.CurrentLunarMission;
                if (CosmicHelper.SheetMissionDict.TryGetValue(id, out var sheet))
                {

                    if (sheet.Attributes.HasFlag(MissionAttributes.Critical))
                    {
                        if (currentScore < sheet.BronzeScore)
                        {
                            IceLogging.Info("We still need score for the critical mission, so going to craft some more", tag);
                            SchedulerMain.State = IceState.Craft;
                            return true;
                        }
                        else
                        {
                            IceLogging.Info("Minimum score for criticals has been hit WOOO", tag);
                        }
                    }
                    else if (rank == MissionRank.None)
                    {
                        IceLogging.Info("We haven't completed the minimum crafts required for the turnin. Going to craft more", tag);
                        return true;
                    }

                    if (rank != MissionRank.None || sheet.Attributes.HasFlag(MissionAttributes.Critical))
                    {
                        bool shouldTurnin = false;

                        if (sheet.Attributes.HasFlag(MissionAttributes.Critical))
                        {
                            IceLogging.Verbose("We're in a critical mission, this needs to just be turned in", tag);
                            shouldTurnin = true;
                        }
                        else if (CosmicHandler.IsMissionTimedOut())
                        {
                            IceLogging.Verbose("We seem to be timed out of our current mission. But we've hit a minimum threshold of score, so we're just going to turnin", tag);
                            shouldTurnin = true;
                        }
                        else if (Mission_Settings.Mode == ModeSelect.LevelMode && rank >= MissionRank.Bronze)
                        {
                            IceLogging.Debug("We're in Leveling Mode, and we only need a bronze. So we're setting turnin to true");
                            shouldTurnin = true;
                        }
                        else
                        {
                            var config = C.MissionConfig[id];

                            var sheetInfo = CosmicHelper.SheetMissionDict[currentMission];

                            if (sheetInfo.IsMaster)
                            {


                                shouldTurnin = (config.TurninGoal is TurninState.Gold && rank >= MissionRank.Gold)
                                    || (config.TurninGoal is TurninState.Master_Score && rank >= MissionRank.Gold && currentScore >= config.Master_Score)
                                    || (config.TurninGoal is TurninState.TimeExpired && rank >= MissionRank.Gold && CosmicHandler.IsMissionTimedOut());

                                if (sheetInfo.Jobs.ContainsAny(CosmicHelper.CrafterJobList) && config.TurninGoal is TurninState.Master_Items)
                                {
                                    var craftItem = sheetInfo.Crafts_Main.FirstOrDefault();
                                    var itemId = craftItem.Value.ItemId;
                                    shouldTurnin = PlayerHelper.GetItemCount(itemId, out var count) && count >= config.Master_Items && rank >= MissionRank.Gold;
                                    if (EzThrottler.Throttle("Item Count Message"))
                                    {
                                        IceLogging.Verbose($"Turnin was set to Item Count in master. Current Count: {count} | goal: {config.Master_Items}");
                                    }
                                }
                            }
                            else
                            {
                                shouldTurnin = (config.TurninGoal is TurninState.Gold && rank >= MissionRank.Gold)
                                    || (config.TurninGoal is TurninState.Silver && rank >= MissionRank.Silver)
                                    || (config.TurninGoal is TurninState.Bronze && rank >= MissionRank.Bronze);
                            }
                        }

                        if (shouldTurnin)
                        {
                            IceLogging.Info("The threshold for scoring was met. Time to turnin", tag);
                            SchedulerMain.State = IceState.TurninMission;
                            P.TaskManager.Tasks.Clear();

                            return true;
                        }
                        else
                        {
                            var config = C.MissionConfig[id];

                            IceLogging.Debug("We're still going for a score/not met threshold.\n" +
                                $"Rank: {rank.ToString()}\n" +
                                $"Highest Goal: {config.TurninGoal.ToString()}");
                            return true;
                        }
                    }
                    else
                    {
                        IceLogging.Error($"Hey, it seems something slipped through the cracks. If you could report to me this it would be great\n" +
                            $"ID: {id}\n" +
                            $"Name: {sheet.Name}\n" +
                            $"Missed the score ranking but slipped through", tag);

                        return true;
                    }
                }
            }
            else if (GenericHelpers.TryGetAddonMaster<WKSHud>(out var moonHud) && moonHud.IsAddonReady)
            {
                if (EzThrottler.Throttle("Opening the moon hud", 1000))
                {
                    moonHud.Mission();
                    IceLogging.Info("Hud wasn't visible. Opening it", "[Score Check]");
                }
            }

            return false;
        }
        public static bool? Gather_V2()
        {
            string tag = "[Check Score: Gather]";

            var currentScore = CosmicHandler.GetScore();
            var rank = CurrentRank();

            if (rank == MissionRank.Failed)
            {
                IceLogging.Debug("Mission is either timed out, or out of resources. So going to force a turnin", tag);
                SchedulerMain.State = IceState.AbandonMission;
                P.TaskManager.Tasks.Clear();
                return true;
            }

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>(out var missionInfo) && missionInfo.IsAddonReady)
            {
                var id = CosmicHelper.CurrentLunarMission;
                if (CosmicHelper.SheetMissionDict.TryGetValue(id, out var sheet))
                {
                    if (sheet.Attributes.HasFlag(MissionAttributes.Critical))
                    {
                        foreach (var item in sheet.Gathering_Min)
                        {
                            if (PlayerHelper.GetItemCount(item.Key, out var count) && count < item.Value)
                            {
                                IceLogging.Info("We're still missing items for the critical mission, so continuing on", tag);
                                return true;
                            }
                        }
                    }
                    else if (rank == MissionRank.None)
                    {
                        IceLogging.Info("We still haven't achieved atleast bronze scoring for the missions. So we're going to continue on", tag);
                        return true;
                    }

                    if (rank != MissionRank.None || sheet.Attributes.HasFlag(MissionAttributes.Critical))
                    {
                        bool shouldTurnin = false;

                        if (sheet.Attributes.HasFlag(MissionAttributes.Critical))
                        {
                            IceLogging.Verbose("We're in a critical mission, this needs to just be turned in", tag);
                            shouldTurnin = true;
                        }
                        else if (CosmicHandler.IsMissionTimedOut())
                        {
                            IceLogging.Verbose("We seem to be timed out of our current mission. But we've hit a minimum threshold of score, so we're just going to turnin", tag);
                            shouldTurnin = true;
                        }
                        else if (Mission_Settings.Mode == ModeSelect.LevelMode && rank >= MissionRank.Bronze)
                        {
                            IceLogging.Debug("We're in Leveling Mode, and we only need a bronze. So we're setting turnin to true", tag);
                            shouldTurnin = true;
                        }
                        else if (sheet.Attributes.HasFlag(MissionAttributes.Score_TimeRemaining))
                        {
                            IceLogging.Debug("Score is based on time remaining, and we have some sort of rank. Turning in", tag);
                            shouldTurnin = true;
                        }
                        else if (sheet.Attributes.HasFlag(MissionAttributes.Limited) && Mission_Settings.nodeTotal == 8)
                        {
                            if (!Svc.Condition[ConditionFlag.Gathering])
                            {
                                shouldTurnin = true;
                                IceLogging.Debug("We might of not reached our turnin point, but we've ran out of nodes to gather at. So we're just going to just turnin", tag);
                            }
                        }
                        else
                        {
                            var config = C.MissionConfig[id];
                            var sheetInfo = CosmicHelper.SheetMissionDict[id];

                            if (sheetInfo.IsMaster)
                            {
                                shouldTurnin = (config.TurninGoal is TurninState.Gold && rank >= MissionRank.Gold)
                                    || (config.TurninGoal is TurninState.Master_Score && rank >= MissionRank.Gold && currentScore >= config.Master_Score)
                                    || (config.TurninGoal is TurninState.TimeExpired && rank >= MissionRank.Gold && CosmicHandler.IsMissionTimedOut());
                            }
                            else
                            {
                                shouldTurnin = (config.TurninGoal is TurninState.Gold && rank >= MissionRank.Gold)
                                    || (config.TurninGoal is TurninState.Silver && rank >= MissionRank.Silver)
                                    || (config.TurninGoal is TurninState.Bronze && rank >= MissionRank.Bronze);
                            }
                        }

                        if (shouldTurnin)
                        {
                            IceLogging.Info("The threshold for scoring was met. Time to turnin", tag);
                            SchedulerMain.State = IceState.TurninMission;
                            P.TaskManager.Tasks.Clear();

                            return true;
                        }
                        else
                        {
                            var config = C.MissionConfig[id];

                            IceLogging.Debug("We're still going for a score/not met threshold.\n" +
                                $"Rank: {rank.ToString()}\n" +
                                $"Turnin Goal: {config.TurninGoal.ToString()}");
                            return true;
                        }
                    }
                    else
                    {
                        IceLogging.Error($"Hey, it seems something slipped through the cracks. If you could report to me this it would be great\n" +
                            $"ID: {id}\n" +
                            $"Name: {sheet.Name}\n" +
                            $"Missed the score ranking but slipped through", tag);

                        return true;
                    }
                }
            }
            else if (GenericHelpers.TryGetAddonMaster<WKSHud>(out var moonHud) && moonHud.IsAddonReady)
            {
                if (EzThrottler.Throttle("Opening the moon hud", 1000))
                {
                    moonHud.Mission();
                    IceLogging.Info("Hud wasn't visible. Opening it", "[Score Check]");
                }
            }

            return false;
        }
        public static unsafe bool? DualClass()
        {
            string tag = "Score Check: Dual Class";

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>(out var missionInfo) && missionInfo.IsAddonReady)
            {
                var currentScore = CosmicHandler.GetScore()
                    ;
                var rank = CurrentRank();
                var Id = CosmicHelper.CurrentLunarMission;

                if (CosmicHandler.IsMissionTimedOut())
                {
                    SchedulerMain.State = IceState.AbandonMission;
                    P.TaskManager.Tasks.Clear();
                    return true;
                }
                else if (rank == MissionRank.Failed)
                {
                    SchedulerMain.State = IceState.AbandonMission;
                    P.TaskManager.Tasks.Clear();
                    return true;
                }

                var config = C.MissionConfig[Id];
                bool shouldTurnin = (config.TurninGoal is TurninState.Gold && rank >= MissionRank.Gold) ||
                               (config.TurninGoal is TurninState.Silver && rank >= MissionRank.Silver) ||
                               (config.TurninGoal is TurninState.Bronze && rank >= MissionRank.Bronze);

                if (shouldTurnin)
                {
                    IceLogging.Info("The threshold for scoring was met. Time to turnin", tag);
                    SchedulerMain.State = IceState.TurninMission;
                    P.TaskManager.Tasks.Clear();

                    return true;
                }
                else
                {
                    IceLogging.Debug("We're still going for a score/not met threshold.\n" +
                        $"Rank: {rank.ToString()}\n" +
                        $"Turnin Goal: {config.TurninGoal.ToString()}", tag);
                    return true;
                }
            }
            else if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var moonHud))
            {
                if (EzThrottler.Throttle("Opening the moon hud", 1000))
                {
                    moonHud.Mission();
                    IceLogging.Info("Hud wasn't visible. Opening it", tag);
                }
            }

            return false;
        }
        private static unsafe uint CurrentCollectedTotal()
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return 0;

            return managerPtr->State.CurrentMission.CollectedTotal;
        }
        private static unsafe uint CurrentIndividualTotal()
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return 0;

            return managerPtr->State.CurrentMission.CollectedIndividual;
        }
        public static unsafe MissionRank CurrentRank()
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return MissionRank.None;

            return managerPtr->State.CurrentMission.Rank;
        }
    }
}
