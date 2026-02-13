using ICE.Ui.DebugWindowTabs;
using ICE.Ui.MainUi.HelpFolder;
using System.Collections.Generic;

namespace ICE.Ui;

internal class DebugWindow : Window
{
    public DebugWindow() :
        base($"ICE {P.GetType().Assembly.GetName().Version} Debugger ###IceCosmicDebug1")
    {
        Flags = ImGuiWindowFlags.None;
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

    private readonly Dictionary<string, Action> DebugViews = new()
    {
        // HUD Elements
        ["Hud: Moon Main"] = () => Hud_MainMoon.Draw(),
        ["Hud: Mission"] = () => Hud_Mission.Draw(),
        ["Hud: Mission Info"] = () => Hud_MissionInfo.Draw(),
        ["Hud: Wheel of fortune!"] = () => Hud_WheelofFortune.Draw(),
        ["Hud: Moon Recipe"] = () => Hud_MoonRecipe.Draw(),
        ["Hud: Gather Collectable"] = () => Hud_CollectableGathering.Draw(),
        ["Hud: Item Exchange"] = () => Hud_ItemExchange.Draw(),

        // Table Elements
        ["Table: Mission Info"] = () => Table_MissionInfo.Draw(),
        ["Table: Item List"] = () => Table_CustomItems.Draw(),
        ["Table: Gathering Missions"] = () => Table_GatheringInfo.Draw(),
        ["Table: Special Missions"] = () => Table_TimeWeather.Draw(),
        ["Table: Mission Text"] = () => Table_MissionText.Draw(),
        ["Table: Recipies"] = () => Table_MoonRecipies.Draw(),
        ["Table: Fish Info"] = () => Table_FishInfo.Draw(),

        // UI Elements
        ["Ui: Fishing Hole Editor"] = () => Ui_Fish_HoleEditor.Draw(),
        ["Ui: Fishing Preset Editor"] = () => Ui_FishPresets.Draw(),
        ["Ui: Gather Editor"] = () => Ui_GatherRoute_Editor.Draw(),
        ["Ui: Log Viewer"] = () => helpSelect_Logs.Draw_Debug(),
        ["Ui: Player Gearsets"] = () => Ui_Gearsets.Draw(),

        // Non-labeled Elements
        ["Player Info"] = () => Ui_PlayerInfo.Draw(),
        ["Test Buttons"] = () => Ui_TestButtons.Draw(),
        ["IPC Testing"] = () => Ui_IPCTesting.Draw(),
        ["Map Test"] = () => Ui_MapTesting.Draw(),
        ["Navmesh Testing"] = () => Ui_NavmeshTesting.Draw(),
        ["Relic Info"] = () => Ui_RelicInfo.Draw(),
        ["TaskManager Testing"] = () => Ui_TaskManagerInfo.Draw(),
        ["NPC Box Viewer"] = () => Ui_NpcViewer.Draw(),

        // Sheet Viewer Info
        ["Sheet: Mission Rewards"] = () => Sheet_MissionRewards.Draw(),
        ["Table: Leveling Missions"] = () => Table_LevelingMissions.Draw(),
        ["Table: Mission Select"] = () => Table_MissionSelect.Draw(),
        ["Oizyr Map Stuff"] = () => Ui_OyzinMap.Draw(),
    };

    private string selectedDebugView = "Hud: Moon Main"; // Store the name instead of index

    public override unsafe void Draw()
    {
        float spacing = 10f;
        float leftPanelWidth = 200f;
        float rightPanelWidth = ImGui.GetContentRegionAvail().X - leftPanelWidth - spacing;
        float childHeight = ImGui.GetContentRegionAvail().Y;

        if (ImGui.BeginChild("DebugSelector", new Vector2(leftPanelWidth, childHeight), true))
        {
            foreach (var viewName in DebugViews.Keys)
            {
                bool isSelected = (selectedDebugView == viewName);
                string label = isSelected ? $"→ {viewName}" : $"   {viewName}";

                if (ImGui.Selectable(label, isSelected))
                {
                    selectedDebugView = viewName;
                }
            }
        }
        ImGui.EndChild();

        ImGui.SameLine(0, spacing);

        if (ImGui.BeginChild("DebugContent", new System.Numerics.Vector2(rightPanelWidth, childHeight), true))
        {
            if (DebugViews.TryGetValue(selectedDebugView, out var drawAction))
            {
                drawAction();
            }
            else
            {
                ImGui.Text("Unknown Debug View");
            }
        }
        ImGui.EndChild();
    }
}
