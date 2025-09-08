using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using static ICE.Utilities.CosmicHelper;
using static ICE.Enums.MissionAttributes;
using static ICE.Utilities.ExcelHelper;
using Lumina.Excel.Sheets;

namespace ICE;

public sealed partial class ICE
{
    public static unsafe void DictionaryCreation()
    {
        MoonRecipies = [];

        var wk = WKSManager.Instance();

        foreach (var item in MoonMissionSheet)
        {
            List<(int Type, int Amount)> Exp = new();
            Dictionary<ushort, int> MainItems = new();
            Dictionary<ushort, int> PreCrafts = new();
            Dictionary<uint, int> GatherItems = new();
            uint keyId = item.RowId;
            string LeveName = item.Name.ToString();
            LeveName = LeveName.Replace("<nbsp>", " ");
            LeveName = LeveName.Replace("<->", "");

            if (LeveName == "")
                continue;


            int JobId = (int)item.ClassJobCategory[0].RowId - 1;
            int Job2 = (int)item.ClassJobCategory[1].RowId;
            if (Job2 != 0)
            {
                Job2 = Job2 - 1;
            }
            uint timeLimit = item.MissionTime;
            uint silver = item.SilverStarRequirement;
            uint gold = item.GoldStarRequirement;
            uint previousMissionId = item.LockedBehind.RowId;

            uint timeAndWeather = item.WKSMissionLotterySpecialCond.RowId;
            uint startTime = 0;
            uint endTime = 0;
            CosmicWeather weather = CosmicWeather.FairSkies;
            if (!CosmicHelper.WeatherSelection.Contains(timeAndWeather))
            {
                var timeSheet = Svc.Data.GetExcelSheet<WKSMissionLotterySpecialCond>().GetRow(timeAndWeather);
                startTime = timeSheet.Unknown1; // Start Time
                endTime = timeSheet.Unknown2; // End Time
            }
            else
            {
                weather = (CosmicWeather)(timeAndWeather - 12);
                // TODO: Go back and assign enums based on the value instead... or just directly give it a flag. Unsure. Feels dirty
            }

            uint rank = item.LevelGroup;
            bool isCritical = item.IsSpecialQuest;

            uint RecipeId = item.WKSMissionRecipe.RowId;

            uint toDoValue = item.MissionToDo[0].RowId;

            var todo = ToDoSheet.GetRow(toDoValue);
            uint missionText = todo.WKSMissionText.Value.RowId;
            var marker = MarkerSheet.GetRow(todo.Unknown13);
            uint territoryId = 1237; // TODO: Make this set the correct territoryId once new planets are added and we figure out where it is.

            int _x = marker.Unknown1 - 1024;
            int _y = marker.Unknown2 - 1024;
            int radius = marker.Unknown3;

            MissionAttributes attributes = missionText switch
            {
                99 or 101 or 145 or 236 or 238 => Craft | Limited,
                100 or 102 or 146 or 147 or 148 or 235 or 237 => Craft | Limited | Collectables,
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
                139 => JobId == 18 ? Fish : Gather, // Critical
                140 or 149 => Craft,
                _ => None
            };
            attributes |= isCritical ? Critical : None;
            attributes |= weather != CosmicWeather.FairSkies ? ProvisionalWeather : None;
            attributes |= (startTime != 0 || endTime != 0) ? ProvisionalTimed : None;
            attributes |= previousMissionId != 0 ? ProvisionalSequential : None;

            uint bronze = todo.Unknown2; // Bronze score for Score missions
            attributes |= bronze > 0 ? ScoreScore : None;

            if (CrafterJobList.Contains(JobId))
            {
                var wksRecipeRow = wksMissionRecipe.GetRow(RecipeId);
                var wksToDo = ToDoSheet.GetRow(toDoValue);
                bool preCraftsbool = false;

                if (isCritical) // Criticals are sus
                {
                    var itemAmount = 3; // It's a pass/fail progress, you need to go till you are full on score
                                        // Realistically need 3 items. So just going to hard code this as that for now. Until square decides to change the formula haha.
                    var missionRecipeRow = RecipeSheet.Where(e => e.RowId == wksRecipeRow.Recipe[0].RowId).First();
                    var itemId = missionRecipeRow.ItemResult.RowId;
                    var itemName = ItemSheet.GetRow(itemId).Name.ToString();
                    var craftingType = missionRecipeRow.CraftType.Value.RowId;
                    IceLogging.Verbose($"Recipe Row ID: {missionRecipeRow.RowId} | for item: {itemId} | {itemName}");
                    var item1RecipeId = missionRecipeRow.RowId;
                    MainItems.Add((ushort)item1RecipeId, itemAmount);
                }
                else
                {
                    // Reason for the following code is this:
                    // If it's a pre-craft, it should be further down the list, which means adding it first to the pre-crafts
                    // If it's required, then all of them SHOULD... be required. *-shrugs-*
                    for (int i = 2; i >= 0; i--)
                    {
                        var recipeId = (ushort)wksRecipeRow.Recipe[i].Value.RowId;

                        IceLogging.Info($"MissionID: {keyId} | ToDoId: {toDoValue} | recipeId: {recipeId} @ slot {i}");

                        if (recipeId != 0)
                        {
                            var recipeRow = RecipeSheet.GetRow(recipeId);
                            var itemId = recipeRow.ItemResult.RowId;

                            var amountNeeded = wksToDo.RequiredItemQuantity[i];

                            // Appears to be a valid recipeId, time to grab the infomation from the other sheets.
                            if (amountNeeded == 0)
                            {
                                // Item isn't a required item, but is a pre-craft. Going to set the default of 1 for now, then change post.
                                IceLogging.Info($"Adding Pre-Craft: {itemId}");
                                PreCrafts[recipeId] = 1;
                            }
                            else
                            {
                                // Item count was more than 0. Which means THEORETICALLY... it should be a main item. 
                                IceLogging.Info($"Adding MainItem: {itemId}");
                                MainItems[recipeId] = amountNeeded;

                                var recipeMaterialId = recipeRow.AmountIngredient[0];

                                // Checking to see if the material exist in the crafts_pre. If so, then updating the value
                                var preCraftRecipe = PreCrafts.FirstOrDefault();

                                if (preCraftRecipe.Key != 0)
                                {
                                    PreCrafts[preCraftRecipe.Key] = amountNeeded;
                                }
                            }
                        }
                    }

                    // This is just a general sanity check in itself for mission where there isn't a required item count, but moreso just needs score. 
                    if (MainItems.Count == 0)
                    {
                        // These are missions that don't require an item, but for the sanity check of it all, going to just have it be 1. 
                        // Still need to hardcode the bronze scores in though

                        foreach (var itemRecipe in PreCrafts)
                        {
                            MainItems[itemRecipe.Key] = 1;
                            PreCrafts.Remove(itemRecipe.Key);
                        }
                    }

                    if (PreCrafts.Count != 0)
                        preCraftsbool = true;
                }

                CosmicHelper.MoonRecipies[item.RowId] = new()
                {
                    MainCraftsDict = MainItems,
                    PreCrafts = preCraftsbool,
                    PreCraftDict = PreCrafts,
                };
            }

            if (GatheringJobList.Contains(JobId))
            {
                var todoRow = ToDoSheet.GetRow(toDoValue);

                if (todoRow.RequiredItem[0].RowId != 0) // First item in the gathering list. Shouldn't be 0...
                {
                    var minAmount = todoRow.RequiredItemQuantity[0].ToInt();
                    var itemInfoId = MoonItemInfoSheet.GetRow(todoRow.RequiredItem[0].RowId).Item.RowId;
                    if (!GatherItems.ContainsKey(itemInfoId))
                    {
                        GatherItems.Add(itemInfoId, minAmount);
                    }
                }
                if (todoRow.RequiredItem[1].RowId != 0) // First item in the gathering list. Shouldn't be 0...
                {
                    var minAmount = todoRow.RequiredItemQuantity[1].ToInt();
                    var itemInfoId = MoonItemInfoSheet.GetRow(todoRow.RequiredItem[1].RowId).Item.RowId;
                    if (!GatherItems.ContainsKey(itemInfoId))
                    {
                        GatherItems.Add(itemInfoId, minAmount);
                    }
                }
                if (todoRow.RequiredItem[2].RowId != 0) // First item in the gathering list. Shouldn't be 0...
                {
                    var minAmount = todoRow.RequiredItemQuantity[2].ToInt();
                    var itemInfoId = MoonItemInfoSheet.GetRow(todoRow.RequiredItem[2].RowId).Item.RowId;
                    if (!GatherItems.ContainsKey(itemInfoId))
                    {
                        GatherItems.Add(itemInfoId, minAmount);
                    }
                }

                if (!GatheringItemDict.ContainsKey(keyId))
                {
                    GatheringItemDict[keyId] = new GatheringInfo()
                    {
                        MinGatherItems = GatherItems
                    };
                }
            }

            if (GatheringJobList.Contains(JobId) && CrafterJobList.Contains(Job2))
            {
                var MissionRecipe = item.WKSMissionRecipe.RowId;
                var DualRecipeId = wksMissionRecipe.GetRow(MissionRecipe).Recipe[0].Value.RowId;
                var Recipe = RecipeSheet.GetRow(DualRecipeId);
                var MainItem = Recipe.ItemResult.Value.RowId;
                var GatherItem = Recipe.Ingredient[0].Value.RowId;
                var GatherAmount = Recipe.AmountIngredient[0].ToInt();

                MainItems.Add((ushort)DualRecipeId, 1);
                GatherItems.Add(GatherItem, GatherAmount);

                if (!MoonRecipies.ContainsKey(keyId))
                {
                    MoonRecipies[keyId] = new MoonRecipieInfo()
                    {
                        MainCraftsDict = MainItems,
                        PreCrafts = false
                    };
                }
                else
                {
                    MoonRecipies[keyId].MainCraftsDict = MainItems;
                }

                if (GatheringItemDict.ContainsKey(keyId))
                {
                    GatheringItemDict[keyId] = new GatheringInfo()
                    {
                        MinGatherItems = GatherItems
                    };
                }
                else
                {
                    GatheringItemDict[keyId].MinGatherItems = GatherItems;
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

            uint Cosmo = ExpSheet.GetRow(keyId).Unknown0;
            uint Lunar = ExpSheet.GetRow(keyId).Unknown1;

            if (ExpSheet.GetRow(keyId).Unknown2 != 0)
            {
                Exp.Add((ExpSheet.GetRow(keyId).Unknown12, ExpSheet.GetRow(keyId).Unknown2));
            }
            if (ExpSheet.GetRow(keyId).Unknown3 != 0)
            {
                Exp.Add((ExpSheet.GetRow(keyId).Unknown13, ExpSheet.GetRow(keyId).Unknown3));
            }
            if (ExpSheet.GetRow(keyId).Unknown4 != 0)
            {
                Exp.Add((ExpSheet.GetRow(keyId).Unknown14, ExpSheet.GetRow(keyId).Unknown4));
            }

            uint nodeSet = 0;
            if (GatheringUtil.Nodeset.TryGetValue(new Vector2(_x, _y), out nodeSet)) 
            {

            }

            if (!MissionInfoDict.ContainsKey(keyId))
            {
                MissionInfoDict[keyId] = new MissionListInfo()
                {
                    Name = LeveName,
                    JobId = ((uint)JobId),
                    JobId2 = ((uint)Job2),
                    ToDoSlot = toDoValue,
                    Rank = rank,
                    Attributes = attributes,
                    TimeLimit = timeLimit,
                    StartTime = startTime,
                    EndTime = endTime,
                    Weather = weather,
                    RecipeId = RecipeId,
                    BronzeRequirement = bronze,
                    SilverRequirement = silver,
                    GoldRequirement = gold,
                    CosmoCredit = Cosmo,
                    LunarCredit = Lunar,
                    ExperienceRewards = Exp,
                    PreviousMissionID = previousMissionId,
                    MarkerId = marker.RowId,
                    TerritoryId = territoryId,
                    X = _x,
                    Y = _y,
                    NodeSet = nodeSet,
                    Radius = radius,
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

        if (C.Missions.Count == 0)
        {
            // fresh install?
            C.Missions = [.. MissionInfoDict.Select(x => new CosmicMission()
        {
            Id = x.Key,
            Name = x.Value.Name,
            Type = GetMissionType(x.Value),
            PreviousMissionId = x.Value.PreviousMissionID,
            JobId = x.Value.JobId,
        })];
            C.Save();
        }
        else
        {
            var newMissions = MissionInfoDict.Where(x => !C.Missions.Any(y => y.Id == x.Key)).Select(x => new CosmicMission()
            {
                Id = x.Key,
                Name = x.Value.Name,
                Type = GetMissionType(x.Value),
                PreviousMissionId = x.Value.PreviousMissionID,
                JobId = x.Value.JobId,
            });

            if (newMissions.Any())
            {
                C.Missions.AddRange(newMissions);
                C.Save();
            }
        }
    }
    private static MissionType GetMissionType(MissionListInfo mission)
    {
        if (mission.Attributes.HasFlag(Critical))
        {
            return MissionType.Critical;
        }
        else if (mission.Attributes.HasFlag(ProvisionalTimed))
        {
            return MissionType.Timed;
        }
        else if (mission.Attributes.HasFlag(ProvisionalWeather))
        {
            return MissionType.Weather;
        }
        else if (mission.Attributes.HasFlag(ProvisionalSequential))
        {
            return MissionType.Sequential;
        }

        return MissionType.Standard;
    }
}
