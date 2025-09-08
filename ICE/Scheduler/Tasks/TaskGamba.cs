using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using System.Collections.Generic;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskGamba
    {
        public static readonly List<Gamba> DefaultGambaItems = new()
        {
            // Mounts
            new Gamba { ItemId = 44505, Weight = 200, Type = GambaType.Mount }, // Vacuum Suit Identification Key
            new Gamba { ItemId = 47973, Weight = 200, Type= GambaType.Mount }, // Warp Loader Identification Key

            // Emotes
            new Gamba { ItemId = 44509, Weight = 25, Type = GambaType.Emote }, // Ballroom Etiquette - Personal Perfection
            new Gamba { ItemId = 46795, Weight = 25, Type = GambaType.Emote }, // Ballroom Etiquette - Anticipating Exertion

            // Outfits
            new Gamba { ItemId = 47937, Weight = 50, Type = GambaType.Outfit }, // Cosmosuit Coffer
            new Gamba { ItemId = 47095, Weight = 50, Type = GambaType.Outfit }, // Star Pilot Attire Coffer

            // Minions
            new Gamba { ItemId = 47966, Weight = 25, Type = GambaType.Minion }, // Micro Rover
            new Gamba { ItemId = 46782, Weight = 25, Type = GambaType.Minion }, // Model Suit

            // Accessories
            new Gamba { ItemId = 48154, Weight = 5, Type = GambaType.Accessory }, // The Faces We Wear - Tinted Sunglasses
            new Gamba { ItemId = 48160, Weight = 5, Type = GambaType.Accessory }, // Loparasol
            new Gamba { ItemId = 46840, Weight = 5, Type = GambaType.Accessory }, // The Faces We Wear - Scaevan Headgear

            // Orchestration
            new Gamba { ItemId = 48210, Weight = 0, Type = GambaType.Orchestrion }, // Stargazers Orchestrion Roll
            new Gamba { ItemId = 48220, Weight = 0, Type = GambaType.Orchestrion }, // Echoes in the Distance Orchestrion Roll
            new Gamba { ItemId = 48221, Weight = 0, Type = GambaType.Orchestrion }, // Close in the Distance (Instrumental) Orchestrion Roll
            new Gamba { ItemId = 46155, Weight = 0, Type = GambaType.Orchestrion }, // Kaleidoscope Orchestrion Roll

            // Housing Items
            new Gamba { ItemId = 23892, Weight = 0, Type = GambaType.Housing }, // Verdant Partition
            new Gamba { ItemId = 48733, Weight = 0, Type = GambaType.Housing }, // Cosmotable
            new Gamba { ItemId = 48734, Weight = 0, Type = GambaType.Housing }, // Cosmolamp
            new Gamba { ItemId = 48136, Weight = 0, Type = GambaType.Housing }, // Drafting Table
            new Gamba { ItemId = 32215, Weight = 0, Type = GambaType.Housing }, // Spring Meadow Partition
            new Gamba { ItemId = 46175, Weight = 0, Type = GambaType.Housing }, // Portable Exoterminal
            new Gamba { ItemId = 46174, Weight = 0, Type = GambaType.Housing }, // Cosmokitchen Partition
            new Gamba { ItemId = 46173, Weight = 0, Type = GambaType.Housing }, // Cosmoseat


            // Dyes
            new Gamba { ItemId = 48169, Weight = 0, Type = GambaType.Dye }, // Metallic Pink Dye
            new Gamba { ItemId = 48170, Weight = 0, Type = GambaType.Dye }, // Metallic Ruby Red Dye
            new Gamba { ItemId = 48171, Weight = 0, Type = GambaType.Dye }, // Metallic Cobalt Green Dye
            new Gamba { ItemId = 48172, Weight = 0, Type = GambaType.Dye }, // Metallic Dark Blue Dye

            // Materia
            new Gamba { ItemId = 41762, Weight = 0, Type = GambaType.Materia }, // Gatherer's Guerdon Materia XI
            new Gamba { ItemId = 41763, Weight = 0, Type = GambaType.Materia }, // Gatherer's Guile Materia XI
            new Gamba { ItemId = 41764, Weight = 0, Type = GambaType.Materia }, // Gatherer's Grasp Materia XI
            new Gamba { ItemId = 41765, Weight = 0, Type = GambaType.Materia }, // Craftsman's Competence Materia XI
            new Gamba { ItemId = 41766, Weight = 0, Type = GambaType.Materia }, // Craftsman's Cunning Materia XI
            new Gamba { ItemId = 41767, Weight = 0, Type = GambaType.Materia }, // Craftsman's Command Materia XI
            new Gamba { ItemId = 41775, Weight = 0, Type = GambaType.Materia }, // Gatherer's Guerdon Materia XII
            new Gamba { ItemId = 41776, Weight = 0, Type = GambaType.Materia }, // Gatherer's Guile Materia XII
            new Gamba { ItemId = 41777, Weight = 0, Type = GambaType.Materia }, // Gatherer's Grasp Materia XII
            new Gamba { ItemId = 41778, Weight = 0, Type = GambaType.Materia }, // Craftsman's Competence Materia XII
            new Gamba { ItemId = 41779, Weight = 0, Type = GambaType.Materia }, // Craftsman's Cunning Materia XII
            new Gamba { ItemId = 41780, Weight = 0, Type = GambaType.Materia }, // Craftsman's Command Materia XII

            // Other
            new Gamba { ItemId = 43943, Weight = 0, Type = GambaType.Other }, // Cracked Prismaticrystal
            new Gamba { ItemId = 43944, Weight = 0, Type = GambaType.Other }, // Cracked Novacrystal
            new Gamba { ItemId = 28724, Weight = 0, Type = GambaType.Other }, // Crafter's Delineation
            new Gamba { ItemId = 6141,  Weight = 0, Type = GambaType.Other }, // Cordial HQ
            new Gamba { ItemId = 48158, Weight = 0, Type = GambaType.Other }, // Magicked Prism (Cosmic Exploration)
        };

        public static void EnsureGambaWeightsInitialized(bool force = false)
        {
            bool changed = false;
            if (force)
                C.GambaItemWeights.Clear();
            foreach (var item in DefaultGambaItems)
            {
                if (C.GambaItemWeights.Any(x => x.ItemId == item.ItemId))
                    continue;
                C.GambaItemWeights.Add(new Gamba { ItemId = item.ItemId, Weight = item.Weight, Type = item.Type });
                changed = true;
            }
            if (changed)
                C.Save();
        }

        public static void TryHandleGamba()
        {
            if (EzThrottler.Throttle("Gamba", C.GambaDelay))
            {
                EnsureGambaWeightsInitialized();
                if (GenericHelpers.TryGetAddonMaster<WKSLottery>("WKSLottery", out var gamba) && gamba.IsAddonReady)
                {
                    var zoneId = PlayerHelper.CurrentTerritory();
                    var currency = CosmicHelper.CosmicCurrencies[zoneId];

                    if (PlayerHelper.GetItemCount((int)currency, out var credits))
                    {
                        bool confirmEnabled, leftWheelEnabled, rightWheelEnabled;
                        unsafe
                        {
                            confirmEnabled = gamba.SpinWheelButton->IsEnabled;
                            leftWheelEnabled = gamba.WheelLeftButton->IsEnabled;
                            rightWheelEnabled = gamba.WheelRightButton->IsEnabled;
                        }

                        if (GenericHelpers.TryGetAddonMaster<SelectYesno>("SelectYesno", out var select) && select.IsAddonReady)
                        {
                            if (credits >= 1000 + C.GambaCreditsMinimum)
                                select.Yes();
                            else
                                select.No();
                        }
                        else if (confirmEnabled)
                            gamba.ConfirmButton();
                        else if (leftWheelEnabled || rightWheelEnabled)
                        {
                            float leftWeight = gamba.LeftWheelItems.Sum(item => C.GambaItemWeights.FirstOrDefault(x => x.ItemId == item.itemId)?.Weight ?? 0);
                            float rightWeight = gamba.RightWheelItems.Sum(item => C.GambaItemWeights.FirstOrDefault(x => x.ItemId == item.itemId)?.Weight ?? 0);

                            if (C.GambaPreferSmallerWheel)
                            {
                                leftWeight /= gamba.LeftWheelItems.Length;
                                rightWeight /= gamba.RightWheelItems.Length;
                            }

                            if (leftWeight > rightWeight)
                            {
                                IceLogging.Info($"[Gamba] First wheel is better with total weight: {leftWeight}");
                                SelectWheelLeft(gamba);
                            }
                            else if (rightWeight > leftWeight)
                            {
                                IceLogging.Info($"[Gamba] Second wheel is better with total weight: {rightWeight}");
                                SelectWheelRight(gamba);
                            }
                            else
                            {
                                IceLogging.Info("[Gamba] Both wheels are equal in weight. Randomly selecting one.");
                                if (new Random().Next(2) == 0)
                                    SelectWheelLeft(gamba);
                                else
                                    SelectWheelRight(gamba);
                            }
                        }
                    }
                }
                else
                    SchedulerMain.DisablePlugin();
            }
        }

        public static unsafe void SelectWheelLeft(WKSLottery gamba)
        {
            gamba.WheelLeftButton->Flags = 327936U; // Checked, Enabled, Selected
            gamba.WheelRightButton->Flags = 65792U; // Not Checked, Enabled, Not Selected
            IceLogging.Debug($"[Gamba] Selecting Left Wheel");
        }

        public static unsafe void SelectWheelRight(WKSLottery gamba)
        {
            gamba.WheelLeftButton->Flags = 65792U; // Not Checked, Enabled, Not Selected
            gamba.WheelRightButton->Flags = 327936U; // Checked, Enabled, Selected
            IceLogging.Debug($"[Gamba] Selecting Right Wheel");
        }
    }
}
