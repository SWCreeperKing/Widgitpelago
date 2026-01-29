using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;

namespace Widgitpelago.Patches;

[PatchAll]
public static class GameDetectionPatches
{
    [HarmonyPatch(typeof(SaveGameFile), "LoadSaveGame"), HarmonyPostfix]
    public static void LoadGame() => GameStarted();

    [HarmonyPatch(typeof(GamePlayer), "StartNewGame"), HarmonyPostfix]
    public static void StartNewGame() => GameStarted();

    public static void GameStarted()
    {
        TechNode tech1 = "t1_tech";
        tech1.Tier = 0;
        tech1.CostMultiplier = 0;

        TechNode ironOre = "t1f_iron_ore";
        ironOre.CostMultiplier = 0;
        
        TechNode ironSmelter = "t1f_iron_ingot";
        ironSmelter.CostMultiplier = 0;
        
        // var tech = GamePlayer.Current.GetPrivateField<bool[]>("_techUnlocked");
        // for (var i = 0; i < tech.Length; i++)
        // {
        //     tech[i] = false;
        // }
        
        // SetTier(1);
        // AddTech("t1_tech");
        // AddTech("t1f_iron_ore");
        // AddTech("t1f_iron_ingot");
    }

    public static void AddTech(string tech)
    {
        if (GamePlayer.Current.HasTech(tech)) return;
        GamePlayer.Current.AddTech(new TechNode($"AP__{tech}"));
    }

    public static void SetTier(int tier)
    {
        GamePlayer.Current.SetTechTier(-tier);
    }
}