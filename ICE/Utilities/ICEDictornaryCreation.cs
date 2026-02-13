using ECommons;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.ConfigFiles;
using ICE.Ui.MainUi.ModeSelect;
using ICE.Ui.MainUi.Settings.Settings_Table;
using ICE.Utilities.Cosmic_Helper;
using ICE.Utilities.GatheringHelper;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using static ICE.ConfigFiles.Config;
using static ICE.Enums.MissionAttributes;
using static ICE.Utilities.CosmicHelper;
using static ICE.Utilities.ExcelHelper;

namespace ICE;

public sealed partial class ICE
{
    public static unsafe void DictionaryCreation()
    {
        var wk = WKSManager.Instance();

        foreach (var entry in MoonMissionSheet)
        {
            Dictionary<ushort, CosmicHelper.CraftingInfo> crafts_Main = new();
            Dictionary<ushort, CosmicHelper.CraftingInfo> crafts_Pre = new();
            Dictionary<uint, int> gathering_Min = new();
            List<uint> jobs = new();
            Dictionary<int, int> relicXp = new();
            bool isExpert = false;
            bool isCollectable = false;

            uint keyId = entry.RowId;
            string missionName = entry.Name.ToString();
            missionName = missionName.Replace("<nbsp>", " ");
            missionName = missionName.Replace("<->", "");

            if (missionName == "")
                continue;

            jobs.Add(entry.ClassJobCategory[0].RowId - 1);
            var Job2 = entry.ClassJobCategory[1].RowId;
            if (Job2 != 0)
            {
                jobs.Add(Job2 - 1);
            }
            uint timeLimit = entry.MissionTime;
            uint silver = entry.SilverStarRequirement;
            uint gold = entry.GoldStarRequirement;
            HashSet<uint> previousMissionId = new() { entry.LockedBehind.RowId };

            uint timeAndWeather = entry.WKSMissionLotterySpecialCond.RowId;
            uint startTime = 0;
            uint endTime = 0;
            CosmicWeather weather = CosmicWeather.None;
            if (!CosmicHelper.WeatherSelection.Contains(timeAndWeather))
            {
                var timeSheet = Svc.Data.GetExcelSheet<WKSMissionLotterySpecialCond>().GetRow(timeAndWeather);
                startTime = timeSheet.StartTimeHour; // Start Time
                endTime = timeSheet.EndTimeHour; // End Time
            }
            else
            {
                weather = timeAndWeather switch  // <-- Added 'timeAndWeather' here
                {
                    13 => CosmicWeather.UmbralWind,
                    14 => CosmicWeather.MoonDust,
                    15 => CosmicWeather.Clouds,
                    16 => CosmicWeather.Rain,
                    23 => CosmicWeather.ClearSkies,
                    24 => CosmicWeather.FairSkies,
                    _ => CosmicWeather.None,
                };
            }

            uint rank = entry.LevelGroup;
            uint level = 0;
            if (rank == 1)
                level = 10;
            else if (rank == 2)
                level = 50;
            else if (rank == 3)
                level = 90;
            else if (rank >= 4)
                level = 100;

            bool isCritical = entry.IsSpecialQuest;

            uint RecipeId = entry.WKSMissionRecipe.RowId;

            uint toDoValue = entry.MissionToDo[0].RowId;

            var wksToDo = ToDoSheet.GetRow(toDoValue);
            uint missionText = wksToDo.WKSMissionText.Value.RowId;
            var marker = MarkerSheet.GetRow(wksToDo.Unknown13);
            uint territoryId = 1237; 
            if (keyId < 545)
            {
                territoryId = 1237;
            }
            else if (keyId < 1040)
            {
                territoryId = 1291;
            }
            else if (keyId < 1370)
            {
                territoryId = 1310;
            }
            // TODO: Make this set the correct territoryId once new planets are added and we figure out where it is.

            Vector2 mapFlag = new((int)marker.Unknown1 - 1024, (int)(marker.Unknown2 - 1024));

            int _x = marker.Unknown1 - 1024;
            int _y = marker.Unknown2 - 1024;

            // This is really specific ONLY cause there are 2 rings that overlap... and this can't happen
            if (keyId == 1272)
            {
                mapFlag = new(-340, 870);
            }
            else if (keyId == 1264)
            {
                mapFlag = new(-573, 3);
            }
            else if (keyId == 1296)
            {
                mapFlag = new(-514, 232);
            }

            int radius = marker.Unknown3;

            MissionAttributes attributes = None;

            if (CosmicHelper.CrafterJobList.Any(x => jobs.Contains(x)) && CosmicHelper.GatheringJobList.Any(x => jobs.Contains(x)))
            {
                if (jobs.Contains(18))
                    attributes = Craft | Fish;
                else
                    attributes = Craft | Gather;
            }
            else if (CosmicHelper.CrafterJobList.Any(x => jobs.Contains(x)))
            {
                // Just making this nice and simple. Making all crafter missions just check for crafting.
                // Then going to add critical after
                attributes = Craft;
            }
            else if (CosmicHelper.GatheringJobList.Any(x => jobs.Contains(x)))
            {
                if (jobs.Contains(18))
                    attributes |= Fish;
                else
                    attributes |= Gather;

                attributes = missionText switch
                {
                    103 => Gather | Limited,
                    104 => Gather | ScoreTimeRemaining,
                    105 => Gather,
                    106 => Gather | ScoreChains,
                    107 => Gather | ScoreGatherersBoon,
                    108 => Gather | ScoreChains | ScoreGatherersBoon,
                    109 or 111 => Gather | Collectables,
                    110 => Gather | ReducedItems | ScoreTimeRemaining,
                    112 => Gather | ReducedItems,
                    113 => Fish | ScoreVariety | ScoreTimeRemaining,
                    114 or 115 => Fish | ScoreTimeRemaining,
                    116 => Fish | Limited | ScoreVariety,
                    117 => Fish | Limited | ScoreLargestSize,
                    118 => Fish | Limited | Collectables,
                    119 or 121 => Fish,
                    120 => Fish | ScoreLargestSize,
                    122 => Fish | Collectables,
                    >= 123 and <= 134 => Craft | Gather, // Dual class
                    >= 135 and <= 138 => Craft | Fish,  // Dual class
                    139 => jobs.Contains(18) ? Fish : Gather, // Critical
                    141 => Fish,
                    _ => None
                };
            }

            attributes |= isCritical ? Critical : None;
            attributes |= weather != CosmicWeather.None ? ProvisionalWeather : None;
            attributes |= (startTime != 0 || endTime != 0) ? ProvisionalTimed : None;
            attributes |= !previousMissionId.Contains(0) ? ProvisionalSequential : None;

            // - - - HEY. BRONZE SCORE IS KEPT HERE - - - //
            uint bronze = wksToDo.Unknown2; // Bronze score for Score missions

            if (CrafterJobList.Any(x => jobs.Contains(x)))
            {
                var wksRecipeRow = wksMissionRecipe.GetRow(RecipeId);

                if (isCritical) // Criticals are sus
                {
                    var itemAmount = 3; // It's a pass/fail progress, you need to go till you are full on score
                    if (keyId < 535)
                        itemAmount = 3;
                    else if (keyId < 1039)
                        itemAmount = 2;
                    else if (keyId < 1370)
                        itemAmount = 2;

                    var missionRecipeRow = RecipeSheet?.Where(e => e.RowId == wksRecipeRow.Recipe[0].RowId).FirstOrDefault();
                    var itemId = missionRecipeRow.Value.ItemResult. RowId;
                    var itemName = ItemSheet.GetRow(itemId).Name.ToString();
                    var craftingType = missionRecipeRow.Value.CraftType.Value.RowId;
                    // IceLogging.Verbose($"Recipe Row ID: {missionRecipeRow.Value.RowId} | for item: {itemId} | {itemName}", debugOnly: true);
                    var item1RecipeId = missionRecipeRow.Value.RowId;

                    crafts_Main[(ushort)item1RecipeId] = new CraftingInfo()
                    {
                        ItemId = itemId,
                        RequiredAmount = itemAmount,
                        RecipeId = RecipeId
                    };
                }
                else
                {
                    // Reason for the following code is this:
                    // If it's a pre-craft, it should be further down the list, which means adding it first to the pre-crafts
                    // If it's required, then all of them SHOULD... be required. *-shrugs-*
                    List<ushort> recipeIds = new();
                    for (int x = 2; x >= 0; x--)
                    {
                        var recipeId = (ushort)wksRecipeRow.Recipe[x].Value.RowId;
                        if (recipeId != 0 && !recipeIds.Contains(recipeId))
                            recipeIds.Add(recipeId);
                    }

                    if (recipeIds.Count == 1)
                    {
                        // Only a single item exist in this table. So into the maincrafts it goes
                        IceLogging.Verbose($"Mission: {keyId} had 1 recipie", debugOnly:true);
                        var recipeId = recipeIds[0];
                        var recipeRow = RecipeSheet.GetRow(recipeId);
                        var itemId = recipeRow.ItemResult.RowId;
                        var amountNeeded = wksToDo.RequiredItemQuantity[0];
                        if (amountNeeded == 0)
                        {
                            // this should never happen. But on the off chance that square decides to be a dick and change it's place
                            amountNeeded = 1;
                        }
                        var requiredItem = recipeRow.Ingredient[0].RowId;
                        var requiredAmount = recipeRow.AmountIngredient[0];
                        var requiredItem2 = recipeRow.Ingredient[1].RowId;
                        var requiredAmount2 = recipeRow.AmountIngredient[1];
                        bool expertMat = recipeRow.IsExpert;

                        if (requiredItem2 != 0)
                        {
                            crafts_Main[recipeId] = new()
                            {
                                ItemId = itemId,
                                RequiredAmount = amountNeeded,
                                RecipeId = recipeId,
                                ExpertCraft = expertMat,
                                RequiredItems = new()
                                {
                                    [requiredItem] = requiredAmount,
                                    [requiredItem2] = requiredAmount2
                                }
                            };
                        }
                        else
                        {
                            crafts_Main[recipeId] = new()
                            {
                                ItemId = itemId,
                                RequiredAmount = amountNeeded,
                                RecipeId = recipeId,
                                ExpertCraft = expertMat,
                                RequiredItems = new()
                                {
                                    [requiredItem] = requiredAmount
                                }
                            };
                        }

                        isExpert |= expertMat;
                        isCollectable |= recipeRow.CollectableMetadataKey == 1;
                        // if (isExpert)
                            // IceLogging.Verbose($"{recipeRow.RowId} is an expert craft", debugOnly: true);
                    }
                    else if (recipeIds.Count == 2)
                    {
                        IceLogging.Verbose($"Mission: {keyId} had 2 recipies", debugOnly: true);
                        // First one is going to be the main item that you need.

                        var recipeId = recipeIds[0];
                        var recipeRow = RecipeSheet.GetRow(recipeId);
                        var itemId = recipeRow.ItemResult.RowId;
                        var amountNeeded = wksToDo.RequiredItemQuantity[0];
                        if (amountNeeded == 0)
                        {
                            // this should never happen. But on the off chance that square decides to be a dick and change it's place
                            amountNeeded = 1;
                        }
                        var requiredItem = recipeRow.Ingredient[0].RowId;
                        var requiredAmount = recipeRow.AmountIngredient[0];
                        bool requiredItemExpert = recipeRow.IsExpert;
                        crafts_Main[recipeId] = new()
                        {
                            ItemId = itemId,
                            RequiredAmount = amountNeeded,
                            RecipeId = recipeId,
                            ExpertCraft = requiredItemExpert,
                            RequiredItems = new()
                            {
                                [requiredItem] = requiredAmount
                            }
                        };

                        // if (isExpert)
                        // IceLogging.Verbose($"{recipeRow.RowId} is an expert craft", debugOnly: true);

                        // Second one is going to be the pre-crafting mat that you need
                        var preRecipeId = recipeIds[1];
                        var preRecipeRow = RecipeSheet.GetRow(preRecipeId);
                        var preItemId = preRecipeRow.ItemResult.RowId;
                        var preAmountNeeded = requiredAmount;
                        var preCraftExpert = preRecipeRow.IsExpert;

                        var crateId = preRecipeRow.Ingredient[0].RowId;

                        crafts_Pre[preRecipeId] = new()
                        {
                            ItemId = preItemId,
                            RequiredAmount = preAmountNeeded,
                            RecipeId = preRecipeId,
                            ExpertCraft = preCraftExpert,
                            RequiredItems = new()
                            {
                                [crateId] = preAmountNeeded
                            }
                        };

                        isExpert |= requiredItemExpert || preCraftExpert;
                        isCollectable |= recipeRow.CollectableMetadataKey == 1 || preRecipeRow.CollectableMetadataKey == 1;

                    }
                    else if (recipeIds.Count == 3)
                    {
                        IceLogging.Verbose($"Mission: {keyId} had 3 recipies", debugOnly: true);
                        // all of these should be valid. 
                        for (int i = 0; i < recipeIds.Count; i++)
                        {
                            // Only a single item exist in this table. So into the maincrafts it goes
                            var recipeId = recipeIds[i];
                            var recipeRow = RecipeSheet.GetRow(recipeId);
                            var itemId = recipeRow.ItemResult.RowId;
                            var amountNeeded = wksToDo.RequiredItemQuantity[i];
                            if (amountNeeded == 0)
                            {
                                // this should never happen. But on the off chance that square decides to be a dick and change it's place
                                amountNeeded = 1;
                            }
                            var requiredItem = recipeRow.Ingredient[0].RowId;
                            var requiredAmount = recipeRow.AmountIngredient[0];
                            bool expertCraft = recipeRow.IsExpert;
                            crafts_Main[recipeId] = new()
                            {
                                ItemId = itemId,
                                RequiredAmount = amountNeeded,
                                RecipeId = recipeId,
                                ExpertCraft = expertCraft,
                                RequiredItems = new()
                                {
                                    [requiredItem] = requiredAmount
                                }
                            };
                            isExpert |= expertCraft;
                            isCollectable |= recipeRow.CollectableMetadataKey == 1;
                            // if (isExpert)
                            // IceLogging.Verbose($"{recipeRow.RowId} is an expert craft", debugOnly: true);
                        }
                    }

                    // This is just a general sanity check in itself for mission where there isn't a required item count, but moreso just needs score. 
                    if (crafts_Main.Count == 0)
                    {
                        // These are missions that don't require an item, but for the sanity check of it all, going to just have it be 1. 
                        // Still need to hardcode the bronze scores in though

                        foreach (var item in crafts_Pre)
                        {
                            item.Value.RequiredAmount = 1;
                            crafts_Main.Add(item);
                            crafts_Pre.Remove(item);
                        }
                    }
                }
            }

            // - - - Attribute check for experts here cause needs to be done pd ost crafting - - - - // 
            if (isExpert)
                attributes |= ExpertCraft;
            if (isCollectable)
                attributes |= Collectables;

            if (GatheringJobList.Any(x => jobs.Contains(x)))
            {
                var todoRow = ToDoSheet.GetRow(toDoValue);

                if (todoRow.RequiredItem[0].RowId != 0) // First item in the gathering list. Shouldn't be 0...
                {
                    var minAmount = todoRow.RequiredItemQuantity[0].ToInt();
                    var itemInfoId = MoonItemInfoSheet.GetRow(todoRow.RequiredItem[0].RowId).Item.RowId;
                    if (!gathering_Min.ContainsKey(itemInfoId))
                    {
                        gathering_Min.Add(itemInfoId, minAmount);
                    }
                }
                if (todoRow.RequiredItem[1].RowId != 0) // First item in the gathering list. Shouldn't be 0...
                {
                    var minAmount = todoRow.RequiredItemQuantity[1].ToInt();
                    var itemInfoId = MoonItemInfoSheet.GetRow(todoRow.RequiredItem[1].RowId).Item.RowId;
                    if (!gathering_Min.ContainsKey(itemInfoId))
                    {
                        gathering_Min.Add(itemInfoId, minAmount);
                    }
                }
                if (todoRow.RequiredItem[2].RowId != 0) // First item in the gathering list. Shouldn't be 0...
                {
                    var minAmount = todoRow.RequiredItemQuantity[2].ToInt();
                    var itemInfoId = MoonItemInfoSheet.GetRow(todoRow.RequiredItem[2].RowId).Item.RowId;
                    if (!gathering_Min.ContainsKey(itemInfoId))
                    {
                        gathering_Min.Add(itemInfoId, minAmount);
                    }
                }
            }

            var fish_varietyAmount = 0;
            var fish_AmountRequired = 0;
 
            if (jobs.Contains(18))
            {
                // Some things to note while I'm trying to document this shit...
                // TODO sheet -> Unknown 9 = Required variety of fish?
                // It might... be worth just doing a scan of all the fish kinds -> seeing if we meet that requirement

                // Unknown 17 -> Amount required to complete mission within x time
                // usually... for things like x12->20+ fish 

                // The requiredItem[0] - 2 still is good for things that require a specific amount of a certain fish (it'll give the id for it)

                // Score requirements still might exist for those, but might be good to just filter those out after checking for base requirements...
                // This might break shit LOL

                var todo = entry.MissionToDo[0];

                if (todo.Value.Unknown9 != 0)
                {
                    // Variety fish amount has been found, assigning
                    fish_varietyAmount = todo.Value.Unknown9;
                }
                else if (todo.Value.Unknown17 != 0)
                {
                    // Amount of fish is required to complete the mission
                    fish_AmountRequired = todo.Value.Unknown17;
                }
            }

            // Col 3 -> Cosmocredits - Unknown 0
            // Col 4 -> Lunar Credits - Unknown 1
            // Col 7 ->  Lv. 1 Type - Unknown 12
            // Col 8 ->  Lv. 1 Exp - Unknown 2
            // Col 10 -> Lv. 2 Type - Unknown 13
            // Col 11 -> Lv. 2 Exp - Unknown 3
            // Col 13 -> Lv. 3 Type - Unknown 14
            // Col 14 -> Lv. 3 Exp - Unknown 4

            // Something to note here, a mission can only have a max of 3 types of XP at a time.
            // Which is why there's only 3 entries.

            uint Cosmo = ExpSheet.GetRow(keyId).CosmoCredits;
            uint Lunar = ExpSheet.GetRow(keyId).PlanetCredits;
            uint rewardItemId = 0;
            uint rewardItemAmount = 0;
            uint dronebitAmount = ExpSheet.GetRow(keyId).Unknown20;

            // Exp Modifiers
            uint expModifier_1 = 0;
            uint expModifier_2 = 0;
            uint expModifier_3 = 0;

            if (ExpSheet.TryGetRow(keyId, out var rewardSheet))
            {
                expModifier_1 = rewardSheet.ExpModifier[0].ToUInt();
                expModifier_2 = rewardSheet.ExpModifier[1].ToUInt();
                expModifier_3 = rewardSheet.ExpModifier[2].ToUInt();
            }

            for (var i = 0; i < 3; i++)
            {
                var expKind = ExpSheet.GetRow(keyId).TypeIndex[i];
                var expAmount = ExpSheet.GetRow(keyId).ResearchReward[i];

                if (expKind != 0)
                    relicXp[expKind] = expAmount;
            }

            if (ExpSheet.GetRow(keyId).ItemCount != 0)
            {
                rewardItemId = ExpSheet.GetRow(keyId).Item.RowId; // Column 15 | Item
                rewardItemAmount = ExpSheet.GetRow(keyId).ItemCount;
            }

            if (!SheetMissionDict.ContainsKey(keyId))
            {
                SheetMissionDict[keyId] = new CosmicInfo()
                {
                    Name = missionName,
                    Jobs = jobs,
                    ToDoId = toDoValue,
                    Rank = rank,
                    Level = level,
                    Attributes = attributes,
                    Weather = weather,
                    StartTime = startTime,
                    EndTime = endTime,
                    CosmoCredit = Cosmo,
                    LunarCredit = Lunar,
                    PreviousMissions = previousMissionId,
                    RelicXpInfo = relicXp,
                    BronzeScore = bronze,
                    SilverScore = silver,
                    GoldScore = gold,

                    ExpModifier_1 = expModifier_1,
                    ExpModifier_2 = expModifier_2,
                    ExpModifier_3 = expModifier_3,

                    RewardItem = rewardItemId,
                    RewardItemAmount = rewardItemAmount,
                    DronebitReward = dronebitAmount,

                    MapPosition = mapFlag,
                    Radius = radius,
                    TerritoryId = territoryId,
                    MarkerId = marker.RowId,

                    Gathering_Min = gathering_Min,

                    Fish_AmountRequired = fish_AmountRequired,
                    Fish_VarietyAmount = fish_varietyAmount,

                    Crafts_Main = crafts_Main,
                    Crafts_Pre = crafts_Pre,
                    IsExpert = isExpert
                };
            }
        }

        foreach (var Icon in LeveAssignmentSheet)
        {
            var iconId = Icon.RowId;

            if (iconId is 2 or 3 or 4)
            {
                iconId += 14;
            }
            else if (iconId > 4 && iconId < 13)
            {
                iconId += 3;
            }
            else
                continue;

            if (Icon.Name != "" && Icon.Icon is { } jobicon)
            {
                if (Svc.Texture.TryGetFromGameIcon(jobicon, out var texture))
                {
                    JobIconDict.TryAdd(iconId, texture);
                }
            }
        }

        for (int i = 0; i < GreyIconList.Count; i++)
        {
            var slot = i + 8;
            var iconId = GreyIconList[i];

            if (Svc.Texture.TryGetFromGameIcon(iconId, out var texture))
            {
                GreyTexture.TryAdd((uint)slot, texture);
            }
        }

        CosmicHelper.LoadMissionScores();

        foreach (var entry in C.ScoreKeeper)
        {
            if (SheetMissionDict.TryGetValue(entry.Key, out var missionEntry) && missionEntry.ClassScore == 0)
                missionEntry.ClassScore = entry.Value;
        }

        foreach (var entry in SheetMissionDict)
        {
            var missionId = entry.Key;

            if (MissionScoreDict.TryGetValue(missionId, out var score) && score != 0)
            {
                entry.Value.ClassScore = score;
            }
            else if (C.ScoreKeeper.TryGetValue(missionId, out var storedScore) && storedScore != 0)
            {
                entry.Value.ClassScore = storedScore;
            }
            else
            {
                entry.Value.ClassScore = 0;
            }
        }

        foreach (var item in MoonItemInfoSheet)
        {
            var itemId = item.Item.RowId;
            if (itemId == 0) continue;
            string itemName = ItemSheet.GetRow(itemId).Name.ToString();
            var type = item.WKSItemSubCategory.RowId;
            // IceLogging.Debug($"RowID: {item.RowId} | ID: {itemId} | Name: {itemName}", debugOnly: true);

            if (CosmicHelper.GatheringItems.TryGetValue(itemName, out var itemEntry))
            {
                itemEntry.itemIds.Add(itemId);
            }
            else
            {
                // IceLogging.Debug($"Adding a new entry: {itemName}", debugOnly: true);

                CosmicHelper.GatheringItems[itemName] = new()
                {
                    Type = item.WKSItemSubCategory.RowId,
                    itemIds = new HashSet<uint> { itemId },
                };
            }
        }

        foreach (var weather in WeatherIds)
        {
            if (Svc.Texture.TryGetFromGameIcon(weather.Value, out var texture))
            {
                WeatherIconDict[weather.Key] = texture;
            }
        }

        foreach (var supply in Svc.Data.GetExcelSheet<WKSItemInfo>())
        {
            if (supply.Item.RowId == 0) continue;
            if (supply.WKSItemSubCategory.RowId != 2 && supply.WKSItemSubCategory.RowId != 5) continue;

            var itemId = supply.Item.RowId;
            var kind = supply.WKSItemSubCategory.RowId;
            var name = Svc.Data.GetExcelSheet<Item>().GetRow(itemId).Name.ToString();
            // IceLogging.Info($"Name: {name} | ItemID: {itemId} | kind: {kind}");

            // Seafood/fish
            if (kind == 2)
            {
                if (GatheringUtil.MoonFish.TryGetValue(name, out var fishList))
                {
                    if (!fishList.Contains(itemId))
                    {
                        fishList.Add(itemId);
                    }
                }
                else
                {
                    GatheringUtil.MoonFish[name] = new() { itemId };
                }
            }
            // Baits
            else if (kind == 5)
            {
                if (GatheringUtil.MoonBaits.TryGetValue(name, out var fishBait))
                {
                    if (!fishBait.Contains(itemId))
                    {
                        fishBait.Add(itemId);
                    }
                }
                else
                {
                    GatheringUtil.MoonBaits[name] = new() { itemId };
                }
            }
        }

        EnsureAllMission();

        foreach (var mission in C.MissionConfig)
        {
            if (C.GatherProfiles == null)
            {
                C.GatherProfiles = new Dictionary<int, GatherProfile>();
            }
            IceLogging.Debug($"Checking: {mission.Key}");
            IceLogging.Debug($"Profile ID: {mission.Value.GProfileId}");
            if (!C.GatherProfiles.ContainsKey(mission.Value.GProfileId))
            {
                mission.Value.GProfileId = 0;
            }
        }

        // This is here, merely for the reason of I want a random joke to show up every time they boot up the plugin. I even added some more!
        var random = new Random();
        modeSelect_TableInfo.jokeId = random.Next(0, modeSelect_TableInfo.JokeList.Count-1);

        if (!C.ShowManualMode)
        {
            foreach (var mission in C.MissionConfig)
            {
                mission.Value.ManualMode = false;
            }
        }

        foreach (var fishPreset in GatheringUtil.FishingPreset)
        {
            if (CosmicHelper.SheetMissionDict.TryGetValue(fishPreset.Key, out var mission))
            {
                mission.Fish_Presets = fishPreset.Value;
            }
            else
            {
                IceLogging.Error($"Coulnd't find ID: {fishPreset.Key}");
            }
        }

        // Just a safety check for fishing missions. 
        // If a mission has a preset, and it's not on by default/on the off chance someone turned it off and didn't input a preset name, will auto enable
        foreach (var mission in C.MissionConfig)
        {
            var id = mission.Key;
            if (CosmicHelper.SheetMissionDict.TryGetValue(id, out var missionInfo))
            {
                if (missionInfo.Fish_Presets.Count > 0)
                {
                    // we have a fishing preset here. Time to check to see if we need to enable it (if it doesn't have a custom profile)
                    if (!mission.Value.Use_BuildinPreset && mission.Value.AutoHookPresetName == string.Empty)
                        mission.Value.Use_BuildinPreset = true;
                }
            }
        }

        // quick check on gathering profiles. We should always have a "default" profile set
        // and for specifically first time creation, if the default profile is the only existing, then we should go ahead and import -> set all the profiles
        if (!C.GatherProfiles.TryGetValue(0, out var profileDefault))
        {
            // We somehow are missing a default profile. . . which is honestly quite impressive how the fuck people manage to do this. 
            C.GatherProfiles.Add(0, new GatherProfile
            {
                Id = 0,
                Name = "Default"
            });
        }
        if (C.GatherProfiles.Count == 1)
        {
            // This is a first time setup more than likely (nobody at this point has just the "default" profile for things, that's insanity)
            // So going to inialize the first time setup and auto-select all the profiles at once

            GatherSettings.SetupAllProfiles();
        }

        C.Save();
    }

    private static void MigrateConfigV1()
    {
        if (!C.OldConfigMigrateV1)
        {
            // That means we're still on the old config version. Time to migrate if it exist
            ConfigMigration.MigrateFromOldYaml(C);
        }
    }

    public static void EnsureAllMission()
    {
        IceLogging.Debug("Starting Mission Updater");
        foreach (var mission in SheetMissionDict)
        {
            if (C.MissionConfig.TryGetValue(mission.Key, out var config) && config != null)
            {
                // Config exists and is not null, nothing to do
            }
            else
            {
                // Either key doesn't exist OR value is null
                C.MissionConfig[mission.Key] = new MissionSettings();
                IceLogging.Debug($"Added/Fixed Mission: {mission.Key}");
            }
        }
        C.Save();
    }
}
