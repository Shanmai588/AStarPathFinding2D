using UnityEngine;

namespace RTS.Pathfinding
{
    public class Reservation
    {
        public int agentId;
        public Vector2Int position;
        public float startTime, endTime;

        public bool IsActive(float currentTime)
        {
            return currentTime >= startTime && currentTime <= endTime;
        }

        public bool IsExpired(float currentTime)
        {
            return currentTime > endTime;
        }
    }

}