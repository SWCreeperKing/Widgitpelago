using Assets.Source.World;
using HarmonyLib;

namespace Widgitpelago.Patches;

[PatchAll]
public static class ProductivityPatch
{
    public static long Multiplier = 4;
    public static long HandMultiplier = 2;

    [HarmonyPatch(typeof(WorldFrame), "GetProductivityMultiplier"), HarmonyPostfix]
    public static void ProductivityMulti(bool handCraft, ref double __result) => __result *= handCraft ? HandMultiplier : Multiplier;
}