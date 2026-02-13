using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ICE.Ui;
using ICE.Ui.MainUi;
using ICE.Ui.MainUi.ModeSelect;
using ICE.Utilities.Cosmic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.GroupPoseModule;

namespace ICE.Utilities.ImGuiTools;

public static partial class ImGui_Tools
{
    // This is used to store the various category states for ImGui collapsible headers
    // Key: Category Name
    // Value: Is Expanded (true/false)
    // This is just a simple way of keeping track of all of them, so I can easily reference what state they're in
    public static Dictionary<string, bool> CategoryStates = new()
    {
        ["main_AllEnabled"] = false,
        ["main_Critical"] = false,
        ["main_Sequence"] = false,
        ["main_Weather"] = false,
        ["main_Timed"] = false,
        ["main_ARank"] = true,
        ["main_BRank"] = true,
        ["main_CRank"] = true,
        ["main_DRank"] = true,
    };

    // My Custom Header, Uses FontAwesomeIcon and a 
    public static bool DrawCategoryHeader_AutoSize(string label, FontAwesomeIcon? icon = null, IDalamudTextureWrap? imageTexture = null)
    {
        float scale = ImGuiHelpers.GlobalScale;

        // Default Colors for Theming. This is really here to make sure it's formatted as I want it to be
        var headerColor = ImGui.GetColorU32(ImGuiCol.Header);
        var textColor = ImGui.GetColorU32(ImGuiCol.Text);
        // var textColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);


        // This is here to make sure that
        // A: If it doesn't already exist, add it and just make it false (This makes it to where it's not expanded by default)
        //    - Could absolutely change that to true if I want to make it shown on inital creation
        // B: Returns the state in a form to where if that's true, then I could display the elements below it properly
        string categoryId = label;
        if (!CategoryStates.ContainsKey(categoryId))
            CategoryStates[categoryId] = false;

        bool isExpanded = CategoryStates[categoryId];

        // Need these here for two reasons:
        // 1: drawList allows me to create un-conventional things that isn't included in the Imgui Library
        // 2: curserPos allows me to grab the absolute position, which is necessary to make sure things are lined up properly
        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();

        // This is currently how I'm going to autosize it based on the contents of the window.
        // (as of typing this). It gets the width of the window and expands it on that (these are meant for the sidebar)
        // Height is scaled based on the global font scale
        float width = ImGui.GetContentRegionAvail().X;
        float height = 30 * scale;

        // Check for click, nice little rectangle area where it can be clicked at. This takes in account the above things to make sure it's only clicking within this area
        bool isHovered = ImGui.IsMouseHoveringRect(cursorPos, new Vector2(cursorPos.X + width, cursorPos.Y + height));
        bool isClicked = isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        if (isClicked)
        {
            CategoryStates[categoryId] = !CategoryStates[categoryId];
            isExpanded = CategoryStates[categoryId];
        }

        // Change header color slightly on hover, just a nice QOL
        if (isHovered)
            headerColor = ImGui.GetColorU32(ImGuiCol.HeaderHovered);

        // Drawing the rectangle itself/ custom thingy. This is the container for all the fancy smancy stuff.
        // Border radius scaled
        drawList.AddRectFilled(cursorPos, new Vector2(cursorPos.X + width, cursorPos.Y + height), headerColor, 5.0f * scale);

        // Calculating the vertical spacing here, need to make sure it fits within our custom box nice and cozy
        float imageSize = 23 * scale; // Used for images specifically, since I like things being aligned with each other
        float textHeight = ImGui.CalcTextSize(label).Y;
        float verticalPadding = (height - textHeight) / 2;

        // Adding some padding to the left, don't need it feeling like it's right against the box. We're making somewhat bubbly things
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalPadding);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8 * scale);

        // Drawing either an image, icon, or nothing at all (should really only be the first 2, but on the off chance I decide to just use text)
        if (imageTexture != null)
        {
            // Calculating the offset here to center the image with the text (OCD here)
            float imageYOffset = (textHeight - imageSize) / 2;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + imageYOffset);

            // Adding a little bit of padding here for the image -> text (scaled)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 4 * scale); // Current set to -4 to bring it closer, but can be changed to give it more space (lowering the number) or removing more space (increasing number)

            ImGui.Image(imageTexture.Handle, new Vector2(imageSize, imageSize));

            // Resetting the Y position for text, to make sure it lines up (scaled spacing)
            ImGui.SameLine(0, 2 * scale);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - imageYOffset);
        }
        else if (icon.HasValue)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, textColor); // Disabled text color here to match the rest of it
            ImGuiEx.Icon(icon.Value);
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }
        else
        {
            // No icon, just adding some sameline spacing
            ImGui.SameLine();
        }

        // Actual label here, disabled text color
        ImGui.PushStyleColor(ImGuiCol.Text, textColor);
        ImGui.Text(label);
        ImGui.PopStyleColor();

        // Replace the badge count section with the caret icon (scaled padding)
        float iconSize = ImGui.CalcTextSize(FontAwesomeIcon.CaretDown.ToIconString()).X;
        float rightPadding = 10 * scale;

        float iconXPos = cursorPos.X + width - iconSize - rightPadding;
        float iconYPos = cursorPos.Y + verticalPadding;

        ImGui.SetCursorScreenPos(new Vector2(iconXPos, iconYPos));
        ImGui.PushStyleColor(ImGuiCol.Text, textColor);
        ImGuiEx.Icon(isExpanded ? FontAwesomeIcon.CaretSquareDown : FontAwesomeIcon.CaretSquareRight);
        ImGui.PopStyleColor();

        // Last but not least, resetting it for the next thing here:
        ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + height));
        ImGui.Spacing();

        return isExpanded;
    }

    public static bool DrawCategoryHeader(string label, FontAwesomeIcon? icon = null)
    {
        // Default coloring here
        var headerColor = ImGui.GetColorU32(ImGuiCol.Header);
        var textColor = ImGui.GetColorU32(ImGuiCol.Text);

        // Setting the values of the content size (padding, spacing, ect) that way it's used across the board
        float horizontalPadding = 8;
        float verticalPadding = 4;
        float iconTextSpacing = 4;

        // These are to make sure that they're drawn in place (Look at DrawCategoryHeader_AutoSize if I really need to go deep in refreshments)
        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();

        // We want this to fit within the whole thing so. Going to get the text size so it scales properly if people have their ui text more than 100%
        var textSize = ImGui.CalcTextSize(label);
        float contentWidth = ImGui.GetContentRegionAvail().X;
        float contentHeight = verticalPadding * 2 + textSize.Y;

        // If it doesn't already exist, then creating an entry in the category state. If it does exist, then grabbing it and storing it for later
        string categoryId = label;
        if (!CategoryStates.ContainsKey(categoryId))
            CategoryStates[categoryId] = false;

        bool isExpanded = CategoryStates[categoryId];
        bool isHovered = ImGui.IsMouseHoveringRect(cursorPos, new Vector2(cursorPos.X + contentWidth, cursorPos.Y + contentHeight));
        bool isClicked = isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        if (isClicked)
        {
            CategoryStates[categoryId] = !CategoryStates[categoryId];
            isExpanded = CategoryStates[categoryId];
        }

        // Draw background rectangle with rounded corners
        drawList.AddRectFilled(cursorPos, new Vector2(cursorPos.X + contentWidth, cursorPos.Y + contentHeight), headerColor, 5.0f);

        // Change header color slightly on hover
        if (isHovered)
            headerColor = ImGui.GetColorU32(ImGuiCol.HeaderHovered);

        // Position cursor with padding
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + horizontalPadding);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalPadding);

        // Draw icon if provided
        if (icon.HasValue)
        {
            ImGuiEx.Icon(icon.Value);
            ImGui.SameLine(0, iconTextSpacing);
        }

        ImGui.Text(label);
        ImGui.SameLine(0, 5);
        ImGui.Text(""); // This is here, mainly to give that little bit more buffer. I don't like it being as close as it is to the lettering

        // Advance cursor past the header
        ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + contentHeight));
        ImGui.Spacing();

        return isExpanded;
    }

    /// <summary>
    /// I realize that these are probably more over the top for a simple button than needs to be. But TBH this kind of does 2 things for me: <br></br>
    /// 1: Lets me use custom coloring like the tabs would. Tabs absolutely hate having anything in that selected tab change. (Ex. Changing the active missions currently enabled immediately kicks you off that tab)
    /// 2: Lets me create custom buttons in any form, and also lets me show the active state of them all. PLUS. Uses the expanded headers so works out for the showing of mission headers
    /// </summary>
    /// <param name="label"></param>
    /// <param name="categoryId"></param>
    /// <param name="icon"></param>
    /// <param name="spacingAfter"></param>
    /// <returns></returns>
    public static bool DrawCategoryButton(string label, string categoryId, FontAwesomeIcon? icon = null, float spacingAfter = 5)
    {
        float scale = ImGuiHelpers.GlobalScale;

        // Default coloring here
        var headerColor = ImGui.GetColorU32(ImGuiCol.Button);
        var textColor = ImGui.GetColorU32(ImGuiCol.Text);

        // Setting the values of the content size (padding, spacing, ect) that way it's used across the board
        float horizontalPadding = 8 * scale;
        float verticalPadding = 4 * scale;
        float iconTextSpacing = 4 * scale;

        // These are to make sure that they're drawn in place
        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();

        // Calculate text size
        var textSize = ImGui.CalcTextSize(label);

        // Calculate icon width if present
        float iconWidth = 0;
        if (icon.HasValue)
        {
            iconWidth = textSize.Y + iconTextSpacing;
        }

        // Calculate button dimensions based on content
        float contentWidth = horizontalPadding * 2 + iconWidth + textSize.X;
        float contentHeight = verticalPadding * 2 + textSize.Y;

        // If it doesn't already exist, then creating an entry in the category state
        if (!CategoryStates.ContainsKey(categoryId))
            CategoryStates[categoryId] = false;

        bool isExpanded = CategoryStates[categoryId];
        bool isHovered = ImGui.IsMouseHoveringRect(cursorPos, new Vector2(cursorPos.X + contentWidth, cursorPos.Y + contentHeight))
                      && ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup | ImGuiHoveredFlags.ChildWindows);
        bool isClicked = isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        if (isClicked)
        {
            CategoryStates[categoryId] = !CategoryStates[categoryId];
            isExpanded = CategoryStates[categoryId];
        }

        // Color changing! Based on the various states
        if (isExpanded)
            headerColor = ImGui.GetColorU32(ImGuiCol.TabActive);
        if (isHovered)
            headerColor = ImGui.GetColorU32(ImGuiCol.HeaderHovered);

        // Draw background rectangle with rounded corners (scaled)
        drawList.AddRectFilled(cursorPos, new Vector2(cursorPos.X + contentWidth, cursorPos.Y + contentHeight), headerColor, 5.0f * scale);

        // Position cursor with padding
        ImGui.SetCursorScreenPos(new Vector2(cursorPos.X + horizontalPadding, cursorPos.Y + verticalPadding));

        // Draw icon if provided
        if (icon.HasValue)
        {
            ImGuiEx.Icon(icon.Value);
            ImGui.SameLine(0, iconTextSpacing);
        }

        ImGui.Text(label);

        // Create an invisible button to properly reserve space and handle layout
        ImGui.SetCursorScreenPos(cursorPos);
        ImGui.InvisibleButton($"##{categoryId}_btn", new Vector2(contentWidth, contentHeight));

        // Add spacing after the button (scaled)
        ImGui.SameLine(0, spacingAfter * scale);

        return isExpanded;
    }
    public static void DrawImageBox(ISharedImmediateTexture texture, string? label = null, float imageZoom = 1.5f, float spacingAfter = 5)
    {
        float scale = ImGuiHelpers.GlobalScale;

        // Default coloring to match button style
        var headerColor = ImGui.GetColorU32(ImGuiCol.Button);

        // Setting the values of the content size (padding, spacing, ect)
        float horizontalPadding = 8 * scale;
        float verticalPadding = 4 * scale;
        float iconTextSpacing = 4 * scale;

        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();

        // Calculate text size if label provided
        Vector2 textSize = Vector2.Zero;
        float textWidth = 0;
        if (!string.IsNullOrEmpty(label))
        {
            textSize = ImGui.CalcTextSize(label);
            textWidth = textSize.X + iconTextSpacing;
        }

        // Image size should match the text height from buttons to keep consistent height
        float imageSize = textSize.Y > 0 ? textSize.Y : ImGui.CalcTextSize("A").Y;

        // Calculating box dimensions - height should match button height
        float contentWidth = horizontalPadding * 2 + imageSize + textWidth;
        float contentHeight = verticalPadding * 2 + (textSize.Y > 0 ? textSize.Y : ImGui.CalcTextSize("A").Y);

        // Draw background rectangle with rounded corners
        drawList.AddRectFilled(cursorPos, new Vector2(cursorPos.X + contentWidth, cursorPos.Y + contentHeight), headerColor, 5.0f * scale);

        // Calculate vertical center
        float contentVerticalCenter = cursorPos.Y + (contentHeight / 2);

        // Draw image centered vertically with UV zoom
        float imageYOffset = contentVerticalCenter - (imageSize / 2);
        var imagePos = new Vector2(cursorPos.X + horizontalPadding, imageYOffset);

        // Calculate UV coordinates for zoom effect (zooms into center)
        float uvZoom = 1.0f / imageZoom;
        float uvOffset = (1.0f - uvZoom) / 2.0f;
        Vector2 uv0 = new Vector2(uvOffset, uvOffset);
        Vector2 uv1 = new Vector2(uvOffset + uvZoom, uvOffset + uvZoom);

        drawList.AddImage(texture.GetWrapOrEmpty().Handle, imagePos, new Vector2(imagePos.X + imageSize, imagePos.Y + imageSize), uv0, uv1);

        // Draw text if provided, centered vertically
        if (!string.IsNullOrEmpty(label))
        {
            float textYOffset = contentVerticalCenter - (textSize.Y / 2);
            float textXPos = cursorPos.X + horizontalPadding + imageSize + iconTextSpacing;
            ImGui.SetCursorScreenPos(new Vector2(textXPos, textYOffset));
            ImGui.Text(label);
        }

        // Create an invisible button to properly reserve space and handle layout (matches DrawCategoryButton)
        ImGui.SetCursorScreenPos(cursorPos);
        ImGui.InvisibleButton($"##imagebox_{label}", new Vector2(contentWidth, contentHeight));

        // Add spacing after the button (scaled) - matches DrawCategoryButton
        ImGui.SameLine(0, spacingAfter * scale);
    }

    public static void EndCategoryButtonRow()
    {
        ImGui.NewLine();
        ImGui.Separator();
    }

    public static void DrawJobButtons(uint jobId, string tooltip)
    {
        float scale = ImGuiHelpers.GlobalScale;

        uint selectedJob = C.SelectedJob;
        bool state = selectedJob == jobId;
        ISharedImmediateTexture? icon = state ? CosmicHelper.JobIconDict[jobId] : CosmicHelper.GreyTexture[jobId];
        Vector2 size = new Vector2(26 * scale, 26 * scale);
        bool autoPickCurrentJob = C.AutoPickCurrentJob;

        if (StyledImageButton.DrawStyledImageButton(icon, size, state))
        {
            if (autoPickCurrentJob)
            {
                autoPickCurrentJob = false;
                C.AutoPickCurrentJob = autoPickCurrentJob;
            }

            C.SelectedJob = jobId;
            C.Save();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(tooltip);
            ImGui.EndTooltip();
        }
    }

    public static void IconWithTooltip(Vector4 col, FontAwesomeIcon icon, string? tooltip = null)
    {
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, col);
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextUnformatted(icon.ToIconString());
        ImGui.PopFont();
        ImGui.PopStyleColor();

        if (tooltip != null)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
        }
    }

    public static void IconWithTooltip(FontAwesomeIcon icon, string? tooltip = null)
    {
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextUnformatted(icon.ToIconString());
        ImGui.PopFont();

        if (tooltip != null)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
        }
    }
}
