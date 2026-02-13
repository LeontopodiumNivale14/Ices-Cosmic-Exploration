using System.Collections.Generic;
using System.Diagnostics;
using static ICE.ConfigFiles.Config.MissionSettings;
using static ICE.Ui.MainUi.ModeSelect.modeSelect_TableInfo;

public class MissionTimer
{
    private Stopwatch stopwatch;
    private uint currentMission;
    private bool isRunning;

    public int TimeHistoryLimit => C.TimeHistoryLimit;

    public MissionTimer()
    {
        stopwatch = new Stopwatch();
    }

    public void StartMission(uint missionId)
    {
        currentMission = missionId;
        stopwatch.Restart();
        isRunning = true;
    }

    public TimeSpan CompleteMission()
    {
        if (!isRunning)
        {
            return TimeSpan.Zero;
        }

        stopwatch.Stop();
        var duration = stopwatch.Elapsed;
        isRunning = false;

        UpdateMissionStats(currentMission, duration);
        currentMission = 0;

        return duration;
    }

    private void UpdateMissionStats(uint missionId, TimeSpan duration)
    {
        if (!C.MissionConfig.ContainsKey(missionId))
        {
            C.MissionConfig[missionId] = new();
        }

        var stats = C.MissionConfig[missionId];

        // Adding the new time entry here
        stats.TurninRecords.Add(new TurninData
        {
            Time = duration.TotalSeconds,
            State = Mission_Settings.TurninState,
        });

        if (Mission_Settings.TurninState == TurninState.Bronze)
            stats.BronzeCompletion++;
        else if (Mission_Settings.TurninState == TurninState.Silver)
            stats.SilverCompletions++;
        else if (Mission_Settings.TurninState == TurninState.Gold)
            stats.GoldCompletions++;
        else if (Mission_Settings.TurninState == TurninState.Critical)
            stats.CriticalCompletions++;

        // Increment total completions (always tracks full history)
        stats.TotalCompletions++;

        // Apply time history limit per state if set
        if (TimeHistoryLimit > 0)
        {
            // Group records by state
            var groupedByState = stats.TurninRecords
                .GroupBy(t => t.State)
                .ToList();

            // Keep only the most recent TimeHistoryLimit records for each state
            var trimmedRecords = new List<TurninData>();
            foreach (var group in groupedByState)
            {
                trimmedRecords.AddRange(group.TakeLast(TimeHistoryLimit));
            }

            stats.TurninRecords = trimmedRecords;
        }

        // Calculate stats based on the (possibly limited) time history
        if (stats.TurninRecords.Any())
        {
            stats.BestTime = stats.TurninRecords.Min(t => t.Time);
            stats.AverageTime = stats.TurninRecords.Average(t => t.Time);

            // Calculate per-state averages
            var bronzeRecords = stats.TurninRecords.Where(t => t.State == TurninState.Bronze).ToList();
            var silverRecords = stats.TurninRecords.Where(t => t.State == TurninState.Silver).ToList();
            var goldRecords = stats.TurninRecords.Where(t => t.State == TurninState.Gold).ToList();
            var criticalRecords = stats.TurninRecords.Where(t => t.State == TurninState.Critical).ToList();

            stats.AverageBronzeTime = bronzeRecords.Any() ? bronzeRecords.Average(t => t.Time) : 0;
            stats.AverageSilverTime = silverRecords.Any() ? silverRecords.Average(t => t.Time) : 0;
            stats.AverageGoldTime = goldRecords.Any() ? goldRecords.Average(t => t.Time) : 0;
            stats.AverageCriticalTime = criticalRecords.Any() ? criticalRecords.Average(t => t.Time) : 0;
        }

        C.Save();
    }

    public void ResetTimers(uint missionId)
    {
        if (!C.MissionConfig.ContainsKey(missionId))
        {
            C.MissionConfig[missionId] = new();
        }

        var stats = C.MissionConfig[missionId];
        stats.TurninRecords.Clear();
        stats.BestTime = double.MaxValue;

        stats.AverageTime = 0;
        stats.AverageBronzeTime = 0;
        stats.AverageSilverTime = 0;
        stats.AverageGoldTime = 0;
        stats.AverageCriticalTime = 0;

        stats.TotalCompletions = 0;
        stats.BronzeCompletion = 0;
        stats.SilverCompletions = 0;
        stats.GoldCompletions = 0;
        stats.CriticalCompletions = 0;
        stats.FailedCounters = 0;

        C.Save();
    }

    public static class MissionStatsCalculator
    {
        public static double CalculateCurrencyPerMinute(double averageTimeSeconds, uint baseScore, double multiplier)
        {
            if (averageTimeSeconds <= 0) return 0;

            return (60.0 * baseScore * multiplier) / averageTimeSeconds;
        }
        public static double CalculateAverageSequenceScorePerMinute(uint id, int multiplier)
        {
            double totalTimeSeconds = 0;
            double totalScore = 0;

            List<uint> sequenceMissions = new();
            sequenceMissions = GetOnlyPreviousMissionsRecursive(id);
            sequenceMissions.Add(id);

            foreach (var missionId in sequenceMissions)
            {
                if (C.MissionConfig.TryGetValue(missionId, out var mission))
                {
                    // If any mission has invalid time, return 0
                    if (mission.AverageTime <= 0) return 0;

                    if (CosmicHelper.SheetMissionDict.TryGetValue(missionId, out var sheetInfo))
                    {
                        totalTimeSeconds += mission.AverageTime;
                        totalScore += sheetInfo.ClassScore * multiplier;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            if (totalTimeSeconds <= 0) return 0;

            double totalTimeMinutes = totalTimeSeconds / 60.0;
            return totalScore / totalTimeMinutes;
        }

        public static double CalculateActualScorePerMinute(List<TurninData> turninRecords, uint baseScore)
        {
            if (turninRecords.Count == 0) return 0;

            // Count each turnin type
            int bronzeCount = turninRecords.Count(t => t.State == TurninState.Bronze);
            int silverCount = turninRecords.Count(t => t.State == TurninState.Silver);
            int goldCount = turninRecords.Count(t => t.State == TurninState.Gold);

            // Calculate total score earned
            double totalScore = (bronzeCount * baseScore * 1.0) +
                               (silverCount * baseScore * 4.0) +
                               (goldCount * baseScore * 5.0);

            // Calculate total time spent (in minutes)
            double totalTimeMinutes = turninRecords.Sum(t => t.Time) / 60.0;

            if (totalTimeMinutes <= 0) return 0;

            return totalScore / totalTimeMinutes;
        }

        public static double CalculateActualScorePerHour(List<TurninData> turninRecords, uint baseScore)
        {
            if (turninRecords.Count == 0) return 0;

            // Count each turnin type
            int bronzeCount = turninRecords.Count(t => t.State == TurninState.Bronze);
            int silverCount = turninRecords.Count(t => t.State == TurninState.Silver);
            int goldCount = turninRecords.Count(t => t.State == TurninState.Gold);

            // Calculate total score earned
            double totalScore = (bronzeCount * baseScore * 1.0) +
                               (silverCount * baseScore * 4.0) +
                               (goldCount * baseScore * 5.0);

            // Calculate total time spent (in hours)
            double totalTimeHours = turninRecords.Sum(t => t.Time) / 3600.0;

            if (totalTimeHours <= 0) return 0;

            return totalScore / totalTimeHours;
        }
    }

    public void AbandonMission()
    {
        stopwatch.Stop();
        stopwatch.Reset();
        isRunning = false;

        if (!C.MissionConfig.ContainsKey(currentMission))
        {
            C.MissionConfig[currentMission] = new();
        }

        var stats = C.MissionConfig[currentMission];
        stats.FailedCounters++;
        C.Save();
        currentMission = 0;
    }
}