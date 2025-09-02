using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility.Table;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Enums;
using ICE.Utilities.Cosmic;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Xml.Schema;
using static ICE.Utilities.CosmicHelper;

namespace ICE.Ui
{
    internal class MainWindowV2 : Window
    {
        public MainWindowV2() :
#if DEBUG
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion} Debug build ###ICEMainWindowV2")
#else
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICEMainWindow")
#endif
        {
            Flags = ImGuiWindowFlags.None;

            // Set up size constraints to ensure window cannot be too small or too large.
            // Increased minimum size to better accommodate larger font sizes.
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(100, 100),
                MaximumSize = new Vector2(2000, 3000)
            };

            P.windowSystem.AddWindow(this);

            AllowPinning = true;
            AllowClickthrough = true;
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        // Available jobs and their IDs.
        // Matching up to the sheet `ClassJob` vs `ClassJobCategory` for future use (idk why that sheet even exist...)
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

        // Available mission ranks and their identifiers.
        private static List<(uint RankId, string RankName)> rankOptions = new()
        {
            (1, "D"),
            (2, "C"),
            (3, "B"),
            (4, "A")
        };

        private uint? currentJobId => PlayerHelper.GetClassJobId();
        private bool usingSupportedJob => jobOptions.Any(job => job.Id == currentJobId);
        private uint selectedJob = C.SelectedJob;

        // Left Column Settings
        private bool onlyGrabMission = C.OnlyGrabMission;
        private bool stopCosmic = C.StopOnceHitCosmoCredits;
        private int cosmicCap = C.CosmoCreditsCap;
        private bool stopLunar = C.StopOnceHitLunarCredits;
        private int lunarCap = C.LunarCreditsCap;
        private bool stopScore = C.StopOnceHitCosmicScore;
        private int scoreCap = C.CosmicScoreCap;
        private bool autoPickCurrentJob = C.AutoPickCurrentJob;
        private bool stopWhenLevel = C.StopWhenLevel;
        private int targetLevel = C.TargetLevel;

        private bool showCritical = C.showCritical;
        private bool showSequential = C.showSequential;
        private bool showWeather = C.showWeather;
        private bool showTimeRestricted = C.showTimeRestricted;
        private bool showClassA = C.showClassA;
        private bool showClassB = C.showClassB;
        private bool showClassC = C.showClassC;
        private bool showClassD = C.showClassD;

        // Middle Column stuff
        private Dictionary<string, bool> headerStates = new();
        private int SortOption = C.TableSortOption;
        private bool hideUnsupported = C.HideUnsupportedMissions;
        private bool showCredits = C.ShowCreditsColumn;
        private bool showExp = C.ShowExpColums;
        private bool showNotes = C.ShowNotes;
        private bool increaseMiddleColumn = C.IncreaseMiddleColumn;

        private bool showTableSetting = false;
        private string[] modes = ["Gold", "Silver", "Bronze", "Manual"];
        private bool[] selectedModes = [false, false, false, false];

        private string[] missionOptions = ["Current Class", "All Missions", "Currently Enabled"];
        private string selectedOption = "Current Class";

        private List<(uint Id, string SortOptionName, Func<IEnumerable<KeyValuePair<uint, MissionListInfo>>, IEnumerable<KeyValuePair<uint, MissionListInfo>>> SortFunc)> sortOptions = new()
        {
            (0, "", missions => missions),
            (1, "Name", missions => missions.OrderBy(x => x.Value.Name)),
            (2, "Mission ID", missions => missions),
            (3, "Cosmocredits", missions => missions.OrderByDescending(x => x.Value.CosmoCredit)),
            (4, "Lunar Credits", missions => missions.OrderByDescending(x => x.Value.LunarCredit)),
            (5, "Research I", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => CosmicHelper.ExpDictionary[exp.Type] == "I").FirstOrDefault().Amount)),
            (6, "Research II", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => CosmicHelper.ExpDictionary[exp.Type] == "II").FirstOrDefault().Amount)),
            (7, "Research III", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => CosmicHelper.ExpDictionary[exp.Type] == "III").FirstOrDefault().Amount)),
            (8, "Research IV", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => CosmicHelper.ExpDictionary[exp.Type] == "IV").FirstOrDefault().Amount)),
            (9, "Research V", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => CosmicHelper.ExpDictionary[exp.Type] == "V").FirstOrDefault().Amount)),
            (10, "Map Marker", missions => missions.OrderByDescending(x => x.Value.MarkerId)),
        };

        // Right Column stuff
        private uint selectedMission = 0;

        public override void Draw()
        {
            // Calculate scaling factors based on current font size
            float fontScale = ImGui.GetIO().FontGlobalScale;
            float textLineHeight = ImGui.GetTextLineHeight();
            float scaledSpacing = ImGui.GetStyle().ItemSpacing.Y * fontScale;
            float headerPadding = textLineHeight * 1.2f;

            float headerHeight = textLineHeight + headerPadding * 2;
            float contentAreaHeight = ImGui.GetWindowHeight() - headerHeight - 4;
            float labelHeight = ImGui.GetTextLineHeightWithSpacing();
            float childHeight = ImGui.GetContentRegionAvail().Y;

            // Setting up the columns to be 3 right here. 
            float leftPanelWidth = Math.Max(220, textLineHeight * 14);
            float middlePanelWidth = Math.Max(0, textLineHeight * 22);

            ImGui.Columns(3, "Main Window", false);
            // ----------------------------
            //  LEFT PANEL (Start/Stop, Class Selection, Filter Ui)
            // ----------------------------

            ImGui.SetColumnWidth(0, leftPanelWidth);

            if (ImGui.BeginChild("Filter Panel##Filter Panel", new Vector2(0, childHeight), true))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);

                using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !usingSupportedJob))
                {
                    if (ImGui.Button("Start", new Vector2(ImGui.GetContentRegionAvail().X, textLineHeight * 1.5f)))
                    {
                        SchedulerMain.EnablePlugin();
                    }
                }

                using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
                {
                    if (ImGui.Button("Stop", new Vector2(ImGui.GetContentRegionAvail().X, textLineHeight * 1.5f)))
                    {
                        SchedulerMain.DisablePlugin();
                    }
                }
                if (ImGui.Button("Settings", new Vector2(ImGui.GetContentRegionAvail().X, textLineHeight * 1.5f)))
                {
                    P.settingsWindowV2.IsOpen = !P.settingsWindowV2.IsOpen;
                }

                if (ImGui.Checkbox($"Only grab mission", ref onlyGrabMission))
                {
                    C.OnlyGrabMission = onlyGrabMission;
                    C.Save();
                }

                ImGui.PopStyleVar();

                ImGui.Spacing();

                ImGui.Separator();

                ImGui.Spacing();

                ImGui.Checkbox("Stop after current mission", ref SchedulerMain.StopBeforeGrab);
                if (ImGui.Checkbox($"Stop at Cosmic Credits", ref stopCosmic))
                {
                    C.StopOnceHitCosmoCredits = stopCosmic;
                    C.Save();
                }
                if (stopCosmic)
                {
                    ImGui.Indent(15);
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.SliderInt("##CosmicStop", ref cosmicCap, 0, 30000))
                    {
                        C.CosmoCreditsCap = cosmicCap;
                        C.Save();
                    }
                    ImGui.Unindent(15);
                }

                if (ImGui.Checkbox($"Stop at Lunar Credits", ref stopLunar))
                {
                    C.StopOnceHitLunarCredits = stopLunar;
                    C.Save();
                }
                if (stopLunar)
                {
                    ImGui.Indent(15);
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.SliderInt("##LunarStop", ref lunarCap, 0, 10000))
                    {
                        C.LunarCreditsCap = lunarCap;
                        C.Save();
                    }
                    ImGui.Unindent(15);
                }

                if (ImGui.Checkbox($"Stop at Cosmic Score", ref stopScore))
                {
                    C.StopOnceHitCosmicScore = stopScore;
                    C.Save();
                }
                if (stopScore)
                {
                    ImGui.Indent(15);
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputInt("###ScoreStop", ref scoreCap, 10000, 50000))
                    {
                        C.CosmicScoreCap = scoreCap >= 0 ? scoreCap : 0;
                        C.Save();
                    }
                    ImGui.Unindent(15);
                }

                if (ImGui.Checkbox($"Stop at Level", ref stopWhenLevel))
                {
                    C.StopWhenLevel = stopWhenLevel;
                    C.Save();
                }
                if (stopWhenLevel)
                {
                    ImGui.Indent(15);
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.SliderInt("##Level", ref targetLevel, 10, 100))
                    {
                        C.TargetLevel = targetLevel;
                        C.Save();
                    }
                    ImGui.Unindent(15);
                }

                ImGui.Spacing();

                ImGui.Separator();

                ImGui.Dummy(new(0, 10));

                bool EnableRelicXp = C.XPRelicGrind;
                if (ImGui.Checkbox("Auto-Pick For Relic XP", ref EnableRelicXp))
                {
                    C.XPRelicGrind = EnableRelicXp;
                    C.Save();
                }
                if (EnableRelicXp)
                {
                    bool IgnoreManual = C.XPRelicIgnoreManual;
                    if (ImGui.Checkbox("Ignore Manual Mode Missions", ref IgnoreManual))
                    {
                        C.XPRelicIgnoreManual = IgnoreManual;
                        C.Save();
                    }

                    bool OnlySelected = C.XPRelicOnlyEnabled;
                    if (ImGui.Checkbox("Only selected missions", ref OnlySelected))
                    {
                        C.XPRelicOnlyEnabled = OnlySelected;
                        C.Save();
                    }
                }

                ImGui.Spacing();

                ImGui.Separator();

                ImGui.Dummy(new(0, 10));
                if (ImGui.Checkbox("Auto Pick Current Job", ref autoPickCurrentJob))
                {
                    C.AutoPickCurrentJob = autoPickCurrentJob;
                    C.Save();
                }

                if (autoPickCurrentJob && usingSupportedJob)
                {
                    if (currentJobId != selectedJob)
                    {
                        selectedJob = currentJobId.Value;
                        C.SelectedJob = selectedJob;
                        C.Save();
                    }
                }

                ImGui.Dummy(new(0, 5));

                float iconSize = 32;
                float iconSpacing = 8;
                float availWidth = ImGui.GetContentRegionAvail().X;
                float startX = (availWidth - (iconSize + iconSpacing) * 4 + iconSpacing) * 0.5f;
                ImGui.SetCursorPosX(startX);

                // Row 1: CRP, BSM, ARM, GSM
                DrawJobSelection(8, "CRP");
                ImGui.SameLine(0, iconSpacing);
                DrawJobSelection(9, "BSM");
                ImGui.SameLine(0, iconSpacing);
                DrawJobSelection(10, "ARM");
                ImGui.SameLine(0, iconSpacing);
                DrawJobSelection(11, "GSM");

                // Row 2: LTW, WVR, ALC, CUL
                ImGui.SetCursorPosX(startX);

                DrawJobSelection(12, "LWT");
                ImGui.SameLine(0, iconSpacing);
                DrawJobSelection(13, "WVR");
                ImGui.SameLine(0, iconSpacing);
                DrawJobSelection(14, "ALC");
                ImGui.SameLine(0, iconSpacing);
                DrawJobSelection(15, "CUL");

                // Row 3: MIN, BTN, FSH
                ImGui.SetCursorPosX(startX);
                DrawJobSelection(16, "MIN");
                ImGui.SameLine(0, iconSpacing);
                DrawJobSelection(17, "BTN");
                ImGui.SameLine(0, iconSpacing);
                DrawJobSelection(18, "FSH");

                ImGui.Dummy(new Vector2(0, 5));

                ImGui.Separator();

                ImGui.Dummy(new Vector2(0, 5));

                ImGui.Text("Quick Mission Apply");

                ImGui.Dummy(new Vector2(0, 5));
                UpdateMissions();

                ImGui.Dummy(new Vector2(0, 5));

                ImGui.Separator();

                ImGui.Dummy(new Vector2(0, 5));

                RelicXP();
            }

            ImGui.EndChild();

            // ------------------------------------------ 
            //  MIDDLE PANEL: MISSION LISTING
            // ------------------------------------------
            ImGui.NextColumn();

            middlePanelWidth += 250;
            if (showCredits)
            {
                middlePanelWidth += Utils.cosmicLength;
                middlePanelWidth += Utils.lunarLength;
            }
            if (showExp)
            {
                middlePanelWidth += Utils.XPLength;
            }
            if (showNotes)
            {
                middlePanelWidth += Utils.MissionNotesLength + 100;
            }
            // Buffer room for the scrollbar
            middlePanelWidth += 100;
            ImGui.SetColumnWidth(1, middlePanelWidth);

            if (ImGui.BeginChild("##MissionList", new Vector2(0, childHeight), true))
            {

                if (ImGui.Checkbox("Hide Unsupported Missions", ref hideUnsupported))
                {
                    C.HideUnsupportedMissions = hideUnsupported;
                    C.Save();
                }

                ImGui.SameLine();

                if (ImGui.Button("Open Table Settings"))
                {
                    ImGui.OpenPopup("Open Table Settings");
                }

                if (ImGui.BeginPopup("Open Table Settings"))
                {
                    ImGui.Text("Toggle Configs");
                    ImGui.Separator();

                    if (ImGui.Checkbox("Show Credit Column", ref showCredits))
                    {
                        C.ShowCreditsColumn = showCredits;
                        C.Save();
                    }
                    if (ImGui.Checkbox("Show XP Amounts", ref showExp))
                    {
                        C.ShowExpColums = showExp;
                        C.Save();
                    }
                    if (ImGui.Checkbox("Show Notes", ref showNotes))
                    {
                        C.ShowNotes = showNotes;
                        C.Save();
                    }
                    if (ImGui.Checkbox("Increase Middle Column Size", ref increaseMiddleColumn))
                    {
                        C.IncreaseMiddleColumn = increaseMiddleColumn;
                        C.Save();
                    }

                    ImGui.EndPopup();
                }

                ImGui.SameLine();

                ImGui.SetNextItemWidth(150);
                if (ImGui.BeginCombo("Sort By", sortOptions[SortOption].SortOptionName))
                {
                    for (int i = 0; i < sortOptions.Count; i++)
                    {
                        bool isSelected = (i == SortOption);
                        if (ImGui.Selectable(sortOptions[i].SortOptionName, isSelected))
                        {
                            SortOption = i;
                        }
                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                        if (SortOption != C.TableSortOption)
                        {
                            C.TableSortOption = SortOption;
                            C.Save();
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.Dummy(new Vector2(0, 5));

                ImGui.Separator();

                ImGui.Dummy(new Vector2(0, 5));

                // Mission Dropdown Sorting + Dropdowns themselves

                #region Mission Dropdowns

                IEnumerable<KeyValuePair<uint, MissionListInfo>> criticalMissions =
                    MissionInfoDict
                .Where(m => m.Value.JobId == selectedJob)
                .Where(m => m.Value.Attributes.HasFlag(MissionAttributes.Critical));
                criticalMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(criticalMissions);
                bool criticalGather = criticalMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));

                IEnumerable<KeyValuePair<uint, MissionListInfo>> weatherRestrictedMissions =
                        MissionInfoDict
                            .Where(m => m.Value.JobId == selectedJob || m.Value.JobId2 == selectedJob)
                            .Where(m => m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                            .Where(m => !m.Value.Attributes.HasFlag(MissionAttributes.Critical));
                bool weatherGather = weatherRestrictedMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
                weatherRestrictedMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(weatherRestrictedMissions);

                IEnumerable<KeyValuePair<uint, MissionListInfo>> timeRestrictedMissions =
                        MissionInfoDict
                            .Where(m => m.Value.JobId == selectedJob)
                            .Where(m => m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed));
                bool timeGather = timeRestrictedMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
                timeRestrictedMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(timeRestrictedMissions);

                IEnumerable<KeyValuePair<uint, MissionListInfo>> sequentialMissions =
                        MissionInfoDict
                            .Where(m => m.Value.JobId == selectedJob)
                            .Where(m => m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential));
                bool sequentialGather = sequentialMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
                sequentialMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(sequentialMissions);


                if (showCritical)
                {
                    DrawCollapsibleHeader($"Critical Missions", $"Critical Missions - {criticalMissions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} Enabled");
                    if (headerStates.TryGetValue("Critical Missions", out var isOpen) && isOpen)
                    {
                        MissionInfo("Critical Missions", criticalMissions, criticalGather);
                    }
                }

                if (showSequential)
                {
                    DrawCollapsibleHeader($"Sequential Missions", $"Sequential Missions - {sequentialMissions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} Enabled");
                    if (headerStates.TryGetValue("Sequential Missions", out var isOpen) && isOpen)
                    {
                        MissionInfo("Sequential Missions", sequentialMissions, sequentialGather);
                    }
                }

                if (showWeather)
                {
                    DrawCollapsibleHeader($"Weather Missions", $"Weather Missions - {weatherRestrictedMissions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} Enabled");
                    if (headerStates.TryGetValue("Weather Missions", out var isOpen) && isOpen)
                    {
                        MissionInfo("Weather Missions", weatherRestrictedMissions, weatherGather);
                    }
                }

                if (showTimeRestricted)
                {
                    DrawCollapsibleHeader($"Time-Restricted Missions", $"Time-Restricted Missions - {timeRestrictedMissions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} Enabled");
                    if (headerStates.TryGetValue("Time-Restricted Missions", out var isOpen) && isOpen)
                    {
                        MissionInfo("Sequential Missions", timeRestrictedMissions, timeGather);
                    }
                }

                foreach (var rank in rankOptions.OrderBy(r => r.RankName))
                {
                    IEnumerable<KeyValuePair<uint, MissionListInfo>> missions =
                        MissionInfoDict
                            .Where(m => m.Value.JobId == selectedJob || m.Value.JobId2 == selectedJob)
                            .Where(m => (m.Value.Rank == rank.RankId) || (rank.RankName == "A" && ARankIds.Contains(m.Value.Rank)))
                            .Where(m => !m.Value.Attributes.HasFlag(MissionAttributes.Critical))
                            .Where(m => !m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                            .Where(m => !m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                            .Where(m => !m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather));
                    missions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(missions);

                    bool missionGather = missions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
                    DrawCollapsibleHeader($"Class {rank.RankName}", $"Class {rank.RankName} - {missions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} enabled");
                    if (headerStates.TryGetValue($"Class {rank.RankName}", out var isOpen) && isOpen)
                    {
                        MissionInfo($"Class {rank.RankName} Missions", missions, missionGather);
                    }
                }

                #endregion
            }

            ImGui.EndChild();

            // ------------------------------------------
            // RIGHT PANEL: MISSION INFO
            // ------------------------------------------
            ImGui.NextColumn();

            if (ImGui.BeginChild("###MissionDetailPanel", new Vector2(0, childHeight), true))
            {
                Kofi.DrawRight();
                ImGui.NewLine();

                if (selectedMission != 0)
                {
                    ImGui.Text($"Mission Info (More Detailed)");
                    ImGui.Separator();

                    var mission = MissionInfoDict[selectedMission];

                    var MissionInfo = new List<(string Label, string Value)>
                    {
                        ("ID:", $"{selectedMission}"),
                        ("Mission Name:", mission.Name),
                        ("Cosmocredits:", mission.CosmoCredit.ToString()),
                        ("Lunar Credits", mission.LunarCredit.ToString()),
                        ("Silver Requirements:", mission.SilverRequirement.ToString()),
                        ("Gold Requirements:", mission.GoldRequirement.ToString())
                    };

                    float infoSize1 = MissionInfo.Max(row => ImGui.CalcTextSize(row.Label).X) + 10;
                    float infoSize2 = MissionInfo.Max(row => ImGui.CalcTextSize(row.Value).X) + 10;

                    if (ImGui.BeginTable("Detail##DetailPanelTable", 2))
                    {
                        ImGui.TableSetupColumn("##Label", ImGuiTableColumnFlags.WidthFixed, infoSize1);
                        ImGui.TableSetupColumn("##Value", ImGuiTableColumnFlags.WidthFixed, infoSize2);

                        foreach (var row in MissionInfo)
                        {
                            ImGui.TableNextRow();

                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text(row.Label);

                            ImGui.TableSetColumnIndex(1);
                            ImGui.Text(row.Value);
                        }

                        // used as a dummy spacer because don't wanna make a whole new table / CBA
                        ImGui.TableNextRow();

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text($"Tool XP Reward");

                        for (int i = mission.ExperienceRewards.Count - 1; i >= 0; i--)
                        {
                            var row = mission.ExperienceRewards[i];
                            if (row.Amount != 0)
                            {
                                ImGui.TableNextRow();

                                ImGui.TableSetColumnIndex(0);
                                string type = "";
                                if (row.Type == 1)
                                    type = "I";
                                else if (row.Type == 2)
                                    type = "II";
                                else if (row.Type == 3)
                                    type = "III";
                                else if (row.Type == 4)
                                    type = "IV";
                                else if (row.Type == 5)
                                    type = "V";
                                ImGui.Text($"Lv {type}:");

                                ImGui.TableSetColumnIndex(1);
                                ImGui.Text($"{row.Amount}");
                            }
                        }

                        ImGui.EndTable();

                        ImGui.Dummy(new Vector2(0, 5));

                        ImGui.Separator();

                        ImGui.Dummy(new Vector2(0, 5));

                        MissionAttributes flags = mission.Attributes;
                        var activeFlags = Enum.GetValues(typeof(MissionAttributes))
                                              .Cast<MissionAttributes>()
                                              .Where(f => f != MissionAttributes.None && flags.HasFlag(f))
                                              .ToList();

                        var entry = C.Missions.Where(e => e.Id == selectedMission);

                        ImGui.Text("Notes:");
                        bool hasPreviousNotes = false;
                        if (mission.Weather != CosmicWeather.FairSkies)
                        {
                            hasPreviousNotes = true;

                            ImGui.TextWrapped(mission.Weather.ToString());
                        }
                        else if (mission.Time != 0)
                        {
                            hasPreviousNotes = true;

                            ImGui.TextWrapped($"{2 * (mission.Time - 1)}:00 - {2 * (mission.Time)}:00");
                        }
                        else if (mission.PreviousMissionID != 0)
                        {
                            hasPreviousNotes = true;

                            var (Id, Name) = MissionInfoDict.Where(m => m.Key == mission.PreviousMissionID).Select(m => (Id: m.Key, Name: m.Value.Name)).FirstOrDefault();
                            ImGui.TextWrapped($"[{Id}] {Name}");
                        }
                        if (mission.JobId2 != 0)
                        {
                            if (hasPreviousNotes) ImGui.SameLine();
                            ImGui.TextWrapped($"{jobOptions.Find(job => job.Id == mission.JobId).Name}/{jobOptions.Find(job => job.Id == mission.JobId2).Name}");
                        }

                        if (mission.Attributes.HasFlag(MissionAttributes.Gather))
                        {
                            ImGui.Dummy(new Vector2(0, 5));

                            ImGui.Separator();

                            ImGui.Dummy(new Vector2(0, 5));

                            bool craftMission = mission.Attributes.HasFlag(MissionAttributes.Craft);

                            bool LimitedQuant = mission.Attributes.HasFlag(MissionAttributes.Limited);
                            // Gather X Amount is just "Gather" 
                            bool TimedMission = mission.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining);
                            bool ChainedMission = mission.Attributes.HasFlag(MissionAttributes.ScoreChains);
                            bool BoonMission = mission.Attributes.HasFlag(MissionAttributes.ScoreGatherersBoon);
                            bool collectableMission = mission.Attributes.HasFlag(MissionAttributes.Collectables);
                            bool stellerReductionMission = mission.Attributes.HasFlag(MissionAttributes.ReducedItems);

                            bool GatherX = !stellerReductionMission && !collectableMission && !BoonMission && !ChainedMission && !TimedMission && !LimitedQuant;

                            string MissionType = "";
                            if (craftMission)
                            {
                                MissionType = "Dual Class Mission";
                            }
                            else if (LimitedQuant)
                            {
                                MissionType = "Limited Quantity/Nodes";
                            }
                            else if (TimedMission)
                                MissionType = "Timed Scoring/Time Attack";
                            else if (ChainedMission && !BoonMission)
                                MissionType = "Chained Gather Scoring";
                            else if (BoonMission && !ChainedMission)
                                MissionType = "Gatherer's Boon Scoring";
                            else if (BoonMission && ChainedMission)
                                MissionType = "Chained + Gatherer's Boon Scoring";
                            else if (collectableMission && !stellerReductionMission)
                                MissionType = "Collectable Scoring";
                            else if (stellerReductionMission)
                                MissionType = "Steller Reduction/Collectables";
                            else if (GatherX)
                                MissionType = "Gather X Amount of Items";

                            ImGui.Text("Mission Type: " + MissionType);
                        }
#if DEBUG
                        ImGui.Dummy(new(0, 10));
                        ImGui.Text($"Debug Section");
                        ImGui.Spacing();

                        ImGui.Text($"[Debug] Nodeset: {mission.NodeSet}");

                        ImGui.Text($"[Debug] Active Mission Flags:");
                        foreach (var flag in activeFlags)
                        {
                            ImGui.Text($"{flag}");
                        }
#endif
                    }
                }
            }
            ImGui.EndChild();

        }

        public void DrawJobSelection(uint jobId, string tooltip)
        {
            bool state = selectedJob == jobId;
            ISharedImmediateTexture? icon = state ? CosmicHelper.JobIconDict[jobId] : CosmicHelper.GreyTexture[jobId];

            // Slight padding around the button
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));

            int styleCount = 1;
            int colorCount = 0;

            if (state)
            {
                // Dalamud theme
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.35f, 0.7f));
                ImGui.PushStyleColor(ImGuiCol.Border, ImGuiColors.ParsedGold);
                colorCount = 2;

                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.0f);
                styleCount++;
            }
            else
            {
                // Disabled job with Dalamud theme
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 0.1f));
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.4f, 0.4f, 0.4f, 0.5f));
                colorCount = 2;
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.5f);
                styleCount++;
            }

            // Rounded corners
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2.0f);
            styleCount++;

            Vector2 size = new Vector2(26, 26);
            float zoomFactor = 0.25f; // 25% zoom-in
            float cropAmount = zoomFactor / 2; // Crop equally from all sides

            Vector2 uv0 = state ? new Vector2(0, 0) : new Vector2(cropAmount, cropAmount);
            Vector2 uv1 = state ? new Vector2(1, 1) : new Vector2(1 - cropAmount, 1 - cropAmount);


            if (ImGui.ImageButton(icon.GetWrapOrEmpty().Handle, size, uv0, uv1))
            {
                if (autoPickCurrentJob)
                {
                    autoPickCurrentJob = false;
                    C.AutoPickCurrentJob = autoPickCurrentJob;
                }
                C.SelectedJob = jobId;
                selectedJob = jobId;
                C.Save();
            }

            // Pop style variables and colors
            ImGui.PopStyleVar(styleCount);
            if (colorCount > 0)
            {
                ImGui.PopStyleColor(colorCount);
            }

            // Show tooltip on hover
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"{tooltip}");
                ImGui.EndTooltip();
            }
        }

        private void DrawCollapsibleHeader(string id, string label, float spacing = 4f)
        {
            var drawList = ImGui.GetWindowDrawList();
            var cursorPos = ImGui.GetCursorScreenPos();
            var windowWidth = ImGui.GetContentRegionAvail().X;

            var padding = 6.0f;
            var textSize = ImGui.CalcTextSize(label);
            var bgHeight = textSize.Y + padding * 2;

            if (!headerStates.ContainsKey(id))
                headerStates[id] = false;

            var headerRectMin = cursorPos;
            var headerRectMax = new Vector2(cursorPos.X + windowWidth, cursorPos.Y + bgHeight);

            // Draw background
            drawList.AddRectFilled(headerRectMin, headerRectMax, ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1f)), 2f);
            drawList.AddRect(headerRectMin, headerRectMax, ImGui.GetColorU32(ImGuiColors.ParsedGold), 2f);

            // Draw centered label text
            var textPos = new Vector2(
                cursorPos.X + (windowWidth - textSize.X) * 0.5f,
                cursorPos.Y + padding
            );
            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), label);

            // Register invisible button for interaction using a unique ID
            ImGui.SetCursorScreenPos(cursorPos);
            ImGui.PushID(id); // Use internal ID
            ImGui.InvisibleButton("##header", new Vector2(windowWidth, bgHeight));
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                headerStates[id] = !headerStates[id];
            ImGui.PopID();

            ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + bgHeight + spacing));
        }

        private void MissionInfo(string tableName, IEnumerable<KeyValuePair<uint, MissionListInfo>> missions, bool showGatherConfig = false)
        {
            int columnAmount = 4;
            if (showCredits)
                columnAmount += 2;
            if (showExp)
                columnAmount += 5;
            if (showGatherConfig)
                columnAmount += 1;
            if (showNotes)
                columnAmount += 1;

            if (ImGui.BeginTable($"MissionList###{tableName}", columnAmount, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
            {
                // Push font to get icon width
                ImGui.PushFont(UiBuilder.IconFont);
                float iconWidth = ImGui.CalcTextSize(FontAwesomeIcon.Flag.ToIconString()).X;
                ImGui.PopFont();

                // Extra buffer (empirically determined to prevent clipping)
                float iconBuffer = iconWidth + 10f;

                float maxMissionNameWidth = 0f;
                foreach (var entry in missions)
                {
                    float nameWidth = ImGui.CalcTextSize(entry.Value.Name).X;
                    bool hasFlag = MissionInfoDict[entry.Key].MarkerId != 0;
                    float totalWidth = nameWidth + (hasFlag ? iconBuffer : 0f);
                    if (totalWidth > maxMissionNameWidth)
                        maxMissionNameWidth = totalWidth;
                }

                float col3Width = maxMissionNameWidth + 20f; // 20f buffer for spacing

                float col1Width = ImGui.CalcTextSize("Enabled").X + 10f;  // Add buffer
                float col2Width = ImGui.CalcTextSize("ID").X + 10f;       // Add buffer
                float col4Width = ImGui.CalcTextSize("Cosmo").X + 5f;
                float col5Width = ImGui.CalcTextSize("Lunar").X + 5f;
                float colXPWidth = ImGui.CalcTextSize("III").X + 5f;

                // Updating the column lengths based on the text size
                Utils.enableColumnLength = col1Width;
                Utils.IDLength = col2Width;
                Utils.cosmicLength = col4Width;
                Utils.lunarLength = col5Width;
                Utils.XPLength = colXPWidth * 5f;

                ImGui.TableSetupColumn("###EnableCheckbox", ImGuiTableColumnFlags.WidthFixed, col1Width);
                ImGui.TableSetupColumn("###MissionIDs", ImGuiTableColumnFlags.WidthFixed, col2Width);
                ImGui.TableSetupColumn("Mission Name", ImGuiTableColumnFlags.WidthFixed, maxMissionNameWidth);
                if (showCredits)
                {
                    ImGui.TableSetupColumn("###Cosmo", ImGuiTableColumnFlags.WidthFixed, col4Width);
                    ImGui.TableSetupColumn("###Lunar", ImGuiTableColumnFlags.WidthFixed, col5Width);
                }
                IOrderedEnumerable<KeyValuePair<int, string>> orderedExp = ExpDictionary.ToList().OrderBy(exp => exp.Key);
                if (showExp)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        ImGui.TableSetupColumn($"###XPColumn{i}", ImGuiTableColumnFlags.WidthFixed, colXPWidth);
                    }
                }
                // Settings column
                ImGui.TableSetupColumn("Turn In", ImGuiTableColumnFlags.WidthFixed, 100);

                if (showGatherConfig)
                {
                    float columnWidth = ImGui.CalcTextSize("Gather Config").X + 5;
                    ImGui.TableSetupColumn("Gather Config", ImGuiTableColumnFlags.WidthFixed, columnWidth);
                }

                if (showNotes)
                {
                    ImGui.TableSetupColumn("Mission Notes", ImGuiTableColumnFlags.WidthStretch);
                }

                // Render the header row (static headers get drawn here)
                ImGui.TableHeadersRow();

                // Setup to make sure the labels are centered
                ImGui.TableSetColumnIndex(0);
                CenterText("Enabled");

                ImGui.TableNextColumn();
                CenterText("ID");

                ImGui.TableSetColumnIndex(2);
                if (showCredits)
                {
                    ImGui.TableNextColumn();
                    CenterText("Cosmo");

                    ImGui.TableNextColumn();
                    CenterText("Lunar");
                }
                if (showExp)
                {
                    ImGui.TableNextColumn();
                    CenterText("I");

                    ImGui.TableNextColumn();
                    CenterText("II");

                    ImGui.TableNextColumn();
                    CenterText("III");
                    
                    ImGui.TableNextColumn();
                    CenterText("IV");

                    ImGui.TableNextColumn();
                    CenterText("V");
                }

                // Actual table entries now
                foreach (var entry in missions)
                {
                    bool unsupported = UnsupportedMissions.Ids.Contains(entry.Key);

                    bool craftMission = entry.Value.Attributes.HasFlag(MissionAttributes.Craft);
                    bool gatherMission = entry.Value.Attributes.HasFlag(MissionAttributes.Gather);
                    bool fishMission = entry.Value.Attributes.HasFlag(MissionAttributes.Fish);
                    bool collectableMission = entry.Value.Attributes.HasFlag(MissionAttributes.Collectables);
                    bool stellerReductionMission = entry.Value.Attributes.HasFlag(MissionAttributes.ReducedItems);

                    bool dualclass = craftMission && (gatherMission || fishMission);

                    if (fishMission || (gatherMission && (collectableMission || stellerReductionMission)) || (gatherMission && entry.Value.NodeSet == 0))
                    {
                        unsupported = true;
                    }

                    if (unsupported && hideUnsupported)
                        continue;

                    ImGui.TableNextRow();

                    var mission = C.Missions.Single(x => x.Id == entry.Key);
                    var isEnabled = mission.Enabled;

                    Utils.missionLength = Math.Max(Utils.missionLength, ImGui.CalcTextSize(mission.Name).X);

                    // Column 0: Enable checkbox
                    ImGui.TableSetColumnIndex(0);
                    // Estimate the width of the checkbox (label is invisible, so the box is all that matters)
                    float cellWidth = ImGui.GetContentRegionAvail().X;
                    float checkboxWidth = ImGui.GetFrameHeight(); // Width of the square checkbox only
                    float offset = (cellWidth - checkboxWidth) * 0.5f;

                    if (offset > 0f)
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

                    // Use an invisible label for the checkbox to avoid text spacing
                    if (ImGui.Checkbox($"###{entry.Value.Name}_{entry.Key}", ref isEnabled))
                    {
                        mission.Enabled = isEnabled;
                        CosmicMission chain;

                        if (isEnabled)
                        {
                            var prevChainList = GetOnlyPreviousMissionsRecursive(mission.Id);
                            foreach (var missionId in prevChainList)
                            {
                                chain = C.Missions.Single(x => x.Id == missionId);
                                chain.Enabled = isEnabled;
                            }
                        }
                        else
                        {
                            var nextChainList = GetOnlyNextMissionsRecursive(mission.Id);
                            foreach (var missionId in nextChainList)
                            {
                                chain = C.Missions.Single(x => x.Id == missionId);
                                chain.Enabled = isEnabled;
                            }
                        }

                        C.Save();
                    }
                    if (ImGui.IsItemClicked())
                    {
                        selectedMission = entry.Key;
                    }

                    // Column 1: Mission ID
                    ImGui.TableNextColumn();
                    string MissionId = entry.Key.ToString();
                    col2Width = Math.Max(ImGui.CalcTextSize(MissionId).X + 10, col2Width);
                    CenterTextInTableCell($"{MissionId}");

                    // Column 2: Mission Name
                    ImGui.TableNextColumn();
                    string MissionName = entry.Value.Name;
                    if (unsupported)
                    {
                        ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), MissionName);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Currently can only be done in manual mode");
                        }
                    }
                    else
                    {
                        ImGui.Text($"{MissionName}");
                    }
                    if (ImGui.IsItemClicked())
                    {
                        selectedMission = entry.Key;
                    }

                    MissionListInfo info = MissionInfoDict[mission.Id];
                    if (info.MarkerId != 0)
                    {
                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Text(FontAwesomeIcon.Flag.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemClicked())
                            Utils.SetGatheringRing(info.TerritoryId, info.X, info.Y, info.Radius, info.Name);
                    }

                    // Column 3: Credits
                    if (showCredits)
                    {
                        ImGui.TableNextColumn();
                        CenterTextInTableCell(entry.Value.CosmoCredit.ToString());
                        ImGui.TableNextColumn();
                        CenterTextInTableCell(entry.Value.LunarCredit.ToString());
                    }

                    // Col 4-7
                    if (showExp)
                    {
                        foreach (var expType in orderedExp)
                        {
                            ImGui.TableNextColumn();
                            var relicXp = entry.Value.ExperienceRewards.Where(exp => exp.Type == expType.Key).FirstOrDefault().Amount.ToString();
                            if (relicXp == "0")
                            {
                                relicXp = "-";
                            }
                            CenterTextInTableCell(relicXp);
                        }
                    }

                    // Col 8 Turnin Settings
                    ImGui.TableNextColumn();
                    string[] modes;
                    bool[] selectedModes;
                    if (unsupported)
                    {
                        modes = ["Manual"];
                        selectedModes = [mission.ManualMode];
                    }
                    else if (entry.Value.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining) || entry.Value.Attributes.HasFlag(MissionAttributes.Critical))
                    {
                        modes = ["ASAP", "Manual"];
                        selectedModes = [mission.TurnInASAP, mission.ManualMode];
                    }
                    else
                    {
                        modes = ["Gold", "Silver", "Bronze", "Manual"];
                        selectedModes =
                        [
                            mission.TurnInGold,
                                mission.TurnInSilver,
                                mission.TurnInASAP,
                                mission.ManualMode
                        ];
                    }

                    ImGui.SetNextItemWidth(-1);
                    bool changed = false;
                    if (ImGui.BeginCombo($"###{entry.Value.Name}_{entry.Key}_turninMode", string.Join(", ", modes.Where((m, i) => selectedModes[i]))))
                    {
                        for (int i = 0; i < modes.Length; i++)
                        {
                            bool selected = selectedModes[i];
                            if (ImGui.Selectable(modes[i], selected, ImGuiSelectableFlags.DontClosePopups))
                            {
                                selectedModes[i] = !selected;
                                changed = true;
                            }
                        }
                        ImGui.EndCombo();
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Turnin's Enabled:");
                        for (int i = 0; i < selectedModes.Length; i++)
                        {
                            if (selectedModes[i] == true)
                                ImGui.Text($"{modes[i]}");
                        }
                        ImGui.EndTooltip();
                    }
                    if (changed)
                    {
                        if (unsupported)
                        {
                            mission.ManualMode = true;
                        }
                        else if (entry.Value.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining) || entry.Value.Attributes.HasFlag(MissionAttributes.Critical))
                        {
                            mission.TurnInASAP = true;
                            mission.ManualMode = selectedModes[1];
                        }
                        else
                        {
                            mission.TurnInGold = selectedModes[0];
                            mission.TurnInSilver = selectedModes[1];
                            mission.TurnInASAP = selectedModes[2];
                            mission.ManualMode = selectedModes[3];
                        }
                        C.Save();
                    }

                    // Column #9
                    if (showGatherConfig)
                    {
                        ImGui.TableNextColumn();
                        if (GatheringJobList.Contains((int)entry.Value.JobId) || GatheringJobList.Contains((int)entry.Value.JobId2))
                        {
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.BeginCombo($"###GatherProfile{entry.Value.Name}_{entry.Key}", mission.GatherSetting.Name))
                            {
                                foreach (var profile in C.GatherSettings)
                                {
                                    bool isSelected = mission.GatherSettingId == profile.Id;
                                    if (ImGui.Selectable(profile.Name, isSelected))
                                    {
                                        mission.GatherSettingId = profile.Id;
                                    }
                                    if (isSelected)
                                        ImGui.SetItemDefaultFocus();
                                }
                                ImGui.EndCombo();
                            }
                        }
                    }

                    if (showNotes)
                    {
                        ImGui.TableNextColumn();
                        bool hasPreviousNotes = false;
                        string notes = "";
                        if (entry.Value.Weather != CosmicWeather.FairSkies)
                        {
                            hasPreviousNotes = true;
                            notes = entry.Value.Weather.ToString();
                        }
                        else if (entry.Value.Time != 0)
                        {
                            hasPreviousNotes = true;
                            notes = $"{2 * (entry.Value.Time - 1)}:00 - {2 * (entry.Value.Time)}:00";
                        }
                        else if (entry.Value.PreviousMissionID != 0)
                        {
                            hasPreviousNotes = true;
                            var (Id, Name) = MissionInfoDict.Where(m => m.Key == entry.Value.PreviousMissionID).Select(m => (Id: m.Key, Name: m.Value.Name)).FirstOrDefault();
                            notes = $"[{Id}] {Name}";
                        }
                        if (entry.Value.JobId2 != 0)
                        {
                            if (hasPreviousNotes) ImGui.SameLine();
                            notes += $"{jobOptions.Find(job => job.Id == entry.Value.JobId).Name}/{jobOptions.Find(job => job.Id == entry.Value.JobId2).Name}";
                        }
                        float NotesLength = Math.Max(Utils.missionLength, ImGui.CalcTextSize(notes).X);
                        Utils.MissionNotesLength = NotesLength;
                        ImGui.TextWrapped(notes);
                    }
                }

                ImGui.EndTable();
            }
        }

        private void UpdateMissions()
        {
            ImGui.SetNextItemWidth(100);
            if (ImGui.Button("Select Modes"))
            {
                ImGui.OpenPopup("Select Mission Profiles");
            }

            if (ImGui.BeginPopup("Select Mission Profiles"))
            {
                ImGui.Checkbox($"Gold", ref selectedModes[0]);
                ImGui.Checkbox($"Silver", ref selectedModes[1]);
                ImGui.Checkbox($"Bronze/ASAP", ref selectedModes[2]);
                ImGui.Checkbox($"Manual", ref selectedModes[3]);

                ImGui.EndPopup();
            }

            if (ImGui.BeginCombo("###ProfileSelector", selectedOption))
            {
                for (int i = 0; i < missionOptions.Length; i++)
                {
                    bool selected = selectedModes[i];
                    if (ImGui.Selectable(missionOptions[i], selected))
                    {
                        selectedOption = missionOptions[i];
                    }
                }
                ImGui.EndCombo();
            }

            if (ImGui.Button("Apply to selected profiles"))
            {
                var currentJob = PlayerHelper.GetClassJobId();

                foreach (var mission in C.Missions)
                {
                    var id = mission.Id;

                    bool unsupported = UnsupportedMissions.Ids.Contains(id);

                    var missionDict = MissionInfoDict[id];

                    bool craftMission = missionDict.Attributes.HasFlag(MissionAttributes.Craft);
                    bool gatherMission = missionDict.Attributes.HasFlag(MissionAttributes.Gather);
                    bool fishMission = missionDict.Attributes.HasFlag(MissionAttributes.Fish);
                    bool collectableMission = missionDict.Attributes.HasFlag(MissionAttributes.Collectables);
                    bool stellerReductionMission = missionDict.Attributes.HasFlag(MissionAttributes.ReducedItems);
                    bool TimedMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining);

                    bool dualclass = craftMission && (gatherMission || fishMission);

                    if (dualclass || fishMission || (gatherMission && (collectableMission || stellerReductionMission)) || (gatherMission && missionDict.NodeSet == 0))
                    {
                        unsupported = true;
                    }

                    // "Current Class", "All Missions", "Currently Enabled"
                    void UpdateMissions()
                    {
                        if (TimedMission)
                        {
                            if (!selectedModes[2] && !selectedModes[3])
                            {
                                mission.TurnInASAP = true;
                                mission.ManualMode = false;

                            }
                            else
                            {
                                mission.TurnInGold = false;
                                mission.TurnInSilver = false;
                                mission.TurnInASAP = selectedModes[2];
                                mission.ManualMode = selectedModes[3];
                            }
                        }
                        else if (unsupported)
                        {
                            mission.TurnInGold = false;
                            mission.TurnInSilver = false;
                            mission.TurnInASAP = false;
                            mission.ManualMode = true;
                        }
                        else
                        {
                            // should be the catch all for all missions
                            mission.TurnInGold = selectedModes[0];
                            mission.TurnInSilver = selectedModes[1];
                            mission.TurnInASAP = selectedModes[2];
                            mission.ManualMode = selectedModes[3];
                        }
                    }


                    if (selectedOption == missionOptions[0])
                    {
                        if (missionDict.JobId == currentJob)
                        {
                            UpdateMissions();
                        }
                        else
                            continue;
                    }
                    else if (selectedOption == missionOptions[1])
                    {
                        UpdateMissions();
                    }
                    else if (selectedOption == missionOptions[2])
                    {
                        if (mission.Enabled)
                        {
                            UpdateMissions();
                        }
                        else
                            continue;
                    }
                }

                C.Save();
            }
        }

        private float current = 25f;

        private class XPType
        {
            public uint CurrentXP { get; set; }
            public uint NeededXP { get; set; }
            public uint MaxXP { get; set; }
        }

        private unsafe void RelicXP()
        {
            var wksManager = WKSManager.Instance();
            if (wksManager == null || wksManager->ResearchModule == null || !wksManager->ResearchModule->IsLoaded)
                return;

            var job = selectedJob;
            var toolClassId = (byte)(job - 7);
            var stage = wksManager->ResearchModule->CurrentStages[toolClassId - 1];
            var nextstate = wksManager->ResearchModule->UnlockedStages[toolClassId - 1];

            // Unsure... why this is here? 
            if (Svc.Data.GetExcelSheet<WKSCosmoToolClass>().TryGetRow(toolClassId, out var row))
            {

            }

            Dictionary<uint, XPType> XPTable = new Dictionary<uint, XPType>();

            for (byte type = 1; type <= 5; type++)
            {
                if (!wksManager->ResearchModule->IsTypeAvailable(toolClassId, type))
                    break;

                var neededXP = wksManager->ResearchModule->GetNeededAnalysis(toolClassId, type);

                var maxXP = wksManager->ResearchModule->GetMaxAnalysis(toolClassId, type);

                var currentXp = wksManager->ResearchModule->GetCurrentAnalysis(toolClassId, type);
                if (!XPTable.ContainsKey(type))
                {
                    XPTable[type] = new XPType()
                    {
                        CurrentXP = currentXp,
                        NeededXP = neededXP,
                        MaxXP = maxXP,
                    };
                }
            }

            ImGui.Text($"Stage: {stage}");

            if (stage != 9)
            {
                foreach (var type in XPTable)
                {
                    uint current = type.Value.CurrentXP;
                    uint needed = type.Value.NeededXP;
                    uint max = type.Value.MaxXP;
                    float windowSize = ImGui.GetWindowSize().X - 20;
                    Vector2 size = new Vector2(windowSize, 10);

                    string overlay = $"ID: {current} / {max}";
                    string xpType = "";
                    if (type.Key == 1)
                        xpType = "I";
                    else if (type.Key == 2)
                        xpType = "II";
                    else if (type.Key == 3)
                        xpType = "III";
                    else if (type.Key == 4)
                        xpType = "IV";
                    else if (type.Key == 5)
                        xpType = "V";
                    else
                        xpType = "???";

                    DrawXPBar($"Type: {xpType}", current, needed, size, max);
                }
            }
            else
            {
                float windowSize = ImGui.GetWindowSize().X - 20;
                Vector2 size = new Vector2(windowSize, 10);

                var (classScore, cappedClassScore, totalScores, classId) = MissionHandler.GetCosmicClassScores();

                DrawXPBar("Score", (uint)classScore, 0, size, 500_000);
            }
        }

        void DrawXPBar(string label, uint currentXP, uint neededXP, Vector2 size, uint maxXP = 0)
        {
            // Handle capped and invalid data
            float fraction;
            if (neededXP == 0)
            {
                fraction = Math.Clamp((float)currentXP / (float)maxXP, 0f, 1f);
            }
            else
            {
                fraction = Math.Clamp((float)currentXP / (float)neededXP, 0f, 1f);
            }

            // Display correct text
            string displayText = (neededXP == 0 && maxXP > 0)
                ? $"{label}: {currentXP:N0} / {maxXP:N0}"
                : $"{label}: {currentXP:N0} / {neededXP:N0}";

            ImGui.Text(displayText);

            // Draw bar
            var pos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();

            var barStart = pos;
            var barEnd = new Vector2(pos.X + size.X, pos.Y + size.Y);

            drawList.AddRectFilled(barStart, barEnd, ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.15f, 1f)));

            float filledWidth = size.X * fraction;
            if (filledWidth > 0f)
            {
                var filledEnd = new Vector2(pos.X + filledWidth, pos.Y + size.Y);

                drawList.AddRectFilledMultiColor(
                    barStart,
                    filledEnd,
                    ImGui.GetColorU32(new Vector4(0.2f, 0.6f, 1f, 1f)), // left
                    ImGui.GetColorU32(new Vector4(0.6f, 1f, 0.8f, 1f)), // right
                    ImGui.GetColorU32(new Vector4(0.6f, 1f, 0.8f, 1f)), // right
                    ImGui.GetColorU32(new Vector4(0.2f, 0.6f, 1f, 1f))  // left
                );
            }

            ImGui.Dummy(new Vector2(size.X, size.Y + 5));
        }


        #region Table Tools

        private void CenterText(string text)
        {
            float colWidth = ImGui.GetColumnWidth();
            Vector2 textSize = ImGui.CalcTextSize(text);
            float offset = (colWidth - textSize.X) * 0.5f;
            if (offset > 0)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            ImGui.TextUnformatted(text);
        }
        private void CenterTextInTableCell(string text)
        {
            float cellWidth = ImGui.GetContentRegionAvail().X;
            float textWidth = ImGui.CalcTextSize(text).X;
            float offset = (cellWidth - textWidth) * 0.5f;

            if (offset > 0f)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

            ImGui.TextUnformatted(text);
        }
        public void CenterCheckboxInTableCell(string label, ref bool value, ref bool config)
        {
            float cellWidth = ImGui.GetContentRegionAvail().X;
            float checkboxWidth = ImGui.CalcTextSize(label).X + ImGui.GetFrameHeight(); // estimate checkbox width
            float offset = (cellWidth - checkboxWidth) * 0.5f;

            if (offset > 0f)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

            if (ImGui.Checkbox(label, ref value))
            {

            }
        }
        private void DrawCenteredHeader(string label, float spacing = 4f)
        {
            var drawList = ImGui.GetWindowDrawList();
            var cursorPos = ImGui.GetCursorScreenPos();
            var windowWidth = ImGui.GetContentRegionAvail().X;

            float padding = 6.0f;
            var textSize = ImGui.CalcTextSize(label);
            float bgHeight = textSize.Y + padding * 2;

            var headerRectMin = cursorPos;
            var headerRectMax = new Vector2(cursorPos.X + windowWidth, cursorPos.Y + bgHeight);

            // Background and border
            drawList.AddRectFilled(headerRectMin, headerRectMax, ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1f)), 2f);
            drawList.AddRect(headerRectMin, headerRectMax, ImGui.GetColorU32(ImGuiColors.ParsedGold), 2f);

            // Centered text
            var textPos = new Vector2(
                cursorPos.X + (windowWidth - textSize.X) * 0.5f,
                cursorPos.Y + padding
            );
            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), label);

            // Advance cursor to next line
            ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + bgHeight + spacing));
        }
        private List<uint> GetOnlyPreviousMissionsRecursive(uint missionId)
        {
            if (!MissionInfoDict.TryGetValue(missionId, out var missionInfo) || missionInfo.PreviousMissionID == 0)
                return [];

            var chain = GetOnlyPreviousMissionsRecursive(missionInfo.PreviousMissionID);
            chain.Add(missionInfo.PreviousMissionID);
            return chain;
        }
        private List<uint> GetOnlyNextMissionsRecursive(uint missionId)
        {
            uint? nextMissionId = MissionInfoDict
                .Where(m => m.Value.PreviousMissionID == missionId)
                .Select(m => (uint?)m.Key)
                .FirstOrDefault();

            if (!nextMissionId.HasValue)
                return [];

            var chain = new List<uint> { nextMissionId.Value };
            chain.AddRange(GetOnlyNextMissionsRecursive(nextMissionId.Value));
            return chain;
        }

        #endregion

    }
}
