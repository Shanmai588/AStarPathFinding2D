using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class HierarchicalPathfinder
    {
        private readonly PathCache cache;
        private readonly RoomGraphPathfinder roomPathfinder;
        private AStarPathfinder localPathfinder;

        public HierarchicalPathfinder(PathCache pathCache)
        {
            roomPathfinder = new RoomGraphPathfinder();
            localPathfinder = new AStarPathfinder();
            cache = pathCache;
        }

        public void Initialize(Dictionary<int, Room> rooms)
        {
            roomPathfinder.BuildRoomGraph(rooms);
        }

        public Path FindPath(PathRequest request)
        {
            var cacheKey = new PathCacheKey(request.StartPos, request.EndPos, request.StartRoomId,
                request.CostProvider.GetType().Name);

            var cachedPath = cache.GetCachedPath(cacheKey);
            if (cachedPath != null && cachedPath.IsValid)
                return cachedPath;

            if (request.StartRoomId == request.EndRoomId)
            {
                // Same room, direct path
                var path = FindLocalPath(request.StartRoomId, request.StartPos, request.EndPos, request.CostProvider);
                cache.CachePath(cacheKey, path);
                return path;
            }

            // Different rooms, need hierarchical path
            var roomSequence = FindRoomPath(request.StartRoomId, request.EndRoomId);
            if (roomSequence == null || roomSequence.Count == 0)
                return new Path(null, float.MaxValue);

            var fullPath = new List<Vector2Int>();
            float totalCost = 0;

            // Build complete path through rooms
            for (var i = 0; i < roomSequence.Count - 1; i++)
            {
                // Path within current room to door
                // Door finding logic would go here
                // For now, simplified implementation
            }

            var finalPath = new Path(fullPath, totalCost);
            cache.CachePath(cacheKey, finalPath);
            return finalPath;
        }

        private List<int> FindRoomPath(int startRoom, int endRoom)
        {
            return roomPathfinder.FindRoomSequence(startRoom, endRoom);
        }

        private Path FindLocalPath(int roomId, Vector2Int start, Vector2Int end, ICostProvider costProvider)
        {
            // Need room reference - would be provided by GridManager
            // Simplified for now
            return new Path(new List<Vector2Int> { start, end }, 1f);
        }
    }
}