using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Path
    {
        public List<Vector2Int> Waypoints { get; set; } = new();
        public float TotalCost { get; set; }
        public bool IsValid { get; set; }

        public Vector2Int GetNextWaypoint(Vector2Int currentPos)
        {
            if (Waypoints == null || Waypoints.Count == 0)
                return currentPos;

            // Find current position in waypoints
            var currentIndex = Waypoints.FindIndex(w => w == currentPos);

            // If not found, return first waypoint
            if (currentIndex == -1)
                return Waypoints[0];

            // If at end, return current position
            if (currentIndex >= Waypoints.Count - 1)
                return currentPos;

            // Return next waypoint
            return Waypoints[currentIndex + 1];
        }

        public bool IsComplete(Vector2Int currentPos)
        {
            if (Waypoints == null || Waypoints.Count == 0)
                return true;

            return currentPos == Waypoints[Waypoints.Count - 1];
        }

        public List<Vector2Int> GetWaypoints()
        {
            return new List<Vector2Int>(Waypoints);
        }
    }
}