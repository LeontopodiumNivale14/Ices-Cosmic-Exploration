using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Ui.DebugWindowTabs
{
    internal class MissionHud
    {
        private static int BestMission = 0;

        public class DummyXP
        {
            public int CurrentXP {  get; set; }
            public int NeededXP { get; set; }
        }

        public static Dictionary<int, DummyXP> DummyXPTest = new Dictionary<int, DummyXP>()
        {
            { 1, new DummyXP { CurrentXP = 0, NeededXP = 500} },
            { 2, new DummyXP { CurrentXP = 0, NeededXP = 0} },
            { 3, new DummyXP { CurrentXP = 0, NeededXP = 0} },
            { 4, new DummyXP { CurrentXP = 0, NeededXP = 0} },
            { 5, new DummyXP { CurrentXP = 0, NeededXP = 0} },
        };

        public static bool UseXPDebugger = false;

        public static void Draw()
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                ImGui.Text("List of Visible Missions");
                ImGui.Text($"Selected Mission Name: {x.SelectedMissionName}");
                ImGui.Text($"Selected Mission ID: {x.SelectedMissionId}");

                if (ImGui.Button("Help"))
                {
                    x.Help();
                }
                ImGui.SameLine();

                if (ImGui.Button("Mission Selection"))
                {
                    x.MissionSelection();
                }
                ImGui.SameLine();

                if (ImGui.Button("Mission Log"))
                {
                    x.MissionLog();
                }
                ImGui.SameLine();

                if (ImGui.Button("Basic Missions"))
                {
                    x.BasicMissions();
                }
                ImGui.SameLine();

                if (ImGui.Button("Provisional Missions"))
                {
                    x.ProvisionalMissions();
                }
                ImGui.SameLine();

                if (ImGui.Button("Critical Missions"))
                {
                    x.CriticalMissions();
                }

                bool EnableDummyXp = UseXPDebugger;
                if (ImGui.Checkbox("Enable Dummy XP", ref EnableDummyXp))
                    UseXPDebugger = EnableDummyXp;

                foreach (var key in DummyXPTest.Keys.ToList())
                {
                    var xp = DummyXPTest[key];

                    ImGui.Text($"XP: {key}");
                    ImGui.PushID(key);

                    int currentXP = xp.CurrentXP;
                    int neededXP = xp.NeededXP;

                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt("Current XP", ref currentXP))
                        xp.CurrentXP = currentXP;

                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt("Needed XP", ref neededXP))
                        xp.NeededXP = neededXP;

                    ImGui.PopID();

                    // Update back in dictionary (technically not needed since we're editing a class)
                    DummyXPTest[key] = xp;

                    ImGui.Separator();
                }

                foreach (var m in x.StellerMissions)
                {
                    ImGui.Text($"{m.Name}");
                    ImGui.SameLine();
                    if (ImGui.Button($"Select###Select + {m.Name}"))
                    {
                        m.Select();
                    }
                }

                ImGui.Text($"Best Relic Mission: {BestMission}");
                if (ImGui.Button("Update Best Mission"))
                {
                    BestMission = 0;
                    int? test = TaskMissionFind.FindRelicMission();
                    if (test != null)
                    {
                        BestMission = test.Value;
                    }
                }


            }
            else
            {
                ImGui.Text("Waiting for \"WKSMission\" to be visible");
            }
        }
    }
}
