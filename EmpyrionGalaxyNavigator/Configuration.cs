using EmpyrionNetAPIDefinitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EmpyrionGalaxyNavigator
{
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
    }

    [Serializable]
    public class AliasName
    {
        public string PlayfieldName { get; set; }
        public string Alias { get; set; }
    }

    [Serializable]
    public class PlayerWarpDistance
    {
        public int PlayerId { get; set; }
        public string Name { get; set; }
        public int Distance { get; set; }
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
        public List<PlayerWarpDistance> Player { get; set; } = new List<PlayerWarpDistance>();
        public ConcurrentDictionary<string, PlayerTarget> NavigationTargets { get; set; } = new ConcurrentDictionary<string, PlayerTarget>();
    }
}
