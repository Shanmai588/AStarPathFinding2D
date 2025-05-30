using System;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathRequest
    {
        public PathRequest(int agentId, Vector2Int start, Vector2Int end,
            int startRoom, int endRoom, ICostProvider provider,
            Action<Path> onComplete, Agent agent, RequestPriority priority = RequestPriority.Normal)
        {
            AgentId = agentId;
            StartPos = start;
            EndPos = end;
            StartRoomId = startRoom;
            EndRoomId = endRoom;
            CostProvider = provider;
            OnComplete = onComplete;
            Agent = agent;
            Priority = priority;
        }

        public int AgentId { get; private set; }
        public Vector2Int StartPos { get; private set; }
        public Vector2Int EndPos { get; private set; }
        public int StartRoomId { get; private set; }
        public int EndRoomId { get; private set; }
        public ICostProvider CostProvider { get; private set; }
        public Action<Path> OnComplete { get; private set; }
        public RequestPriority Priority { get; private set; }
        public Agent Agent { get; private set; }

        public Path Execute(HierarchicalPathfinder pathfinder)
        {
            return pathfinder.FindPath(this);
        }

        public void Reset()
        {
            OnComplete = null;
            CostProvider = null;
            Agent = null;
        }
    }
}