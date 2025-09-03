using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using static ICE.Utilities.CosmicHelper;
using static ICE.Enums.MissionAttributes;
using static ICE.Utilities.ExcelHelper;

namespace ICE;

public sealed partial class ICE
{
    public static unsafe void DictionaryCreation()
    {
        MoonRecipies = [];

        var wk = WKSManager.Instance();

        foreach (var item in MoonMissionSheet)
        {
            List<(int Type, int Amount)> Exp = [];
            Dictionary<ushort, int> MainItems = [];
            Dictionary<ushort, int> PreCrafts = [];
            Dictionary<uint, int> GatherItems = [];
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
            uint time = 0;
            CosmicWeather weather = CosmicWeather.FairSkies;
            if (timeAndWeather <= 12)
            {
                time = timeAndWeather;
            }
            else
            {
                weather = (CosmicWeather)(timeAndWeather - 12);
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
            attributes |= time != 0 ? ProvisionalTimed : None;
            attributes |= previousMissionId != 0 ? ProvisionalSequential : None;

            uint bronze = todo.Unknown2; // Bronze score for Score missions
            attributes |= bronze > 0 ? ScoreScore : None;

            if (CrafterJobList.Contains(JobId))
            {
                bool preCraftsbool = false;

                var toDoRow = ToDoSheet.GetRow(toDoValue);
                if (isCritical) // Criticals are sus
                {
                    UInt16 item1Amount = 1; // It's a pass/fail progress, you need to go till you are full on score
                    var item1RecipeRow = RecipeSheet.Where(e => e.RowId == MoonRecipeSheet.GetRow(item.WKSMissionRecipe.RowId).Recipe[0].Value.RowId).First();
                    var item1Id = item1RecipeRow.ItemResult.RowId;
                    var item1Name = ItemSheet.GetRow(item1Id).Name.ToString();
                    var craftingType = item1RecipeRow.CraftType.Value.RowId;
                    IceLogging.Verbose($"Recipe Row ID: {item1RecipeRow.RowId} | for item: {item1Id} | {item1Name}");
                    var item1RecipeId = item1RecipeRow.RowId;
                    MainItems.Add(((ushort)item1RecipeId), item1Amount);
                }
                if (toDoRow.RequiredItem[0].RowId != 0 && !isCritical) // shouldn't be 0, 1st item entry
                {
                    var item1Amount = toDoRow.RequiredItemQuantity[0]; // unknown 6
                    var item1Id = MoonItemInfoSheet.GetRow(toDoRow.RequiredItem[0].RowId).Item.RowId;
                    var item1Name = ItemSheet.GetRow(item1Id).Name.ToString();
                    var item1RecipeRow = RecipeSheet.Where(e => e.ItemResult.RowId == item1Id)
                                                    .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[0].Value.RowId ||
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[1].Value.RowId ||
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[2].Value.RowId)
                                                    .First();
                    var craftingType = item1RecipeRow.CraftType.Value.RowId;
                    IceLogging.Verbose($"Recipe Row ID: {item1RecipeRow.RowId} | for item: {item1Id} | {item1Name}");
                    for (var i = 0; i <= 3; i++)
                    {
                        var subitem = item1RecipeRow.Ingredient[i].Value.RowId;
                        if (subitem != 0)
                        {
                            IceLogging.Verbose($"subItemId: {subitem} slot [{i}]");
                            var subitemRecipe = RecipeSheet.Where(x => x.ItemResult.RowId == subitem)
                                                           .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[0].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[1].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[2].Value.RowId)
                                                           .FirstOrDefault();
                            if (subitemRecipe.RowId != 0)
                            {
                                var subItemAmount = item1RecipeRow.AmountIngredient[i].ToInt();
                                subItemAmount = subItemAmount * item1Amount;
                                PreCrafts.Add(((ushort)subitemRecipe.RowId), subItemAmount);
                                preCraftsbool = true;
                            }
                        }
                    }
                    var item1RecipeId = item1RecipeRow.RowId;
                    MainItems.Add(((ushort)item1RecipeId), item1Amount);
                }
                if (toDoRow.RequiredItem[1].RowId != 0) // 2nd item entry
                {
                    var item2Amount = toDoRow.RequiredItemQuantity[1];
                    var item2Id = MoonItemInfoSheet.GetRow(toDoRow.RequiredItem[1].RowId).Item.RowId;
                    var item2Name = ItemSheet.GetRow(item2Id).Name.ToString();

                    var item2RecipeRow = RecipeSheet.Where(e => e.ItemResult.RowId == item2Id)
                                                    .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[0].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[1].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[2].Value.RowId)
                                                    .First();
                    IceLogging.Verbose($"Recipe Row ID: {item2RecipeRow.RowId} | for item: {item2Id} | {item2Name}");
                    for (var i = 0; i <= 3; i++)
                    {
                        var subitem = item2RecipeRow.Ingredient[i].Value.RowId;
                        if (subitem != 0)
                        {
                            IceLogging.Verbose($"subItemId: {subitem} slot [{i}]");
                            var subitemRecipe = RecipeSheet.Where(e => e.ItemResult.RowId == item2Id)
                                                           .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[0].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[1].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[2].Value.RowId)
                                                           .First();
                            if (subitemRecipe.RowId != 0)
                            {
                                var subItemAmount = item2RecipeRow.AmountIngredient[i].ToInt();
                                subItemAmount = subItemAmount * item2Amount;
                                PreCrafts.Add(((ushort)subitemRecipe.RowId), subItemAmount);
                                preCraftsbool = true;
                            }
                        }
                    }
                    var item2RecipeId = item2RecipeRow.RowId;
                    MainItems.Add(((ushort)item2RecipeId), item2Amount);
                }
                if (toDoRow.RequiredItem[2].RowId != 0) // 3rd item entry
                {
                    var item3Amount = toDoRow.RequiredItemQuantity[2];
                    var item3Id = MoonItemInfoSheet.GetRow(toDoRow.RequiredItem[2].RowId).Item.RowId;
                    var item3Name = ItemSheet.GetRow(item3Id).Name.ToString();

                    var item3RecipeRow = RecipeSheet.Where(e => e.ItemResult.RowId == item3Id)
                                                    .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[0].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[1].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[2].Value.RowId)
                                                    .First();
                    IceLogging.Verbose($"Recipe Row ID: {item3RecipeRow.RowId} | for item: {item3Id} | {item3Name}");
                    for (var i = 0; i <= 3; i++)
                    {
                        var subitem = item3RecipeRow.Ingredient[i].Value.RowId;
                        if (subitem != 0)
                        {
                            IceLogging.Verbose($"subItemId: {subitem} slot [{i}]");
                            var subitemRecipe = RecipeSheet.FirstOrDefault(x => x.ItemResult.RowId == subitem);
                            if (subitemRecipe.RowId != 0)
                            {
                                var subItemAmount = item3RecipeRow.AmountIngredient[i].ToInt();
                                subItemAmount = subItemAmount * item3Amount;
                                PreCrafts.Add(((ushort)subitemRecipe.RowId), subItemAmount);
                                preCraftsbool = true;
                            }
                        }
                    }
                    var item3RecipeId = item3RecipeRow.RowId;
                    MainItems.Add(((ushort)item3RecipeId), item3Amount);
                }

                if (MainItems.Count == 0)
                {
                    // Has to be one of the new moon missions... becuase square hates me I swear lol
                    var wksRecipeSheet = MoonRecipeSheet.GetRow(RecipeId);
                    var recipeId = wksRecipeSheet.Recipe[0].RowId;
                    var recipeName = ItemSheet.GetRow(recipeId).Name.ToString();
                    var recipeRow = RecipeSheet.GetRow(recipeId);
                    var recipeAmount = 1;

                    for (var i = 0; i < 4; i++)
                    {
                        var subItem = recipeRow.Ingredient[i].Value.RowId;
                        if (subItem != 0)
                        {
                            IceLogging.Verbose($"subItem: {subItem} slot [{i}]");
                            var subitemRecipe = RecipeSheet.FirstOrDefault(x => x.ItemResult.RowId == subItem);
                            if (subitemRecipe.RowId != 0)
                            {
                                var subItemAmount = recipeRow.AmountIngredient[i].ToInt();
                                subItemAmount = subItemAmount * recipeAmount;
                                PreCrafts.Add(((ushort)subitemRecipe.RowId), subItemAmount);
                                preCraftsbool = true;
                            }
                        }
                    }

                    MainItems.Add((ushort)recipeId, recipeAmount);
                }

                if (preCraftsbool)
                {
                    foreach (var preItem in PreCrafts)
                    {
                        if (MainItems.ContainsKey(preItem.Key))
                            PreCrafts.Remove(preItem.Key);
                    }

                    if (PreCrafts.Count == 0)
                    {
                        preCraftsbool = false;
                    }
                }

                if (!MoonRecipies.ContainsKey(keyId))
                {
                    MoonRecipies[keyId] = new MoonRecipieInfo()
                    {
                        MainCraftsDict = MainItems,
                        PreCraftDict = PreCrafts,
                        PreCrafts = preCraftsbool
                    };
                }

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
                var DualRecipeId = MoonRecipeSheet.GetRow(MissionRecipe).Recipe[0].Value.RowId;
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
                    Time = time,
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
