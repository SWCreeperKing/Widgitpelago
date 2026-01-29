using Assets.Source.Util;
using HarmonyLib;
using JetBrains.Annotations;

namespace Widgitpelago.Patches;

[PatchAll]
public static class SavePatch
{
    [CanBeNull] public static DirectoryInfo SavePath;
    [CanBeNull] public static List<SaveGameFile> Files;

    [HarmonyPatch(typeof(SaveGame), "GetSaveGames"), HarmonyPrefix]
    public static bool GetSavePath(ref List<SaveGameFile> __result)
    {
        if (SavePath is null)
        {
            __result = [];
            return false;
        }

        Files ??= SavePath.GetFiles("*.save").Select(file => new SaveGameFile(file)).ToList();

        __result = Files;
        return false;
    }
}