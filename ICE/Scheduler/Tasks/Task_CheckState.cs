using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Sounds;
using ICE.Utilities.Cosmic;
using ICE.Utilities.Cosmic_Helper;
using System.Collections.Generic;
using YamlDotNet.Core.Tokens;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ICE.Utilities.CosmicHelper;

namespace ICE.Scheduler.Tasks
{
    internal static class Task_CheckState
    {
        public static void Enqueue()
        {
            P.TaskManager.Enqueue(() => CheckState(), "Checking to see what state we should be in");
        }

        private static unsafe bool? CheckState()
        {
            string tag = "Task: Check State";
            var currentMissionId = CosmicHelper.CurrentLunarMission;

            IceLogging.Debug("Task: Check State, has commenced");

            if (!C.ShowManualMode)
            {
                if (C.MissionConfig.Any(x => x.Value.ManualMode))
                {
                    foreach (var mission in C.MissionConfig.Where(x => x.Value.ManualMode))
                    {
                        mission.Value.ManualMode = false;
                    }
                    C.Save();
                    IceLogging.ChatInfo("You turned on manual mode, then turned it off. \n" +
                                        "So we did you a favor and stopped yourself from asking questions", "Ice's Cosmic");
                }
            }

            if (AddonHelper.IsAddonActive("WKSLottery"))
            {
                IceLogging.Info("Setting State to gambling");
                SchedulerMain.State = IceState.Gambling;
                return true;
            }
            else
            {
                RelicInfo(out var allComplete, out var currentStage, out var XPTable);
                bool canTurnin = allComplete && currentStage != CosmicHelper.MaxRelicLevel && C.TurninRelic;

                if (C.StopWhenLevel && Player.Level >= C.TargetLevel)
                {
                    SchedulerMain.State = IceState.Idle;
                    IceLogging.ChatInfo("Stop At Player Level is enabled. \n" +
                                       $"Your current level is: {Player.Level} and Goal: {C.TargetLevel}", "[I.C.E.]");
                    if (C.PlaySoundAlert)
                    {
                        _ = SoundPlayer.PlaySoundAsync();
                    }

                    return true;
                }
                if (C.StopOnceHitCosmicScore)
                {
                    var scores = CosmicHelper.GetCosmicClassScores();

                    if (scores.classScore >= C.CosmicScoreCap)
                    {
                        IceLogging.ChatInfo("Stop At Cosmic Score is enabled. \n" +
                                           $"Your current level is: {scores.classScore} and Goal: {C.CosmicScoreCap}", "[I.C.E.]");
                        SchedulerMain.State = IceState.Idle;
                        if (C.PlaySoundAlert)
                        {
                            _ = SoundPlayer.PlaySoundAsync();
                        }
                        return true;
                    }
                }
                if (C.StopOnceHitLunarCredits)
                {
                    var territory = Player.Territory.RowId;
                    var itemId = CosmicHelper.PlanetCreditInfo[territory];

                    PlayerHelper.GetItemCount(itemId, out var credits);
                    if (credits >= C.LunarCreditsCap)
                    {
                        IceLogging.ChatInfo($"You've either hit the Lunar Credit threshold, or gone above it.\n" +
                                            $"Stopping I.C.E.", "[I.C.E.]");
                        SchedulerMain.State = IceState.Idle;
                        if (C.PlaySoundAlert)
                        {
                            _ = SoundPlayer.PlaySoundAsync();
                        }
                        return true;
                    }
                }
                if (C.StopOnceHitCosmoCredits && !C.BuyItems)
                {
                    if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && (hud.CosmoCredit >= C.CosmoCreditsCap))
                    {
                        IceLogging.ChatInfo($"Stopping the plugin as you have {hud.CosmoCredit} Cosmocredits.", "[I.C.E.]");
                        SchedulerMain.State = IceState.Idle;
                        if (C.PlaySoundAlert)
                        {
                            _ = SoundPlayer.PlaySoundAsync();
                        }
                        return true;
                    }
                }
                if (C.StopOnceRelicFinished)
                {
                    if (allComplete && !canTurnin)
                    {
                        IceLogging.Debug("It says all have been completed. This is the current report");
                        for (int i = 0; i < XPTable.Count; i++)
                        {
                            var bar = XPTable[i + 1];
                            IceLogging.Debug($"Kind: [{i+1}] | Current: {bar.CurrentXP} | Needed: {bar.NeededXP}");
                        }
                        IceLogging.Debug("Config Status:\n" +
                                        $"Turnin Relic: {C.TurninRelic}\n" +
                                        $"Stop When Relic Finished: {C.StopOnceRelicFinished}");

                        IceLogging.Info("You have met all necessary relic xp, and you have \"Stop on Relic Completion\" enabled, so stopping for now");
                        SchedulerMain.State = IceState.Idle;
                        if (C.PlaySoundAlert)
                        {
                            _ = SoundPlayer.PlaySoundAsync();
                        }
                        return true;
                    }
                    else
                    {
                        IceLogging.Debug($"Check Relic XP has been concluded, and we still need some. So going to continue on because we don't need to stop.");
                    }



                    if (allComplete)
                    {

                        if (C.TurninRelic && currentStage != CosmicHelper.MaxRelicLevel)
                        {
                            IceLogging.Info("We've hit a point where we can turnin the relic! Going to add a thing to check for that later");
                        }
                        else if (C.TurninRelic && currentStage == CosmicHelper.MaxRelicLevel && C.StopOnceRelicFinished)
                        {
                            IceLogging.Info("You have met all necessary relic xp, and you have \"Stop on Relic Completion\" enabled, so stopping for now");
                            SchedulerMain.State = IceState.Idle;
                            if (C.PlaySoundAlert)
                            {
                                _ = SoundPlayer.PlaySoundAsync();
                            }
                            return true;
                        }
                    }
                }
                if (C.FarmAllRelics) // checking for all class completions here
                {
                    foreach (var jobId in C.RelicJobs)
                    {
                        var enabled = jobId.Value;
                        var job = (Job)jobId.Key;

                        if (Player.GetLevel(job) < 10 && enabled)
                        {
                            IceLogging.Debug($"Skipping job: {jobId}");
                            continue;
                        }

                        RelicInfo(out var jobComplete, out var jobStage, out var jobXpTable);
                        if (!jobComplete)
                        {
                            IceLogging.Debug($"We found a job that still needs to be completed! {jobId} Current stage: {jobStage}.");
                            if (Player.Job != job)
                            {
                                if (EzThrottler.Throttle("Swapping jobs"))
                                    GearsetHandler.TaskClassChange(job);

                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    if (C.Stop_AllRelicsComplete) // If we've gotten this far, and we're telling us to stop when all relics that are enabled are completed
                    {
                        IceLogging.Info("All relics that you have selected have been completed for this planet! And you've said to stop once we're done.");
                        SchedulerMain.State = IceState.Idle;
                        if (C.PlaySoundAlert)
                        {
                            _ = SoundPlayer.PlaySoundAsync();
                        }
                        return true;
                    }
                }
                if (currentMissionId != 0)
                {
                    IceLogging.Debug($"Current mission id is not 0, which means we're in the middle of a mission");
                    if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var missionInfo) && missionInfo.IsAddonReady)
                    {
                        IceLogging.Debug($"Mission Infomation was active, checking if a mission is timed out.");
                        if (CosmicHandler.IsMissionTimedOut())
                        {
                            // Mission time has reached 0, checking the score/aborting if necessary
                            IceLogging.Info("Mission is currently timed out. Going to abandon the mission state", "[Task: Check State]");
                            SchedulerMain.State = IceState.AbandonMission;
                            P.TaskManager.Tasks.Clear();
                            return true;
                        }
                        else
                        {
                            IceLogging.Debug($"Mission isn't timed out... checking other states");
                            UpdateMissionState(currentMissionId);
                            C.MissionConfig.TryGetValue(currentMissionId, out var config);

                            var s = SchedulerMain.MissionState;
                            bool dualMission = (s.HasFlag(MissionAttributes.Craft) && (s.HasFlag(MissionAttributes.Gather) || s.HasFlag(MissionAttributes.Fish)));
                            // In the middle of a dual mission. 
                            // First, checking to see if you're in the middle of a gathering or crafting action
                            if (C.OnlyGrabMission_Debug || config.ManualMode || UnsupportedMissions.Ids.Contains(currentMissionId))
                            {
                                // TODO: Remove this once properly coded
                                if (s.HasFlag(MissionAttributes.Fish))
                                {
                                    IceLogging.Info("Currently not built in/supported yet. Swapping to manual mode");
                                }
                                else
                                {
                                    IceLogging.Info($"You have either manual mode enabled, or you have OnlyGrabMission enabled. Swapping to manual mode state");
                                }
                                SchedulerMain.State = IceState.ManualMode;
                            }
                            else if (dualMission)
                            {
                                IceLogging.Info("We're in a dual craft mission, going to kick it over there", "[Task: Check State]");
                                Mission_Settings.ResetNodeCounter();
                                SchedulerMain.State = IceState.DualClass;
                            }
                            else if (Svc.Condition[ConditionFlag.Crafting] || P.Artisan.IsBusy())
                            {
                                IceLogging.Info("We are on a crafter, and either in the middle of crafting or need to start.", "[Task: Check State]");
                                SchedulerMain.State = IceState.Craft;
                            }
                            else if (Svc.Condition[ConditionFlag.Gathering])
                            {
                                Mission_Settings.ResetNodeCounter();
                                IceLogging.Info("On a gathering class, kicking over to the gathering action", "[Task: Check State]");
                                SchedulerMain.State = IceState.Gather;
                            }
                            else if (s.HasFlag(MissionAttributes.Fish))
                            {
                                IceLogging.Debug("We seem to be in the middle of a fishing mission. Going to reset/import all the presets");
                                Task_ExecuteMission.FishingTask(currentMissionId);
                                SchedulerMain.State = IceState.ScoreCheck;
                            }
                            else
                            {
                                // Not currently in the middle of an action, so time to check score and go from there.
                                IceLogging.Debug("Not in the middle of an action, swapping to score checking", "[Task_CheckState]");
                                SchedulerMain.State = IceState.ScoreCheck;
                            }

                            return true;
                        }
                    }
                    else
                    {
                        // The mission info (the one that contains the timer + current score while a mission is active) isn't loaded. Going to fix that.
                        if (EzThrottler.Throttle("Attempting to open the mission information window"))
                        {
                            IceLogging.Info("Opening the mission information window, you're in the middle of one!", "[Check State]");
                            CosmicHelper.OpenStellarMission();
                        }
                        return false;
                    }
                }
                else
                {
                    if (PlayerHelper.IsInOizys() && C.Cosmodrone_Run)
                    {
                        IceLogging.Info("Checking to see if we can run cosmodrone hunting");
                        if (C.Cosmodrone_FinishCurrent && Task_ArtifactSearch.IsTreasureDetected())
                        {
                            IceLogging.Debug("A drone was apperently still loaded on the map, so going to just do a double check into making sure it's valid -> finding it if it exist");
                            P.TaskManager.Tasks.Clear();
                            P.TaskManager.Enqueue(() => Task_ArtifactSearch.RefreshMapInfo(), "Refreshing map info for drone check");
                            SchedulerMain.State = IceState.ArtifactSearch;
                            return true;
                        }
                        
                        if (PlayerHelper.GetItemCount(50414, out var count))
                        {
                            bool canRun = (C.Cosmodrone_RunAt == 0 && count > 0) || (count >= C.Cosmodrone_RunAt);
                            if (canRun)
                            {
                                IceLogging.Debug("We have enough drones to consider start running! So we're just going to kick it off");
                                P.TaskManager.Tasks.Clear();
                                P.TaskManager.Enqueue(() => Task_ArtifactSearch.RefreshMapInfo(), "Refreshing map info for drone check");
                                SchedulerMain.State = IceState.ArtifactSearch;
                                return true;
                            }
                        }
                    }

                    var currentJob = (uint)Player.Job;

                    bool repairVendor = C.RepairAtVendor && PlayerHelper.NeedsRepair(C.RepairPercent);
                    bool selfRepairCraft = C.SelfRepairCrafter && PlayerHelper.NeedsRepair(C.RepairPercent) && CosmicHelper.CrafterJobList.Contains(currentJob);
                    bool selfRepairGather = C.SelfRepairGather && PlayerHelper.NeedsRepair(C.RepairPercent) && CosmicHelper.GatheringJobList.Contains(currentJob);
                    bool extractSpiritbond = C.SelfSpiritbondGather && Task_Spiritbond.IsSpiritbondReadyAny();
                    PlayerHelper.GetItemCount(45690, out var cosmoCreditAmount);
                    bool canBuyItems = C.BuyItems && Task_BuyCosmoItems.CanPurchaseAnyItem() && cosmoCreditAmount >= C.CosmoBuyAtAmount;
                    bool canGamba = false;

                    bool canBuyDrones = PlayerHelper.IsInOizys() && C.Cosmodrone_Buy && Task_ArtifactSearch.CanBuyDroneBoxes();

                    var territory = Player.Territory.RowId;
                    var itemId = CosmicHelper.PlanetCreditInfo[territory];

                    if (C.GambaBetweenRuns)
                    {
                        if (PlayerHelper.GetItemCount(itemId, out var lunarCredits))
                        {
                            if (C.GambaAtAmount <= lunarCredits)
                                canGamba = true;
                            IceLogging.Debug($"Current Credit Setting: {C.GambaAtAmount} >= {lunarCredits} && AutoGamba: {C.GambaBetweenRuns}");
                        }
                    }

                    if (extractSpiritbond && CosmicHelper.GatheringJobList.Contains(currentJob))
                    {
                        IceLogging.Info("Extracting spiritbond is enabled. And you have some to extract. Going to go do so now", tag);
                        SchedulerMain.State = IceState.Spiritbond;
                    }
                    else if (!C.RepairAtVendor && (selfRepairCraft || selfRepairGather))
                    {
                        IceLogging.Info("We need to repair! So going to go repair", tag);
                        SchedulerMain.State = IceState.Repair;
                    }
                    else if (repairVendor || canTurnin || canBuyItems || canGamba || canBuyDrones)
                    {
                        SchedulerMain.State = IceState.HubReturn;
                        Task_HubActivities.RepairNpc = repairVendor;
                        Task_HubActivities.RelicTurnin = canTurnin;
                        Task_HubActivities.CosmoBuy = canBuyItems;
                        Task_HubActivities.CanGamba = canGamba;
                        Task_HubActivities.CanBuyDrones = canBuyDrones;
                        IceLogging.Info("We have some reason to return back to the base so... we're doing so.\n" +
                                        $"Repairing at NPC: {repairVendor}\n" +
                                        $"Relic Turnin: {canTurnin}\n" +
                                        $"Buying Cosmocredit Items: {canBuyItems}\n" +
                                        $"Can Gamba: {canGamba}\n" +
                                        $"Can buy drones: {canBuyDrones}");
                    }
                    else
                    {
                        IceLogging.Info("Not in the middle of a mission, and don't need to repair/extract materia. So going to grab mission", tag);
                        SchedulerMain.State = IceState.GrabMission;
                    }

                    IceLogging.Info($"There is no physical possible way for you to not be in a different state here. . . So reporting back the current state upon exiting here: {SchedulerMain.State}", tag);
                    return true;
                }
            }
        }

        private static void UpdateMissionState(uint missionId)
        {
            // Clearing the current mission modifiers.
            SchedulerMain.MissionState = MissionAttributes.None;

            // Grabbing the mission info from the dictionary entry
            var missionDictInfo = CosmicHelper.SheetMissionDict[missionId];

            // Updating the Mission state to be the same as the current mission that's fired.
            SchedulerMain.MissionState = missionDictInfo.Attributes;
        }

        private static unsafe bool RelicInfo(out bool isComplete, out int currentStage, out Dictionary<int, XPType> XPTable, uint jobId = 0)
        {
            var maxStage = CosmicHelper.MaxRelicLevel;

            string tag = "Relic Info Check";
            currentStage = 0; // Must initialize out parameters
            isComplete = false; // Must initialize out parameters
            XPTable = new();

            var wksManager = WKSManager.Instance();
            if (wksManager == null || wksManager->ResearchModule == null || !wksManager->ResearchModule->IsLoaded)
            {
                return false;
            }

            uint job = 8;

            if (jobId == 0)
                job = (uint)Player.Job;
            else
                job = jobId;

            var toolClassId = (byte)(job - 7);
            var stage = wksManager->ResearchModule->CurrentStages[toolClassId - 1];
            var nextstate = wksManager->ResearchModule->UnlockedStages[toolClassId - 1];

            currentStage = stage;

            if (currentStage != maxStage)
            {
                for (byte type = 1; type < 7; type++)
                {
                    if (!wksManager->ResearchModule->IsTypeAvailable(toolClassId, type))
                    {
                        continue;
                    }

                    var neededXP = wksManager->ResearchModule->GetNeededAnalysis(toolClassId, type);
                    var currentXp = wksManager->ResearchModule->GetCurrentAnalysis(toolClassId, type);

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
                // We're checking to make sure the max stage is completed
                for (byte type = 1; type < 7; type++)
                {
                    if (!wksManager->ResearchModule->IsTypeAvailable(toolClassId, type))
                    {
                        continue;
                    }

                    var maxXP = wksManager->ResearchModule->GetMaxAnalysis(toolClassId, type);
                    var currentXp = wksManager->ResearchModule->GetCurrentAnalysis(toolClassId, type);


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

                for (int i = 0; i < XPTable.Count; i++)
                {
                    var bar = XPTable[i + 1];
                    IceLogging.Debug($"Checking: [{i+1}] Current: {bar.CurrentXP} | Needed: {bar.NeededXP}", tag);
                    if (bar.CurrentXP < bar.NeededXP)
                    {
                        IceLogging.Debug($"We're missing XP, so going to change this to false");
                        isComplete = false;
                        return false;
                    }
                }

            isComplete = true;
            return true;
        }
    }
}
