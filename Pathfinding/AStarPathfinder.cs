using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class AStarPathfinder
    {
        private ObjectPool<PathNode> nodePool = new ObjectPool<PathNode>();
        private List<PathNode> openSet = new List<PathNode>();
        private HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        public Path FindPath(Vector2Int start, Vector2Int goal, Room room, ICostProvider costProvider)
        {
            openSet.Clear();
            closedSet.Clear();

            var startNode = nodePool.Get();
            startNode.position = start;
            startNode.gCost = 0;
            startNode.hCost = costProvider.GetHeuristicCost(start, goal);

            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                var current = GetLowestFCostNode();
                openSet.Remove(current);
                closedSet.Add(current.position);

                if (current.position == goal)
                {
                    var path = ReconstructPath(current);
                    ReturnNodesToPool();
                    return path;
                }

                var neighbors = GetNeighbors(current, room);
                foreach (var neighbor in neighbors)
                {
                    if (closedSet.Contains(neighbor.position))
                        continue;

                    var tile = room.GetTile(neighbor.position.x, neighbor.position.y);
                    if (costProvider.ShouldAvoidTile(tile, null))
                        continue;

                    float tentativeGCost = current.gCost + costProvider.GetMovementCost(tile, null);

                    var existingNode = openSet.Find(n => n.position == neighbor.position);
                    if (existingNode == null)
                    {
                        neighbor.gCost = tentativeGCost;
                        neighbor.hCost = costProvider.GetHeuristicCost(neighbor.position, goal);
                        neighbor.parent = current;
                        openSet.Add(neighbor);
                    }
                    else if (tentativeGCost < existingNode.gCost)
                    {
                        existingNode.gCost = tentativeGCost;
                        existingNode.parent = current;
                    }
                }
            }

            ReturnNodesToPool();
            return new Path { isValid = false };
        }

        private PathNode GetLowestFCostNode()
        {
            PathNode lowest = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < lowest.FCost)
                    lowest = openSet[i];
            }

            return lowest;
        }

        private Path ReconstructPath(PathNode node)
        {
            var path = new Path();
            var current = node;

            while (current != null)
            {
                path.waypoints.Insert(0, current.position);
                current = current.parent;
            }

            return path;
        }

        private List<PathNode> GetNeighbors(PathNode current, Room room)
        {
            var neighbors = new List<PathNode>();
            var neighborPositions = room.GetNeighbors(current.position.x, current.position.y);

            foreach (var pos in neighborPositions)
            {
                var neighbor = nodePool.Get();
                neighbor.position = pos;
                neighbors.Add(neighbor);
            }

            return neighbors;
        }

        private void ReturnNodesToPool()
        {
            foreach (var node in openSet)
                nodePool.Return(node);
            // Note: In a real implementation, you'd also return closed set nodes
        }
    }
}