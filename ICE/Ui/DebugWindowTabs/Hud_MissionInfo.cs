using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.Cosmic_Helper;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Ui.DebugWindowTabs
{
    internal class Hud_MissionInfo
    {
        public static unsafe void Draw()
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var x) && x.IsAddonReady)
            {
                var isAddonReady = AddonHelper.IsAddonActive("WKSMissionInfomation");
                ImGui.Text($"Addon Ready: {isAddonReady}");
                if (isAddonReady)
                {
                    ImGui.Text($"Node Text: {AddonHelper.GetNodeText("WKSMissionInfomation", 27)}");
                }

                ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                             ImGuiTableFlags.Borders |
                                             ImGuiTableFlags.SizingFixedFit |
                                             ImGuiTableFlags.Resizable |           // Allow column resizing
                                             ImGuiTableFlags.Reorderable |         // Allow column reordering
                                             ImGuiTableFlags.Hideable;             // Allow hiding columns via right-click

                if (ImGui.BeginTable("WKSMissionInfomationAddon_Table", 2, tableFlags))
                {
                    ImGui.TableSetupColumn("###Info", ImGuiTableColumnFlags.WidthFixed, 150);
                    ImGui.TableSetupColumn("###UiInfo", ImGuiTableColumnFlags.WidthFixed, 100);

                    var missionId = CosmicHelper.CurrentLunarMission;
                    

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Current Mission:");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{missionId}");

                    if (CosmicHelper.SheetMissionDict.TryGetValue(missionId, out var mission) && !mission.Attributes.HasFlag(MissionAttributes.Critical))
                    {
                        ImGui.TableNextColumn();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text("Current Score:");
                        ImGui.TableNextColumn();

                        ImGui.Text($"{CurrentScore()}");

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text($"Current State");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{Task_CheckScore.CurrentRank()}");


                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text("Is Mission Timed out");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{CosmicHandler.IsMissionTimedOut()}");
                    }
                    else if (mission.Attributes.HasFlag(MissionAttributes.Critical))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text("Critical Value:");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{x.CriticalScore}");
                    }
                    
                    if (mission.Attributes.HasFlag(MissionAttributes.Fish))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text("Current Bait");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{CosmicHelper.CurrentBait}");
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Cosmo Pouch"))
                    {
                        x.CosmoPouch();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Cosmo Crafting Log"))
                    {
                        x.CosmoCraftingLog();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Steller Reduction"))
                    {
                        x.StellerReduction();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Report"))
                    {
                        x.Report();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Abandon"))
                    {
                        x.Abandon();
                    }

                    var wks = WKSManager.Instance();
                    if (wks == null)
                        return;

                    var scores = wks->Scores;

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Score 1");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{scores.Length}");

                    for (int score = 0; score < scores.Length; score++)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text($"Score: [{score}]");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{scores[score]}");
                    }

                    /*
                    var currentlyEquippped = wks->FishingBait | 0;

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Bait:");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{currentlyEquippped}");
                    */


                    ImGui.EndTable();
                }
            }
            else
            {
                ImGui.Text("Waiting for \"WKSMissionInfomation\" to be visible");
            }
        }

        private static unsafe uint CurrentScore()
        {
            var managerPtr = WKSManager.Instance();
            if (managerPtr == null) return 0;

            return managerPtr->CurrentScore;
        }
    }
}
