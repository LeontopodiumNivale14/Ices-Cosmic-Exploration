using ECommons.GameHelpers;
using ICE.Utilities.Cosmic_Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace ICE.Scheduler.Tasks
{
    internal class Task_StartMode
    {
        public static void Enqueue()
        {
            P.TaskManager.Enqueue(() => CheckStartState(), "Checking for initial starting state");
        }

        private static bool? CheckStartState()
        {
            string tag = "[Task_StartMode: Check Start]";
            var currentMode = C.SelectedMode;

            if (CosmicHelper.CurrentLunarMission != 0)
            {
                IceLogging.Info("We're currently in a mission when we started, so we're going to finish this up first before we continue", tag);

            }

            if (currentMode == ModeSelect.AgendaMode)
            {
                IceLogging.Verbose("We're currently in agenda mode. We need to check to see if we have anything even in the agenda before we continue", tag);
                if (C.Cosmic_Agenda.Count > 0)
                {
                    IceLogging.Verbose($"We have a task list that we need to complete! Going to swap over to check and see what goal we need to complete");
                    P.TaskManager.Enqueue(() => AgendaCheck(), "Start Mode: Agenda Check");
                    return true;
                }
                else
                {
                    IceLogging.Error($"We're currently set in agenda mode, and it says we have none... if you believe this is an error, and you can prove that" +
                        $"you have setup your agenda to do as you want, please let me know <3", tag);
                    SchedulerMain.State = IceState.Idle;
                    P.TaskManager.Tasks.Clear();
                    return true;
                }
            }
            else
            {
                Mission_Settings.Mode = currentMode;

                IceLogging.Info("We have a pre-selected mode enabled. So we're just going to run that down till we're told to stop\n" +
                    $"Selected Mode: {currentMode}\n" +
                    $"Main job for basic missions: {Player.Job}", tag);
                // 
            }

            return false;
        }

        private static bool? AgendaCheck()
        {
            var agenda = C.Cosmic_Agenda;
            var relicProgress = CosmicHelper.Cosmic_ClassInfo();
            PlayerHelper.GetItemCount(CosmicHelper.CosmoCreditItemId, out var creditAmount);
            int planetCreditAmount = 10000;
            var territory = Player.Territory.RowId;
            if (PlayerHelper.IsInCosmicZone())
            {
                if (CosmicMoonRegistry.TryGetPlanetCreditItemId(territory, out var planetCreditId))
                {
                    PlayerHelper.GetItemCount(planetCreditId, out planetCreditAmount);
                }
            }

            // Same as AgendaCheck — only moons with a cosmodrome have dronebit currency
            int dronebitAmount = 5000;
            if (CosmicMoonRegistry.TryGetDronebit(territory, out var dronebit))
                PlayerHelper.GetItemCount(dronebit.creditId, out dronebitAmount);

            foreach (var entry in agenda)
            {
                IceLogging.Verbose($"Checking:\t" +
                    $"Job: {entry.SelectedJob}\n" +
                    $"Agenda: {entry.SelectedMode}");

                var job = entry.SelectedJob;
                var relicInfo = relicProgress[job];

                var relicLevel = relicInfo.Stage_Current;
                var classScore = relicInfo.Score;
                var level = Player.GetLevel((Job)job);

                var goal = entry.SelectedOption;
                bool achieved = false;

                if (CosmicMoonRegistry.IsMaxRelicPlaylistGoal(goal))
                    achieved = relicLevel >= CosmicMoonRegistry.GetMaxRelicGoal(goal);
                else
                achieved = goal switch
                {
                    PlaylistOptions.SelectedRelicLv => relicLevel >= entry.SelectedRelicLevel,
                    PlaylistOptions.CreditAmount => creditAmount >= entry.CreditAmount,
                    PlaylistOptions.PlanetAmount => planetCreditAmount >= entry.PlanetAmount,
                    PlaylistOptions.DronebitAmount => dronebitAmount >= entry.DronebitAmount,
                    PlaylistOptions.ClassLevel => level >= entry.ClassLevel,
                    PlaylistOptions.ClassScore => classScore >= entry.ClassScore,
                    PlaylistOptions.MasteryScore => relicInfo.Mastery >= entry.ClassScore
                };

                if (!achieved)
                {
                    IceLogging.Info($"Priority has been found to achieve: {goal}. Going to aim to complete this goal");
                    Mission_Settings.Mode = entry.SelectedMode;

                    return true;
                }
            }

            IceLogging.Info("We've actually finished our agenda! Congrats. Stopping the process");
            P.TaskManager.Tasks.Clear();
            SchedulerMain.State = IceState.Idle;

            return false;
        }
    }
}
