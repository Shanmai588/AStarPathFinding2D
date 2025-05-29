using System;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathRequest
    {
        public int agentId;
        public Vector2Int startPos, endPos;
        public int startRoomId, endRoomId;
        public ICostProvider costProvider;
        public Action<Path> onComplete;
        public RequestPriority priority;

        public Path Execute()
        {
            // This will be implemented by the pathfinding system
            return new Path();
        }

        public void Reset()
        {
            agentId = 0;
            startPos = endPos = Vector2Int.zero;
            startRoomId = endRoomId = 0;
            costProvider = null;
            onComplete = null;
            priority = RequestPriority.NORMAL;
        }
    }

}