using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class ReachabilityAnalyzer
{
    private GridManager gridManager;
    private int maxSearchRadius;

    public ReachabilityAnalyzer(GridManager manager, int maxRadius = 50)
    {
        gridManager = manager;
        maxSearchRadius = maxRadius;
    }

    public Vector2Int FindClosestReachablePoint(Vector2Int start, Vector2Int target, 
                                               int roomId, MovementCapabilities capabilities)
    {
        var room = gridManager.GetRoom(roomId);
        if (room == null)
        {
            Debug.LogWarning($"Room {roomId} not found. Cannot find closest reachable point.");
            return target;
        }

        // If target is outside room bounds, clamp it first
        Vector2Int clampedTarget = new Vector2Int(
            Mathf.Clamp(target.x, room.MinX, room.MaxX),
            Mathf.Clamp(target.y, room.MinY, room.MaxY)
        );

        // If clamped target is already reachable, use it
        if (IsPointReachable(start, clampedTarget, roomId, capabilities))
            return clampedTarget;

        // Search in expanding circles from the clamped target
        for (int radius = 1; radius <= maxSearchRadius; radius++)
        {
            var candidates = ExpandSearch(clampedTarget, radius, roomId, capabilities);
            
            if (candidates.Count == 0) continue;
            
            // Find closest reachable candidate to the original target (not clamped)
            Vector2Int closest = clampedTarget;
            float minDistance = float.MaxValue;

            foreach (var candidate in candidates)
            {
                if (IsPointReachable(start, candidate, roomId, capabilities))
                {
                    // Distance to original target, not start position
                    float dist = Vector2Int.Distance(target, candidate);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closest = candidate;
                    }
                }
            }

            if (minDistance < float.MaxValue)
            {
                Debug.Log($"Found closest reachable point {closest} for target {target} (was outside room or unreachable)");
                return closest;
            }
        }

        Debug.LogWarning($"No reachable point found near {target}. Returning clamped position {clampedTarget}");
        return clampedTarget; // Return clamped position as fallback
    }

    public bool IsPointReachable(Vector2Int from, Vector2Int to, int roomId, MovementCapabilities capabilities)
    {
        var room = gridManager.GetRoom(roomId);
        if (room == null) return false;

        // Check if target is within room bounds
        if (!room.IsValidPosition(to.x, to.y))
            return false;

        var tile = room.GetTile(to.x, to.y);
        if (tile == null || !capabilities.CanTraverse(tile))
            return false;

        // For now, simplified check - in full implementation would use pathfinding
        // to ensure there's actually a valid path
        return true;
    }

    private List<Vector2Int> ExpandSearch(Vector2Int center, int radius, int roomId, MovementCapabilities capabilities)
    {
        var room = gridManager.GetRoom(roomId);
        if (room == null)
            return new List<Vector2Int>();

        var points = new List<Vector2Int>();

        // Get points on the perimeter of the square
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            // Top and bottom edges
            CheckAndAddPoint(x, center.y - radius, room, capabilities, points);
            CheckAndAddPoint(x, center.y + radius, room, capabilities, points);
        }

        for (int y = center.y - radius + 1; y < center.y + radius; y++)
        {
            // Left and right edges
            CheckAndAddPoint(center.x - radius, y, room, capabilities, points);
            CheckAndAddPoint(center.x + radius, y, room, capabilities, points);
        }

        return points;
    }

    private void CheckAndAddPoint(int x, int y, Room room, MovementCapabilities capabilities, List<Vector2Int> points)
    {
        if (room.IsValidPosition(x, y))
        {
            var tile = room.GetTile(x, y);
            if (tile != null && capabilities.CanTraverse(tile))
            {
                points.Add(new Vector2Int(x, y));
            }
        }
    }

    private bool ValidateReachability(Vector2Int point, int roomId, MovementCapabilities capabilities)
    {
        var room = gridManager.GetRoom(roomId);
        if (room == null) return false;

        var tile = room.GetTile(point.x, point.y);
        return tile != null && capabilities.CanTraverse(tile);
    }
}
}