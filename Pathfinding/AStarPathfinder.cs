using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class AStarPathfinder
    {
        private readonly Dictionary<Vector2Int, PathNode> nodeMap;
        private readonly ObjectPool<PathNode> nodePool;

        public AStarPathfinder()
        {
            nodePool = new ObjectPool<PathNode>(
                () => new PathNode(Vector2Int.zero),
                node => node.Reset()
            );
            nodeMap = new Dictionary<Vector2Int, PathNode>();
        }

        public Path FindPath(Vector2Int start, Vector2Int goal, Room room, ICostProvider costProvider)
        {
            nodeMap.Clear();
            var openSet = new SortedSet<PathNode>(Comparer<PathNode>.Create((a, b) =>
            {
                var result = a.FCost.CompareTo(b.FCost);
                if (result == 0)
                    result = a.Position.x.CompareTo(b.Position.x);
                if (result == 0)
                    result = a.Position.y.CompareTo(b.Position.y);
                return result;
            }));
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
                    if (tile == null || costProvider.ShouldAvoidTile(tile, null))
                        continue;

                    var tentativeGCost = current.GCost + costProvider.GetMovementCost(tile, null);

                    var neighborNode = GetOrCreateNode(neighbor.Position);
                    var isNewNode = !openSet.Contains(neighborNode);

                    if (tentativeGCost < neighborNode.GCost || isNewNode)
                    {
                        if (!isNewNode)
                            openSet.Remove(neighborNode);

                        neighborNode.GCost = tentativeGCost;
                        neighborNode.HCost = costProvider.GetHeuristicCost(neighbor.Position, goal);
                        neighborNode.Parent = current;

                        openSet.Add(neighborNode);
                    }
                }
            }

            return new Path(null, float.MaxValue); // No path found
        }

        private PathNode GetOrCreateNode(Vector2Int position)
        {
            if (!nodeMap.TryGetValue(position, out var node))
            {
                node = nodePool.Get();
                node = new PathNode(position);
                nodeMap[position] = node;
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
            return new Path(waypoints, totalCost);
        }

        private List<PathNode> GetNeighbors(PathNode current, Room room)
        {
            var neighbors = new List<PathNode>();
            var positions = room.GetNeighbors(current.Position.x, current.Position.y);

            foreach (var pos in positions) neighbors.Add(GetOrCreateNode(pos));

            return neighbors;
        }
    }
}