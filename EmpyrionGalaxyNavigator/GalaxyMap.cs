using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace EmpyrionGalaxyNavigator
{
    public class GalaxyMap
    {
        public SaveGameDBAccess DbAccess { get; set; }
        public Stopwatch GalaxyReadTime { get; private set; }
        public DateTime LastUpdateTime { get; set; } = DateTime.MinValue;
        public int GalaxyAutoUpdateMinutes { get; set; }
        public Map SolarSystemNavMap { get; set; } = new Map();
        public IDictionary<string, Map> SectorNavMap { get; set; } = new Dictionary<string, Map>();
        public IDictionary<string, string> PlayfieldInSolarSystem { get; set; } = new Dictionary<string, string>();
        public IDictionary<string, string> NameMapping { get; set; } = new Dictionary<string, string>();

        public bool Exists(string name) => NameMapping.ContainsKey(name.ToLowerInvariant().Trim()) || NameMapping.ContainsKey(name.ToLowerInvariant().Replace(" ", ""));
        public string RealName(string name) =>
            NameMapping.TryGetValue(name.ToLowerInvariant().Trim(), out var realPlainName)
            ? realPlainName
            : NameMapping.TryGetValue(name.ToLowerInvariant().Replace(" ", ""), out var realName) ? realName : name;

        public void ReadDbData(string path)
        {
            DbAccess = new SaveGameDBAccess(path);
            ForceUpdateFromDb();
        }

        public void ForceUpdateFromDb()
        {
            LastUpdateTime = DateTime.MinValue;
            UpdateFromDb();
        }

        public void UpdateFromDb()
        {
            if ((DateTime.Now - LastUpdateTime).TotalMinutes < GalaxyAutoUpdateMinutes) return;

            LastUpdateTime = DateTime.Now;
            GalaxyReadTime = Stopwatch.StartNew();

            SolarSystemNavMap = DbAccess.GetSolarSystems();
            var sectorSystems = DbAccess.GetSectorSystems();

            foreach (var item in SolarSystemNavMap.Nodes.Values)
            {
                if (sectorSystems.Nodes.TryGetValue(item.Name, out var pfNode)) item.PlayfieldId = pfNode.PlayfieldId;
            }

            var solarSystemsById = SolarSystemNavMap.Nodes.ToDictionary(N => N.Value.SolarSystemId, N => N.Key);

            PlayfieldInSolarSystem = sectorSystems.Nodes.ToDictionary(N => N.Key, N => solarSystemsById[N.Value.SolarSystemId]);
            SectorNavMap = sectorSystems.Nodes.Values
                .GroupBy(N => N.SolarSystemId)
                .ToDictionary(G => solarSystemsById[G.Key], G => new Map() { Nodes = G.ToDictionary(N => N.Name, N => N) });

            NameMapping = PlayfieldInSolarSystem.Keys.ToDictionary(N => N.ToLowerInvariant(), N => N)
                .Merge(SolarSystemNavMap.Nodes.Keys.ToDictionary(N => N.ToLowerInvariant(), N => N));

            GalaxyReadTime.Stop();
        }

        public List<NavPoint> Navigate(string startLocation, string destLocation, double maxTravelDistance)
        {
            var currentLocation = RealName(startLocation);
            var destination     = RealName(destLocation);

            var currentSolarSystem     = GetSolarSystem(currentLocation);
            var destinationSolarSystem = GetSolarSystem(destination);

            List<NavPoint> navPoints = new List<NavPoint>();

            if(currentSolarSystem != destinationSolarSystem)
            {
                navPoints.AddRange(CreateRoute(new SearchEngine(SolarSystemNavMap)
                {
                    Start = currentSolarSystem,
                    End   = destinationSolarSystem
                }.GetShortestPathAstart(maxTravelDistance))
                .Skip(1));
            }

            if(destinationSolarSystem.Name != destination && SectorNavMap.TryGetValue(destinationSolarSystem.Name, out var sectorsMap))
            {
                navPoints.AddRange(CreateRoute(new SearchEngine(sectorsMap)
                {
                    Start = GetSunNode(sectorsMap, destinationSolarSystem.Name),
                    End   = sectorsMap.Nodes[destination]
                }.GetShortestPathAstart(double.MaxValue)
                .Skip(1)));
            }

            return navPoints;
        }

        private Node GetSunNode(Map sectorsMap, string sectorName)
            => sectorsMap.Nodes.FirstOrDefault(n => n.Key.StartsWith($"{sectorName} [Sun")).Value;

        private Node GetSolarSystem(string name) 
            => SolarSystemNavMap.Nodes.TryGetValue(PlayfieldInSolarSystem.TryGetValue(name, out var solarSystem) ? solarSystem : name, out var nodeSolarSystem)
                ? nodeSolarSystem
                : null;

        private List<NavPoint> CreateRoute(IEnumerable<Node> list)
        {
            Node lastNode = null;

            return list
                .Select(N =>
                {
                    var n = new NavPoint() { 
                        Name        = N.Name, 
                        PlayfieldId = N.PlayfieldId, 
                        Distance    = lastNode == null ? 0 : lastNode.StraightLineDistanceTo(N), 
                        Coordinates = N.Coordinates 
                    };
                    lastNode = N;
                    return n;
                })
                .ToList();
        }

    }
}