using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathRequestManager
    {
        private readonly int maxRequestsPerFrame;
        private readonly HierarchicalPathfinder pathfinder;
        private readonly Queue<PathRequest> requestQueue;
        private ObjectPool<PathRequest> requestPool;

        public PathRequestManager(HierarchicalPathfinder finder, int maxPerFrame = 5)
        {
            requestQueue = new Queue<PathRequest>();
            requestPool = new ObjectPool<PathRequest>(
                () => new PathRequest(0, Vector2Int.zero, Vector2Int.zero, 0, 0, null, null, null),
                req => req.Reset()
            );
            maxRequestsPerFrame = maxPerFrame;
            pathfinder = finder;
        }

        public void QueueRequest(PathRequest request)
        {
            requestQueue.Enqueue(request);
        }

        public void ProcessRequests()
        {
            var processed = 0;
            while (requestQueue.Count > 0 && processed < maxRequestsPerFrame)
            {
                var request = requestQueue.Dequeue();
                ExecuteRequest(request);
                processed++;
            }
        }

        private void ExecuteRequest(PathRequest request)
        {
            var path = request.Execute(pathfinder);
            request.OnComplete?.Invoke(path);
        }
    }
}