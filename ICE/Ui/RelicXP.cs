using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using Lumina.Excel.Sheets;

namespace ICE.Ui
{
    internal class RelicXP
    {
        private float current = 25f;

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
            var nextstate = wksManager->ResearchModule->UnlockedStages[toolClassId - 1];

            // Unsure... why this is here? 
            if (Svc.Data.GetExcelSheet<WKSCosmoToolClass>().TryGetRow(toolClassId, out var row))
            {

            }

            Dictionary<uint, XPType> XPTable = new Dictionary<uint, XPType>();

            for (byte type = 1; type < 6; type++)
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

            ImGui.Text($"Stage: {stage}");

            if (stage != 14)
            {
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
                    else
                        xpType = "???";

                    DrawXPBar($"Type: {xpType}", current, needed, size, max);
                }
            }
            else
            {
                float windowSize = ImGui.GetWindowSize().X - 20;
                Vector2 size = new Vector2(windowSize, 10);

                var (classScore, cappedClassScore, totalScores, classId) = MissionHandler.GetCosmicClassScores();

                DrawXPBar("Score", (uint)classScore, 0, size, 500_000);
            }
        }

        private static void DrawXPBar(string label, uint currentXP, uint neededXP, Vector2 size, uint maxXP = 0)
        {
            // Handle capped and invalid data
            float fraction;
            if (neededXP == 0)
            {
                fraction = Math.Clamp((float)currentXP / (float)maxXP, 0f, 1f);
            }
            else
            {
                fraction = Math.Clamp((float)currentXP / (float)neededXP, 0f, 1f);
            }

            // Display correct text
            string displayText = (neededXP == 0 && maxXP > 0)
                ? $"{label}: {currentXP:N0} / {maxXP:N0}"
                : $"{label}: {currentXP:N0} / {neededXP:N0}";

            ImGui.Text(displayText);

            // Draw bar
            var pos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();

            var barStart = pos;
            var barEnd = new Vector2(pos.X + size.X, pos.Y + size.Y);

            drawList.AddRectFilled(barStart, barEnd, ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.15f, 1f)));

            float filledWidth = size.X * fraction;
            if (filledWidth > 0f)
            {
                var filledEnd = new Vector2(pos.X + filledWidth, pos.Y + size.Y);
                var left = new Vector4(0.2f, 0.6f, 1f, 1f); // Blue #3399ff
                var right = new Vector4(0.6f, 1f, 0.8f, 1f); // Green #99ffcc
                if (currentXP > neededXP)
                {
                    if (currentXP >= maxXP)
                    {
                        left = new Vector4(1f, 0.84f, 0f, 1f); // Gold #ffd600
                        right = new Vector4(1f, 0.84f, 0f, 1f); // Gold #ffd600
                    }
                    else
                    {
                        left = new Vector4(0.6f, 1f, 0.8f, 1f); // Green #99ffcc
                        right = new Vector4(0.2f, 0.6f, 1f, 1f); // Blue #3399ff
                    }
                }

                drawList.AddRectFilledMultiColor(
                    barStart,
                    filledEnd,
                    ImGui.GetColorU32(left), // top-left
                    ImGui.GetColorU32(right), // top-right
                    ImGui.GetColorU32(right), // bottom-right
                    ImGui.GetColorU32(left)  // bottom-left
                );
            }

            ImGui.Dummy(new Vector2(size.X, size.Y + 5));
        }
    }
}
