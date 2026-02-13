using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using ICE.Ui.MainUi.Settings.Settings_Table;
using ICE.Utilities.ImGuiTools;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace ICE.Ui.MainUi.ModeSelect
{
    internal class modeSelect_Standard
    {
        private static readonly Dictionary<string, uint> BattleJobs = new()
        {
            // Tanks
            { "Paladin", 19 },
            { "Warrior", 21 },
            { "Dark Knight", 32 },
            { "Gunbreaker", 37 },
    
            // Healers
            { "White Mage", 24 },
            { "Scholar", 28 },
            { "Astrologian", 33 },
            { "Sage", 40 },
    
            // Melee DPS
            { "Monk", 20 },
            { "Dragoon", 22 },
            { "Ninja", 30 },
            { "Samurai", 34 },
            { "Reaper", 39 },
            { "Viper", 41 },
    
            // Physical Ranged DPS
            { "Bard", 23 },
            { "Machinist", 31 },
            { "Dancer", 38 },
    
            // Magical Ranged DPS
            { "Black Mage", 25 },
            { "Summoner", 27 },
            { "Red Mage", 35 },
            { "Pictomancer", 42 }
        };
        private static string newListName = "";

        public static void Draw()
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 10).Push(ImGuiStyleVar.ChildBorderSize, 1);

            // Header at the top
            float scale = ImGuiHelpers.GlobalScale;

            bool autoSelectMoon = C.AutoSelectMoon;
            if (autoSelectMoon)
            {
                if (PlayerHelper.IsInSinusArdorum() && (!C.ShowSinusMissions || C.ShowPhaennaMissions || C.ShowOizysMissions))
                {
                    C.ShowSinusMissions = true;
                    C.ShowPhaennaMissions = false;
                    C.ShowOizysMissions = false;
                    C.Save();
                }
                else if (PlayerHelper.IsInPhaenna() && (C.ShowSinusMissions || !C.ShowPhaennaMissions || C.ShowOizysMissions))
                {
                    C.ShowSinusMissions = false;
                    C.ShowPhaennaMissions = true;
                    C.ShowOizysMissions = false;
                    C.Save();
                }
                else if (PlayerHelper.IsInOizys() && (C.ShowSinusMissions || C.ShowPhaennaMissions || !C.ShowOizysMissions))
                {
                    C.ShowSinusMissions = false;
                    C.ShowPhaennaMissions = false;
                    C.ShowOizysMissions = true;
                    C.Save();
                }
            }

            using (var headerChild = ImRaii.Child("##modeSelect_StandardHeader", new Vector2(0, 45 * scale), true, ImGuiWindowFlags.NoScrollbar))
            {
                if (!headerChild.Success) return;

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10 * scale);
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5 * scale);

                string modeType = string.Empty;
                FontAwesomeIcon modeIcon = FontAwesomeIcon.List;

                bool relicMode = C.XPRelicGrind;
                bool xpLeveling = C.XPLeveling_Mode;
                bool standard = (!relicMode && !xpLeveling);


                if (standard)
                    modeType = "Standard";
                else if (relicMode)
                {
                    modeType = "Relic Grind";
                    modeIcon = FontAwesomeIcon.ArrowUpRightDots;
                }
                else if (xpLeveling)
                {
                    modeType = "Leveling Grind";
                    modeIcon = FontAwesomeIcon.Leaf;
                }

                ImGuiEx.IconWithText(modeIcon, $"{modeType} Mode");

                ImGui.SameLine(0, 10 * scale);

                // Adjust the Y position to center the button vertically with the text
                float textHeight = ImGui.GetTextLineHeight();
                float buttonHeight = ImGui.GetFrameHeight();
                float yOffset = (textHeight - buttonHeight) / 2f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);

                if (ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "Mode Selection"))
                {
                    ImGui.OpenPopup("Mode Select | Select Mode Window");
                }
                if (ImGui.BeginPopup("Mode Select | Select Mode Window"))
                {
                    ImGui.Text("Select Mode");
                    ImGui.Separator();

                    if (ImGui.RadioButton("Standard", standard))
                    {
                        C.XPRelicGrind = false;
                        C.XPLeveling_Mode = false;
                        C.Save();
                    }
                    ImGuiEx.HelpMarker("Stand Mode \n" +
                                       "-> Used to select which missions you want to grind. It'll priortize in the following order:\n" +
                                       "-> Critical -> Provisional [Sequence/Timed/Weather] -> Standard [A->D]\n" +
                                       "-> Select which missions you want to do, and go at it.");
                    if (ImGui.RadioButton("Relic Grind", relicMode))
                    {
                        C.XPRelicGrind = true;
                        C.XPLeveling_Mode = false;
                        C.Save();
                    }
                    ImGuiEx.HelpMarker("Relic Grind\n" +
                                       "-> Automatically select which missions that are best to finish up your relic\n" +
                                       "-> These are weighed based on what is needed to complete the tool to the next step\n" +
                                       "-> If you want to only do certain missions, enable the option and select which ones you want to do");

                    if (ImGui.RadioButton("Leveling Grind", xpLeveling))
                    {
                        C.XPRelicGrind = false;
                        C.XPLeveling_Mode = true;
                        C.Save();
                    }
                    ImGuiEx.HelpMarker("Leveling Grind\n" +
                                       "-> Will automatically select which mission is the best for leveling your current class based on what level bracket you're in\n" +
                                       "-> These are hand picked by me, and determined by the time it takes to complete it\n" +
                                       "-> For crafters it's whatever missions take the least amount of progress" +
                                       "-> For gathering, it's whatever is the least pain to do w/ the minimum amount of skills\n" +
                                       "**These will automatically set settings for using these modes temporarily**");

                    ImGui.EndPopup();
                }

                uint currentJobId = (uint)Player.Job;
                bool usingSupportedJob = CosmicHelper.CrafterJobList.Contains(currentJobId) || CosmicHelper.GatheringJobList.Contains(currentJobId);

                bool AnyStop = C.StopOnceHitCosmicScore
                             | C.StopWhenLevel
                            || C.StopOnceHitCosmoCredits
                            || C.StopOnceHitLunarCredits
                            || C.StopOnceRelicFinished;
                if (AnyStop)
                {
                    ImGui.SameLine(0, 10 * scale);
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);
                    ImGuiEx.Icon(FontAwesomeIcon.ExclamationTriangle);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();

                        ImGui.Text("It appears that you have on of the following enabled");
                        if (C.StopOnceHitCosmicScore)
                            ImGui.BulletText($"Stop at Cosmic Score [{C.CosmicScoreCap:N0}]");
                        if (C.StopWhenLevel)
                            ImGui.BulletText($"Stop When Level [{C.TargetLevel:N0}]");
                        if (C.StopOnceHitCosmoCredits)
                            ImGui.BulletText($"Stop once cosmo credit hit [{C.CosmoCreditsCap:N0}]");
                        if (C.StopOnceHitLunarCredits)
                            ImGui.BulletText($"Stop once planetary credit hit [{C.LunarCreditsCap:N0}]");
                        if (C.StopOnceRelicFinished)
                            ImGui.BulletText($"Stop once relic completed");

                        ImGui.Text("So if you stop and you're unsure why... this might be why");

                        ImGui.EndTooltip();
                    }
                }

                ImGui.SameLine(0, 10 * scale);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);

                bool unsupportedArtisan = xpLeveling && !P.Artisan.UpdatedArtisan() && CosmicHelper.CrafterJobList.Contains((uint)Player.Job);
                bool unsupportedMoon = PlayerHelper.IsInOizys() && xpLeveling;

                using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !usingSupportedJob || unsupportedArtisan || unsupportedMoon))
                {
                    if (ImGui.Button("Start", new Vector2(150 * scale, 0)))
                    {
                        SchedulerMain.EnablePlugin();
                    }
                }

                if (unsupportedArtisan)
                {
                    ImGui.SameLine(0, 10 * scale);
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);
                    ImGuiEx.Icon(EColor.Red, FontAwesomeIcon.ExclamationTriangle);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Hey! You need to update artisan to use this mode, please update to at minimum:");
                        ImGui.Text("4.0.4.29");
                        ImGui.EndTooltip();
                    }
                }
                else if (unsupportedMoon)
                {
                    ImGui.SameLine(0, 10 * scale);
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);
                    ImGuiEx.Icon(EColor.Red, FontAwesomeIcon.ExclamationTriangle);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Hey! This moon is currently not supported for leveling yet. (It's also worse than sinus or phaenna)");
                        ImGui.Text("Please wait till I get the time to focus on this");
                        ImGui.EndTooltip();
                    }
                }

                ImGui.SameLine(0, 10 * scale);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + yOffset);

                using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
                {
                    using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f)))
                    using (ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f)))
                    using (ImRaii.PushColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1.0f)))
                    {
                        if (ImGui.Button("Stop", new Vector2(150 * scale, 0)))
                        {
                            SchedulerMain.DisablePlugin();
                        }
                    }
                }
            }

            if (ImGui.BeginTable("modeSelect_TableHeader", 5, ImGuiTableFlags.SizingFixedFit, Vector2.Zero))
            {
                ImGui.TableSetupColumn("Class Selector");
                ImGui.TableSetupColumn("Other Settings");

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                bool tableSettingExpanded = modeSelect_Tools.DrawCompactCategoryHeader("Table Settings", FontAwesomeIcon.Table);

                ImGui.TableNextColumn();
                bool missionSettingExpanded = modeSelect_Tools.DrawCompactCategoryHeader("Mission Settings", FontAwesomeIcon.UserCog);

                bool relicGrindExpanded = false;
                if (C.XPRelicGrind)
                {
                    ImGui.TableNextColumn();
                    relicGrindExpanded = modeSelect_Tools.DrawCompactCategoryHeader("Relic Grind Settings", FontAwesomeIcon.ArrowUpRightDots);
                }

                bool completionExpanded = false;
                if (C.ShowCompletionWindow)
                {
                    ImGui.TableNextColumn();
                    completionExpanded = modeSelect_Tools.DrawCompactCategoryHeader("Completion Table Settings", FontAwesomeIcon.Trophy);
                }

                bool showPlaylistExpanded = false;
                bool standard = !(C.XPRelicGrind || C.XPLeveling_Mode || C.ShowCompletionWindow);
                if (standard)
                {
                    ImGui.TableNextColumn();
                    showPlaylistExpanded = modeSelect_Tools.DrawCompactCategoryHeader("Mission Presets", FontAwesomeIcon.PlayCircle);
                }

                bool showJobSwapExpanded = false;
                bool relicJobSwap = C.TurninRelic;

                if (relicJobSwap)
                {
                    ImGui.TableNextColumn();
                    showJobSwapExpanded = modeSelect_Tools.DrawCompactCategoryHeader("Relic Job Swap", FontAwesomeIcon.Hammer);
                }

                bool showNextColumn = tableSettingExpanded || missionSettingExpanded || (relicGrindExpanded && C.XPRelicGrind) || (completionExpanded && C.ShowCompletionWindow) || showPlaylistExpanded || showJobSwapExpanded;

                if (showNextColumn)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (tableSettingExpanded)
                    {
                        Settings_TableColumns.ColumnSettings();
                    }

                    ImGui.TableNextColumn();
                    if (missionSettingExpanded)
                    {
                        Settings_TableColumns.GeneralMissionSettings();
                    }

                    if (C.XPRelicGrind)
                    {
                        ImGui.TableNextColumn();
                        if (relicGrindExpanded)
                        {
                            bool relicTurnin = C.TurninRelic;
                            if (ImGui.Checkbox($"Turnin if relic is complete##RelicTurnin_RelicGrind", ref relicTurnin))
                            {
                                C.TurninRelic = relicTurnin;
                                C.Save();
                            }
                            ImGui.SameLine();
                            ImGui.TextDisabled("?");
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("THIS IS YOUR HEADS UP ON HOW THIS WORKS. If I change this in the future, this tooltip will also change.\n" +
                                                 "1: This will check for your current CLASS [not menu class, actual current class] for relic turnin.\n" +
                                                 "2: This will take prio over \"Stop @ Relic Turnin\", in the sense that if you have both enabled, it will turnin vs stop. And continue about it's day\n" +
                                                 "3: If you're on a crafting class, it will return you back to the stop you were crafting post turnin. \n" +
                                                 "\t- This is optional, you can disable it at your own free will, I just like this so I can just go back to an isolated area of my choosing");
                            }

                            ImGui.Separator();

                            bool EnableRelicXp = C.XPRelicGrind;
                            if (ImGui.Checkbox("Auto-Pick For Relic XP", ref EnableRelicXp))
                            {
                                C.XPRelicGrind = EnableRelicXp;
                                C.Save();
                            }
                            ImGui.SameLine();
                            ImGui.TextDisabled("?");
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Please note. This will ONLY grind for relic Exp under the basic mission tab. \n" +
                                                   "This will NOT work (even with missions selected) on the Sequence/Timed/Weather/Critical Missions");
                            }
                            if (EnableRelicXp)
                            {
                                bool OnlySelected = C.XPRelicOnlyEnabled;
                                if (ImGui.Checkbox("Only selected missions", ref OnlySelected))
                                {
                                    C.XPRelicOnlyEnabled = OnlySelected;
                                    C.Save();
                                }
                                if (C.ShowManualMode)
                                {
                                    bool IgnoreManual = C.XPRelicIgnoreManual;
                                    if (ImGui.Checkbox("Ignore Manual Mode Missions", ref IgnoreManual))
                                    {
                                        C.XPRelicIgnoreManual = IgnoreManual;
                                        C.Save();
                                    }
                                }
                            }
                        }
                    }

                    if (C.ShowCompletionWindow)
                    {
                        ImGui.TableNextColumn();
                        if (completionExpanded)
                        {
                            bool showSelectedJobOnly = C.ShowSelectedJobOnly;
                            if (ImGui.Checkbox("Show only selected job", ref showSelectedJobOnly))
                            {
                                C.ShowSelectedJobOnly = showSelectedJobOnly;
                                if (showSelectedJobOnly)
                                    C.ShowCompletionOnlyJob = false;
                                C.Save();
                            }

                            bool nonGold = C.ShowCompletion_MissingGold;
                            if (ImGui.Checkbox("Show Only Non-Gold Missions", ref nonGold))
                            {
                                C.ShowCompletion_MissingGold = nonGold;
                                C.Save();
                            }
                        }
                    }

                    if (standard)
                    {
                        ImGui.TableNextColumn();

                        if (showPlaylistExpanded)
                        {
                            if (ImGui.Button("Save Current Mission Preset"))
                            {
                                ImGui.OpenPopup("Preset Save Editor");
                            }

                            if (ImGui.BeginPopup("Preset Save Editor"))
                            {
                                ImGui.InputText($"Playlist Name", ref newListName);
                                using (ImRaii.Disabled(string.IsNullOrEmpty(newListName)))
                                {
                                    if (ImGui.Button("Save New List"))
                                    {
                                        List<uint> new_Playlist = new();
                                        foreach (var mission in C.MissionConfig.Where(x => x.Value.Enabled))
                                        {
                                            new_Playlist.Add(mission.Key);
                                        }
                                        if (C.Mission_Playlist.ContainsKey(newListName))
                                        {
                                            C.Mission_Playlist[newListName] = new_Playlist;
                                        }
                                        else
                                        {
                                            C.Mission_Playlist.Add(newListName, new_Playlist);
                                        }
                                        C.Save();
                                        ImGui.CloseCurrentPopup();
                                    }
                                }

                                ImGui.EndPopup();
                            }

                            if (C.Mission_Playlist.Count > 0)
                            {
                                if (ImGui.Button("View All Presets"))
                                {
                                    ImGui.OpenPopup("Preset: List Viewer");
                                }

                                if (ImGui.BeginPopup("Preset: List Viewer"))
                                {
                                    ImGui.Text($"Load Mission Preset");

                                    if (ImGui.BeginTable($"Preset: TableViewer", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
                                    {
                                        ImGui.TableSetupColumn("Name");
                                        ImGui.TableSetupColumn("Amount Enabled");

                                        ImGui.TableHeadersRow();

                                        ImGui.TableNextRow();
                                        ImGui.TableSetColumnIndex(0);
                                        ImGui.AlignTextToFramePadding();
                                        ImGui.Text($"Clear All");
                                        ImGui.SameLine();
                                        if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare, $"FreshPreset_Button"))
                                        {
                                            foreach (var mission in C.MissionConfig)
                                            {
                                                mission.Value.Enabled = false;
                                            }
                                            C.Save();
                                            ImGui.CloseCurrentPopup();
                                        }

                                        foreach (var item in C.Mission_Playlist)
                                        {
                                            ImGui.TableNextRow();
                                            ImGui.TableSetColumnIndex(0);
                                            ImGui.AlignTextToFramePadding();
                                            ImGui.Text($"{item.Key}");
                                            ImGui.SameLine();
                                            if (ImGuiEx.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare, $"{item.Key}_Button"))
                                            {
                                                foreach (var mission in C.MissionConfig)
                                                {
                                                    if (item.Value.Contains(mission.Key))
                                                        mission.Value.Enabled = true;
                                                    else
                                                        mission.Value.Enabled = false;
                                                }
                                                C.Save();
                                                ImGui.CloseCurrentPopup();
                                            }
                                            if (ImGui.IsItemHovered())
                                            {
                                                ImGui.SetTooltip("Import Missions");
                                            }

                                            ImGui.TableNextColumn();
                                            ImGui.AlignTextToFramePadding();
                                            ImGui.Text($"{item.Value.Count}");

                                            ImGui.TableNextColumn();
                                            if (ImGuiEx.IconButton(FontAwesomeIcon.Trash, $"{item.Key}_Remove"))
                                            {
                                                C.Mission_Playlist.Remove(item);
                                                C.Save();
                                            }
                                            if (ImGui.IsItemHovered())
                                            {
                                                ImGui.SetTooltip("Remove from list");
                                            }
                                        }

                                        ImGui.EndTable();
                                    }

                                    ImGui.EndPopup();
                                }
                            }
                        }
                    }

                    if (C.TurninRelic)
                    {
                        ImGui.TableNextColumn();
                        if (showJobSwapExpanded)
                        {
                            bool swapJobs = C.Relic_SwapJob;
                            if (ImGui.Checkbox("Swap jobs when turning in relic", ref swapJobs))
                            {
                                C.Relic_SwapJob = swapJobs;
                                C.Save();
                            }

                            string currentJobName = BattleJobs.FirstOrDefault(x => x.Value == C.Relic_BattleJob).Key ?? "None";

                            if (ImGui.BeginCombo("Battle Job", currentJobName))
                            {
                                foreach (var job in BattleJobs)
                                {
                                    bool isSelected = C.Relic_BattleJob == job.Value;
                                    if (ImGui.Selectable(job.Key, isSelected))
                                    {
                                        C.Relic_BattleJob = job.Value;
                                        C.Save();
                                    }
                                    if (isSelected)
                                        ImGui.SetItemDefaultFocus();
                                }
                                ImGui.EndCombo();
                            }

                            bool useStylist = C.Relic_Stylist;
                            if (ImGui.Checkbox($"Use Stylist to re-equip tools", ref useStylist))
                            {
                                C.Relic_Stylist = useStylist;
                                C.Save();
                            }
                        }
                    }
                }

                ImGui.EndTable();
            }

            using (var bodyChild = ImRaii.Child("##modeSelect_Body", new Vector2(0, -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (!bodyChild.Success) return;

                foreach (var missionType in modeSelect_TableInfo.missionList)
                {
                    missionType.Value.Clear();
                }

                foreach (var mission in CosmicHelper.SheetMissionDict)
                {
                    var Jobs = mission.Value.Jobs;
                    var territoryId = mission.Value.TerritoryId;
                    uint selectedJob = C.SelectedJob;
                    bool sinusEnabled = C.ShowSinusMissions;
                    bool phaennaEnabled = C.ShowPhaennaMissions;
                    bool oizysEnabled = C.ShowOizysMissions;

                    if (C.ShowCompletionWindow)
                    {
                        if (C.ShowCompletionOnlyJob)
                        {
                            if (!Jobs.Contains(selectedJob))
                                continue;
                        }
                    }

                    if (!sinusEnabled && territoryId == 1237)
                        continue;

                    if (!phaennaEnabled && territoryId == 1291)
                        continue;

                    if (!oizysEnabled && territoryId == 1310)
                        continue;

                    bool provisional = mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather)
                                    || mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed)
                                    || mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential);

                    if (provisional)
                    {
                        if (!C.GrindAllProvisionals)
                        {
                            if (!Jobs.Contains(selectedJob))
                                continue;
                        }

                        if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                            modeSelect_TableInfo.missionList["Weather"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                            modeSelect_TableInfo.missionList["Timed"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                            modeSelect_TableInfo.missionList["Sequence"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });

                        if (C.MissionConfig.ContainsKey(mission.Key) && C.MissionConfig[mission.Key].Enabled)
                        {
                            modeSelect_TableInfo.missionList["All Enabled"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        }
                    }
                    else
                    {
                        if (!Jobs.Contains(selectedJob))
                            continue;

                        if (mission.Value.Attributes.HasFlag(MissionAttributes.Critical))
                            modeSelect_TableInfo.missionList["Critical"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank > 3)
                            modeSelect_TableInfo.missionList["ARank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank == 3)
                            modeSelect_TableInfo.missionList["BRank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank == 2)
                            modeSelect_TableInfo.missionList["CRank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        else if (mission.Value.Rank == 1)
                            modeSelect_TableInfo.missionList["DRank"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });

                        if (C.MissionConfig.ContainsKey(mission.Key) && C.MissionConfig[mission.Key].Enabled)
                        {
                            modeSelect_TableInfo.missionList["All Enabled"].Add(new modeSelect_TableInfo.Mission { id = mission.Key, enabled = C.MissionConfig[mission.Key].Enabled });
                        }
                    }
                }

                int criticalEnabled = modeSelect_TableInfo.missionList.ContainsKey("Critical") ? modeSelect_TableInfo.missionList["Critical"].Count(mission => mission.enabled) : 0;
                int sequenceEnabled = modeSelect_TableInfo.missionList.ContainsKey("Sequence") ? modeSelect_TableInfo.missionList["Sequence"].Count(mission => mission.enabled) : 0;
                int weatherEnabled = modeSelect_TableInfo.missionList.ContainsKey("Weather") ? modeSelect_TableInfo.missionList["Weather"].Count(mission => mission.enabled) : 0;
                int timedEnabled = modeSelect_TableInfo.missionList.ContainsKey("Timed") ? modeSelect_TableInfo.missionList["Timed"].Count(mission => mission.enabled) : 0;
                int aRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("ARank") ? modeSelect_TableInfo.missionList["ARank"].Count(mission => mission.enabled) : 0;
                int bRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("BRank") ? modeSelect_TableInfo.missionList["BRank"].Count(mission => mission.enabled) : 0;
                int cRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("CRank") ? modeSelect_TableInfo.missionList["CRank"].Count(mission => mission.enabled) : 0;
                int dRankEnabled = modeSelect_TableInfo.missionList.ContainsKey("DRank") ? modeSelect_TableInfo.missionList["DRank"].Count(mission => mission.enabled) : 0;
                int allEnabled = modeSelect_TableInfo.missionList.ContainsKey("All Enabled") ? modeSelect_TableInfo.missionList["All Enabled"].Count(mission => mission.enabled) : 0;

                float scrollbarSize = ImGui.GetStyle().ScrollbarSize;
                float buttonRowHeight = (ImGui.GetTextLineHeight() + 8 * scale + 4 * scale) + scrollbarSize;

                using (var missionButtons = ImRaii.Child("##tab_scroll", new Vector2(0, buttonRowHeight), false, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    if (!missionButtons.Success)
                        return;

                    ImGui_Tools.DrawCategoryButton($"Critical [{criticalEnabled}]", "main_Critical");
                    ImGui_Tools.DrawCategoryButton($"Sequence [{sequenceEnabled}]", "main_Sequence");
                    ImGui_Tools.DrawCategoryButton($"Weather [{weatherEnabled}]", "main_Weather");
                    ImGui_Tools.DrawCategoryButton($"Timed [{timedEnabled}]", "main_Timed");
                    ImGui_Tools.DrawCategoryButton($"A Rank [{aRankEnabled}]", "main_ARank");
                    ImGui_Tools.DrawCategoryButton($"B Rank [{bRankEnabled}]", "main_BRank");
                    ImGui_Tools.DrawCategoryButton($"C Rank [{cRankEnabled}]", "main_CRank");
                    ImGui_Tools.DrawCategoryButton($"D Rank [{dRankEnabled}]", "main_DRank");
                    var selectedClass = C.SelectedJob;
                    var jobIcon = CosmicHelper.JobIconDict[selectedClass];
                    ImGui_Tools.DrawImageBox(jobIcon, "Selected", spacingAfter: 5);
                    if (allEnabled > 0)
                    {
                        ImGui_Tools.DrawCategoryButton($"All Enabled [{allEnabled}]", "main_AllEnabled");
                    }

                    ImGui_Tools.EndCategoryButtonRow();
                }

                if (C.ShowExtraMissionInfo)
                {
                    if (ImGui.BeginTable("Mission Info | Extra Details", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable, Vector2.Zero))
                    {
                        ImGui.TableSetupColumn("Mission Selection Viewer", ImGuiTableColumnFlags.WidthFixed, 200f);
                        ImGui.TableSetupColumn("Specific Mission Info", ImGuiTableColumnFlags.WidthStretch);

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        MissionTableInfo();

                        ImGui.TableNextColumn();
                        using (var missionInfoChild = ImRaii.Child("##modeSelect_MissionInfo", new Vector2(0, 0), false))
                        {
                            modeSelect_TableInfo.DrawMissionDetails();
                        }

                        ImGui.EndTable();
                    }
                }
                else
                {
                    MissionTableInfo();
                }
            }
        }

        private static void MissionTableInfo()
        {
            using (var missionTableChild = ImRaii.Child("##modeSelect_MissionTables", new Vector2(0, 0), false))
            {
                var enabledTabs = ImGui_Tools.CategoryStates;
                modeSelect_TableInfo.missionList["All Enabled"] = modeSelect_TableInfo.missionList["All Enabled"]
                    .OrderBy(x => {
                        var missionInfo = CosmicHelper.SheetMissionDict[x.id];
                        var attributes = missionInfo.Attributes;

                        // Determine which provisional type this mission is
                        if (attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                            return C.MissionPrio.IndexOf(ProvisionalTypes.ProvisionalWeather);
                        else if (attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                            return C.MissionPrio.IndexOf(ProvisionalTypes.ProvisionalSequential);
                        else if (attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                            return C.MissionPrio.IndexOf(ProvisionalTypes.ProvisionalTimed);
                        else
                            return int.MaxValue; // Non-provisional missions sort last
                    })
                    .ThenBy(x => {
                        var firstJob = CosmicHelper.SheetMissionDict[x.id].Jobs.First();
                        return C.JobPrio.IndexOf(firstJob);
                    })
                    .ThenByDescending(x => CosmicHelper.SheetMissionDict[x.id].Rank)
                    .ToList();

                modeSelect_TableInfo.missionList["Sequence"] = modeSelect_TableInfo.missionList["Sequence"]
                    .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                    .ToList();

                modeSelect_TableInfo.missionList["Weather"] = modeSelect_TableInfo.missionList["Weather"]
                    .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                    .ToList();

                modeSelect_TableInfo.missionList["Timed"] = modeSelect_TableInfo.missionList["Timed"]
                    .OrderBy(x => C.JobPrio.IndexOf(CosmicHelper.SheetMissionDict[x.id].Jobs.First()))
                    .ToList();

                if (enabledTabs["main_AllEnabled"])
                {
                    int allEnabled = modeSelect_TableInfo.missionList.ContainsKey("All Enabled") ? modeSelect_TableInfo.missionList["All Enabled"].Count(mission => mission.enabled) : 0;
                    if (allEnabled == 0)
                        enabledTabs["main_AllEnabled"] = false;

                    if (modeSelect_TableInfo.missionList["All Enabled"].Count > 0)
                    {
                        modeSelect_TableInfo.DrawMissionTablev2("All Enabled", "All_Enabled", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["All Enabled"]));
                    }
                    else
                    {
                        ImGui.Text("HEY. ENABLE SOME MISSIONS SO WE CAN DISPLAY SOMETHING HERE");
                    }
                }
                if (enabledTabs["main_Critical"])
                    modeSelect_TableInfo.DrawMissionTablev2("Critical", "Critical_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Critical"]));
                if (enabledTabs["main_Sequence"])
                    modeSelect_TableInfo.DrawMissionTablev2("Sequence", "Sequence_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Sequence"]));
                if (enabledTabs["main_Weather"])
                    modeSelect_TableInfo.DrawMissionTablev2("Weather", "Weather_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Weather"]));
                if (enabledTabs["main_Timed"])
                    modeSelect_TableInfo.DrawMissionTablev2("Timed", "Timed_Missions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["Timed"]));
                if (enabledTabs["main_ARank"])
                    modeSelect_TableInfo.DrawMissionTablev2("A Rank", "A_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["ARank"]));
                if (enabledTabs["main_BRank"])
                    modeSelect_TableInfo.DrawMissionTablev2("B Rank", "B_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["BRank"]));
                if (enabledTabs["main_CRank"])
                    modeSelect_TableInfo.DrawMissionTablev2("C Rank", "C_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["CRank"]));
                if (enabledTabs["main_DRank"])
                    modeSelect_TableInfo.DrawMissionTablev2("D Rank", "D_RankMissions", modeSelect_TableInfo.SortMissionList(modeSelect_TableInfo.missionList["DRank"]));
            }
        }
    }
}
