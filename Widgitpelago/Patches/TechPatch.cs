using Assets.Source.Player;
using HarmonyLib;

namespace Widgitpelago.Patches;

[PatchAll]
public static class TechPatch
{
    [HarmonyPatch(typeof(GamePlayer), "SetTechTier"), HarmonyPrefix]
    public static bool SetTechTier(ref int tier)
    {
        if (tier < 1)
        {
            tier = -tier;
            return true;
        }
        
        // todo: send Tier location
        Core.Log.Msg($"Tier Set: [{tier}]");
        // return false;
        return true;
    }

    [HarmonyPatch(typeof(GamePlayer), "AddTech"), HarmonyPrefix]
    public static bool AddTech(ref TechNode tech, bool notify = false)
    {
        if (tech.Identifier.StartsWith("AP__"))
        {
            tech = tech.Identifier[4..];
            return true;
        }
        
        // todo: send tech location
        Core.Log.Msg($"Added Tech: [{tech.Identifier}]");
        // return false;
        return true;
    }

    // [HarmonyPatch(typeof(TechNode), "get_IsPurchased"), HarmonyPrefix]
    // public static bool Purchased(ref bool __result)
    // {
    //     __result = false;
    //     return false;
    // }
    //
    // [HarmonyPatch(typeof(TechNode), "get_IsAvailable"), HarmonyPrefix]
    // public static bool Available(TechNode __instance, ref bool __result)
    // {
    //     if (GamePlayer.Current.TechTier < __instance.Tier)
    //         return false;
    //     __result = __instance.Previous is null || __instance.Previous.IsPurchased;
    //     return false;
    // }
}