using MelonLoader;
using UnityEngine;
using Widgitpelago;
using Widgitpelago.Archipelago;

[assembly: MelonInfo(typeof(Core), "Widgitpelago", Core.VersionNumber, "SW_CreeperKing", null)]
[assembly: MelonGame("Leaping Turtle", "WidgetInc")]

namespace Widgitpelago;

public class Core : MelonMod
{
    public const string VersionNumber = "0.2.0";
    public const string DataFolder = "Mods/SW_CreeperKing.Widgitpelago/Data";

    public static string[] Locations;
    public static GameObject ContinueButton;
    public static GameObject NewGameButton;
    public static GameObject LoadGameButton;
    public static string Scene;

    public static MelonLogger.Instance Log;

    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;

        Log.Msg("Setting up Custom Assets");

        Locations = File.ReadAllLines($"{DataFolder}/locations.txt");
        CustomAssets.Init();
        
        Log.Msg("Loading Data");

        WidgetClient.FrameIdMap = File.ReadAllLines($"{DataFolder}/idMap.txt")
                                      .Select(s => s.Split(':'))
                                      .Where(arr => arr[1] is not "")
                                      .ToDictionary(arr => arr[0], arr => arr[1]);
            
        WidgetClient.IdFrameMap = WidgetClient.FrameIdMap.ToDictionary(kv => kv.Value, kv => kv.Key);

        WidgetClient.ScoutHintList = File.ReadAllLines($"{DataFolder}/scoutHints.txt")
                                         .Select(s => s.Split(',').Select(s => s.Trim()).ToArray()).ToArray();

        WidgetClient.RequiresMap = File.ReadAllLines($"{DataFolder}/requireMap.txt")
                                       .Select(s => s.Split(':')).ToDictionary(
                                            arr => arr[0], arr => arr[1].Split(',')
                                        );

        WidgetClient.Recipes = File.ReadAllLines($"{DataFolder}/recipes.txt").Select(s => s.Split(':'))
                                   .ToDictionary(arr => arr[0], arr => arr[1].Split(','));

        WidgetClient.Resources = File.ReadAllLines($"{DataFolder}/resources.txt").Select(s => s.Split(':'))
                                     .ToDictionary(arr => arr[0], arr => arr[1]);

        HarmonySetup.Init(MelonAssembly.Assembly, HarmonyInstance);
        
        LoggerInstance.Msg("Setting up Client");

        WidgetClient.Init();

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
                var obj = new GameObject("AP Menu");
                obj.AddComponent<APGui>();
                break;
            case "Game":
                WidgetClient.SendLocation("Starting Check (1)");
                WidgetClient.SendLocation("Starting Check (2)");
                WidgetClient.SendLocation("Starting Check (3)");
                break;
        }
    }

    public override void OnApplicationQuit() => WidgetClient.Client.TryDisconnect();
    public override void OnUpdate() => WidgetClient.Update();
}
