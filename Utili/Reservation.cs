using UnityEngine;

namespace RTS.Pathfinding
{
    public class Reservation
    {
        public int AgentId { get; set; }
        public Vector2Int Position { get; set; }
        public float StartTime { get; set; }
        public float EndTime { get; set; }

        public bool IsActive(float currentTime)
        {
            return currentTime >= StartTime && currentTime <= EndTime;
        }

        public bool IsExpired(float currentTime)
        {
            return currentTime > EndTime;
        }
    }
}