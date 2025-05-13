using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Jobs;

namespace RTS.Pathfinding 
{
    // Main pathfinding service that processes path requests
    public class PathfindingService : MonoBehaviour 
    {
        // References to other components
        [SerializeField] private GridManager gridManager;
        [SerializeField] private RoomGraph roomGraph;
        [SerializeField] private ReservationTable reservationTable;
        [SerializeField] private PathCache pathCache;
        
        // Configuration options
        [SerializeField] private int maxPathsPerFrame = 5;
        [SerializeField] private IHeuristic heuristic;
        [SerializeField] private bool useJobSystem = true;
        
        // Flag for allowing diagonal movement
        [SerializeField] private bool allowDiagonalMovement = true;
        
        // Queue for path requests
        private Queue<PathRequest> pendingRequests = new Queue<PathRequest>();
        
        // The number of paths processed this frame
        private int pathsProcessedThisFrame = 0;
        
        // Directional offsets for 4-way movement (up, right, down, left)
        private readonly int[] dx4 = { 0, 1, 0, -1 };
        private readonly int[] dy4 = { 1, 0, -1, 0 };
        
        // Directional offsets for 8-way movement (including diagonals)
        private readonly int[] dx8 = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private readonly int[] dy8 = { 1, 1, 0, -1, -1, -1, 0, 1 };
        
        // Costs for movement (straight vs diagonal)
        private readonly float[] moveCosts = { 1.0f, 1.4142f, 1.0f, 1.4142f, 1.0f, 1.4142f, 1.0f, 1.4142f };
        
        // Initialize components
        private void Awake() 
        {
            // Get references if not set
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            if (roomGraph == null) roomGraph = FindObjectOfType<RoomGraph>();
            if (reservationTable == null) reservationTable = FindObjectOfType<ReservationTable>();
            if (pathCache == null) pathCache = FindObjectOfType<PathCache>();
            
            // Set default heuristic if not defined
            if (heuristic == null) 
            {
                heuristic = allowDiagonalMovement ? 
                    (IHeuristic)new ChebyshevHeuristic() : 
                    (IHeuristic)new ManhattanHeuristic();
            }
        }
        
        private void OnEnable() 
        {
            // Subscribe to events
            if (gridManager != null) gridManager.TileChanged += OnTileChanged;
            if (roomGraph != null) roomGraph.GraphChanged += OnRoomGraphChanged;
        }
        
        private void OnDisable() 
        {
            // Unsubscribe from events
            if (gridManager != null) gridManager.TileChanged -= OnTileChanged;
            if (roomGraph != null) roomGraph.GraphChanged -= OnRoomGraphChanged;
        }
        
        private void Update() 
        {
            // Reset counter each frame
            pathsProcessedThisFrame = 0;
            
            // Process pending requests up to the limit
            while (pendingRequests.Count > 0 && pathsProcessedThisFrame < maxPathsPerFrame) 
            {
                PathRequest request = pendingRequests.Dequeue();
                ProcessPathRequest(request);
                pathsProcessedThisFrame++;
            }
        }
        
        // Event handlers
        private void OnTileChanged(int tileIndex) 
        {
            // Path cache should handle invalidation automatically
        }
        
        private void OnRoomGraphChanged() 
        {
            // Room graph changes might invalidate room-level paths
            // The path cache should handle this 
        }
        
        // Queue a new path request
        public void RequestPath(PathRequest request) 
        {
            pendingRequests.Enqueue(request);
        }
        
        // Process a path request
        private void ProcessPathRequest(PathRequest request) 
        {
            // If start and goal are the same, return a simple path
            if (request.StartIndex == request.GoalIndex) 
            {
                // Create a simple path with just the start/goal position
                Vector3 position = gridManager.GridToWorld(gridManager.IndexToGrid(request.StartIndex));
                
                List<int> indices = new List<int> { request.StartIndex };
                List<Vector3> waypoints = new List<Vector3> { position };
                
                PathResult result = new PathResult(request, indices, waypoints);
                request.OnPathReady?.Invoke(result);
                return;
            }
            
            // Look up room information if not provided
            if (request.StartRoom == null)
                request.StartRoom = roomGraph.FindRoomContaining(request.StartIndex);
                
            if (request.GoalRoom == null)
                request.GoalRoom = roomGraph.FindRoomContaining(request.GoalIndex);
            
            // If start and goal are in the same room, use direct path
            if (request.StartRoom == request.GoalRoom) 
            {
                FindPathWithinRoom(request);
                return;
            }
            
            // For different rooms, use hierarchical path planning
            FindHierarchicalPath(request);
        }
        
        // Find a direct path within a single room
        private void FindPathWithinRoom(PathRequest request) 
        {
            // Check if the path is cached
            if (pathCache.TryGetTilePath(
                request.StartIndex, 
                request.GoalIndex, 
                request.CostProvider, 
                request.Agent, 
                out List<int> cachedIndices, 
                out List<Vector3> cachedWaypoints)) 
            {
                // Use cached path
                PathResult result = new PathResult(request, cachedIndices, cachedWaypoints);
                request.OnPathReady?.Invoke(result);
                return;
            }
            
            // Choose between job-based or regular A* pathfinding
            if (useJobSystem) 
            {
                StartCoroutine(FindPathWithJob(request));
            }
            else 
            {
                List<int> pathIndices = FindPath(
                    request.StartIndex, 
                    request.GoalIndex, 
                    request.CostProvider, 
                    request.Agent);
                    
                if (pathIndices != null && pathIndices.Count > 0) 
                {
                    // Convert path indices to world positions
                    List<Vector3> waypoints = new List<Vector3>();
                    foreach (int index in pathIndices) 
                    {
                        Vector3 worldPos = gridManager.GridToWorld(gridManager.IndexToGrid(index));
                        waypoints.Add(worldPos);
                    }
                    
                    // Cache the path
                    pathCache.CacheTilePath(
                        request.StartIndex, 
                        request.GoalIndex, 
                        request.CostProvider, 
                        request.Agent, 
                        pathIndices, 
                        waypoints);
                    
                    // Create and return result
                    PathResult result = new PathResult(request, pathIndices, waypoints);
                    request.OnPathReady?.Invoke(result);
                }
                else 
                {
                    // Path not found
                    PathResult result = new PathResult(request);
                    request.OnPathReady?.Invoke(result);
                }
            }
        }
        
        // Find a hierarchical path between different rooms
        private void FindHierarchicalPath(PathRequest request) 
        {
            // First, try to get a cached room-to-room path
            List<Door> doors = null;
            bool foundRoomPath = pathCache.TryGetRoomPath(request.StartRoom, request.GoalRoom, out doors);
            
            if (!foundRoomPath) 
            {
                // If not cached, compute the room-level path
                doors = roomGraph.FindPath(request.StartRoom, request.GoalRoom);
                
                if (doors != null) 
                {
                    // Cache the room path
                    pathCache.CacheRoomPath(request.StartRoom, request.GoalRoom, doors);
                }
            }
            
            if (doors == null || doors.Count == 0) 
            {
                // No path found between rooms
                PathResult result = new PathResult(request);
                request.OnPathReady?.Invoke(result);
                return;
            }
            
            // Now we have a room-level path, let's compute the fine-grained path
            // Divide the path into segments and solve each one
            List<int> fullPathIndices = new List<int>();
            List<Vector3> fullWaypoints = new List<Vector3>();
            
            // First segment: from start to first door
            int currentPos = request.StartIndex;
            Room currentRoom = request.StartRoom;
            
            // Add each door-to-door segment
            foreach (Door door in doors) 
            {
                // Find path from current position to the door
                List<int> segmentIndices = FindPath(
                    currentPos, 
                    door.TileIndex, 
                    request.CostProvider, 
                    request.Agent);
                    
                if (segmentIndices == null || segmentIndices.Count == 0) 
                {
                    // Failed to find path through this door
                    PathResult result = new PathResult(request);
                    request.OnPathReady?.Invoke(result);
                    return;
                }
                
                // Add segment to the full path (excluding the last point to avoid duplicates)
                for (int i = 0; i < segmentIndices.Count - 1; i++) 
                {
                    fullPathIndices.Add(segmentIndices[i]);
                    fullWaypoints.Add(gridManager.GridToWorld(gridManager.IndexToGrid(segmentIndices[i])));
                }
                
                // Update current position and room
                currentPos = door.TileIndex;
                currentRoom = door.GetOtherRoom(currentRoom);
            }
            
            // Final segment: from last door to goal
            List<int> finalSegment = FindPath(
                currentPos, 
                request.GoalIndex, 
                request.CostProvider, 
                request.Agent);
                
            if (finalSegment == null || finalSegment.Count == 0) 
            {
                // Failed to find path from last door to goal
                PathResult result = new PathResult(request);
                request.OnPathReady?.Invoke(result);
                return;
            }
            
            // Add final segment to the full path
            foreach (int index in finalSegment) 
            {
                fullPathIndices.Add(index);
                fullWaypoints.Add(gridManager.GridToWorld(gridManager.IndexToGrid(index)));
            }
            
            // Create and return result
            PathResult finalResult = new PathResult(request, fullPathIndices, fullWaypoints, doors);
            request.OnPathReady?.Invoke(finalResult);
        }
        
        // Core A* pathfinding algorithm
        private List<int> FindPath(int startIndex, int goalIndex, ICostProvider costProvider, AStarNavAgent agent) 
        {
            // Get pooled collections
            var openSet = PathfindingPools.GetQueue();
            var closedSet = new HashSet<int>();
            var cameFrom = new Dictionary<int, int>(); // Maps node to its parent
            var gScore = new Dictionary<int, float>(); // Cost from start to node
            
            try 
            {
                // Initialize
                openSet.Enqueue(new PathNode(startIndex, 0, 
                    heuristic.EstimateDistance(startIndex, goalIndex, gridManager), -1));
                
                gScore[startIndex] = 0;
                
                // Used for neighbors
                int[] dxArray = allowDiagonalMovement ? dx8 : dx4;
                int[] dyArray = allowDiagonalMovement ? dy8 : dy4;
                int numDirections = allowDiagonalMovement ? 8 : 4;
                
                // Main A* loop
                while (openSet.Count > 0) 
                {
                    // Get node with lowest f-score
                    PathNode current = openSet.Dequeue();
                    
                    // If we reached the goal
                    if (current.GridIndex == goalIndex) 
                    {
                        // Reconstruct path
                        List<int> path = new List<int>();
                        int currentIndex = current.GridIndex;
                        
                        while (currentIndex != startIndex) 
                        {
                            path.Add(currentIndex);
                            currentIndex = cameFrom[currentIndex];
                        }
                        
                        path.Add(startIndex);
                        path.Reverse();
                        
                        return path;
                    }
                    
                    // Add to closed set
                    closedSet.Add(current.GridIndex);
                    
                    // Get current grid position
                    Vector2Int currentPos = gridManager.IndexToGrid(current.GridIndex);
                    
                    // Check all neighbors
                    for (int i = 0; i < numDirections; i++) 
                    {
                        Vector2Int neighborPos = new Vector2Int(
                            currentPos.x + dxArray[i],
                            currentPos.y + dyArray[i]
                        );
                        
                        // Skip if out of bounds
                        if (neighborPos.x < 0 || neighborPos.x >= gridManager.Width ||
                            neighborPos.y < 0 || neighborPos.y >= gridManager.Height) 
                        {
                            continue;
                        }
                        
                        int neighborIndex = gridManager.GridToIndex(neighborPos);
                        
                        // Skip if in closed set
                        if (closedSet.Contains(neighborIndex)) 
                        {
                            continue;
                        }
                        
                        // Get tile and check if it's traversable
                        Tile tile = gridManager.GetTile(neighborIndex);
                        if (!costProvider.IsTraversable(tile, agent)) 
                        {
                            continue;
                        }
                        
                        // Check reservation table (cooperative A*)
                        if (reservationTable)
                        {
                            // Use time-based lookahead based on g-score
                            int timestep = Mathf.RoundToInt(current.G);
                            if (reservationTable.IsReserved(neighborIndex, timestep) && 
                                reservationTable.GetReservation(neighborIndex, timestep) != agent)
                            {
                                continue;
                            }
                        }
                        
                        // Calculate tentative g-score
                        float moveCost = allowDiagonalMovement ? moveCosts[i] : 1.0f;
                        float tileCost = costProvider.CalculateCost(tile, agent);
                        float tentativeGScore = current.G + moveCost * tileCost;
                        
                        // Check if this path is better
                        if (!gScore.TryGetValue(neighborIndex, out float existingGScore) || 
                            tentativeGScore < existingGScore) 
                        {
                            // Record this path
                            cameFrom[neighborIndex] = current.GridIndex;
                            gScore[neighborIndex] = tentativeGScore;
                            
                            // Calculate h-score (heuristic)
                            float hScore = heuristic.EstimateDistance(neighborIndex, goalIndex, gridManager);
                            
                            // Add to open set
                            openSet.Enqueue(new PathNode(neighborIndex, tentativeGScore, hScore, current.GridIndex));
                        }
                    }
                }
                
                // No path found
                return null;
            }
            finally 
            {
                // Return pooled objects
                PathfindingPools.ReturnQueue(openSet);
            }
        }
        
        // Find path using Unity's Job System for better performance
        private IEnumerator FindPathWithJob(PathRequest request) 
        {
            // Create the pathfinding job
            PathfindingJob job = new PathfindingJob 
            {
                StartIndex = request.StartIndex,
                GoalIndex = request.GoalIndex,
                Width = gridManager.Width,
                Height = gridManager.Height,
                AllowDiagonal = allowDiagonalMovement,
                AgentId = request.Agent != null ? request.Agent.GetInstanceID() : 0
            };
            
            // Copy the grid data to the job
            int gridSize = gridManager.Width * gridManager.Height;
            job.TileData = new NativeArray<Tile>(gridSize, Allocator.TempJob);
            
            for (int i = 0; i < gridSize; i++) 
            {
                Tile tile = gridManager.GetTile(i);
                
                // Apply cost provider logic here before passing to the job
                bool isTraversable = request.CostProvider.IsTraversable(tile, request.Agent);
                float cost = request.CostProvider.CalculateCost(tile, request.Agent);
                
                // Create modified tile for the job
                Tile jobTile = new Tile(isTraversable, cost, tile.Version, tile.Flags);
                job.TileData[i] = jobTile;
            }
            
            // Create result arrays
            int maxPathLength = gridSize;
            job.ResultPath = new NativeArray<int>(maxPathLength, Allocator.TempJob);
            job.PathLength = new NativeArray<int>(1, Allocator.TempJob);
            
            // Schedule the job
            JobHandle handle = job.Schedule();
            
            // Wait for job completion
            while (!handle.IsCompleted) 
            {
                yield return null;
            }
            
            // Complete the job
            handle.Complete();
            
            // Process results
            int pathLength = job.PathLength[0];
            List<int> pathIndices = new List<int>();
            List<Vector3> waypoints = new List<Vector3>();
            
            if (pathLength > 0) 
            {
                // Copy results to managed lists
                for (int i = 0; i < pathLength; i++) 
                {
                    int index = job.ResultPath[i];
                    pathIndices.Add(index);
                    waypoints.Add(gridManager.GridToWorld(gridManager.IndexToGrid(index)));
                }
                
                // Cache the path
                pathCache.CacheTilePath(
                    request.StartIndex, 
                    request.GoalIndex, 
                    request.CostProvider, 
                    request.Agent, 
                    pathIndices, 
                    waypoints);
                
                // Create and return result
                PathResult result = new PathResult(request, pathIndices, waypoints);
                request.OnPathReady?.Invoke(result);
            }
            else 
            {
                // Path not found
                PathResult result = new PathResult(request);
                request.OnPathReady?.Invoke(result);
            }
            
            // Dispose of native arrays
            job.TileData.Dispose();
            job.ResultPath.Dispose();
            job.PathLength.Dispose();
        }
        
        // Job to perform A* pathfinding in parallel
        private struct PathfindingJob : IJob 
        {
            public int StartIndex;
            public int GoalIndex;
            public int Width;
            public int Height;
            public int AgentId;
            public bool AllowDiagonal;
            
            public NativeArray<Tile> TileData;
            public NativeArray<int> ResultPath;
            public NativeArray<int> PathLength;
            
            public void Execute() 
            {
                // Implementation of A* in the job
                // This is similar to the regular A* but using NativeContainers
                // For brevity, a simplified implementation is shown
                
                // Create temporary collections for the algorithm
                NativeArray<int> openSet = new NativeArray<int>(TileData.Length, Allocator.Temp);
                NativeArray<float> fScore = new NativeArray<float>(TileData.Length, Allocator.Temp);
                NativeArray<float> gScore = new NativeArray<float>(TileData.Length, Allocator.Temp);
                NativeArray<int> cameFrom = new NativeArray<int>(TileData.Length, Allocator.Temp);
                NativeArray<bool> closedSet = new NativeArray<bool>(TileData.Length, Allocator.Temp);
                
                try 
                {
                    // Initialize
                    for (int i = 0; i < TileData.Length; i++) 
                    {
                        fScore[i] = float.MaxValue;
                        gScore[i] = float.MaxValue;
                        cameFrom[i] = -1;
                    }
                    
                    // Starting node
                    int openSetCount = 1;
                    openSet[0] = StartIndex;
                    gScore[StartIndex] = 0;
                    fScore[StartIndex] = EstimateDistance(StartIndex, GoalIndex);
                    
                    // Main loop
                    while (openSetCount > 0) 
                    {
                        // Find node with lowest fScore
                        int currentIndex = -1;
                        float lowestFScore = float.MaxValue;
                        
                        for (int i = 0; i < openSetCount; i++) 
                        {
                            int index = openSet[i];
                            if (fScore[index] < lowestFScore) 
                            {
                                lowestFScore = fScore[index];
                                currentIndex = index;
                            }
                        }
                        
                        // If we reached the goal
                        if (currentIndex == GoalIndex) 
                        {
                            // Reconstruct path
                            ReconstructPath(cameFrom);
                            return;
                        }
                        
                        // Remove from open set
                        for (int i = 0; i < openSetCount; i++) 
                        {
                            if (openSet[i] == currentIndex) 
                            {
                                openSet[i] = openSet[openSetCount - 1];
                                openSetCount--;
                                break;
                            }
                        }
                        
                        // Add to closed set
                        closedSet[currentIndex] = true;
                        
                        // Calculate grid position
                        int currentX = currentIndex % Width;
                        int currentY = currentIndex / Width;
                        
                        // Check neighbors
                        int[] dxArray = AllowDiagonal ? new[] { 0, 1, 1, 1, 0, -1, -1, -1 } : new[] { 0, 1, 0, -1 };
                        int[] dyArray = AllowDiagonal ? new[] { 1, 1, 0, -1, -1, -1, 0, 1 } : new[] { 1, 0, -1, 0 };
                        float[] moveCosts = AllowDiagonal ? 
                            new[] { 1.0f, 1.4142f, 1.0f, 1.4142f, 1.0f, 1.4142f, 1.0f, 1.4142f } : 
                            new[] { 1.0f, 1.0f, 1.0f, 1.0f };
                        
                        int numDirections = AllowDiagonal ? 8 : 4;
                        
                        for (int i = 0; i < numDirections; i++) 
                        {
                            int nx = currentX + dxArray[i];
                            int ny = currentY + dyArray[i];
                            
                            // Skip if out of bounds
                            if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) 
                                continue;
                                
                            int neighborIndex = ny * Width + nx;
                            
                            // Skip if in closed set
                            if (closedSet[neighborIndex])
                                continue;
                                
                            // Skip if not walkable
                            if (!TileData[neighborIndex].Walkable)
                                continue;
                                
                            // Calculate tentative gScore
                            float moveCost = moveCosts[i];
                            float tentativeGScore = gScore[currentIndex] + moveCost * TileData[neighborIndex].BaseCost;
                            
                            // Check if this path is better
                            if (tentativeGScore < gScore[neighborIndex]) 
                            {
                                // Record this path
                                cameFrom[neighborIndex] = currentIndex;
                                gScore[neighborIndex] = tentativeGScore;
                                fScore[neighborIndex] = tentativeGScore + EstimateDistance(neighborIndex, GoalIndex);
                                
                                // Add to open set if not already there
                                bool inOpenSet = false;
                                for (int j = 0; j < openSetCount; j++) 
                                {
                                    if (openSet[j] == neighborIndex) 
                                    {
                                        inOpenSet = true;
                                        break;
                                    }
                                }
                                
                                if (!inOpenSet && openSetCount < openSet.Length) 
                                {
                                    openSet[openSetCount] = neighborIndex;
                                    openSetCount++;
                                }
                            }
                        }
                    }
                    
                    // No path found
                    PathLength[0] = 0;
                }
                finally 
                {
                    // Dispose of temporary collections
                    openSet.Dispose();
                    fScore.Dispose();
                    gScore.Dispose();
                    closedSet.Dispose();
                }
            }
            
            // Estimate distance between two indices (Manhattan distance)
            private float EstimateDistance(int fromIndex, int toIndex) 
            {
                int fromX = fromIndex % Width;
                int fromY = fromIndex / Width;
                int toX = toIndex % Width;
                int toY = toIndex / Width;
                
                if (AllowDiagonal) 
                {
                    // Chebyshev distance for diagonal movement
                    return Mathf.Max(Mathf.Abs(fromX - toX), Mathf.Abs(fromY - toY));
                }
                else 
                {
                    // Manhattan distance for 4-way movement
                    return Mathf.Abs(fromX - toX) + Mathf.Abs(fromY - toY);
                }
            }
            
            // Reconstruct path from cameFrom array
            private void ReconstructPath(NativeArray<int> cameFrom) 
            {
                int current = GoalIndex;
                int pathIndex = 0;
                
                // Count path length
                int pathLength = 0;
                while (current != StartIndex && current != -1) 
                {
                    pathLength++;
                    current = cameFrom[current];
                }
                pathLength++; // Include start index
                
                // Make sure path fits in the result array
                pathLength = Mathf.Min(pathLength, ResultPath.Length);
                
                // Build path array in reverse
                current = GoalIndex;
                pathIndex = pathLength - 1;
                
                while (current != StartIndex && pathIndex >= 0) 
                {
                    ResultPath[pathIndex] = current;
                    pathIndex--;
                    current = cameFrom[current];
                }
                
                // Add start index
                if (pathIndex >= 0)
                    ResultPath[pathIndex] = StartIndex;
                
                // Store path length
                PathLength[0] = pathLength;
            }
        }
    }
}