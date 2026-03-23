using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using KaitoKid.ArchipelagoUtilities.AssetDownloader.ItemSprites;
using KaitoKid.Utilities.Interfaces;
using Newtonsoft.Json;
using UnityEngine;
using ILogger = KaitoKid.Utilities.Interfaces.ILogger;

namespace Widgitpelago.Archipelago;

public static class CustomAssets
{
    public static Dictionary<string, Sprite> Spritemap = [];
    public static Dictionary<string, Sprite> ItemSprites = [];
    public static Dictionary<string, ScoutedItemInfo> ScoutedLocations = [];
    public static ArchipelagoItemSprites ItemSpritesManager;
    private static Logger Logger;

    public static void Init()
    {
        CreateSprite("prog", "Prog");
        CreateSprite("trap", "Trap");
        CreateSprite("filler", "Filler");
        CreateSprite("useful", "Useful");

        Logger = new Logger();
        ItemSpritesManager = new ArchipelagoItemSprites(Logger, JsonConvert.DeserializeObject<ItemSpriteAliases>);
    }

    public static void PopulateSprites(string[] locations)
    {
        if (!WidgetClient.Data.UseCustomAssets) return;
        foreach (var loc in locations)
        {
            try
            {
                var itemInfo = ScoutItem(loc);
                if (itemInfo is null) continue;
                ItemImage(itemInfo);
            }
            catch { Core.Log.Error($"Could not scout location: [{loc}]"); }
        }
    }

    public static ScoutedItemInfo ScoutItem(string loc)
    {
        if (!ScoutedLocations.TryGetValue(loc, out var itemInfo))
        {
            var scoutedLoc = WidgetClient.Client.ScoutLocation(loc);
            if (scoutedLoc is null) return null;
            itemInfo = ScoutedLocations[loc] = scoutedLoc;
        }
        return itemInfo;
    }

    public static string GetSpriteFromItemFlag(this ItemFlags itemFlags)
    {
        if (itemFlags.HasFlag(ItemFlags.Advancement)) return "prog";
        if (itemFlags.HasFlag(ItemFlags.NeverExclude)) return "useful";
        if (itemFlags.HasFlag(ItemFlags.Trap)) return "trap";
        return "filler";
    }
    
    public static string GetTextFromItemFlag(this ItemFlags itemFlags)
    {
        if (itemFlags.HasFlag(ItemFlags.Advancement)) return "Progression";
        if (itemFlags.HasFlag(ItemFlags.NeverExclude)) return "Useful";
        if (itemFlags.HasFlag(ItemFlags.Trap)) return "Trap";
        return "Filler";
    }
    
    public static string GetColorFromItemFlag(this ItemFlags itemFlags)
    {
        if (itemFlags.HasFlag(ItemFlags.Advancement)) return "#ffff00";
        if (itemFlags.HasFlag(ItemFlags.NeverExclude)) return "#008080";
        if (itemFlags.HasFlag(ItemFlags.Trap)) return "#ff4500";
        return "#747474";
    }

    public static void CreateSprite(string key, string file, string fileType = "png")
        => Spritemap[key] = CreateSprite($"Mods/SW_CreeperKing.Widgitpelago/Assets/{file}.{fileType}");

    public static Sprite CreateSprite(string file)
    {
        Texture2D texture = new(2, 2) { filterMode = FilterMode.Point };
        if (!texture.LoadImage(File.ReadAllBytes(file)))
            throw new ArgumentException($"Error sprite not created: [{file}]");
        return Sprite.Create(
            texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), texture.width
        );
    }

    public static Sprite ItemImage(AssetItem location)
    {
        var fallback = Spritemap[location.ItemFlags.GetSpriteFromItemFlag()];
        try
        {
            if (!WidgetClient.Data.UseCustomAssets) return fallback;

            var res = ItemSpritesManager.TryGetCustomAsset(
                location, "Widget Inc", false, true,
                out var spriteData
            );

            if (!res || spriteData is null) return fallback;
            var file = spriteData.FilePath;

            if (ItemSprites.TryGetValue(file, out var sprite)) return sprite;

            ItemSprites[file] = sprite = CreateSprite(file);
            return sprite;
        }
        catch (Exception e) { Core.Log.Error(e); }

        return fallback;
    }
}

public class Logger : ILogger
{
    public void LogError(string message) => Core.Log?.Error(message);
    public void LogError(string message, Exception e) => Core.Log?.Error(message, e);
    public void LogWarning(string message) => Core.Log?.Warning(message);
    public void LogInfo(string message) => Core.Log?.Msg(message);
    public void LogMessage(string message) => Core.Log?.Msg(message);
    public void LogDebug(string message) => Core.Log?.Msg(message);

    public void LogDebugPatchIsRunning(
        string patchedType, string patchedMethod, string patchType, string patchMethod,
        params object[] arguments
    )
        => Core.Log?.Msg($"Debug Patch: [{patchedMethod}] -> [{patchMethod}]");

    public void LogDebug(string message, params object[] arguments) => Core.Log?.Msg(message);
    public void LogErrorException(string prefixMessage, Exception ex, params object[] arguments) => Core.Log?.Error(ex);

    public void LogWarningException(string prefixMessage, Exception ex, params object[] arguments)
        => Core.Log?.Error(ex);

    public void LogErrorException(Exception ex, params object[] arguments) => Core.Log?.Error(ex);
    public void LogWarningException(Exception ex, params object[] arguments) => Core.Log?.Error(ex);
    public void LogErrorMessage(string message, params object[] arguments) => Core.Log?.Error(message);

    public void LogErrorException(string patchType, string patchMethod, Exception ex, params object[] arguments)
        => Core.Log?.Error(ex);
}

public class AssetItem(string game, string item, ItemFlags flags) : IAssetLocation
{
    public int GetSeed() => 0;
    public string GameName { get; } = game;
    public string ItemName { get; } = item;
    public ItemFlags ItemFlags { get; } = flags;

    public static implicit operator AssetItem(ScoutedItemInfo item) => new(item.ItemGame, item.ItemName, item.Flags);
    public static implicit operator AssetItem(ItemInfo item) => new(item.ItemGame, item.ItemName, item.Flags);
}