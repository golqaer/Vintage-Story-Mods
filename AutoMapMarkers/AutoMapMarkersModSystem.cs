using AutoMapMarkers.BlockBehavior;
using AutoMapMarkers.EntityBehavior;
using AutoMapMarkers.Events;
using AutoMapMarkers.Network;
using AutoMapMarkers.Patches;
using AutoMapMarkers.Settings;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using AutoMapMarkers.SharableWaypoints.Client;
using AutoMapMarkers.SharableWaypoints.Server;

namespace AutoMapMarkers
{
    public class AutoMapMarkersModSystem : ModSystem
    {
        public static string SavegameIdentifier => CoreAPI!.World.SavegameIdentifier;

        private SharableWaypointsClient? _client;
        private SharableWaypointsServer? _server;

        public static ICoreAPI CoreAPI;
        public static ICoreServerAPI CoreServerAPI;
        public static ICoreClientAPI CoreClientAPI;
        public static MapMarkerNetwork Network;

        public override bool ShouldLoad(EnumAppSide side)
        {
            return true;
        }

        /// <summary>
        /// Server/client shared initialization
        /// </summary>
        public override void Start(ICoreAPI api)
        {
            CoreAPI = api;
            CoreAPI.RegisterEntityBehaviorClass("egocarib_TraderMarkerBehavior", typeof(TraderMarkerBehavior));
            CoreAPI.RegisterBlockBehaviorClass("egocarib_HarvestMarkerBehavior", typeof(HarvestMarkerBehavior));
            //CoreAPI.RegisterBlockBehaviorClass("egocarib_MushroomMarkerBehavior", typeof(MushroomMarkerBehavior));
            CoreAPI.RegisterBlockBehaviorClass("egocarib_LooseOreMarkerBehavior", typeof(LooseOresMarkerBehavior));
            HarmonyAgent.Harmonize();
        }

        /// <summary>
        /// Server-specific intialization
        /// </summary>
        public override void StartServerSide(ICoreServerAPI api)
        {
            CoreServerAPI = api;
            _server = new SharableWaypointsServer(this);
            MapMarkerConfig.GetSettings(api, true); //Ensure config file is generated at startup if one does not exist yet.
            Network = new MapMarkerNetwork(CoreServerAPI);
        }

        /// <summary>
        /// Client-specific initialization
        /// </summary>
        public override void StartClientSide(ICoreClientAPI api)
        {
            CoreClientAPI = api;
            _client = new SharableWaypointsClient(this);
            Network = new MapMarkerNetwork(CoreClientAPI);
            CoreClientAPI.Input.InWorldAction += SneakHandler.HandlePlayerSneak;

            //api.Event.KeyDown += (KeyEvent ev) => ClientKeyInputProxyHandler(ev, inputIndex, KeyEventType.KEY_DOWN);

            ////public delegate void OnEntityAction(EnumEntityAction action, bool on, ref EnumHandling handled);
            //OnEntityAction test = (EnumEntityAction action, bool on, ref EnumHandling handled) => { };
            //api.Input.InWorldAction += (EnumEntityAction action, bool on, ref EnumHandling handled) =>
            //{
            //    //Do thing when player crouches
            //    IPlayer.CurrentBlockSelection // <-- do thing to this block
            //};
        }

        /// <summary>
        /// Unapplies Harmony patches and disposes of all static variables in the ModSystem.
        /// </summary>
        public override void Dispose()
        {
            HarmonyAgent.Deharmonize();
            if (CoreClientAPI != null)
            {
                if (CoreClientAPI.Input != null)
                    CoreClientAPI.Input.InWorldAction -= SneakHandler.HandlePlayerSneak;
                CoreClientAPI = null;
            }
            CoreAPI = null;
            CoreServerAPI = null;
            Network?.Dispose();
            Network = null;

            _client?.Dispose();
            _client = null;

            _server?.Dispose();
            _server = null;
        }
    }
}
