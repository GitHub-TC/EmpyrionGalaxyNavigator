using EmpyrionNetAPIDefinitions;
using EmpyrionNetAPITools;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

namespace EmpyrionGalaxyNavigator
{
    public class Const
    {
        public const int SectorsPerLY = 100000;
    }

    [Serializable]
    public class NavPoint
    {
        public string Name { get; set; }
        public double Distance { get; set; }
        public Vector3 Coordinates { get; set; }
        public int PlayfieldId { get; set; }
        public string NavPointInfo => ToString();

        public override string ToString() => $"{Name} [{Distance / Const.SectorsPerLY:0} LY]";

    }

    [Serializable]
    public class PlayerTarget
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CurrentLocation { get; set; }
        public string NextLocation { get; set; }
        public string Target { get; set; }
        public string Alias { get; set; }
        public DateTime LastMessage { get; set; }
        public List<NavPoint> Route { get; set; }
    }

    [Serializable]
    public class AliasName
    {
        public string PlayfieldName { get; set; }
        public string Alias { get; set; }
    }

    [Serializable]
    public class PlayerSettings
    {
        public int PlayerId { get; set; }
        public string Name { get; set; }
        public int Distance { get; set; }
        public bool HideMessages { get; set; }
    }

    [Serializable]
    public class Configuration
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LogLevel { get; set; } = LogLevel.Message;
        public string ChatCommandPrefix { get; set; } = "/\\";
        public int MessageLoopMS { get; set; } = 10000;
        public int GalaxyAutoUpdateMinutes { get; set; } = 10;
        public List<AliasName> Aliases { get; set; } = new List<AliasName>();
        public List<PlayerSettings> Player { get; set; } = new List<PlayerSettings>();
        public ConcurrentDictionary<string, PlayerTarget> NavigationTargets { get; set; } = new ConcurrentDictionary<string, PlayerTarget>();
    }

}
