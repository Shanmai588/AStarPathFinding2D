using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Path
    {
        private int currentIndex;

        public Path(List<Vector2Int> points, float cost)
        {
            Waypoints = points ?? new List<Vector2Int>();
            TotalCost = cost;
            IsValid = Waypoints.Count > 0;
            currentIndex = 0;
        }

        public List<Vector2Int> Waypoints { get; }

        public float TotalCost { get; }

        public bool IsValid { get; }

        public Vector2Int GetNextWaypoint(Vector2Int currentPos)
        {
            if (!IsValid || currentIndex >= Waypoints.Count)
                return currentPos;

            // Skip waypoints we've already passed
            while (currentIndex < Waypoints.Count - 1 &&
                   Vector2Int.Distance(currentPos, Waypoints[currentIndex]) < 0.5f)
                currentIndex++;

            return Waypoints[currentIndex];
        }

        public bool IsComplete(Vector2Int currentPos)
        {
            if (!IsValid)
                return true;

            return currentIndex >= Waypoints.Count - 1 &&
                   Vector2Int.Distance(currentPos, Waypoints[Waypoints.Count - 1]) < 0.5f;
        }

        public List<Vector2Int> GetWaypoints()
        {
            return new List<Vector2Int>(Waypoints);
        }
    }
}