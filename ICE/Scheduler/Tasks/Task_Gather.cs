using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ICE.Utilities.Cosmic_Helper;
using ICE.Utilities.GatheringHelper;
using System.Collections.Generic;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ICE.ConfigFiles.Config;

namespace ICE.Scheduler.Tasks
{
    internal static class Task_Gather
    {

        public static void Enqueue()
        {
            if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var gather) && gather.IsAddonReady || GenericHelpers.TryGetAddonMaster<GatheringMasterpiece>("GatheringMasterpiece", out var collectable) && collectable.IsAddonReady)
            {
                IceLogging.Debug("Current in a gathering session");
                Task_CheckScore.Enqueue();
                P.TaskManager.Enqueue(() => GatherInteractV2(), "Interacting with gathering menu", Utils.TaskConfig);
            }
            else
            {
                IceLogging.Debug("Not currently gathering, starting fresh instead");
                P.TaskManager.EnqueueDelay(100);
                if (CosmicHelper.SheetMissionDict[CosmicHelper.CurrentLunarMission].Attributes.HasFlag(MissionAttributes.ReducedItems))
                {
                    Task_CheckScore.Enqueue();
                    P.TaskManager.Enqueue(() => CheckReduceMission(), "Checking to see if we need to reduce items");
                    P.TaskManager.EnqueueDelay(500);
                    Task_CheckScore.Enqueue();
                }
                else
                {
                    Task_CheckScore.Enqueue();
                }
                P.TaskManager.Enqueue(() => Mission_Settings.ResetCollectableState());
                P.TaskManager.Enqueue(() => UseFood());
                P.TaskManager.Enqueue(() => CheckCurrentLocation(), "Checking to see if gathering flags needs updated");
                P.TaskManager.Enqueue(() => PathandCheckNode());
            }
        }

        public static bool? GatherInteractV2()
        {
            var missionInfo = CosmicHelper.CurrentMissionInfo;
            bool collectableItem = missionInfo.Attributes.HasFlag(MissionAttributes.Collectables);
            bool reduceItems = missionInfo.Attributes.HasFlag(MissionAttributes.ReducedItems);

            if (Svc.Condition[ConditionFlag.Gathering])
            {
                // We should always have this condition up while we're gathering. Even if a revisit happens
                if (!Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                {
                    // This should prevent us from actually attempting to do another gathering action, while we are currently doing one
                    if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var gather) && gather.IsAddonReady)
                    {
                        if (reduceItems || collectableItem)
                        {
                            // We need to find an item where it's a collectable so we can just initiate the gathering window
                            var item = gather.GatheredItems.Where(x => x.IsCollectable).FirstOrDefault();
                            if (item != null)
                            {
                                if (EzThrottler.Throttle("Collectable item select"))
                                {
                                    item.Gather();
                                    Mission_Settings.Collectable_BuffCount = GatheringUtil.CollectStandardCharges();
                                    IceLogging.Debug($"Gathering {item.ItemName} for collectability");
                                }
                            }
                        }
                        else
                        {
                            var configId = C.MissionConfig[CosmicHelper.CurrentLunarMission].GProfileId;

                            // just a normal item to gather. so we're just going to do our normal gathering process
                            bool missingDur = gather.CurrentIntegrity != gather.TotalIntegrity;
                            var testItem = gather.GatheredItems.Where(x => x.ItemID != 0).FirstOrDefault();
                            int gatherChance = testItem.GatherChance;
                            int boonChance = testItem.BoonChance;
                            int playerGp = PlayerHelper.GetGp();

                            if (UseGatherAction(configId, gatherChance, boonChance, gather.CurrentIntegrity, gather.TotalIntegrity, playerGp))
                            {
                                return false;
                            }

                            // Find the item with the largest deficit
                            var itemToGather = CosmicHelper.CurrentMissionInfo.Gathering_Min
                                .Select(x => new
                                {
                                    ItemId = x.Key,
                                    Required = x.Value,
                                    Current = PlayerHelper.GetItemCount(x.Key, out var count) ? count : 0,
                                    Deficit = x.Value - (PlayerHelper.GetItemCount(x.Key, out count) ? count : 0)
                                })
                                .Where(x => x.Deficit > 0) // Only items we still need
                                .OrderByDescending(x => x.Deficit) // Sort by largest deficit first
                                .FirstOrDefault();

                            if (itemToGather != null)
                            {
                                if (EzThrottler.Throttle("Gathering Item"))
                                {
                                    gather.GatheredItems
                                        .FirstOrDefault(x => x.ItemID == itemToGather.ItemId)
                                        ?.Gather();
                                }
                                return false;
                            }
                            else
                            {
                                // we must not need any of those items, so going to just do a first item gather
                                gather.GatheredItems.Where(x => x.ItemID != 0).FirstOrDefault().Gather();
                                return false;
                            }
                        }
                    }
                    else if (GenericHelpers.TryGetAddonMaster<GatheringMasterpiece>("GatheringMasterpiece", out var collectable) && collectable.IsAddonReady)
                    {
                        // this is all nice and tidy in one little function. Well that is split across 3 other ones but reguardless the general gathering task will be completed via this.
                        if (Mission_Settings.item_collectableId != collectable.ItemID)
                        {
                            IceLogging.Debug($"Setting Mission CollectableId to: {collectable.ItemID}", "[Gather: Collectable Interacting]");
                            Mission_Settings.item_collectableId = collectable.ItemID;
                        }

                        CollectableGather(collectable);
                    }
                }
            }
            else
            {
                return true;
            }

            return false;
        }
        public static unsafe void CollectableGather(GatheringMasterpiece collectable)
        {
            var integrity = collectable.CurrentIntegrity;
            var collect_Current = collectable.CurrentCollectability;
            var collect_Max = collectable.MaxCollectability;
            var collect_highGrade = collectable.HighCollectability;
            var playerGp = PlayerHelper.GetGp();
            bool missingDur = integrity < collectable.TotalIntegrity;

            if (integrity > 1 && collect_Current < collect_highGrade)
            {
                // this is the rotation we should be aiming for in general...
                // this should cover all baselines
                if (!PlayerHelper.HasStatusId(3911))
                {
                    var currentCharge = GatheringUtil.CollectStandardCharges();

                    if (currentCharge != 0 && currentCharge >= Mission_Settings.Collectable_BuffCount)
                    {
                        ActionManager.Instance()->UseAction(ActionType.GeneralAction, 27);
                    }
                    else if (CanUseCollectableAction("Scrutiny"))
                    {
                        UseCollectableBuff("Scrutiny");
                    }
                    else
                    {
                        UseCollectableAction("Meticulous");
                    }
                }
                else
                {
                    // We currently have the buff. Time to math out which would be the best for what we need.
                    var collect_Missing = collect_Max - collect_Current;
                    var brazenPower = collectable.BrazenPowerMax;
                    var meticulousPower = collectable.MeticulousPower;


                    if (CanUseCollectableAction("Scrutiny"))
                    {
                        UseCollectableBuff("Scrutiny");
                    }
                    else if (collect_Missing <= meticulousPower)
                    {
                        UseCollectableAction("Meticulous");
                    }
                    else
                    {
                        UseCollectableAction("Brazen");
                    }
                }
            }
            else
            {
                // if we've gotten this far, that means we're in a state that we should just be collecting
                if (CanUseCollectableAction("BonusIntegrityChance", missingDur))
                {
                    if (EzThrottler.Throttle("Integrity bonus"))
                        UseCollectableAction("BonusIntegrityChance");
                }
                else if (CanUseCollectableAction("BonusIntegrity", missingDur))
                {
                    if (EzThrottler.Throttle("Integrity bonus"))
                        UseCollectableAction("BonusIntegrity");
                }
                else
                {
                    UseCollectableAction("Collect");
                }
            }
        }

        // Old Gathering system here

        public static bool? CheckCurrentLocation()
        {
            ThrottleMessage("- - - Check Gather Locations Task - - -", "[Check Gather Locations]");

            var zoneId = Player.Territory;
            var missionEntry = CosmicHelper.CurrentMissionInfo;
            var missionFlag = missionEntry.MapPosition;
            var gatherInfo = GatheringRouteLoader.GetRoute(zoneId.RowId, missionFlag);

            if (gatherInfo != null)
            {
                if (Mission_Settings.previousMap != missionFlag)
                {
                    // We're currently at a whole new area. So going to check the gathering nodes to see which one we're closest to
                    Mission_Settings.previousMap = missionFlag;
                    var closestNodeIndex = gatherInfo.Select((node, index) => new { Node = node, Index = index })
                                                     .Where(x => Svc.Objects.Any(obj => obj.ObjectKind == ObjectKind.GatheringPoint && obj.IsTargetable && obj.BaseId == x.Node.NodeId))
                                                     .OrderBy(x =>
                                                     {
                                                         var gameObject = Svc.Objects.First(obj => obj.BaseId == x.Node.NodeId);
                                                         return Player.DistanceTo(gameObject.Position);
                                                     })
                                                     .Select(x => x.Index)
                                                     .FirstOrDefault(0);

                    Mission_Settings.nodeCounter = closestNodeIndex;
                }
                else
                {
                    // we're currently in a map location that has been previously recorded, so we're going to check to see if we're within range of any first
                    var closestDistance = gatherInfo.Where(x => Player.DistanceTo(x.Position) < 5).FirstOrDefault();
                    if (closestDistance == null)
                    {
                        // We're currently to far from any node. going to rely on the index to tell us where we should be
                        if (Mission_Settings.nodeCounter >= gatherInfo.Count)
                        {
                            // resetting it back to 0 because we're outside the normal index array
                            Mission_Settings.nodeCounter = 0;
                        }
                        return true;

                    }
                    else
                    {
                        // We're currently close to a node, time to check and see if it's a viable node, or if we need to pathfind to the next
                        var nodeId = closestDistance.NodeId;
                        var closestNode = Svc.Objects.Where(x => x.BaseId == nodeId && x.IsTargetable).FirstOrDefault();

                        if (closestNode != null)
                        {
                            // Node is targetable, set the counter to this node's index
                            var currentNodeIndex = gatherInfo.FindIndex(x => x.NodeId == nodeId);
                            if (currentNodeIndex >= 0)
                            {
                                Mission_Settings.nodeCounter = currentNodeIndex;
                            }
                            return true;
                        }
                        else
                        {
                            // Node is not targetable, increment to next node
                            Mission_Settings.nodeCounter++;

                            // Check if we're out of bounds and wrap back to 0
                            if (Mission_Settings.nodeCounter >= gatherInfo.Count)
                            {
                                Mission_Settings.nodeCounter = 0;
                            }
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        public static bool? PathandCheckNode()
        {
            var zoneId = Player.Territory;
            var missionEntry = CosmicHelper.CurrentMissionInfo;
            var missionFlag = missionEntry.MapPosition;
            var gatherInfo = GatheringRouteLoader.GetRoute(zoneId.RowId, missionFlag);

            var location = gatherInfo[Mission_Settings.nodeCounter];
            if (!Task_NavmeshMove.Task_NavTo(location.LandZone, distance: 1).Value)
            {
                UseCordial();
                ThrottleMessage("Currently in the process of moving, so going to wait", "Task_Gather: NavmeshMovement");
                return false;
            }
            else
            {
                if (CosmicHandler.IsMissionTimedOut())
                {
                    IceLogging.Info($"We've managed to time out the mission. Going to attempt to turnin, and abandon if not", "[Gathering: Open Gathering Menu]");
                    SchedulerMain.State = IceState.AbandonMission;
                    P.TaskManager.Tasks.Clear();
                    return true;
                }
                else if (Svc.Condition[ConditionFlag.Gathering] && GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var gather) && gather.IsAddonReady || GenericHelpers.TryGetAddonMaster<GatheringMasterpiece>("GatheringMasterpiece", out var collectable) && collectable.IsAddonReady)
                {
                    Mission_Settings.CollectableStep = 0;

                    IceLogging.Info($"Gathering window is now visible, continuing onto GatheringInteraction Task", "[Gathering: OpenGatheringMenu]");
                    P.TaskManager.Insert(() => GatherInteractV2(), "Gathering at the node", Utils.TaskConfig);
                    Mission_Settings.nodeTotal += 1;
                    return true;
                }
                else
                {
                    Utils.TryGetObjectByDataId(location.NodeId, out var node);
                    if (node != null && !Player.IsJumping)
                    {
                        if (node.IsTargetable)
                        {
                            if (EzThrottler.Throttle("Target + Interacting w/ node"))
                            {
                                Utils.TargetgameObject(node);
                                Utils.InteractWithObject(node);
                            }
                        }
                        else
                        {
                            // Node doesn't exist/isn't targetable. 
                            IceLogging.Info($"The current node doesn't exist, continuing onto the next", "[Gathering: OpenGatheringMenu]");
                            Mission_Settings.nodeTotal += 1;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        public static unsafe bool UseGatherAction(int profileId, int gatherChance, int? boonChance, int currentDur, int maxDur, int availableGp)
        {
            C.GatherProfiles.TryGetValue(profileId, out var gatherProfile);

            bool missingDur = currentDur != maxDur;

            if (C.XPLeveling_Mode)
            {
                if (EzThrottler.Throttle("Level grind message", 1000))
                    IceLogging.Debug("Leveling mode enabled, setting it to gatherProfile");
                gatherProfile = LevelProfile;
            }
            else if (gatherProfile == null)
            {
                gatherProfile = C.GatherProfiles[0];
                if (EzThrottler.Throttle("Null Profile Selected"))
                {
                    IceLogging.Error("Hey! We've somehow stumbled into a null profile being selected. Please make sure:\n" +
                                     "1: The mission you have selected has a gathering profile selected\n" +
                                     "2: If it does have one, try to click on it again\n" +
                                     "3: If that still doesn't work, let me know you're getting this error message.\n" +
                                     $"Expected profileId: {profileId} | Defaulted to the default profile");
                }
            }

            if (gatherChance != 100)
            {
                if (EzThrottler.Throttle("Helper Log"))
                {
                    IceLogging.Debug($"Gathering Chance: {gatherChance}", debugOnly: true);
                }
                uint MasteryBuff = GatheringUtil.GathActionDict["FieldMasteryI"].StatusId;

                string? SelectBestFieldMastery(int currentChance, int availableGp)
                {
                    int playerLevel = Player.Level;

                    bool MasteryIII = gatherProfile.GatherBuffs.Buffs["FieldMasteryIII"].Enabled
                                   && (gatherProfile.GatherBuffs.Buffs["FieldMasteryIII"].MinGp <= PlayerHelper.GetGp())
                                   && (PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["FieldMasteryIII"].RequiredGp)
                                   && (playerLevel >= GatheringUtil.GathActionDict["FieldMasteryIII"].RequiredLv)
                                   && (gatherProfile.GatherBuffs.Buffs["FieldMasteryIII"].MaxUse == -1
                                       || Mission_Settings.SkillUseAmount["FieldMasteryIII"] < gatherProfile.GatherBuffs.Buffs["FieldMasteryIII"].MaxUse);
                    bool MasteryII = gatherProfile.GatherBuffs.Buffs["FieldMasteryII"].Enabled
                                  && (gatherProfile.GatherBuffs.Buffs["FieldMasteryII"].MinGp <= PlayerHelper.GetGp())
                                  && (PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["FieldMasteryII"].RequiredGp)
                                  && (playerLevel >= GatheringUtil.GathActionDict["FieldMasteryII"].RequiredLv)
                                  && (gatherProfile.GatherBuffs.Buffs["FieldMasteryII"].MaxUse == -1
                                      || Mission_Settings.SkillUseAmount["FieldMasteryII"] < gatherProfile.GatherBuffs.Buffs["FieldMasteryII"].MaxUse);
                    bool MasteryI = gatherProfile.GatherBuffs.Buffs["FieldMasteryI"].Enabled
                                 && (gatherProfile.GatherBuffs.Buffs["FieldMasteryI"].MinGp <= PlayerHelper.GetGp())
                                 && (PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["FieldMasteryI"].RequiredGp)
                                 && (playerLevel >= GatheringUtil.GathActionDict["FieldMasteryI"].RequiredLv)
                                 && (gatherProfile.GatherBuffs.Buffs["FieldMasteryI"].MaxUse == -1
                                     || Mission_Settings.SkillUseAmount["FieldMasteryI"] < gatherProfile.GatherBuffs.Buffs["FieldMasteryI"].MaxUse);

                    // Already at 100%? No skill needed, so continuing on
                    if (currentChance >= 100)
                        return null;

                    int neededBonus = 100 - currentChance;

                    // Find the cheapest skill that gets us to 100%
                    if (neededBonus <= 5 && availableGp >= 50 && MasteryI)
                        return "FieldMasteryI";

                    if (neededBonus <= 15 && availableGp >= 100 && MasteryII)
                        return "FieldMasteryII";

                    if (neededBonus <= 50 && availableGp >= 250 && MasteryIII)
                        return "FieldMasteryIII";

                    // If we can't reach 100%, use the best skill we can afford 
                    if (availableGp >= 250  && MasteryIII)
                        return "FieldMasteryIII";

                    if (availableGp >= 100 && MasteryII)
                        return "FieldMasteryII";

                    if (availableGp >= 50  && MasteryI)
                        return "FieldMasteryI";

                    return null; // Can't afford any skill
                }

                if (!PlayerHelper.HasStatusId(MasteryBuff) && (SelectBestFieldMastery(gatherChance, availableGp) != null))
                {
                    string? ActionName = SelectBestFieldMastery(gatherChance, availableGp);
                    if (ActionName != null)
                    {
                        if (EzThrottler.Throttle($"Using Gathering Action: {ActionName}", 100))
                        {
                            uint jobId = (uint)Player.Job;

                            IceLogging.Debug($"Using the following action: {ActionName} to gain some collectability from the node", debugOnly: true);
                            var actionId = GatheringUtil.GathActionDict[ActionName].ClassAction[jobId];
                            ActionManager.Instance()->UseAction(ActionType.Action, actionId);
                            Mission_Settings.SkillUseAmount[ActionName] += 1;
                        }
                        return true;
                    }
                }

                int playerLevel = Player.Level;
                uint TempMasteryBuffId = GatheringUtil.GathActionDict["FieldMasteryTemp"].StatusId;
                bool TempMasteryBuff = gatherProfile.GatherBuffs.Buffs["FieldMasteryTemp"].Enabled
                                    && (gatherProfile.GatherBuffs.Buffs["FieldMasteryTemp"].MinGp <= PlayerHelper.GetGp())
                                    && (PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["FieldMasteryTemp"].RequiredGp)
                                    && (playerLevel >= GatheringUtil.GathActionDict["FieldMasteryTemp"].RequiredLv)
                                    && (gatherProfile.GatherBuffs.Buffs["FieldMasteryTemp"].MaxUse == -1);

                if (!PlayerHelper.HasStatusId(TempMasteryBuffId) && TempMasteryBuff)
                {
                    if (EzThrottler.Throttle($"Using Gathering Action: {"FieldMasteryTemp"}", 100))
                    {
                        uint jobId = (uint)Player.Job;

                        IceLogging.Debug($"Using the following action: {"FieldMasteryTemp"} to gain some collectability from the node", debugOnly: true);
                        var actionId = GatheringUtil.GathActionDict["FieldMasteryTemp"].ClassAction[jobId];
                        ActionManager.Instance()->UseAction(ActionType.Action, actionId);
                        Mission_Settings.SkillUseAmount["FieldMasteryTemp"] += 1;
                    }
                    return true;
                }
            }

            if (PlayerHelper.HasStatusId(4437) && (currentDur == 1 || currentDur == maxDur - 4) && PlayerHelper.GetGp() != PlayerHelper.MaxGp())
            {
                ActionManager.Instance()->UseAction(ActionType.GeneralAction, 27);
                return true;
            }

            // general logic for checking for the rest of the buffs now
            foreach (var buff in Mission_Settings.SkillUseAmount)
            {
                string action = buff.Key;
                if (CanUseGatheringAction(action, profileId, missingDur, maxDur, currentDur, boonChance))
                {
                    var actionInfo = GatheringUtil.GathActionDict[action];
                    if (EzThrottler.Throttle($"Using Gathering Action: {action}"))
                    {
                        uint jobId = (uint)Player.Job;

                        IceLogging.Debug($"Using the following action: {action} on the node", debugOnly: true);
                        var actionId = GatheringUtil.GathActionDict[action].ClassAction[jobId];
                        ActionManager.Instance()->UseAction(ActionType.Action, actionId);
                        Mission_Settings.SkillUseAmount[action] += 1;
                    }

                    return true;
                }
            }

            return false;
        }
        public static bool CanUseGatheringAction(string actionName, int profileId, bool missingDur, int maxDur, int currentDur, int? boonChance = null)
        {
            var actionInfo = GatheringUtil.GathActionDict[actionName];
            bool hasStatus = PlayerHelper.HasStatusId(actionInfo.StatusId);
            bool hasGp = PlayerHelper.GetGp() >= actionInfo.RequiredGp;
            var used = Mission_Settings.SkillUseAmount[actionName];
            bool properLvl = Player.Level >= actionInfo.RequiredLv;

            if (actionName == "BonusIntegrityChance")
            {
                return hasStatus && currentDur == 1;
            }

            var gatherBuff = C.GatherProfiles[profileId].GatherBuffs.Buffs[actionName];

            return actionName switch
            {
                "BoonIncrease1" => gatherBuff.Enabled
                                && boonChance < 100
                                && !hasStatus
                                && !missingDur
                                && hasGp
                                && PlayerHelper.GetGp() >= gatherBuff.MinGp
                                && (gatherBuff.MaxUse == -1 || gatherBuff.MaxUse > used)
                                && properLvl,
                "BoonIncrease2" => gatherBuff.Enabled
                                && boonChance < 100
                                && !hasStatus
                                && !missingDur
                                && hasGp
                                && PlayerHelper.GetGp() >= gatherBuff.MinGp
                                && (gatherBuff.MaxUse == -1 || gatherBuff.MaxUse > used)
                                && properLvl,
                "Tidings" => gatherBuff.Enabled
                          && !hasStatus
                          && !missingDur
                          && hasGp
                          && PlayerHelper.GetGp() >= gatherBuff.MinGp
                          && (gatherBuff.MaxUse == -1 || gatherBuff.MaxUse > used)
                          && properLvl,
                "YieldI" => gatherBuff.Enabled
                          && !hasStatus
                          && !missingDur
                          && hasGp
                          && PlayerHelper.GetGp() >= gatherBuff.MinGp
                          && (gatherBuff.MaxUse == -1 || gatherBuff.MaxUse > used)
                          && (maxDur >= gatherBuff.MinUsableDurability)
                          && properLvl,
                "YieldII" => gatherBuff.Enabled
                         && !hasStatus
                         && !missingDur
                         && hasGp
                         && PlayerHelper.GetGp() >= gatherBuff.MinGp
                         && (maxDur >= gatherBuff.MinUsableDurability)
                         && (gatherBuff.MaxUse == -1 || gatherBuff.MaxUse > used)
                         && properLvl,
                "BonusIntegrity" => gatherBuff.Enabled
                                    && currentDur == 1
                                    && hasGp
                                    && PlayerHelper.GetGp() >= gatherBuff.MinGp
                                    && (gatherBuff.MaxUse == -1 || gatherBuff.MaxUse > used)
                                    && properLvl,
                "BountifulYieldII" => gatherBuff.Enabled
                                   && !hasStatus
                                   && hasGp
                                   && PlayerHelper.GetGp() >= gatherBuff.MinGp
                                   && (gatherBuff.MaxUse == -1 || gatherBuff.MaxUse > used)
                                   && properLvl,
                _ => false,
            };
        }
        private static bool CanUseCollectableAction(string action, bool missingDur = false)
        {
            var actionInfo = GatheringUtil.GathCollectableBuffs[action];
            bool hasStatus = PlayerHelper.HasStatusId(actionInfo.StatusId);
            bool hasGp = PlayerHelper.GetGp() >= actionInfo.RequiredGp;

            return action switch
            {
                "Scrutiny" => !hasStatus
                           && hasGp,
                "Focus" => !hasStatus
                        && hasGp,
                "Priming" => !hasStatus
                          && hasGp,
                "CollectorsHigh" => !hasStatus
                                 && hasGp,
                "BonusIntegrityChance" => hasStatus
                                       && missingDur,
                "BonusIntegrity" => hasGp
                                 && missingDur
                                 && PlayerHelper.GetGp() >= 300,
                _ => false,
            };
        }
        public static unsafe void UseCollectableBuff(string action)
        {
            var collectorBuffs = GatheringUtil.GathCollectableBuffs;
            var jobId = (uint)Player.Job;

            var actionId = collectorBuffs[action].ClassAction[jobId];
            if (EzThrottler.Throttle("Using Action Buff", 100))
            {
                ActionManager.Instance()->UseAction(ActionType.Action, actionId);
            }
        }
        public static unsafe void UseCollectableAction(string action)
        {
            var collectorAction = GatheringUtil.GathCollectableActions;
            var jobId = (uint)Player.Job;

            var actionId = collectorAction[action].ClassAction[jobId];
            ActionManager.Instance()->UseAction(ActionType.Action, actionId);
        }
        public static bool? CheckReduceMission()
        {
            IceLogging.Info($"Current itemId: {Mission_Settings.item_collectableId}", "[Gather: Check Reduce Mission]");
            bool hasCollectable = PlayerHelper.GetItemCount(Mission_Settings.item_collectableId, out var count) && count > 0;
            bool isReducableMission = CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.ReducedItems);
            if (hasCollectable && isReducableMission)
            {
                P.TaskManager.InsertMulti(
                                            new(() => CheckReduceItems(), "Starting the desynth process"),
                                            new(() => WaitForDesynthCompletion(), "Waiting for desyntht to complete")
                                         );
            }

            return true;
        }
        public static unsafe bool? CheckReduceItems()
        {
            if (Svc.Condition[ConditionFlag.Occupied39])
            {
                IceLogging.Info("We're currently desynthing an item, continuing on to wait to stop", "[Task Gather: Reducing Item Check]");
                return true;
            }
            else
            {
                if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var gather) && gather.IsAddonReady)
                {
                    // we shouldn't have this open while we're desynthing. . . closing it out.
                    if (EzThrottler.Throttle("Closing gather window"))
                    {
                        // 
                    }
                }
                if (Player.Mounted)
                {
                    Utils.Dismount();
                }

                // We have items to desynth! Time to check and see which window we need to interact with... or just wait. 
                if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("PurifyItemSelector", out var desynthWindow) && desynthWindow->IsReady)
                {
                    if (EzThrottler.Throttle("Desynthing the item"))
                    {
                        if (!Player.IsBusy)
                            ECommons.Automation.Callback.Fire(desynthWindow, true, 12, 0);
                    }
                }
                else if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var missionInfo) && missionInfo.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Opening the desynth window"))
                    {
                        missionInfo.StellerReduction();
                    }
                }
                else if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var moonHud) && moonHud.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Opening the moon hud"))
                    {
                        moonHud.Mission();
                    }
                }
            }

            return false;
        }
        public static bool? WaitForDesynthCompletion()
        {
            if (!Svc.Condition[ConditionFlag.Occupied39])
            {
                PlayerHelper.GetItemCount(Mission_Settings.item_collectableId, out var count);
                if (count != 0)
                {
                    // Still have some more items to desynth, going to reset the current task count and re-check
                    P.TaskManager.Tasks.Clear();
                }

                return true;
            }

            return false;
        }
        public static unsafe void UseCordial()
        {
            if (EzThrottler.Throttle("Cordial usage check while moving"))
            {
                if (!Player.IsBusy)
                {
                    IceLogging.Debug("Cordial Checkers");
                    if (C.AutoCordial)
                    {
                        IceLogging.Debug($"Min GP: {C.CordialMinGp} <= {PlayerHelper.GetGp()}");

                        if (PlayerHelper.GetGp() <= C.CordialMinGp)
                        {
                            Dictionary<uint, int> cordials = new()
                            {
                                { 12669, 400}, // Hi
                                { 1006141, 350}, // HQ Regular
                                { 6141, 300}, // NQ Regular
                                { 1016911, 200}, // HQ Watered
                                { 16911, 150} // HQ Watered
                            };

                            foreach (var cordial in C.inverseCordialPrio ? cordials.Reverse() : cordials)
                            {
                                IceLogging.Debug($"Checking Cordial: {cordial.Key}");
                                bool hq = cordial.Key >= 1_000_000;
                                if (PlayerHelper.GetItemCount(cordial.Key, out var amount, hq, !hq) && amount > 0)
                                {
                                    if (ActionManager.Instance()->GetActionStatus(ActionType.Item, cordial.Key) == 0)
                                    {
                                        if (!C.PreventOvercap || (C.PreventOvercap && !WillOvercap(cordial.Value)))
                                        {
                                            if (EzThrottler.Throttle("Using the cordial"))
                                            {
                                                ActionManager.Instance()->UseAction(ActionType.Item, cordial.Key, extraParam: 65535);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    IceLogging.Info("Cordial Check Complete");
                }
            }
        }
        private static bool WillOvercap(int recoveryGP)
        {
            return ((PlayerHelper.GetGp() + recoveryGP) > PlayerHelper.MaxGp());
        }
        public static unsafe bool? UseFood()
        {
            var ItemId = C.GatheringFood;
            if (C.UseGatheringFood && ItemId != 0)
            {
                PlayerHelper.GetItemCount(ItemId, out var HqCount, includeNq: false);
                PlayerHelper.GetItemCount(ItemId, out var NqCount, includeHq: false);

                if (HqCount > 0 || NqCount > 0)
                {
                    // We've gotten this far, which means we have a gathering item to use...
                    if (!PlayerHelper.HasFoodRunning())
                    {
                        // We need to apply the food, since we have some, we're going to use some here
                        if (EzThrottler.Throttle("Using Food Item", 3000))
                        {
                            if (HqCount > 0)
                                ItemId += 1_000_000;

                            ActionManager.Instance()->UseAction(ActionType.Item, ItemId, extraParam: 65535);
                            IceLogging.Debug($"Attempting to use food: {ItemId}");
                        }
                        return false;
                    }
                    else
                    {
                        IceLogging.Info("We have food running, and it's the proper one! Continuing");
                        return true;
                    }
                }
                else
                {
                    IceLogging.Info("We are out of the current food, continuing on w/o buff");
                    return true;
                }
            }
            else
            {
                IceLogging.Info("We either don't have use food enabled, or have no food selected. Continuing on\n" +
                               $"Use Food Enabled: {C.UseGatheringFood}\n" +
                               $"ItemId of food: {ItemId}");
                return true;
            }
        }
        private static void ThrottleMessage(string s, string handle)
        {
            if (EzThrottler.Throttle($"Throttling the following message: {s}", 1000))
            {
                IceLogging.Debug(s, handle);
            }
        }
        private static GatherProfile LevelProfile = new()
        {
            Name = "Leveing Profile",
            Id = 99999,
            MinimumGp = 0,
            DualClassCraftAmount = 0,
            GatherBuffs = new()
            {
                BountifulMinItem = 1,
                Buffs = new()
                {
                    ["BoonIncrease2"] = new() { Enabled = true },
                    ["BoonIncrease1"] = new() { Enabled = true },
                    ["Tidings"] = new() { Enabled = true },
                    ["YieldII"] = new() { Enabled = true },
                    ["YieldI"] = new() { Enabled = true },
                    ["BountifulYieldII"] = new() { Enabled = false },
                    ["BonusIntegrity"] = new() { Enabled = true },
                    ["BonusIntegrityChance"] = new() { Enabled = true },
                    ["FieldMasteryIII"] = new() { Enabled = true },
                    ["FieldMasteryII"] = new() { Enabled = true },
                    ["FieldMasteryI"] = new() { Enabled = true },
                    ["FieldMasteryTemp"] = new() { Enabled = true },
                }
            }
        };
    }
}
