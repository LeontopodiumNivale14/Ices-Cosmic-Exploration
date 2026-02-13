using System.Collections.Generic;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.Cosmic_Helper;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Utilities;

public static partial class CosmicHelper
{
    public static CosmicInfo CurrentMissionInfo => SheetMissionDict[CurrentLunarMission];

    /// <summary>
    /// Gives the current mission that is active
    /// </summary>
    public static unsafe uint CurrentLunarMission
    {
        get
        {
            try
            {
                var manager = (WKSManagerCustom*)WKSManager.Instance();
                if (manager == null)
                    return 0; // or some default value

                return manager->CurrentMissionId;
            }
            catch (AccessViolationException)
            {
                IceLogging.Error("We're currently getting access violations with this, so returning 0");
                return 0;
            }
            catch (Exception)
            {
                IceLogging.Error("Welp. Somehow not getting it still. Exception exit");
                return 0;
            }
        }
    }
    public static unsafe uint? CurrentBait => WKSManager.Instance()->FishingBait;
    public static unsafe uint CurrentLunarDevelopment => ExcelHelper.DevGrade.GetRow(WKSManager.Instance()->DevGrade).Unknown6;

    public static Dictionary<int, string> ExpDictionary = new()
    {
        { 1, "I" },
        { 2, "II" },
        { 3, "III" },
        { 4, "IV" },
        { 5, "V" }
    };

    public static readonly Dictionary<uint, uint> PlanetCreditInfo = new()
    {
        [1237] = 45691, // sinus
        [1291] = 48146, // phaenna
        [1310] = 48147, // Oizys
        // [] = 48148, // moon 4
    };

    // General use functions used across the codebase, specifically tied to cosmic related functions
    public static void OpenStellarMission()
    {
        if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var missionInfo) && missionInfo.IsAddonReady)
            {
                return;
            }
            else
            {
                if (EzThrottler.Throttle("Opening Stellar Missions"))
                {
                    IceLogging.Debug("Opening Mission Menu");
                    hud.Mission();
                }
            }
        }
    }

    public static void UpdateStateFlags()
    {
        // just a shorthand for me to be able to grab all of it, while also just snapshotting the mission we're currently running
        var missionInfo = CurrentMissionInfo; 
        if (missionInfo.Attributes.HasFlag(MissionAttributes.Critical))
        {
            // TODO:
            // I really need to just add a collection point to the critical mission infomation
            // From here, grab the mission info
            // RedAlertCollectionPoint = infohere
        }
        if (missionInfo.Attributes.HasFlag(MissionAttributes.Craft))
            SchedulerMain.State |= IceState.Craft;
    }

    public unsafe static (int classScore, int cappedClassScore, int totalScores, uint classId) GetCosmicClassScores(bool useSelectedJob = false, uint jobId = 0)
    {
        int classScore = 0;
        int cappedClassScore = 0;
        int totalScores = 0;
        var wksManager = WKSManager.Instance();
        var currentMissionId = wksManager->CurrentMissionUnitRowId;

        uint classId;

        if (useSelectedJob)
        {
            // For UI that should respect C.SelectedJob
            classId = C.SelectedJob;
        }
        else
        {
            // For overlay that should show current/mission job
            if (currentMissionId > 0 && CosmicHelper.SheetMissionDict.TryGetValue(currentMissionId, out var missionInfo))
            {
                if (jobId != 0)
                    classId = jobId;
                else if (missionInfo.Jobs.Contains((uint)Player.Job))
                    classId = (uint)Player.Job;
                else
                    classId = missionInfo.Jobs.First();
            }
            else if (CosmicHelper.CrafterJobList.Contains((uint)Player.Job) || CosmicHelper.GatheringJobList.Contains((uint)Player.Job))
                classId = (uint)Player.Job;
            else
                classId = C.SelectedJob;
        }

        if (classId is >= 8 and <= 18)
        {
            var scores = wksManager->Scores;

            classScore = scores[(int)classId - 8];
            cappedClassScore = Math.Min(500_000, classScore);

            totalScores = 0;
            for (int i = 0; i < scores.Length; ++i)
                totalScores += Math.Min(500_000, scores[i]);
        }

        return (classScore, cappedClassScore, totalScores, classId);
    }
}
