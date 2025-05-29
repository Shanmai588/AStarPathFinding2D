using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Path
    {
        public List<Vector2Int> waypoints = new List<Vector2Int>();
        public float totalCost;
        public bool isValid = true;
        private int currentWaypointIndex = 0;

        public Vector2Int GetNextWaypoint(Vector2Int currentPos)
        {
            if (currentWaypointIndex < waypoints.Count)
            {
                if (Vector2Int.Distance(currentPos, waypoints[currentWaypointIndex]) < 0.5f)
                    currentWaypointIndex++;
            
                if (currentWaypointIndex < waypoints.Count)
                    return waypoints[currentWaypointIndex];
            }
            return currentPos;
        }

        public bool IsComplete(Vector2Int currentPos)
        {
            return currentWaypointIndex >= waypoints.Count - 1;
        }

        public List<Vector2Int> GetWaypoints()
        {
            return waypoints;
        }
    }

}