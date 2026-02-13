using ECommons.Automation.NeoTaskManager;
using ECommons.GameHelpers;
using ICE.Utilities.Cosmic_Helper;
using static ICE.Enums.IceState;

namespace ICE.Scheduler
{
    internal static unsafe class SchedulerMain
    {
        internal static bool EnablePlugin()
        {
            State = Start;
            IceLogging.Info($"Setting State to: {State} / Enabling Plugin");
            Mission_Settings.StartJob = Player.Job;
            GenericManager.StorePandoraStates();
            return true;
        }
        internal static bool DisablePlugin()
        {
            IceLogging.Debug("Stopping the plugin state", "[Schedular - Disable Plugin]");
            P.TaskManager.Abort();
            State = IceState.Idle;
            GenericManager.RestorePandoraStates();
            if (P.Navmesh.Installed)
            {
                if (P.Navmesh.IsRunning())
                    P.Navmesh.Stop();
            }

            return true;
        }

        // Debug only settings
        internal static bool DebugOOMMain = false;
        internal static bool DebugOOMSub = false;

        internal static IceState State = Idle;
        internal static MissionAttributes MissionState = MissionAttributes.None;

        internal static void Tick()
        {
            if (Throttles.GenericThrottle && P.TaskManager.NumQueuedTasks == 0 && State != Idle)
            {
                switch (State)
                {
                    case Gambling: Task_Gamba.Enqueue(); break;
                    case Start: Task_CheckState.Enqueue(); break;
                    case Spiritbond: Task_Spiritbond.Enqueue(); break;
                    case Repair: Task_Repair.Enqueue(); break;
                    case HubReturn: Task_HubActivities.Enqueue(); break;
                    case GrabMission: Task_FindMission.Enqueue(); break;
                    case AbandonMission: Task_AbandonMission.Enqueue(); break;
                    case ExecutingMission: Task_ExecuteMission.Enqueue(); break;
                    case ScoreCheck: Task_CheckScore.Enqueue(); break;
                    case TurninMission: Task_TurninMission.Enqueue(); break;
                    case Craft: Task_Craft.Enqueue(); break;
                    case Gather: Task_Gather.Enqueue(); break;
                    case Fish: Task_Fishing.Enqueue(); break;
                    case DualClass: Task_DualClass.Enqueue(); break;
                    case ManualMode: Task_Manual.Enqueue(); break;
                    default: DisablePlugin(); break;
                }
            }
        }
    }
}