using System;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Motor : MonoBehaviour
    {
        private Agent agent;
        private Transform agentTransform;
        private float arrivalThreshold = 0.1f;
        private Path currentPath;
        private Vector2 currentTarget;
        private bool isMoving;
        private float moveSpeed = 5f;

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

        public void Initialize(Agent a, float speed = 5f)
        {
            agent = a;
            moveSpeed = speed;
        }

        public void SetPath(Path path)
        {
            currentPath = path;
            if (path != null && path.IsValid) StartMovement();
        }

        public void StartMovement()
        {
            if (currentPath != null && currentPath.IsValid) isMoving = true;
        }

        public void StopMovement()
        {
            isMoving = false;
            OnMovementStopped?.Invoke();
        }

        public bool IsAtDestination()
        {
            if (currentPath == null || !currentPath.IsValid)
                return true;

            return currentPath.IsComplete(agent.CurrentPosition);
        }

        private void MoveAlongPath(float deltaTime)
        {
            var worldPos = new Vector2(agentTransform.position.x, agentTransform.position.y);
            var gridPos = agent.CurrentPosition; // Would need world-to-grid conversion

            var nextWaypoint = currentPath.GetNextWaypoint(gridPos);

            // Convert grid waypoint to world position
            var targetWorldPos = new Vector2(nextWaypoint.x, nextWaypoint.y); // Simplified

            var direction = (targetWorldPos - worldPos).normalized;
            var movement = direction * moveSpeed * deltaTime;

            agentTransform.position += new Vector3(movement.x, movement.y, 0);

            // Update agent grid position
            // Would need proper world-to-grid conversion here
            agent.UpdatePosition(nextWaypoint, agent.CurrentRoomId);

            if (IsAtDestination())
            {
                StopMovement();
                OnDestinationReached?.Invoke();
            }
        }

        private void HandleCollision()
        {
            // Collision handling logic
        }
    }
}