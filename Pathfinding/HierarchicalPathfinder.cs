using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class HierarchicalPathfinder
    {
        private RoomGraphPathfinder roomPathfinder = new RoomGraphPathfinder();
        private AStarPathfinder localPathfinder = new AStarPathfinder();
        private PathCache cache = new PathCache();
        private GridManager gridManager;

        public void SetGridManager(GridManager gm)
        {
            gridManager = gm;
            // Build room graph when grid manager is set
            if (gridManager != null)
            {
                roomPathfinder.BuildRoomGraph(gridManager.GetAllRooms());
            }
        }

        public Path FindPath(PathRequest request)
        {
            // Check cache first
            var cacheKey = new PathCacheKey
            {
                start = request.startPos,
                end = request.endPos,
                roomId = request.startRoomId,
                costProviderType = request.costProvider.GetType().Name
            };

            var cachedPath = cache.GetCachedPath(cacheKey);
            if (cachedPath != null)
                return cachedPath;

            Path path;
            if (request.startRoomId == request.endRoomId)
            {
                // Same room - use local pathfinding
                path = FindLocalPath(request.startRoomId, request.startPos, request.endPos, request.costProvider);
            }
            else
            {
                // Cross-room pathfinding
                var roomPath = FindRoomPath(request.startRoomId, request.endRoomId);
                path = CombineRoomPaths(roomPath, request);
            }

            cache.CachePath(cacheKey, path);
            return path;
        }

        private List<int> FindRoomPath(int startRoom, int endRoom)
        {
            return roomPathfinder.FindRoomSequence(startRoom, endRoom);
        }

        private Path FindLocalPath(int roomId, Vector2Int start, Vector2Int end, ICostProvider costProvider)
        {
            var gridManager = RoomBasedNavigationController.Instance?.GetGridManager();
            var room = gridManager?.GetRoom(roomId);

            if (room == null)
            {
                return new Path { isValid = false };
            }

            return localPathfinder.FindPath(start, end, room, costProvider);
        }

        private Path CombineRoomPaths(List<int> roomPath, PathRequest request)
        {
            if (roomPath.Count == 0)
                return new Path { isValid = false };

            if (roomPath.Count == 1)
            {
                // Single room path
                return FindLocalPath(roomPath[0], request.startPos, request.endPos, request.costProvider);
            }

            var combinedPath = new Path();
            var gridManager = RoomBasedNavigationController.Instance?.GetGridManager();

            if (gridManager == null)
                return new Path { isValid = false };

            // For each room transition, find the path to the door
            for (int i = 0; i < roomPath.Count - 1; i++)
            {
                var currentRoomId = roomPath[i];
                var nextRoomId = roomPath[i + 1];
                var currentRoom = gridManager.GetRoom(currentRoomId);

                if (currentRoom == null) continue;

                // Find the door connecting current room to next room
                var door = currentRoom.doors.FirstOrDefault(d => d.connectedRoomId == nextRoomId);
                if (door == null) continue;

                Vector2Int startPos, endPos;

                if (i == 0)
                {
                    // First room: start from request start position
                    startPos = request.startPos;
                }
                else
                {
                    // Middle room: start from entry door
                    var entryDoor = currentRoom.doors.FirstOrDefault(d => d.connectedRoomId == roomPath[i - 1]);
                    startPos = entryDoor?.positionInRoom ?? Vector2Int.zero;
                }

                if (i == roomPath.Count - 2)
                {
                    // Last transition: go to final destination in next room
                    var nextRoom = gridManager.GetRoom(nextRoomId);
                    if (nextRoom != null)
                    {
                        // Path from door to final destination
                        var pathToDoor = FindLocalPath(currentRoomId, startPos, door.positionInRoom,
                            request.costProvider);
                        var pathToEnd = FindLocalPath(nextRoomId, door.connectedPosition, request.endPos,
                            request.costProvider);

                        // Combine paths
                        if (pathToDoor.isValid && pathToEnd.isValid)
                        {
                            combinedPath.waypoints.AddRange(pathToDoor.waypoints);
                            combinedPath.waypoints.AddRange(pathToEnd.waypoints
                                .Skip(1)); // Skip duplicate door position
                            combinedPath.totalCost += pathToDoor.totalCost + pathToEnd.totalCost;
                        }
                    }
                }
                else
                {
                    // Middle room: go to exit door
                    endPos = door.positionInRoom;
                    var segmentPath = FindLocalPath(currentRoomId, startPos, endPos, request.costProvider);

                    if (segmentPath.isValid)
                    {
                        if (combinedPath.waypoints.Count == 0)
                        {
                            combinedPath.waypoints.AddRange(segmentPath.waypoints);
                        }
                        else
                        {
                            combinedPath.waypoints.AddRange(segmentPath.waypoints.Skip(1)); // Skip duplicate position
                        }

                        combinedPath.totalCost += segmentPath.totalCost;
                    }
                }
            }

            combinedPath.isValid = combinedPath.waypoints.Count > 0;
            return combinedPath;
        }
    }
}