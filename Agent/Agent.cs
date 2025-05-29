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
            var request = new PathRequest(
                AgentId, CurrentPosition, target, CurrentRoomId, targetRoom,
                costProvider, OnPathFound
            );

            navController.GetPath(request);
        }

        public void RequestPathToClosestReachable(Vector2Int target, int targetRoom)
        {
            var reachableTarget = navController.FindClosestReachablePoint(
                CurrentPosition, target, targetRoom, capabilities
            );

            RequestPath(reachableTarget, targetRoom);
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