using Dalamud.Interface;
using ICE.Utilities.ImGuiTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace ICE.Ui.MainUi.Settings.Settings_Table
{
    internal class Shop_Dronebit
    {
        public static void Draw()
        {
            bool buyDrones = C.Cosmodrone_Buy;
            if (ImGui.Checkbox("Buy Drones", ref buyDrones))
            {
                C.Cosmodrone_Buy = buyDrones;
                C.Save();
            }
            ImGui_Tools.IconWithTooltip(FontAwesomeIcon.QuestionCircle, 
                "Do you want to buy drones? If yes, enable this"
                );

            int drone_buyAtAmount = C.Cosmodrone_BuyAt;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Buy At Amount", ref drone_buyAtAmount, 200, 5000))
            {
                drone_buyAtAmount = (int)Math.Round(drone_buyAtAmount / 200.0) * 200;
                C.Cosmodrone_BuyAt = drone_buyAtAmount;
                C.SaveDebounced();
            }
            ImGui_Tools.IconWithTooltip(FontAwesomeIcon.QuestionCircle, 
                "When do you wanna buy drones from the vendor?\n" +
                "Set in incriments of 200, max of 5,000"
                );

            int maxCrateAmount = C.Cosmodrone_MaxKeep;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("Maximum Drones", ref maxCrateAmount))
            {
                if (maxCrateAmount < 0)
                    maxCrateAmount = 0;
                C.Cosmodrone_MaxKeep = maxCrateAmount;
                C.SaveDebounced();
            }
            ImGui_Tools.IconWithTooltip(FontAwesomeIcon.QuestionCircle,
                "What's the maximum amount of drones you wanna keep?\n" +
                "0 = will just keep buying\n" +
                "Anything above 0 will just be a hard cap and will stop buying if it reaches this"
                );

            bool runDroneFinder = C.Cosmodrone_Run;
            if (ImGui.Checkbox("Automate cosmodrone", ref runDroneFinder))
            {
                C.Cosmodrone_Run = runDroneFinder;
                C.Save();
            }
            ImGui_Tools.IconWithTooltip(FontAwesomeIcon.QuestionCircle,
                "Do you want to run the automated drone finding? If yes, enable this\n" +
                "PLEASE NOTE. DO. NOT. LEAVE. THIS. ALONE. This is still being worked on heavily");

            int runAtAmount = C.Cosmodrone_RunAt;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("Start finding drones at", ref runAtAmount))
            {
                C.Cosmodrone_RunAt = runAtAmount;
                C.Save();
            }
            ImGui_Tools.IconWithTooltip(FontAwesomeIcon.QuestionCircle,
                "How many drone boxes do you want to have before it starts running?\n" +
                "If you set this at 1, the moment you get any it will proceed to open -> find"
                );

            bool finishCurrent = C.Cosmodrone_FinishCurrent;
            if (ImGui.Checkbox("If drone is active, finish current", ref finishCurrent))
            {
                C.Cosmodrone_FinishCurrent = finishCurrent;
                C.Save();
            }
            ImGui_Tools.IconWithTooltip(FontAwesomeIcon.QuestionCircle,
                "If it detects that you're on Oizys and have a drone that is currently on the map,\n" +
                "It will proceed to pathfind -> collecting said drone for you");
        }
    }
}
