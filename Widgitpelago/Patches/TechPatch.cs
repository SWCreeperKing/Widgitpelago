using Assets.Source.Player;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Widgitpelago.Archipelago;

namespace Widgitpelago.Patches;

[PatchAll]
public static class TechPatch
{
    [HarmonyPatch(typeof(GamePlayer), "SetTechTier"), HarmonyPrefix]
    public static bool SetTechTier(ref int tier)
    {
        if (tier >= 1) return false;
        tier = -tier;
        return true;
    }
    
    // [HarmonyPatch(typeof(TechTreeNode), "Start"), HarmonyPostfix]
    // public static void ReplaceButtonPress(TechTreeNode __instance)
    // {
    //     var buttonEvent = __instance.GetPrivateField<Button>("_button").onClick = new Button.ButtonClickedEvent();
    //     buttonEvent.AddListener(() =>
    //     {
    //         WidgetClient.Client.SendLocation(WidgetClient.IdFrameMap[__instance.Node.Identifier]);
    //     });
    // }
    
    [HarmonyPatch(typeof(GamePlayer.TechConstructionProgress), "OnConstructionCompleted"), HarmonyPostfix]
    public static void ReplaceButtonPress(GamePlayer.TechConstructionProgress __instance)
    { 
        WidgetClient.Client.SendLocation(WidgetClient.IdFrameMap[__instance.Tech.Identifier]);
    }

    [HarmonyPatch(typeof(GamePlayer), "AddTech"), HarmonyPrefix]
    public static bool AddTech(ref TechNode tech, bool notify = false)
    {
        if (!tech.Identifier.StartsWith("AP__")) return false;
        tech = tech.Identifier[4..];
        return true;
    }

    [HarmonyPatch(typeof(TechNode), "get_IsPurchased"), HarmonyPrefix]
    public static bool Purchased(TechNode __instance, ref bool __result)
    {
        if (!WidgetClient.IdFrameMap.ContainsKey(__instance.Identifier)) return true;
        __result = !WidgetClient.Client.MissingLocations.Contains(WidgetClient.IdFrameMap[__instance.Identifier]);
        return false;
    }

    [HarmonyPatch(typeof(TechNode), "get_IsAvailable"), HarmonyPrefix]
    public static bool Available(TechNode __instance, ref bool __result)
    {
        if (!WidgetClient.IdFrameMap.ContainsKey(__instance.Identifier)) return true;
        if (GamePlayer.Current.TechTier < __instance.Tier) return false;
        __result = __instance.Previous is null || ItemHandler.FramesHave.Contains(WidgetClient.IdFrameMap[__instance.Previous.Identifier]);
        return false;
    }

    [HarmonyPatch(typeof(TechTreeNode), "UpdateStatus"), HarmonyPostfix]
    public static void ShowHave(TechTreeNode __instance)
    {
        if (!WidgetClient.IdFrameMap.ContainsKey(__instance.Node.Identifier)) return;
        var have = ItemHandler.FramesHave.Contains(WidgetClient.IdFrameMap[__instance.Node.Identifier]);
        __instance.GetPrivateField<SpriteRenderer>("_purchased").gameObject.SetActive(have);
    }
}