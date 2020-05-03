using System.Linq;
using System.Collections.Generic;
using System.IO;
using EmpyrionNetAPITools.Extensions;
using System.Numerics;
using System;

namespace EmpyrionGalaxyNavigator
{
    public class NavPoint
    {
        public string Name { get; set; }
        public double Distance { get; set; }
    }

    public class SectorData
    {
        public List<int> Coordinates { get; set; }
        public string Color { get; set; }
        public string Icon { get; set; }
        public bool OrbitLine { get; set; }
        public string SectorMapType { get; set; }
        public string ImageTemplateDir { get; set; }
        public List<List<string>> Playfields { get; set; }
        public List<string> Allow { get; set; }
        public List<string> Deny { get; set; }
    }

    public class SolarSystems
    {
        public string Name { get; set; }
        public List<int> Coordinates { get; set; }
        public string StarClass { get; set; }
        public List<SectorData> Sectors { get; set; }
    }

    public class SectorsData
    {
        public bool GalaxyMode { get; set; }
        public List<SectorData> Sectors { get; set; }
        public List<SolarSystems> SolarSystems { get; set; }
    }

    public class GalaxyMap
    {
        private const float MaxWarpDistance = 250;

        public List<SectorData> Sectors { get; set; }
        public Map GalaxyNavMap { get; private set; }
        public Dictionary<string, Node> GalaxyNodes { get; private set; }

        public bool Exists(string name)
        {
            return Sectors.Any(S => S.Playfields.Any(P => P[1] == name));
        }

        public void ReadSectors(string path)
        {
            Sectors = FlattenSectors(ReadSectorFiles(path));
            BuildGalaxyNavMap();
        }

        public void ReadSectorsData(string sectorsData)
        {
            Sectors = FlattenSectors(YamlExtensions.YamlToObject<SectorsData>(new StringReader(sectorsData)));
            BuildGalaxyNavMap();
        }

        public static SectorsData ReadSectorFiles(string path)
        {
            SectorsData result;
            using (var input = File.OpenText(Path.Combine(path, "Sectors.yaml")))
            {
                result = YamlExtensions.YamlToObject<SectorsData>(input);
            }

            if (result.GalaxyMode)
            {
                Directory.GetFiles(path, "Sectors*.yaml")
                    .Where(F => !Path.GetFileName(F).Equals("Sectors.yaml", StringComparison.InvariantCultureIgnoreCase))
                    .ToList()
                    .ForEach(F =>
                    {
                        using (var input = File.OpenText(F))
                        {
                            var AddSectorsData = YamlExtensions.YamlToObject<SectorsData>(input);
                            if (AddSectorsData.GalaxyMode) result.SolarSystems.AddRange(AddSectorsData.SolarSystems);
                        }
                    });
            }

            return result;
        }

        public static List<SectorData> FlattenSectors(SectorsData sectorsData)
        {
            if (sectorsData.SolarSystems == null) return sectorsData.Sectors;

            var sectors = sectorsData.Sectors?.ToList() ?? new List<SectorData>();

            sectorsData.SolarSystems.ForEach(U => {
                U.Sectors.ForEach(S => {
                    sectors.Add(new SectorData()
                    {
                        Coordinates         = new[] { S.Coordinates[0] + U.Coordinates[0], S.Coordinates[1] + U.Coordinates[1], S.Coordinates[2] + U.Coordinates[2] }.ToList(),
                        Color               = S.Color,
                        Icon                = S.Icon,
                        OrbitLine           = S.OrbitLine,
                        SectorMapType       = S.SectorMapType,
                        ImageTemplateDir    = S.ImageTemplateDir,
                        Playfields          = S.Playfields,
                        Allow               = S.Allow,
                        Deny                = S.Deny,
                    });
                });
            });

            return sectors;
        }

        private void BuildGalaxyNavMap()
        {
            GalaxyNavMap = new Map();

            var allOrbits = Sectors
                .Where(S => S.SectorMapType == null || (S.SectorMapType.ToLowerInvariant() != "none" && S.SectorMapType.ToLowerInvariant() != "warptarget"))
                .Select(S =>
                {
                    var orbit = S.Playfields.Last();
                    return new {
                        Name    = orbit[1],
                        Point   = new Vector3(S.Coordinates[0], S.Coordinates[1], S.Coordinates[2]),
                        Planets = S.Playfields.Take(S.Playfields.Count - 1).Select(P => P[1]),
                        S.Allow,
                        S.Deny,
                    };
                })
                .ToDictionary(O => O.Name, O => O);

            Sectors.ForEach(S => {
                var orbit = S.Playfields.Last();
                var orbitNode = new Node()
                {
                    Name  = orbit[1],
                    Point = new Vector3(S.Coordinates[0], S.Coordinates[1], S.Coordinates[2]),
                };
                GalaxyNavMap.Nodes.Add(orbitNode);

                S.Playfields.Take(S.Playfields.Count - 1).ToList()
                    .ForEach(P =>
                    {
                        var planetNode = new Node()
                        {
                            Name  = P[1],
                            Point = new Vector3(S.Coordinates[0], S.Coordinates[1], S.Coordinates[2]),
                        };
                        GalaxyNavMap.Nodes.Add(planetNode);
                        planetNode.Connections.Add(new Edge() { ConnectedNode = orbitNode,  Cost = 0 });
                        orbitNode .Connections.Add(new Edge() { ConnectedNode = planetNode, Cost = 0 });
                    });
            });

            GalaxyNodes = GalaxyNavMap.Nodes.ToDictionary(N => N.Name, N => N);

            GalaxyNavMap.Nodes.ForEach(N => {
                allOrbits.Where(T => T.Key != N.Name).ToList()
                    .ForEach(T => {
                        if (!allOrbits.TryGetValue(N.Name, out var O)) return;

                        var dist = Vector3.Distance(N.Point, T.Value.Point);
                        var warp = dist <= MaxWarpDistance;
                        if (O.Deny  != null && O.Deny .Contains(T.Key)) warp = false;
                        if (O.Allow != null && O.Allow.Contains(T.Key)) warp = true;

                        if(warp) N.Connections.Add(new Edge(){ ConnectedNode = GalaxyNodes[T.Key], Cost = dist});
                    });
            });

        }

        public List<NavPoint> Navigate(string currentLocation, string destination)
        {
            var search = new SearchEngine(GalaxyNavMap)
            {
                Start = GalaxyNodes[currentLocation],
                End   = GalaxyNodes[destination]
            };

            return CreateRoute(search.GetShortestPathAstart());
        }

        private List<NavPoint> CreateRoute(IEnumerable<Node> list)
        {
            Node lastNode = null;

            return list
                .Reverse()
                .Select(N =>
                {
                    var n = new NavPoint() { Name = N.Name, Distance = lastNode == null ? 0 : N.Connections.First(C => C.ConnectedNode == lastNode).Cost };
                    lastNode = N;
                    return n;
                })
                .Reverse()
                .ToList();
        }
    }
}