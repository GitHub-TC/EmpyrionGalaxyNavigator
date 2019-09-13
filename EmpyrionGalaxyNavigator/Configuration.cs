﻿using EmpyrionNetAPIDefinitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EmpyrionGalaxyNavigator
{
    public class PlayerTarget
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CurrentLocation { get; set; }
        public string NextLocation { get; set; }
        public string Target { get; set; }
        public DateTime LastMessage { get; set; }
    }

    public class Configuration
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LogLevel { get; set; } = LogLevel.Message;
        public string ChatCommandPrefix { get; set; } = "/\\";
        public int MessageLoopMS { get; set; } = 10000;
        public ConcurrentDictionary<string, PlayerTarget> NavigationTargets { get; set; } = new ConcurrentDictionary<string, PlayerTarget>();
    }
}
