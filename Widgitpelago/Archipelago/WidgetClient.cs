using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Archipelago.MultiClient.Net.Enums;
using Assets.Source.Util;
using CreepyUtil.Archipelago;
using CreepyUtil.Archipelago.ApClient;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine.UI;
using Widgitpelago.Patches;

namespace Widgitpelago.Archipelago;

public static class WidgetClient
{
    public static Dictionary<string, string> FrameIdMap;
    public static Dictionary<string, string> IdFrameMap;
    public static ApClient Client = new(new TimeSpan(0, 1, 0));
    public static ApData Data = new();
    public static string GameUUID = "";
    
    public static void Init()
    {
        if (File.Exists("ApConnection.json"))
        {
            Data = JsonConvert.DeserializeObject<ApData>(File.ReadAllText("ApConnection.json").Replace("\r", ""));
        }

        if (File.Exists("ApConnection.txt"))
        {
            Data = new ApData();
            SaveFile();
            File.Delete("ApConnection.txt");
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

                var l = new List<SaveGameFile>();
                SavePatch.GetSavePath(ref l);
                
                if (l.Count == 0) return;
                MainMenuUI.Instance.GetPrivateField<Button>("_continueButton").interactable = true;
                MainMenuUI.Instance.GetPrivateField<Button>("_loadGameButton").interactable = true;
            }
            catch (Exception e)
            {
                Core.Log.Error(e);
            }
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
            
            foreach (var item in Client.GetOutstandingItems()!)
            {
                 ItemHandler.HandleItem(item);
            }
        }
        catch (Exception e)
        {
            Core.Log.Error(e);
        }
    }
}