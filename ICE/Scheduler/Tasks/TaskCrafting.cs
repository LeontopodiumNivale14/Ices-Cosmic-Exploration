using Dalamud.Game.ClientState.Conditions;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskCrafting
    {
        public static void TryEnqueueCrafts()
        {
            if (CosmicHelper.CurrentLunarMission != 0)
            {
                Job targetClass;
                if (((Job)CosmicHelper.CurrentMissionInfo.JobId).IsDoh())
                    targetClass = (Job)CosmicHelper.CurrentMissionInfo.JobId;
                else if (((Job)CosmicHelper.CurrentMissionInfo.JobId2).IsDoh())
                    targetClass = (Job)CosmicHelper.CurrentMissionInfo.JobId2;
                else
                    return;
                if ((Job)PlayerHelper.GetClassJobId() != targetClass)
                    GearsetHandler.TaskClassChange(targetClass);
                else
                    MakeCraftingTasks();
            }
        }

        internal static void MakeCraftingTasks()
        {
            var (currentScore, bronzeScore, silverScore, goldScore) = MissionHandler.GetCurrentScores();

            if (currentScore == 0 && silverScore == 0 && goldScore == 0)
            {
                (currentScore, bronzeScore, silverScore, goldScore) = MissionHandler.GetCurrentScores();
                if (currentScore == 0 && silverScore == 0 && goldScore == 0)
                {
                    IceLogging.Debug("Failed to get scores, aborting");
                    return;
                }
            }

            if (MissionHandler.IsMissionTimedOut())
            {
                SchedulerMain.State |= IceState.AbortInProgress;
                return;
            }

            if (currentScore >= goldScore)
            {
                IceLogging.Debug("We reached gold, switching to Score Check.");
                SchedulerMain.State |= IceState.ScoringMission;
                return;
            }


            if (!P.TaskManager.IsBusy) // ensure no pending tasks or manual craft while plogon enabled
            {
                SchedulerMain.PossiblyStuck = 0;

                var needPreCraft = false;
                var itemsToCraft = new Dictionary<ushort, Tuple<int, int>>();
                var preItemsToCraft = new Dictionary<ushort, Tuple<int, int>>();

                var mainCrafts = CosmicHelper.CurrentMoonRecipe.MainCraftsDict;
                var preCrafts = CosmicHelper.CurrentMoonRecipe.PreCraftDict;

                // Calculate if we need to do more than base amount of crafts
                int craftsDone = mainCrafts.Sum(main => PlayerHelper.GetItemCount((int)ExcelHelper.RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId, out var count) ? count : 0); // How many mains we made
                int craftsNeeded = mainCrafts.Sum(main => main.Value); // How many we need for mission
                int CraftMultipleMissionItems = (craftsDone / craftsNeeded) + 1; // How many whole sets (+1) of crafts we did
                IceLogging.Debug($"[Crafting] Loop Number: {CraftMultipleMissionItems} | Items Done: {craftsDone} | Items Needed: {craftsNeeded}");

                bool OOMMain = false;
                bool OOMSub = false;

                foreach (var main in mainCrafts)
                {
                    var itemId = ExcelHelper.RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
                    var subItem = ExcelHelper.RecipeSheet.GetRow(main.Key).Ingredient[0].Value.RowId; // need to directly reference this in the future
                    var mainNeed = main.Value;
                    if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Gather) && CraftMultipleMissionItems == 1) // For Dual Class specifically, if we doing a mult - use it on first loop.
                        mainNeed *= SchedulerMain.InitialGatheringItemMultiplier;
                    var subItemNeed = ExcelHelper.RecipeSheet.GetRow(main.Key).AmountIngredient[0].ToInt() * main.Value;

                    if (!PlayerHelper.GetItemCount((int)itemId, out var currentAmount))
                    {
                        IceLogging.Error($"[Crafting] Failed to get item count of {itemId} ({currentAmount})");
                        return;
                    }

                    if (!PlayerHelper.GetItemCount((int)subItem, out var currentSubItemAmount))
                    {
                        IceLogging.Error($"[Crafting] Failed to get sub item count of {subItem} ({currentSubItemAmount})");
                        return;
                    }

                    var mainItemName = ExcelHelper.ItemSheet.GetRow(itemId).Name.ToString();

                    IceLogging.Info($"RecipeID: {main.Key}");
                    //IceLogging.Info($"ItemID: {itemId}");

                    if ((currentSubItemAmount / (subItemNeed / mainNeed)) == 0) // This should OOM only if not enough to craft a single Main
                    {
                        IceLogging.Info($"[OOM] Not enough to craft main item");
                        OOMMain = true; // All current 3x Main items share Sub items
                    }

                    IceLogging.Debug($"Main ItemID: {itemId} [{mainItemName}] | Current Amount: {currentAmount} | RecipeId {main.Key}");
                    IceLogging.Debug($"Required Items for Recipe: ItemID: {subItem} | Currently have: {currentSubItemAmount} | Amount Needed [Base]: {subItemNeed}");

                    // Increase how many crafts we want to have made if needed so we can reach Score Checker goals.
                    subItemNeed = subItemNeed * CraftMultipleMissionItems;
                    mainNeed = mainNeed * CraftMultipleMissionItems;

                    if (currentAmount < mainNeed)
                    {
                        subItemNeed = subItemNeed - currentAmount;

                        if (currentSubItemAmount >= subItemNeed)
                        {
                            IceLogging.Debug($"Pre-crafts present.");
                            int craftAmount = mainNeed - currentAmount;
                            itemsToCraft.Add(main.Key, new(craftAmount, mainNeed));
                        }
                        else
                        {
                            IceLogging.Debug($"Pre-crafts required.");
                            int craftAmount = mainNeed - currentAmount;
                            itemsToCraft.Add(main.Key, new(craftAmount, mainNeed));
                            needPreCraft = true;
                        }
                    }
                }

                if (needPreCraft)
                {
                    if (PlayerHelper.GetItemCount(48233, out var count) && count == 0) // Subs only have Cosmo Containers as requirement.
                    {
                        IceLogging.Info($"[Crafting] [OOM] Not enough to craft sub item");
                        OOMSub = true;
                    }
                    else
                    {
                        //IceLogging.Debug($"You need pre-craft items. Starting the process of finding pre-crafts");
                        foreach (var pre in preCrafts)
                        {
                            var itemId = ExcelHelper.RecipeSheet.GetRow(pre.Key).ItemResult.Value.RowId;
                            if (PlayerHelper.GetItemCount((int)itemId, out var currentAmount))
                            {

                                var PreCraftItemName = ExcelHelper.ItemSheet.GetRow(itemId).Name.ToString();
                                IceLogging.Debug($"Checking Pre-crafts to see if {itemId} [{PreCraftItemName}] has enough.");
                                IceLogging.Debug($"Item Amount: {currentAmount} | Goal Amount: {pre.Value} | RecipeId: {pre.Key}");
                                var goalAmount = pre.Value;

                                if (currentAmount < goalAmount)
                                {
                                    IceLogging.Debug($"Found an item that needs to be crafted: {itemId} | Item Name: {PreCraftItemName}");
                                    int craftAmount = goalAmount - currentAmount;
                                    preItemsToCraft.Add(pre.Key, new(craftAmount, goalAmount));
                                }
                            }
                            else
                            {
                                IceLogging.Error($"[Crafting] Failed to get item count of {itemId}");
                            }
                        }
                    }
                }
#if DEBUG
                OOMMain = OOMMain || SchedulerMain.DebugOOMMain;
                OOMSub = OOMSub || SchedulerMain.DebugOOMSub;
#endif

                if (OOMMain && (OOMSub || !needPreCraft) && CosmicHelper.CurrentLunarMission < 361 && !SchedulerMain.AnimationLockAbandonState) // We only OOM if both are true: 1) Main is OOM, 2) Either Sub is OOM and we somehow don't need PreCrafts.
                {
                    IceLogging.Error($"[Crafting] [OOM] Not enough to craft");
                    SchedulerMain.State = IceState.AbortInProgress;
                    return;
                }

                P.TaskManager.BeginStack(); // Enable stack mode

                if (preItemsToCraft.Count > 0)
                {
                    IceLogging.Debug("Queuing up pre-craft items");
                    foreach (var craft in preItemsToCraft)
                    {
                        EnqueueCraft(craft);
                    }
                }

                if (itemsToCraft.Count > 0)
                {
                    IceLogging.Debug("Queuing up main craft items");
                    foreach (var craft in itemsToCraft)
                    {
                        EnqueueCraft(craft);
                    }
                }

                if (P.TaskManager.NumQueuedTasks == 1 && C.Missions.SingleOrDefault(x => x.Id == CosmicHelper.CurrentLunarMission).Type == MissionType.Critical)
                {
                    P.TaskManager.Enqueue(() =>
                    {
                        if (EzThrottler.Throttle("Manual Synthesis"))
                            if (GenericHelpers.TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var recipe) && recipe.IsAddonReady)
                            {
                                IceLogging.Debug("Starting manual synthesis of Critical Mission item");
                                recipe.Synthesize();
                            }
                    });
                }

                if (C.DelayCraft)
                    P.TaskManager.EnqueueDelay(C.DelayCraftIncrease); // Post-craft delay between Synthesis and RecipeLog reopening
                P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.NormalConditions] || (Svc.Condition[ConditionFlag.Crafting] && Svc.Condition[ConditionFlag.PreparingToCraft]));
                P.TaskManager.Enqueue(() =>
                {
                    IceLogging.Debug("Check score and turn in cause crafting is done.");
                    SchedulerMain.State |= IceState.ScoringMission;
                }, "Check score and turn in if complete");

                P.TaskManager.EnqueueStack();
            }
        }

        private static void EnqueueCraft(KeyValuePair<ushort, Tuple<int, int>> craft)
        {
            var item = ExcelHelper.ItemSheet.GetRow(ExcelHelper.RecipeSheet.GetRow(craft.Key).ItemResult.RowId);
            IceLogging.Debug($"Adding craft {craft} | {item.Name}");
            P.TaskManager.Enqueue(() => !P.Artisan.IsBusy());
            P.TaskManager.Enqueue(() => Craft(craft.Key, craft.Value.Item1, item), "PreCraft item");
            P.TaskManager.EnqueueDelay(2000); // Give artisan a moment before we track it.
            P.TaskManager.Enqueue(() => SchedulerMain.State |= IceState.Waiting, "Change state to wait for crafts");
            P.TaskManager.Enqueue(() => WaitTillActuallyDone(), "Wait for item", new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration()
            {
                TimeLimitMS = CosmicHelper.CurrentMissionInfo.TimeLimit == 0 ? 240000 : (int?)CosmicHelper.CurrentMissionInfo.TimeLimit * 1000, // Limit to mission time limit (If no limit - 4 minute limit per craft)
            });
        }


        internal static void Craft(ushort id, int craftAmount, Item item)
        {
#if DEBUG
            if (C.FailsafeRecipeSelect)
            {
                if (GenericHelpers.TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var m) && m.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Selecting Item"))
                    {
                        if (!m.SelectedCraftingItem.Contains($"{item.Name}"))
                        {
                            foreach (var i in m.CraftingItems)
                            {
                                if (i.Name.Contains(item.Name.ToString()))
                                {
                                    IceLogging.Debug($"Selecting item: {i.Name}");
                                    i.Select();
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
#endif

            IceLogging.Debug($"Telling Artisan to use recipe: {id} | {craftAmount} for {item.Name}");
            P.Artisan.CraftItem(id, craftAmount);
        }

        internal static bool? WaitTillActuallyDone()
        {
            if (EzThrottler.Throttle("WaitTillActuallyDone", 1000))
            {
                if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Critical))
                {
                    if ((Svc.Condition[ConditionFlag.NormalConditions] || Svc.Condition[ConditionFlag.PreparingToCraft]) && !P.Artisan.IsBusy())
                    {
                        IceLogging.Debug("We seem to no longer be crafting");
                        SchedulerMain.State |= IceState.ScoringMission;
                        SchedulerMain.State &= ~IceState.Waiting;
                        return true;
                    }
                    else
                        return false;
                }
                var (currentScore, bronzeScore, silverScore, goldScore) = MissionHandler.GetCurrentScores(); // some scoring checks
                var currentMission = C.Missions.SingleOrDefault(x => x.Id == CosmicHelper.CurrentLunarMission);
                var enoughMain = MissionHandler.HaveEnoughMain();
                if (enoughMain == null || currentMission == null)
                {
                    IceLogging.Error($"[Crafting] [Wait] Current mission is {CosmicHelper.CurrentLunarMission}, aborting");
                    SchedulerMain.State = IceState.GrabMission;
                    P.TaskManager.Abort();
                    return true;
                }
                uint targetLevel = 0;
                if (currentMission.TurnInGold)
                    targetLevel = 3;
                else if (currentMission.TurnInSilver)
                    targetLevel = 2;
                else if (currentMission.TurnInASAP)
                    targetLevel = 1;

                if (currentScore >= goldScore && enoughMain.Value)
                {
                    IceLogging.Debug("Gold wanted. Gold reached.");
                    SchedulerMain.State |= IceState.ScoringMission;
                    SchedulerMain.State &= ~IceState.Waiting;
                    P.Artisan.SetEnduranceStatus(false);
                    return true;
                }
                else if (targetLevel == 2 && currentScore >= silverScore && enoughMain.Value)
                {
                    IceLogging.Debug("Silver wanted. Silver reached.");
                    SchedulerMain.State |= IceState.ScoringMission;
                    SchedulerMain.State &= ~IceState.Waiting;
                    P.Artisan.SetEnduranceStatus(false);
                    return true;
                }
                else if (targetLevel == 1 && (currentScore >= bronzeScore || bronzeScore == 0) && enoughMain.Value)
                {
                    IceLogging.Debug("Bronze wanted. Turning in.");
                    SchedulerMain.State |= IceState.ScoringMission;
                    SchedulerMain.State &= ~IceState.Waiting;
                    P.Artisan.SetEnduranceStatus(false);
                    return true;
                }

                if ((Svc.Condition[ConditionFlag.PreparingToCraft] || Svc.Condition[ConditionFlag.NormalConditions]) && !P.Artisan.IsBusy())
                {
                    IceLogging.Debug("We seem to no longer be crafting");
                    SchedulerMain.State |= IceState.ScoringMission;
                    SchedulerMain.State &= ~IceState.Waiting;
                    return true;
                }
            }
            return false;
        }
    }
}
