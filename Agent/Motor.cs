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

        private void Awake()
        {
            agentTransform = transform;
        }

        private void Update()
        {
            if (!isMoving || currentPath == null || !currentPath.IsValid)
                return;

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
        }

        public void StartMovement()
        {
            if (currentPath != null && currentPath.IsValid) isMoving = true;
        }

        public void StopMovement()
        {
            isMoving = false;
            currentPath = null;
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