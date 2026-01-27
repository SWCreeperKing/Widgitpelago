using Assets.Behaviour.UI;
using HarmonyLib;

namespace Widgitpelago.Patches;

[PatchAll]
public static class ForceCaveOptionPatch
{
    public static bool CaveOption = true;

    [HarmonyPatch(typeof(UIAlertWindow), "ShowAlert"), HarmonyPrefix]
    public static bool ForceOption(string title, string message, ref string[] buttons, Action<string> onClick)
    {
        if (title is not "@UICaveRestart") return true;
        onClick(CaveOption ? "@DialogYes" : "@DialogNo");
        return false;
    }
}