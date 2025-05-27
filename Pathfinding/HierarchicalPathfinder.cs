using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class HierarchicalPathfinder 
    {
        private readonly PathCache cache = new();
        private readonly AStarPathfinder localPathfinder = new();
        private readonly RoomGraphPathfinder roomPathfinder = new();

        public Path FindPath(PathRequest request)
        {
            // Check cache first
            var cacheKey = new PathCacheKey
            {
                Start = request.StartPos,
                End = request.EndPos,
                RoomId = request.StartRoomId,
                CostProviderType = request.CostProvider.GetType().Name
            };

            var cachedPath = cache.GetCachedPath(cacheKey);
            if (cachedPath != null && cachedPath.IsValid)
                return cachedPath;

            // If same room, use local pathfinder
            if (request.StartRoomId == request.EndRoomId)
            {
                var gridManager = Singleton<GridManager>.Instance;
                var room = gridManager.GetRoom(request.StartRoomId);
                var path = localPathfinder.FindPath(request.StartPos, request.EndPos, room, request.CostProvider);
                cache.CachePath(cacheKey, path);
                return path;
            }

            // Different rooms, use hierarchical approach
            var roomSequence = roomPathfinder.FindRoomSequence(request.StartRoomId, request.EndRoomId);
            if (roomSequence == null || roomSequence.Count == 0)
                return new Path { IsValid = false };

            var fullPath = new List<Vector2Int>();
            var currentPos = request.StartPos;
            var currentRoomId = request.StartRoomId;

            for (var i = 1; i < roomSequence.Count; i++)
            {
                var nextRoomId = roomSequence[i];
                var door = FindDoorBetweenRooms(currentRoomId, nextRoomId);

                if (door == null) return new Path { IsValid = false };

                // Path to door in current room
                var gridManager = GameObject.FindObjectOfType<GridManager>();
                var currentRoom = gridManager.GetRoom(currentRoomId);
                var pathToDoor =
                    localPathfinder.FindPath(currentPos, door.PositionInRoom, currentRoom, request.CostProvider);

                if (!pathToDoor.IsValid) return new Path { IsValid = false };

                fullPath.AddRange(pathToDoor.GetWaypoints());

                // Move to next room
                currentPos = door.ConnectedPosition;
                currentRoomId = nextRoomId;
            }

            // Path to final destination
            var finalRoom = GameObject.FindObjectOfType<GridManager>().GetRoom(request.EndRoomId);
            var finalPath = localPathfinder.FindPath(currentPos, request.EndPos, finalRoom, request.CostProvider);

            if (!finalPath.IsValid) return new Path { IsValid = false };

            fullPath.AddRange(finalPath.GetWaypoints());

            var completePath = new Path
            {
                Waypoints = fullPath,
                IsValid = true,
                TotalCost = CalculateTotalCost(fullPath, request.CostProvider)
            };

            cache.CachePath(cacheKey, completePath);
            return completePath;
        }

        private Door FindDoorBetweenRooms(int fromRoom, int toRoom)
        {
            var gridManager = GameObject.FindObjectOfType<GridManager>();
            var room = gridManager.GetRoom(fromRoom);
            var doors = room.GetDoors();

            foreach (var door in doors)
                if (door.ConnectedRoomId == toRoom)
                    return door;

            return null;
        }

        private float CalculateTotalCost(List<Vector2Int> waypoints, ICostProvider costProvider)
        {
            float total = 0;
            for (var i = 1; i < waypoints.Count; i++)
                total += costProvider.GetHeuristicCost(waypoints[i - 1], waypoints[i]);
            return total;
        }
    }
}