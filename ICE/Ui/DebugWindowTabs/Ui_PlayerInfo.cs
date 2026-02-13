using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Generic;
using System.Reflection;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ICE.Ui.MainUi.ModeSelect.modeSelect_TableInfo;

namespace ICE.Ui.DebugWindowTabs
{
    internal class Ui_PlayerInfo
    {
        private static uint best_LevelMission = 0;
        private static uint playerLevel = 90;

        public static unsafe void Draw()
        {
            ImGui.Text("Need to actually put the player info here. It got lost");
            ImGui.Spacing();
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"Player Position: X:{Player.Position.X:N2}, Y:{Player.Position.Y:N2}, Z:{Player.Position.Z:N2}");
            ImGui.SameLine();
            if (ImGui.Button("Copy Vector2"))
            {
                ImGui.SetClipboardText($"{Player.Position.X:N2}f, {Player.Position.Z:N2}f");
            }
            ImGui.SameLine();
            if (ImGui.Button("Copy Vector3"))
            {
                ImGui.SetClipboardText($"{Player.Position.X:N2}f, {Player.Position.Y:N2}f, {Player.Position.Z:N2}f");
            }
            ImGui.Text($"Job: {Player.Job}");
            ImGui.Text($"JobId: {(uint)Player.Job}");
            ImGui.Text($"Current Territory/ZoneId: {Player.Territory}");
            if (PlayerHelper.IsInCosmicZone())
            {
                var manager = (WKSManagerCustom*)WKSManager.Instance();
                var currentMission = manager->CurrentMissionId;

                ImGui.Text($"Current Mission: {currentMission}");
            }
            if (Svc.Targets.Target != null)
            {
                var currentTarget = Svc.Targets.Target;
                if (ImGui.Button($"Name: {currentTarget.Name}"))
                {
                    ImGui.SetClipboardText(currentTarget.Name.ToString());
                }
                if (ImGui.Button($"Id: {currentTarget.BaseId}"))
                {
                    ImGui.SetClipboardText(currentTarget.BaseId.ToString());
                }
                if (ImGui.Button($"Position: X: {currentTarget.Position.X:N2}, Y: {currentTarget.Position.Y:N2}, Z: {currentTarget.Position.Z:N2}"))
                {
                    ImGui.SetClipboardText($"{currentTarget.Position.X:N2}f, {currentTarget.Position.Y:N2}f, {currentTarget.Position.Z:N2}f");
                }
                ImGui.Text($"Distance: {Player.DistanceTo(currentTarget):N2}");
            }

            ImGui.Text($"Items on person: ");
            foreach (var item in ConsumableInfo.Food)
            {
                if (PlayerHelper.GetItemCount(item.Id, out var count) && count > 0)
                    ImGui.Text($"{item.Name} | {item.Id}");
            }
            if (ImGui.Button("Use Gathering Food"))
            {
                P.TaskManager.Enqueue(() => Task_Gather.UseFood());
            }
            if (ImGui.Button("Set all leveling missions"))
            {
                foreach (var mission in C.MissionConfig)
                {
                    if (!CosmicHelper.QuickLevelList.Contains(mission.Key))
                        mission.Value.Enabled = false;
                    else
                        mission.Value.Enabled = true;
                }
                C.SaveDebounced();
            }

            ImGui.SliderUInt("Player Level", ref playerLevel, 10, 100);
            if (ImGui.Button("Update best mission"))
            {
                best_LevelMission = LevelTest();
            }
            ImGui.Text($"Best Mission for leveling: [{best_LevelMission}]");


            ClassInfo();

            DroidCheck();

            if (ImGui.CollapsingHeader("Test Picto"))
            {
                PictoManager.DrawPicto();
            }

            ImGui.Text($"Drone Ready: {DroneReady()}");

            if (ImGui.Button("Use Drone"))
            {
                UseDrone();
            }
            if (ImGui.Button("Test Pathing to position"))
            {
                P.TaskManager.Enqueue(() => MovetoFlag());
            }
            ImGui.SameLine();
            if (ImGui.Button($"Set position: {customDestination:N2}"))
            {
                customDestination = Player.Position;
            }
        }

        private static unsafe void ClassInfo()
        {
            ImGui.Text("Manipulation Check");
            Dictionary<uint, uint> ManipClassInfo = new()
            {
                [8] = 4574,
                [9] = 4575,
                [10] = 4576,
                [11] = 4577,
                [12] = 4578,
                [13] = 4579,
                [14] = 4580,
                [15] = 4581,
            };

            foreach (var job in ManipClassInfo)
            {
                var isUnlocked = ActionManager.Instance()->GetActionStatus(ActionType.Action, job.Value, checkRecastActive: false, checkCastingActive: false) is 574 or 586;
                ImGui.Text($"JobId: {job.Key} | Unlocked: {isUnlocked}");
                // 573 | Not unlocked??? 
                // 574 | Is unlocked
                // 586 | Is unlocked for current class/ready to use
            }

            ImGui.Separator();
            PlayerHelper.UpdateHasManip();

            ImGui.Text($"Custom Is Busy: {PlayerHelper.CustomIsBusy}");
            foreach (var job in PlayerHelper.ManipClassInfo)
            {
                ImGui.Text($"JobID: {job.Key} | HasUnlocked: {job.Value.HasUnlocked}");
            }
        }

        private static uint LevelTest()
        {
            uint bestMission = 0;

            foreach (var mission in C.MissionConfig)
            {
                var id = mission.Key;

                if (!CosmicHelper.QuickLevelList.Contains(id))
                    continue;

                if (CosmicHelper.SheetMissionDict.TryGetValue(id, out var missionInfo))
                {
                    var attribute = missionInfo.Attributes;
                    var missionLevel = missionInfo.Level;

                    // if (!missionInfo.Jobs.Contains((uint)Player.Job))
                        // continue;

                    int playerTier = playerLevel >= 90 ? 90 : playerLevel >= 50 ? 50 : 10;

                    if (missionLevel != playerTier)
                        continue;

                    bestMission = id;
                }
            }

            return bestMission;
        }

        private static uint selectedId = 0;

        private static void DroidCheck()
        {
            if (ImGui.CollapsingHeader("Object info"))
            {
                foreach (var obect in Svc.Objects.OrderBy(x => Player.DistanceTo(x.Position)))
                {
                    ImGui.Text($"Name: {obect.Name} : {obect.BaseId}");
                }
            }
        }

        private static unsafe bool DroneReady()
        {
            var actionManager = ActionManager.Instance();

            // For regular items
            uint itemId = 50414; // your item ID
            var actionStatus = actionManager->GetActionStatus(
                ActionType.Item,
                itemId
            );

            // actionStatus == 0 means the item is ready to use
            // any other value indicates it's not ready (on cooldown, requirements not met, etc.)
            if (actionStatus == 0)
            {
                return true;
            }
            else if (Task_ArtifactSearch.IsTreasureDetected())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static unsafe void UseDrone()
        {
            uint itemId = 50414;
            var inventoryManager = InventoryManager.Instance();

            // Array of inventory types to check
            var inventoryTypes = new[]
            {
                InventoryType.Inventory1,
                InventoryType.Inventory2,
                InventoryType.Inventory3,
                InventoryType.Inventory4
            };

            foreach (var invType in inventoryTypes)
            {
                var container = inventoryManager->GetInventoryContainer(invType);
                if (container == null) continue;

                for (int i = 0; i < container->Size; i++)
                {
                    var item = container->GetInventorySlot(i);
                    if (item != null && item->ItemId == itemId)
                    {
                        // Use the item from inventory
                        AgentInventoryContext.Instance()->UseItem(item->ItemId, invType, (uint)i, 0);
                        return;
                    }
                }
            }

            // If we get here, item wasn't found
            PluginLog.Warning($"Item {itemId} not found in any inventory container");
        }

        private static Vector3 customDestination = Vector3.Zero;

        private static bool? MovetoFlag()
        {
            if (Task_NavmeshMove.Task_NavTo(customDestination, stayMounted: true).Value)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
