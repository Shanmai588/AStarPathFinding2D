using UnityEngine;
using System.Collections.Generic;

namespace RTS.Pathfinding
{
    /// <summary>
    /// Handles path requests and manages path data.
    /// </summary>
    public class PathRequester : MonoBehaviour
    {
        [SerializeField] private AStarNavAgent navAgent;
        [SerializeField] private PathfindingService pathfindingService;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private RoomGraph roomGraph;
        [SerializeField] private float repathInterval = 0.5f;
        [SerializeField] private float repathThreshold = 1.0f;
        [SerializeField] private bool repath = true;

        // Path state
        private List<Vector3> currentPath;
        private List<int> currentPathIndices;
        private List<Door> doorsToPass;
        private bool pathPending;
        private bool hasPath;
        private float lastPathRequestTime;

        // Events
        public System.Action<List<Vector3>, List<int>, List<Door>> OnPathFoundEvent;
        public System.Action OnPathFailedEvent;

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
        }

        // Event handler for tile changes
        private void OnTileChanged(int tileIndex)
        {
            // If the changed tile is on our path, request a new path
            if (currentPathIndices.Contains(tileIndex))
            {
                if (!pathPending)
                {
                    RequestPath(GetComponent<DestinationSetter>().GetDestination());
                }
            }
        }

        /// <summary>
        /// Request a path to the specified destination.
        /// </summary>
        public void RequestPath(Vector2 destination)
        {
            // Skip if a path request is already pending
            if (pathPending)
                return;

            pathPending = true;
            lastPathRequestTime = Time.time;

            // Get current position as tile index
            int startIndex = CurrentTileIndex;

            // Convert target position to tile index
            Vector2Int targetGrid = gridManager.WorldToGrid(destination);
            int goalIndex = gridManager.GridToIndex(targetGrid);
            float distanceToGoal;
            // Quick check - are we already at the destination?
            if (startIndex == goalIndex)
            {
                // Already at destination, no need to path
                pathPending = false;
                
                // If we're extremely close to the destination, consider it reached
                 distanceToGoal = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.y),
                    destination
                );
                
                if (distanceToGoal < 0.1f)
                {
                    OnPathFailedEvent?.Invoke(); // This will trigger destination reached
                }
                else
                {
                    // Create a simple direct path
                    List<Vector3> simplePath = new List<Vector3>()
                    {
                        new Vector3(destination.x, destination.y, transform.position.z)
                    };
                    currentPath = simplePath;
                    currentPathIndices = new List<int>() { goalIndex };
                    hasPath = true;
                    OnPathFoundEvent?.Invoke(currentPath, currentPathIndices, new List<Door>());
                }
                return;
            }

            // Check if we're already very close to the destination
             distanceToGoal = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                destination
            );

            if (distanceToGoal < 0.2f) // Small threshold
            {
                // Very close to destination, just move directly
                pathPending = false;
                List<Vector3> simplePath = new List<Vector3>()
                {
                    new Vector3(destination.x, destination.y, transform.position.z)
                };
                currentPath = simplePath;
                currentPathIndices = new List<int>() { goalIndex };
                hasPath = true;
                
                OnPathFoundEvent?.Invoke(currentPath, currentPathIndices, new List<Door>());
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
                // Store the path
                currentPath = result.Waypoints;
                currentPathIndices = result.PathIndices;
                doorsToPass = result.DoorsToPass;
                hasPath = true;

                // Notify that a path has been found
                OnPathFoundEvent?.Invoke(currentPath, currentPathIndices, doorsToPass);
            }
            else
            {
                // Path failed
                Debug.LogWarning($"Path finding failed for {gameObject.name}");
                hasPath = false;
                OnPathFailedEvent?.Invoke();
            }
        }

        /// <summary>
        /// Check if we should repath based on time and distance.
        /// </summary>
        public bool ShouldRepath(float remainingPathDistance)
        {
            if (!repath || pathPending) return false;
            
            float timeSinceLastPath = Time.time - lastPathRequestTime;
            return timeSinceLastPath > repathInterval && remainingPathDistance > repathThreshold * 3;
        }

        /// <summary>
        /// Check if the agent has a valid path.
        /// </summary>
        public bool HasPath()
        {
            return hasPath;
        }

        /// <summary>
        /// Check if a path request is pending.
        /// </summary>
        public bool IsPathPending()
        {
            return pathPending;
        }

        /// <summary>
        /// Clear the current path.
        /// </summary>
        public void ClearPath()
        {
            hasPath = false;
            pathPending = false;
            currentPath.Clear();
            currentPathIndices.Clear();
            doorsToPass.Clear();
        }
    }
}