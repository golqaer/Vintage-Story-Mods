using System.Reflection;
using HarmonyLib;

namespace AutoMapMarkers.SharableWaypoints.Common;

public abstract class SharableWaypoints
{
    protected const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly string _modId;

    protected Harmony Harmony { get; }

    protected SharableWaypoints(string modId)
    {
        _modId = modId;
        Harmony = new Harmony(modId);
    }

    public void Dispose()
    {
        Harmony.UnpatchAll(_modId);
    }
}
