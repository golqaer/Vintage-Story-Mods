﻿using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace AutoMapMarkers.Utilities
{
    public class MessageUtil
    {
        /// <summary>
        /// Send a chat message. If called from the Server thread, serverPlayer must be specified.
        /// </summary>
        public static void Chat(string message, IPlayer serverPlayer = null, EnumChatType chatTypeForServer = EnumChatType.Notification)
        {
            if (serverPlayer != null)
            {
                AutoMapMarkersModSystem.CoreServerAPI.SendMessage(serverPlayer, GlobalConstants.GeneralChatGroup, message, chatTypeForServer);
            }
            else if (AutoMapMarkersModSystem.CoreClientAPI != null)
            {
                AutoMapMarkersModSystem.CoreClientAPI.ShowChatMessage(message);
            }
            else
            {
                LogError("Unable to propagate chat message (\"{message}\")");
            }
        }

        /// <summary>
        /// Logs a message to the client or server logs, depending on side of calling thread.
        /// </summary>
        public static void Log(string message, EnumLogType type = EnumLogType.Notification)
        {
            AutoMapMarkersModSystem.CoreAPI.Logger.Log(type, $"[Auto Map Markers] {message}");
        }

        /// <summary>
        /// Logs an error to the client or server logs.
        /// </summary>
        public static void LogError(string message)
        {
            Log($"[Auto Map Markers] {message}", EnumLogType.Error);
        }
    }
}
