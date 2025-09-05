using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ICE.Ui.Waypoint_Manager;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui;

internal class SettingsWindowV2 : Window
{
    public SettingsWindowV2() : 
        base($"I.C.E. Settings {P.GetType().Assembly.GetName().Version} ###ICESettingsWindowV2")
    {
        Flags = ImGuiWindowFlags.None;
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(100, 100),
            MaximumSize = new Vector2(2000, 2000),
        };
        P.windowSystem.AddWindow(this);
        AllowPinning = true;
    }

    public void Dispose()
    {
        P.windowSystem.RemoveWindow(this);
    }

    private string SelectedSetting = "Safety";
    private string[] SettingOptions = ["Safety", "Gathering", "Overlay", "Misc", "Gamble Wheel"];
    private string[] DebugOptions = ["Debug", "Path Creation"];

    public override void Draw()
    {
        float paddingX = 20f;
        float paddingY = 5f;
        Vector2 textSize = new Vector2(0.0f);

        foreach (var setting in SettingOptions)
        {
            Vector2 testText = ImGui.CalcTextSize(setting);
            
            if (testText.X > textSize.X)
            {
                textSize = testText;
            }
        }

#if DEBUG
        foreach (var setting in DebugOptions)
        {
            Vector2 testText = ImGui.CalcTextSize(setting);

            if (testText.X > textSize.X)
            {
                textSize = testText;
            }
        }
#endif

        Vector2 buttonSize = new Vector2(textSize.X + paddingX * 2, textSize.Y + paddingY * 2);

        // New child windows
        // LEFT PANEL
        if (ImGui.BeginChild("LeftPanel", new System.Numerics.Vector2(buttonSize.X + 20, 0), true))
        {
            foreach (var setting in SettingOptions)
            {
                if (ImGui.Button(setting, buttonSize))
                {
                    SelectedSetting = setting;
                }
            }

#if DEBUG
            foreach (var setting in DebugOptions)
            {
                if (ImGui.Button(setting, buttonSize))
                {
                    SelectedSetting = setting;
                }
            }
#endif

            ImGui.EndChild();
        }

        // RIGHT PANEL (same line so it appears to the right)
        ImGui.SameLine();

        if (ImGui.BeginChild("RightPanel", new System.Numerics.Vector2(0, 0), true)) // 0 width = fill remaining
        {
            if (SelectedSetting == SettingOptions[0])
                SafetySettings();
            else if (SelectedSetting == SettingOptions[1])
                GatherSettings();
            else if (SelectedSetting == SettingOptions[2])
                Overlay();
            else if (SelectedSetting == SettingOptions[3])
                Misc();
            else if (SelectedSetting == SettingOptions[4])
                GambaWheel();
#if DEBUG
            else if (SelectedSetting == DebugOptions[0])
                Debug();
            else if (SelectedSetting == DebugOptions[1])
                WaypointUi.WPUi();
#endif
            else
            {
                ImGui.Text($"Hmm... Maybe should put a dad joke here. For now it's empty.");
            }

            ImGui.EndChild();
        }

    }

    private bool animationLockAbandon = C.AnimationLockAbandon;
    private bool stopOnAbort = C.StopOnAbort;
    private bool rejectUnknownYesNo = C.RejectUnknownYesno;
    private bool delayGrabMission = C.DelayGrabMission;
    private int delayAmount = C.DelayIncrease;
    private bool delayCraft = C.DelayCraft;
    private int delayCraftAmount = C.DelayCraftIncrease;

    private void SafetySettings()
    {
        if (ImGui.Checkbox("[Experimental] Animation Lock Unstuck", ref animationLockAbandon))
        {
            C.AnimationLockAbandon = animationLockAbandon;
            C.Save();
        }
        ImGui.Checkbox("[Experimental] Animation Lock Manual Unstuck", ref SchedulerMain.AnimationLockAbandonState);

        if (ImGui.Checkbox("Stop on Errors", ref stopOnAbort))
        {
            C.StopOnAbort = stopOnAbort;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "Warning! This is a safety feature to stop if something goes wrong!\n" +
            "You have been warned. Disable at your own risk."
        );

        if (ImGui.Checkbox("Ignore non-Cosmic prompts", ref rejectUnknownYesNo))
        {
            C.RejectUnknownYesno = rejectUnknownYesNo;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "Warning! This is a safety feature to avoid joining random parties!\n" +
            "If you you uncheck this, YOU WILL JOIN random party invites.\n" +
            "You have been warned. Disable at your own risk."
        );
        if (ImGui.Checkbox("Add delay to mission menu", ref delayGrabMission))
        {
            C.DelayGrabMission = delayGrabMission;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "This is here for safety! If you want to decrease the delay between missions be my guest.\n" +
            "Safety is around... 250? If you're having animation locks you can absolutely increase it higher\n" +
            "Or if you're feeling daredevil. Lower it. I'm not your dad (will tell dad jokes though.");
        if (delayGrabMission)
        {
            ImGui.SetNextItemWidth(150);
            ImGui.SameLine();
            if (ImGui.SliderInt("ms###Mission", ref delayAmount, 0, 1000))
            {
                if (C.DelayIncrease != delayAmount)
                {
                    C.DelayIncrease = delayAmount;
                    C.Save();
                }
            }
        }
        if (ImGui.Checkbox("Add delay to crafting menu", ref delayCraft))
        {
            C.DelayCraft = delayCraft;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "This is here for safety! If you want to decrease the delay before turnin be my guest.\n" +
            "Safety is around... 2500? If you're having animation locks you can absolutely increase it higher\n" +
            "Or if you're feeling daredevil. Lower it. I'm not your dad (will tell dad jokes though.");
        if (delayCraft)
        {
            ImGui.SetNextItemWidth(150);
            ImGui.SameLine();
            if (ImGui.SliderInt("ms###Crafting", ref delayCraftAmount, 0, 10000))
            {
                if (C.DelayCraftIncrease != delayCraftAmount)
                {
                    C.DelayCraftIncrease = delayCraftAmount;
                    C.Save();
                }
            }
        }
    }

    private bool SelfRepairGather = C.SelfRepairGather;
    private float SelfRepairPercent = C.RepairPercent;
    private bool SelfSpiritbondGather = C.SelfSpiritbondGather;
    private bool AutoCordial = C.AutoCordial;
    private bool InverseCordialPrio = C.inverseCordialPrio;
    private bool UseOnFisher = C.UseOnFisher;
    private bool PreventOvercap = C.PreventOvercap;
    private int CordialMinGp = C.CordialMinGp;
    private bool useOnlyInMission = C.UseOnlyInMission;
    private string newProfileName = "";

    private string[] MissionTypes = ["Limited Nodes", "Gather x Amount", "Time Attack", "Chained Scoring", "Boon Scoring", "Chain + Boon Scoring", "Dual Class"];
    private int MissionIndex = 0;

    private void GatherSettings()
    {
        void DrawBuffSetting(string label, string uniqueId, bool currentEnabled, int currentMinGp, int minGpLimit, int maxGpLimit, string entryName, string ActionInfo, Action<bool> onEnabledChange, Action<int> onMinGpChange, int currentMaxUse, Action<int> onMaxUseChange)
        {
            bool enabled = currentEnabled;
            if (ImGui.Checkbox($"{label}###Enable{uniqueId}", ref enabled))
            {
                if (enabled != currentEnabled)
                    onEnabledChange(enabled);
            }
            ImGuiEx.HelpMarker(ActionInfo);

            if (enabled)
            {
                ImGui.Indent(15);

                if (ImGui.TreeNode($"{label} Settings###Tree{uniqueId}{entryName}"))
                {
                    int minGp = currentMinGp;
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Minimum GP");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    if (ImGui.SliderInt($"###Slider{uniqueId}{entryName}", ref minGp, minGpLimit, maxGpLimit))
                    {
                        if (minGp != currentMinGp)
                            onMinGpChange(minGp);
                    }
                    int maxUse = currentMaxUse;
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Maximum use count");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt($"###Slider{uniqueId}{entryName}_1", ref maxUse, 1))
                    {
                        if (maxUse != currentMaxUse)
                            onMaxUseChange(maxUse);
                    }
                    ImGuiEx.HelpMarker("Set to -1 to allow for infinite uses \n" +
                                       "Set to 1-> X to set maximum amount of uses per mission");

                    ImGui.TreePop();
                }
                ImGui.Unindent(15);
            }
        }

        void DrawCustomBuffSetting(string label, string uniqueId, bool currentEnabled, int currentMinGp, int minGpLimit, int maxGpLimit, string entryName, string ActionInfo, Action<bool> onEnabledChange, Action<int> onMinGpChange, int currentMaxUse, Action<int> onMaxUseChange, int MinItemUsage, Action<int> onMinItemMaxUseChange)
        {
            bool enabled = currentEnabled;
            if (ImGui.Checkbox($"{label}###Enable{uniqueId}", ref enabled))
            {
                if (enabled != currentEnabled)
                    onEnabledChange(enabled);
            }
            ImGuiEx.HelpMarker(ActionInfo);

            if (enabled)
            {
                ImGui.Indent(15);

                if (ImGui.TreeNode($"{label} Settings###Tree{uniqueId}{entryName}"))
                {
                    int minGp = currentMinGp;
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Minimum GP");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    if (ImGui.SliderInt($"###Slider{uniqueId}{entryName}", ref minGp, minGpLimit, maxGpLimit))
                    {
                        if (minGp != currentMinGp)
                            onMinGpChange(minGp);
                    }
                    int maxUse = currentMaxUse;
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Maximum use count");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt($"###Slider{uniqueId}{entryName}_1", ref maxUse, 1))
                    {
                        if (maxUse != currentMaxUse)
                            onMaxUseChange(maxUse);
                    }
                    ImGuiEx.HelpMarker("Set to -1 to allow for infinite uses \n" +
                                       "Set to 1-> X to set maximum amount of uses per mission");

                    int MinItem = MinItemUsage;
                    ImGui.Text($"Minimum BYII Item");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.SliderInt($"###MinItemsBYII{uniqueId}{entryName}_1", ref MinItem, 2, 4))
                    {
                        if (MinItem != MinItemUsage)
                            onMinItemMaxUseChange(MinItem);
                    }
                    ImGuiEx.HelpMarker($"Set the minimum amount of items that you want BYII to activate on\n" +
                                       $"Ex. Setting it to 2 will make it to where if you only activate if you need need 2 or more items\n" +
                                       $"Useful if you're trying to save gp on gather x amount or dual class missions");

                    ImGui.TreePop();
                }
                ImGui.Unindent(15);
            }
        }

        int maxGp = 1200;

        if (ImGui.Checkbox("Self Repair on Gather", ref SelfRepairGather))
        {
            if (C.SelfRepairGather != SelfRepairGather)
            {
                C.SelfRepairGather = SelfRepairGather;
                C.Save();
            }
        }
        if (SelfRepairGather)
        {
            ImGui.Indent(15);
            ImGui.Text("Repair at");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("###Repair %", ref SelfRepairPercent, 0f, 99f, "%.0f%%"))
            {
                if (C.RepairPercent != SelfRepairPercent)
                {
                    C.RepairPercent = (int)SelfRepairPercent;
                    C.Save();
                }
            }
            ImGui.Unindent(15);
        }
        if (ImGui.Checkbox("Extract Spiritbond on Gather", ref SelfSpiritbondGather))
        {
            if (C.SelfSpiritbondGather != SelfSpiritbondGather)
            {
                C.SelfSpiritbondGather = SelfSpiritbondGather;
                C.Save();
            }
        }
        if (ImGui.Checkbox("Auto Cordial", ref AutoCordial))
        {
            C.AutoCordial = AutoCordial;
            C.Save();
        }
        ImGuiEx.HelpMarker("Will only work while using ICE and not manual mode\n" +
                           "Will also pause pandora cordial usage while on the moon");
        if (AutoCordial)
        {
            if (ImGui.TreeNode("Cordial Settings"))
            {
                if (ImGui.Checkbox("Inverse Priority (Watered -> Regular -> Hi)", ref InverseCordialPrio))
                {
                    C.inverseCordialPrio = InverseCordialPrio;
                    C.Save();
                }
                if (ImGui.Checkbox("Prevent Overcap", ref PreventOvercap))
                {
                    C.PreventOvercap = PreventOvercap;
                    C.Save();
                }
                if (ImGui.Checkbox("Use on Fisher", ref UseOnFisher))
                {
                    C.UseOnFisher = UseOnFisher;
                    C.Save();
                }
                if (ImGui.Checkbox("Only use in mission", ref useOnlyInMission))
                {
                    C.UseOnlyInMission = useOnlyInMission;
                    C.Save();
                }
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderInt("Gp Threshold", ref CordialMinGp, 0, maxGp))
                {
                    C.CordialMinGp = CordialMinGp;
                    C.Save();
                }

                ImGui.TreePop();
            }
        }

        ImGui.Dummy(new(0, 5));

        ImGui.SetNextItemWidth(200);
        ImGui.InputText("New Profile Name", ref newProfileName, 64);
        using (ImRaii.Disabled(newProfileName == ""))
        {
            if (ImGui.Button("Add Profile") && !string.IsNullOrWhiteSpace(newProfileName))
            {
                if (!C.GatherSettings.Any(x => x.Name == newProfileName))
                {
                    int newId = C.GatherSettings.Max(x => x.Id) + 1;
                    C.GatherSettings.Add(new GatherBuffProfile { Id = newId, Name = newProfileName });
                    C.Save();
                    newProfileName = ""; // Reset input
                }
            }
        }

        ImGui.Columns(2, "Gather Settings Columns", false);

        // ------------------ 
        //  Left Column, Profile Settings
        // ------------------
        ImGui.SetColumnWidth(0, 350);

        ImGui.Text("Gather Profiles");

        bool canDelete = C.GatherSettings.Count > 1 && C.SelectedGatherIndex != 0;
        using (ImRaii.Disabled(!canDelete))
        {
            if (ImGui.Button("Delete Selected Profile"))
            {
                var deletedProfile = C.GatherSettings[C.SelectedGatherIndex];
                int deletedId = deletedProfile.Id;

                // Remove the profile
                C.GatherSettings.RemoveAt(C.SelectedGatherIndex);

                // Update all missions using this GatherSettingId
                foreach (var mission in C.Missions)
                {
                    if (mission.GatherSettingId == deletedId)
                    {
                        mission.GatherSettingId = C.GatherSettings[0].Id; // fallback to default
                    }
                }

                // Clamp the selected index and save
                C.SelectedGatherIndex = Math.Clamp(C.SelectedGatherIndex, 0, C.GatherSettings.Count - 1);
                C.Save();
            }
        }

        ImGui.BeginChild("GatherProfileChild", new Vector2(300, ImGui.GetTextLineHeightWithSpacing() * 5 + 10), true);
        for (int i = 0; i < C.GatherSettings.Count; i++)
        {
            bool isSelected = (i == C.SelectedGatherIndex);

            if (ImGui.Selectable(C.GatherSettings[i].Name, isSelected))
            {
                C.SelectedGatherIndex = i;
                C.Save();
            }

            if (isSelected)
                ImGui.SetItemDefaultFocus();
        }
        ImGui.EndChild();

        GatherBuffProfile entry = C.GatherSettings[C.SelectedGatherIndex];

        ImGui.Combo("Mission Type", ref MissionIndex, MissionTypes, MissionTypes.Length);
        if (ImGui.Button("Apply to Mission Types"))
        {
            foreach (var mission in C.Missions)
            {
                var id = mission.Id;

                var missionDict = CosmicHelper.MissionInfoDict[id];

                bool craftMission = missionDict.Attributes.HasFlag(MissionAttributes.Craft);
                bool gatherMission = missionDict.Attributes.HasFlag(MissionAttributes.Gather);

                bool LimitedQuant = missionDict.Attributes.HasFlag(MissionAttributes.Limited);
                // Gather X Amount is just "Gather" 
                bool TimedMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining);
                bool ChainedMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreChains);
                bool BoonMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreGatherersBoon);
                bool collectableMission = missionDict.Attributes.HasFlag(MissionAttributes.Collectables);
                bool stellerReductionMission = missionDict.Attributes.HasFlag(MissionAttributes.ReducedItems);

                bool GatherX = !stellerReductionMission && !collectableMission && !BoonMission && !ChainedMission && !TimedMission && !LimitedQuant;

                void UpdateMissions()
                {
                    mission.GatherSettingId = entry.Id;
                }

                if (gatherMission && (!collectableMission && !stellerReductionMission))
                {
                    if (MissionIndex == 0 && LimitedQuant)
                        UpdateMissions();
                    else if (MissionIndex == 2 && TimedMission)
                        UpdateMissions();
                    else if (MissionIndex == 3 && ChainedMission && !BoonMission)
                        UpdateMissions();
                    else if (MissionIndex == 4 && BoonMission && !ChainedMission)
                        UpdateMissions();
                    else if (MissionIndex == 5 && ChainedMission && BoonMission)
                        UpdateMissions();
                    else if (MissionIndex == 6 && craftMission)
                        UpdateMissions();
                    else if (MissionIndex == 1 && GatherX)
                        UpdateMissions();
                }
            }

            C.Save();
        }

        // ---------------------------------
        // Right Column, Gathering setttings
        // ---------------------------------

        ImGui.NextColumn();
        ImGui.SetColumnWidth(1, ImGui.GetWindowWidth() - 300);

        // Pathfinding
        int pathfinding = entry.Pathfinding;
        string[] modes = ["Simple", "Nearest", "Cyclic"];
        ImGui.SetNextItemWidth(100);
        if (ImGui.Combo("Pathfinding mode", ref pathfinding, modes, modes.Length))
        {
            entry.Pathfinding = pathfinding;
            C.Save();
        }
        ImGuiEx.HelpMarker("Simple - From 1st node in list until the last.\nNearest - Always go to Nearest node then find a path that minimises distance through all remaining nodes.\nCyclic - Find nodes that are close together and stick to those nodes only.");
        if (pathfinding == 2)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            int cycle = entry.TSPCycleSize;
            if (ImGui.InputInt("Cycle size", ref cycle, 1))
            {
                entry.TSPCycleSize = cycle >= 2 ? cycle : 2;
                C.Save();
            }
        }

        // GP Settings
        int minGP = entry.MinimumGP;
        ImGui.SetNextItemWidth(100);
        if (ImGui.SliderInt("Minimum GP to start mission", ref minGP, -1, maxGp))
        {
            entry.MinimumGP = minGP;
            C.Save();
        }

        // Multiply gathered items on FIRST gather loop only. Should only be used for Dual Class really.
        int gatherMult = entry.InitialGatheringItemMultiplier;
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("Dual Class Craft Amount", ref gatherMult, 1))
        {
            entry.InitialGatheringItemMultiplier = gatherMult >= 1 ? gatherMult : 1;
            C.Save();
        }
        ImGuiEx.HelpMarker("This increases how many items you gather before you are 'done' before switching to crafting.\nSet this to however many items you need to craft to reach your target score.\nOnly affects Dual Class missions.");

        // Boon Increase 2 (+30% Increase)
        DrawBuffSetting(
            label: "Pioneer's / Mountaineer's Gift II",
            uniqueId: $"Boon2Inc{entry.Id}",
            currentEnabled: entry.Buffs.BoonIncrease2,
            currentMinGp: entry.Buffs.BoonIncrease2Gp,
            minGpLimit: 100,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "Apply a 30% buff to your boon chance.",
            onEnabledChange: newVal =>
            {
                entry.Buffs.BoonIncrease2 = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BoonIncrease2Gp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BoonIncrease2MaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BoonIncrease2MaxUse = newVal;
                C.Save();
            }
        );

        // Boon Increase 1 (+10% Increase)
        DrawBuffSetting(
            label: "Pioneer's / Mountaineer's Gift I",
            uniqueId: $"Boon1Inc{entry.Id}",
            currentEnabled: entry.Buffs.BoonIncrease1,
            currentMinGp: entry.Buffs.BoonIncrease1Gp,
            minGpLimit: 50,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "Apply a 10% buff to your boon chance.",
            onEnabledChange: newVal =>
            {
                entry.Buffs.BoonIncrease1 = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BoonIncrease1Gp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BoonIncrease1MaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BoonIncrease1MaxUse = newVal;
                C.Save();
            }
        );

        // Tidings (+2 to boon instead of +1)
        DrawBuffSetting(
            label: "Nophica's / Nald'thal's Tidings Buff",
            uniqueId: $"TidingsBuff{entry.Id}",
            currentEnabled: entry.Buffs.TidingsBool,
            currentMinGp: entry.Buffs.TidingsGp,
            minGpLimit: 200,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "Increases item yield from Gatherer's Boon by 1",
            onEnabledChange: newVal =>
            {
                entry.Buffs.TidingsBool = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.TidingsGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.TidingsMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.TidingsMaxUse = newVal;
                C.Save();
            }
        );

        // Yield II (+2 to all items on node)
        DrawBuffSetting(
            label: "Blessed / Kings Yield II",
            uniqueId: $"Blessed/KingsYieldIIBuff{entry.Id}",
            currentEnabled: entry.Buffs.YieldII,
            currentMinGp: entry.Buffs.YieldIIGp,
            minGpLimit: 500,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "Increases the number of items obtained when gathering by 2\n" +
                        "Will only apply when the gathering node has full durability",
            onEnabledChange: newVal =>
            {
                entry.Buffs.YieldII = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.YieldIIGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.YieldIIMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.YieldIIMaxUse = newVal;
                C.Save();
            }
        );

        // Yield I (+1 to all items on node)
        DrawBuffSetting(
            label: "Blessed / Kings Yield I",
            uniqueId: $"Blessed/KingsYieldIBuff{entry.Id}",
            currentEnabled: entry.Buffs.YieldI,
            currentMinGp: entry.Buffs.YieldIGp,
            minGpLimit: 400,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "Increases the number of items obtained when gathering by 1\n" +
                        "Will only apply when the gathering node has full durability",
            onEnabledChange: newVal =>
            {
                entry.Buffs.YieldI = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.YieldIGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.YieldIMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.YieldIMaxUse = newVal;
                C.Save();
            }
        );

        // Bonus Integrity (+1 integrity)
        DrawBuffSetting(
            label: "Ageless Words / Solid Reason",
            uniqueId: $"Incrase Intregity{entry.Id}",
            currentEnabled: entry.Buffs.BonusIntegrity,
            currentMinGp: entry.Buffs.BonusIntegrityGp,
            minGpLimit: 300,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "Increase the Integrity by 1\n" +
                        "50% chance to grant Eureka Moment",
            onEnabledChange: newVal =>
            {
                entry.Buffs.BonusIntegrity = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BonusIntegrityGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BonusIntegrityMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BonusIntegrityMaxUse = newVal;
                C.Save();
            }
        );

        // Bountiful Yield/Harvest II (+Amount based on gathering)
        DrawCustomBuffSetting(
            label: "Bountiful Yield II / Bountiful Harvest II",
            uniqueId: $"Bountiful Yield II {entry.Id}",
            currentEnabled: entry.Buffs.BountifulYieldII,
            currentMinGp: entry.Buffs.BountifulYieldIIGp,
            minGpLimit: 100,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "Increase item's gained on next gathering attempt by 1, 2, or 3 \n" +
                        "This is based on your gathering rating",
            onEnabledChange: newVal =>
            {
                entry.Buffs.BountifulYieldII = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BountifulYieldIIGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BountifulYieldIIMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BountifulYieldIIMaxUse = newVal;
                C.Save();
            },
            entry.Buffs.BountifulMinItem,
            onMinItemMaxUseChange: newVal =>
            {
                entry.Buffs.BountifulMinItem = newVal;
                C.Save();
            }
        );

        ImGui.Columns(1);
    }

    private bool gambaEnabled = C.GambaEnabled;
    private int gambaDelay = C.GambaDelay;
    private int gambaCreditsMinimum = C.GambaCreditsMinimum;
    private bool gambaPreferSmallerWheel = C.GambaPreferSmallerWheel;

    private void GambaWheel()
    {
        if (ImGui.Checkbox("Enable Gamba", ref gambaEnabled))
        {
            C.GambaEnabled = gambaEnabled;
            C.Save();
        }
        ImGuiEx.HelpMarker("To run this, make sure you have the gamble wheels shown at Orbitingway, and press start. It will full auto from there.");
        if (gambaEnabled)
        {
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("Gamba Delay", ref gambaDelay, 50, 2000))
            {
                C.GambaDelay = gambaDelay;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("Mininum credits to keep", ref gambaCreditsMinimum, 0, 10000))
            {
                C.GambaCreditsMinimum = gambaCreditsMinimum;
                C.Save();
            }
        }
        if (ImGui.Checkbox("Prefer smaller wheel", ref gambaPreferSmallerWheel))
        {
            C.GambaPreferSmallerWheel = gambaPreferSmallerWheel;
            C.Save();
        }
        ImGuiEx.HelpMarker("This will make the Gamba prefer wheels with less items.");
        ImGui.Separator();
        ImGui.TextUnformatted("Configure the weights for each item in the Gamba. Higher weight = more desirable.");
        ImGui.Spacing();
        foreach (GambaType type in Enum.GetValues(typeof(GambaType)))
        {
            var itemsType = C.GambaItemWeights.Where(x => x.Type == type).OrderBy(x => x.ItemId).ToList();
            if (itemsType.Count == 0) continue;
            if (ImGui.TreeNodeEx($"{type} ({itemsType.Count})##gamba_type_{type}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                foreach (var gamba in itemsType)
                {
                    var itemName = ExcelItemHelper.GetName(gamba.ItemId);
                    int weight = gamba.Weight;
                    ImGui.SetNextItemWidth(120f);
                    if (ImGui.InputInt($"[{gamba.ItemId}] {itemName}##gamba_weight", ref weight))
                    {
                        gamba.Weight = weight;
                        C.Save();
                    }
                }
                ImGui.Unindent();
                ImGui.TreePop();
            }
        }
        if (ImGui.Button("Reset Weights"))
        {
            TaskGamba.EnsureGambaWeightsInitialized(true);
        }
    }

    private bool showOverlay = C.ShowOverlay;
    private bool ShowSeconds = C.ShowSeconds;
    private bool ShowXP = C.ShowExpBars;

    private void Overlay()
    {
        if (ImGui.Checkbox("Show Overlay", ref showOverlay))
        {
            C.ShowOverlay = showOverlay;
            C.Save();
        }

        if (ImGui.Checkbox("Show Seconds", ref ShowSeconds))
        {
            C.ShowSeconds = ShowSeconds;
            C.Save();
        }

        if (ImGui.Checkbox("Show XP", ref ShowXP))
        {
            C.ShowExpBars = ShowXP;
            C.Save();
        }
    }

    private bool EnableAutoSprint = C.EnableAutoSprint;

    private void Misc()
    {
        if (ImGui.Checkbox("Enable Auto Sprint", ref EnableAutoSprint))
        {
            C.EnableAutoSprint = EnableAutoSprint;
            C.Save();
        }
    }

#if DEBUG

    private void Debug()
    {
        ImGui.Checkbox("Force OOM Main", ref SchedulerMain.DebugOOMMain);
        ImGui.Checkbox("Force OOM Sub", ref SchedulerMain.DebugOOMSub);
        ImGui.Checkbox("Legacy Failsafe WKSRecipe Select", ref C.FailsafeRecipeSelect);

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
        /*
        ImGuiHelpers.ScaledDummy(5, 0);
        ImGui.SameLine();
        if (ImGui.CollapsingHeader("Provision Mission Priority"))
        {
            for (int i = 0; i < sorted.Count; i++)
            {
                var item = sorted[i];
                ImGuiHelpers.ScaledDummy(5, 0);
                ImGui.SameLine();
                ImGui.Selectable(item.Name);
                if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                {
                    int nextIndex = i + (ImGui.GetMouseDragDelta(0).Y < 0f ? -1 : 1);
                    if (nextIndex >= 0 && nextIndex < sorted.Count)
                    {
                        // Swap the priority values
                        var otherItem = sorted[nextIndex];

                        // Swap their priority values via the original setters
                        byte temp = missionMap[item.Index].get();
                        missionMap[item.Index].set(missionMap[otherItem.Index].get());
                        missionMap[otherItem.Index].set(temp);
                        ImGui.ResetMouseDragDelta();
                    }
                }
            }
        }
        */

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
    }


#endif

}
