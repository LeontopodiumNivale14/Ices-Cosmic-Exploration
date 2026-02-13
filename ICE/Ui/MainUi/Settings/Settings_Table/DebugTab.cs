using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.MainUi.Settings.Settings_Table
{
    internal class DebugTab
    {
        public static void Draw()
        {
            ImGui.Checkbox("Force OOM Main", ref SchedulerMain.DebugOOMMain);
            ImGui.Checkbox("Force OOM Sub", ref SchedulerMain.DebugOOMSub);
            // ImGui.Checkbox("Legacy Failsafe WKSRecipe Select", ref C.FailsafeRecipeSelect);

            var missionMap = new List<(string name, Func<byte> get, Action<byte> set)>
                {
                    ("Sequence Missions", new Func<byte>(() => C.SequenceMissionPriority), new Action<byte>(v => { C.SequenceMissionPriority = v; C.Save(); })),
                    ("Timed Missions", new Func<byte>(() => C.TimedMissionPriority), new Action<byte>(v => { C.TimedMissionPriority = v; C.Save(); })),
                    ("Weather Missions", new Func<byte>(() => C.WeatherMissionPriority), new Action<byte>(v => { C.WeatherMissionPriority = v; C.Save(); }))
                };

            var sorted = missionMap
                .Select((m, i) => new { Index = i, Name = m.name, Priority = m.get() })
                .OrderBy(m => m.Priority)
                .ToList();

            if (ImGui.Button("Get Sinus Forecast"))
            {
                List<WeatherForecast> forecast = WeatherForecastHandler.GetTerritoryForecast(1237);
                Func<WeatherForecast, string> formatTime = (forecast) => WeatherForecastHandler.FormatForecastTime(forecast.Time);

                Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                {
                    Message = $"Sinus Ardorum Weather - {forecast[0].Name}",
                    Type = Dalamud.Game.Text.XivChatType.Echo,
                });
                for (int i = 1; i < forecast.Count; i++)
                {
                    Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                    {
                        Message = $"{forecast[i].Name} In {formatTime(forecast[i])}",
                        Type = Dalamud.Game.Text.XivChatType.Echo,
                    });
                }
            }

            using (ImRaii.Disabled(!PlayerHelper.IsInCosmicZone()))
            {
                if (ImGui.Button("Refresh Forecast"))
                {
                    WeatherForecastHandler.GetForecast();
                }
            }
            bool gatherDebug = C.ShowDebugGatherInfo;
            if (ImGui.Checkbox("Show Gather Debug Info", ref gatherDebug))
            {
                C.ShowDebugGatherInfo = gatherDebug;
                C.Save();
            }

            bool highlightTable = C.HighlightVisibleMissions;
            if (ImGui.Checkbox("Highlight Visible Missions", ref highlightTable))
            {
                C.HighlightVisibleMissions = highlightTable;
                C.Save();
            }

            bool onlyGrabMission = C.OnlyGrabMission_Debug;
            if (ImGui.Checkbox($"Only grab mission", ref onlyGrabMission))
            {
                C.OnlyGrabMission_Debug = onlyGrabMission;
                C.Save();
            }
        }
    }
}
