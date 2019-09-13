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
            var connection = node.Connections.SingleOrDefault(x => x.ConnectedNode == routeNode.NearestToStart);
            ShortestPathCost += connection == null ? 0 : connection.Cost;
            BuildShortestPath(list, routeNode.NearestToStart);
        }

        public List<Node> GetShortestPathAstart()
        {
            RouteData = Map.Nodes.ToDictionary(N => N, N => new NodeRouteData() { StraightLineDistanceToEnd = N.StraightLineDistanceTo(End) });
            AstarSearch();
            var shortestPath = new List<Node>();
            shortestPath.Add(End);
            BuildShortestPath(shortestPath, End);
            shortestPath.Reverse();
            return shortestPath;
        }

        private void AstarSearch()
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

                foreach (var cnn in node.Connections.OrderBy(x => x.Cost))
                {
                    var childNode = cnn.ConnectedNode;
                    var childRouteNode = RouteData[childNode];
                    if (childRouteNode.Visited)
                        continue;
                    if (childRouteNode.MinCostToStart == null ||
                        routeNode.MinCostToStart + cnn.Cost < childRouteNode.MinCostToStart)
                    {
                        childRouteNode.MinCostToStart = routeNode.MinCostToStart + cnn.Cost;
                        childRouteNode.NearestToStart = node;
                        if (!prioQueue.Contains(childNode))
                            prioQueue.Add(childNode);
                    }
                }
                routeNode.Visited = true;
                if (node == End)
                    return;
            } while (prioQueue.Any());
        }
    }
}
