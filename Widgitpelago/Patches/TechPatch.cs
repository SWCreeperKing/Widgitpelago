using System.Numerics;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Assets.Source.Item;
using Assets.Source.Player;
using Assets.Source.Util;
using Assets.Source.World;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Widgitpelago.Archipelago;
using Color = UnityEngine.Color;

namespace Widgitpelago.Patches;

[PatchAll]
public static class TechPatch
{
    public static readonly Color NormalColor = new(0.5f, 1, 0.5f, 0.502f);
    public static readonly Color HintedColor = new(0, 1, 1, 0.502f);
    public static readonly Color HintedCantGetColor = new(0.3f, 0.3f, 1, 0.502f);

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
        WidgetClient.SendLocation(WidgetClient.IdFrameMap[__instance.Tech.Identifier]);
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
        try
        {
            if (!WidgetClient.IdFrameMap.TryGetValue(__instance.Identifier, out var value)) return true;
            __result = !WidgetClient.Client.MissingLocations.Contains(value);
        }
        catch (Exception e) { Core.Log.Error(e); }
        return false;
    }

    [HarmonyPatch(typeof(TechNode), "get_IsAvailable"), HarmonyPrefix]
    public static bool Available(TechNode __instance, ref bool __result, string ___Identifier)
    {
        try
        {
            if (!WidgetClient.IdFrameMap.ContainsKey(___Identifier)) return true;
            if (GamePlayer.Current.TechTier < __instance.Tier) return false;

            // if (WidgetClient.IdFrameMap[___Identifier] is "Tier 2") Core.Log.Msg($"Logic: [{WidgetClient.IsInLogic(___Identifier)}]");

            __result = (__instance.Previous is null
                        || ItemHandler.FramesHave.Contains(WidgetClient.IdFrameMap[__instance.Previous.Identifier]))
                       && WidgetClient.IsInLogic(___Identifier);
        }
        catch (Exception e) { Core.Log.Error(e); }
        return false;
    }

    [HarmonyPatch(typeof(TechTreeNode), "UpdateStatus"), HarmonyPostfix]
    public static void ShowHave(TechTreeNode __instance)
    {
        try
        {
            if (!WidgetClient.IdFrameMap.TryGetValue(__instance.Node.Identifier, out var value)) return;
            var have = ItemHandler.FramesHave.Contains(value);
            __instance.GetPrivateField<SpriteRenderer>("_purchased").gameObject.SetActive(have);
        }
        catch (Exception e) { Core.Log.Error(e); }
    }

    [HarmonyPatch(typeof(TechTreeNode), "UpdateStatus"), HarmonyPostfix]
    public static void IconReplace(TechTreeNode __instance, SpriteRenderer ____icon, SpriteRenderer ____glow,
        ref Color ____glowColor)
    {
        var item = CustomAssets.ScoutItem(WidgetClient.IdFrameMap[__instance.Node.Identifier]);
        var sprite = CustomAssets.ItemImage(item);
        sprite.texture.filterMode = FilterMode.Point;
        ____icon.sprite = sprite;

        if (__instance.Node.IsPurchased) return;
        if (!WidgetClient.HintData.TryGetValue(item.ItemId, out var hint)) return;
        if (hint.Found || hint.Status is not HintStatus.Priority) return;
        ____glowColor = __instance.Node.IsAvailable ? HintedColor : HintedCantGetColor;
        ____glow.gameObject.SetActive(true);
        Core.Log.Msg($"glow for [{item.ItemName}]");
    }

    [HarmonyPatch(typeof(TechTreeNode), "GetTooltipTitle"), HarmonyPostfix]
    public static void TooltipName(TechTreeNode __instance, ref string __result, TechNode ___Node) => __result = null;

    [HarmonyPatch(typeof(TechTreeNode), "AddTooltipCustomContent"), HarmonyPrefix]
    public static bool Tooltip(
        TechTreeNode __instance, UITooltip tooltip, TechNode ___Node, ConstructionProgress ____construction
    )
    {
        var item = CustomAssets.ScoutItem(WidgetClient.IdFrameMap[___Node.Identifier]);

        tooltip.SetText($"<color={item.Flags.GetColorFromItemFlag()}>{item.ItemName}</color>", 24);
        tooltip.SetText($"for {item.Player.Alias}");
        tooltip.SetText($"<color={item.Flags.GetColorFromItemFlag()}>{item.Flags.GetTextFromItemFlag()}</color>");
        tooltip.SetText($"<color=#747474>{WidgetClient.IdFrameMap[___Node.Identifier]}</color>", 12);
        if (____construction != null)
        {
            tooltip.AddTextLine("@TechNodeCancel");
            tooltip.AddConstructionLines(____construction);
        }
        else if (!__instance.Node.IsPurchased) tooltip.AddCostLines(___Node.GetCost());

        return false;
    }

    public static UITooltipText SetText(this UITooltip tooltip, string text, int fontSize = 16)
    {
        var textLine = tooltip.AddTextLine("");
        var label = textLine.GetPrivateField<TMP_Text>("_text");
        label.text = text;
        label.fontSize = fontSize;
        return textLine;
    }

    // [HarmonyPatch(typeof(TechTreeNode), "AddTooltipCustomContent"), HarmonyPostfix]
    // public static void FinalizerTest()
    // {
    //     throw new NullReferenceException("teehee");
    // }
}