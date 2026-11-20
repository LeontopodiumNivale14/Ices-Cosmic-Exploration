using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ICE.Ui.Debug_Tabs.Debug_Ui;
using ICE.Utilities.Cosmic_Helper;
using ICE.Utilities.GatheringHelper;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ICE.Utilities.GatheringHelper.GatheringUtil;

namespace ICE.Scheduler.Tasks
{
    internal static class Task_Fishing
    {
        // Something to note. 42, 43, 85 are the conditions that you get while you're fishing
        // 43 and 85 are active while you're fishing
        // 42 is active when reeling in a fish
        // Something to consider, start fishing... (that's condition 42 when you start)
        // Whenever all the conditions are cleared, check the inventory for the frame, see if you have enough/meet the score

        private static FishingDebug _fishingDebug = null;

        public static void Enqueue()
        {
            // think the process should be:
            // check score
            // if score not complete, check if can craft
            // if craft not required, fish
            // wait for fishing to be done

            P.TaskManager.EnqueueMulti
                (
                    new(() => Task_CheckScore.Fish(), "Checking Score: Fishing"),
                    new(() => Task_Gather.UseFood(), "Checking for food usage"),
                    new(() => FishCheckV2(), "Checking Fishing State")
                );
        }

        private static int StartedFishing = 0;
        private static int SafetyThrottle = 0;

        private static unsafe bool? FishCheckV2()
        {
            string handle = "Fishing Task: State Check";
            if (Svc.Condition[ConditionFlag.Fishing])
            {
                IceLogging.Info("We're currently in the middle of fishing, so we're going to wait for us to complete");
                StartedFishing = 0;
                P.TaskManager.Enqueue(() => FinishFishing(), "Waiting for fishing to complete");
                SafetyThrottle = 0;
                return true;
            }
            else
            {
                if (EzThrottler.Throttle("Delay Throttle", 200))
                {
                    SafetyThrottle += 1;
                }
                if (SafetyThrottle < 2)
                    return false;

                if (EzThrottler.Throttle("Checking fishing state"))
                    IceLogging.Verbose("We're not currently fishing. Checking to see what we should do", handle);

                if (Player.Mounted || Player.IsJumping)
                {
                    if (EzThrottler.Throttle("Log message: Jump/Dismount", 1000))
                        IceLogging.Verbose("We're in the middle of dismounting/jumping, waiting");

                    Utils.Dismount();
                    return false;
                }

                if (Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                {
                    if (EzThrottler.Throttle("Gathering Action Execution", 2000))
                        IceLogging.Info("We're currently executing some gathering action, so waiting");

                    return false;
                }

                var hasBait = false;
                uint firstBait = 0;
                foreach (var bait in GatheringUtil.MoonBaits)
                {
                    foreach (var baitId in bait.Value)
                    {
                        if (PlayerHelper.GetItemCount(baitId, out var baitCount) && baitCount > 0)
                        {
                            hasBait = true;
                            firstBait = baitId;
                            break;
                        }
                    }
                    if (hasBait) break;
                }

                if (!hasBait)
                {
                    IceLogging.Info("We are reporting to be out of bait, proceeding to abandon/turnin mission");
                    SchedulerMain.State = IceState.AbandonMission;
                    SafetyThrottle = 0;
                    return true;
                }

                if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Collectables) && !PlayerHelper.HasStatusId(805))
                {
                    uint fishCollectable = 4101;

                    if (EzThrottler.Throttle("Collectable message"))
                    {
                        IceLogging.Verbose("We might be missing collectors glove? Or it might still be being applied by autohook. Please give it time", handle);
                    }
                    if (PlayerHelper.CanUseAction(fishCollectable))
                    {
                        if (EzThrottler.Throttle("Attempting to turn on collectability"))
                            ActionManager.Instance()->UseAction(ActionType.Action, fishCollectable);
                    }
                    return false;
                }
                if (_fishingDebug == null)
                {
                    _fishingDebug = new FishingDebug();
                }

                if (_fishingDebug.IsFishable() && !Svc.Condition[ConditionFlag.Gathering])
                {
                    if (EzThrottler.Throttle("Started Fishing Throttle", 500))
                    {
                        IceLogging.Verbose($"+1 to waiting for fishing to actually start [Might be waiting on gp/buffs]... {StartedFishing}", handle);
                        Svc.Commands.ProcessCommand("/ahstart");
                    }
                }
                else
                {
                    IceLogging.Verbose("We apperently aren't facing toward the fishing hole... or not close enough to one that we can actually start. So going to attempt to fix it", handle);
                    if (_fishingDebug.FindFishableLocation(out var fishablePos, searchSteps: 64))
                    {
                        IceLogging.Info("We're not in a fishable angle, so going to face one", handle);
                        P.TaskManager.Enqueue(() => FacePosition(fishablePos.Value));
                        SafetyThrottle = 0;
                        return true;
                    }
                    else
                    {
                        IceLogging.Debug("Our current fishing position isn't viable. So going to move to the next fishing spot");
                        var mission = CosmicHelper.CurrentMissionInfo;
                        var flag = mission.MapPosition;
                        var territoryId = mission.TerritoryId;

                        var nextFishingSpot = GetNextFishingSpot(territoryId, flag, Player.Position);
                        if (nextFishingSpot != null)
                        {
                            IceLogging.Info($"We found another fishing spot to move to! {nextFishingSpot.FishingSpot} | moving to it");
                            P.TaskManager.Tasks.Clear();
                            P.TaskManager.Enqueue(() => InitiateMoving(nextFishingSpot.FishingSpot), "Vnav moving to fishing");
                            SafetyThrottle = 0;
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private static int BaitCounter = 0;

        private static unsafe bool? FishingCheck()
        {
            if (_fishingDebug == null)
            {
                _fishingDebug = new FishingDebug();
            }

            if (Player.Mounted)
            {
                Utils.Dismount();
                return false;
            }

            string handle = "[Standard Fishing: Fishing Check]";
            if (EzThrottler.Throttle("Throttling intro message", 1000))
            {
                IceLogging.Debug("Checking to see where we need to be here", handle);
            }
            bool hasBait = false;

            if (CosmicHelper.CurrentBait() == 0)
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

                    IceLogging.Info("If we've gotten here, that means we're out of bait. Proceeding to turnin/abandon the mission");
                    SchedulerMain.State = IceState.AbandonMission;
                    P.TaskManager.Tasks.Clear();
                    return true;
                }
                return false;
            }

            // little check here for seeing if we have any baits
            foreach (var bait in GatheringUtil.MoonBaits)
            {
                foreach (var baitId in bait.Value)
                {
                    if (PlayerHelper.GetItemCount(baitId, out var count) && count > 0)
                    {
                        if (EzThrottler.Throttle("Throttling bait message", 1000))
                            IceLogging.Debug("We have the bait! Continuing onwards");
                        hasBait = true;
                        break;
                    }
                }
            }

            if (!hasBait)
            {
                IceLogging.Info("If we've gotten here, that means we're out of bait. Proceeding to turnin/abandon the mission");
                SchedulerMain.State = IceState.AbandonMission;
                P.TaskManager.Tasks.Clear();
                return true;
            }
            else if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Collectables) && !PlayerHelper.HasStatusId(805))
            {
                if (EzThrottler.Throttle("Log Throttle for fishing"))
                    IceLogging.Debug("We need to apply collector's glove", "Task_Start Fishing");

                if (!Player.IsBusy)
                {
                    if (EzThrottler.Throttle("Attempting to turn on collectability"))
                        ActionManager.Instance()->UseAction(ActionType.Action, 4101);
                }
                return false;
            }
            else if (!Svc.Condition[ConditionFlag.Gathering])
            {
                if (!_fishingDebug.IsFishable())
                {
                    if (_fishingDebug.FindFishableLocation(out var fishablePos, searchSteps: 64))
                    {
                        IceLogging.Info("We're not in a fishable angle, so going to face one", handle);
                        P.TaskManager.Tasks.Clear();
                        P.TaskManager.Enqueue(() => FacePosition(fishablePos.Value));
                        return true;
                    }
                    else
                    {
                        IceLogging.Debug("Our current fishing position isn't viable. So going to move to the next fishing spot");
                        var mission = CosmicHelper.CurrentMissionInfo;
                        var flag = mission.MapPosition;
                        var territoryId = mission.TerritoryId;

                        var nextFishingSpot = GetNextFishingSpot(territoryId, flag, Player.Position);
                        if (nextFishingSpot != null)
                        {
                            IceLogging.Info($"We found another fishing spot to move to! {nextFishingSpot.FishingSpot} | moving to it");
                            P.TaskManager.Tasks.Clear();
                            P.TaskManager.Enqueue(() => InitiateMoving(nextFishingSpot.FishingSpot), "Vnav moving to fishing");
                            return true;
                        }
                    }
                }
                else if (EzThrottler.Throttle("Starting to fish", 1000))
                {
                    IceLogging.Debug("Telling it to start fishing", handle);
                    // ActionManager.Instance()->UseAction(ActionType.Action, 289);
                    Svc.Commands.ProcessCommand("/ahstart");
                }
                else if (EzThrottler.Throttle("Adding counter for bait not equipped"))
                {
                    BaitCounter++;
                    IceLogging.Debug($"Adding 1 to the counter. Counter is at: {BaitCounter}");
                    if (BaitCounter >= 2)
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
                }
                return false;
            }
            else
            {
                // Means we are fishing, all we need to do is enable autohook then wait for us to get the amount of fish we need
                P.AutoHook.Ah_State(true);
                IceLogging.Info("We're starting to fish. So kicking it over to checking the fish items", handle);
                P.TaskManager.Insert(() => FinishFishing(), "Waiting till we actually start fishing", Utils.TaskConfig);
                BaitCounter = 0;
                return true;
            }
        }

        private static int collectableCounter = 0;
        private static unsafe bool? FinishFishing()
        {
            if (!Svc.Condition[ConditionFlag.Fishing])
            {
                IceLogging.Info("We're done fishing, time to go back to the score check", "[Fishing: Finished]");
                return true;
            }
            else
            {
                if (GenericHelpers.TryGetAddonMaster<SelectYesno>("SelectYesno", out var yesNo) && yesNo.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Adding +1 to counter", 250))
                    {
                        collectableCounter += 1;
                    }
                    if (collectableCounter >= 2)
                    {
                        if (EzThrottler.Throttle("Selecting yes to collectables"))
                        {
                            yesNo.Yes();
                        }
                        return false;
                    }
                    return false;
                }
            }
            if (collectableCounter != 0)
                collectableCounter = 0;

            return false;
        }
        public static unsafe bool? FacePosition(Vector3 pos, float tolerance = 0.1f)
        {
            float currentRotation = Player.Rotation;

            // If rotation is still changing, wait for it to stabilize
            if (Math.Abs(Player.Rotation - currentRotation) > tolerance)
            {
                return false;
            }

            Vector3 direction = pos - Player.Position;
            float targetRotation = (float)Math.Atan2(direction.X, direction.Z);

            float angleDifference = GetShortestAngleDifference(Player.Rotation, targetRotation);

            if (Math.Abs(angleDifference) < tolerance)
            {
                return true;
            }

            if (EzThrottler.Throttle("Facing toward the fishing hole"))
            {
                var fwk = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();

                var autoRotateConfig = fwk->SystemConfig.GetConfigOption((uint)ConfigOption.AutoFaceTargetOnAction);
                var autoRotateOriginal = autoRotateConfig->Value.UInt;

                autoRotateConfig->Value.UInt = 1;

                IceLogging.Debug($"Telling the game to face you to: {pos}");
                Vector3 temp = pos;
                ActionManager.Instance()->AutoFaceTargetPosition(&temp);

                autoRotateConfig->Value.UInt = autoRotateOriginal;
            }

            return false;
        }
        public static float GetShortestAngleDifference(float currentAngle, float targetAngle)
        {
            float difference = targetAngle - currentAngle;

            // Normalize to [-π, π] for shortest path
            while (difference > Math.PI) difference -= (float)(2 * Math.PI);
            while (difference < -Math.PI) difference += (float)(2 * Math.PI);

            return difference;
        }
        public static FisherSpotInfo? GetNextFishingSpot(uint zone, Vector2 flag, Vector3 playerPosition)
        {
            // Check if the zone and flag exist
            if (!MoonFishingLocations.TryGetValue(zone, out var zoneData) ||
                !zoneData.TryGetValue(flag, out var spots) ||
                spots.Count == 0)
            {
                return null;
            }

            // Find the index of the spot close to the player (within distance of 2)
            int currentIndex = -1;
            for (int i = 0; i < spots.Count; i++)
            {
                float distance = Vector3.Distance(playerPosition, spots[i].FacePosition);
                if (distance < 2f)
                {
                    currentIndex = i;
                    break;
                }
            }

            // If a close spot was found, return the next one (cycling back to 0 if at the end)
            if (currentIndex != -1)
            {
                int nextIndex = (currentIndex + 1) % spots.Count;
                return spots[nextIndex];
            }

            // If no close spot found, return the first entry
            return spots[0];
        }
        public static FisherSpotInfo? GetCurrentFishingSpot()
        {
            var zone = Player.Territory.RowId;
            var mission = CosmicHelper.CurrentMissionInfo;
            var flag = mission.MapPosition;

            // Check if the zone and flag exist
            if (!MoonFishingLocations.TryGetValue(zone, out var zoneData) || !zoneData.TryGetValue(flag, out var spots) || spots.Count == 0)
            {
                return null;
            }

            var closestSpot = spots.MinBy(x => Player.DistanceTo(x.FishingSpot));
            return closestSpot;
        }
        public static bool? InitiateMoving(Vector3 fishingPos)
        {
            if (!P.Navmesh.IsReady())
            {
                Utils.VnavBuildInfo();
                return false;
            }
            else if (P.Navmesh.IsRunning())
            {
                P.TaskManager.Enqueue(() => !P.Navmesh.IsRunning());
                return true;
            }
            else
            {
                if (EzThrottler.Throttle("Navmesh movement"))
                {
                    IceLogging.DestinationLogs.Log(fishingPos);
                    P.Navmesh.PathfindAndMoveTo(fishingPos, false);
                }
                return false;
            }
        }
    }
}
