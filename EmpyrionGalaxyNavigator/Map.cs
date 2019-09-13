using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace EmpyrionGalaxyNavigator
{
    public class Map
    {
        public List<Node> Nodes { get; set; } = new List<Node>();
    }

    public class Node
    {
        public string Name { get; set; }
        public Vector3 Point { get; set; }
        public List<Edge> Connections { get; set; } = new List<Edge>();

        public double StraightLineDistanceTo(Node end)
        {
            return Vector3.Distance(Point, end.Point);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Edge
    {
        public double Cost { get; set; }
        public Node ConnectedNode { get; set; }

        public override string ToString()
        {
            return "-> " + ConnectedNode.ToString();
        }
    }

}
