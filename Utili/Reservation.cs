using UnityEngine;

namespace RTS.Pathfinding
{
    public class Reservation
    {
        public Reservation(int agentId, Vector2Int pos, float start, float end)
        {
            AgentId = agentId;
            Position = pos;
            StartTime = start;
            EndTime = end;
        }

        public int AgentId { get; private set; }
        public Vector2Int Position { get; private set; }
        public float StartTime { get; }
        public float EndTime { get; }

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