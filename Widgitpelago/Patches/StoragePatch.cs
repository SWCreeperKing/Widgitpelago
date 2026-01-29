using System.Numerics;
using Assets.Source.Item;
using Assets.Source.Player;
using HarmonyLib;

namespace Widgitpelago.Patches;

[PatchAll]
public static class StoragePatch
{
    public static BigInteger InventorySize = new(5000);
    
    [HarmonyPatch(typeof(GamePlayer), "GetInventoryCapacity"), HarmonyPostfix]
    public static void StorageSize(ItemType type, ref BigInteger __result)
    {
        if (type == GamePlayer.RocketPartItem || type == GamePlayer.DemoTurtleItem || type == GamePlayer.CityPartItem || type == ItemType.GlitchedWidget || type == ItemType.Stone || type == ItemType.Biomass) return;
        __result += InventorySize;
    }
}