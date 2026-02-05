using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Globalization;
using static ICE.Utilities.CosmicHelper;

namespace ICE.Ui
{
    internal class OverlayWindow : Window
    {
        private uint selectedJob = C.SelectedJob;
        public OverlayWindow() : base("ICE Overlay")
        {
            Flags = ImGuiWindowFlags.AlwaysAutoResize;

            P.windowSystem.AddWindow(this);
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        public override bool DrawConditions()
        {
            return C.ShowOverlay
                && (PlayerHelper.IsInCosmicZone());
        }

        public override void Draw()
        {
            MissionDetails();

            if (ImGui.BeginTable("Weather/Time Info", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("");
                ImGui.TableSetupColumn("Current");
                ImGui.TableSetupColumn("Next");

                ImGui.TableHeadersRow();


                if (true)
                {
                    WeatherForcast();
                }

                if (true)
                {
                    TimedMissionDetails();
                }

                ImGui.EndTable();
            }

            HomeButtons();

            ClassExpDetails();

            ClassRelicDetails();
        }

        private void MissionDetails()
        {
            ImGui.Text($"Current state: " + SchedulerMain.State.ToString());
            if (CosmicHelper.SheetMissionDict.TryGetValue(CosmicHelper.CurrentLunarMission, out var missionName) && SchedulerMain.State != IceState.AbandonMission)
            {
                ImGui.TextWrapped($"Current Mission: [{CosmicHelper.CurrentLunarMission}] {missionName.Name}");
            }
            else
            {
                ImGui.Text("Current Mission: None");
            }
#if DEBUG
            if (C.ShowDebugGatherInfo)
            {
                ImGui.Text($"Current Collectable State: {Mission_Settings.CollectableStep}");
                ImGui.Text($"Total Node: {Mission_Settings.nodeTotal}");
                ImGui.Text($"Node Counter: {Mission_Settings.nodeCounter}");
            }
#endif
        }
        private void WeatherForcast()
        {
            var weatherForecasts = WeatherForecastHandler.GetNextWeathers(6); // Current + next 4

            if (weatherForecasts.Count == 0) return;

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Weather");

            // Current weather column
            ImGui.TableNextColumn();
            if (weatherForecasts.Count > 0)
            {
                var current = weatherForecasts[0];
                if (Svc.Texture.TryGetFromGameIcon(current.IconId, out var weatherIcon))
                {
                    ImGui.Image(weatherIcon.GetWrapOrEmpty().Handle, new Vector2(23, 23));

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"{current.Name}");
                        ImGui.EndTooltip();
                    }
                }
            }

            // Next weather column - show next 4 weathers
            ImGui.TableNextColumn();
            for (int i = 1; i < Math.Min(6, weatherForecasts.Count); i++)
            {
                if (i > 1)
                    ImGui.SameLine(0, 2);

                var weather = weatherForecasts[i];
                if (Svc.Texture.TryGetFromGameIcon(weather.IconId, out var weatherIcon))
                {
                    ImGui.Image(weatherIcon.GetWrapOrEmpty().Handle, new Vector2(23, 23));

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"{weather.Name}");
                        ImGui.Text($"In: {weather.TimeUntil}");
                        ImGui.EndTooltip();
                    }
                }
            }
        }
        private unsafe void TimedMissionDetails()
        {
            var eorzeaTime = DateTimeOffset.FromUnixTimeSeconds(Framework.Instance()->ClientTime.EorzeaTime);
            int currentHour = eorzeaTime.Hour;
            int nextHour = (currentHour + 1) % 24;

            var currentHourMissions = SheetMissionDict
                .Where(kvp => IsAvailableAtHour(kvp.Value, currentHour))
                .Where(kvp => kvp.Value.TerritoryId == Player.Territory.RowId)
                .OrderBy(kvp => kvp.Value.Jobs.FirstOrDefault())
                .ToList();

            var nextHourMissions = SheetMissionDict
                .Where(kvp => IsAvailableAtHour(kvp.Value, nextHour))
                .Where(kvp => kvp.Value.TerritoryId == Player.Territory.RowId)
                .OrderBy(kvp => kvp.Value.Jobs.FirstOrDefault())
                .ToList();

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Timed");

            ImGui.TableNextColumn();
            for (int i = 0; i < currentHourMissions.Count; i++)
            {
                if (i > 0)
                    ImGui.SameLine(0, 2);

                var mission = currentHourMissions[i];
                if (CosmicHelper.JobIconDict.TryGetValue(mission.Value.Jobs[0], out var jobIcon))
                {
                    var imageSize = new Vector2(23, 23);
                    ImGui.Image(jobIcon.GetWrapOrEmpty().Handle, imageSize);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"[{mission.Key}]");
                        ImGui.SameLine(0, 2);
                        ImGui.Text($"{mission.Value.Name}");
                        ImGui.EndTooltip();
                    }
                }
            }

            ImGui.TableNextColumn();
            for (int i = 0; i < nextHourMissions.Count; i++)
            {
                if (i > 0)
                    ImGui.SameLine(0, 2);

                var mission = nextHourMissions[i];
                if (CosmicHelper.JobIconDict.TryGetValue(mission.Value.Jobs[0], out var jobIcon))
                {
                    var imageSize = new Vector2(23, 23);
                    ImGui.Image(jobIcon.GetWrapOrEmpty().Handle, imageSize);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"[{mission.Key}]");
                        ImGui.SameLine(0, 2);
                        ImGui.Text($"{mission.Value.Name}");
                        ImGui.EndTooltip();
                    }
                }
            }
        }
        private bool IsAvailableAtHour(CosmicInfo mission, int hour)
        {
            // If StartTime <= EndTime: normal range (e.g., 8-16)
            if (mission.StartTime <= mission.EndTime)
            {
                return hour >= mission.StartTime && hour < mission.EndTime;
            }
            // If StartTime > EndTime: crosses midnight (e.g., 20-4)
            else
            {
                return hour >= mission.StartTime || hour < mission.EndTime;
            }
        }
        private void HomeButtons()
        {
            if (ImGuiEx.IconButton(FontAwesomeIcon.Home, "Open ICE"))
            {
                P.mainWindow.IsOpen = true;
            }
            ImGui.SameLine();

            // Start button (disabled while already ticking).
            bool xpLeveling = C.XPLeveling_Mode;
            bool unsupportedArtisan = xpLeveling && !P.Artisan.UpdatedArtisan() && CosmicHelper.CrafterJobList.Contains((uint)Player.Job);
            bool unsupportedClass = !PlayerHelper.UsingSupportedJob();

            bool unsupported = SchedulerMain.State != IceState.Idle || !PlayerHelper.UsingSupportedJob() || unsupportedArtisan;

            using (ImRaii.Disabled(unsupported))
            {
                var defaultButtonColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
                var color = unsupported ? EColor.Red : defaultButtonColor;

                using var tempButton = ImRaii.PushColor(ImGuiCol.Button, color);
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
            ImGui.Checkbox("Stop after current mission", ref Mission_Settings.StopAfterCurrent);
        }
        private void ClassExpDetails()
        {
            if (true && PlayerHelper.UsingSupportedJob())
            {
                List<uint> jobs = new();
                if (CosmicHelper.CurrentLunarMission != 0)
                    jobs = CosmicHelper.SheetMissionDict[CurrentLunarMission].Jobs;
                else
                    jobs.Add((uint)Player.Job);

                foreach (var job in jobs)
                {
                    if (CosmicHelper.JobIconDict.TryGetValue(job, out var jobIcon))
                    {
                        var imageSize = new Vector2(23, 23);
                        ImGui.Image(jobIcon.GetWrapOrEmpty().Handle, imageSize);
                        ImGui.SameLine();
                        ImGui.AlignTextToFramePadding();
                        Relic_XP.DrawScoreBar(new Vector2(ImGui.GetContentRegionAvail().X, 10), false, job);
                    }
                }
            }

            if (C.ShowTotalScore)
            {
                (uint TotalScore, uint TotalComplete, uint MaxScore, Dictionary<uint, uint> ClassInfo) = Relic_XP.GetTotalScores();
                var ScoreBarSize = new Vector2(ImGui.GetContentRegionAvail().X, 10);
                Relic_XP.DrawXPBar($"Total Score | Completed: [{TotalComplete} / 11]", TotalScore, MaxScore, ScoreBarSize);
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    foreach (var job in ClassInfo)
                    {
                        var jobIdInfo = job.Key;
                        var jobScore = job.Value;
                        var jobImage = CosmicHelper.JobIconDict[jobIdInfo];
                        ImGui.Image(jobImage.GetWrapOrEmpty().Handle, new Vector2(23, 23));
                        ImGui.SameLine();
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text($"Score: {jobScore:N0}");
                    }
                    ImGui.EndTooltip();
                }
            }
        }
        private void ClassRelicDetails()
        {
            if (C.ShowExpBars)
            {
                var currentJobId = (uint)Player.Job;

                bool showExp = (CosmicHelper.CrafterJobList.Contains(currentJobId) || CosmicHelper.GatheringJobList.Contains(currentJobId));

                if (CosmicHelper.CrafterJobList.Contains(currentJobId) || CosmicHelper.GatheringJobList.Contains(currentJobId))
                {
                    if (ImGui.CollapsingHeader("Relic Tool XP"))
                    {
                        Relic_XP.DrawRelicXP((uint)currentJobId);
                    }
                }
            }
        }
    }
}
