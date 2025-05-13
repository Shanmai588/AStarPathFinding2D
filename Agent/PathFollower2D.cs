using UnityEngine;
using System.Collections.Generic;

namespace RTS.Pathfinding
{
    /// <summary>
    /// Handles following a path in a 2D environment.
    /// </summary>
    public class PathFollower2D : MonoBehaviour
    {
        [SerializeField] private AStarNavAgent navAgent;
        [SerializeField] private float waypointArrivalDistance = 0.1f;

        // Path state
        private List<Vector3> currentPath;
        private List<int> currentPathIndices;
        private List<Door> doorsToPass;
        private int currentWaypointIndex;
        private bool hasPath;
        private bool isMoving;

        // Movement state
        private Vector2 desiredDirection = Vector2.zero;
        private Vector2 overrideDirection = Vector2.zero;
        private bool hasDirectionOverride = false;

        // Events
        public System.Action OnWaypointReachedEvent;
        public System.Action OnPathCompletedEvent;
        public System.Action<int> OnWaypointIndexUpdatedEvent;

        private void Awake()
        {
            if (navAgent == null) navAgent = GetComponent<AStarNavAgent>();
            currentPath = new List<Vector3>();
            currentPathIndices = new List<int>();
            doorsToPass = new List<Door>();
        }

        /// <summary>
        /// Set the path to follow.
        /// </summary>
        public void SetPath(List<Vector3> path, List<int> pathIndices, List<Door> doors)
        {
            // Don't set hasPath to false yet, as we need to check if this is a repath
            bool pathTransition = hasPath && isMoving && currentPath != null && currentPath.Count > 0;
            
            if (pathTransition)
            {
                // If we're already following a path, we need to be smart about switching to the new path
                // Find the closest point in the new path to our current position
                Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
                int closestIndex = 0;
                float closestDistance = float.MaxValue;

                for (int i = 0; i < path.Count; i++)
                {
                    Vector2 waypointPos = new Vector2(path[i].x, path[i].y);
                    float distance = Vector2.Distance(currentPos, waypointPos);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIndex = i;
                    }
                }

                // Store the new path
                currentPath = path;
                currentPathIndices = pathIndices;
                doorsToPass = doors;

                // Set waypoint index to the closest point, so we don't go backwards
                currentWaypointIndex = closestIndex;

                // If we're already very close to the closest waypoint, move to the next one
                if (closestDistance < waypointArrivalDistance && currentWaypointIndex < currentPath.Count - 1)
                {
                    currentWaypointIndex++;
                }
            }
            else
            {
                // First path or new path after completion - store it normally
                currentPath = path;
                currentPathIndices = pathIndices;
                doorsToPass = doors;
                currentWaypointIndex = 0;
            }
            
            hasPath = path != null && path.Count > 0;
            isMoving = hasPath;

            // Notify that the waypoint index has been updated
            OnWaypointIndexUpdatedEvent?.Invoke(currentWaypointIndex);
        }

        private void Update()
        {
            if (hasPath && isMoving)
            {
                FollowPath();
            }
        }

        // Follow the current path
        private void FollowPath()
        {
            if (currentPath == null || currentWaypointIndex >= currentPath.Count)
            {
                hasPath = false;
                isMoving = false;
                return;
            }

            // Get current waypoint
            Vector3 waypoint = currentPath[currentWaypointIndex];

            // For 2D, we only care about X and Y coordinates
            Vector2 waypointXY = new Vector2(waypoint.x, waypoint.y);
            Vector2 currentPositionXY = new Vector2(transform.position.x, transform.position.y);

            // Calculate distance to waypoint (in 2D plane)
            float distance = Vector2.Distance(currentPositionXY, waypointXY);

            // If we're close enough to the waypoint, move to the next one
            if (distance < waypointArrivalDistance)
            {
                // Notify that we've reached a waypoint
                OnWaypointReachedEvent?.Invoke();
                
                currentWaypointIndex++;
                
                // Notify that the waypoint index has been updated
                OnWaypointIndexUpdatedEvent?.Invoke(currentWaypointIndex);

                // If we've reached the end of the path
                if (currentWaypointIndex >= currentPath.Count)
                {
                    hasPath = false;
                    isMoving = false;
                    OnPathCompletedEvent?.Invoke();
                    return;
                }
            }

            // Move towards the current waypoint
            MoveTowards(waypointXY);
        }

        // Calculate the remaining distance along the path
        public float CalculateRemainingPathDistance()
        {
            if (currentPath == null || currentWaypointIndex >= currentPath.Count)
                return 0f;

            float distance = 0f;
            Vector2 currentPositionXY = new Vector2(transform.position.x, transform.position.y);
            Vector2 prevPosition = currentPositionXY;

            // Add distance to current waypoint
            Vector2 currentWaypoint =
                new Vector2(currentPath[currentWaypointIndex].x, currentPath[currentWaypointIndex].y);
            distance += Vector2.Distance(currentPositionXY, currentWaypoint);
            prevPosition = currentWaypoint;

            // Add distances between remaining waypoints
            for (int i = currentWaypointIndex + 1; i < currentPath.Count; i++)
            {
                Vector2 waypoint = new Vector2(currentPath[i].x, currentPath[i].y);
                distance += Vector2.Distance(prevPosition, waypoint);
                prevPosition = waypoint;
            }

            return distance;
        }

        // Move towards a position in 2D
        private void MoveTowards(Vector2 position)
        {
            // Calculate direction in 2D
            Vector2 currentPos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 direction = (position - currentPos2D).normalized;

            // Skip if the direction is zero (already at destination)
            if (direction == Vector2.zero)
                return;

            // Store the desired direction for avoidance system
            desiredDirection = direction;

            // Use override direction if available (from local avoidance)
            if (hasDirectionOverride)
            {
                direction = overrideDirection;
            }

            // Calculate movement distance
            float moveSpeed = navAgent.MoveSpeed;
            float moveDistance = moveSpeed * Time.deltaTime;

            // Apply movement in 2D, preserving Z position
            float zPos = transform.position.z;
            transform.position = new Vector3(
                currentPos2D.x + direction.x * moveDistance,
                currentPos2D.y + direction.y * moveDistance,
                zPos
            );
        }

        /// <summary>
        /// Get the agent's current desired direction (for local avoidance).
        /// </summary>
        public Vector2 GetDesiredDirection()
        {
            return desiredDirection;
        }

        /// <summary>
        /// Set an override direction from local avoidance.
        /// </summary>
        public void SetOverrideDirection(Vector2 direction)
        {
            overrideDirection = direction;
            hasDirectionOverride = true;
        }

        /// <summary>
        /// Clear the direction override.
        /// </summary>
        public void ClearOverrideDirection()
        {
            hasDirectionOverride = false;
        }

        /// <summary>
        /// Stop following the current path.
        /// </summary>
        public void StopMovement()
        {
            isMoving = false;
        }

        /// <summary>
        /// Resume following the current path.
        /// </summary>
        public void ResumeMovement()
        {
            isMoving = hasPath;
        }

        /// <summary>
        /// Check if the agent is currently moving.
        /// </summary>
        public bool IsMoving()
        {
            return isMoving;
        }

        /// <summary>
        /// Get the current waypoint index.
        /// </summary>
        public int GetCurrentWaypointIndex()
        {
            return currentWaypointIndex;
        }

        /// <summary>
        /// Get the list of path indices.
        /// </summary>
        public List<int> GetPathIndices()
        {
            return currentPathIndices;
        }

        private void OnDrawGizmos()
        {
            if (!hasPath || currentPath == null || currentPath.Count <= 1)
                return;

            // Draw lines between waypoints
            Gizmos.color = Color.green;
            for (int i = currentWaypointIndex; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }

            // Mark current waypoint
            if (currentWaypointIndex < currentPath.Count)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(currentPath[currentWaypointIndex], 0.2f);
            }
        }
    }
}