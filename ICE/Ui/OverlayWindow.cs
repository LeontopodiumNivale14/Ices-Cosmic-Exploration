using System.Globalization;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using Lumina.Excel.Sheets;

namespace ICE.Ui
{
    internal class OverlayWindow : Window
    {
        public OverlayWindow() : base("ICE Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize)
        {
            P.windowSystem.AddWindow(this);
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        

        public override void Draw()
        {
            ImGui.Text($"Current state: " + SchedulerMain.State.ToString());
#if DEBUG
            if (CosmicHelper.CurrentLunarMission != 0)
            {
                ImGui.Text($"Current node: {SchedulerMain.CurrentIndex} / Visited: {SchedulerMain.NodesVisited}");
                ImGui.Text($"NodeSet: {CosmicHelper.MissionInfoDict[CosmicHelper.CurrentLunarMission].NodeSet}");
                ImGui.Text($"Attributes: {CosmicHelper.CurrentMissionInfo.Attributes}");
            }
#endif

                ImGuiHelpers.ScaledDummy(2);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(2);

            (string currentWeather, string nextWeather, string nextWeatherTime) = WeatherForecastHandler.GetNextWeather();

            if (currentWeather != null)
            {
                ImGui.Text($"Weather: {currentWeather} -> {nextWeather} in [{nextWeatherTime}]");
            }

            (var currentTimedBonus, var nextTimedBonus) = PlayerHandlers.GetTimedJob();
            if (currentTimedBonus.Value == null)
            {
                ImGui.Text($"Timed Mission(s): None -> {string.Join(", ", nextTimedBonus.Value)} [{nextTimedBonus.Key.start:D2}:00]");
            }
            else
            {
                ImGui.Text($"Timed Mission(s): {string.Join(", ", currentTimedBonus.Value)} -> {string.Join(", ", nextTimedBonus.Value)} [{nextTimedBonus.Key.start:D2}:00]");
            }

            (string type, var locations) = AnnouncementHandlers.CheckForRedAlert();
            if (type != null && locations != null)
            {
                ImGui.Text($"[Red Alert] {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(type)}");
                ImGui.Spacing();
                for (int i = 0; i < locations.Length; i++)
                {
                    if (locations.Length > 0)
                    {
                        ImGui.Text($"Variant [{i + 1}]");
                        ImGui.SameLine();
                    }

                    (string job, uint territoryId, float x, float y) = locations[i].first;
                    if (ImGui.Button($"{job}"))
                    {
                        Utils.SetFlagForNPC(territoryId, x, y);
                    }

                    ImGui.SameLine();

                    (job, territoryId, x, y) = locations[i].second;
                    if (ImGui.Button($"{job}"))
                    {
                        Utils.SetFlagForNPC(territoryId, x, y);
                    }
                    ImGui.Spacing();
                }
            }

            ImGuiHelpers.ScaledDummy(2);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(2);

            DrawScore();

            ImGuiHelpers.ScaledDummy(2);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(2);

            if (ImGuiEx.IconButton("\uf013##Config", "Open ICE"))
            {
                P.mainWindow2.IsOpen = true;
            }
            ImGui.SameLine();

            // Start button (disabled while already ticking).
            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !PlayerHelper.UsingSupportedJob()))
            {
                if (ImGui.Button("Start"))
                {
                    SchedulerMain.EnablePlugin();
                }
            }

            ImGui.SameLine();

            // Stop button (disabled while not ticking).
            using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
            {
                if (ImGui.Button("Stop"))
                {
                    SchedulerMain.DisablePlugin();
                }
            }
            ImGui.SameLine();
            ImGui.Checkbox("Stop after current mission", ref SchedulerMain.StopBeforeGrab);
            //    //    Type = Dalamud.Game.Text.XivChatType.Debug,
            //    //});
            //}
        }

        void DrawScore()
        {
            try
            {
                unsafe
                {
                    var wksManager = WKSManager.Instance();
                    var currentMissionId = wksManager->CurrentMissionUnitRowId;

                    uint? classId;
                    if (currentMissionId > 0 &&
                        CosmicHelper.MissionInfoDict.TryGetValue(currentMissionId, out var missionInfo))
                        classId = missionInfo.JobId;
                    else
                        classId = Svc.ClientState.LocalPlayer?.ClassJob.RowId;

                    if (classId is >= 8 and <= 18)
                    {
                        var scores = wksManager->Scores;

                        int classScore = scores[(int)classId - 8];
                        var cappedClassScore = Math.Min(500_000, classScore);

                        int totalScores = 0;
                        for (int i = 0; i < scores.Length; ++i)
                            totalScores += Math.Min(500_000, scores[i]);

                        ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture,
                            $"{Svc.Data.GetExcelSheet<ClassJob>().GetRow(classId.Value).Abbreviation}: {(float)cappedClassScore / 500_000:P} ({classScore:N0})"));
                        ImGui.SameLine();
                        using (ImRaii.Disabled())
                        {
                            ImGui.TextUnformatted("--");
                            ImGui.SameLine();
                            ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture,
                                $"All: {(float)totalScores / 11 / 500_000:P} ({SeIconChar.CrossWorld.ToIconChar()} {11 * 500_000 - totalScores:N0})"));
                        }
                    }
                }
            }
            catch
            {
                // meh
            }
        }
    }
}
