using Assets.Behaviour.Frame.Parts;
using HarmonyLib;
using Widgitpelago.Archipelago;

namespace Widgitpelago.Patches;

[PatchAll]
public static class RocketPatch
{
    [HarmonyPatch(typeof(T12LaunchPadLauncher), "LaunchButton"), HarmonyPostfix]
    public static void Goal()
    {
        if (WidgetClient.Client.HasGoaled) return;
        WidgetClient.Client.Goal();
    }
}