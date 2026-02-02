using ICE.Utilities.Cosmic_Helper;

namespace ICE.Ui.MainUi.Settings.Settings_Table;

public static class Settings_TableColumns
{
    private static string[] missionSortOptions = 
        ["Id", "Name", "Cosmo Credits", "Lunar Credits", 
        "Exp I", "Exp II", "Exp III", "Exp IV", "Exp V", 
        "Map Location", "Class Score", "Class Exp"];

    public static void ColumnSettings()
    {
        int missionSelectedOption = C.TableSortOption;
        if (ImGui.BeginCombo("Sort By", missionSortOptions[missionSelectedOption]))
        {
            for (int i = 0; i < missionSortOptions.Length; i++)
            {
                bool isSelected = (i == missionSelectedOption);
                if (ImGui.Selectable(missionSortOptions[i], isSelected))
                {
                    missionSelectedOption = i;
                }
                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
                if (missionSelectedOption != C.TableSortOption)
                {
                    C.TableSortOption = missionSelectedOption;
                    C.Save();
                }
            }
            ImGui.EndCombo();
        }

        bool hideUnsupported = C.HideUnsupportedMissions;
        if (ImGui.Checkbox("Hide Unsupported Missions", ref hideUnsupported))
        {
            C.HideUnsupportedMissions = hideUnsupported;
            C.Save();
        }

        bool grindAllProvisionals = C.GrindAllProvisionals;
        if (ImGui.Checkbox("Allow All Provisional Kinds", ref grindAllProvisionals))
        {
            C.GrindAllProvisionals = grindAllProvisionals;
            C.Save();
        }
        ImGuiEx.HelpMarker("Enabling this will show you all weather/timed/sequence missions that you can grind, \n" +
                           "ON TOP OF doing the normal missions for whichever class you start on.\n" +
                           "If you just want to focus one specific class, set this to false\n" +
                           "Do note: this replaced provisional grinding, due to just being built into the standard mode now (finally)");

        bool showExtraInfo = C.ShowExtraMissionInfo;
        if (ImGui.Checkbox("Show Extra Mission Info Side-Window", ref showExtraInfo))
        {
            C.ShowExtraMissionInfo = showExtraInfo;
            C.Save();
        }

        bool autoShowToken = C.Auto_ShowTokens;
        if (ImGui.Checkbox("Auto Hide/Show Planet Tokens", ref autoShowToken))
        {
            C.Auto_ShowTokens = autoShowToken;
            C.Save();
        }

        bool showManualMode = C.ShowManualMode;
        if (ImGui.Checkbox("Show Manual Mode Column", ref showManualMode))
        {
            C.ShowManualMode = showManualMode;
            if (!showManualMode)
            {
                foreach (var mission in C.MissionConfig)
                {
                    mission.Value.ManualMode = false;
                }
            }
            C.Save();
        }
        ImGuiEx.HelpMarker("Only enable this if you want plan on doing missions YOURSELF. AND NOT AUTOMATING IT. " +
                           "Or if you're letting a different plugin do all the automating of turning in, craftings, gathering... and not letting I.C.E. handle interacting with those plugins");
    }

    private static bool ApplyToAllClasses = true;
    private static bool ApplyToSpecicClass = false;
    private static int SpecificClass = 8;
    private static int selectedClassIndex = 0;

    private static readonly string[] classOptions = new[]
    {
        "Carpenter (CRP)",      // 0
        "Blacksmith (BSM)",     // 1
        "Armorer (ARM)",        // 2
        "Goldsmith (GSM)",      // 3
        "Leatherworker (LTW)",  // 4
        "Weaver (WVR)",         // 5
        "Alchemist (ALC)",      // 6
        "Culinarian (CUL)",     // 7
        "Miner (MIN)",          // 8
        "Botanist (BTN)",       // 9
        "Fisher (FSH)"          // 10
    };

    private static readonly int[] classIds = new[]
    {
        8,  // Carpenter
        9,  // Blacksmith
        10, // Armorer
        11, // Goldsmith
        12, // Leatherworker
        13, // Weaver
        14, // Alchemist
        15, // Culinarian
        16, // Miner
        17, // Botanist
        18  // Fisher
    };

    private static bool AnyTurnin = true;
    private static bool TurninGold = false;
    private static bool TurninSilver = false;
    private static bool TurninBronze = false;

    public static void GeneralMissionSettings()
    {
        bool removeGold = C.RemoveAfterGold;
        if (ImGui.Checkbox("Remove Mission Upon Gold Completion", ref removeGold))
        {
            C.RemoveAfterGold = removeGold;
            C.Save();
        }

        ImGui.Checkbox("Stop after current mission", ref Mission_Settings.StopAfterCurrent);
        ImGui.Checkbox("Return to original class after mission", ref Mission_Settings.ReturnToOriginalJob);
        bool relicTurnin = C.TurninRelic;
        if (ImGui.Checkbox($"Turnin if relic is complete##RelicTurnin_GeneralSetting", ref relicTurnin))
        {
            C.TurninRelic = relicTurnin;
            C.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("?");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("THIS IS YOUR HEADS UP ON HOW THIS WORKS. If I change this in the future, this tooltip will also change.\n" +
                             "1: This will check for your current CLASS [not menu class, actual current class] for relic turnin.\n" +
                             "2: You must not have the tool eqipped for this to run full auto. \n" +
                             "\t- This is due to the fact that I cba coding this in at this time. (might change my mind in the future *shrugs*)\n" +
                             "3: This will take prio over \"Stop @ Relic Turnin\", in the sense that if you have both enabled, it will turnin vs stop. And continue about it's day\n" +
                             "4: If you're on a crafting class, it will return you back to the stop you were crafting post turnin. \n" +
                             "\t- This is optional, you can disable it at your own free will, I just like this so I can just go back to an isolated area of my choosing");
        }
        if (ImGui.Button("Quick Apply Turnins"))
        {
            ImGui.OpenPopup("Quick Apply_Mission Turnins");
        }

        if (ImGui.BeginPopup("Quick Apply_Mission Turnins"))
        {
            if (ImGui.RadioButton("Apply to all classes", ApplyToAllClasses))
            {
                ApplyToAllClasses = true;
                ApplyToSpecicClass = false;
            }

            if (ImGui.RadioButton("Apply to specific class", ApplyToSpecicClass))
            {
                ApplyToAllClasses = false;
                ApplyToSpecicClass = true;
            }
            if (ImGui.Combo("##ClassSelector", ref selectedClassIndex, classOptions, classOptions.Length))
            {
                // Update SpecificClass when selection changes
                SpecificClass = classIds[selectedClassIndex];
                IceLogging.Debug($"Selected class: {classOptions[selectedClassIndex]}, ID: {SpecificClass}");
            }
            ImGui.Separator();
            ImGui.Text("Select Turnin Options");
            ImGui.Dummy(new Vector2(0, 2));

            if (ImGui.Checkbox("Auto", ref AnyTurnin))
            {
                if (AnyTurnin)
                {
                    TurninGold = false;
                    TurninSilver = false;
                    TurninBronze = false;

                    AnyTurnin = true;
                }
                else
                {
                    if (!(TurninBronze && TurninSilver && TurninGold))
                    {
                        AnyTurnin = true;
                    }
                }

                C.Save();
            }
            ImGuiEx.HelpMarker("This option will strive to get the best result, but will turn in any result if necessary without stopping.");

            ImGui.Separator();

            if (ImGui.Checkbox("Gold", ref TurninGold))
            {
                if (AnyTurnin && TurninGold)
                    AnyTurnin = false;

            }
            if (ImGui.Checkbox("Silver", ref TurninSilver))
            {
                if (AnyTurnin && TurninSilver)
                    AnyTurnin = false;

            }
            if (ImGui.Checkbox("Bronze", ref TurninBronze))
            {
                if (AnyTurnin && TurninBronze)
                    AnyTurnin = false;

            }

            if (!AnyTurnin && !TurninGold && !TurninSilver && !TurninBronze)
                AnyTurnin = true;

            ImGui.Separator();

            if (ImGui.Button("Apply"))
            {
                var amountApplied = 0;
                foreach (var mission in C.MissionConfig)
                {
                    if (CosmicHelper.SheetMissionDict.TryGetValue(mission.Key, out var sheetInfo))
                    {
                        if (ApplyToSpecicClass && !sheetInfo.Jobs.Contains((uint)SpecificClass))
                            continue;

                        if (sheetInfo.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining))
                            continue;

                        if (C.MissionConfig.TryGetValue(mission.Key, out var config))
                        {
                            config.AutoTurnin = AnyTurnin;
                            config.TurninGold = TurninGold;
                            config.TurninSilver = TurninSilver;
                            config.TurninBronze = TurninBronze;
                        }
                        amountApplied += 1;
                    }
                }
                C.SaveDebounced();

                Notify.Success($"Applied settings to: {amountApplied} missions, just for you buddy.");
                ImGui.CloseCurrentPopup();
            }


            ImGui.EndPopup();
        }
    }
}
