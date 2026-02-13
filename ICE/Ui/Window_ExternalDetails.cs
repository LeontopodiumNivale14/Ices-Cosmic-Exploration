using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ICE.Ui.MainUi.ModeSelect;
using System;
using System.Collections.Generic;
using System.Text;

namespace ICE.Ui
{
    internal class Window_ExternalDetails : Window
    {
        public Window_ExternalDetails() : base($"Ice's Cosmic Exploration | Mission Details")
        {
            Flags = ImGuiWindowFlags.None;
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(100, 100)
            };
            P.windowSystem.AddWindow(this);
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        public override void Draw()
        {
            var selectedId = modeSelect_TableInfo.selectedMission;
            if (selectedId == 0)
            {
                P.externalDetails.IsOpen = false;
            }
            else
            {
                modeSelect_TableInfo.DrawMissionDetails();
            }
        }
    }
}
