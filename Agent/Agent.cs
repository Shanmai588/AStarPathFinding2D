using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Agent : MonoBehaviour
    {
        [SerializeField] private int agentId;
        [SerializeField] private Vector2Int currentPosition;
        [SerializeField] private int currentRoomId;
        [SerializeField] private AgentType type = AgentType.INFANTRY;
        [SerializeField] private MovementCapabilities capabilities;

        private Path currentPath;
        private ICostProvider costProvider;
        private RoomBasedNavigationController navigationController;

        public event Action<Path> OnPathReceived;
        public event Action OnPathFailed;

        public int AgentId => agentId;
        public Vector2Int CurrentPosition => currentPosition;
        public int CurrentRoomId => currentRoomId;
        public AgentType Type => type;

        private void Awake()
        {
            if (capabilities == null)
            {
                capabilities = new MovementCapabilities();
            }

            if (costProvider == null)
            {
                costProvider = new StandardCostProvider();
            }

            agentId = GetInstanceID(); // Simple ID assignment
        }

        private void Start()
        {
            navigationController = FindObjectOfType<RoomBasedNavigationController>();
            if (navigationController != null)
            {
                // Initialize position based on current transform
                InitializePositionFromTransform();
                navigationController.RegisterAgent(this);
            }
        }

        private void InitializePositionFromTransform()
        {
            if (navigationController != null)
            {
                // Get current world position and convert to grid coordinates
                Vector2 worldPos = transform.position;
                int roomId;
                Vector2Int gridPos = navigationController.WorldToGrid(worldPos, out roomId);

                // Update agent's position
                currentPosition = gridPos;
                currentRoomId = roomId;

                // Verify the position is valid, if not find closest valid position
                if (!navigationController.IsPositionWalkable(roomId, gridPos))
                {
                    var closestValid = navigationController.FindClosestReachablePoint(
                        gridPos, gridPos, roomId, capabilities);
                    currentPosition = closestValid;

                    // Update transform to match the corrected position
                    var correctedWorldPos = navigationController.GridToWorld(roomId, closestValid);
                    transform.position = new Vector3(correctedWorldPos.x, correctedWorldPos.y, transform.position.z);
                }

                // Occupy the tile
                var tile = navigationController.GetTile(roomId, currentPosition.x, currentPosition.y);
                if (tile != null && tile.isWalkable)
                {
                    tile.SetOccupant(this);
                }
            }
        }

        private void OnDestroy()
        {
            if (navigationController != null)
            {
                navigationController.UnregisterAgent(this);
            }
        }

        public void RequestPath(Vector2Int target, int targetRoom)
        {
            if (navigationController == null) return;

            var request = new PathRequest
            {
                agentId = agentId,
                startPos = currentPosition,
                endPos = target,
                startRoomId = currentRoomId,
                endRoomId = targetRoom,
                costProvider = costProvider,
                priority = RequestPriority.NORMAL,
                onComplete = OnPathRequestComplete
            };

            navigationController.RequestPath(request);
        }

        public void RequestPathToClosestReachable(Vector2Int target, int targetRoom)
        {
            if (navigationController == null) return;

            var closestPoint = navigationController.FindClosestReachablePoint(
                currentPosition, target, targetRoom, capabilities);

            RequestPath(closestPoint, targetRoom);
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
            costProvider = provider;
        }

        public void UpdatePosition(Vector2Int newPosition, int newRoomId)
        {
            // Clear occupancy from old position
            if (navigationController != null)
            {
                var oldTile = navigationController.GetTile(currentRoomId, currentPosition.x, currentPosition.y);
                if (oldTile != null && oldTile.occupyingAgent == this)
                {
                    oldTile.SetOccupant(null);
                }
            }

            currentPosition = newPosition;
            currentRoomId = newRoomId;

            // Set occupancy on new position
            if (navigationController != null)
            {
                var newTile = navigationController.GetTile(newRoomId, newPosition.x, newPosition.y);
                if (newTile != null && newTile.isWalkable)
                {
                    newTile.SetOccupant(this);
                }
            }
        }

        private void OnPathRequestComplete(Path path)
        {
            currentPath = path;
            if (path != null && path.isValid)
            {
                OnPathReceived?.Invoke(path);
            }
            else
            {
                OnPathFailed?.Invoke();
            }
        }

        private Vector2Int FindClosestReachablePoint(Vector2Int target, int targetRoom)
        {
            if (navigationController != null)
            {
                return navigationController.FindClosestReachablePoint(
                    currentPosition, target, targetRoom, capabilities);
            }

            return target;
        }
    }
}