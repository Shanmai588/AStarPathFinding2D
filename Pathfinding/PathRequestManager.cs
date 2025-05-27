using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class PathRequestManager
    {
        private readonly int maxRequestsPerFrame = 5;
        private readonly ObjectPool<PathRequest> requestPool;
        private readonly Queue<PathRequest> requestQueue = new();

        public PathRequestManager()
        {
            requestPool = new ObjectPool<PathRequest>(
                () => new PathRequest(),
                request => request.Reset()
            );
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
            var path = request.Execute();
            request.OnComplete?.Invoke(path);
            requestPool.Return(request);
        }
    }
}