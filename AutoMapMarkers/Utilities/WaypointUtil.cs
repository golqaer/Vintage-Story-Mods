using AutoMapMarkers.Settings;
using AutoMapMarkers.SharableWaypoints.Client;
using AutoMapMarkers.SharableWaypoints.Server;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AutoMapMarkers.Utilities
{
    public class WaypointUtil
    {
        private readonly MapMarkerConfig.Settings _settings;
        private readonly WaypointMapLayer WaypointMapLayer;
        private readonly IServerPlayer ServerPlayer;
        private static readonly MethodInfo ResendWaypointsMethod =
            typeof(WaypointMapLayer).GetMethod("ResendWaypoints", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo RebuildMapComponentsMethod =
            typeof(WaypointMapLayer).GetMethod("RebuildMapComponents", BindingFlags.NonPublic | BindingFlags.Instance);

        private bool Valid => ServerPlayer != null && WaypointMapLayer != null
            && ResendWaypointsMethod != null && RebuildMapComponentsMethod != null;

        private string PlayerInfo => ServerPlayer != null ? $"{ServerPlayer.PlayerName} [{ServerPlayer.PlayerUID}]" : "NULL";

        /// <summary>
        /// Waypoint generator. To be instantiated and used only on the Server thread.
        /// </summary>
        public WaypointUtil(IServerPlayer serverPlayer, MapMarkerConfig.Settings settings)
        {
            _settings = settings;
            ServerPlayer = serverPlayer;
            WorldMapManager serverWorldMapManager = AutoMapMarkersModSystem.CoreServerAPI.ModLoader.GetModSystem<WorldMapManager>();
            WaypointMapLayer = serverWorldMapManager.MapLayers.FirstOrDefault((ml) => ml is WaypointMapLayer) as WaypointMapLayer;
        }

        /// <summary>
        /// Parses the provided map marker settings and determines whether a new waypoint should be added
        /// at the specified coordinates based on those settings. Then creates the waypoint and syncs it
        /// back to the client.
        /// </summary>
        public void AddWaypoint(Vec3d position, MapMarkerConfig.Settings.AutoMapMarkerSetting settings, bool sendChatMessageToPlayer, int groupId, string dynamicTitleComponent)
        {
            if (!Valid)
            {
                MessageUtil.LogError($"Unable to fulfill waypoint create request from player {PlayerInfo} - ServerPlayer, WaypointMapLayer, or Reflected method is inaccessible.");
                return;
            }
            if (position == null || settings == null)
            {
                MessageUtil.LogError($"Unable to fulfill waypoint create request from player {PlayerInfo} - missing position or settings data.");
                return;
            }
            if (!settings.Enabled)
            {
                return;
            }

            // If there's a dynamic component to the marker title, figure that out before creating the waypoint
            string title = $"{settings.MarkerTitle}{(dynamicTitleComponent != null ? $" ({dynamicTitleComponent})" : "")}";

            foreach (Waypoint waypoint in WaypointMapLayer.Waypoints.Where(w => w.OwningPlayerUid == ServerPlayer.PlayerUID))
            {
                double xDiff = Math.Abs(waypoint.Position.X - position.X);
                double zDiff = Math.Abs(waypoint.Position.Z - position.Z);
                if (Math.Max(xDiff, zDiff) < settings.MarkerCoverageRadius)
                {
                    bool sameTitle = waypoint.Title == title;
                    bool sameIcon = waypoint.Icon == settings.MarkerIcon;
                    if (sameTitle && sameIcon)
                    {
                        int? settingColor = settings.MarkerColorInteger;
                        if (settingColor == null || waypoint.Color == settingColor)
                        {
                            return; // Don't create another waypoint because this spot is too close to an existing waypoint
                        }
                    }
                }
            }

            AddWaypointToMap(position, title, settings, sendChatMessageToPlayer, groupId);
        }

        /// <summary>
        /// Adds a waypoint marker to the map at the specified position, using the specified parameters, and syncs it back to the client.
        /// </summary>
        private void AddWaypointToMap(Vec3d pos, string title, MapMarkerConfig.Settings.AutoMapMarkerSetting settings, bool sendChatMessageToPlayer, int groupId, bool pinned = false)
        {
            if (!Valid)
            {
                return;
            }
            if (pos == null || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(settings.MarkerIcon))
            {
                MessageUtil.LogError("Unable to create map marker - missing position, title, or icon.");
                return;
            }
            if (settings.MarkerColorInteger == null)
            {
                MessageUtil.LogError("Unable to create map marker - invalid color.");
                return;
            }

            Waypoint waypoint = new Waypoint()
            {
                Color = (int)settings.MarkerColorInteger,
                OwningPlayerUid = ServerPlayer.PlayerUID,
                OwningPlayerGroupId = groupId,
                Position = pos,
                Title = title,
                Icon = settings.MarkerIcon,
                Pinned = pinned
            };

            WaypointMapLayer.Waypoints.Add(waypoint);

            var playersToSend = AutoMapMarkersModSystem.CoreServerAPI.Server.Players
                .Where(p => p.Groups.Any(g => g.GroupUid == groupId))
                .ToList()
                ;

            if (!playersToSend.Any(p => p.PlayerUID == ServerPlayer.PlayerUID))
                playersToSend.Add(ServerPlayer);

            if (sendChatMessageToPlayer)
            {
                int waypointIndex = WaypointMapLayer.Waypoints.Count(p => p.OwningPlayerUid == ServerPlayer.PlayerUID) - 1;
                MessageUtil.Chat(Lang.Get("Ok, waypoint nr. {0} added", waypointIndex), ServerPlayer);
            }

            playersToSend.ForEach(p => ResendWaypoints(p));
        }

        /// <summary>
        /// Finds the waypoint nearest to the player's current location. Then deletes the waypoint and syncs data back to the client.
        /// </summary>
        public void DeleteNearestWaypoint(bool sendChatMessageToPlayer)
        {
            if (!Valid)
            {
                MessageUtil.LogError($"Unable to fulfill map marker delete request from player {PlayerInfo} - ServerPlayer, WaypointMapLayer, or Reflected method is inaccessible.");
                return;
            }

            double closestWpDistance = double.MaxValue;
            Waypoint closestWp = null;

            foreach (Waypoint wp in WaypointMapLayer.Waypoints.Where(w => w.OwningPlayerUid == ServerPlayer.PlayerUID))
            {
                double thisDist = ServerPlayer.Entity.Pos.DistanceTo(wp.Position);
                if (thisDist < closestWpDistance)
                {
                    closestWpDistance = thisDist;
                    closestWp = wp;
                }
            }
            if (closestWp != null)
            {
                WaypointMapLayer.Waypoints.Remove(closestWp);
                RebuildMapComponents();
                ResendWaypoints(ServerPlayer);
                if (sendChatMessageToPlayer)
                {
                    MessageUtil.Chat(Lang.Get("Ok, deleted waypoint."), ServerPlayer);
                }
            }
            else
            {
                MessageUtil.Log($"Player {PlayerInfo} tried to delete their nearest waypoint, but no waypoints owned by that player were found.");
            }
        }

        /// <summary>
        /// Resends waypoints to the client(s).
        /// </summary>
        private void ResendWaypoints(IServerPlayer player)
        {
            if (Valid)
            {
                ResendWaypointsMethod.Invoke(WaypointMapLayer, new object[] { player as IServerPlayer });
            }
        }

        /// <summary>
        /// Rebuilds waypoint map components.
        /// </summary>
        private void RebuildMapComponents()
        {
            if (Valid)
            {
                RebuildMapComponentsMethod.Invoke(WaypointMapLayer, null);
            }
        }

    }
}
