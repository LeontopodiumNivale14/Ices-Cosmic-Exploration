using Dalamud.Bindings.ImPlot;
using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ICE.Scheduler.Handlers.PictoStuff;
using ICE.Utilities.Cosmic_Helper;
using ICE.Utilities.GatheringHelper;
using ICE.Utilities.GatheringHelper.RouteLoader;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal class Task_NavmeshMove
    {
        // Constants
        private static readonly Random _random = new();
        private const float navmeshTolerance = 0.25f;
        private const float distanceToBeStuck = 1.0f;
        private static int stuckTimeThresholdMs => C.StuckDelayMs;
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

        private static Random random = new();
        private static int randomCounter = 0;

        public enum TravelTypes
        {
            Direct,
            Aethernet,
            RedAlert,
            Hub_Return,
            Hub_Aethernet,
            Hub_RedAlert,
        }

        #region Navmesh Stuff

        public static bool? Task_NavTo(Vector3 pos, bool waitForBusy = true, float distance = 2.0f, bool stayMounted = false, Vector3? npcLoc = null, bool mountBeforeMove = false)
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

            CloseExtraWindows();

            // Handle running navmesh
            if (P.Navmesh.IsRunning())
            {
                return HandleRunningNavmesh(pos, waitForBusy, distance, stayMounted, npcLoc, usingCosmoliner, mounted, useMount, minMountDistance, dismountDistance, distanceToTarget, handle);
            }

            // Handle starting navmesh
            return HandleStartNavmesh(pos, distance, stayMounted, npcLoc, usingCosmoliner, mounted, distanceToTarget, handle, useMount, mountBeforeMove);
        }
        private static readonly Dictionary<uint, Vector3> _cachedGatherPositions = new();
        private static uint _lastGatherNodeKey = 0;

        public static void ResetGatherMove()
        {
            _cachedGatherPositions.Clear();
            _lastGatherNodeKey = 0;
        }

        public static bool? Task_GatherMove(NodeInfo routeinfo, bool waitForBusy = true, float distance = 3.5f, bool stayMounted = false, bool mountBeforeMove = false)
        {
            string handle = "Navmesh: Gather Move";

            bool usingCosmoliner = Svc.Condition[ConditionFlag.Unknown101];
            bool mounted = Player.Mounted;
            bool inMission = CosmicHelper.CurrentLunarMission != 0;
            float minMountDistance = C.MountRadius;
            float dismountDistance = C.DismountRadius;
            bool useMount = ShouldUseMount(inMission);

            if (EzThrottler.Throttle("Navmesh message throttle", navmeshThrottleMs))
                IceLogging.Verbose("Executing Navmesh Task", handle, debugOnly: true);

            if (!P.Navmesh.Installed)
            {
                IceLogging.Info("We seem to be missing navmesh... so we're just going to exit here", handle);
                return true;
            }

            if (!P.Navmesh.IsReady())
            {
                if (EzThrottler.Throttle("Waiting on navmesh", waitingThrottleMs))
                {
                    var navProgress = P.Navmesh.BuildProgress();
                    IceLogging.Debug($"Waiting for navmesh to finish building. Currently at: {navProgress:N2}", handle);
                }
                return false;
            }

            Vector3 nodePos = routeinfo.Position;
            uint nodeKey = routeinfo.NodeId;

            if (_lastGatherNodeKey != nodeKey || !_cachedGatherPositions.ContainsKey(nodeKey))
            {
                float node_MinAngle = routeinfo.RadiusStart;
                float node_MaxAngle = routeinfo.RadiusEnd;
                float rangeSpan = GetRangeSpan(node_MinAngle, node_MaxAngle);
                float sectionSize = C.GatherFanSectionSize;

                float selectedAngle;
                if (rangeSpan >= 359.9f)
                {
                    // Full fan — pure random, no bias
                    selectedAngle = RandomAngleInRange(node_MinAngle, node_MaxAngle);
                }
                else
                {
                    // Partial fan — find the section closest to where the player is approaching from
                    float angleToPlayer = CalculateAngleToPlayer(nodePos, Player.Position);
                    var (sectionMin, sectionMax) = GetNearestSection(node_MinAngle, node_MaxAngle, angleToPlayer, sectionSize);
                    selectedAngle = RandomAngleInRange(sectionMin, sectionMax);
                }

                float selectedDistance = NextFloat(routeinfo.MinDistance, routeinfo.MaxDistance);

                Vector3 randomPosition = CalculateFanPosition(nodePos, selectedAngle, selectedDistance, routeinfo.FanHeight);
                _cachedGatherPositions[nodeKey] = randomPosition;
                _lastGatherNodeKey = nodeKey;

                IceLogging.Debug($"[GatherMove] New cached position: angle={selectedAngle:F1}, dist={selectedDistance:F2}, pos={randomPosition}", handle);
            }

            Vector3 cachedPos = _cachedGatherPositions[nodeKey];
            float distanceToTarget = Player.DistanceTo(nodePos);

            if (P.Navmesh.IsRunning())
            {
                var result = HandleRunningNavmesh(cachedPos, waitForBusy, distance, stayMounted, nodePos, usingCosmoliner, mounted, useMount, minMountDistance, dismountDistance, distanceToTarget, handle);
                if (result == true)
                {
                    _cachedGatherPositions.Remove(nodeKey);
                    _lastGatherNodeKey = 0;
                }
                return result;
            }

            var startResult = HandleStartNavmesh(cachedPos, distance, stayMounted, nodePos, usingCosmoliner, mounted, distanceToTarget, handle, useMount, mountBeforeMove);
            if (startResult == true)
            {
                _cachedGatherPositions.Remove(nodeKey);
                _lastGatherNodeKey = 0;
            }
            return startResult;
        }
        private static bool ShouldUseMount(bool inMission)
        {
            return (C.UseMountInMission && inMission) || (C.UseMountOutsideMission && !inMission);
        }
        private static bool? HandleRunningNavmesh(Vector3 pos, bool waitForBusy, float distance, bool stayMounted, Vector3? npcLoc, bool usingCosmoliner, bool mounted, bool useMount, float minMountDistance, float dismountDistance, float distanceToTarget, string handle)
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

            if (C.CrazyTaxiArrow)
            {
                if (npcLoc != null)
                    PictoManager.DrawArrowToward(npcLoc.Value);
                else
                    PictoManager.DrawArrowToward(pos);
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
        private static bool? HandleStartNavmesh(Vector3 pos, float distance, bool stayMounted, Vector3? npcLoc, bool usingCosmoliner, bool mounted, float distanceToTarget, string handle, bool useMount = false, bool mountBeforeMove = false)
        {

            if (C.CrazyTaxiArrow)
            {
                if (npcLoc != null)
                    PictoManager.DrawArrowToward(npcLoc.Value);
                else
                    PictoManager.DrawArrowToward(pos);
            }

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

            if (!mounted && useMount && mountBeforeMove)
            {
                Utils.MountAction();
                return false;
            }

            // Start navmesh pathfinding
            if (EzThrottler.Throttle("Telling navmesh to start"))
            {
                IceLogging.Debug("Telling navmesh to start pathfinding", handle);
                whenStarted = DateTime.Now;
                ResetInfo();
                IceLogging.DestinationLogs.Log(pos);
                P.Navmesh.SetTolerance(navmeshTolerance);

                var targetPos = pos;

                P.Navmesh.PathfindAndMoveTo(targetPos, false);
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

            if (EzThrottler.Throttle($"Met Location throttle", 5000))
            {
                IceLogging.Debug("We've met the distance threshold for our destination", handle);
                IceLogging.Debug($"Player Distance: {distanceToTarget:N2}");
                IceLogging.Debug($"Expected Distance: {requiredDistance}");
            }
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

            /*
            // Throttled logging for debug purposes
            if (EzThrottler.Throttle("Log stuck info", stuckCheckLogThrottleMs))
            {
                IceLogging.Verbose($"Last time checked: {timeSinceLastChecked:N0}ms");
                IceLogging.Verbose($"Navmesh Start time: {navmeshStartTime:N0}ms");
                IceLogging.Verbose($"Current Pos: {currentPos:N2} | Last position: {lastPosition:N2} | Distance: {distanceMoved:N2}");
            }
            */

            // If stuck for long enough, try unstuck actions
            if (timeSinceLastChecked >= stuckTimeThresholdMs && navmeshStartTime >= stuckTimeThresholdMs)
            {
                if (C.RetargetIfStuck && EzThrottler.Throttle("Retarget if stuck", 1000))
                {
                    IceLogging.Debug("Stuck detected - stopping navmesh to trigger retarget");
                    P.Navmesh.Stop();
                    ResetInfo();
                    return;
                }

                if (C.JumpIfStuck_V2 && EzThrottler.Throttle("Using jump action"))
                {
                    isJumpInProgress = true;
                    ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2);
                }
            }
        }

        #endregion

        #region Best Pathfinding

        public class PathInfo
        {
            public Vector3 destination { get; set; } = Vector3.Zero;
            public float distance { get; set; } = 0;
            public List<Vector3> pathTo { get; set; } = null;
            public List<Vector3> pathFrom { get; set; } = null;
            public uint Aethernet_TravelTo { get; set; } = 0;
            public uint Aethernet_TravelFrom { get; set; } = 0;
        }
        private static Dictionary<TravelTypes, PathInfo> TravelMethods = new()
        {
            [TravelTypes.Direct] = new(),
            [TravelTypes.Aethernet] = new(),
            [TravelTypes.RedAlert] = new(),
            [TravelTypes.Hub_Return] = new(),
            [TravelTypes.Hub_Aethernet] = new(),
            [TravelTypes.Hub_RedAlert] = new(),
        };
        public class AethernetSystem
        {
            public uint AethernetId { get; set; } = 0;
            public Vector3 Location { get; set; } = Vector3.Zero;
            public Vector3 LandZone { get; set; } = Vector3.Zero;
            public int MapSelector { get; set; } = 0;
            public uint RequiredLogLv { get; set; } = 0;
        }
        public static Dictionary<uint, List<AethernetSystem>> PlanetAethernet = new()
        {
            // Keys must match CosmicMoonRegistry.*.TerritoryId — validated in CosmicMoonContent.ValidateRegistry()
            [CosmicMoonRegistry.Sinus.TerritoryId] = new()
            {
                new()
                {
                    MapSelector = 0,
                    AethernetId = 2015100,
                    LandZone = new(-2.66f, 1.64f, -29.62f),
                    Location = new(-3.80f, 1.64f, -32.15f),
                },
                new()
                {
                    MapSelector = 1,
                    AethernetId = 2015053,
                    LandZone = new(-540.94f, 59.55f, -529.95f),
                    Location = new(-540.00f, 59.55f, -526.75f),
                },
                new()
                {
                    MapSelector = 2,
                    AethernetId = 2015054,
                    LandZone = new(429.26f, 45.55f, 499.95f),
                    Location = new(427.23f, 45.55f, 497.46f),
                },
                new()
                {
                    MapSelector = 3,
                    AethernetId = 2015055,
                    LandZone = new(-621.24f, 50.55f, 396.78f),
                    Location = new(-619.56f, 50.55f, 399.77f),
                },
                new()
                {
                    MapSelector = 4,
                    AethernetId = 2015056,
                    LandZone = new(631.38f, -73.95f, -572.59f),
                    Location = new(629.80f, -73.95f, -572.78f),
                }
            },
            [CosmicMoonRegistry.Phaenna.TerritoryId] = new()
            {
                new()
                {
                    MapSelector = 0,
                    AethernetId = 2015057,
                    Location = new(336.26f, 52.64f, -381.73f),
                    LandZone = new(337.50f, 52.64f, -383.00f),
                },
                new()
                {
                    MapSelector = 1,
                    AethernetId = 2015058,
                    Location = new(-150.82f, 29.05f, 314.44f),
                    LandZone = new(-151.47f, 29.05f, 313.33f),
                },
                new()
                {
                    MapSelector = 2,
                    AethernetId = 2015059,
                    Location = new(-640.11f, -1.45f, -627.05f),
                    LandZone = new(-640.05f, -1.45f, -628.36f),
                },
                new()
                {
                    MapSelector = 3,
                    AethernetId = 2015060,
                    Location = new(672.88f, -241.50f, 422.65f),
                    LandZone = new(673.69f, -241.86f, 424.29f),
                },
                new()
                {
                    MapSelector = 4,
                    AethernetId = 2015061,
                    Location = new(-590.90f, 28.50f, 722.25f),
                    LandZone = new(-591.95f, 28.50f, 722.10f),
                }
            },
            [CosmicMoonRegistry.Oizys.TerritoryId] = new()
            {
                new()
                {
                    MapSelector = 0,
                    AethernetId = 2015062,
                    Location = new(-164.10f, 0.50f, 83.50f),
                    LandZone = new(-165.44f, 0.38f, 84.41f),
                },
                new()
                {
                    MapSelector = 1,
                    AethernetId = 2015063,
                    Location = new(-141.20f, -74.95f, -591.17f),
                    LandZone = new(-140.56f, -74.95f, -589.59f),
                },
                new()
                {
                    MapSelector = 2,
                    AethernetId = 2015064,
                    Location = new(-454.18f, 102.05f, 760.83f),
                    LandZone = new(-453.34f, 102.05f, 761.97f),
                },
                new()
                {
                    MapSelector = 3,
                    AethernetId = 2015065,
                    Location = new(733.85f, 218.80f, -100.98f),
                    LandZone = new(732.61f, 218.80f, -101.53f),
                },
                new()
                {
                    MapSelector = 4,
                    AethernetId = 2015066,
                    Location = new(-124.6f, -193.7f, -801.46f),
                    LandZone = new(-123.94f, -193.70f, -802.01f),
                    RequiredLogLv = 14,
                }
            },
            [CosmicMoonRegistry.Auxesia.TerritoryId] = new()
            {
                new() // The Timberlodge (hub)
                {
                    MapSelector = 0,
                    AethernetId = 2015422,
                    Location = new(259.8f, 205.64f, 356.3f),
                    LandZone = new(260.9f, 205.6f, 355.6f)
                },
                new() // Full Bloom Gardens
                {
                    MapSelector = 1,
                    AethernetId = 2015423,
                    Location = new(-226.37f, 145.01f, -560.4f),
                    LandZone = new(-226.0f, 145.0f, -559.3f)
                },
                new() // Pileus Pergola
                {
                    MapSelector = 2,
                    AethernetId = 2015424,
                    Location = new(-242.73f, 168.05f, 321.17f),
                    LandZone = new(-243.1f, 168.0f, 320.6f)
                }
            }
        };
        private static Task? _PathCalculations = null;

        public static void Enqueue_NavmeshTask(Vector3 destination, bool waitForBusy = true, float distance = 2.0f)
        {
            if (P.Navmesh.Installed)
            {
                P.TaskManager.InsertMulti
                (
                    new(() => Paths_Clear(), "Clearing all Navmesh Paths"),
                    new(() => CalculateAethernet(destination), "Calculating Aethernet Path"),
                    new(() => CalculateHub(destination), "Calculating Hub Path"),
                    new(() => CalculateDirect(destination), "Calculating Direct Path"),
                    new(() => CalculateHubAethernet(destination), "Calculate Hub Aetheryte Travel"),
                    new(() => FindBestTravel(destination, waitForBusy, distance), "Finding best pathing method")
                );
            }
            else
            {
                IceLogging.Verbose("Navmesh was not installed, so we're not even going to attempt to move", "Enqueue Navmesh");
            }
        }
        public static void Enqueue_RedAlertNavmesh(Vector3 destination, bool waitForBusy = true, float distance = 2.0f, uint missionId = 0)
        {
            if (P.Navmesh.Installed)
            {
                P.TaskManager.InsertMulti
                (
                    new(() => Paths_Clear(), "Clearing all Navmesh Paths"),
                    new(() => CalculateDirect(destination), "Calculating Direct Path"),
                    new(() => CalculateRedAlert(missionId, destination), "Calculating Red Alert Direct Travel [Direct -> NPC -> RE]"),
                    new(() => CalculateRedAlertHub(missionId, destination), "Calculating Red Alert Hub Travel [Hub -> NPC -> RE]"),
                    new(() => FindBestTravel(destination, waitForBusy, distance, missionId))
                );
            }
            else
            {
                IceLogging.Verbose("Navmesh was not installed, so we're not even going to attempt to move", "Enqueue Navmesh");
            }
        }

        private static bool? Paths_Clear()
        {
            foreach (var path in TravelMethods)
            {
                path.Value.pathTo = null;
                path.Value.pathFrom = null;
                path.Value.destination = Vector3.Zero;
                path.Value.distance = 0;
                path.Value.Aethernet_TravelTo = 0;
                path.Value.Aethernet_TravelFrom = 0;
            }
            return true;
        }

        public static Dictionary<uint, uint> PlanetProgress { get; } =
            CosmicMoonRegistry.All.ToDictionary(m => m.TerritoryId, m => m.DefaultAethernetLogLevel);

        private static bool? CalculateAethernet(Vector3 destination)
        {
            string tag = "Navmesh: Aethernet Calculation";
            var territoryId = Player.Territory.RowId;
            var planetProgress = PlanetProgress[territoryId];

            if (planetProgress == 0)
            {
                if (GenericHelpers.TryGetAddonMaster<WKSHistoryBoard>("WKSHistoryBoard", out var progress) && progress.IsAddonReady)
                {
                    PlanetProgress[territoryId] = progress.NumEntries;
                    IceLogging.Info($"We've updated the entried to contain the following value: {PlanetProgress[territoryId]}");
                    if (EzThrottler.Throttle("Closing addon"))
                        GenericHandlers.FireCallback("WKSHistoryBoard", true, -1);
                }
                else if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Opening Progress Hud"))
                    {
                        hud.Infrastructor();
                    }
                }

                return false;
            }

            var territory = Player.Territory.RowId;
            if (!PlanetAethernet.TryGetValue(territory, out var aetherList))
            {
                IceLogging.Info("No valid aethernets, returning", tag);
                return true;
            }

            if (!C.UseAethernet)
            {
                IceLogging.Info("We have aethernet travel turned off, so continuing");
                return true;
            }

            var closestAetheryte = aetherList
                .Where(x => x.RequiredLogLv <= planetProgress)
                .OrderBy(x => Player.DistanceTo(x.Location)).FirstOrDefault();
            var destinationAetheryte = aetherList
                .Where(x => x.RequiredLogLv <= planetProgress)
                .OrderBy(x => Vector3.Distance(x.Location, destination)).FirstOrDefault();

            if (closestAetheryte == null || destinationAetheryte == null)
            {
                IceLogging.Info("Was not able to find a valid aetheryte for either going to or destination, continuing", tag);
                return true;
            }

            if (closestAetheryte.Location == destinationAetheryte.Location)
            {
                IceLogging.Info("Both aetherytes were the same ID/Location, so we don't need to take one, continuing", tag);
                return true;
            }

            var aethernet = TravelMethods[TravelTypes.Aethernet];

            var playerPosition = Player.Position;

            // Starting the distance calculations here
            if (_PathCalculations == null)
            {
                _PathCalculations = Task.Run(async () =>
                {
                    aethernet.pathTo = await FindPath(playerPosition, closestAetheryte.LandZone);
                    aethernet.pathFrom = await FindPath(destinationAetheryte.LandZone, destination);
                });
                if (EzThrottler.Throttle("Started task"))
                    IceLogging.Verbose("Started to calculate path", tag);
                return false; // Keep checking
            }

            // Wait for completion
            if (!_PathCalculations.IsCompleted)
            {
                if (EzThrottler.Throttle("Calculating path message", 1000))
                {
                    IceLogging.Verbose("Still calculating path that would be between aetherytes (via navmesh)", tag);
                }

                return false; // Still calculating
            }

            // Done!
            _PathCalculations = null; // Reset for next use
            float distance = 0;
            if (aethernet.pathTo != null)
            {
                for (int i = 0; i < aethernet.pathTo.Count - 1; i++)
                {
                    var start = aethernet.pathTo[i];
                    var end = aethernet.pathTo[i + 1];

                    distance += Vector3.Distance(start, end);
                }
            }

            if (aethernet.pathFrom != null)
            {
                for (int i = 0; i < aethernet.pathFrom.Count - 1; i++)
                {
                    var start = aethernet.pathFrom[i];
                    var end = aethernet.pathFrom[i + 1];

                    distance += Vector3.Distance(start, end);
                }
            }

            // Both legs must be pathable — a null leg would otherwise make this route look artificially short
            // (same guard as CalculateHubAethernet).
            if (distance > 0 && aethernet.pathTo != null && aethernet.pathFrom != null)
            {
                aethernet.distance = distance;
                aethernet.Aethernet_TravelTo = closestAetheryte.AethernetId;
                aethernet.Aethernet_TravelFrom = destinationAetheryte.AethernetId;
            }

            return true;
        }
        private static bool? CalculateDirect(Vector3 destination)
        {
            string tag = "[Navmesh: Calculate Direct]";
            var method = TravelMethods[TravelTypes.Direct];

            var playerPosition = Player.Position;

            if (_PathCalculations == null)
            {
                _PathCalculations = Task.Run(async () =>
                {
                    method.pathTo = await FindPath(playerPosition, destination);
                });
                if (EzThrottler.Throttle("Started task"))
                    IceLogging.Verbose("Started to calculate path", tag);
                return false; // Keep checking
            }

            // Wait for completion
            if (!_PathCalculations.IsCompleted)
            {
                if (EzThrottler.Throttle("Calculating path message", 1000))
                {
                    IceLogging.Verbose("Still calculating path that would be direct (via navmesh)", tag);
                }

                return false; // Still calculating
            }

            // Done!
            _PathCalculations = null; // Reset for next use
            if (method.pathTo != null)
            {
                float distance = 0;

                for (int i = 0; i < method.pathTo.Count - 1; i++)
                {
                    var start = method.pathTo[i];
                    var end = method.pathTo[i + 1];

                    distance += Vector3.Distance(start, end);
                }

                if (distance > 0)
                {
                    method.distance = distance;
                }
            }
            IceLogging.Info($"Direct Pathing Complete", tag);
            return true;
        }
        private static bool? CalculateRedAlert(uint missionId, Vector3 destination)
        {
            string tag = "Navmesh: Position -> Red Alert";
            var territoryId = Player.Territory.RowId;

            var criticalKey = CosmicHelper.SheetMissionDict[missionId].Critical_MapKey;

            if (!C.UseRedAlertNpc)
            {
                IceLogging.Info("We were told not to use the red alert NPC travel method, so we're going to just nope out of here", tag);
                return true;
            }
            else if (NpcData.MoonNpcs.TryGetValue(territoryId, out var planetInfo))
            {
                if (planetInfo.TryGetValue(NpcData.NpcType.RedAlert, out var npcInfo))
                {
                    if (!GatheringUtil.CriticalSpots.TryGetValue(criticalKey, out var approxStart))
                    {
                        IceLogging.Warning($"No red-alert turn-in coords for critical route: {criticalKey} on {CosmicMoonRegistry.GetDisplayName(territoryId)} — add to RedAlert_Selection", tag);
                        return true;
                    }

                    var method = TravelMethods[TravelTypes.RedAlert];

                    if (_PathCalculations == null)
                    {
                        var start = Player.Position;

                        _PathCalculations = Task.Run(async () =>
                        {
                            method.pathTo = await FindPath(start, npcInfo.Location_Circle);
                            method.pathFrom = await FindPath(approxStart.WorldCords, destination);
                        });
                        if (EzThrottler.Throttle("Started task: Direct"))
                            IceLogging.Verbose("Started to calculate path", tag);
                        return false;
                    }

                    if (!_PathCalculations.IsCompleted)
                    {
                        if (EzThrottler.Throttle("Calculating path message", 1000))
                            IceLogging.Verbose("Still calculating path for red alert (via navmesh)", tag);

                        return false; // Still calculating
                    }

                    // Done!
                    _PathCalculations = null; // Reset for next use
                    float distance = 0;
                    if (method.pathTo != null && method.pathTo.Count > 1)
                    {
                        for (int i = 0; i < method.pathTo.Count - 1; i++)
                        {
                            var start = method.pathTo[i];
                            var end = method.pathTo[i + 1];
                            distance += Vector3.Distance(start, end);
                        }
                    }

                    if (method.pathFrom != null && method.pathFrom.Count > 1)
                    {
                        for (int i = 0; i < method.pathFrom.Count - 1; i++)
                        {
                            var start = method.pathFrom[i];
                            var end = method.pathFrom[i + 1];
                            distance += Vector3.Distance(start, end);
                        }
                    }

                    if (distance > 0 && method.pathTo != null && method.pathFrom != null)
                    {
                        method.distance = distance;
                    }

                    IceLogging.Info($"Hub -> Aethernet Complete", tag);
                    return true;
                }
                else
                {
                    IceLogging.Error("This territory doesn't exist... which means I fucked up and haven't added it, or it's the new planet. Lmk", tag);
                }
            }
            else
            {
                IceLogging.Error("This territory doesn't exist... which means I fucked up and haven't added it, or it's the new planet. Lmk", tag);
            }

            return true;
        }
        private static bool? CalculateHub(Vector3 destination)
        {
            string tag = "[Navmesh: Calculate Hub Path]";

            if (CosmicMoonRegistry.TryGetHubCenter(Player.Territory.RowId, out var HubCenter))
            {
                var method = TravelMethods[TravelTypes.Hub_Return];

                if (!C.UseHubReturn)
                {
                    IceLogging.Info("We were told not to use hub return, so we're going to respect your decision", tag);
                }

                if (Player.DistanceTo(HubCenter) > C.HubReturn_Distance)
                {
                    if (_PathCalculations == null)
                    {
                        _PathCalculations = Task.Run(async () =>
                        {
                            TravelMethods[TravelTypes.Hub_Return].pathTo = await FindPath(HubCenter, destination);
                        });
                        if (EzThrottler.Throttle("Started task"))
                            IceLogging.Verbose("Started to calculate path", tag);
                        return false; // Keep checking
                    }

                    // Wait for completion
                    if (!_PathCalculations.IsCompleted)
                    {
                        if (EzThrottler.Throttle("Calculating path message", 1000))
                        {
                            IceLogging.Verbose("Still calculating path that would be direct (via navmesh)", tag);
                        }

                        return false; // Still calculating
                    }

                    // Done!
                    _PathCalculations = null; // Reset for next use
                    if (method.pathTo != null)
                    {
                        float distance = 0;

                        for (int i = 0; i < method.pathTo.Count - 1; i++)
                        {
                            var start = method.pathTo[i];
                            var end = method.pathTo[i + 1];

                            distance += Vector3.Distance(start, end);
                        }

                        if (distance > 0)
                        {
                            distance *= 1.2f;
                            method.distance = distance;
                        }
                    }
                    IceLogging.Info("HubPath Calculations Complete", tag);
                    return true;
                }
                else
                {
                    IceLogging.Info("We're within walking distance of the hub currently, so teleporting seems... reduntant to say the least. \n" +
                        "Going to just in turn set this to 0");
                    method.distance = 0;
                    return true;
                }
            }
            else
            {
                IceLogging.Error("No hub center is currently recorded. So we're just gonna skip this", tag);
                return true;
            }
        }
        private static bool? CalculateHubAethernet(Vector3 destination)
        {
            string tag = "[Navmesh: Calculate Hub -> Aethernet]";
            var territoryId = Player.Territory.RowId;
            var planetProgress = PlanetProgress[territoryId];

            if (CosmicMoonRegistry.TryGetHubCenter(Player.Territory.RowId, out var HubCenter))
            {
                var method = TravelMethods[TravelTypes.Hub_Aethernet];

                if (!C.UseHubReturn)
                {
                    IceLogging.Info("We were told no hub return, so not going to do so", tag);
                    return true;
                }
                if (!C.UseAethernet)
                {
                    IceLogging.Info("We were told no teleporting via aethernet, so we shall respect this decision");
                    return true;
                }

                if (Player.DistanceTo(HubCenter) > C.HubReturn_Distance)
                {
                    var territory = Player.Territory.RowId;
                    if (!PlanetAethernet.TryGetValue(territory, out var aetherList))
                    {
                        IceLogging.Info("No valid aethernets, returning", tag);
                        return true;
                    }

                    var closestAetheryte = aetherList.OrderBy(x => Vector3.Distance(HubCenter, x.Location)).FirstOrDefault();
                    var destinationAetheryte = aetherList
                        .Where(x => x.RequiredLogLv <= planetProgress)
                        .OrderBy(x => Vector3.Distance(x.Location, destination)).FirstOrDefault();

                    if (closestAetheryte == null || destinationAetheryte == null)
                    {
                        IceLogging.Info("Was not able to find a valid aetheryte for either going to or destination, continuing", tag);
                        return true;
                    }

                    if (closestAetheryte.Location == destinationAetheryte.Location)
                    {
                        IceLogging.Info("Both aetherytes were the same ID/Location, so we don't need to take one, continuing", tag);
                        return true;
                    }

                    if (_PathCalculations == null)
                    {
                        IceLogging.Verbose($"Hub Center: {HubCenter}\n" +
                            $"Closest Aetheryte: {closestAetheryte.LandZone}\n" +
                            $"Destination Aetheryte: {destinationAetheryte.LandZone}\n" +
                            $"Destination: {destination}", tag);

                        _PathCalculations = Task.Run(async () =>
                        {
                            method.pathTo = await FindPath(HubCenter, closestAetheryte.LandZone);
                            method.pathFrom = await FindPath(destinationAetheryte.LandZone, destination);
                        });
                        if (EzThrottler.Throttle("Started task: Direct"))
                            IceLogging.Verbose("Started to calculate path", tag);
                        return false; // Keep checking
                    }

                    // Wait for completion
                    if (!_PathCalculations.IsCompleted)
                    {
                        if (EzThrottler.Throttle("Calculating path message", 1000))
                        {
                            IceLogging.Verbose("Still calculating path that would be direct (via navmesh)", tag);
                        }

                        return false; // Still calculating
                    }

                    // Done!
                    _PathCalculations = null; // Reset for next use
                    float distance = 0;
                    if (method.pathTo != null && method.pathTo.Count > 1)
                    {
                        for (int i = 0; i < method.pathTo.Count - 1; i++)
                        {
                            var start = method.pathTo[i];
                            var end = method.pathTo[i + 1];
                            distance += Vector3.Distance(start, end);
                        }
                    }

                    if (method.pathFrom != null && method.pathFrom.Count > 1)
                    {
                        for (int i = 0; i < method.pathFrom.Count - 1; i++)
                        {
                            var start = method.pathFrom[i];
                            var end = method.pathFrom[i + 1];
                            distance += Vector3.Distance(start, end);
                        }
                    }

                    if (distance > 0 && method.pathTo != null && method.pathFrom != null)
                    {
                        method.distance = distance;
                        method.Aethernet_TravelTo = closestAetheryte.AethernetId;
                        method.Aethernet_TravelFrom = destinationAetheryte.AethernetId;
                    }

                    IceLogging.Info($"Hub -> Aethernet Complete", tag);
                    return true;
                }
                else
                {
                    IceLogging.Info("Player distance to the hub center is more than 50, which seems reduntant to cast a hub return in there, so going to just set this to 0", tag);
                    method.distance = 0;
                    return true;
                }
            }
            else
            {
                IceLogging.Error("No hub center is currently recorded. So we're just gonna skip this", tag);
                return true;
            }
        }
        private static bool? CalculateRedAlertHub(uint missionId, Vector3 destination)
        {
            string tag = "Navmesh: Calculate Hub -> Red Alert";
            var territoryId = Player.Territory.RowId;
            var method = TravelMethods[TravelTypes.Hub_RedAlert];

            var criticalKey = CosmicHelper.SheetMissionDict[missionId].Critical_MapKey;

            if (!C.UseHubReturn)
                return true;

            if (!C.UseRedAlertNpc)
                return true;

            if (!CosmicHelper.SheetMissionDict[missionId].IsCritical)
                return true;

            if (!GatheringUtil.CriticalSpots.TryGetValue(criticalKey, out var criticalInfo))
            {
                IceLogging.Warning($"No red-alert turn-in coords for mission {missionId} — hub return via NPC skipped");
                return true;
            }

            if (CosmicMoonRegistry.TryGetHubCenter(Player.Territory.RowId, out var HubCenter))
            {
                if (NpcData.MoonNpcs.TryGetValue(territoryId, out var planetInfo))
                {
                    if (planetInfo.TryGetValue(NpcData.NpcType.RedAlert, out var npcInfo))
                    {
                        if (Player.DistanceTo(HubCenter) > C.HubReturn_Distance)
                        {
                            if (_PathCalculations == null)
                            {
                                IceLogging.Verbose("Doing the following calculations:\n" +
                                    "Hub Center -> Npc\n" +
                                    "Npc -> Teleport spot\n" +
                                    "Teleport spot -> Destination");

                                _PathCalculations = Task.Run(async () =>
                                {
                                    method.pathTo = await FindPath(HubCenter, npcInfo.Location_Circle);
                                    method.pathFrom = await FindPath(criticalInfo.WorldCords, destination);
                                });
                                if (EzThrottler.Throttle("Started task: Direct"))
                                    IceLogging.Verbose("Started to calculate path", tag);
                                return false; // Keep checking
                            }

                            if (!_PathCalculations.IsCompleted)
                            {
                                if (EzThrottler.Throttle("Calculating path message", 1000))
                                    IceLogging.Verbose("Still calculating path for red alert (via navmesh)", tag);

                                return false; // Still calculating
                            }

                            // Done!
                            _PathCalculations = null; // Reset for next use
                            float distance = 0;
                            if (method.pathTo != null && method.pathTo.Count > 1)
                            {
                                for (int i = 0; i < method.pathTo.Count - 1; i++)
                                {
                                    var start = method.pathTo[i];
                                    var end = method.pathTo[i + 1];
                                    distance += Vector3.Distance(start, end);
                                }
                            }

                            if (method.pathFrom != null && method.pathFrom.Count > 1)
                            {
                                for (int i = 0; i < method.pathFrom.Count - 1; i++)
                                {
                                    var start = method.pathFrom[i];
                                    var end = method.pathFrom[i + 1];
                                    distance += Vector3.Distance(start, end);
                                }
                            }

                            if (distance > 0 && method.pathTo != null && method.pathFrom != null)
                            {
                                method.distance = distance * 1.2f;
                            }

                            IceLogging.Info($"Hub -> Aethernet Complete", tag);
                            return true;
                        }
                    }
                    else
                    {
                        IceLogging.Error("This territory doesn't exist... which means I fucked up and haven't added it, or it's the new planet. Lmk", tag);
                    }
                }
                else
                {
                    IceLogging.Error("This territory doesn't exist... which means I fucked up and haven't added it, or it's the new planet. Lmk", tag);
                }
            }
            else
            {
                IceLogging.Error("We don't have any record for this hub center, so we're going to ignore it for now", tag);
            }

            return true;
        }
        private static bool? FindBestTravel(Vector3 destination, bool waitForBusy, float distance, uint missionId = 0)
        {
            var bestTravel = TravelMethods.Where(x => x.Value.distance != 0)
                .Where(x => !C.AvoidStellarReturn || (x.Key != TravelTypes.Hub_Return && x.Key != TravelTypes.Hub_Aethernet))
                .OrderBy(x => x.Value.distance)
                .FirstOrDefault();

            foreach(var travelKind in TravelMethods)
            {
                IceLogging.Verbose($"[{travelKind.Key}] = {travelKind.Value.distance}");
            }

            if (bestTravel.Key == TravelTypes.Aethernet)
            {
                var targetAether = bestTravel.Value.Aethernet_TravelTo;
                var AethernetLoc = PlanetAethernet[Player.Territory.RowId].Where(x => x.AethernetId == targetAether).FirstOrDefault();
                P.TaskManager.InsertMulti
                    (
                        new(() => DestinationPathing(AethernetLoc.LandZone), "Traveling to Aetheryte"),
                        new(() => TravelToAethershard(bestTravel.Value), "Traveling Via Aethernet"),
                        new(() => DestinationPathing(destination, waitForBusy, distance, mountBeforeMove: true), "Pathing to our destination: Aethernet")
                    );
            }
            else if (bestTravel.Key == TravelTypes.Hub_Return)
            {
                P.TaskManager.InsertMulti
                    (
                        new(() => Task_Repair.HubCheck(), "Returning back to hub"),
                        new(() => DestinationPathing(destination, waitForBusy, distance), "Pathing to our destination: Hub Return")
                    );
            }
            else if (bestTravel.Key == TravelTypes.Hub_Aethernet)
            {
                var targetAether = bestTravel.Value.Aethernet_TravelTo;
                var AethernetLoc = PlanetAethernet[Player.Territory.RowId].Where(x => x.AethernetId == targetAether).FirstOrDefault();
                P.TaskManager.InsertMulti
                    (
                        new(() => Task_Repair.HubCheck(), "Returning back to hub"),
                        new(() => DestinationPathing(AethernetLoc.LandZone), "Traveling to the hub aetheryte"),
                        new(() => TravelToAethershard(bestTravel.Value), "Travel Via Aethernet"),
                        new(() => DestinationPathing(destination, waitForBusy, distance, mountBeforeMove: true), "Pathing to our destination: HubAethernet")
                    );
            }
            else if (bestTravel.Key == TravelTypes.RedAlert)
            {
                if (!NpcData.TryGetNpc(Player.Territory.RowId, NpcData.NpcType.RedAlert, out var redAlertNpc))
                {
                    IceLogging.Warning($"No red-alert NPC configured for {CosmicMoonRegistry.GetDisplayName(Player.Territory.RowId)}");
                    P.TaskManager.Insert(() => DestinationPathing(destination, waitForBusy, distance), "Pathing to our destination: Basic");
                    return true;
                }

                P.TaskManager.InsertMulti
                    (
                        new(() => DestinationPathing(redAlertNpc.Location_Circle), "Traveling to the Red Alert NPC"),
                        new(() => TravelToRedAlertNpc(redAlertNpc, missionId), "Interacting + Traveling Via RedAlert NPC"),
                        new(() => DestinationPathing(destination, waitForBusy, distance), "Pathing to our destination: Red Alert")
                    );
            }
            else if (bestTravel.Key == TravelTypes.Hub_RedAlert)
            {
                if (!NpcData.TryGetNpc(Player.Territory.RowId, NpcData.NpcType.RedAlert, out var redAlertNpc))
                {
                    IceLogging.Warning($"No red-alert NPC configured for {CosmicMoonRegistry.GetDisplayName(Player.Territory.RowId)}");
                    P.TaskManager.Insert(() => DestinationPathing(destination, waitForBusy, distance), "Pathing to our destination: Basic");
                    return true;
                }

                P.TaskManager.InsertMulti
                    (
                        new(() => Task_Repair.HubCheck(), "Returning back to hub"),
                        new(() => DestinationPathing(redAlertNpc.Location_Circle), "Traveling to the Red Alert NPC"),
                        new(() => TravelToRedAlertNpc(redAlertNpc, missionId), "Interacting + Traveling Via RedAlert NPC"),
                        new(() => DestinationPathing(destination, waitForBusy, distance), "Pathing to our destination: Red Alert")
                    );
            }
            else
            {
                P.TaskManager.Insert(() => DestinationPathing(destination, waitForBusy, distance), "Pathing to our destination: Basic");
            }

            return true;
        }

        private static unsafe bool? TravelToAethershard(PathInfo shardInfo)
        {
            string tag = "[Navmesh: Aethershard movement]";

            uint targetId = shardInfo.Aethernet_TravelTo;
            uint destinationId = shardInfo.Aethernet_TravelFrom;

            int menuId = 0; // TODO: Actually code this in, esentially grab the destationId -> grab the submenuID

            var territoryId = Player.Territory.RowId;
            var targetAether = PlanetAethernet[territoryId].Where(x => x.AethernetId == targetId).FirstOrDefault();
            var destinationAether = PlanetAethernet[territoryId].Where(x => x.AethernetId == destinationId).FirstOrDefault();

            menuId = destinationAether.MapSelector;

            if (Player.DistanceTo(targetAether.Location) < 5)
            {
                void InteractWithShard()
                {
                    if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("TelepotTown", out var TelepotTown) && TelepotTown->IsReady)
                    {
                        if (EzThrottler.Throttle("Use Aethernet", 100))
                        {
                            GenericHandlers.FireCallback("TelepotTown", true, 11, menuId);
                        }
                    }
                    else
                    {
                        var aethernet = Svc.Objects.Where(x => x.BaseId == targetId).FirstOrDefault();
                        if (aethernet != null)
                        {
                            if (Player.Mounted || Player.IsJumping)
                            {
                                Utils.Dismount();
                                return;
                            }
                            else if (!Player.IsBusy)
                            {
                                Utils.TargetgameObject(aethernet);
                                Utils.InteractWithObject(aethernet);
                            }
                        }
                    }
                }

                if (C.Delay_Aethernet)
                {
                    int delay = random.Next(1999, 6001);
                    if (EzThrottler.Throttle("Delay for npc travel", delay))
                    {
                        randomCounter += 1;
                    }

                    if (randomCounter < 2)
                    {
                        if (EzThrottler.Throttle("Wait for random encounter"))
                            IceLogging.Verbose("Waiting for the random timer to fully randomize");

                        return false;
                    }
                    else
                    {
                        if (EzThrottler.Throttle("Interact with shard"))
                            InteractWithShard();
                    }
                }
                else
                {
                    InteractWithShard();
                }
            }
            else if (Player.DistanceTo(destinationAether.Location) < 10)
            {
                if (!PlayerHelper.IsScreenReady())
                {
                    return false;
                }
                else
                {
                    IceLogging.Info("We've reached our destination!", tag);
                    return true;
                }
            }

            return false;
        }
        private static unsafe bool? TravelToRedAlertNpc(NpcData.NPCInfo redAlertNpc, uint missionId)
        {
            string tag = "Travel: Via RedAlert NPC";

            IceLogging.Verbose("Travel via Npc commenced", tag);
            var criticalKey = CosmicHelper.SheetMissionDict[missionId].Critical_MapKey;

            if (GatheringUtil.CriticalSpots.TryGetValue(criticalKey, out var criticalInfo))
            {
                if (Player.DistanceTo(redAlertNpc.Location_Circle) < 5)
                {
                    if (EzThrottler.Throttle("Close enough log"))
                        IceLogging.Verbose("Close enough to npc to travel", tag);

                    if (C.Delay_Aethernet)
                    {
                        int delay = random.Next(1999, 6001);
                        if (EzThrottler.Throttle("Delay for npc travel", delay))
                        {
                            randomCounter += 1;
                        }

                        if (randomCounter < 2)
                        {
                            if (EzThrottler.Throttle("Wait for random encounter"))
                                IceLogging.Verbose("Waiting for the random timer to fully randomize");

                            return false;
                        }
                    }

                    if (GenericHelpers.TryGetAddonMaster<SelectString>(out var selectString) && selectString.IsAddonReady)
                    {
                        if (EzThrottler.Throttle("Selecting teleport option"))
                        {
                            IceLogging.Verbose($"Selecting Option: {criticalInfo.NpcSelector} for mission: {missionId}", tag);
                            selectString.Entries[criticalInfo.NpcSelector].Select();
                        }
                    }
                    else if (GenericHelpers.TryGetAddonMaster<SelectYesno>(out var yesNo) && yesNo.IsAddonReady)
                    {
                        if (EzThrottler.Throttle("Selecting yes"))
                            yesNo.Yes();
                    }
                    else if (GenericHelpers.TryGetAddonMaster<Talk>(out var talk) && talk.IsAddonReady)
                    {
                        if (EzThrottler.Throttle("Clicking on talk", 100))
                        {
                            talk.Click();
                        }
                    }
                    else
                    {
                        if (EzThrottler.Throttle("Log Message"))
                            IceLogging.Verbose("Should be interacting here...", tag);

                        var npc = Svc.Objects.Where(x => x.BaseId == redAlertNpc.NpcId).FirstOrDefault();
                        if (npc != null)
                        {
                            if (Player.Mounted || Player.IsJumping)
                            {
                                Utils.Dismount();
                                return false;
                            }

                            if (EzThrottler.Throttle("Target + Interact"))
                            {
                                Utils.TargetgameObject(npc);
                                Utils.InteractWithObject(npc);
                            }

                            return false;
                        }
                    }
                }
                else if (Player.DistanceTo(criticalInfo.WorldCords) < 75)
                {
                    if (!PlayerHelper.IsScreenReady())
                        return false;
                    else
                    {
                        randomCounter = 0;
                        IceLogging.Info("We've reached the red alert destination, need to just do the final pathing", tag);
                        return true;
                    }
                }
            }
            else
            {
                randomCounter = 0;
                return true;
            }

            return false;
        }
        private static bool? DestinationPathing(Vector3 pos, bool waitForBusy = true, float distance = 2.0f, bool stayMounted = false, Vector3? npcLoc = null, bool mountBeforeMove = false)
        {
            string tag = "Navmesh Move: Destination Pathing";

            if (!Task_NavTo(pos, waitForBusy, distance, stayMounted, npcLoc, mountBeforeMove:mountBeforeMove).Value)
            {
                return false;
            }
            else
            {
                IceLogging.Info($"We've reached our destination: {pos}", tag);
                return true;
            }
        }
        private static async Task<List<Vector3>> FindPath(Vector3 position, Vector3 destination)
        {
            return await P.Navmesh.Pathfind(position, destination, false);
        }

        #endregion

        #region Gathering Functions

        /// <summary>
        /// Converts Pictomancy coordinates to FFXIV world coordinates.
        /// Pictomancy: 0=South, 90=West, 180=North, 270=East
        /// FFXIV:      0=North, 90=East, 180=South, 270=West
        /// </summary>
        private static float PictomancyToFFXIV(float pictomancyAngle)
        {
            float ffxivAngle = (pictomancyAngle + 180f) % 360f;
            if (ffxivAngle < 0f)
                ffxivAngle += 360f;
            return ffxivAngle;
        }

        private static float CalculateAngleToPlayer(Vector3 nodePos, Vector3 playerPos)
        {
            Vector3 direction = playerPos - nodePos;
            float angle = MathF.Atan2(direction.X, direction.Z) * (180f / MathF.PI);
            angle = 180f - angle;

            if (angle < 0f)
                angle += 360f;
            else if (angle >= 360f)
                angle -= 360f;

            return angle;
        }

        private static float NormalizeAngle(float angle)
        {
            angle = angle % 360f;
            if (angle < 0f)
                angle += 360f;
            return angle;
        }

        private static float GetRangeSpan(float min, float max)
        {
            float diff = MathF.Abs(max - min);
            if (MathF.Abs(diff - 360f) < 0.01f)
                return 360f;

            min = NormalizeAngle(min);
            max = NormalizeAngle(max);

            float span = max - min;
            if (span < 0)
                span += 360f;

            return span;
        }

        private static bool IsAngleInRange(float angle, float min, float max)
        {
            angle = NormalizeAngle(angle);
            min = NormalizeAngle(min);
            max = NormalizeAngle(max);

            float rangeSpan = max - min;
            if (rangeSpan < 0)
                rangeSpan += 360f;

            if (rangeSpan >= 360f)
                return true;

            if (min <= max)
                return angle >= min && angle <= max;
            else
                return angle >= min || angle <= max;
        }

        private static float GetAngularDistance(float angle1, float angle2)
        {
            angle1 = NormalizeAngle(angle1);
            angle2 = NormalizeAngle(angle2);

            float diff = angle2 - angle1;
            while (diff > 180f) diff -= 360f;
            while (diff < -180f) diff += 360f;

            return MathF.Abs(diff);
        }

        private static float ClampAngleToRange(float angle, float allowedMin, float allowedMax, bool preferMin)
        {
            angle = NormalizeAngle(angle);

            if (IsAngleInRange(angle, allowedMin, allowedMax))
                return angle;

            float distToMin = GetAngularDistance(angle, allowedMin);
            float distToMax = GetAngularDistance(angle, allowedMax);

            if (MathF.Abs(distToMin - distToMax) < 0.01f)
                return preferMin ? allowedMin : allowedMax;

            return distToMin < distToMax ? allowedMin : allowedMax;
        }

        private static (float sectionMin, float sectionMax) GetNearestSection(float allowedMin, float allowedMax, float targetAngle, float sectionSize)
        {
            float rangeSpan = GetRangeSpan(allowedMin, allowedMax);

            if (rangeSpan >= 359.9f)
            {
                float halfSection = sectionSize / 2f;
                return (NormalizeAngle(targetAngle - halfSection), NormalizeAngle(targetAngle + halfSection));
            }

            allowedMin = NormalizeAngle(allowedMin);
            allowedMax = NormalizeAngle(allowedMax);
            targetAngle = NormalizeAngle(targetAngle);

            if (IsAngleInRange(targetAngle, allowedMin, allowedMax))
            {
                // Target is inside fan — center section on it as before
                float half = sectionSize / 2f;
                float secMin = ClampAngleToRange(NormalizeAngle(targetAngle - half), allowedMin, allowedMax, true);
                float secMax = ClampAngleToRange(NormalizeAngle(targetAngle + half), allowedMin, allowedMax, false);
                return (secMin, secMax);
            }
            else
            {
                // Target is outside fan — find nearest edge and carve inward
                bool nearMax = GetAngularDistance(targetAngle, allowedMax) < GetAngularDistance(targetAngle, allowedMin);

                if (nearMax)
                {
                    // Nearest edge is allowedMax, carve inward toward allowedMin
                    float secMin = ClampAngleToRange(NormalizeAngle(allowedMax - sectionSize), allowedMin, allowedMax, true);
                    return (secMin, allowedMax);
                }
                else
                {
                    // Nearest edge is allowedMin, carve inward toward allowedMax
                    float secMax = ClampAngleToRange(NormalizeAngle(allowedMin + sectionSize), allowedMin, allowedMax, false);
                    return (allowedMin, secMax);
                }
            }
        }

        private static float RandomAngleInRange(float min, float max)
        {
            min = NormalizeAngle(min);
            max = NormalizeAngle(max);

            if (min <= max)
                return NextFloat(min, max);

            float rangeSize = (360f - min) + max;
            return NormalizeAngle(min + NextFloat(0, rangeSize));
        }

        private static float NextFloat(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        private static Vector3 CalculateFanPosition(Vector3 center, float angleDegrees, float distance, float height)
        {
            float standardAngle = 180f - angleDegrees;
            float angleRadians = standardAngle * (MathF.PI / 180f);

            return new Vector3(
                center.X + distance * MathF.Sin(angleRadians),
                center.Y,
                center.Z + distance * MathF.Cos(angleRadians)
            );
        }

        #endregion

        private static void CloseExtraWindows()
        {
            if (EzThrottler.Throttle("Closing Extra Windows"))
            {
                if (GenericHelpers.TryGetAddonMaster<ShopExchangeCurrency>("ShopExchangeCurrency", out var shopExchange) && shopExchange.IsAddonReady)
                {
                    GenericHandlers.FireCallback("ShopExchangeCurrency", true, -1);
                }
            }
        }
    }
}