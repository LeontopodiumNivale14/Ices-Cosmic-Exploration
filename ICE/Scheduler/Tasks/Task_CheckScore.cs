using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.Cosmic_Helper;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using MissionRank = FFXIVClientStructs.FFXIV.Client.Game.WKS.WKSManager.MissionRank;

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

            if (EzThrottler.Throttle("Fish Score Check Throttle", 1000))
                IceLogging.Verbose($"Score check for fish was initialized. Checking for minimum requirements: [{currentMission}]", tag);

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var missionInfo) && missionInfo.IsAddonReady)
            {
                if (CosmicHelper.SheetMissionDict.TryGetValue(currentMission, out var sheetInfo))
                {
                    var rank = CurrentRank();

                    IceLogging.Verbose("Mission info was valid, searching what we should be checking for", tag);

                    if (sheetInfo.Gathering_Min.Count > 0)
                    {
                        IceLogging.Verbose($"Fishing mission has a minumum amount of fish needed. Checking the specifics for each", tag);
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
                    else if (sheetInfo.Fish_AmountRequired > 0)
                    {
                        IceLogging.Verbose("We're in a mission where we need a certain amount of fish overall. So checking that", tag);
                        var amount = CurrentCollectedTotal();

                        if (amount < sheetInfo.Fish_AmountRequired)
                        {
                            IceLogging.Debug($"We're not at the total amount needed for the mission. Need: {sheetInfo.Fish_AmountRequired} | Have: {amount}", tag);
                            return true;
                        }
                    }
                    else if (sheetInfo.Fish_VarietyAmount > 0)
                    {
                        var amount = CurrentIndividualTotal();
                        if (amount < sheetInfo.Fish_VarietyAmount)
                        {
                            IceLogging.Debug($"Need a variety of fish, and we're missing some. Need: {sheetInfo.Fish_VarietyAmount} | Have: {amount}", tag);
                            return true;
                        }
                    }
                    else
                    {
                        var currentScore = CurrentScore();
                        var bronzeScore = sheetInfo.BronzeScore;

                        if (currentScore < bronzeScore && sheetInfo.BronzeScore != 0)
                        {
                            IceLogging.Debug("We need to still hit the score threshold for bronze. So we're still gonna fish", tag);
                            return true;
                        }
                    }

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

                                shouldTurnin = (config.TurninGoal is TurninState.Gold && rank >= MissionRank.Gold) ||
                                               (config.TurninGoal is TurninState.Silver && rank >= MissionRank.Silver) ||
                                               (config.TurninGoal is TurninState.Bronze && rank >= MissionRank.Bronze);
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
                                var config = C.MissionConfig[currentMission];

                                IceLogging.Debug("We're still going for a score/not met threshold.\n" +
                                    $"Rank: {rank.ToString()}\n" +
                                    $"Turnin Rank: {config.TurninGoal.ToString()}");
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
            else if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var moonHud) && moonHud.IsAddonReady)
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

            var currentScore = CurrentScore();
            var rank = CurrentRank();

            if (Svc.Condition[ConditionFlag.ExecutingGatheringAction])
            {
                return false;
            }

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var missionInfo) && missionInfo.IsAddonReady)
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

                            shouldTurnin = (config.TurninGoal is TurninState.Gold && rank >= MissionRank.Gold) ||
                                           (config.TurninGoal is TurninState.Silver && rank >= MissionRank.Silver) ||
                                           (config.TurninGoal is TurninState.Bronze && rank >= MissionRank.Bronze);
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
            else if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var moonHud) && moonHud.IsAddonReady)
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

            var currentScore = CurrentScore();
            var rank = CurrentRank();

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var missionInfo) && missionInfo.IsAddonReady)
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

                            shouldTurnin = (config.TurninGoal is TurninState.Gold && rank >= MissionRank.Gold) ||
                                           (config.TurninGoal is TurninState.Silver && rank >= MissionRank.Silver) ||
                                           (config.TurninGoal is TurninState.Bronze && rank >= MissionRank.Bronze);
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
            else if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var moonHud) && moonHud.IsAddonReady)
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

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var missionInfo) && missionInfo.IsAddonReady)
            {
                var currentScore = CurrentScore();
                var rank = CurrentRank();
                var Id = CosmicHelper.CurrentLunarMission;

                if (CosmicHandler.IsMissionTimedOut())
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

            return managerPtr->CollectedTotal;
        }
        private static unsafe uint CurrentIndividualTotal()
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return 0;
            return managerPtr->CollectedIndividual;
        }
        private static unsafe uint CurrentScore()
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return 0;
            return managerPtr->CurrentScore;
        }
        public static unsafe MissionRank CurrentRank()
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return MissionRank.None;

            return managerPtr->CurrentRank;
        }
    }
}
