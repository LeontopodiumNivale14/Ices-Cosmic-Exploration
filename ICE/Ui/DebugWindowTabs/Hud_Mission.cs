using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.Cosmic_Helper;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ICE.Utilities.CosmicHelper;

namespace ICE.Ui.DebugWindowTabs
{
    internal class Hud_Mission
    {
        private static int BestMission = 0;
        private static string MissionName = "";

        public static List<int> XpKinds = new() { 1, 2, 3, 4, 5 };

        public static void Draw()
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                ImGui.Text("List of Visible Missions");
                ImGui.Text($"Selected Mission Name: {x.SelectedMissionName}");
                ImGui.Text($"Selected Mission ID: {x.SelectedMissionId}");

                if (ImGui.Button("Help"))
                {
                    x.Help();
                }
                ImGui.SameLine();

                if (ImGui.Button("Mission Selection"))
                {
                    x.MissionSelection();
                }
                ImGui.SameLine();

                if (ImGui.Button("Mission Log"))
                {
                    x.MissionLog();
                }
                ImGui.SameLine();

                if (ImGui.Button("Basic Missions"))
                {
                    x.BasicMissions();
                }
                ImGui.SameLine();

                if (ImGui.Button("Provisional Missions"))
                {
                    x.ProvisionalMissions();
                }
                ImGui.SameLine();

                if (ImGui.Button("Critical Missions"))
                {
                    x.CriticalMissions();
                }

                bool EnableDummyXp = C.UseDummyXp;
                if (ImGui.Checkbox("Enable Dummy XP", ref EnableDummyXp))
                {
                    C.UseDummyXp = EnableDummyXp;
                    C.Save();
                }

                for (int i = 0; i < x.SelectClass.Length; i++)
                {
                    if (i != 0)
                        ImGui.SameLine();
                    
                    if (ImGui.Button($"[{i}]"))
                    {
                        x.SelectClass[i].Select();
                    }
                }

                bool EnableXpGrind = C.XPRelicGrind;
                bool IgnoreManual = C.XPRelicIgnoreManual;
                bool onlyEnabled = C.XPRelicOnlyEnabled;

                if (ImGui.Checkbox("Relic XP Grind", ref EnableDummyXp))
                {
                    C.XPRelicGrind = EnableDummyXp;
                    C.Save();
                }
                if (ImGui.Checkbox("Ignore Manual Mode", ref IgnoreManual))
                {
                    C.XPRelicIgnoreManual = IgnoreManual;
                    C.Save();
                }
                if (ImGui.Checkbox("Only Enabled Missions", ref onlyEnabled))
                {
                    C.XPRelicOnlyEnabled = onlyEnabled;
                    C.Save();
                }

                if (ImGui.CollapsingHeader("Show Following Kinds"))
                {
                    bool useDummyRank = C.UseDummyRanks;
                    bool useDummyA = C.ShowDummyA;
                    bool useDummyB = C.ShowDummyB;
                    bool useDummyC = C.ShowDummyC;
                    bool useDummyD = C.ShowDummyD;

                    if (ImGui.Checkbox("Use Dummy Ranks", ref useDummyRank))
                    {
                        C.UseDummyRanks = useDummyRank;
                        C.Save();
                    }
                    if (ImGui.Checkbox("Show A Ranks", ref useDummyA))
                    {
                        C.ShowDummyA = useDummyA;
                        C.Save();
                    }
                    if (ImGui.Checkbox("Show B Ranks", ref useDummyB))
                    {
                        C.ShowDummyB = useDummyB;
                        C.Save();
                    }
                    if (ImGui.Checkbox("Show C Ranks", ref useDummyC))
                    {
                        C.ShowDummyC = useDummyC;
                        C.Save();
                    }
                    if (ImGui.Checkbox("Show D Ranks", ref useDummyD))
                    {
                        C.ShowDummyD = useDummyD;
                        C.Save();
                    }
                }

                if (ImGui.Button("Update Dummy XP"))
                {
                    foreach (var kind in XpKinds)
                    {
                        if (!C.DummyXP.ContainsKey(kind))
                        {
                            C.DummyXP[kind] = new()
                            {
                                CurrentXP = 0,
                                NeededXP = 1,
                            };
                            C.Save();
                        }
                    }
                }

                foreach (var key in C.DummyXP.Keys.ToList())
                {
                    var xp = C.DummyXP[key];

                    ImGui.Text($"XP: {key}");
                    ImGui.PushID(key);

                    int currentXP = xp.CurrentXP;
                    int neededXP = xp.NeededXP;

                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt("Current XP", ref currentXP))
                    {
                        xp.CurrentXP = currentXP;
                        C.Save();
                    }

                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt("Needed XP", ref neededXP))
                    {
                        xp.NeededXP = neededXP;
                        C.Save();
                    }

                    ImGui.PopID();

                    ImGui.Separator();
                }

                foreach (var m in x.StellerMissions)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"[{m.MissionId}] {m.Name}");
                    ImGui.SameLine();
                    if (ImGui.Button($"Select###Select + {m.Name}"))
                    {
                        m.Select();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"Initiate##Initiate + {m.Name}"))
                    {
                        m.Initiate();
                    }
                }

                ImGui.Text($"Best Relic Mission: {BestMission} | {MissionName}");
                if (ImGui.Button("Update Best Mission"))
                {
                    BestMission = (int)RelicMissionFinder();
                    if (BestMission < 1)
                        MissionName = "None";
                    else
                    {
                        MissionName = CosmicHelper.SheetMissionDict[(uint)BestMission].Name;
                    }
                }


            }
            else
            {
                ImGui.Text("Waiting for \"WKSMission\" to be visible");
            }
        }

        private static unsafe uint RelicMissionFinder()
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                var wksManager = WKSManager.Instance();
                if (wksManager == null || wksManager->ResearchModule == null || !wksManager->ResearchModule->IsLoaded)
                    return 0;

                var job = (uint)Player.Job;
                var toolClassId = (byte)(job - 7);
                var stage = wksManager->ResearchModule->CurrentStages[toolClassId - 1];
                var nextstate = wksManager->ResearchModule->UnlockedStages[toolClassId - 1];

                if (Svc.Data.GetExcelSheet<WKSCosmoToolClass>().TryGetRow(toolClassId, out var row)) { }

                Dictionary<int, CosmicHelper.XPType> XPTable = new Dictionary<int, CosmicHelper.XPType>();

                if (C.UseDummyXp)
                {
                    XPTable = C.DummyXP;
                }
                else
                {
                    for (byte type = 1; type <= 4; type++)
                    {
                        if (!wksManager->ResearchModule->IsTypeAvailable(toolClassId, type))
                            break;

                        var neededXP = wksManager->ResearchModule->GetNeededAnalysis(toolClassId, type);

                        var currentXp = wksManager->ResearchModule->GetCurrentAnalysis(toolClassId, type);
                        var requiredXp = neededXP - currentXp;
                        if (!XPTable.ContainsKey(type))
                        {
                            XPTable[type] = new XPType()
                            {
                                CurrentXP = currentXp,
                                NeededXP = neededXP,
                            };
                        }
                    }
                }

                var urgencies = new Dictionary<int, float>();
                for (int i = 0; i < XPTable.Count; i++)
                {
                    var bar = XPTable[i + 1];
                    urgencies[i + 1] = bar.NeededXP > 0 ? 1f - (float)bar.CurrentXP / bar.NeededXP : 0f;
                    IceLogging.Debug($"XP Type: {i+1} | Urgency: {urgencies[i + 1]}");
                }

                Dictionary<uint, Dictionary<int, float>> rewardMissions = new();
                foreach (var availMission in x.StellerMissions)
                {
                    var id = availMission.MissionId;
                    if (CosmicHelper.SheetMissionDict.TryGetValue(id, out var mission))
                    {
                        var missionConfig = C.MissionConfig[id];

                        int minLevel = 10;
                        var rank = mission.Rank;
                        switch (rank)
                        {
                            case 1:
                                minLevel = 10;
                                break;
                            case 2:
                                minLevel = 50;
                                break;
                            case 3:
                                minLevel = 90;
                                break;
                            case 4:
                            case 5:
                            case 6:
                                minLevel = 100;
                                break;
                            default:
                                minLevel = 10;
                                break;
                        }

                        bool properLevel = Player.Level <= minLevel;
                        bool IgnoreManual = C.XPRelicIgnoreManual && missionConfig.ManualMode;
                        bool IgnoreNotEnabled = C.XPRelicOnlyEnabled && !missionConfig.Enabled;

                        IceLogging.Info($"Mission Info: {id}\n" +
                                        $"Proper Level: {properLevel} | Mission level: {minLevel} | Player Level: {Player.Level} \n" +
                                        $"Ignore Manual: {IgnoreManual}\n" +
                                        $"Enabled: {missionConfig.Enabled} | Ignore cause not enabled: {IgnoreNotEnabled}");

                        if (!properLevel) continue;
                        if (IgnoreManual) continue;
                        if (IgnoreNotEnabled) continue;

                        Dictionary<int, float> rewardDict = new();
                        foreach (var reward in mission.RelicXpInfo.OrderBy(x => x.Key))
                        {
                            rewardDict[reward.Key] = reward.Value;
                        }
                        rewardMissions[id] = rewardDict;
                    }
                }

                int bestIndex = -1;
                float bestScore = float.NegativeInfinity;
                IceLogging.Info($"Current viable mission count: {rewardMissions.Count}");

                foreach (var mission in rewardMissions)
                {
                    var id = (int)mission.Key;
                    var reward = mission.Value;
                    float score = 0f;

                    foreach (var rewardEntry in reward)
                    {
                        if (urgencies.TryGetValue(rewardEntry.Key, out var urgency))
                        {
                            score += urgency * rewardEntry.Value;
                        }
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = id;
                    }
                }

                if (bestIndex > 0)
                    return (uint)bestIndex;
                else
                    return 0;
            }
            else
            {
                IceLogging.Info("We're somehow not showing the window, so returning 0.");

                return 0;
            }
        }
    }
}
