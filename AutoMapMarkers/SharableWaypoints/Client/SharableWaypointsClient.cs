using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AutoMapMarkers.SharableWaypoints.Client;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class SharableWaypointsClient : Common.SharableWaypoints
{
    public SharableWaypointsClient(ModSystem mod) : base(mod.Mod.Info.ModID)
    {
        Harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnDataFromServer", Flags),
            prefix: typeof(SharableWaypointsClient).GetMethod("PreOnDataFromServer"));

        Harmony.Patch(typeof(GuiDialogAddWayPoint).GetMethod("autoSuggestName", Flags),
            prefix: typeof(SharableWaypointsClient).GetMethod("PreAutoSuggestName"));
        Harmony.Patch(typeof(GuiDialogAddWayPoint).GetMethod("onSave", Flags),
            postfix: typeof(SharableWaypointsClient).GetMethod("PostOnAddSave"));

        Harmony.Patch(typeof(GuiDialogEditWayPoint).GetMethod("TryOpen", Flags, Array.Empty<Type>()),
            prefix: typeof(SharableWaypointsClient).GetMethod("PreEditTryOpen"));

        Harmony.Patch(typeof(GuiDialogEditWayPoint).GetMethod("onDelete", Flags),
            prefix: typeof(SharableWaypointsClient).GetMethod("PreOnEditDelete"));
        Harmony.Patch(typeof(GuiDialogEditWayPoint).GetMethod("onSave", Flags),
            prefix: typeof(SharableWaypointsClient).GetMethod("PreOnEditSave"));
    }

    public static bool PreOnDataFromServer(WaypointMapLayer __instance, byte[] data, ref ICoreAPI ___api, ref List<MapComponent> ___tmpWayPointComponents)
    {
        __instance.ownWaypoints.Clear();
        ___tmpWayPointComponents.Clear();

        string uuid = ((ICoreClientAPI)___api).World.Player.PlayerUID;

        var wp = SerializerUtil.Deserialize<List<Waypoint>>(data);

        foreach (Waypoint waypoint in SerializerUtil.Deserialize<List<Waypoint>>(data))
        {
            if (waypoint.OwningPlayerUid == uuid)
            {
                __instance.ownWaypoints.Add(waypoint);
            }
            else
            {
                __instance.AddTemporaryWaypoint(waypoint);
            }
        }

        __instance.GetType().GetMethod("RebuildMapComponents", Flags)?.Invoke(__instance, null);

        return false;
    }

    public static bool PreAutoSuggestName(GuiDialogAddWayPoint __instance, ref string ___curIcon, ref string ___curColor, ref bool ___ignoreNextAutosuggestDisable)
    {
        string savedName = Settings.GetWaypointName($"{___curIcon}-{___curColor}");
        if (string.IsNullOrEmpty(savedName))
        {
            return true;
        }

        GuiElementTextInput textInput = __instance.SingleComposer.GetTextInput("nameInput");
        ___ignoreNextAutosuggestDisable = true;
        textInput.SetValue(savedName);
        return false;
    }

    public static void PostOnAddSave(GuiDialogAddWayPoint __instance, ref string ___curIcon, ref string ___curColor)
    {
        string curName = __instance.SingleComposer.GetTextInput("nameInput").GetText();
        Settings.SetWaypointName($"{___curIcon}-{___curColor}", curName);
    }

    //public static void PostOnAddSaveTitled(string currentTitle, string currentIcon, string currentColor)
    //{
    //    Settings.SetWaypointName($"{currentIcon}-{currentColor}", currentTitle);
    //}

    public static bool PreEditTryOpen(GuiDialogEditWayPoint __instance, ref bool __result, ref ICoreClientAPI ___capi, ref Waypoint ___waypoint)
    {
        if (___waypoint.OwningPlayerUid == ___capi.World.Player.PlayerUID)
        {
            return true;
        }

        ___capi.ShowChatMessage(Lang.Get("automapmarkers:cannot-edit"));
        ___capi.Event.RegisterCallback(_ => { __instance.TryClose(); }, 1);

        __result = false;
        return false;
    }

    public static bool PreOnEditDelete(ref bool __result, ref ICoreClientAPI ___capi, ref Waypoint ___waypoint)
    {
        if (___waypoint.OwningPlayerUid == ___capi.World.Player.PlayerUID)
        {
            return true;
        }

        ___capi.ShowChatMessage(Lang.Get("automapmarkers:cannot-delete"));

        __result = true;
        return false;
    }

    public static bool PreOnEditSave(ref bool __result, ref ICoreClientAPI ___capi, ref Waypoint ___waypoint)
    {
        if (___waypoint.OwningPlayerUid == ___capi.World.Player.PlayerUID)
        {
            return true;
        }

        ___capi.ShowChatMessage(Lang.Get("automapmarkers:cannot-save"));

        __result = true;
        return false;
    }
}
