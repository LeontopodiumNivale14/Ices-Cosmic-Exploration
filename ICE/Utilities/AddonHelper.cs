using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;

namespace ICE.Utilities;

public static class AddonHelper
{
    public static unsafe void OpenRecipeNote()
    {
        int[] basicCrafts = [1008, 1, 170, 663, 302, 464, 1101, 901];
        uint recipeId = (uint)basicCrafts[(int)PlayerHelper.GetClassJobId()-8];

        AgentRecipeNote.Instance()->OpenRecipeByRecipeId(ExcelHelper.RecipeSheet.GetRow(recipeId).RowId);
    }
    public static unsafe bool IsAddonActive(string AddonName) // Used to see if the addon is active/ready to be fired on
    {
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(AddonName);
        return addon != null && addon->IsVisible && addon->IsReady;
    }

    public static unsafe string GetNodeText(string addonName, params int[] nodeNumbers)
    {
        try
        {
            var ptr = Svc.GameGui.GetAddonByName(addonName, 1);
            if (ptr.Address == IntPtr.Zero)
                return String.Empty;

            var addon = (AtkUnitBase*)ptr.Address;
            if (addon->UldManager.NodeList == null || addon->UldManager.NodeListCount == 0)
                return String.Empty;

            var uld = addon->UldManager;
            AtkResNode* currentNode = null;

            for (var i = 0; i < nodeNumbers.Length; i++)
            {
                int nodeIndex = nodeNumbers[i];

                if (nodeIndex < 0 || nodeIndex >= uld.NodeListCount)
                    return $"IndexOutOfRange: {nodeIndex}";

                currentNode = uld.NodeList[nodeIndex];

                // More nodes to traverse
                if (i < nodeNumbers.Length - 1)
                {
                    if (currentNode->Type != NodeType.Component || ((AtkComponentNode*)currentNode)->Component == null)
                        return $"InvalidComponentNode: {currentNode->Type}";

                    var component = ((AtkComponentNode*)currentNode)->Component;
                    if (component->UldManager.NodeList == null ||
                        component->UldManager.NodeListCount == 0)
                        return "EmptyChildUld";

                    uld = component->UldManager;
                }
            }

            if (currentNode == null)
                return String.Empty;

            return currentNode->Type switch
            {
                NodeType.Counter => GetCounterNodeText((AtkCounterNode*)currentNode),
                NodeType.Text => GetTextNodeText((AtkTextNode*)currentNode),
                _ => $"UnsupportedNodeType: {currentNode->Type}"
            };
        }
        catch (Exception ex) when (ex is AccessViolationException or NullReferenceException)
        {
            PluginLog.Error($"Memory access violation in GetNodeText: {ex.Message}");
            return string.Empty;
        }
    }

    private static unsafe string GetCounterNodeText(AtkCounterNode* node)
    {
        return node != null ? node->NodeText.ToString() : string.Empty;
    }

    private static unsafe string GetTextNodeText(AtkTextNode* node)
    {
        return node != null ? node->NodeText.GetText() : string.Empty;
    }
    
    public static unsafe AtkTextNode* GetAtkTextNode(string addonName, params int[] nodeNumbers)
    {

        var ptr = Svc.GameGui.GetAddonByName(addonName, 1);

        var addon = (AtkUnitBase*)ptr.Address;
        var uld = addon->UldManager;

        AtkResNode* node = null;
        var debugString = string.Empty;
        for (var i = 0; i < nodeNumbers.Length; i++)
        {
            var nodeNumber = nodeNumbers[i];

            var count = uld.NodeListCount;

            node = uld.NodeList[nodeNumber];
            debugString += $"[{nodeNumber}]";

            // More nodes to traverse
            if (i < nodeNumbers.Length - 1)
            {
                uld = ((AtkComponentNode*)node)->Component->UldManager;
            }
        }

        var textNode = (AtkTextNode*)node;
        return textNode;
    }

    private static unsafe AtkResNode* GetNodeByIDChain(AtkResNode* node, params int[] ids)
    {
        if (node == null || ids.Length <= 0)
            return null;

        if (node->NodeId == ids[0])
        {
            if (ids.Length == 1)
                return node;

            var newList = new List<int>(ids);
            newList.RemoveAt(0);

            var childNode = node->ChildNode;
            if (childNode != null)
                return GetNodeByIDChain(childNode, [.. newList]);

            if ((int)node->Type >= 1000)
            {
                var componentNode = node->GetAsAtkComponentNode();
                var component = componentNode->Component;
                var uldManager = component->UldManager;
                childNode = uldManager.NodeList[0];
                return childNode == null ? null : GetNodeByIDChain(childNode, [.. newList]);
            }

            return null;
        }

        //check siblings
        var sibNode = node->PrevSiblingNode;
        return sibNode != null ? GetNodeByIDChain(sibNode, ids) : null;
    }
}
