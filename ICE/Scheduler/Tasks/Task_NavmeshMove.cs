using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.Toast;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ICE.Utilities.Cosmic_Helper;

namespace ICE.Scheduler.Tasks
{
    internal class Task_NavmeshMove
    {
        // Constants
        private const float navmeshTolerance = 0.25f;
        private const float distanceToBeStuck = 1.0f;
        private const int stuckTimeThresholdMs = 1000;
        private const int navmeshThrottleMs = 3000;
        private const int waitingThrottleMs = 1000;
        private const int positionLogThrottleMs = 500;
        private const int stuckCheckLogThrottleMs = 500;
        private const int movingMessageThrottleMs = 2000;

        // State tracking
        private static Vector3 lastPosition = Vector3.Zero;
        private static DateTime whenStarted = DateTime.Now;
        private static DateTime lastTimeTracked = DateTime.Now;
        private static bool isJumpInProgress = false;

        public static bool? Task_NavTo(Vector3 pos, bool waitForBusy = true, float distance = 2.0f, bool stayMounted = false, Vector3? npcLoc = null)
        {
            string handle = "[Navmesh Task_NavTo]";

            // Cache frequently accessed values
            bool usingCosmoliner = Svc.Condition[ConditionFlag.Unknown101];
            bool mounted = Player.Mounted;
            bool inMission = CosmicHelper.CurrentLunarMission != 0;
            float minMountDistance = C.MountRadius;
            float dismountDistance = C.DismountRadius;
            bool useMount = ShouldUseMount(inMission);

            // Calculate distance once
            float distanceToTarget = npcLoc.HasValue
                ? Player.DistanceTo(npcLoc.Value)
                : Player.DistanceTo(pos);

            if (EzThrottler.Throttle("Navmesh message throttle", navmeshThrottleMs))
                IceLogging.Verbose("Executing Navmesh Task", handle, debugOnly: true);

            // Early exit if navmesh not installed
            if (!P.Navmesh.Installed)
            {
                IceLogging.Info("We seem to be missing navmesh... so we're just going to exit here", handle);
                return true;
            }

            // Handle navmesh not ready
            if (!P.Navmesh.IsReady())
            {
                if (EzThrottler.Throttle("Waiting on navmesh", waitingThrottleMs))
                {
                    var navProgress = P.Navmesh.BuildProgress();
                    IceLogging.Debug($"Waiting for navmesh to finish building. Currently at: {navProgress:N2}", handle);
                }
                return false;
            }

            // Handle running navmesh
            if (P.Navmesh.IsRunning())
            {
                return HandleRunningNavmesh(pos, waitForBusy, distance, stayMounted, npcLoc, usingCosmoliner, mounted, useMount, minMountDistance, dismountDistance, distanceToTarget, handle);
            }

            // Handle starting navmesh
            return HandleStartNavmesh(pos, distance, stayMounted, npcLoc, usingCosmoliner, mounted, distanceToTarget, handle);
        }

        private static bool ShouldUseMount(bool inMission)
        {
            return (C.UseMountInMission && inMission) || (C.UseMountOutsideMission && !inMission);
        }

        private static bool? HandleRunningNavmesh
            (Vector3 pos, bool waitForBusy, float distance, 
            bool stayMounted, Vector3? npcLoc, bool usingCosmoliner, 
            bool mounted, bool useMount, float minMountDistance, 
            float dismountDistance, float distanceToTarget, string handle)
        {
            // Stop navmesh if using cosmoliner
            if (usingCosmoliner)
            {
                P.Navmesh.Stop();
                return false;
            }

            // Stop navmesh after jump completes
            if (!Player.IsJumping && isJumpInProgress)
            {
                P.Navmesh.Stop();
                isJumpInProgress = false;
                return false;
            }

            // Track position for stuck detection
            if (EzThrottler.Throttle("Logging last position", positionLogThrottleMs))
            {
                lastPosition = Player.Position;
            }

            CheckIfIsStuck();

            // Handle mounting
            if (!mounted && distanceToTarget > minMountDistance && useMount)
            {
                if (EzThrottler.Throttle("Using mount"))
                    Utils.MountAction();
            }

            // Handle dismounting
            if (distanceToTarget <= dismountDistance && !stayMounted)
            {
                if (EzThrottler.Throttle("Dismounting the mount"))
                {
                    Utils.Dismount();
                }
            }

            // Check if we should wait for movement to stop
            if (Player.IsMoving && waitForBusy)
            {
                if (EzThrottler.Throttle("Throttle message tehe", movingMessageThrottleMs))
                    IceLogging.Verbose("We're currently moving, and we were told to wait for us to NOT be moving so... yeah, we waiting", handle);
                return false;
            }

            // Stop navmesh if we're close enough and not waiting for busy
            if (!waitForBusy && distanceToTarget <= distance)
            {
                if (EzThrottler.Throttle("Distance stop throttle"))
                    IceLogging.Debug("We're within stopping distance, so stopping navmesh", handle);
                P.Navmesh.Stop();
            }

            return false;
        }

        private static bool? HandleStartNavmesh(
            Vector3 pos, float distance, bool stayMounted,
            Vector3? npcLoc, bool usingCosmoliner, bool mounted,
            float distanceToTarget, string handle)
        {
            // Don't start navmesh while using cosmoliner
            if (usingCosmoliner)
            {
                ResetInfo();
                return false;
            }

            // Wait for jump to complete
            if (Player.IsJumping)
            {
                return false;
            }

            // Check if we've arrived at destination
            if (distanceToTarget < distance)
            {
                return HandleArrival(mounted, stayMounted, distanceToTarget, distance, handle);
            }

            // Start navmesh pathfinding
            if (EzThrottler.Throttle("Telling navmesh to start"))
            {
                IceLogging.Debug("Telling navmesh to start pathfinding", handle);
                whenStarted = DateTime.Now;
                ResetInfo();
                IceLogging.DestinationLogs.Log(pos);
                P.Navmesh.SetTolerance(navmeshTolerance);
                P.Navmesh.PathfindAndMoveTo(pos, false);
            }

            return false;
        }

        private static bool? HandleArrival(bool mounted, bool stayMounted, float distanceToTarget, float requiredDistance, string handle)
        {
            if (mounted && !stayMounted)
            {
                if (EzThrottler.Throttle("Dismounting the mount"))
                {
                    Utils.Dismount();
                }
                return false;
            }

            IceLogging.Debug("We've met the distance threshold for our destination", handle);
            IceLogging.Debug($"Player Distance: {distanceToTarget:N2}");
            IceLogging.Debug($"Expected Distance: {requiredDistance}");
            ResetInfo();
            return true;
        }

        private static void ResetInfo()
        {
            lastPosition = Vector3.Zero;
            lastTimeTracked = DateTime.Now;
            isJumpInProgress = false;
        }

        private static unsafe void CheckIfIsStuck()
        {
            var currentPos = Player.Position;
            var timeSinceLastChecked = (DateTime.Now - lastTimeTracked).TotalMilliseconds;
            var navmeshStartTime = (DateTime.Now - whenStarted).TotalMilliseconds;
            var distanceMoved = Vector3.Distance(currentPos, lastPosition);

            // Early exit if player has moved enough (not stuck)
            if (distanceMoved > distanceToBeStuck)
            {
                ResetInfo();
                return;
            }

            // Throttled logging for debug purposes
            if (EzThrottler.Throttle("Log stuck info", stuckCheckLogThrottleMs))
            {
                IceLogging.Verbose($"Last time checked: {timeSinceLastChecked:N0}ms");
                IceLogging.Verbose($"Navmesh Start time: {navmeshStartTime:N0}ms");
                IceLogging.Verbose($"Current Pos: {currentPos:N2} | Last position: {lastPosition:N2} | Distance: {distanceMoved:N2}");
            }

            // If stuck for long enough, try jumping
            if (timeSinceLastChecked >= stuckTimeThresholdMs && navmeshStartTime >= stuckTimeThresholdMs)
            {
                if (EzThrottler.Throttle("Using jump action"))
                {
                    isJumpInProgress = true;
                    ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2);
                }
            }
        }
    }
}