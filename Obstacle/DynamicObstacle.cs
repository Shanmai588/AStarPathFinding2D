using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding 
{
    // Marks a game object as an obstacle that blocks tiles at runtime
    public class DynamicObstacle : MonoBehaviour 
    {
        // References
        [SerializeField] private ObstacleTracker obstacleTracker;
        
        // Configuration
        [SerializeField] private Vector2Int size = new Vector2Int(1, 1);
        [SerializeField] private bool blockWalkable = true;
        [SerializeField] private byte tileFlags = 0;
        
        // List of occupied tile indices
        private List<int> occupiedTiles = new List<int>();
        private Vector3 lastPosition;
        private bool isActive = false;
        
        // Get list of occupied tiles
        public List<int> OccupiedTiles => new List<int>(occupiedTiles);
        
        private void Awake() 
        {
            // Find obstacle tracker if not set
            if (obstacleTracker == null)
                obstacleTracker = FindObjectOfType<ObstacleTracker>();
                
            lastPosition = transform.position;
        }
        
        private void OnEnable() 
        {
            // Register with obstacle tracker
            if (obstacleTracker != null) 
            {
                obstacleTracker.Register(this);
                isActive = true;
                UpdateOccupiedTiles();
            }
        }
        
        private void OnDisable() 
        {
            // Unregister from obstacle tracker
            if (obstacleTracker != null && isActive) 
            {
                obstacleTracker.Unregister(this);
                isActive = false;
            }
        }
        
        private void Update() 
        {
            // Check if position has changed
            if (transform.position != lastPosition) 
            {
                UpdateOccupiedTiles();
                lastPosition = transform.position;
            }
        }
        
        // Update the list of tiles occupied by this obstacle
        public void UpdateOccupiedTiles() 
        {
            if (obstacleTracker == null || !isActive)
                return;
                
            // Clear previous tiles
            ClearOccupiedTiles();
            
            // Calculate new occupied tiles
            Vector2Int centerTile = obstacleTracker.WorldToGrid(transform.position);
            
            // Calculate the minimum and maximum tiles
            int minX = centerTile.x - (size.x / 2);
            int maxX = minX + size.x;
            int minY = centerTile.y - (size.y / 2);
            int maxY = minY + size.y;
            
            // Occupy the tiles
            for (int x = minX; x < maxX; x++) 
            {
                for (int y = minY; y < maxY; y++) 
                {
                    Vector2Int tile = new Vector2Int(x, y);
                    int tileIndex = obstacleTracker.GridToIndex(tile);
                    
                    if (tileIndex >= 0) 
                    {
                        occupiedTiles.Add(tileIndex);
                        
                        // Update tile state
                        if (blockWalkable) 
                        {
                            obstacleTracker.SetWalkable(tileIndex, false);
                        }
                        
                        if (tileFlags != 0) 
                        {
                            obstacleTracker.SetTileFlags(tileIndex, tileFlags);
                        }
                    }
                }
            }
        }
        
        // Clear all occupied tiles
        private void ClearOccupiedTiles() 
        {
            if (obstacleTracker == null || !isActive)
                return;
                
            foreach (int tileIndex in occupiedTiles) 
            {
                // Reset tile state
                if (blockWalkable) 
                {
                    obstacleTracker.SetWalkable(tileIndex, true);
                }
                
                if (tileFlags != 0) 
                {
                    obstacleTracker.SetTileFlags(tileIndex, 0);
                }
            }
            
            occupiedTiles.Clear();
        }
        
        // Set the size of the obstacle
        public void SetSize(Vector2Int newSize) 
        {
            size = newSize;
            UpdateOccupiedTiles();
        }
        
        // Set whether the obstacle blocks walkable tiles
        public void SetBlockWalkable(bool blocks) 
        {
            if (blockWalkable != blocks) 
            {
                blockWalkable = blocks;
                UpdateOccupiedTiles();
            }
        }
        
        // Set the tile flags for the obstacle
        public void SetTileFlags(byte flags) 
        {
            if (tileFlags != flags) 
            {
                tileFlags = flags;
                UpdateOccupiedTiles();
            }
        }
        
        private void OnDrawGizmosSelected() 
        {
            // Draw the bounds of the obstacle
            Gizmos.color = Color.red;
            
            if (obstacleTracker != null) 
            {
                Vector2Int centerTile = obstacleTracker.WorldToGrid(transform.position);
                
                int minX = centerTile.x - (size.x / 2);
                int maxX = minX + size.x;
                int minY = centerTile.y - (size.y / 2);
                int maxY = minY + size.y;
                
                for (int x = minX; x < maxX; x++) 
                {
                    for (int y = minY; y < maxY; y++) 
                    {
                        Vector2Int tile = new Vector2Int(x, y);
                        Vector3 worldPos = obstacleTracker.GridToWorld(tile);
                        
                        Gizmos.DrawWireCube(worldPos, new Vector3(
                            obstacleTracker.CellSize,
                            0.1f,
                            obstacleTracker.CellSize
                        ));
                    }
                }
            }
        }
    }
}