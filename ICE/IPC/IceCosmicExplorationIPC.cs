using ECommons.EzIpcManager;
using ICE.Utilities.Cosmic_Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.IPC;

public class IceCosmicExplorationIPC
{
    public IceCosmicExplorationIPC()
    {
        EzIPC.Init(this);
    }

    /// <summary>
    /// Fires whenever a mission is completed.
    /// </summary>
    [EzIPCEvent("MissionCompleted")] public Action MissionCompleted = null!;

    // Function to fire off whenever I complete the mission
    public void NotifyMissionCompleted()
    {
        if (MissionCompleted == null)
        {
            IceLogging.Error("MissionCompleted IPC event was not initialized by EzIPC.Init");
            return;
        }

        try
        {
            MissionCompleted.Invoke();
        }
        catch (Exception ex)
        {
            IceLogging.Error($"A subscriber threw on MissionCompleted: {ex.Message}");
        }
    }


    [EzIPC] public bool IsRunning()
    {
        return SchedulerMain.State != IceState.Idle;
    }
    [EzIPC] public void Enable()
    {
        SchedulerMain.EnablePlugin();
    }
    [EzIPC] public void Disable()
    {
        IceLogging.Info("We were told to stop via IPC");
        SchedulerMain.DisablePlugin();
    }
    /// <summary>
    /// Adds the following missions to your mission list
    /// </summary>
    /// <param name="missionId"></param>
    [EzIPC] public void AddMissions(HashSet<uint> missionId)
    {
        foreach (var id in missionId)
        {
            C.MissionConfig[id].Enabled = true;
        }
        C.Save();
    }

    /// <summary>
    /// Disables/Removes the missions from being selected
    /// </summary>
    /// <param name="missionIds"></param>
    [EzIPC] public void RemoveMissions(HashSet<uint> missionIds)
    {
        foreach (var id in missionIds)
        {
            C.MissionConfig[id].Enabled = false;
        }
        C.Save();
    }

    /// <summary>
    /// Toggle the following missions states
    /// </summary>
    /// <param name="missionIds"></param>
    [EzIPC] public void ToggleMissions(HashSet<uint> missionIds)
    {
        foreach (var id in missionIds)
        {
            C.MissionConfig[id].Enabled = !C.MissionConfig[id].Enabled;
        }
        C.Save();
    }
    /// <summary>
    /// Sets only the missions in the hashset to enabled, everything else will be false
    /// </summary>
    /// <param name="missionIds"></param>
    [EzIPC] public void OnlyMissions(HashSet<uint> missionIds)
    {
        foreach (var mission in C.MissionConfig.Where(x => x.Value.Enabled))
        {
            mission.Value.Enabled = false;
        }

        foreach (var id in missionIds)
        {
            if (C.MissionConfig.TryGetValue(id, out var mission))
            {
                mission.Enabled = true;
            }
        }
        C.Save();
    }

    /// <summary>
    /// Clears all missions that you have enabled
    /// </summary>
    [EzIPC] public void ClearAllMissions()
    {
        foreach (var mission in C.MissionConfig)
        {
            mission.Value.Enabled = false;
        }
        C.Save();
    }

    /// <summary>
    /// Opens the map and flags/shows a radius of where the mission's gathering point is
    /// </summary>
    /// <param name="id"></param>
    [EzIPC] public void FlagMissionArea(uint id)
    {
        var info = CosmicHelper.SheetMissionDict.FirstOrDefault(x => x.Key == id);
        if (info.Value == default) return;
        if (info.Value.MarkerId == 0) return;

        Utils.SetGatheringRing(info.Value.TerritoryId, (int)info.Value.MapPosition.X, (int)info.Value.MapPosition.Y, info.Value.Radius, info.Value.Name);
    }

    /// <summary>
    /// General way of changing settings states. 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="state"></param>
    [EzIPC] public void ChangeSetting(string config, bool state)
    {
        IceLogging.Info($"Setting: {config}, state: {state}");
        switch (config)
        {
            case "OnlyGrabMission": C.OnlyGrabMission_Debug = state; break;
            case "StopAfterCurrent": Mission_Settings.StopAfterCurrent = state; break;
            case "StopOnceHitCosmoCredits": C.StopOnceHitCosmoCredits = state; break;
            case "StopOnceHitLunarCredits": C.StopOnceHitLunarCredits = state; break;
            case "XPRelicGrind": C.SelectedMode = ModeSelect.RelicMode; break;
            default: return;
        }
        C.Save();
    }

    /// <summary>
    /// Ability to change the amounts on certain things.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="amount"></param>
    [EzIPC] public void ChangeSettingAmount(string config, int amount)
    {
        switch (config)
        {
            case "CosmoCreditsCap": C.CosmoCreditsCap = amount; break;
            case "LunarCreditsCap": C.LunarCreditsCap = amount; break;
            default: return;
        }
        C.Save();
    }

    /// <summary>
    /// Returns the current state(s) that ICE is currently in a string format. 
    /// </summary>
    /// <returns></returns>
    [EzIPC] public string CurrentState()
    {
        return SchedulerMain.State.ToString();
    }

    [EzIPC] public uint CurrentMission()
    {
        return CosmicHelper.CurrentLunarMission;
    }
}
