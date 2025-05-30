using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class HierarchicalPathfinder
    {
        private readonly PathCache cache;
        private readonly GridManager gridManager;
        private readonly AStarPathfinder localPathfinder;
        private readonly RoomGraphPathfinder roomPathfinder;

        public HierarchicalPathfinder(PathCache pathCache, GridManager gridMgr)
        {
            roomPathfinder = new RoomGraphPathfinder();
            localPathfinder = new AStarPathfinder();
            cache = pathCache;
            gridManager = gridMgr;
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

            // Get the agent for capability checks
            var agent = GetAgentFromRequest(request);

            if (request.StartRoomId == request.EndRoomId)
            {
                // Same room, direct path
                var path = FindLocalPath(request.StartRoomId, request.StartPos, request.EndPos,
                    request.CostProvider, agent);
                if (path != null && path.IsValid)
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
                var currentRoomId = roomSequence[i];
                var nextRoomId = roomSequence[i + 1];
                var currentRoom = gridManager.GetRoom(currentRoomId);
                var nextRoom = gridManager.GetRoom(nextRoomId);

                if (currentRoom == null || nextRoom == null)
                    continue;

                // Find the door connecting these rooms
                Door connectingDoor = null;
                foreach (var door in currentRoom.GetDoors())
                    if (door.ConnectedRoomId == nextRoomId && door.IsPassable())
                    {
                        connectingDoor = door;
                        break;
                    }

                if (connectingDoor == null)
                    return new Path(null, float.MaxValue); // No valid door

                // Determine start and end positions for this segment
                var segmentStart = i == 0 ? request.StartPos : fullPath[fullPath.Count - 1];
                var segmentEnd = connectingDoor.PositionInRoom;

                // Find path to the door
                var pathToDoor = FindLocalPath(currentRoomId, segmentStart, segmentEnd,
                    request.CostProvider, agent);
                if (pathToDoor == null || !pathToDoor.IsValid)
                    return new Path(null, float.MaxValue);

                // Add waypoints (excluding the first one if it's not the start to avoid duplicates)
                var startIndex = i == 0 ? 0 : 1;
                for (var j = startIndex; j < pathToDoor.Waypoints.Count; j++) fullPath.Add(pathToDoor.Waypoints[j]);

                totalCost += pathToDoor.TotalCost;

                // Add the transition point in the next room
                fullPath.Add(connectingDoor.ConnectedPosition);
            }

            // Final path segment in the destination room
            if (roomSequence.Count > 1)
            {
                var lastRoomId = roomSequence[roomSequence.Count - 1];
                var lastStart = fullPath[fullPath.Count - 1];
                var finalPath = FindLocalPath(lastRoomId, lastStart, request.EndPos,
                    request.CostProvider, agent);

                if (finalPath == null || !finalPath.IsValid)
                    return new Path(null, float.MaxValue);

                // Add final waypoints (excluding the first to avoid duplicate)
                for (var j = 1; j < finalPath.Waypoints.Count; j++) fullPath.Add(finalPath.Waypoints[j]);

                totalCost += finalPath.TotalCost;
            }

            var completePath = new Path(fullPath, totalCost);
            cache.CachePath(cacheKey, completePath);
            return completePath;
        }

        private List<int> FindRoomPath(int startRoom, int endRoom)
        {
            return roomPathfinder.FindRoomSequence(startRoom, endRoom);
        }

        private Path FindLocalPath(int roomId, Vector2Int start, Vector2Int end,
            ICostProvider costProvider, Agent agent)
        {
            var room = gridManager.GetRoom(roomId);
            if (room == null)
                return new Path(null, float.MaxValue);

            return localPathfinder.FindPath(start, end, room, costProvider, agent);
        }

        private Agent GetAgentFromRequest(PathRequest request)
        {
            return request.Agent;
        }
    }
}