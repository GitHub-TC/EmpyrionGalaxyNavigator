using System.Collections.Generic;
using System.Numerics;

namespace EmpyrionGalaxyNavigator
{
    public class Map
    {
        public Dictionary<string, Node> Nodes { get; set; } = new Dictionary<string, Node>();
    }

    public class Node
    {
        public int SolarSystemId { get; set; }
        public string Name { get; set; }
        public Vector3 Coordinates { get; set; }
        public int PlayfieldId { get; set; }

        public double StraightLineDistanceTo(Node end) => Vector3.Distance(Coordinates, end.Coordinates);
        public override string ToString() => $"{Name} [{Coordinates.X},{Coordinates.Y},{Coordinates.Z}]";
    }

}
