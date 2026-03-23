using System.Reflection;
using Archipelago.MultiClient.Net.Enums;
using Assets.Behaviour.UI;
using Assets.Source.Util;
using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.ApClient;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Widgitpelago.Patches;

namespace Widgitpelago.Archipelago;

public static class WidgetClient
{
    public static Dictionary<string, string[]> RequiresMap;
    public static Dictionary<string, string[]> Recipes;
    public static Dictionary<string, string> Resources;
    public static Dictionary<string, bool> CraftingCache = [];

    public static Dictionary<string, string> FrameIdMap;
    public static Dictionary<string, string> IdFrameMap;

    public static string[][] ScoutHintList;
    public static ApClient Client = new(new TimeSpan(0, 1, 0));
    public static ApData Data = new();
    public static string GameUUID = "";
    public static long ScoutLevel;

    public static void Init()
    {
        if (File.Exists("ApConnection.json"))
        {
            Data = JsonConvert.DeserializeObject<ApData>(File.ReadAllText("ApConnection.json").Replace("\r", ""));
        }

        Client.OnConnectionLost += () =>
        {
            if (Core.Scene is "Game") GameUI.Instance.IngameMenuReturnToTitle();
            Core.Log.Error("Lost Connection to Ap");
        };

        Client.OnConnectionEvent += _ =>
        {
            try
            {
                CustomAssets.ScoutedLocations.Clear();
                ItemHandler.FramesHave.Clear();
                ItemHandler.TiersHave = 0;
                ItemHandler.LocalItemsReceived = 0; 
                ItemHandler.TotalItemsReceived = Client.GetFromStorage("item_on", def: 0L); 
                CraftingCache.Clear();
                
                GameUUID = (string)Client.SlotData["uuid"];
                ProductivityPatch.Multiplier = (long)Client.SlotData["production_multiplier"];
                ProductivityPatch.HandMultiplier = (long)Client.SlotData["hand_crafting_multiplier"];

                SaveGame.SavesPath = $"Archipelago/{GameUUID}";
                var dir = new DirectoryInfo(SaveGame.SavesPath);

                var saveField = typeof(SaveGame).GetField("SavesDir", BindingFlags.NonPublic | BindingFlags.Static);
                saveField!.SetValue(null, dir);

                SavePatch.SavePath = dir;
                SavePatch.Files = null;
                if (!dir.Exists) dir.Create();

                ScoutLevel = Client.SlotData.TryGetValue("starting_tier_producers", out var scoutHintLevel)
                    ? (long)scoutHintLevel
                    : 5;

                CustomAssets.PopulateSprites(Core.Locations);

                var l = new List<SaveGameFile>();
                SavePatch.GetSavePath(ref l);

                if (l.Count == 0) return;
                MainMenuUI.Instance.GetPrivateField<Button>("_continueButton").interactable = true;
                MainMenuUI.Instance.GetPrivateField<Button>("_loadGameButton").interactable = true;
            }
            catch (Exception e) { Core.Log.Error(e); }
        };

        Client.OnConnectionErrorReceived += (e, s) => { Core.Log.Error(e); };
    }

    [CanBeNull]
    public static string[] TryConnect(string addressPort, string password, string slotName)
    {
        var addressSplit = addressPort.Split(':');

        if (addressSplit.Length != 2) return ["Address Field is incorrect"];
        if (!int.TryParse(addressSplit[1], out var port)) return ["Port is incorrect"];

        var login = new LoginInfo(port, slotName, addressSplit[0], password);

        return Client.TryConnect(login, "Widget Inc", ItemsHandlingFlags.AllItems);
    }

    public static void SaveFile() => File.WriteAllText("ApConnection.json", JsonConvert.SerializeObject(Data));

    public static void Update()
    {
        try
        {
            if (Client is null) return;
            Client.UpdateConnection();

            if (!Client.IsConnected) return;
            if (Core.Scene is not "Game") return;
            var items = Client.GetOutstandingItems()!;
            if (items.Length == 0) return;

            foreach (var (key, _) in CraftingCache.Where(kv => !kv.Value).ToArray()) { CraftingCache.Remove(key); }

            foreach (var item in items) { ItemHandler.HandleItem(item); }
        }
        catch (Exception e) { Core.Log.Error(e); }
    }

    public static bool IsInLogic(string frameId)
    {
        if (CraftingCache.TryGetValue(frameId, out var res)) return res;
        if (!RequiresMap.ContainsKey(frameId)) return true;
        return CraftingCache[frameId] = CanCraft(RequiresMap[frameId]);
    }

    public static bool CanCraft(string[] requires) => requires.All(CanCraft);

    private static bool CanCraft(string requires)
    {
        if (CraftingCache.TryGetValue(requires, out var res)) return res;
        var frame = Resources[requires];
        if (Recipes.TryGetValue(requires, out var items))
            return CraftingCache[requires] = CanCraft(items) && ItemHandler.FramesHave.Contains(frame);
        return CraftingCache[requires] = IsInLogic(FrameIdMap[frame]) && ItemHandler.FramesHave.Contains(frame);
    }

    public static void SendLocation(string loc)
    {
        if (!Client.MissingLocations.Contains(loc)) return;
        var item = CustomAssets.ScoutItem(loc);
        Client.SendLocation(loc);
        UIStatusMessage.Show($"Sent {item.ItemName} to {item.Player.Alias}", CustomAssets.ItemImage(item), false);
    }
}