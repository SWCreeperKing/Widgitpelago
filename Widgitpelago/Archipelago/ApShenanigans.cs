using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;
using Range = CreepyUtil.Archipelago.WorldFactory.Range;

namespace Widgitpelago.Archipelago;

public static class ApShenanigans
{
    // Spreadsheet used for logic:
    // https://docs.google.com/spreadsheets/d/1IdcSvjcpVu7AlMY5EUUnbr9mLd9X6LaLiIemBAgD_gA/edit?usp=sharing
    public const string Spreadsheet = "Widget Inc - Sheet1.csv";
    public const string DataFolder = "Mods/SW_CreeperKing.Widgitpelago/Data";

    public const string FileLink
        = "https://github.com/SWCreeperKing/Widgitpelago/blob/master/Widgitpelago/Archipelago/ApShenanigans.cs";

    public const string ApWorldOutput = "E:/coding projects/python/Deathipelago/worlds/widget_inc";

    public static void RunShenanigans()
    {
        if (!File.Exists(Spreadsheet)) return;
        if (!Directory.Exists("output")) Directory.CreateDirectory("output");
        if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);

        CsvParser parser = new(Spreadsheet, 1, 0);

        parser.ToFactory()
              .ReadTable(new TechTreeCreator(), 6, out var techTreeData).SkipColumn()
              .ReadTable(new ResourceCreator(), 2, out var resourceData);

        var frameIdMap = techTreeData.ToDictionary(data => data.Tech, data => data.Id);

        var resourceBuildingRequirement =
            techTreeData.Where(data => data.Unlock != "")
                        .ToDictionary(data => data.Unlock, data => data.Tech);

        var craftingRecipes =
            resourceData.ToDictionary(data => data.Resource, data => data.CraftingRequirements);

        var craftlessResources =
            resourceBuildingRequirement.Keys.Except(craftingRecipes.Keys).ToArray();

        var worldFactory = new WorldFactory("Widget Inc")
                          .SetOnCompilerError((e, s) => Core.Log.Error(s, e))
                          .SetOutputDirectory(ApWorldOutput);

        var endingNodes = techTreeData.Where(tech => techTreeData.All(data => data.PreviousTech != tech.Tech)).Select(data => data.Tech).ToArray();
        
        worldFactory
           .GetOptionsFactory(FileLink)
           .AddOption("Production Multiplier", "Gives a production multiplier", new Range(4, 1, 10))
           .AddOption("Hand Crafting Multiplier", "Gives a multiplier to hand crafting", new Range(2, 1, 10))
           .AddCheckOptions()
           .GenerateOptionFile();

        worldFactory
           .GetLocationFactory(FileLink)
           .AddLocations(
                "tech_tree",
                techTreeData
                   .Append(new TechTreeData(["Starting Check (1)", "", "", "", "", "0"]))
                   .Append(new TechTreeData(["Starting Check (2)", "", "", "", "", "0"]))
                   .Append(new TechTreeData(["Starting Check (3)", "", "", "", "", "0"]))
                   .Select(data => (string[])
                        [data.Tech, $"Tier {data.TierRequirement}".Replace("Tier 0", "Menu")]
                    )
            )
           .GenerateLocationFile();

        var ruleFactory =
            worldFactory.GetRuleFactory(FileLink)
                        .AddLogicFunction(
                             "tier", "has_tier", StateHasSR("Progressive Tier", "tier"), "tier"
                         )
                        .AddLogicFunction("frame", "has_frame", StateHasR("frame"), "frame");

        craftlessResources.Aggregate(
            ruleFactory, (factory, s) =>
                factory.AddCompoundLogicFunction(
                    s.Replace(" ", ""),
                    s.ToLower().Replace(" ", "_"), $"frame['{resourceBuildingRequirement[s]}']"
                )
        );

        craftingRecipes.Aggregate(
            ruleFactory, (factory, pair) =>
                factory.AddCompoundLogicFunction(
                    pair.Key.Replace(" ", ""), pair.Key.ToLower().Replace(" ", "_"),
                    $"frame[\"{techTreeData.First(data => data.Unlock == pair.Key).Tech}\"] and {string.Join(" and ", pair.Value.Select(s => s.Replace(" ", "")))}"
                )
        );

        techTreeData.Aggregate(
            ruleFactory, (factory, data) =>
            {
                List<string> rules = [];

                if (data.PreviousTech is not "") rules.Add($"frame['{data.PreviousTech}']");
                rules.AddRange(data.ResourceRequirements.Select(res => res.Replace(" ", "")));
                rules.Add($"tier[{data.TierRequirement}]");

                return factory.AddLogicRule(data.Tech, string.Join(" and ", rules));
            }
        );

        ruleFactory.GenerateRulesFile();

        worldFactory.GetItemFactory()
                    .AddItem("Motivational Poster", ItemFactory.ItemClassification.Filler)
                    .AddItemCountVariable(
                         "progressive_tier", new Dictionary<string, int> { ["Progressive Tier"] = 12 },
                         ItemFactory.ItemClassification.Progression
                     )
                    .AddItems(
                         ItemFactory.ItemClassification.Filler,
                         items: techTreeData.Where(tech => endingNodes.Contains(tech.Tech) && tech.Unlock is "").Select(data => data.Tech)
                                            .Where(s => !s.StartsWith("Tier") || s.EndsWith("Mastery")).ToArray()
                     )
                    .AddItems(
                         ItemFactory.ItemClassification.Progression,
                         items: techTreeData.Where(tech => !endingNodes.Contains(tech.Tech) || tech.Unlock is not "").Select(data => data.Tech)
                                            .Where(s => !s.StartsWith("Tier") || s.EndsWith("Mastery")).ToArray()
                     )
                    .AddCreateItems(method =>
                         method
                            .AddCode(CreateItemsFromMapCountGenCode("progressive_tier"))
                            .AddCode(CreateItemsFromClassificationList())
                            .AddCode(CreateItemsFillRemainingWithItem("Motivational Poster"))
                     )
                    .GenerateItemsFile();

        var regionFactory = worldFactory
           .GetRegionFactory(FileLink);

        for (var i = 0; i < 12; i++)
        {
            regionFactory.AddRegion($"Tier {i + 1}");

            if (i == 0)
            {
                regionFactory.AddConnectionCompiledRule("Menu", "Tier 1", "tier[1]");
                continue;
            }
            regionFactory.AddConnectionCompiledRule($"Tier {i}", $"Tier {i + 1}", $"tier[{i + 1}]");
        }

        regionFactory
           .AddLocationsFromList("tech_tree")
           .GenerateRegionFile();

        worldFactory
           .GetInitFactory(FileLink)
           .UseInitFunction()
           .UseGenerateEarly()
           .AddUseUniversalTrackerPassthrough()
           .UseGenerateEarly(method => method.AddCode(CreatePushPrecollected("Widget Factory")))
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method
                                 .AddCode("player = self.player")
                                 .AddCode(
                                      $"self.multiworld.completion_condition[self.player] = lambda state: {worldFactory.GetRuleFactory().GenerateCompiledRule("RocketSegment")}"
                                  )
            )
           .UseFillSlotData(
                new Dictionary<string, string> { ["uuid"] = "str(shuffled)" },
                method => method.AddCode(CreateUniqueId())
            )
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()))
           .GenerateInitFile();

        worldFactory.GenerateArchipelagoJson("0.6.5", Core.VersionNumber, "SW_CreeperKing");

        File.WriteAllLines($"{DataFolder}/idMap.txt", frameIdMap.Select(kv => $"{kv.Key}:{kv.Value}"));
        if (File.Exists($"output/{Spreadsheet}")) { File.Delete($"output/{Spreadsheet}"); }

        File.Move(Spreadsheet, $"output/{Spreadsheet}");
    }
}

public readonly struct TechTreeData(string[] param)
{
    public readonly string Tech = param[0].Trim();
    public readonly string PreviousTech = param[1].Trim();
    public readonly string Id = param[2].Trim();

    public readonly string[] ResourceRequirements
        = param[3].Split(',').Select(s => s.Trim()).Where(s => s is not "").ToArray();

    public readonly string Unlock = param[4].Trim();
    public readonly int TierRequirement = int.Parse(param[5].Trim());
}

public readonly struct ResourceData(string[] param)
{
    public readonly string Resource = param[0].Trim();
    public readonly string[] CraftingRequirements = param[1].Split(',').Select(s => s.Trim()).ToArray();
}

public class TechTreeCreator : CsvTableRowCreator<TechTreeData>
{
    public override TechTreeData CreateRowData(string[] param) => new(param);
}

public class ResourceCreator : CsvTableRowCreator<ResourceData>
{
    public override ResourceData CreateRowData(string[] param) => new(param);
}