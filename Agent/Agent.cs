using System;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Agent
    {
        private readonly MovementCapabilities capabilities;
        private readonly RoomBasedNavigationController navController;
        private ICostProvider costProvider;
        private Path currentPath;

        public Agent(int id, AgentType agentType, Vector2Int startPos, int startRoom,
            RoomBasedNavigationController controller)
        {
            AgentId = id;
            Type = agentType;
            CurrentPosition = startPos;
            CurrentRoomId = startRoom;
            navController = controller;

            // Set up capabilities based on type
            capabilities = Type switch
            {
                AgentType.Infantry => new MovementCapabilities(),
                AgentType.Vehicle => new MovementCapabilities(false, false, 2f),
                AgentType.Flying => new MovementCapabilities(true, false, 1.5f),
                AgentType.Naval => new MovementCapabilities(false, true, 3f),
                _ => new MovementCapabilities()
            };

            costProvider = new StandardCostProvider();
        }

        public int AgentId { get; }

        public Vector2Int CurrentPosition { get; private set; }

        public int CurrentRoomId { get; private set; }

        public AgentType Type { get; }

        public event Action<Path> OnPathReceived;
        public event Action OnPathFailed;

        public void RequestPath(Vector2Int target, int targetRoom)
        {
            // If target room is invalid, find closest room
            if (targetRoom == -1)
            {
                Debug.LogWarning($"Target room is invalid. Finding closest room to target {target}");
                targetRoom = FindClosestRoom(target);
                if (targetRoom == -1)
                {
                    Debug.LogError("No valid room found!");
                    OnPathFailed?.Invoke();
                    return;
                }
            }

            var request = new PathRequest(
                AgentId, CurrentPosition, target, CurrentRoomId, targetRoom,
                costProvider, OnPathFound, this
            );

            navController.GetPath(request);
        }

        public void RequestPathToClosestReachable(Vector2Int target, int targetRoom)
        {
            // If target room is invalid, find closest room
            if (targetRoom == -1)
            {
                targetRoom = FindClosestRoom(target);
                if (targetRoom == -1)
                {
                    Debug.LogError("No valid room found!");
                    OnPathFailed?.Invoke();
                    return;
                }
            }

            var reachableTarget = navController.FindClosestReachablePoint(
                CurrentPosition, target, targetRoom, capabilities
            );

            Debug.Log(
                $"Agent {AgentId}: Requesting path to closest reachable point {reachableTarget} (original target: {target})");
            RequestPath(reachableTarget, targetRoom);
        }

        private int FindClosestRoom(Vector2Int gridPos)
        {
            var rooms = navController.GetAllRooms();
            var closestRoom = -1;
            var minDistance = float.MaxValue;

            foreach (var kvp in rooms)
            {
                var room = kvp.Value;
                var roomCenter = new Vector2Int(room.Width / 2, room.Height / 2);
                var distance = Vector2Int.Distance(gridPos, roomCenter);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestRoom = room.RoomId;
                }
            }

            return closestRoom;
        }

        public Path GetCurrentPath()
        {
            return currentPath;
        }

        public MovementCapabilities GetMovementCapabilities()
        {
            return capabilities;
        }

        public void SetCostProvider(ICostProvider provider)
        {
            costProvider = provider ?? new StandardCostProvider();
        }

        public void UpdatePosition(Vector2Int newPos, int roomId)
        {
            CurrentPosition = newPos;
            CurrentRoomId = roomId;
        }

        private void OnPathFound(Path path)
        {
            currentPath = path;
            if (path != null && path.IsValid)
                OnPathReceived?.Invoke(path);
            else
                OnPathFailed?.Invoke();
        }

        private Vector2Int FindClosestReachablePoint(Vector2Int target, int targetRoom)
        {
            return navController.FindClosestReachablePoint(CurrentPosition, target, targetRoom, capabilities);
        }
    }
}