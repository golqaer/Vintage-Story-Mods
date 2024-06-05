using AutoMapMarkers.Settings;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AutoMapMarkers.SharableWaypoints.Server;

public class SharableWaypointsServer : Common.SharableWaypoints
{
    private static readonly Dictionary<string, int> GroupCache = new();

    public SharableWaypointsServer(ModSystem mod) : base(mod.Mod.Info.ModID)
    {
        MethodInfo preOnCmdWaypoint = typeof(SharableWaypointsServer).GetMethod("PreOnCmdWayPoint");
        MethodInfo postOnCmdWaypoint = typeof(SharableWaypointsServer).GetMethod("PostOnCmdWayPoint");

        Harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnCmdWayPointAdd", Flags, new[] { typeof(TextCommandCallingArgs) }),
            prefix: preOnCmdWaypoint, postfix: postOnCmdWaypoint);
        Harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnCmdWayPointAddp", Flags, new[] { typeof(TextCommandCallingArgs) }),
            prefix: preOnCmdWaypoint, postfix: postOnCmdWaypoint);
        Harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnCmdWayPointAddat", Flags, new[] { typeof(TextCommandCallingArgs) }),
            prefix: preOnCmdWaypoint, postfix: postOnCmdWaypoint);
        Harmony.Patch(typeof(WaypointMapLayer).GetMethod("OnCmdWayPointAddati", Flags, new[] { typeof(TextCommandCallingArgs) }),
            prefix: preOnCmdWaypoint, postfix: postOnCmdWaypoint);

        Harmony.Patch(typeof(WaypointMapLayer).GetMethod("AddWaypoint", Flags, new[] { typeof(Waypoint), typeof(IServerPlayer) }),
            postfix: typeof(SharableWaypointsServer).GetMethod("PostAddWaypoint"));

        AutoMapMarkersModSystem.ServerSharableNtChannel.
            SetMessageHandler<MapMarkerConfig.Settings>(OnReceiveSettings);
            ;
    }

    private void OnReceiveSettings(IServerPlayer fromPlayer, MapMarkerConfig.Settings response)
    {
        GroupCache.Add(fromPlayer.PlayerUID, response.ShareWaypointGroupId);   
    }

    public static void PreOnCmdWayPoint(TextCommandCallingArgs args)
    {
        //GroupCache.Add(args.Caller.Player.PlayerUID, args.Caller.FromChatGroupId);
    }
    public static void AddToDict(string playerUID, int groupId)
    {
        GroupCache.Add(playerUID, groupId);
    }

    public static void PostOnCmdWayPoint(TextCommandCallingArgs args)
    {
        GroupCache.Remove(args.Caller.Player.PlayerUID);
    }
    public static void RemoveFromDict(string playerUID)
    {
        GroupCache.Remove(playerUID);
    }

    public static void PostAddWaypoint(Waypoint waypoint, IServerPlayer player)
    {
        waypoint.OwningPlayerGroupId = GroupCache.GetValueOrDefault(player.PlayerUID, waypoint.OwningPlayerGroupId);
    }


}
