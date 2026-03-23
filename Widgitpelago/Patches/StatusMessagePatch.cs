using Assets.Behaviour.UI;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Widgitpelago.Patches;

public static class StatusMessagePatch
{
    [HarmonyPatch(typeof(UIStatusMessage), "Show"), HarmonyPrefix]
    public static bool Show(string text, string iconName, bool persistent) => false;

    [HarmonyPatch(typeof(UIStatusMessage), "Show"), HarmonyPrefix]
    public static bool Show(
        string text, Sprite icon, ref bool persistent, UIStatusMessage ___MessagePrefab, RectTransform ___MessageContainer
    )
    {
        persistent = false;
        try
        {
            if (!(bool)___MessageContainer) return false;
            var uiStatusMessage = Object.Instantiate(___MessagePrefab, ___MessageContainer);
            uiStatusMessage.StatusText.text = text;
            uiStatusMessage.StatusIcon.sprite = icon;
            uiStatusMessage.SetPersistent(persistent);
            var y = 0;
            while (true)
            {
                var flag = ___MessageContainer
                          .Cast<RectTransform>()
                          .All(rectTransform => Mathf.RoundToInt(rectTransform.anchoredPosition.y) != y);
                if (!flag) y += 48 /*0x30*/;
                else break;
            }
            ((RectTransform)uiStatusMessage.transform).anchoredPosition = new Vector2(0.0f, y);
        }
        catch (Exception e) { Core.Log.Error(e); }
        return false;
    }
}