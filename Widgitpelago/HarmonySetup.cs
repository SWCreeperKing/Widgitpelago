using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace Widgitpelago;

public static class HarmonySetup
{
    public static void Init(Assembly assembly, HarmonyLib.Harmony harmonyInstance)
    {
        var finalizer = new HarmonyMethod(typeof(HarmonySetup).GetMethod("Finalizer"));

        var classesToPatch = assembly.GetTypes()
                                     .Where(t => t.GetCustomAttributes(typeof(PatchAllAttribute), false).Any())
                                     .ToArray();

        Core.Log.Msg($"Loading [{classesToPatch.Length}] Class patches");

        foreach (var patch in classesToPatch)
        {
            harmonyInstance.PatchAll(patch);

            // var methods =
            //     patch.GetMethods()
            //          .Where(mi => mi.GetCustomAttributes(typeof(HarmonyPatch), false).Any())
            //          .GroupBy(mi =>
            //               {
            //                   var method
            //                       = ((HarmonyPatch)mi.GetCustomAttributes(typeof(HarmonyPatch)).First()).info;
            //                   return
            //                       $"{method.declaringType.Name}::{method.method?.Name ?? method.methodName}"
            //                       + $"({(method.argumentTypes is not null && method.argumentTypes.Length != 0 
            //                           ? $"{method.argumentTypes.Select(t => t.Name)}" : "")})";
            //               }
            //           )
            //          .ToArray();
            //
            // List<string> patched = [];
            // foreach (var grouping in methods)
            // {
            //     var method = grouping.First();
            //     var attr = (HarmonyPatch)method.GetCustomAttributes(typeof(HarmonyPatch)).First();
            //
            //     patched.Add(grouping.Key);
            //
            //     harmonyInstance.Patch(
            //         // attr.info.method, finalizer: finalizer.Merge(HarmonyMethodExtensions.GetMergedFromMethod(method))
            //         attr.info.method, finalizer: finalizer
            //     );
            // }
            //
            // Core.Log.Msg($"Loaded patches from: [{patch.Name}], patching methods: [{string.Join(", ", patched)}]");
            // Core.Log.Msg($"Loaded patches from: [{patch.Name}]");
        }
    }

    [HarmonyFinalizer]
    public static void Finalizer(Exception __exception)
    {
        if (__exception is null) return;
        Core.Log.Error("From Finalizer: ", __exception);
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class PatchAllAttribute : Attribute;