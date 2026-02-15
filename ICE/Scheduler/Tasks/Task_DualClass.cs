using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ICE.Ui.DebugWindowTabs;
using ICE.Utilities.Cosmic_Helper;
using ICE.Utilities.GatheringHelper;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class Task_DualClass
    {
        private static FishingDebug _fishingDebug = null;

        public static void Enqueue()
        {
            Task_CheckScore.Enqueue();
            P.TaskManager.Enqueue(() => CheckMaterials(), "Checking status for dual craft missions", Utils.TaskConfig);
        }

        private static unsafe bool? CheckMaterials()
        {
            if (P.Artisan.IsBusy())
            {
                if (EzThrottler.Throttle("Waiting for artisan to finish making macro's...", 3000))
                    IceLogging.Debug("Waiting for artisan to finish making macros");

                return false;
            }

            var id = CosmicHelper.CurrentLunarMission;
            var mission = CosmicHelper.SheetMissionDict[id];
            var missionConfig = C.MissionConfig[id];
            var crafterJobId = mission.Jobs.Where(x => CosmicHelper.CrafterJobList.Contains(x)).FirstOrDefault();
            var gatheringJobId = mission.Jobs.Where(x => CosmicHelper.GatheringJobList.Contains(x)).FirstOrDefault();

            // Should only be one of these existing here for dual crafts.
            var mainCraft = mission.Crafts_Main.Values.FirstOrDefault();
            var recipeId = mission.Crafts_Main.Keys.FirstOrDefault();
            var itemId = mainCraft.ItemId;

            var gatherProfileId = C.MissionConfig[id].GProfileId;
            var dualCraftAmount = 3;

            if (missionConfig.TurninGold || missionConfig.AutoTurnin)
            {
                // Currently, gold needs x3 of the items
                dualCraftAmount = 3;
            }
            else if (missionConfig.TurninSilver)
            {
                // Currently, silver needs x2 of the items
                dualCraftAmount = 2;
            }
            if (PlayerHelper.GetItemCount(mainCraft.ItemId, out var craftAmount) && craftAmount >= dualCraftAmount)
            {
                // Anything extra after we initial crafts, really just want to only craft x1
                dualCraftAmount = 1;
            }

            foreach (var requiredItem in mainCraft.RequiredItems)
            {
                uint crateId = 48233;

                var materialItemId = requiredItem.Key;
                var amountNeeded = requiredItem.Value;

                if (materialItemId == crateId && PlayerHelper.GetItemCount(crateId, out var crateAmount) && crateAmount == 0)
                {
                    // This means we've ran out of crates. Going to just exit out and abandon
                    SchedulerMain.State = IceState.AbandonMission;
                    P.TaskManager.Tasks.Clear();
                    IceLogging.Info("We've ran out of crates. Proceeding to turnin/abandon mission");
                    return true;
                }
                else
                {
                    if (PlayerHelper.GetItemCount(itemId, out var mainItemCount) && mainItemCount < dualCraftAmount)
                    {
                        amountNeeded = amountNeeded * dualCraftAmount;
                    }

                    if (PlayerHelper.GetItemCount(materialItemId, out var gatherAmount) && gatherAmount < amountNeeded)
                    {
                        // We don't have enough to craft. So going to actually exit out and enter the gathering state.
                        IceLogging.Info("We don't have enough to craft the dual class item, so proceeding to gather");
                        P.TaskManager.Tasks.Clear();
                        P.TaskManager.Insert(() => CheckGatheringState(), "Checking the current state of gathering");
                        return true;
                    }
                }
            }

            // If we've gotten this far. That means that we *-should-* be able to craft
            // Order of operations will be:
            // 1: If we're in the middle of a gathering node, need to exit out of it and wait for us to not be gathering/busy
            // 2: Swap to the crafter (if we're not there already)
            // 3: Tell Artisan to craft
            // 4: Wait for Artisan to not be busy

            if (Svc.Condition[ConditionFlag.Gathering])
            {
                if (GenericHelpers.TryGetAddonByName("Gathering", out AtkUnitBase* gather) && GenericHelpers.IsAddonReady(gather))
                {
                    if (EzThrottler.Throttle("Closing Gathering Window"))
                        ECommons.Automation.Callback.Fire(gather, true, -1);
                }
                else if ((uint)Player.Job == 18)
                {
                    StopFishing();
                }
                return false;
            }

            if ((uint)Player.Job != crafterJobId)
            {
                if (EzThrottler.Throttle("Swapping to crafter job"))
                    GearsetHandler.TaskClassChange((Job)crafterJobId);
                return false;
            }

            if (PlayerHelper.GetItemCount(itemId, out var count) && count < dualCraftAmount)
            {
                // We have enough to craft. Telling it to craft the item... x amount of times
                P.Artisan.CraftItem(recipeId, dualCraftAmount);
                P.TaskManager.Tasks.Clear();
                InsertArtisanWait();
                IceLogging.Info($"Told artisan to craft {dualCraftAmount} of the following recipe: {recipeId}");
                return true;
            }
            else
            {
                // We have enough for atleast 1 more craft, telling artisan to craft. Uno mas.
                P.Artisan.CraftItem(recipeId, 1);
                P.TaskManager.Tasks.Clear();
                InsertArtisanWait();
                IceLogging.Info($"Told Artisan to craft 1 item of the following recipe: {recipeId}");
                return true;
            }
        }

        private static unsafe bool? CheckGatheringState()
        {
            string handle = "[Task_DualClass | Check Gather State]";

            IceLogging.Debug("Starting 'Check Gather State'");

            var id = CosmicHelper.CurrentLunarMission;
            var mission = CosmicHelper.SheetMissionDict[id];
            var crafterJobId = mission.Jobs.Where(x => CosmicHelper.CrafterJobList.Contains(x)).FirstOrDefault();
            var gatheringJobId = mission.Jobs.Where(x => CosmicHelper.GatheringJobList.Contains(x)).FirstOrDefault();

            if (Svc.Condition[ConditionFlag.Crafting] || (Svc.Condition[ConditionFlag.PreparingToCraft]))
            {
                if (GenericHelpers.TryGetAddonByName("WKSRecipeNotebook", out AtkUnitBase* moonCraft) && GenericHelpers.IsAddonReady(moonCraft))
                {
                    if (EzThrottler.Throttle("Exiting out of the gathering state"))
                        ECommons.Automation.Callback.Fire(moonCraft, true, -1);
                }
                return false;
            }
            else if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var gatheringAddon))
            {
                IceLogging.Info($"We're currently in the middle of gathering, so going to just swap over to interacting with the gathering node", handle);
                P.TaskManager.Enqueue(() => GatheringInteraction(), "Interacting with the gathering node");
                return true;
            }
            else if ((uint)Player.Job != gatheringJobId)
            {
                if (EzThrottler.Throttle("Swapping to crafter job"))
                    GearsetHandler.TaskClassChange((Job)gatheringJobId);
                return false;
            }
            else if ((uint)Player.Job == 16 || (uint)Player.Job == 17)
            {
                bool selfRepairGather = C.SelfRepairGather && PlayerHelper.NeedsRepair(C.RepairPercent);

                if (selfRepairGather)
                {
                    IceLogging.Info("You have enabled self repair, and you are need in repair. throwing in task to repair self", handle);
                    P.TaskManager.EnqueueMulti
                    (
                        new(Task_Repair.OpenSelfRepair, "Opening the self repair window"),
                        new(Task_Repair.SelfRepair, "Executing the self repair"),
                        new(Task_Repair.CloseRepair, "Closing Self Repair")
                    );
                }

                // Us getting here means that we're fresh into the node gathering. So just going to queue up the rest of the gathering process.
                IceLogging.Info("You've gotten to this point so. Queueing up checking the gathering location, pathing to node, and navmesh movement", handle);
                P.TaskManager.Enqueue(() => Task_Gather.CheckCurrentLocation(), "Checking Gathering Location Info");
                P.TaskManager.Enqueue(() => Task_Gather.PathandCheckNode(), "Pathing to the gathering node");
                return true;
            }
            else if ((uint)Player.Job == 18)
            {
                IceLogging.Info("We're on a fishing job, so going fishing.", handle);
                bool selfRepairGather = C.SelfRepairGather && PlayerHelper.NeedsRepair(C.RepairPercent);

                if (selfRepairGather)
                {
                    IceLogging.Info("You have enabled self repair, and you are need in repair. throwing in task to repair self", "[Task_DualClass | Check Gather State]");
                    P.TaskManager.EnqueueMulti
                    (
                        new(Task_Repair.OpenSelfRepair, "Opening the self repair window"),
                        new(Task_Repair.SelfRepair, "Executing the self repair"),
                        new(Task_Repair.CloseRepair, "Closing Self Repair")
                    );
                }

                P.TaskManager.Enqueue(() => FishingCheck(), "Checking to see what to fish for");
                return true;
            }

            return false;
        }

        private static bool? WaitingForArtisan()
        {
            if (!P.Artisan.IsBusy())
            {
                IceLogging.Info("Artisan is no longer running, continuing the process");
                P.TaskManager.Tasks.Clear();
                return true;
            }
            else
            {
                if (Svc.Condition[ConditionFlag.ExecutingCraftingAction])
                {
                    // Need to add a timer check here. Make it configuarable maybe... 10s?
                    // If the timer exceeds 10 seconds, then that means we're stuck in an animation lock
                    // then need to cancel them all and just force abandon lock failsafe
                }
                if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var moonHud))
                {
                    if (!AddonHelper.IsAddonActive("WKSMissionInfomation"))
                    {
                        if (EzThrottler.Throttle("Opening mission scoring info"))
                        {
                            moonHud.Mission();
                        }
                    }
                }
            }
            return false;
        }
        private static void InsertArtisanWait()
        {
            P.TaskManager.Insert(() => WaitingForArtisan(), "Waiting for artisan to finish", Utils.TaskConfig);
        }

        #region Gathering

        public static unsafe bool? GatheringInteraction()
        {
            var missionInfo = CosmicHelper.CurrentMissionInfo;
            bool collectableItem = missionInfo.Attributes.HasFlag(MissionAttributes.Collectables);
            bool reduceItems = missionInfo.Attributes.HasFlag(MissionAttributes.ReducedItems);
            var configId = C.MissionConfig[CosmicHelper.CurrentLunarMission].GProfileId;
            if (C.GatherProfiles.TryGetValue(configId, out var gatherConfig))
            {

            }
            else
            {
                gatherConfig = C.GatherProfiles[0];
            }
            var gathActions = GatheringUtil.GathActionDict;

            var collectorBuffs = GatheringUtil.GathCollectableBuffs;
            var collectorAction = GatheringUtil.GathCollectableActions;
            var jobId = (uint)Player.Job;

            if (P.Navmesh.IsRunning())
            {
                if (EzThrottler.Throttle("Stopping navmesh, cause we shouldn't be running here"))
                    P.Navmesh.Stop();
            }

            if (Svc.Condition[ConditionFlag.Gathering])
            {
                // This should always be true while either
                // -> Enter gathering window
                // -> Using Actions
                // -> Gathering item
                // -> Exiting the gathering state.

                if (!Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                {
                    // We don't want to try and execute another action while we're currently in the middle of one

                    if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var gather) && gather.IsAddonReady)
                    {
                        // this is the first window that you see. 
                        if (gather.CurrentIntegrity != 0)
                        {
                            foreach (var action in Mission_Settings.SkillUseAmount)
                            {
                                var key = action.Key;
                                var amount = action.Value;
                                bool missingDur = gather.CurrentIntegrity != gather.TotalIntegrity;
                                int boonChance = gather.GatheredItems.FirstOrDefault().BoonChance;

                                bool useBuff = Task_Gather.CanUseGatheringAction(key, configId, missingDur, gather.TotalIntegrity, gather.CurrentIntegrity, boonChance);
                                if (EzThrottler.Throttle($"Checking buff: {key}"))
                                {
                                    IceLogging.Debug($"Action name: {key} | Using? {useBuff}");
                                }
                                if (useBuff)
                                {
                                    if (EzThrottler.Throttle($"Using Gathering Action: {key}"))
                                    {
                                        IceLogging.Debug($"Using the following action: {key} in full durability section", debugOnly: true);
                                        var actionId = gathActions[key].ClassAction[jobId];
                                        ActionManager.Instance()->UseAction(ActionType.Action, actionId);
                                        Mission_Settings.SkillUseAmount[key] += 1;
                                    }
                                    return false;
                                }
                            }

                            if (EzThrottler.Throttle("Gathering item for score"))
                            {
                                // if we're here, then we just need to gather for score. So... gathering for score lol
                                gather.GatheredItems.Where(x => x.ItemID != 0).FirstOrDefault().Gather();
                            }
                        }
                        else
                        {
                            // No more integrity is left, time to just wait for you to stop gathering
                        }
                    }
                }
                else
                {
                    P.TaskManager.Insert(() => WaitToGather());
                    return true;
                }
            }
            else
            {
                // No longer gathering an item. Time to check current state
                return true;
            }

            return false;
        }
        private static bool? WaitToGather()
        {
            if (!Svc.Condition[ConditionFlag.ExecutingGatheringAction])
            {
                IceLogging.Info("No longer executing a gathering action", "[Task Gather: Wait To Gather]");
                return true;
            }

            return false;
        }

        #endregion

        #region Fishing

        private static unsafe bool? FishingCheck()
        {
            string handle = "[Dual Class: Fishing Check]";

            if (_fishingDebug == null)
            {
                _fishingDebug = new FishingDebug();
            }

            if (CosmicHelper.CurrentBait == 0)
            {
                if (EzThrottler.Throttle("Equipping bait"))
                {
                    foreach (var bait in GatheringUtil.MoonBaits)
                    {
                        foreach (var baitId in bait.Value)
                        {
                            if (PlayerHelper.GetItemCount(baitId, out var count) && count > 0)
                            {
                                P.AutoHook.SwapBaitById(baitId);
                                IceLogging.Debug($"Telling it to equip bait ID: {baitId}", handle);
                                return false;
                            }
                        }
                    }
                }
                return false;
            }
            else if (!Svc.Condition[ConditionFlag.Gathering])
            {
                if (!_fishingDebug.IsFishable())
                {
                    if (_fishingDebug.FindFishableLocation(out var fishablePos, searchSteps: 64))
                    {
                        IceLogging.Info("We're not in a fishable spot, so going to face one", handle);
                        P.TaskManager.Tasks.Clear();
                        P.TaskManager.Enqueue(() => Task_Fishing.FacePosition(fishablePos.Value));
                        return true;
                    }
                    else
                    {
                        IceLogging.Debug("Our current fishing position isn't viable. So going to move to the next fishing spot");
                        var mission = CosmicHelper.CurrentMissionInfo;
                        var flag = mission.MapPosition;
                        var territoryId = mission.TerritoryId;

                        var nextFishingSpot = Task_Fishing.GetNextFishingSpot(territoryId, flag, Player.Position);
                        if (nextFishingSpot != null)
                        {
                            IceLogging.Info($"We found another fishing spot to move to! {nextFishingSpot.FishingSpot} | moving to it");
                            P.TaskManager.Tasks.Clear();
                            P.TaskManager.Enqueue(() => Task_Fishing.InitiateMoving(nextFishingSpot.FishingSpot), "Vnav moving to fishing");
                            return true;
                        }
                    }
                }
                else if (EzThrottler.Throttle("Starting to fish", 1000))
                {
                    IceLogging.Debug("Telling it to start fishing", handle);
                    ActionManager.Instance()->UseAction(ActionType.Action, 289);
                }
                return false;
            }
            else
            {
                // Means we are fishing, all we need to do is enable autohook then wait for us to get the amount of fish we need
                P.AutoHook.SetPluginState(true);
                IceLogging.Info("We're starting to fish. So kicking it over to checking the fish items", handle);
                P.TaskManager.Insert(() => CheckItems(), "Checking for items to meet the quantity set", Utils.TaskConfig);
                return true;
            }
        }

        private static unsafe bool? CheckItems()
        {
            string handle = "[Dual Class: Check Items]";

            if (!Svc.Condition[ConditionFlag.Gathering])
            {
                IceLogging.Info("We've stopped fishing for some reason... going to go back and check if we have enough of the materials, or just ran out of bait", handle);
                P.TaskManager.Tasks.Clear();
                return true;
            }
            else
            {
                // Currently in the middle of gathering here. Going to check for just general item progress.
                var mission = CosmicHelper.CurrentMissionInfo;
                if (EzThrottler.Throttle("Checking for current item count"))
                {
                    var mainCraft = mission.Crafts_Main.Values.FirstOrDefault();
                    var missionConfig = C.MissionConfig[CosmicHelper.CurrentLunarMission];

                    // Need to really figure out what all we need... 
                    // If going for gold, should really only need x3 of the item
                    // If going for silver, should really only need x2 of the item
                    // Once we get the initial multiplier amount, should only go for x1 more for the crafts

                    var itemAmount = 1;
                    if (missionConfig.TurninGold || missionConfig.AutoTurnin)
                    {
                        // Currently, gold needs x3 of the items
                        itemAmount = 3;
                    }
                    else if (missionConfig.TurninSilver)
                    {
                        // Currently, silver needs x2 of the items
                        itemAmount = 2;
                    }
                    if (PlayerHelper.GetItemCount(mainCraft.ItemId, out var craftAmount) && craftAmount >= itemAmount)
                    {
                        // Anything extra after we initial crafts, really just want to only craft x1
                        itemAmount = 1;
                    }
                    if (EzThrottler.Throttle("Item Multiplier Amount", 5000))
                    {
                        IceLogging.Info($"[Fishing] Item Multiplier: {itemAmount}", handle ,debugOnly: true);
                    }

                    foreach (var requiredItem in mainCraft.RequiredItems)
                    {
                        uint crateId = 48233;
                        var materialItemId = requiredItem.Key;
                        var amountNeeded = requiredItem.Value;
                        if (materialItemId == crateId) continue;
                        amountNeeded = amountNeeded * itemAmount; // This is to make sure the multiplier is there if needing to craft multiple (aka if 7 * 3 for gold)

                        if (PlayerHelper.GetItemCount(materialItemId, out var mainItemCount) && mainItemCount < amountNeeded)
                        {
                            if (EzThrottler.Throttle("Item Count Checker", 5000))
                            {
                                IceLogging.Debug($"We still need {materialItemId}, so still fishing", handle);
                            }
                        }
                        else
                        {
                            IceLogging.Info($"Current Amount: {mainItemCount} | Amount needed: {amountNeeded} is complete", debugOnly: true);
                            StopFishing();
                        }
                    }
                }

                return false;
            }
        }

        public static unsafe void StopFishing()
        {
            if (EzThrottler.Throttle("Telling it to stop fishing"))
            {
                ActionManager.Instance()->UseAction(ActionType.Action, 299);
            }
        }

        #endregion
    }
}