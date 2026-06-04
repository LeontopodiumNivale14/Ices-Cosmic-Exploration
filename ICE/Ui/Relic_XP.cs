using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Utilities.Cosmic_Helper;
using System.Collections.Generic;namespace ICE.Ui
{
    internal class Relic_XP
    {
        private static bool ShowXP = C.ShowExpBars;

        private class XPType
        {
            public uint CurrentXP { get; set; }
            public uint NeededXP { get; set; }
            public uint MaxXP { get; set; }
        }
        public static unsafe void DrawRelicXP(uint selectedJob)
        {
            var wksManager = WKSManager.Instance();
            if (wksManager == null || wksManager->ResearchModule == null || !wksManager->ResearchModule->IsLoaded)
                return;

            var job = selectedJob;
            var toolClassId = (byte)(job - 7);
            var stage = wksManager->ResearchModule->CurrentStages[toolClassId - 1];

            Dictionary<uint, XPType> XPTable = new Dictionary<uint, XPType>();

            for (byte type = 1; type < 7; type++)
            {
                if (!wksManager->ResearchModule->IsTypeAvailable(toolClassId, type))
                    break;

                var neededXP = wksManager->ResearchModule->GetNeededAnalysis(toolClassId, type);

                var maxXP = wksManager->ResearchModule->GetMaxAnalysis(toolClassId, type);

                var currentXp = wksManager->ResearchModule->GetCurrentAnalysis(toolClassId, type);
                if (!XPTable.ContainsKey(type))
                {
                    XPTable[type] = new XPType()
                    {
                        CurrentXP = currentXp,
                        NeededXP = neededXP,
                        MaxXP = maxXP,
                    };
                }
            }

            bool MaxStage = XPTable.Where(x => x.Value.NeededXP != 0).Count() == 0;

            var maxRelicStage = CosmicMoonRegistry.GetMaxRelicStage((uint)Svc.ClientState.TerritoryType);

            ImGui.Text($"Stage: {stage}");
            if (MaxStage)
            {
                ImGui.SameLine();
                ImGui.Text("[MAX]");
            }
            foreach (var type in XPTable)
            {
                uint current = type.Value.CurrentXP;
                uint needed = type.Value.NeededXP;
                uint max = type.Value.MaxXP;
                float windowSize = ImGui.GetWindowSize().X - 20;
                Vector2 size = new Vector2(windowSize, 10);

                string overlay = $"ID: {current} / {max}";
                string xpType = "";
                if (type.Key == 1)
                    xpType = "I";
                else if (type.Key == 2)
                    xpType = "II";
                else if (type.Key == 3)
                    xpType = "III";
                else if (type.Key == 4)
                    xpType = "IV";
                else if (type.Key == 5)
                    xpType = "V";
                else if (type.Key == 6)
                    xpType = "VI";
                else
                    xpType = "???";

                if (stage != maxRelicStage)
                {
                    DrawXPBar($"Type: {xpType}", current, needed, size, max);
                }
                else
                {
                    DrawXPBar($"Type: {xpType}", current, max, size, max);
                }
            }
        }

        public static void DrawXPBar(string label, uint currentXP, uint neededXP, Vector2 size, uint maxXP = 0)
        {
            // Display text - keep showing current/needed unless neededXP is 0
            string displayText = (neededXP == 0)
                ? $"{label}: {currentXP:N0} / {maxXP:N0}"
                : $"{label}: {currentXP:N0} / {neededXP:N0}";

            ImGui.TextWrapped(displayText);

            // Draw bar
            var pos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();

            var barStart = pos;
            var barEnd = new Vector2(pos.X + size.X, pos.Y + size.Y);

            // Draw background (dark gray)
            drawList.AddRectFilled(barStart, barEnd, ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.15f, 1f)));

            // Case 1: Current XP hasn't reached needed XP yet
            if (currentXP <= neededXP && neededXP > 0)
            {
                // Special case: if needed XP equals max XP and we're at full, show gold
                if (neededXP == maxXP && currentXP >= neededXP)
                {
                    var goldColor = new Vector4(1f, 0.84f, 0f, 1f); // Gold #ffd600
                    drawList.AddRectFilled(barStart, barEnd, ImGui.GetColorU32(goldColor));
                }
                else
                {
                    // Draw blue to green gradient up to current XP
                    float fraction = Math.Clamp((float)currentXP / (float)neededXP, 0f, 1f);
                    float filledWidth = size.X * fraction;

                    if (filledWidth > 0f)
                    {
                        var filledEnd = new Vector2(pos.X + filledWidth, pos.Y + size.Y);
                        var left = new Vector4(0.2f, 0.6f, 1f, 1f); // Blue #3399ff
                        var right = new Vector4(0.6f, 1f, 0.8f, 1f); // Green #99ffcc

                        drawList.AddRectFilledMultiColor(
                            barStart,
                            filledEnd,
                            ImGui.GetColorU32(left), // top-left
                            ImGui.GetColorU32(right), // top-right
                            ImGui.GetColorU32(right), // bottom-right
                            ImGui.GetColorU32(left)  // bottom-left
                        );
                    }
                }
            }
            // Case 2: Current XP has exceeded needed XP (overcapped)
            else if (currentXP > neededXP && maxXP > 0)
            {
                // Drawing the inital xp bar filling for blue/green
                if (neededXP > 0)
                {
                    var neededEnd = new Vector2(pos.X + size.X, pos.Y + size.Y);
                    var left = new Vector4(0.2f, 0.6f, 1f, 1f); // Blue #3399ff
                    var right = new Vector4(0.6f, 1f, 0.8f, 1f); // Green #99ffcc

                    drawList.AddRectFilledMultiColor(
                        barStart,
                        neededEnd,
                        ImGui.GetColorU32(left), // top-left
                        ImGui.GetColorU32(right), // top-right
                        ImGui.GetColorU32(right), // bottom-right
                        ImGui.GetColorU32(left)  // bottom-left
                    );
                }

                // Draw the overcap portion (gold) filling from left to right
                uint overcapAmount = currentXP - neededXP;
                uint overcapRange = maxXP - neededXP;

                if (overcapRange > 0)
                {
                    float overcapFraction = Math.Clamp((float)overcapAmount / (float)overcapRange, 0f, 1f);
                    float goldWidth = size.X * overcapFraction;

                    if (goldWidth > 0f)
                    {
                        var goldEnd = new Vector2(pos.X + goldWidth, pos.Y + size.Y);
                        var goldColor = new Vector4(1f, 0.84f, 0f, 1f); // Gold #ffd600

                        drawList.AddRectFilled(
                            barStart,
                            goldEnd,
                            ImGui.GetColorU32(goldColor)
                        );
                    }
                }
            }
            // Special handling when neededXP is 0 (like the cosmic score case)
            else if (neededXP == 0 && maxXP > 0)
            {
                float fraction = Math.Clamp((float)currentXP / (float)maxXP, 0f, 1f);
                float filledWidth = size.X * fraction;

                if (filledWidth > 0f)
                {
                    var filledEnd = new Vector2(pos.X + filledWidth, pos.Y + size.Y);
                    var goldColor = new Vector4(1f, 0.84f, 0f, 1f); // Gold #ffd600

                    drawList.AddRectFilled(barStart, filledEnd, ImGui.GetColorU32(goldColor));
                }
            }

            ImGui.Dummy(new Vector2(size.X, size.Y + 5));
        }

        public unsafe static (uint TotalScore, uint TotalComplete, uint MaxScore, Dictionary<uint, uint> ScoreInfo) GetTotalScores()
        {
            uint totalScore = 0;
            uint totalComplete = 0;
            uint maxScore = 5_500_000;
            Dictionary<uint, uint> ClassInfo = new();
            var wksManager = WKSManager.Instance();


            foreach (var crafterJob in CosmicHelper.CrafterJobList)
            {
                uint classScore = 0;
                var score = wksManager->Scores;
                int jobId = (int)crafterJob;
                classScore = (uint)score[jobId-8];
                classScore = Math.Min(500_000, classScore);
                if (classScore == 500_000)
                    totalComplete += 1;
                totalScore += classScore;
                ClassInfo[crafterJob] = classScore;
            }

            foreach (var gatherJob in CosmicHelper.GatheringJobList)
            {
                uint classScore = 0;
                var score = wksManager->Scores;
                int jobId = (int)gatherJob;
                classScore = (uint)score[jobId-8];
                classScore = Math.Min(500_000, classScore);
                if (classScore == 500_000)
                    totalComplete += 1;
                totalScore += classScore;
                ClassInfo[gatherJob] = classScore;
            }

            return (totalScore, totalComplete, maxScore, ClassInfo);
        }
    }
}