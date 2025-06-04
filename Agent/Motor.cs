using System;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Motor : MonoBehaviour
    {
        private readonly float arrivalThreshold = 0.1f;
        private Agent agent;
        private Transform agentTransform;
        private Path currentPath;
        private Vector2 currentTarget;
        private int currentWaypointIndex;
        private bool isMoving;
        private float moveSpeed = 5f;
        private RoomBasedNavigationController navController;
        public Vector2 CurrentMovementDirection { get; private set; }
        public bool IsMoving => isMoving; 
        private void Awake()
        {
            agentTransform = transform;
            CurrentMovementDirection = Vector2.zero;
        }

        private void Update()
        {
            if (!isMoving || currentPath == null || !currentPath.IsValid)
            {
                if (isMoving) // If it was moving and now stops due to invalid path
                {
                    CurrentMovementDirection = Vector2.zero;
                }

                return;
            }
            MoveAlongPath(Time.deltaTime);
        }

        public event Action OnDestinationReached;
        public event Action OnMovementStopped;

        public void Initialize(Agent a, RoomBasedNavigationController controller, float speed = 5f)
        {
            agent = a;
            navController = controller;
            moveSpeed = speed;
        }

        public void SetPath(Path path)
        {
            currentPath = path;
            currentWaypointIndex = 0;
            if (path != null && path.IsValid) StartMovement();
            else
            {
                // If path is immediately invalid, ensure movement direction is cleared
                isMoving = false;
                CurrentMovementDirection = Vector2.zero;
            }
        }

        public void StartMovement()
        {
            if (currentPath != null && currentPath.IsValid) isMoving = true;
            else
            {
                isMoving = false;
                CurrentMovementDirection = Vector2.zero;
            }
        }

        public void StopMovement()
        {
            isMoving = false;
            currentPath = null;
            CurrentMovementDirection = Vector2.zero;
            OnMovementStopped?.Invoke();
        }

        public bool IsAtDestination()
        {
            if (currentPath == null || !currentPath.IsValid)
                return true;

            return currentWaypointIndex >= currentPath.Waypoints.Count - 1 &&
                   Vector2.Distance(agentTransform.position,
                       GetTargetWorldPosition(currentPath.Waypoints[currentPath.Waypoints.Count - 1])) <
                   arrivalThreshold;
        }

        private void MoveAlongPath(float deltaTime)
        {
            if (currentWaypointIndex >= currentPath.Waypoints.Count)
            {
                StopMovement();
                OnDestinationReached?.Invoke();
                return;
            }

            var currentGridTarget = currentPath.Waypoints[currentWaypointIndex];
            var targetWorldPos = GetTargetWorldPosition(currentGridTarget);
            var currentWorldPos = new Vector2(agentTransform.position.x, agentTransform.position.y);

            // Check if we've reached the current waypoint
            if (Vector2.Distance(currentWorldPos, targetWorldPos) < arrivalThreshold)
            {
                // Update agent's grid position
                agent.UpdatePosition(currentGridTarget, agent.CurrentRoomId);

                // Move to next waypoint
                currentWaypointIndex++;

                if (currentWaypointIndex >= currentPath.Waypoints.Count)
                {
                    StopMovement();
                    OnDestinationReached?.Invoke();
                    return;
                }

                currentGridTarget = currentPath.Waypoints[currentWaypointIndex];
                targetWorldPos = GetTargetWorldPosition(currentGridTarget);
            }

            // Move towards current target
            var direction = (targetWorldPos - currentWorldPos).normalized;
            var movement = direction * moveSpeed * deltaTime;
            CurrentMovementDirection = direction;

            // Don't overshoot the target
            if (movement.magnitude > Vector2.Distance(currentWorldPos, targetWorldPos))
                agentTransform.position = new Vector3(targetWorldPos.x, targetWorldPos.y, agentTransform.position.z);
            else
                agentTransform.position += new Vector3(movement.x, movement.y, 0);
        }

        private Vector2 GetTargetWorldPosition(Vector2Int gridPos)
        {
            // Convert grid position to world position using the navigation controller
            return navController.GridToWorld(agent.CurrentRoomId, gridPos);
        }

        private void HandleCollision()
        {
            // Collision handling logic
        }
    }
}