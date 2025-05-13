using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding 
{
    // Manages dynamic obstacles and converts their changes to GridManager updates
    public class ObstacleTracker : MonoBehaviour 
    {
        // References
        [SerializeField] private GridManager gridManager;
        
        // Track registered obstacles
        private HashSet<DynamicObstacle> registeredObstacles = new HashSet<DynamicObstacle>();
        
        // Mapping from tile indices to the obstacles occupying them
        private Dictionary<int, List<DynamicObstacle>> occupancyMap = new Dictionary<int, List<DynamicObstacle>>();
        
        // Properties for convenience
        public float CellSize => gridManager.CellSize;
        
        private void Awake() 
        {
            // Get reference if not set
            if (gridManager == null)
                gridManager = FindObjectOfType<GridManager>();
        }
        
        // Register an obstacle
        public void Register(DynamicObstacle obstacle) 
        {
            if (registeredObstacles.Contains(obstacle))
                return;
                
            registeredObstacles.Add(obstacle);
            
            // Update tile occupancy
            foreach (int tileIndex in obstacle.OccupiedTiles) 
            {
                AddToOccupancyMap(tileIndex, obstacle);
            }
        }
        
        // Unregister an obstacle
        public void Unregister(DynamicObstacle obstacle) 
        {
            if (!registeredObstacles.Contains(obstacle))
                return;
                
            registeredObstacles.Remove(obstacle);
            
            // Clear tile occupancy
            foreach (int tileIndex in obstacle.OccupiedTiles) 
            {
                RemoveFromOccupancyMap(tileIndex, obstacle);
            }
        }
        
        // Set the walkable state of a tile
        public void SetWalkable(int tileIndex, bool walkable) 
        {
            if (gridManager == null)
                return;
                
            // If we're trying to set a tile to walkable, check if any other obstacles are blocking it
            if (walkable && occupancyMap.TryGetValue(tileIndex, out List<DynamicObstacle> obstacles) && obstacles.Count > 0) 
            {
                // Still blocked by other obstacles
                return;
            }
            
            // Update the grid
            gridManager.SetWalkable(tileIndex, walkable);
        }
        
        // Set the flags of a tile
        public void SetTileFlags(int tileIndex, byte flags) 
        {
            if (gridManager == null)
                return;
                
            // Get current tile
            Tile tile = gridManager.GetTile(tileIndex);
            
            // Combine flags from all obstacles on this tile
            byte combinedFlags = 0;
            
            if (occupancyMap.TryGetValue(tileIndex, out List<DynamicObstacle> obstacles)) 
            {
                foreach (DynamicObstacle obstacle in obstacles) 
                {
                    // Combine flags (bitwise OR)
                    combinedFlags |= flags;
                }
            }
            
            // Update the grid if flags have changed
            if (tile.Flags != combinedFlags) 
            {
                gridManager.SetTileFlags(tileIndex, combinedFlags);
            }
        }
        
        // Add an obstacle to the occupancy map
        private void AddToOccupancyMap(int tileIndex, DynamicObstacle obstacle) 
        {
            if (!occupancyMap.TryGetValue(tileIndex, out List<DynamicObstacle> obstacles)) 
            {
                obstacles = new List<DynamicObstacle>();
                occupancyMap[tileIndex] = obstacles;
            }
            
            if (!obstacles.Contains(obstacle)) 
            {
                obstacles.Add(obstacle);
            }
        }
        
        // Remove an obstacle from the occupancy map
        private void RemoveFromOccupancyMap(int tileIndex, DynamicObstacle obstacle) 
        {
            if (occupancyMap.TryGetValue(tileIndex, out List<DynamicObstacle> obstacles)) 
            {
                obstacles.Remove(obstacle);
                
                if (obstacles.Count == 0) 
                {
                    occupancyMap.Remove(tileIndex);
                    
                    // Restore walkable state if this was the last obstacle
                    SetWalkable(tileIndex, true);
                }
            }
        }
        
        // Convert world position to grid coordinates (delegate to GridManager)
        public Vector2Int WorldToGrid(Vector3 worldPosition) 
        {
            return gridManager.WorldToGrid(worldPosition);
        }
        
        // Convert grid coordinates to world position (delegate to GridManager)
        public Vector3 GridToWorld(Vector2Int gridPosition) 
        {
            return gridManager.GridToWorld(gridPosition);
        }
        
        // Convert grid coordinates to flat index (delegate to GridManager)
        public int GridToIndex(Vector2Int gridPosition) 
        {
            return gridManager.GridToIndex(gridPosition);
        }
        
        // Check if a tile is occupied by any obstacle
        public bool IsTileOccupied(int tileIndex) 
        {
            return occupancyMap.ContainsKey(tileIndex);
        }
        
        // Get all obstacles occupying a tile
        public List<DynamicObstacle> GetObstaclesAtTile(int tileIndex) 
        {
            if (occupancyMap.TryGetValue(tileIndex, out List<DynamicObstacle> obstacles)) 
            {
                return new List<DynamicObstacle>(obstacles);
            }
            
            return new List<DynamicObstacle>();
        }
    }
}