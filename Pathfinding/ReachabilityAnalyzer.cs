using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class ReachabilityAnalyzer
    {
        private GridManager gridManager;
        public int maxSearchRadius = 10;

        public ReachabilityAnalyzer(GridManager gm)
        {
            gridManager = gm;
        }

        public Vector2Int FindClosestReachablePoint(Vector2Int start, Vector2Int target, int roomId,
            MovementCapabilities capabilities)
        {
            var room = gridManager.GetRoom(roomId);
            if (room == null) return target;

            // Check if target is already reachable
            var targetTile = room.GetTile(target.x, target.y);
            if (targetTile != null && capabilities.CanTraverse(targetTile))
                return target;

            // Expand search in increasing radius
            for (int radius = 1; radius <= maxSearchRadius; radius++)
            {
                var candidates = ExpandSearch(target, radius, roomId, capabilities);
                if (candidates.Count > 0)
                {
                    // Return closest candidate
                    Vector2Int closest = candidates[0];
                    float minDist = Vector2Int.Distance(start, closest);

                    foreach (var candidate in candidates)
                    {
                        float dist = Vector2Int.Distance(start, candidate);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closest = candidate;
                        }
                    }

                    return closest;
                }
            }

            return target; // Fallback
        }

        public bool IsPointReachable(Vector2Int from, Vector2Int to, int roomId, MovementCapabilities capabilities)
        {
            var room = gridManager.GetRoom(roomId);
            if (room == null) return false;

            var tile = room.GetTile(to.x, to.y);
            return tile != null && capabilities.CanTraverse(tile);
        }

        private List<Vector2Int> ExpandSearch(Vector2Int center, int radius, int roomId,
            MovementCapabilities capabilities)
        {
            var results = new List<Vector2Int>();
            var room = gridManager.GetRoom(roomId);
            if (room == null) return results;

            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    if (Vector2Int.Distance(center, new Vector2Int(x, y)) <= radius)
                    {
                        if (ValidateReachability(new Vector2Int(x, y), roomId, capabilities))
                            results.Add(new Vector2Int(x, y));
                    }
                }
            }

            return results;
        }

        private bool ValidateReachability(Vector2Int point, int roomId, MovementCapabilities capabilities)
        {
            var room = gridManager.GetRoom(roomId);
            if (room == null || !room.IsValidPosition(point.x, point.y))
                return false;

            var tile = room.GetTile(point.x, point.y);
            return tile != null && capabilities.CanTraverse(tile);
        }
    }
}