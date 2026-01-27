using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.WorldFactory;

namespace Widgitpelago.Archipelago;

public static class ApShenanigans
{
    // Spreadsheet used for logic:
    // https://docs.google.com/spreadsheets/d/1IdcSvjcpVu7AlMY5EUUnbr9mLd9X6LaLiIemBAgD_gA/edit?usp=sharing
    public const string Spreadsheet = "Widget Inc - Sheet1.csv";

    public static void RunShenanigans()
    {
        if (!File.Exists(Spreadsheet)) return;
        if (!Directory.Exists("output")) Directory.CreateDirectory("output");

        CsvParser parser = new(Spreadsheet, 1, 0);

        parser.ToFactory()
              .ReadTable(new TechTreeCreator(), 6, out var techTreeData).SkipColumn()
              .ReadTable(new ResourceCreator(), 2, out var resourceData);

        var resourceBuildingRequirement =
            techTreeData.Where(data => data.Unlock != "")
                        .ToDictionary(data => data.Unlock, data => data.Tech);

        var craftingRecipes =
            resourceData.ToDictionary(data => data.Resource, data => data.CraftingRequirements);

        var craftlessResources =
            resourceBuildingRequirement.Keys.Except(craftingRecipes.Keys).ToArray();

        var worldFactory = new WorldFactory("Widget Inc")
                          .SetOnCompilerError((e, s) => Core.Log.Error(s, e))
                          .SetOutputDirectory("output");

        worldFactory
           .GetLocationFactory()
           .AddLocations("tech_tree", techTreeData.Select(data => data.Tech))
           .GenerateLocationFile();

        var ruleFactory =
            worldFactory.GetRuleFactory()
                        .AddLogicFunction("tier", "has_tier", "return state.has('Progressive Tier', player, tier)", "tier")
                        .AddLogicFunction("frame", "has_frame", "return state.has(frame, player)", "frame");

        craftlessResources.Aggregate(ruleFactory, (factory, s) =>
            factory.AddCompoundLogicFunction(s.Replace(" ", ""), 
                s.ToLower().Replace(" ", "_"), $"frame['{resourceBuildingRequirement[s]}']"));

        craftingRecipes.Aggregate(ruleFactory, (factory, pair) =>
            factory.AddCompoundLogicFunction(pair.Key.Replace(" ", ""), pair.Key.ToLower().Replace(" ", "_"),
                string.Join(" and ", pair.Value.Select(s => s.Replace(" ", "")))));

        techTreeData.Aggregate(ruleFactory, (factory, data) =>
        {
            List<string> rules = [];
            
            if (data.PreviousTech is not "") rules.Add($"frame['{data.PreviousTech}']");
            rules.AddRange(data.ResourceRequirements.Select(res => res.Replace(" ", "")));
            rules.Add($"tier[{data.TierRequirement}]");
            
            return factory.AddLogicRule(data.Tech, string.Join(" and ", rules));
        });
        
        ruleFactory.GenerateRulesFile();
    }
}

public readonly struct TechTreeData(string[] param)
{
    public readonly string Tech = param[0].Trim();
    public readonly string PreviousTech = param[1].Trim();
    public readonly string Id = param[2].Trim();
    public readonly string[] ResourceRequirements = param[3].Split(',').Select(s => s.Trim()).Where(s => s is not "").ToArray();
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