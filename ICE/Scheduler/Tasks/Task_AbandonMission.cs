using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.Cosmic_Helper;
using MissionRank = FFXIVClientStructs.FFXIV.Client.Game.WKS.WKSManager.MissionRank;

namespace ICE.Scheduler.Tasks
{
    internal static class Task_AbandonMission
    {
        public static void Enqueue()
        {
            P.TaskManager.Enqueue(() => AbandonMission(), "Abandoning the current mission");
            P.TaskManager.Enqueue(() => Task_TurninMission.GoldCheck(), "Checking post mission state + gold state condition");
            P.TaskManager.Enqueue(() => Task_TurninMission.CommandCheck(), "Checking for post mission commands");
            if (C.DelayGrabMission)
                P.TaskManager.EnqueueDelay(C.DelayIncrease);
        }

        public static bool WasAbandoned = false;
        public static bool ForceAbandon = false;

        public static bool? AbandonMission()
        {
            string tag = "Abandon Mission";

            if (CosmicHelper.CurrentLunarMission == 0)
            {
                if (!ForceAbandon)
                {
                    if (Mission_Settings.TurninState > TurninState.None)
                        P.MissionTimer.CompleteMission();
                    else
                        P.MissionTimer.AbandonMission();

                    Mission_Settings.TurninState = TurninState.None;

                    CosmicHelper.Task_UpdateRelicMissionInfo();
                }

                ForceAbandon = false;
                WasAbandoned = false;

                if (P.AutoHook.Installed)
                    P.AutoHook.DeleteAllAnonymousPresets();

                IceLogging.Info("Current mission is 0, checking to see where we need to be now", tag);
                return true;
            }
            else
            {
                Task_TurninMission.PreviousMissionId = CosmicHelper.CurrentLunarMission;
                if (EzThrottler.Throttle("Score Check Update"))
                    Task_TurninMission.ScoreCheck();

                if (Player.Job == (Job)18 && Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Gathering])
                {
                    if (EzThrottler.Throttle("Stop fishing so we can turn in this mission!", 2000))
                        Task_DualClass.StopFishing();

                    return false;
                }

                var rank = Task_CheckScore.CurrentRank();
                if (rank > MissionRank.None)
                {
                   
                        Mission_Settings.TurninState = rank switch
                        {
                            MissionRank.Gold => TurninState.Gold,
                            MissionRank.Silver => TurninState.Silver,
                            MissionRank.Bronze => TurninState.Bronze,
                            _ => TurninState.Bronze,
                        };
                        

                    IceLogging.Debug("Reporting the mission", tag);
                    ReportMissionInstance();
                    WasAbandoned = false;
                    return false;
                }
                else
                {
                    AbandonMissionInstance();
                    IceLogging.Debug("Abandoning the mission", tag);
                    WasAbandoned = true;
                }
            }

            return false;
        }

        private static unsafe void AbandonMissionInstance()
        {
            var WKSInstance = WKSManager.Instance();
            WKSInstance->MissionModule->AbandonMission();
        }

        private static unsafe void ReportMissionInstance()
        {
            var WKSInstance = WKSManager.Instance();
            WKSInstance->MissionModule->ReportMission();
        }

        private static string NormalizeWhitespace(string text)
        {
            return text.Trim()
                       .Replace('\u00A0', ' ')  // Non-breaking space to regular space
                       .Replace('\u2009', ' ')  // Thin space to regular space
                       .Replace('\u202F', ' ')  // Narrow no-break space to regular space
                       .Replace('\u3000', ' '); // Ideographic space to regular space
        }
    }
}
