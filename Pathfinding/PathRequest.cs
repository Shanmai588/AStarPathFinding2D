using System;
using UnityEngine;

namespace RTS.Pathfinding
{
    // Command object representing a pathfinding request
    public class PathRequest
    {
        public int AgentId { get; set; }

        public Vector2Int StartPos { get; set; }

        public Vector2Int EndPos { get; set; }

        public int StartRoomId { get; set; }

        public int EndRoomId { get; set; }

        public ICostProvider CostProvider { get; set; }

        public Action<Path> OnComplete { get; set; }

        public RequestPriority Priority { get; set; }

        public Path Execute()
        {
            var gridManager = GameObject.FindObjectOfType<GridManager>();
            return gridManager?.GetPath(this);
        }

        public void Reset()
        {
            AgentId = 0;
            StartPos = Vector2Int.zero;
            EndPos = Vector2Int.zero;
            StartRoomId = 0;
            EndRoomId = 0;
            CostProvider = null;
            OnComplete = null;
            Priority = RequestPriority.Normal;
        }
    }
}