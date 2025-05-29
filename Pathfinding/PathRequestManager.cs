using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathRequestManager
    {
        private Queue<PathRequest> requestQueue = new Queue<PathRequest>();
        private ObjectPool<PathRequest> requestPool = new ObjectPool<PathRequest>();
        private int maxRequestsPerFrame = 5;
        private HierarchicalPathfinder pathfinder;

        public PathRequestManager(HierarchicalPathfinder pf)
        {
            pathfinder = pf;
        }

        public void QueueRequest(PathRequest request)
        {
            requestQueue.Enqueue(request);
        }

        public void ProcessRequests()
        {
            int processed = 0;
            while (requestQueue.Count > 0 && processed < maxRequestsPerFrame)
            {
                var request = requestQueue.Dequeue();
                ExecuteRequest(request);
                processed++;
            }
        }

        private void ExecuteRequest(PathRequest request)
        {
            var path = pathfinder.FindPath(request);
            request.onComplete?.Invoke(path);
            requestPool.Return(request);
        }
    }
}