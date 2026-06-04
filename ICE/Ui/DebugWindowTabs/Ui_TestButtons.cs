using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using System.Collections.Generic;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using ICE.Utilities;
using ICE.Utilities.Cosmic_Helper;

namespace ICE.Ui.DebugWindowTabs
{
    internal class Ui_TestButtons
    {
        private static Dictionary<int, bool> selectedIcons = new Dictionary<int, bool>();
        private static Dictionary<int, string> iconNames = new Dictionary<int, string>();
        private static string exportedCode = string.Empty;
        private static bool showExportWindow = false;

        // Generated Icon Dictionary
        private static readonly Dictionary<string, string> Icons = new()
        {
            { "uE020", "\uE020" },
            { "uE021", "\uE021" },
            { "uE022", "\uE022" },
            { "uE023", "\uE023" },
            { "uE024", "\uE024" },
            { "uE025", "\uE025" },
            { "uE026", "\uE026" },
            { "uE027", "\uE027" },
            { "uE028", "\uE028" },
            { "uE029", "\uE029" },
            { "uE02A", "\uE02A" },
            { "uE02B", "\uE02B" },
            { "uE031", "\uE031" },
            { "uE032", "\uE032" },
            { "uE033", "\uE033" },
            { "uE034", "\uE034" },
            { "uE035", "\uE035" },
            { "uE037", "\uE037" },
            { "uE038", "\uE038" },
            { "uE039", "\uE039" },
            { "uE03A", "\uE03A" },
            { "uE03B", "\uE03B" },
            { "uE03C", "\uE03C" },
            { "uE03D", "\uE03D" },
            { "uE03E", "\uE03E" },
            { "uE03F", "\uE03F" },
            { "uE040", "\uE040" },
            { "uE041", "\uE041" },
            { "uE042", "\uE042" },
            { "uE043", "\uE043" },
            { "uE044", "\uE044" },
            { "uE048", "\uE048" },
            { "uE049", "\uE049" },
            { "uE04A", "\uE04A" },
            { "uE04B", "\uE04B" },
            { "uE04C", "\uE04C" },
            { "uE04D", "\uE04D" },
            { "uE04E", "\uE04E" },
            { "uE050", "\uE050" },
            { "uE051", "\uE051" },
            { "uE052", "\uE052" },
            { "uE053", "\uE053" },
            { "uE054", "\uE054" },
            { "uE055", "\uE055" },
            { "uE056", "\uE056" },
            { "uE057", "\uE057" },
            { "uE058", "\uE058" },
            { "uE059", "\uE059" },
            { "uE05A", "\uE05A" },
            { "uE05B", "\uE05B" },
            { "uE05C", "\uE05C" },
            { "uE05D", "\uE05D" },
            { "uE05E", "\uE05E" },
            { "uE05F", "\uE05F" },
            { "uE060", "\uE060" },
            { "uE061", "\uE061" },
            { "uE062", "\uE062" },
            { "uE063", "\uE063" },
            { "uE064", "\uE064" },
            { "uE065", "\uE065" },
            { "uE066", "\uE066" },
            { "uE067", "\uE067" },
            { "uE068", "\uE068" },
            { "uE069", "\uE069" },
            { "uE06A", "\uE06A" },
            { "uE06B", "\uE06B" },
            { "uE06C", "\uE06C" },
            { "uE06D", "\uE06D" },
            { "uE06E", "\uE06E" },
            { "uE06F", "\uE06F" },
            { "uE070", "\uE070" },
            { "uE071", "\uE071" },
            { "uE072", "\uE072" },
            { "uE073", "\uE073" },
            { "uE074", "\uE074" },
            { "uE075", "\uE075" },
            { "uE076", "\uE076" },
            { "uE077", "\uE077" },
            { "uE078", "\uE078" },
            { "uE079", "\uE079" },
            { "uE07A", "\uE07A" },
            { "uE07B", "\uE07B" },
            { "uE07C", "\uE07C" },
            { "uE07D", "\uE07D" },
            { "uE07E", "\uE07E" },
            { "uE07F", "\uE07F" },
            { "uE080", "\uE080" },
            { "uE081", "\uE081" },
            { "uE082", "\uE082" },
            { "uE083", "\uE083" },
            { "uE084", "\uE084" },
            { "uE085", "\uE085" },
            { "uE086", "\uE086" },
            { "uE087", "\uE087" },
            { "uE088", "\uE088" },
            { "uE089", "\uE089" },
            { "uE08A", "\uE08A" },
            { "uE08F", "\uE08F" },
            { "uE090", "\uE090" },
            { "uE091", "\uE091" },
            { "uE092", "\uE092" },
            { "uE093", "\uE093" },
            { "uE094", "\uE094" },
            { "uE095", "\uE095" },
            { "uE096", "\uE096" },
            { "uE097", "\uE097" },
            { "uE098", "\uE098" },
            { "uE099", "\uE099" },
            { "uE09A", "\uE09A" },
            { "uE09B", "\uE09B" },
            { "uE09C", "\uE09C" },
            { "uE09D", "\uE09D" },
            { "uE09E", "\uE09E" },
            { "uE09F", "\uE09F" },
            { "uE0A0", "\uE0A0" },
            { "uE0A1", "\uE0A1" },
            { "uE0A2", "\uE0A2" },
            { "uE0A3", "\uE0A3" },
            { "uE0A4", "\uE0A4" },
            { "uE0A5", "\uE0A5" },
            { "uE0A6", "\uE0A6" },
            { "uE0A7", "\uE0A7" },
            { "uE0A8", "\uE0A8" },
            { "uE0A9", "\uE0A9" },
            { "uE0AA", "\uE0AA" },
            { "uE0AB", "\uE0AB" },
            { "uE0AC", "\uE0AC" },
            { "uE0AD", "\uE0AD" },
            { "uE0AE", "\uE0AE" },
            { "uE0AF", "\uE0AF" },
            { "uE0B0", "\uE0B0" },
            { "uE0B1", "\uE0B1" },
            { "uE0B2", "\uE0B2" },
            { "uE0B3", "\uE0B3" },
            { "uE0B4", "\uE0B4" },
            { "uE0B5", "\uE0B5" },
            { "uE0B6", "\uE0B6" },
            { "uE0B7", "\uE0B7" },
            { "uE0B8", "\uE0B8" },
            { "uE0B9", "\uE0B9" },
            { "uE0BA", "\uE0BA" },
            { "uE0BB", "\uE0BB" },
            { "uE0BC", "\uE0BC" },
            { "uE0BD", "\uE0BD" },
            { "uE0BE", "\uE0BE" },
            { "uE0BF", "\uE0BF" },
            { "uE0C0", "\uE0C0" },
            { "uE0C1", "\uE0C1" },
            { "uE0C2", "\uE0C2" },
            { "uE0C3", "\uE0C3" },
            { "uE0C4", "\uE0C4" },
            { "uE0C5", "\uE0C5" },
            { "uE0C6", "\uE0C6" },
            { "uE0D0", "\uE0D0" },
            { "uE0D1", "\uE0D1" },
            { "uE0D2", "\uE0D2" },
            { "uE0D3", "\uE0D3" },
            { "uE0D4", "\uE0D4" },
            { "uE0D5", "\uE0D5" },
            { "uE0D6", "\uE0D6" },
            { "uE0D7", "\uE0D7" },
            { "uE0D8", "\uE0D8" },
            { "uE0D9", "\uE0D9" },
            { "uE0DA", "\uE0DA" },
            { "uE0DB", "\uE0DB" },
            { "uE0E0", "\uE0E0" },
            { "uE0E1", "\uE0E1" },
            { "uE0E2", "\uE0E2" },
            { "uE0E3", "\uE0E3" },
            { "uE0E4", "\uE0E4" },
            { "uE0E5", "\uE0E5" },
            { "uE0E6", "\uE0E6" },
            { "uE0E7", "\uE0E7" },
            { "uE0E8", "\uE0E8" },
            { "uE0E9", "\uE0E9" },
        };

        private static Vector3 WorldPos = Vector3.Zero;
        private static float Scale = 1.0f;
        private static float Height = 1.0f;

        public static unsafe void Draw()
        {
            ImGui.Text($"Current Mission: {CosmicHelper.CurrentLunarMission}");
            ImGui.Text($"Artisan Endurance: {P.Artisan.GetEnduranceStatus()}");

            if (ImGui.Button($"Set Location: {WorldPos}##SetPositionForDraw"))
            {
                var pos = Player.Position;
                WorldPos = pos;
            }
            ImGui.DragFloat("Height", ref Height, 0.1f, 0, 10);
            ImGui.DragFloat("Scale", ref Scale);

            if (WorldPos != Vector3.Zero)
            {
                var size = new Vector2(24 * Scale, 24 * Scale);
                var icon = CosmicHelper.ClassInfoDict[8].JobIcon.GetWrapOrDefault();
                if (icon != null)
                {
                    // PictoManager.DrawIcon(icon.Handle, new(WorldPos.X, WorldPos.Y + Height, WorldPos.Z), size);
                    ImGui.Image(icon.Handle, new(24, 24));
                }
            }

            Ui_WorldIconTEst.DrawControls();
            Ui_WorldIconTEst.DrawOverlay();

            //  4 - Col 2  - Unknown 7
            //  8 - Col 3  - Unknown 0
            // 10 - Col 4  - Unknown 1
            //  3 - Col 7  - Unknown 12
            //  7 - Col 8  - Unknown 2
            //  2 - Col 10 - Unknown 13
            //  5 - Col 11 - Unknown 3
            //  1 - Col 13 - Unknown 14
            //  5 - Col 14 - Unknown 4
            //  0          - Unknown 5
            //  0          - Unknown 6
            //  0          - Unknown 8
            //  1          - Unknown 9 
            //  1          - Unknown 10
            //  1          - Unknown 11

            ImGui.Text($"{WKSManager.Instance()->CurrentMissionUnitRowId}");

            if (ImGui.Button("Test Drone Buy"))
            {
                Task_ArtifactSearch.EnqueueBuy();
            }

            if (ImGui.Button("Find Mission"))
            {
                // TaskMissionFind.Enqueue();
            }
            if (ImGui.Button("Clear Task"))
            {
                P.TaskManager.Abort();
            }
            if (ImGui.Button("Artisan Craft"))
            {
                P.Artisan.CraftItem(36176, 1);
            }
            if (ImGui.Button("RecipeNote"))
            {
                AddonHelper.OpenRecipeNote();
            }
            if (ImGui.TreeNode("All Current objects"))
            {
                if (Player.Available)
                {
                    foreach (var ffObjects in Svc.Objects.OrderBy(x => Player.DistanceTo(x.Position)))
                    {
                        if (ffObjects.BaseId == 2014616 || ffObjects.BaseId == 2014618)
                        {
                            ImGui.Text($"--> Name: {ffObjects.Name} | ID: {ffObjects.BaseId}");
                        }
                        else
                        {
                            ImGui.Text($"Name: {ffObjects.Name} | ID: {ffObjects.BaseId}");
                        }
                    }
                }

                ImGui.TreePop();
            }
            var gameObject = Utils.TryGetObjectNearestEventObject();
            float gameObjectDistance = 0;
            if (gameObject is not null)
                gameObjectDistance = Player.DistanceTo(gameObject);
            if (ImGui.Button("Click Nearest EventObject"))
            {
                Utils.TargetgameObjectTask(gameObject);
                Utils.InteractWithObject(gameObject);
            }
            ImGui.SameLine();
            ImGui.Text($"Distance to nearest: {gameObjectDistance}");

            var collectionPoint = Utils.TryGetObjectCollectionPoint();
            float collectionPointDistance = 0;
            if (collectionPoint is not null)
                collectionPointDistance = Player.DistanceTo(collectionPoint);
            if (ImGui.Button("Click Nearest Collection Point"))
            {
                Utils.TargetgameObjectTask(collectionPoint);
                Utils.InteractWithObject(collectionPoint);
            }
            ImGui.SameLine();
            ImGui.Text($"Distance to nearest: {collectionPointDistance}");

            if (ImGui.Button("Print GatheringPoint Info"))
            {
                var gatheringPoint = Player.Object?.TargetObject;
                if (gatheringPoint is not null)
                {
                    var nodeId = gatheringPoint.BaseId;
                    var position = gatheringPoint.Position;
                    var landZone = gatheringPoint.Position;
                    var gatheringType = Player.Job == Job.MIN ? 2 : 3;
                    var currentMission = CosmicHelper.CurrentMissionInfo;
                    var nodeSet = currentMission?.MapPosition ?? new Vector2(0, 0);

                    // In cosmic zone use where you are; otherwise Sinus so clipboard export still works.
                    var zoneId = PlayerHelper.IsInCosmicZone()
                        ? Player.Territory.RowId
                        : CosmicMoonRegistry.Sinus.TerritoryId;
                    string info = $"new GathNodeInfo\n{{\n    ZoneId = {zoneId},\n    NodeId = {nodeId},\n    Position = new Vector3({position.X}f, {position.Y}f, {position.Z}f),\n    LandZone = new Vector3({landZone.X}f, {landZone.Y}f, {landZone.Z}f),\n    GatheringType = {gatheringType},\n    NodeSet = {nodeSet}\n}}";

                    ImGui.SetClipboardText(info);
                    Svc.Chat.Print(info);
                }
                else
                {
                    Svc.Chat.Print("No GatheringPoint targeted.");
                }
            }

            if (ImGui.Button("Switch class to CRP"))
            {
                GearsetHandler.TaskClassChange(Job.CRP);
            }
            if (ImGui.Button("Switch class to MIN"))
            {
                GearsetHandler.TaskClassChange(Job.MIN);
            }
            if (ImGui.Button("Relic Turnin"))
            {
                Task_RelicTurnin.Enqueue();
            }

            if (ImGui.CollapsingHeader("Square custom font"))
            {
                DrawIconSelector();
            }

            if (ImGui.CollapsingHeader("Font Test"))
            {
                for (int i = 0xE000; i <= 0xE0FF; i++)
                {
                    char icon = (char)i;
                    string unicodeString = $"\\u{i:X3}"; // Formats as \uE000, \uE001, etc.

                    // If using ImGui (common in Dalamud plugins):
                    ImGui.Text($"{unicodeString}: {icon}");
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    {
                        ImGui.SetClipboardText($"{unicodeString}");
                    }

                    // Or just console/log output:
                    Console.WriteLine($"{unicodeString}: {icon}");
                }
            }

            // Draw export window if open
            if (showExportWindow)
            {
                DrawExportWindow();
            }

            if (ImGui.CollapsingHeader("View All SE Custom Fonts (That's known"))
            {
                foreach (var fontIcon in Icons)
                {
                    ImGui.Text($"{fontIcon.Key} -> {fontIcon.Value}");
                }
            }
            ImGui.Text($"Mission Timer: {AddonHelper.GetNodeText("WKSMissionInfomation", 24)}");
            if (ImGui.Button("Move Item"))
            {
                MoveItem();
            }
        }

        private static void DrawIconSelector()
        {
            if (ImGui.Button("Export Selected to Dictionary"))
            {
                ExportSelectedIcons();
                showExportWindow = true;
            }

            ImGui.SameLine();
            if (ImGui.Button("Clear All Selections"))
            {
                selectedIcons.Clear();
                iconNames.Clear();
            }

            ImGui.SameLine();
            ImGui.Text($"Selected: {selectedIcons.Count(kvp => kvp.Value)} icons");

            ImGui.Separator();

            // Scrollable area for icons
            ImGui.BeginChild("IconSelectorList", new System.Numerics.Vector2(0, 300), true);

            // Iterate through the Private Use Area
            for (int i = 0xE000; i <= 0xE0FF; i++)
            {
                string code = $"\\u{i:X4}";
                string iconChar = ((char)i).ToString();

                // Checkbox for selection
                bool isSelected = selectedIcons.ContainsKey(i) && selectedIcons[i];
                if (ImGui.Checkbox($"##{i}", ref isSelected))
                {
                    selectedIcons[i] = isSelected;
                    if (!isSelected && iconNames.ContainsKey(i))
                    {
                        iconNames.Remove(i);
                    }
                }

                ImGui.SameLine();

                // Show the code
                ImGui.Text(code);

                ImGui.SameLine();

                // Show the actual icon
                ImGui.Text(iconChar);

                ImGui.SameLine();

                // Input field for naming the icon (only if selected) - OPTIONAL
                if (isSelected)
                {
                    string name = iconNames.ContainsKey(i) ? iconNames[i] : "";
                    ImGui.SetNextItemWidth(200);
                    if (ImGui.InputText($"##name{i}", ref name, 100))
                    {
                        iconNames[i] = name;
                    }
                    ImGui.SameLine();
                    ImGui.TextDisabled("(optional custom name)");
                }
                else
                {
                    ImGui.TextDisabled("(select to add optional name)");
                }
            }

            ImGui.EndChild();
        }

        private static void ExportSelectedIcons()
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Generated Icon Dictionary");
            sb.AppendLine("private static readonly Dictionary<string, string> Icons = new()");
            sb.AppendLine("{");

            foreach (var kvp in selectedIcons.OrderBy(x => x.Key))
            {
                if (kvp.Value) // Only export selected ones
                {
                    int codePoint = kvp.Key;
                    string code = $"\\u{codePoint:X4}";

                    // Use custom name if provided, otherwise use the unicode string itself
                    string name = iconNames.ContainsKey(codePoint) && !string.IsNullOrWhiteSpace(iconNames[codePoint])
                        ? CleanIdentifier(iconNames[codePoint])
                        : code.Replace("\\", ""); // Remove backslash for dictionary key, e.g., "uE0E9"

                    sb.AppendLine($"    {{ \"{name}\", \"{code}\" }},");
                }
            }

            sb.AppendLine("};");

            exportedCode = sb.ToString();
        }

        private static void DrawExportWindow()
        {
            ImGui.Begin("Exported Icon Dictionary", ref showExportWindow);

            if (ImGui.Button("Copy to Clipboard"))
            {
                ImGui.SetClipboardText(exportedCode);
                Svc.Chat.Print("Dictionary code copied to clipboard!");
            }

            ImGui.Separator();

            ImGui.BeginChild("CodeOutput", new System.Numerics.Vector2(0, 0), true);
            ImGui.TextWrapped(exportedCode);
            ImGui.EndChild();

            ImGui.End();
        }

        private static string CleanIdentifier(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "UnnamedIcon";

            // Remove invalid characters and replace spaces with underscores
            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else if (c == ' ')
                    sb.Append('_');
            }

            string result = sb.ToString();

            // Ensure it doesn't start with a number
            if (!string.IsNullOrEmpty(result) && char.IsDigit(result[0]))
                result = "_" + result;

            return string.IsNullOrEmpty(result) ? "UnnamedIcon" : result;
        }

        private static unsafe void MoveItem()
        {
            InventoryManager.Instance()->MoveItemSlot(InventoryType.Inventory4, 0, InventoryType.Inventory4, 1, false);
        }
    }
}