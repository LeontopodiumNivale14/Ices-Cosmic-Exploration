using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ICE.Ui.DebugWindowTabs;
using ICE.Ui.Waypoint_Manager;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ICE.Utilities.CosmicHelper;

namespace ICE.Ui;

internal class DebugWindow : Window
{
    public DebugWindow() :
        base($"ICE {P.GetType().Assembly.GetName().Version} ###IceCosmicDebug1")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(100, 100),
            MaximumSize = new Vector2(3000, 3000)
        };
        P.windowSystem.AddWindow(this);
    }

    public void Dispose()
    {
        P.windowSystem.RemoveWindow(this);
    }

    private string[] DebugTypes = ["Player Info", "Main Mood Hud", 
                                   "Mission Hud", "Mission Info Hud", 
                                   "Wheel of fortune!", "Moon Recipe Hud", 
                                   "Moon Mission Info", "Crafting Table", "Gathering Table", 
                                   "Test Buttons", "IPC Testing", 
                                   "Map Test", "Gather Editor",
                                   "Navmesh Testing", "Relic Info",
                                   "Route Editor"];

    int selectedDebugIndex = 0; // This should be stored somewhere persistent

    public override unsafe void Draw()
    {
        float spacing = 10f;
        float leftPanelWidth = 200f;
        float rightPanelWidth = ImGui.GetContentRegionAvail().X - leftPanelWidth - spacing;
        float childHeight = ImGui.GetContentRegionAvail().Y;

        if (ImGui.BeginChild("DebugSelector", new System.Numerics.Vector2(leftPanelWidth, childHeight), true))
        {
            for (int i = 0; i < DebugTypes.Length; i++)
            {
                bool isSelected = (selectedDebugIndex == i);
                string label = isSelected ? $"→ {DebugTypes[i]}" : $"   {DebugTypes[i]}"; // Add space for alignment

                if (ImGui.Selectable(label, isSelected))
                {
                    selectedDebugIndex = i;
                }
            }
            ImGui.EndChild();
        }

        ImGui.SameLine(0, spacing);

        if (ImGui.BeginChild("DebugContent", new System.Numerics.Vector2(rightPanelWidth, childHeight), true))
        {
            switch (selectedDebugIndex)
            {
                case 0: PlayerInfo.Draw(); break;
                case 1: MainMoonHud.Draw(); break;
                case 2: MissionHud.Draw(); break;
                case 3: MissionInfoHud.Draw(); break;
                case 4: WheelofFortuneHud.Draw(); break;
                case 5: MoonRecipeHud.Draw(); break;
                case 6: MoonMissionInfo.Draw(); break;
                case 7: MoonRecipeTable.Draw(); break;
                case 8: MoonGatheringTable.Draw(); break;
                case 9: TestButtons.Draw(); break;
                case 10: IPCTesting.Draw(); break;
                case 11: MapTesting.Draw(); break;
                case 12: GatheringViewer.Draw(); break;
                case 13: Navmesh_Testing.Draw(); break;
                case 14: RelicInfo.Draw(); break;
                case 15: RouteEditor.Draw(); break;
                default: ImGui.Text("Unknown Debug View"); break;
            }

            ImGui.EndChild();
        }
    }

    public static void ExportMissionInfoToCsv(Dictionary<uint, MissionListInfo> dict, string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            // Write the header
            writer.Write("Key,Name,JobId,JobId2,JobId3,ToDoSlot,Rank,RecipeId,SilverRequirement,GoldRequirement,CosmoCredit,LunarCredit");

            // Find the max number of ExperienceRewards across all missions
            int maxRewards = dict.Values.Max(info => info.ExperienceRewards?.Count ?? 0);

            // Add headers for each possible reward
            for (int i = 1; i <= maxRewards; i++)
            {
                writer.Write($",Type{i},Amount{i}");
            }

            writer.WriteLine(); // End header line

            foreach (var kvp in dict)
            {
                var info = kvp.Value;

                // Escape name if needed
                string safeName = info.Name.Contains(",") ? $"\"{info.Name}\"" : info.Name;

                writer.Write($"{kvp.Key},{safeName},{info.JobId},{info.JobId2},{info.JobId3},{info.ToDoSlot},{info.Rank},{info.RecipeId},{info.SilverRequirement},{info.GoldRequirement},{info.CosmoCredit},{info.LunarCredit}");

                if (info.ExperienceRewards != null)
                {
                    foreach (var reward in info.ExperienceRewards)
                    {
                        writer.Write($",{reward.Type},{reward.Amount}");
                    }
                }

                // Fill missing cells if this mission has fewer rewards
                int rewardCount = info.ExperienceRewards?.Count ?? 0;
                for (int i = rewardCount; i < maxRewards; i++)
                {
                    writer.Write(",,");

                }

                writer.WriteLine();
            }
        }
    }
}
