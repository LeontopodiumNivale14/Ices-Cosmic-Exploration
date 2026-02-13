using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.DebugWindowTabs
{
    internal class UITest
    {
        private static unsafe void UpdateInventory()
        {
            var inventory = InventoryManager.Instance();
        }
    }

    
}
