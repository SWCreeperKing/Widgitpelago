using Archipelago.MultiClient.Net.Models;
using Assets.Behaviour.UI;
using Widgitpelago.Patches;

namespace Widgitpelago.Archipelago;

public static class ItemHandler
{
    public static List<string> FramesHave = [];
    public static int TiersHave = 0;
    public static long TotalItemsReceived = 0;
    public static long LocalItemsReceived = 0;

    public static void HandleItem(ItemInfo item)
    {
        LocalItemsReceived++;
        if (item.ItemName is "Motivational Poster") return;

        if (LocalItemsReceived > TotalItemsReceived)
        {
            TotalItemsReceived = LocalItemsReceived;
            WidgetClient.Client.SendToStorage("item_on", TotalItemsReceived);
            UIStatusMessage.Show(
                $"Received {item.ItemName} from {item.Player.Alias}", CustomAssets.ItemImage(item), false
            );
        }

        if (item.ItemName is "Progressive Tier")
        {
            TiersHave++;
            GameDetectionPatches.SetTier(TiersHave);
            Core.Log.Msg($"Setting Tier: [{TiersHave}]");
            AddFrame($"Tier {TiersHave}");
            return;
        }

        if (!WidgetClient.FrameIdMap.ContainsKey(item.ItemName)) return;
        Core.Log.Msg($"Adding Tech: [{item.ItemName}]");
        AddFrame(item.ItemName);
    }

    public static void AddFrame(string frame)
    {
        if (!WidgetClient.FrameIdMap.ContainsKey(frame)) return;
        FramesHave.Add(frame);
        GameDetectionPatches.AddTech(WidgetClient.FrameIdMap[frame]);
        TechTreeUI.Instance.UpdateNodes();
    }
}