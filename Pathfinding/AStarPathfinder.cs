using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class AStarPathfinder
    {
        private readonly Dictionary<Vector2Int, PathNode> nodeCache;
        private readonly ObjectPool<PathNode> nodePool;

        public AStarPathfinder()
        {
            nodePool = new ObjectPool<PathNode>(
                () => new PathNode(),
                node => node.Reset()
            );
            nodeCache = new Dictionary<Vector2Int, PathNode>();
        }

        public Path FindPath(Vector2Int start, Vector2Int goal, Room room, ICostProvider costProvider)
        {
            if (room == null || costProvider == null)
                return new Path { IsValid = false };

            nodeCache.Clear();
            var openSet = new SortedSet<PathNode>(new PathNodeComparer());
            var closedSet = new HashSet<Vector2Int>();

            var startNode = GetOrCreateNode(start);
            startNode.GCost = 0;
            startNode.HCost = costProvider.GetHeuristicCost(start, goal);
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                var current = openSet.Min;
                openSet.Remove(current);

                if (current.Position == goal)
                    return ReconstructPath(current);

                closedSet.Add(current.Position);

                foreach (var neighbor in GetNeighbors(current, room))
                {
                    if (closedSet.Contains(neighbor.Position))
                        continue;

                    var tile = room.GetTile(neighbor.Position.x, neighbor.Position.y);
                    if (costProvider.ShouldAvoidTile(tile, null))
                        continue;

                    var tentativeGCost = current.GCost + costProvider.GetMovementCost(tile, null);

                    var neighborNode = GetOrCreateNode(neighbor.Position);

                    if (tentativeGCost < neighborNode.GCost)
                    {
                        neighborNode.Parent = current;
                        neighborNode.GCost = tentativeGCost;
                        neighborNode.HCost = costProvider.GetHeuristicCost(neighbor.Position, goal);

                        if (!openSet.Contains(neighborNode))
                            openSet.Add(neighborNode);
                    }
                }
            }

            return new Path { IsValid = false };
        }

        private PathNode GetOrCreateNode(Vector2Int position)
        {
            if (!nodeCache.TryGetValue(position, out var node))
            {
                node = nodePool.Get();
                node.Position = position;
                nodeCache[position] = node;
            }

            return node;
        }

        private Path ReconstructPath(PathNode endNode)
        {
            var waypoints = new List<Vector2Int>();
            var current = endNode;
            var totalCost = current.GCost;

            while (current != null)
            {
                waypoints.Add(current.Position);
                current = current.Parent;
            }

            waypoints.Reverse();

            return new Path
            {
                Waypoints = waypoints,
                TotalCost = totalCost,
                IsValid = true
            };
        }

        private List<PathNode> GetNeighbors(PathNode current, Room room)
        {
            var neighbors = new List<PathNode>();
            var positions = room.GetNeighbors(current.Position.x, current.Position.y);

            foreach (var pos in positions) neighbors.Add(GetOrCreateNode(pos));

            return neighbors;
        }

        private class PathNodeComparer : IComparer<PathNode>
        {
            public int Compare(PathNode x, PathNode y)
            {
                var fCompare = x.FCost.CompareTo(y.FCost);
                if (fCompare == 0) return x.Position.GetHashCode().CompareTo(y.Position.GetHashCode());

                return fCompare;
            }
        }
    }
}