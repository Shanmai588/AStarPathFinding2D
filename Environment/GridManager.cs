using System;
using UnityEngine;
using Unity.Collections;

namespace RTS.Pathfinding 
{
    /// <summary>
    /// Manages a 2D grid of tiles for pathfinding.
    /// </summary>
    public class GridManager : MonoBehaviour 
    {
        [SerializeField] private int width = 100;
        [SerializeField] private int height = 100;
        [SerializeField] private float cellSize = 1.0f;
        [SerializeField] private float zDepth = 0f; // The Z depth to use for world positioning
        
        // Event for notifying when tiles change (Observer pattern)
        public event Action<int> TileChanged;
        
        // Holds all tile data for the grid
        private NativeArray<Tile> tiles;
        
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        
        private void Awake() 
        {
            InitializeGrid();
        }
        
        private void OnDestroy() 
        {
            // Clean up native memory when destroyed
            if (tiles.IsCreated)
                tiles.Dispose();
        }
        
        private void InitializeGrid() 
        {
            // Allocate native array for tiles
            tiles = new NativeArray<Tile>(width * height, Allocator.Persistent);
            
            // Initialize with default walkable tiles
            for (int i = 0; i < tiles.Length; i++) 
            {
                tiles[i] = new Tile(true, 1.0f, 0, 0);
            }
        }
        
        /// <summary>
        /// Converts a world position to grid coordinates in 2D space.
        /// </summary>
        public Vector2Int WorldToGrid(Vector2 worldPosition) 
        {
            int x = Mathf.FloorToInt(worldPosition.x / cellSize);
            int y = Mathf.FloorToInt(worldPosition.y / cellSize);
            
            // Clamp to grid bounds
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            
            return new Vector2Int(x, y);
        }
        
        /// <summary>
        /// Overload for Vector3 input - extracts X and Y components.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPosition) 
        {
            return WorldToGrid(new Vector2(worldPosition.x, worldPosition.y));
        }
        
        /// <summary>
        /// Converts grid coordinates to world position (center of cell) in 2D space.
        /// </summary>
        public Vector2 GridToWorld2D(Vector2Int gridPosition) 
        {
            float x = gridPosition.x * cellSize + cellSize * 0.5f;
            float y = gridPosition.y * cellSize + cellSize * 0.5f;
            
            return new Vector2(x, y);
        }
        
        /// <summary>
        /// Converts grid coordinates to world position with Z depth.
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPosition) 
        {
            Vector2 pos2D = GridToWorld2D(gridPosition);
            return new Vector3(pos2D.x, pos2D.y, zDepth);
        }
        
        /// <summary>
        /// Converts grid coordinates to a flat index for array access.
        /// </summary>
        public int GridToIndex(Vector2Int gridPosition) 
        {
            return gridPosition.y * width + gridPosition.x;
        }
        
        /// <summary>
        /// Converts a flat index to grid coordinates.
        /// </summary>
        public Vector2Int IndexToGrid(int index) 
        {
            int y = index / width;
            int x = index % width;
            return new Vector2Int(x, y);
        }
        
        /// <summary>
        /// Gets a tile at the specified grid coordinates.
        /// </summary>
        public Tile GetTile(Vector2Int gridPosition) 
        {
            int index = GridToIndex(gridPosition);
            return GetTile(index);
        }
        
        /// <summary>
        /// Gets a tile at the specified index.
        /// </summary>
        public Tile GetTile(int index) 
        {
            if (index < 0 || index >= tiles.Length)
                return new Tile(false, float.MaxValue, 0, 0); // Invalid tile
                
            return tiles[index];
        }
        
        /// <summary>
        /// Sets a tile's walkable state.
        /// </summary>
        public void SetWalkable(int index, bool walkable) 
        {
            if (index < 0 || index >= tiles.Length)
                return;
                
            Tile oldTile = tiles[index];
            if (oldTile.Walkable != walkable) 
            {
                // Create new tile with updated walkable state and version
                tiles[index] = oldTile.WithWalkable(walkable);
                
                // Notify listeners that tile has changed
                TileChanged?.Invoke(index);
            }
        }
        
        /// <summary>
        /// Sets a tile's flags.
        /// </summary>
        public void SetTileFlags(int index, byte flags) 
        {
            if (index < 0 || index >= tiles.Length)
                return;
                
            Tile oldTile = tiles[index];
            if (oldTile.Flags != flags) 
            {
                // Create new tile with updated flags and version
                tiles[index] = oldTile.WithFlags(flags);
                
                // Notify listeners that tile has changed
                TileChanged?.Invoke(index);
            }
        }
        
        /// <summary>
        /// Finds the nearest walkable tile to the given index.
        /// </summary>
        public int FindNearestWalkableTile(int startIndex, int maxSearchRadius = 10) 
        {
            if (startIndex < 0 || startIndex >= tiles.Length)
                return -1;
                
            // If the tile is already walkable, return it
            if (tiles[startIndex].Walkable)
                return startIndex;
                
            Vector2Int startPos = IndexToGrid(startIndex);
            
            // Search in expanding squares around the start position
            for (int radius = 1; radius <= maxSearchRadius; radius++) 
            {
                // Check all tiles at this radius distance
                for (int dx = -radius; dx <= radius; dx++) 
                {
                    for (int dy = -radius; dy <= radius; dy++) 
                    {
                        // Only check tiles exactly at the current radius
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                            continue;
                            
                        Vector2Int checkPos = new Vector2Int(startPos.x + dx, startPos.y + dy);
                        
                        // Skip if out of bounds
                        if (checkPos.x < 0 || checkPos.x >= width || checkPos.y < 0 || checkPos.y >= height)
                            continue;
                            
                        int checkIndex = GridToIndex(checkPos);
                        if (tiles[checkIndex].Walkable)
                            return checkIndex;
                    }
                }
            }
            
            // No walkable tile found within the search radius
            return -1;
        }

        /// <summary>
        /// Finds the nearest walkable tile to the given world position.
        /// </summary>
        public int FindNearestWalkableTile(Vector2 worldPosition, int maxSearchRadius = 10)
        {
            Vector2Int gridPos = WorldToGrid(worldPosition);
            int tileIndex = GridToIndex(gridPos);
            return FindNearestWalkableTile(tileIndex, maxSearchRadius);
        }
        
        /// <summary>
        /// Initializes the grid from a Unity Tilemap.
        /// </summary>
        public void InitializeFromTilemap(UnityEngine.Tilemaps.Tilemap tilemap, Func<UnityEngine.Tilemaps.TileBase, bool> isWalkableFunc)
        {
            // Get the bounds of the tilemap
            var bounds = tilemap.cellBounds;
            
            // Resize the grid if needed
            width = bounds.size.x;
            height = bounds.size.y;
            
            // Reinitialize the grid
            if (tiles.IsCreated)
                tiles.Dispose();
                
            tiles = new NativeArray<Tile>(width * height, Allocator.Persistent);
            
            // Initialize tiles based on the tilemap
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3Int tilePos = new Vector3Int(bounds.x + x, bounds.y + y, 0);
                    Vector2Int gridPos = new Vector2Int(x, y);
                    
                    var tileBase = tilemap.GetTile(tilePos);
                    bool isWalkable = tileBase != null && isWalkableFunc(tileBase);
                    
                    int index = GridToIndex(gridPos);
                    tiles[index] = new Tile(isWalkable, 1.0f, 0, 0);
                }
            }
        }
        
        // Helper method to draw grid in editor
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
                
            Gizmos.color = Color.gray;
            
            // Draw grid lines
            for (int x = 0; x <= width; x++)
            {
                Gizmos.DrawLine(
                    new Vector3(x * cellSize, 0, zDepth),
                    new Vector3(x * cellSize, height * cellSize, zDepth)
                );
            }
            
            for (int y = 0; y <= height; y++)
            {
                Gizmos.DrawLine(
                    new Vector3(0, y * cellSize, zDepth),
                    new Vector3(width * cellSize, y * cellSize, zDepth)
                );
            }
            
            // Draw tile states if tiles array is initialized
            if (tiles.IsCreated)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector2Int gridPos = new Vector2Int(x, y);
                        int index = GridToIndex(gridPos);
                        
                        if (index >= 0 && index < tiles.Length)
                        {
                            Tile tile = tiles[index];
                            Vector3 worldPos = GridToWorld(gridPos);
                            
                            // Color based on tile properties
                            if (!tile.Walkable)
                            {
                                Gizmos.color = new Color(1, 0, 0, 0.3f); // Red for obstacles
                                Gizmos.DrawCube(worldPos, new Vector3(cellSize * 0.8f, cellSize * 0.8f, 0.1f));
                            }
                            else if (tile.HasFlag(Tile.FLAG_MUD))
                            {
                                Gizmos.color = new Color(0.5f, 0.25f, 0, 0.3f); // Brown for mud
                                Gizmos.DrawCube(worldPos, new Vector3(cellSize * 0.8f, cellSize * 0.8f, 0.1f));
                            }
                            else if (tile.HasFlag(Tile.FLAG_POISON))
                            {
                                Gizmos.color = new Color(0, 0.7f, 0, 0.3f); // Green for poison
                                Gizmos.DrawCube(worldPos, new Vector3(cellSize * 0.8f, cellSize * 0.8f, 0.1f));
                            }
                        }
                    }
                }
            }
        }
    }
}