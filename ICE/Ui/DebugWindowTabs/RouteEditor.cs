using System.Collections.Generic;
using System.Text;
using static ICE.Utilities.GatheringUtil;

namespace ICE.Ui.DebugWindowTabs;

public class RouteEditor
{
    private static List<GathNodeInfo> nodeList;
    private static int selectedNodeIndex = -1;
    private static int selectedNodeSetFilter = 0; // 0 = All
    private static int gatheringTypeFilter = 0; // 0 = All, 2 = Miner, 3 = Botanist
    private static string searchFilter = "";
    private static bool showPositionEditor = true;
    private static bool showLandZoneEditor = true;
    private static bool showAdvancedFilters = false;

    // For adding new nodes
    private static bool showAddNodeWindow = false;
    private static GathNodeInfo newNode = new GathNodeInfo();


    private static bool showBulkEditWindow = false;
    private static Vector3 bulkPositionOffset = Vector3.Zero;
    private static Vector3 bulkLandZoneOffset = Vector3.Zero;
    private static List<int> selectedIndices = new List<int>();

    // For code export
    private static bool showExportWindow = false;
    private static string exportedCode = "";
    private static bool exportSelected = false;
    private static bool includeComments = true;
    private static bool formatForRegions = true;

    // Node set selection
    private static int selectedNodeSetIndex = 0;
    private static string[] nodeSetNames;
    private static uint[] nodeSetIds;
    private static bool nodeSetInitialized = false;

    // Sorting
    private enum SortBy { NodeId, NodeSet, GatheringType, Position, None }
    private static SortBy currentSort = SortBy.None;
    private static bool sortAscending = true;

    public static void Initialize()
    {
        InitializeNodeSetData();
        // Start with the first node set if available
        if (nodeSetIds != null && nodeSetIds.Length > 0)
        {
            LoadNodeSetData(nodeSetIds[0]);
        }
        else
        {
            nodeList = new List<GathNodeInfo>();
        }
    }

    public static void Draw()
    {
        // Initialize node set data if not done yet
        if (!nodeSetInitialized)
        {
            Initialize();
        }

        // Node Set Selector at the top
        DrawNodeSetSelector();
        ImGui.Separator();

        if (nodeList == null || nodeList.Count == 0)
        {
            ImGui.Text("No nodes found for the selected node set.");
            return;
        }

        DrawToolbar();
        ImGui.Separator();

        DrawFilterSection();
        ImGui.Separator();

        DrawNodeList();

        if (selectedNodeIndex >= 0 && selectedNodeIndex < nodeList.Count)
        {
            ImGui.Separator();
            DrawNodeEditor();
        }

        // Draw additional windows
        if (showAddNodeWindow)
            DrawAddNodeWindow();

        if (showBulkEditWindow)
            DrawBulkEditWindow();

        if (showExportWindow)
            DrawExportWindow();
    }

    private static void DrawNodeSetSelector()
    {
        ImGui.Text("Select Node Set:");
        ImGui.SetNextItemWidth(400);
        if (ImGui.Combo("##NodeSetSelector", ref selectedNodeSetIndex, nodeSetNames, nodeSetNames.Length))
        {
            // Load the selected node set
            LoadNodeSetData(nodeSetIds[selectedNodeSetIndex]);
            selectedNodeIndex = -1; // Reset selection
        }

        ImGui.SameLine();
        ImGui.Text($"Current Set: {nodeSetIds[selectedNodeSetIndex]} | Nodes: {nodeList?.Count ?? 0}");
    }

    private static void InitializeNodeSetData()
    {
        if (nodeSetInitialized) return;

        var nodeSetPairs = GatheringUtil.Nodeset.OrderBy(kvp => kvp.Value).ToList();
        nodeSetNames = nodeSetPairs.Select(kvp =>
        {
            var pos = kvp.Key;
            var setId = kvp.Value;
            var type = setId <= 10 ? "Miner" : setId <= 20 ? "Botanist" : "Critical";
            return $"Set {setId} ({type}) - ({pos.X:F0}, {pos.Y:F0})";
        }).ToArray();
        nodeSetIds = nodeSetPairs.Select(kvp => kvp.Value).ToArray();
        nodeSetInitialized = true;
    }

    private static void LoadNodeSetData(uint nodeSetId)
    {
        // Get nodes from MoonNodeInfoList that match the selected node set
        var filteredNodes = GatheringUtil.MoonNodeInfoList.Where(n => n.NodeSet == nodeSetId).ToList();

        // Create a new list with copies of the nodes so we can edit them without affecting the original
        nodeList = filteredNodes.Select(n => new GathNodeInfo
        {
            Position = n.Position,
            LandZone = n.LandZone,
            NodeId = n.NodeId,
            GatheringType = n.GatheringType,
            ZoneId = n.ZoneId,
            NodeSet = n.NodeSet
        }).ToList();
    }

    private static void DrawToolbar()
    {
        if (ImGui.Button("Add Node"))
        {
            showAddNodeWindow = true;
            newNode = new GathNodeInfo
            {
                ZoneId = 1237,
                GatheringType = 2,
                NodeSet = nodeSetIds[selectedNodeSetIndex] // Use current selected node set
            };
        }

        ImGui.SameLine();
        if (ImGui.Button("Duplicate Selected") && selectedNodeIndex >= 0)
        {
            var original = nodeList[selectedNodeIndex];
            var duplicate = new GathNodeInfo
            {
                Position = original.Position,
                LandZone = original.LandZone,
                NodeId = GetNextAvailableNodeId(),
                GatheringType = original.GatheringType,
                ZoneId = original.ZoneId,
                NodeSet = original.NodeSet
            };
            nodeList.Add(duplicate);
        }

        ImGui.SameLine();
        if (ImGui.Button("Delete Selected") && selectedNodeIndex >= 0)
        {
            if (ImGui.GetIO().KeyCtrl) // Require Ctrl+Click for safety
            {
                nodeList.RemoveAt(selectedNodeIndex);
                selectedNodeIndex = -1;
            }
            else
            {
                ImGui.SetTooltip("Hold Ctrl and click to delete");
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Bulk Edit"))
        {
            showBulkEditWindow = true;
            selectedIndices.Clear();
        }

        ImGui.SameLine();
        if (ImGui.Button("Export C# Code"))
        {
            showExportWindow = true;
            GenerateCode();
        }

        ImGui.SameLine();
        ImGui.Text($"Total Nodes: {nodeList.Count}");
    }

    private static void DrawFilterSection()
    {
        ImGui.Text("Filters:");

        // Search filter
        ImGui.InputText("Search (Node ID)", ref searchFilter, 100);
        ImGui.SameLine();

        // Node Set filter
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("Node Set", ref selectedNodeSetFilter))
        {
            if (selectedNodeSetFilter < 0) selectedNodeSetFilter = 0;
        }
        ImGui.SameLine();

        // Gathering Type filter
        ImGui.SetNextItemWidth(120);
        if (ImGui.Combo("Type", ref gatheringTypeFilter, "All\0Miner (2)\0Botanist (3)\0"))
        {
            // Combo handles the selection
        }

        if (ImGui.Button("Clear Filters"))
        {
            searchFilter = "";
            selectedNodeSetFilter = 0;
            gatheringTypeFilter = 0;
        }

        ImGui.SameLine();
        ImGui.Checkbox("Advanced Filters", ref showAdvancedFilters);

        if (showAdvancedFilters)
        {
            ImGui.Indent();
            ImGui.Checkbox("Show Position Editor", ref showPositionEditor);
            ImGui.SameLine();
            ImGui.Checkbox("Show LandZone Editor", ref showLandZoneEditor);
            ImGui.Unindent();
        }
    }

    private static void DrawNodeList()
    {
        var filteredNodes = GetFilteredNodes();

        if (ImGui.BeginTable("NodeTable", 6, ImGuiTableFlags.Resizable | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("Node ID", ImGuiTableColumnFlags.DefaultSort);
            ImGui.TableSetupColumn("Node Set");
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("Position");
            ImGui.TableSetupColumn("Land Zone");
            ImGui.TableSetupColumn("Actions");
            ImGui.TableHeadersRow();

            // Handle sorting
            var sortSpecs = ImGui.TableGetSortSpecs();
            if (sortSpecs.SpecsDirty)
            {
                HandleSorting(filteredNodes, sortSpecs);
                sortSpecs.SpecsDirty = false;
            }

            for (int i = 0; i < filteredNodes.Count; i++)
            {
                var node = filteredNodes[i];
                var originalIndex = nodeList.IndexOf(node);

                ImGui.TableNextRow();

                // Node ID
                ImGui.TableSetColumnIndex(0);
                bool isSelected = originalIndex == selectedNodeIndex;
                if (ImGui.Selectable($"{node.NodeId}##node_{originalIndex}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                {
                    selectedNodeIndex = originalIndex;
                }

                // Node Set
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"{node.NodeSet}");

                // Type
                ImGui.TableSetColumnIndex(2);
                ImGui.Text(node.GatheringType == 2 ? "Miner" : node.GatheringType == 3 ? "Botanist" : "Unknown");

                // Position
                ImGui.TableSetColumnIndex(3);
                ImGui.Text($"({node.Position.X:F2}, {node.Position.Y:F2}, {node.Position.Z:F2})");

                // Land Zone
                ImGui.TableSetColumnIndex(4);
                ImGui.Text($"({node.LandZone.X:F2}, {node.LandZone.Y:F2}, {node.LandZone.Z:F2})");

                // Actions
                ImGui.TableSetColumnIndex(5);
                if (ImGui.SmallButton($"Edit##edit_{originalIndex}"))
                {
                    selectedNodeIndex = originalIndex;
                }
            }

            ImGui.EndTable();
        }
    }

    private static void DrawNodeEditor()
    {
        var node = nodeList[selectedNodeIndex];

        ImGui.Text($"Editing Node {selectedNodeIndex + 1}");

        // Basic info
        var nodeId = node.NodeId;
        if (ImGui.InputUInt("Node ID", ref nodeId))
            node.NodeId = nodeId;

        var nodeSet = (int)node.NodeSet;
        if (ImGui.InputInt("Node Set", ref nodeSet))
            node.NodeSet = (uint)Math.Max(0, nodeSet);

        var gatheringType = node.GatheringType;
        if (ImGui.Combo("Gathering Type", ref gatheringType, "Unknown\0\0Miner\0Botanist\0"))
        {
            node.GatheringType = gatheringType == 1 ? 2 : gatheringType == 2 ? 3 : gatheringType;
        }

        var zoneId = node.ZoneId;
        if (ImGui.InputInt("Zone ID", ref zoneId))
            node.ZoneId = zoneId;

        // Position editing
        if (showPositionEditor)
        {
            ImGui.Separator();
            ImGui.Text("Position:");
            var pos = node.Position;
            if (ImGui.DragFloat3("##position", ref pos, 0.1f))
                node.Position = pos;
        }

        // Land Zone editing
        if (showLandZoneEditor)
        {
            ImGui.Separator();
            ImGui.Text("Land Zone:");
            var landZone = node.LandZone;
            if (ImGui.DragFloat3("##landzone", ref landZone, 0.1f))
                node.LandZone = landZone;
        }

        // Quick actions
        ImGui.Separator();
        if (ImGui.Button("Copy Position to Land Zone"))
        {
            node.LandZone = node.Position;
        }
        ImGui.SameLine();
        if (ImGui.Button("Offset Land Zone"))
        {
            node.LandZone = new Vector3(node.Position.X, node.Position.Y - 1.0f, node.Position.Z);
        }
    }

    private static void DrawAddNodeWindow()
    {
        if (!ImGui.Begin("Add New Node", ref showAddNodeWindow))
        {
            ImGui.End();
            return;
        }

        var nodeId = newNode.NodeId;
        ImGui.InputUInt("Node ID", ref nodeId);
        newNode.NodeId = nodeId;

        var nodeSet = (int)newNode.NodeSet;
        ImGui.InputInt("Node Set", ref nodeSet);
        newNode.NodeSet = (uint)Math.Max(0, nodeSet);

        var gatheringType = newNode.GatheringType == 2 ? 0 : newNode.GatheringType == 3 ? 1 : 2;
        if (ImGui.Combo("Gathering Type", ref gatheringType, "Miner\0Botanist\0Unknown\0"))
        {
            newNode.GatheringType = gatheringType == 0 ? 2 : gatheringType == 1 ? 3 : 0;
        }

        var zoneId = newNode.ZoneId;
        if (ImGui.InputInt("Zone ID", ref zoneId))
            newNode.ZoneId = zoneId;

        var pos = newNode.Position;
        ImGui.DragFloat3("Position", ref pos, 0.1f);
        newNode.Position = pos;

        var landZone = newNode.LandZone;
        ImGui.DragFloat3("Land Zone", ref landZone, 0.1f);
        newNode.LandZone = landZone;

        if (ImGui.Button("Add Node"))
        {
            nodeList.Add(new GathNodeInfo
            {
                Position = newNode.Position,
                LandZone = newNode.LandZone,
                NodeId = newNode.NodeId,
                GatheringType = newNode.GatheringType,
                ZoneId = newNode.ZoneId,
                NodeSet = newNode.NodeSet
            });
            showAddNodeWindow = false;
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            showAddNodeWindow = false;
        }

        ImGui.End();
    }

    private static void DrawBulkEditWindow()
    {
        if (!ImGui.Begin("Bulk Edit", ref showBulkEditWindow))
        {
            ImGui.End();
            return;
        }

        ImGui.Text("Select nodes to edit:");

        // Simple selection list
        for (int i = 0; i < nodeList.Count; i++)
        {
            bool selected = selectedIndices.Contains(i);
            if (ImGui.Checkbox($"Node {nodeList[i].NodeId} (Set {nodeList[i].NodeSet})##bulk_{i}", ref selected))
            {
                if (selected)
                    selectedIndices.Add(i);
                else
                    selectedIndices.Remove(i);
            }
        }

        ImGui.Separator();
        ImGui.Text("Bulk Operations:");

        ImGui.DragFloat3("Position Offset", ref bulkPositionOffset, 0.1f);
        if (ImGui.Button("Apply Position Offset"))
        {
            foreach (int idx in selectedIndices)
            {
                if (idx >= 0 && idx < nodeList.Count)
                {
                    nodeList[idx].Position += bulkPositionOffset;
                }
            }
        }

        ImGui.DragFloat3("Land Zone Offset", ref bulkLandZoneOffset, 0.1f);
        if (ImGui.Button("Apply Land Zone Offset"))
        {
            foreach (int idx in selectedIndices)
            {
                if (idx >= 0 && idx < nodeList.Count)
                {
                    nodeList[idx].LandZone += bulkLandZoneOffset;
                }
            }
        }

        if (ImGui.Button("Close"))
        {
            showBulkEditWindow = false;
        }

        ImGui.End();
    }

    private static void DrawExportWindow()
    {
        ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Export C# Code", ref showExportWindow))
        {
            ImGui.End();
            return;
        }

        ImGui.Text("Export Options:");
        ImGui.Checkbox("Export only selected nodes", ref exportSelected);
        ImGui.Checkbox("Include comments", ref includeComments);
        ImGui.Checkbox("Format with regions", ref formatForRegions);

        if (ImGui.Button("Regenerate Code"))
        {
            GenerateCode();
        }

        ImGui.SameLine();
        if (ImGui.Button("Copy to Clipboard"))
        {
            ImGui.SetClipboardText(exportedCode);
        }

        ImGui.Separator();

        // Display the generated code in a scrollable text area
        ImGui.Text("Generated C# Code:");
        ImGui.BeginChild("CodeDisplay", new Vector2(0, -30), true);
        ImGui.TextUnformatted(exportedCode);
        ImGui.EndChild();

        if (ImGui.Button("Close"))
        {
            showExportWindow = false;
        }

        ImGui.End();
    }

    private static void GenerateCode()
    {
        var nodesToExport = exportSelected && selectedIndices.Count > 0
            ? selectedIndices.Select(i => nodeList[i]).ToList()
            : nodeList;

        if (nodesToExport.Count == 0)
        {
            exportedCode = "// No nodes to export";
            return;
        }

        var code = new StringBuilder();

        if (includeComments)
        {
            code.AppendLine("// Generated by Route Editor");
            code.AppendLine($"// Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            code.AppendLine($"// Total Nodes: {nodesToExport.Count}");
            code.AppendLine();
        }

        // Group nodes by NodeSet and GatheringType for organized output
        var groupedNodes = nodesToExport
            .GroupBy(n => new { n.GatheringType, n.NodeSet })
            .OrderBy(g => g.Key.GatheringType)
            .ThenBy(g => g.Key.NodeSet);

        foreach (var group in groupedNodes)
        {
            var gatheringTypeName = group.Key.GatheringType == 2 ? "Miner" :
                                   group.Key.GatheringType == 3 ? "Botanist" : "Unknown";

            if (formatForRegions)
            {
                code.AppendLine($"        #region {gatheringTypeName} Set #{group.Key.NodeSet}");
                code.AppendLine();
            }
            else if (includeComments)
            {
                code.AppendLine($"        // {gatheringTypeName} Set #{group.Key.NodeSet}");
            }

            foreach (var node in group.OrderBy(n => n.NodeId))
            {
                code.AppendLine("        new GathNodeInfo");
                code.AppendLine("        {");
                code.AppendLine($"            ZoneId = {node.ZoneId},");
                code.AppendLine($"            NodeId = {node.NodeId},");
                code.AppendLine($"            Position = new Vector3({node.Position.X:F2}f, {node.Position.Y:F2}f, {node.Position.Z:F2}f),");
                code.AppendLine($"            LandZone = new Vector3({node.LandZone.X:F2}f, {node.LandZone.Y:F2}f, {node.LandZone.Z:F2}f),");
                code.AppendLine($"            GatheringType = {node.GatheringType},");
                code.AppendLine($"            NodeSet = {node.NodeSet}");
                code.AppendLine("        },");

                if (includeComments && node != group.Last())
                {
                    code.AppendLine();
                }
            }

            if (formatForRegions)
            {
                code.AppendLine();
                code.AppendLine("        #endregion");
            }

            if (group != groupedNodes.Last())
            {
                code.AppendLine();
            }
        }

        exportedCode = code.ToString();
    }

    private static List<GathNodeInfo> GetFilteredNodes()
    {
        return nodeList.Where(node =>
        {
            // Search filter
            if (!string.IsNullOrEmpty(searchFilter) &&
                !node.NodeId.ToString().Contains(searchFilter))
                return false;

            // Node Set filter
            if (selectedNodeSetFilter > 0 && node.NodeSet != selectedNodeSetFilter)
                return false;

            // Gathering Type filter
            if (gatheringTypeFilter == 1 && node.GatheringType != 2) // Miner
                return false;
            if (gatheringTypeFilter == 2 && node.GatheringType != 3) // Botanist
                return false;

            return true;
        }).ToList();
    }

    private static void HandleSorting(List<GathNodeInfo> filteredNodes, ImGuiTableSortSpecsPtr sortSpecs)
    {
        if (sortSpecs.SpecsCount == 0) return;

        var spec = sortSpecs.Specs;
        bool ascending = spec.SortDirection == ImGuiSortDirection.Ascending;

        switch (spec.ColumnIndex)
        {
            case 0: // Node ID
                filteredNodes.Sort((a, b) => ascending ? a.NodeId.CompareTo(b.NodeId) : b.NodeId.CompareTo(a.NodeId));
                break;
            case 1: // Node Set
                filteredNodes.Sort((a, b) => ascending ? a.NodeSet.CompareTo(b.NodeSet) : b.NodeSet.CompareTo(a.NodeSet));
                break;
            case 2: // Type
                filteredNodes.Sort((a, b) => ascending ? a.GatheringType.CompareTo(b.GatheringType) : b.GatheringType.CompareTo(a.GatheringType));
                break;
        }
    }

    private static uint GetNextAvailableNodeId()
    {
        return nodeList.Count > 0 ? nodeList.Max(n => n.NodeId) + 1 : 35001;
    }

    private static uint GetNextAvailableNodeSet()
    {
        return nodeList.Count > 0 ? nodeList.Max(n => n.NodeSet) + 1 : 1;
    }

    private static string GetGatheringTypeName(int gatheringType)
    {
        return gatheringType switch
        {
            2 => "Miner",
            3 => "Botanist",
            _ => "Unknown"
        };
    }
}