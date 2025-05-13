using System.Collections;
using System.Collections.Generic;
using RTS.AI;
using UnityEngine;

namespace RTS.Pathfinding
{
    /// <summary>
    /// Controls unit movement using the 2D pathfinding system.
    /// </summary>
    public class AgentController : MonoBehaviour
    {
        // References
        [SerializeField] private AStarNavAgent navAgent;
        [SerializeField] private PathfindingService pathfindingService;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private RoomGraph roomGraph;
        [SerializeField] private ReservationTable reservationTable;

        // Configuration
        [SerializeField] private int lookAheadSteps = 5;
        [SerializeField] private float repathThreshold = 1.0f;
        [SerializeField] private float waypointArrivalDistance = 0.1f;
        [SerializeField] private float repathInterval = 0.5f;
        [SerializeField] private bool repath = true;

        // 2D-specific settings
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool flipSpriteOnDirectionChange = true;
        [SerializeField] private bool rotateToFaceDirection = false;
        [SerializeField] private float rotationOffset = -90f; // Adjust based on your sprite's forward direction

        // Current path state
        private List<Vector3> currentPath;
        private List<int> currentPathIndices;
        private List<Door> doorsToPass;
        private int currentWaypointIndex;
        private float lastPathRequestTime;
        private bool pathPending;
        private bool hasPath;
        private bool destinationReached;

        // Movement state
        private Vector2 targetPosition;
        private bool hasTargetPosition;

        // Events
        public System.Action OnDestinationReachedEvent;
        public System.Action OnPathFailedEvent;

        // Local avoidance fields
        private Vector2 desiredDirection = Vector2.zero;
        private Vector2 overrideDirection = Vector2.zero;
        private bool hasDirectionOverride = false;

        // Get current tile index and room
        private int CurrentTileIndex => gridManager.GridToIndex(gridManager.WorldToGrid(transform.position));
        private Room CurrentRoom => roomGraph.FindRoomContaining(CurrentTileIndex);

        private void Awake()
        {
            // Initialize references if not set
            if (navAgent == null) navAgent = GetComponent<AStarNavAgent>();
            if (pathfindingService == null) pathfindingService = FindObjectOfType<PathfindingService>();
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            if (roomGraph == null) roomGraph = FindObjectOfType<RoomGraph>();
            if (reservationTable == null) reservationTable = FindObjectOfType<ReservationTable>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            currentPath = new List<Vector3>();
            currentPathIndices = new List<int>();
            doorsToPass = new List<Door>();
        }

        private void OnEnable()
        {
            // Subscribe to relevant events
            if (gridManager != null) gridManager.TileChanged += OnTileChanged;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (gridManager != null) gridManager.TileChanged -= OnTileChanged;

            // Release all reservations
            if (reservationTable != null) reservationTable.ReleaseAll(navAgent);
        }

        private void Update()
        {
            // If we have a target and no path is pending
            if (hasTargetPosition && !pathPending)
            {
                // Only repath if we don't have a path yet
                if (!hasPath)
                {
                    RequestPath();
                }
                // We don't repath during movement - that happens in FollowPath after reaching waypoints
            }

            // If we have a path, follow it
            if (hasPath && !destinationReached)
            {
                FollowPath();
            }
        }

        // Event handler for tile changes
        private void OnTileChanged(int tileIndex)
        {
            // If the changed tile is on our path, request a new path
            if (currentPathIndices.Contains(tileIndex))
            {
                if (hasTargetPosition && !pathPending)
                {
                    RequestPath();
                }
            }
        }

        /// <summary>
        /// Set a new target position for the agent to move to.
        /// </summary>
        public void SetDestination(Vector2 position)
        {
            targetPosition = position;
            hasTargetPosition = true;
            destinationReached = false;

            // Request a path to the target
            RequestPath();
        }

        /// <summary>
        /// Set a new target position for the agent to move to (Vector3 overload).
        /// </summary>
        public void SetDestination(Vector3 position)
        {
            SetDestination(new Vector2(position.x, position.y));
        }
        private void Start()
        {
            // Register with local avoidance system if available
            LocalAvoidanceSystem avoidanceSystem = FindObjectOfType<LocalAvoidanceSystem>();
            if (avoidanceSystem != null)
            {
                avoidanceSystem.RegisterAgent(this);
            }
        }

        private void OnDestroy()
        {
            // Unregister from local avoidance system
            LocalAvoidanceSystem avoidanceSystem = FindObjectOfType<LocalAvoidanceSystem>();
            if (avoidanceSystem != null)
            {
                avoidanceSystem.UnregisterAgent(this);
            }

            // Release all reservations
            if (reservationTable != null)
            {
                reservationTable.ReleaseAll(navAgent);
            }
        }
        // Send a path request
        private void RequestPath()
        {
            // Skip if a path request is already pending
            if (pathPending)
                return;

            pathPending = true;
            lastPathRequestTime = Time.time;

            // Get current position as tile index
            int startIndex = CurrentTileIndex;

            // Convert target position to tile index
            Vector2Int targetGrid = gridManager.WorldToGrid(targetPosition);
            int goalIndex = gridManager.GridToIndex(targetGrid);

            // Find nearest walkable tile if the target isn't walkable
            if (!gridManager.GetTile(goalIndex).Walkable)
            {
                goalIndex = gridManager.FindNearestWalkableTile(goalIndex);

                // If no walkable tile found, we can't path
                if (goalIndex < 0)
                {
                    Debug.LogWarning($"No walkable tile found near target for {gameObject.name}");
                    pathPending = false;
                    OnPathFailedEvent?.Invoke();
                    return;
                }

                // Update target position to the nearest walkable tile
                targetPosition = gridManager.GridToWorld2D(gridManager.IndexToGrid(goalIndex));
            }

            // Quick check - are we already at the destination?
            if (startIndex == goalIndex)
            {
                // Already at destination, no need to path
                pathPending = false;
                OnDestinationReached();
                return;
            }

            // Check if we're already very close to the destination
            float distanceToGoal = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                targetPosition
            );

            if (distanceToGoal < waypointArrivalDistance * 2)
            {
                // Very close to destination, just move directly
                pathPending = false;
                List<Vector3> simplePath = new List<Vector3>()
                {
                    new Vector3(targetPosition.x, targetPosition.y, transform.position.z)
                };
                currentPath = simplePath;
                currentPathIndices = new List<int>() { goalIndex };
                currentWaypointIndex = 0;
                hasPath = true;
                return;
            }

            // Create a path request
            ICostProvider costProvider = navAgent.CreateCostProvider();

            PathRequest request = new PathRequest(
                startIndex,
                goalIndex,
                navAgent,
                costProvider,
                OnPathComplete);

            // Look up rooms for hierarchical pathfinding
            request.StartRoom = CurrentRoom;
            request.GoalRoom = roomGraph.FindRoomContaining(goalIndex);

            // Send request to pathfinding service
            pathfindingService.RequestPath(request);
        }

        // Callback for when a path is ready
        private void OnPathComplete(PathResult result)
        {
            pathPending = false;

            if (result.Success && result.Waypoints.Count > 0)
            {
                // If we're already following a path, we need to be smart about switching to the new path
                if (hasPath && currentWaypointIndex < currentPath.Count)
                {
                    // Keep track of our current waypoint's world position
                    Vector2 currentWaypointPos = new Vector2(
                        currentPath[currentWaypointIndex].x,
                        currentPath[currentWaypointIndex].y
                    );

                    // Find the closest point in the new path to our current position
                    Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
                    int closestIndex = 0;
                    float closestDistance = float.MaxValue;

                    for (int i = 0; i < result.Waypoints.Count; i++)
                    {
                        Vector2 waypointPos = new Vector2(result.Waypoints[i].x, result.Waypoints[i].y);
                        float distance = Vector2.Distance(currentPos, waypointPos);

                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestIndex = i;
                        }
                    }

                    // Store the new path
                    currentPath = result.Waypoints;
                    currentPathIndices = result.PathIndices;
                    doorsToPass = result.DoorsToPass;

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
                    currentPath = result.Waypoints;
                    currentPathIndices = result.PathIndices;
                    doorsToPass = result.DoorsToPass;
                    currentWaypointIndex = 0;
                }

                hasPath = true;
                destinationReached = false;

                // Reserve path in the reservation table
                if (reservationTable != null)
                {
                    // Release any existing reservations
                    reservationTable.ReleaseAll(navAgent);

                    // Reserve the next few steps
                    int startIdx = currentWaypointIndex;
                    int endIdx = Mathf.Min(startIdx + lookAheadSteps, currentPathIndices.Count);

                    if (endIdx > startIdx)
                    {
                        List<int> pathSegment = new List<int>();
                        for (int i = startIdx; i < endIdx; i++)
                        {
                            pathSegment.Add(currentPathIndices[i]);
                        }

                        reservationTable.ReserveSequence(pathSegment, 0, navAgent);
                    }
                }
            }
            else
            {
                // Path failed
                Debug.LogWarning($"Path finding failed for {gameObject.name}");
                // Keep following the current path if we have one
                if (!hasPath)
                {
                    OnPathFailedEvent?.Invoke();
                }
            }
        }

        // Follow the current path
        private void FollowPath()
        {
            if (currentPath == null || currentWaypointIndex >= currentPath.Count)
            {
                hasPath = false;
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
                currentWaypointIndex++;

                // If we've reached the end of the path
                if (currentWaypointIndex >= currentPath.Count)
                {
                    OnDestinationReached();
                    return;
                }

                // Update reservations when we move to the next waypoint
                if (reservationTable != null && currentWaypointIndex + lookAheadSteps < currentPathIndices.Count)
                {
                    // Release the previous reservation
                    reservationTable.AdvanceTime();

                    // Reserve the next step in sequence
                    int nextIndex = currentPathIndices[currentWaypointIndex + lookAheadSteps - 1];
                    reservationTable.Reserve(nextIndex, lookAheadSteps - 1, navAgent);
                }

                // Only repath after reaching a waypoint, not during movement
                if (repath && hasTargetPosition)
                {
                    float timeSinceLastPath = Time.time - lastPathRequestTime;
                    if (timeSinceLastPath > repathInterval)
                    {
                        // For long paths, check if we should repath
                        float remainingPathDistance = CalculateRemainingPathDistance();

                        // Only repath if we're not too close to the destination
                        if (remainingPathDistance > repathThreshold * 3)
                        {
                            RequestPath();
                        }
                    }
                }
            }

            // Move towards the current waypoint
            MoveTowards(waypointXY);
        }

        // Calculate the remaining distance along the path
        private float CalculateRemainingPathDistance()
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

            // Handle sprite orientation for 2D
            if (direction != Vector2.zero)
            {
                if (flipSpriteOnDirectionChange && spriteRenderer != null)
                {
                    // Flip sprite based on horizontal direction
                    spriteRenderer.flipX = direction.x < 0;
                }

                if (rotateToFaceDirection)
                {
                    // Calculate angle in degrees from direction vector
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                    // Rotate to face direction (adjusting for sprite orientation)
                    transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
                }
            }
        }

        // Called when the destination is reached
        private void OnDestinationReached()
        {
            hasPath = false;
            destinationReached = true;

            // Release all reservations
            if (reservationTable != null)
            {
                reservationTable.ReleaseAll(navAgent);
            }

            Debug.Log($"Destination reached for {gameObject.name}");
            OnDestinationReachedEvent?.Invoke();
        }

        // Stop following the current path
        public void StopMovement()
        {
            hasTargetPosition = false;
            hasPath = false;
            pathPending = false;

            // Release all reservations
            if (reservationTable != null)
            {
                reservationTable.ReleaseAll(navAgent);
            }
        }

        // Visualize the path in the editor
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

            // Draw a circle at the destination
            if (hasTargetPosition)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(new Vector3(targetPosition.x, targetPosition.y, transform.position.z), 0.3f);
            }
        }

        // Get the current destination
        public Vector2 GetDestination()
        {
            return targetPosition;
        }

        // Check if the agent has reached its destination
        public bool HasReachedDestination()
        {
            return destinationReached;
        }

        // Get the agent's current desired direction (for local avoidance)
        public Vector2 GetDesiredDirection()
        {
            return desiredDirection;
        }

        // Set an override direction from local avoidance
        public void SetOverrideDirection(Vector2 direction)
        {
            overrideDirection = direction;
            hasDirectionOverride = true;
        }

        // Clear the direction override
        public void ClearOverrideDirection()
        {
            hasDirectionOverride = false;
        }

        // Get the current override direction (for debugging)
        public Vector2 GetCurrentOverrideDirection()
        {
            return hasDirectionOverride ? overrideDirection : Vector2.zero;
        }

        

        // Check if the agent is currently moving
        public bool IsMoving()
        {
            return hasPath && !destinationReached;
        }
    }
}