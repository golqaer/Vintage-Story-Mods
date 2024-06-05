using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AutoMapMarkers.SharableWaypoints.Client;

[SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Settings
{
    [YamlMember(Order = 0, ScalarStyle = ScalarStyle.DoubleQuoted)]
    public Dictionary<string, string> StoredWaypointData = new();

    private static Settings _instance;
    private static string _filename;

    public static string GetWaypointName(string index)
    {
        return (_instance ??= Read()).StoredWaypointData.Get(index);
    }

    public static void SetWaypointName(string index, string name)
    {
        (_instance ??= Read()).StoredWaypointData[index] = name;
        Write();
    }

    private static Settings Read()
    {
        _filename = Path.Combine(GamePaths.DataPath, "ModData", AutoMapMarkersModSystem.SavegameIdentifier, "sharablewaypoints.yml");
        try
        {
            return new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build().Deserialize<Settings>(File.ReadAllText(_filename));
        }
        catch (Exception)
        {
            return new Settings();
        }
    }

    private static void Write()
    {
        if (_instance == null)
        {
            return;
        }

        FileInfo fileInfo = new(_filename!);
        if (fileInfo is { Exists: false, Directory: not null })
        {
            GamePaths.EnsurePathExists(fileInfo.Directory!.FullName);
        }

        var txt = new SerializerBuilder()
                .WithQuotingNecessaryStrings()
                .WithNamingConvention(NullNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build().Serialize(_instance);

        File.WriteAllText(_filename!, new SerializerBuilder()
                .WithQuotingNecessaryStrings()
                .WithNamingConvention(NullNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build().Serialize(_instance)
            , Encoding.UTF8);
    }
}
