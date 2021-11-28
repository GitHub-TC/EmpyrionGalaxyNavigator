using System.Collections.Generic;
using System.Linq;

namespace EmpyrionGalaxyNavigator
{
    public class SearchEngine
    {
        public class NodeRouteData
        {
            public double? MinCostToStart { get; set; }
            public Node NearestToStart { get; set; }
            public bool Visited { get; set; }
            public double StraightLineDistanceToEnd { get; set; }
        }

        public Map Map { get; set; }
        public Node Start { get; set; }
        public Node End { get; set; }
        public int NodeVisits { get; private set; }
        public double ShortestPathCost { get; set; }
        public Dictionary<Node, NodeRouteData> RouteData { get; set; } = new Dictionary<Node, NodeRouteData>();

        public SearchEngine(Map map)
        {
            Map = map;
        }

        private void BuildShortestPath(List<Node> list, Node node)
        {
            var routeNode = RouteData[node];
            if (routeNode.NearestToStart == null) return;

            list.Add(routeNode.NearestToStart);
            ShortestPathCost += routeNode.NearestToStart.StraightLineDistanceTo(node);
            BuildShortestPath(list, routeNode.NearestToStart);
        }

        public List<Node> GetShortestPathAstart(double maxTravelDistance)
        {
            if (Start.StraightLineDistanceTo(End) <= maxTravelDistance) return new List<Node>() { Start, End };

            RouteData = Map.Nodes.ToDictionary(N => N.Value, N => new NodeRouteData() { StraightLineDistanceToEnd = N.Value.StraightLineDistanceTo(End) });
            AstarSearch(maxTravelDistance);
            var shortestPath = new List<Node>{ End };
            BuildShortestPath(shortestPath, End);
            shortestPath.Reverse();
            return shortestPath;
        }

        private void AstarSearch(double maxTravelDistance)
        {
            NodeVisits = 0;
            RouteData[Start].MinCostToStart = 0;
            var prioQueue = new List<Node>{ Start };

            do {
                prioQueue = prioQueue.OrderBy(x => RouteData[x].MinCostToStart + RouteData[x].StraightLineDistanceToEnd).ToList();
                var node = prioQueue.First();
                prioQueue.Remove(node);
                NodeVisits++;
                var routeNode = RouteData[node];

                foreach (var childNode in Map.Nodes.Values.Where(n => n.StraightLineDistanceTo(node) <= maxTravelDistance))
                {
                    var childRouteNode = RouteData[childNode];
                    if (childRouteNode.Visited) continue;

                    if (childRouteNode.MinCostToStart == null || routeNode.MinCostToStart + childNode.StraightLineDistanceTo(node) < childRouteNode.MinCostToStart)
                    {
                        childRouteNode.MinCostToStart = routeNode.MinCostToStart + childNode.StraightLineDistanceTo(node);
                        childRouteNode.NearestToStart = node;
                        if (!prioQueue.Contains(childNode)) prioQueue.Add(childNode);
                    }
                }
                routeNode.Visited = true;
                if (node == End) return;
            } while (prioQueue.Any());
        }
    }
}
