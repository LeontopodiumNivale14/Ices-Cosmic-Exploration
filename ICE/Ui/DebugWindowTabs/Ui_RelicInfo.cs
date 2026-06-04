using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using System.Collections.Generic;

namespace ICE.Ui.DebugWindowTabs
{
    internal class Ui_RelicInfo
    {
        public static List<string> XPtypes = ["I", "II", "III", "IV", "V", "VI"];
        public static List<(string Name, uint Id)> jobOptions = new()
        {
            ("CRP", 8),
            ("BSM", 9),
            ("ARM", 10),
            ("GSM", 11),
            ("LTW", 12),
            ("WVR", 13),
            ("ALC", 14),
            ("CUL", 15),
            ("MIN", 16),
            ("BTN", 17),
            ("FSH", 18),
        };

        public static unsafe void Draw()
        {
            var wksManager = WKSManager.Instance();
            if (wksManager == null || wksManager->ResearchModule == null || !wksManager->ResearchModule->IsLoaded)
                return;

            if (ImGui.BeginTable("Relic Info", 15, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Class");
                ImGui.TableSetupColumn("Stage");
                for (int i = 0; i < XPtypes.Count; i++)
                {
                    ImGui.TableSetupColumn($"{XPtypes[i]} - Current");
                    ImGui.TableSetupColumn($"{XPtypes[i]} - Need");
                    ImGui.TableSetupColumn($"{XPtypes[i]} - Max");
                }
                ImGui.TableSetupColumn("Score");

                ImGui.TableHeadersRow();

                foreach (var job in jobOptions)
                {
                    ImGui.TableNextRow();

                    // Class Name
                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted(job.Name);

                    ImGui.TableNextColumn();
                    var toolClassId = (byte)(job.Id - 7);
                    var stage = wksManager->ResearchModule->CurrentStages[toolClassId - 1];
                    ImGui.TextUnformatted(stage.ToString());

                    for (byte type = 1; type <= 4; type++)
                    {
                        if (!wksManager->ResearchModule->IsTypeAvailable(toolClassId, type))
                            break;

                        var neededXP = wksManager->ResearchModule->GetNeededAnalysis(toolClassId, type);

                        var maxXP = wksManager->ResearchModule->GetMaxAnalysis(toolClassId, type);

                        var currentXp = wksManager->ResearchModule->GetCurrentAnalysis(toolClassId, type);

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{currentXp}");

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{neededXP}");

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{maxXP}");
                    }

                    ImGui.TableSetColumnIndex(14);
                    var scores = wksManager->Scores;
                    int classScore = scores[(int)job.Id - 8];

                    ImGui.TextUnformatted($"{classScore}");
                }

                ImGui.EndTable();
            }
        }
    }
}
