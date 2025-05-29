using System;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Motor : MonoBehaviour
    {
        [SerializeField] private Agent agent;
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float arrivalThreshold = 0.1f;
        
        private Vector2 currentTarget;
        private bool isMoving = false;
        private Path currentPath;
        private int currentWaypointIndex = 0;

        public event Action OnDestinationReached;
        public event Action OnMovementStopped;

        private void Awake()
        {
            if (agent == null)
                agent = GetComponent<Agent>();
        }

        public void SetPath(Path path)
        {
            currentPath = path;
            currentWaypointIndex = 0;
            
            if (path != null && path.waypoints.Count > 0)
            {
                currentTarget = GridToWorldPosition(path.waypoints[0]);
            }
        }

        public void StartMovement()
        {
            isMoving = true;
        }

        public void StopMovement()
        {
            isMoving = false;
            OnMovementStopped?.Invoke();
        }

        private void Update()
        {
            if (isMoving && currentPath != null)
            {
                MoveAlongPath(Time.deltaTime);
            }
        }

        public bool IsAtDestination()
        {
            if (currentPath == null || currentPath.waypoints.Count == 0)
                return true;

            return currentWaypointIndex >= currentPath.waypoints.Count - 1 &&
                   Vector2.Distance(transform.position, currentTarget) <= arrivalThreshold;
        }

        private void MoveAlongPath(float deltaTime)
        {
            if (currentPath == null || currentPath.waypoints.Count == 0)
            {
                StopMovement();
                return;
            }

            // Check if we've reached the current waypoint
            if (Vector2.Distance(transform.position, currentTarget) <= arrivalThreshold)
            {
                currentWaypointIndex++;
                
                if (currentWaypointIndex >= currentPath.waypoints.Count)
                {
                    // Reached final destination
                    StopMovement();
                    OnDestinationReached?.Invoke();
                    return;
                }
                
                // Move to next waypoint
                currentTarget = GridToWorldPosition(currentPath.waypoints[currentWaypointIndex]);
            }

            // Move towards current target
            Vector2 direction = (currentTarget - (Vector2)transform.position).normalized;
            Vector2 movement = direction * moveSpeed * deltaTime;
            
            // Check for collisions before moving
            if (!HandleCollision(movement))
            {
                transform.position += (Vector3)movement;
                
                // Update agent position
                if (agent != null)
                {
                    var gridPos = WorldToGridPosition(transform.position);
                    int roomId = GetCurrentRoomId(transform.position);
                    agent.UpdatePosition(gridPos, roomId);
                }
            }
        }

        private bool HandleCollision(Vector2 plannedMovement)
        {
            Vector2 plannedPosition = (Vector2)transform.position + plannedMovement;
            int roomId;
            var gridPos = WorldToGridPosition(plannedPosition, out roomId);
            
            var navController = RoomBasedNavigationController.Instance;
            if (navController != null)
            {
                // Check if the planned position is walkable
                if (!navController.IsPositionWalkable(roomId, gridPos))
                    return true; // Collision detected
                    
                // Check if position is occupied by another agent
                var tile = navController.GetTile(roomId, gridPos.x, gridPos.y);
                if (tile != null && tile.IsOccupied() && tile.occupyingAgent != agent)
                    return true; // Collision with another agent
            }
            
            return false; // No collision
        }

        private Vector2 GridToWorldPosition(Vector2Int gridPos)
        {
            if (agent != null)
            {
                var navController = RoomBasedNavigationController.Instance;
                if (navController != null)
                {
                    return navController.GridToWorld(agent.CurrentRoomId, gridPos);
                }
            }
            return new Vector2(gridPos.x, gridPos.y);
        }

        private Vector2Int WorldToGridPosition(Vector2 worldPos)
        {
            int roomId;
            var navController = RoomBasedNavigationController.Instance;
            if (navController != null)
            {
                return navController.WorldToGrid(worldPos, out roomId);
            }
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        }

        private Vector2Int WorldToGridPosition(Vector2 worldPos, out int roomId)
        {
            var navController = RoomBasedNavigationController.Instance;
            if (navController != null)
            {
                return navController.WorldToGrid(worldPos, out roomId);
            }
            roomId = 0;
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        }

        private int GetCurrentRoomId(Vector2 worldPos)
        {
            int roomId;
            WorldToGridPosition(worldPos, out roomId);
            return roomId;
        }
    }
}