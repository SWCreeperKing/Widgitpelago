using Assets.Behaviour.UI;
using Assets.Source.Player;
using MelonLoader;
using UnityEngine;
using Widgitpelago;
using Widgitpelago.Archipelago;

[assembly: MelonInfo(typeof(Core), "Widgitpelago", Core.VersionNumber, "SW_CreeperKing", null)]
[assembly: MelonGame("Leaping Turtle", "WidgetInc")]

namespace Widgitpelago
{
    public class Core : MelonMod
    {
        public const string VersionNumber = "0.1.0";
        
        public static GameObject ContinueButton;
        public static GameObject NewGameButton;
        public static GameObject LoadGameButton;
        public static string Scene;
        
        public static MelonLogger.Instance Log;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;

            Log.Msg("Running Shenanigans");

            ApShenanigans.RunShenanigans();

            Log.Msg("Shenanigans ran");

            var classesToPatch = MelonAssembly.Assembly.GetTypes()
                                              .Where(t => t.GetCustomAttributes(typeof(PatchAllAttribute), false).Any())
                                              .ToArray();

            Log.Msg($"Loading [{classesToPatch.Length}] Class patches");

            foreach (var patch in classesToPatch)
            {
                HarmonyInstance.PatchAll(patch);

                Log.Msg($"Loaded: [{patch.Name}]");
            }

            Log.Msg("Loading Data");

            WidgetClient.FrameIdMap = File.ReadAllLines($"{ApShenanigans.DataFolder}/idMap.txt").Select(s => s.Split(':')).ToDictionary(arr => arr[0], arr => arr[1]);
            WidgetClient.IdFrameMap = WidgetClient.FrameIdMap.ToDictionary(kv => kv.Value, kv => kv.Key);

            LoggerInstance.Msg("Initialized.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            Scene = sceneName;
            switch (sceneName)
            {
                case "MainMenu":
                    ContinueButton = GameObject.Find("Canvas/Menu/Continue");
                    NewGameButton = GameObject.Find("Canvas/Menu/New Game");
                    LoadGameButton = GameObject.Find("Canvas/Menu/Load Game");
                    break;
                case "Game":
                    break;
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class PatchAllAttribute : Attribute;