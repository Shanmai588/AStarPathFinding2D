using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RTS.Pathfinding 
{
    /// <summary>
    /// Responsible for constructing the grid-based map with rooms and doors for 2D games.
    /// </summary>
    public class MapBuilder : MonoBehaviour 
    {
        // References to other components
        [SerializeField] private GridManager gridManager;
        [SerializeField] private RoomGraph roomGraph;
        
        // Map visualization
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private GameObject doorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private Transform tileContainer;
        
        // Tilemap integration (optional)
        [SerializeField] private bool useExistingTilemap = false;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private TileBase floorTile;
        [SerializeField] private TileBase wallTile;
        [SerializeField] private TileBase doorTile;
        
        // Map configuration
        [SerializeField] private bool visualizeMap = true;
        [SerializeField] private bool randomizeTileFlags = true;
        [SerializeField] private float zDepth = 0f; // Z-position for visualization
        
        // Room definitions for a simple hardcoded map
        [System.Serializable]
        public class RoomDefinition 
        {
            public string name;
            public BoundsInt bounds;
        }
        
        // Door definitions connecting rooms
        [System.Serializable]
        public class DoorDefinition 
        {
            public string room1Name;
            public string room2Name;
            public Vector2Int position;
            public float cost = 1.0f;
        }
        
        // Map definition
        [SerializeField] private List<RoomDefinition> roomDefinitions = new List<RoomDefinition>();
        [SerializeField] private List<DoorDefinition> doorDefinitions = new List<DoorDefinition>();
        
        // Dictionary to keep track of rooms by name
        private Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        
        private void Awake() 
        {
            // Get references if not set
            if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
            if (roomGraph == null) roomGraph = FindObjectOfType<RoomGraph>();
            
            // Create containers if needed
            if (visualizeMap && tileContainer == null) 
            {
                tileContainer = new GameObject("Tiles").transform;
                tileContainer.SetParent(transform);
            }
        }
        
        private void Start() 
        {
            // Build the map
            BuildMap();
        }
        
        /// <summary>
        /// Build the entire map
        /// </summary>
        public void BuildMap() 
        {
            if (useExistingTilemap && tilemap != null)
            {
                // Initialize grid from tilemap
                InitializeFromTilemap();
            }
            else
            {
                // First, make the entire grid unwalkable
                for (int x = 0; x < gridManager.Width; x++) 
                {
                    for (int y = 0; y < gridManager.Height; y++) 
                    {
                        int index = gridManager.GridToIndex(new Vector2Int(x, y));
                        gridManager.SetWalkable(index, false);
                    }
                }
                
                // Create rooms
                CreateRooms();
                
                // Create doors between rooms
                CreateDoors();
                
                // Optionally add some tile flags for different movement costs
                if (randomizeTileFlags) 
                {
                    AddRandomTileFlags();
                }
            }
            
            // Visualize the map
            if (visualizeMap) 
            {
                if (useExistingTilemap)
                {
                    // No need to visualize if we're using an existing tilemap
                    Debug.Log("Using existing tilemap for visualization");
                }
                else
                {
                    VisualizeMap();
                }
            }
        }
        
        /// <summary>
        /// Initialize the grid and rooms from an existing tilemap
        /// </summary>
        private void InitializeFromTilemap()
        {
            if (tilemap == null)
            {
                Debug.LogError("Tilemap reference is missing!");
                return;
            }
            
            // Get bounds of the tilemap
            BoundsInt bounds = tilemap.cellBounds;
            Debug.Log($"Tilemap bounds: {bounds}");
            
            // Initialize grid to match tilemap size
            int width = bounds.size.x;
            int height = bounds.size.y;
            
            // Create a mapping of tiles to walkable state
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3Int tilePos = new Vector3Int(bounds.x + x, bounds.y + y, 0);
                    Vector2Int gridPos = new Vector2Int(x, y);
                    int index = gridManager.GridToIndex(gridPos);
                    
                    TileBase tile = tilemap.GetTile(tilePos);
                    
                    // Set walkable based on tile type
                    bool isWalkable = IsWalkableTile(tile);
                    gridManager.SetWalkable(index, isWalkable);
                    
                    // Check if this is a door tile
                    if (IsDoorTile(tile))
                    {
                        // Mark for later door creation
                        // This would need to be expanded with room detection logic
                    }
                }
            }
            
            // Detect rooms in the tilemap - a more complex algorithm would be needed
            // For now, we'll create rooms from our manual definitions if available
            if (roomDefinitions.Count > 0)
            {
                CreateRoomsFromDefinitions();
                CreateDoors();
            }
            else
            {
                // Auto-detect rooms using flood fill
                DetectRoomsFromTilemap();
            }
        }
        
        // Check if a tile is walkable
        private bool IsWalkableTile(TileBase tile)
        {
            if (tile == null)
                return false;
                
            if (floorTile != null && wallTile != null)
            {
                // Use explicit tile references if available
                return tile == floorTile || tile == doorTile;
            }
            
            // Default: assume non-null tiles are walkable (except wall tiles)
            return tile != wallTile;
        }
        
        // Check if a tile is a door
        private bool IsDoorTile(TileBase tile)
        {
            return tile == doorTile;
        }
        
        // Auto-detect rooms using flood fill (simplified version)
        private void DetectRoomsFromTilemap()
        {
            Debug.Log("Auto-detecting rooms from tilemap...");
            
            // Tracking for visited tiles
            bool[,] visited = new bool[gridManager.Width, gridManager.Height];
            int roomId = 0;
            
            // Scan the grid for unvisited walkable tiles
            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    int index = gridManager.GridToIndex(pos);
                    
                    // Skip if already visited or not walkable
                    if (visited[x, y] || !gridManager.GetTile(index).Walkable)
                        continue;
                    
                    // Found a new room starting point
                    BoundsInt roomBounds = FloodFillRoom(pos, visited);
                    
                    // Create room if it's large enough
                    if (roomBounds.size.x > 1 && roomBounds.size.y > 1)
                    {
                        string roomName = $"Room_{roomId++}";
                        Room room = new Room(roomBounds);
                        rooms[roomName] = room;
                        roomGraph.AddRoom(room);
                        
                        Debug.Log($"Detected {roomName} with bounds {roomBounds}");
                    }
                }
            }
            
            // After detecting rooms, try to find doors between them
            DetectDoorsInTilemap();
        }
        
        // Flood fill to find room bounds (simplified)
        private BoundsInt FloodFillRoom(Vector2Int start, bool[,] visited)
        {
            // Initialize bounds with the start position
            int minX = start.x, maxX = start.x;
            int minY = start.y, maxY = start.y;
            
            // Queue for breadth-first traversal
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited[start.x, start.y] = true;
            
            // Flood fill
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                
                // Update bounds
                minX = Mathf.Min(minX, current.x);
                maxX = Mathf.Max(maxX, current.x);
                minY = Mathf.Min(minY, current.y);
                maxY = Mathf.Max(maxY, current.y);
                
                // Check neighbors (4-way connectivity)
                Vector2Int[] neighbors = new Vector2Int[]
                {
                    new Vector2Int(current.x + 1, current.y),
                    new Vector2Int(current.x - 1, current.y),
                    new Vector2Int(current.x, current.y + 1),
                    new Vector2Int(current.x, current.y - 1)
                };
                
                foreach (Vector2Int neighbor in neighbors)
                {
                    // Skip if out of bounds
                    if (neighbor.x < 0 || neighbor.x >= gridManager.Width ||
                        neighbor.y < 0 || neighbor.y >= gridManager.Height)
                        continue;
                    
                    // Skip if already visited
                    if (visited[neighbor.x, neighbor.y])
                        continue;
                    
                    // Check if walkable
                    int index = gridManager.GridToIndex(neighbor);
                    if (gridManager.GetTile(index).Walkable)
                    {
                        visited[neighbor.x, neighbor.y] = true;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            // Create bounds
            return new BoundsInt(
                minX, minY, 0,                         // Position (min)
                maxX - minX + 1, maxY - minY + 1, 1    // Size
            );
        }
        
        // Try to detect doors between rooms
        private void DetectDoorsInTilemap()
        {
            Debug.Log("Detecting doors between rooms...");
            
            // Get all rooms
            List<Room> allRooms = roomGraph.GetAllRooms();
            
            // Skip if not enough rooms
            if (allRooms.Count < 2)
                return;
            
            // Check for potential doors along room boundaries
            for (int i = 0; i < allRooms.Count; i++)
            {
                Room roomA = allRooms[i];
                
                for (int j = i + 1; j < allRooms.Count; j++)
                {
                    Room roomB = allRooms[j];
                    
                    // Check if rooms are adjacent
                    List<Vector2Int> adjacentTiles = FindAdjacentTiles(roomA, roomB);
                    
                    // Create doors at adjacent walkable tiles
                    foreach (Vector2Int pos in adjacentTiles)
                    {
                        int index = gridManager.GridToIndex(pos);
                        if (gridManager.GetTile(index).Walkable)
                        {
                            // Create a door
                            roomGraph.AddDoor(index, roomA, roomB);
                            Debug.Log($"Created door at {pos} between Room_{i} and Room_{j}");
                        }
                    }
                }
            }
        }
        
        // Find adjacent tiles between two rooms
        private List<Vector2Int> FindAdjacentTiles(Room roomA, Room roomB)
        {
            List<Vector2Int> adjacentTiles = new List<Vector2Int>();
            
            BoundsInt boundsA = roomA.Bounds;
            BoundsInt boundsB = roomB.Bounds;
            
            // Check if rooms are adjacent
            bool adjacentHorizontal = 
                (boundsA.xMin == boundsB.xMax || boundsA.xMax == boundsB.xMin) &&
                !(boundsA.yMax <= boundsB.yMin || boundsA.yMin >= boundsB.yMax);
                
            bool adjacentVertical = 
                (boundsA.yMin == boundsB.yMax || boundsA.yMax == boundsB.yMin) &&
                !(boundsA.xMax <= boundsB.xMin || boundsA.xMin >= boundsB.xMax);
            
            // Find adjacent tiles
            if (adjacentHorizontal)
            {
                int x = (boundsA.xMin == boundsB.xMax) ? boundsA.xMin : boundsA.xMax - 1;
                int minY = Mathf.Max(boundsA.yMin, boundsB.yMin);
                int maxY = Mathf.Min(boundsA.yMax, boundsB.yMax);
                
                for (int y = minY; y < maxY; y++)
                {
                    adjacentTiles.Add(new Vector2Int(x, y));
                }
            }
            
            if (adjacentVertical)
            {
                int y = (boundsA.yMin == boundsB.yMax) ? boundsA.yMin : boundsA.yMax - 1;
                int minX = Mathf.Max(boundsA.xMin, boundsB.xMin);
                int maxX = Mathf.Min(boundsA.xMax, boundsB.xMax);
                
                for (int x = minX; x < maxX; x++)
                {
                    adjacentTiles.Add(new Vector2Int(x, y));
                }
            }
            
            return adjacentTiles;
        }
        
        // Create rooms from manual definitions
        private void CreateRoomsFromDefinitions()
        {
            rooms.Clear();
            
            foreach (RoomDefinition def in roomDefinitions)
            {
                Room room = new Room(def.bounds);
                rooms[def.name] = room;
                roomGraph.AddRoom(room);
            }
        }
        
        // Create all rooms
        private void CreateRooms() 
        {
            rooms.Clear();
            
            foreach (RoomDefinition def in roomDefinitions) 
            {
                // Create room
                Room room = new Room(def.bounds);
                rooms[def.name] = room;
                
                // Add to room graph
                roomGraph.AddRoom(room);
                
                // Make tiles in the room walkable
                for (int x = def.bounds.xMin; x < def.bounds.xMax; x++) 
                {
                    for (int y = def.bounds.yMin; y < def.bounds.yMax; y++) 
                    {
                        int index = gridManager.GridToIndex(new Vector2Int(x, y));
                        gridManager.SetWalkable(index, true);
                    }
                }
            }
        }
        
        // Create all doors
        private void CreateDoors() 
        {
            foreach (DoorDefinition def in doorDefinitions) 
            {
                // Get the rooms
                if (!rooms.TryGetValue(def.room1Name, out Room room1) || 
                    !rooms.TryGetValue(def.room2Name, out Room room2)) 
                {
                    Debug.LogWarning($"Could not find rooms for door: {def.room1Name} -> {def.room2Name}");
                    continue;
                }
                
                // Get the door tile index
                int doorIndex = gridManager.GridToIndex(def.position);
                
                // Ensure the door position is walkable
                gridManager.SetWalkable(doorIndex, true);
                
                // Create the door
                Door door = roomGraph.AddDoor(doorIndex, room1, room2, def.cost);
                
                // Debug log
                Debug.Log($"Created door connecting {def.room1Name} to {def.room2Name} at {def.position}");
            }
        }
        
        // Add random tile flags for testing different cost providers
        private void AddRandomTileFlags() 
        {
            // Seed for consistent results
            Random.InitState(42);
            
            foreach (KeyValuePair<string, Room> entry in rooms) 
            {
                Room room = entry.Value;
                BoundsInt bounds = room.Bounds;
                
                for (int x = bounds.xMin; x < bounds.xMax; x++) 
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++) 
                    {
                        int index = gridManager.GridToIndex(new Vector2Int(x, y));
                        Tile tile = gridManager.GetTile(index);
                        
                        // Skip unwalkable tiles
                        if (!tile.Walkable)
                            continue;
                            
                        // 10% chance for mud
                        if (Random.value < 0.1f) 
                        {
                            gridManager.SetTileFlags(index, Tile.FLAG_MUD);
                        }
                        // 5% chance for poison
                        else if (Random.value < 0.05f) 
                        {
                            gridManager.SetTileFlags(index, Tile.FLAG_POISON);
                        }
                        // 15% chance for water
                        else if (Random.value < 0.15f) 
                        {
                            gridManager.SetTileFlags(index, Tile.FLAG_WATER);
                        }
                    }
                }
            }
        }
        
        // Visualize the map in the scene
        private void VisualizeMap() 
        {
            if (tilePrefab == null || doorPrefab == null || wallPrefab == null)
                return;
                
            // Clear existing visualization
            if (tileContainer != null) 
            {
                while (tileContainer.childCount > 0) 
                {
                    DestroyImmediate(tileContainer.GetChild(0).gameObject);
                }
            }
            
            // Create tiles for each cell in the grid
            for (int x = 0; x < gridManager.Width; x++) 
            {
                for (int y = 0; y < gridManager.Height; y++) 
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    int index = gridManager.GridToIndex(gridPos);
                    Tile tile = gridManager.GetTile(index);
                    
                    Vector2 worldPos = gridManager.GridToWorld2D(gridPos);
                    Vector3 position = new Vector3(worldPos.x, worldPos.y, zDepth);
                    
                    GameObject tileObj = null;
                    
                    // Create visual based on tile type
                    if (tile.Walkable) 
                    {
                        // Check if this is a door
                        bool isDoor = false;
                        foreach (KeyValuePair<string, Room> entry in rooms) 
                        {
                            Room room = entry.Value;
                            foreach (Door door in room.Doors) 
                            {
                                if (door.TileIndex == index) 
                                {
                                    // Create door visual
                                    tileObj = Instantiate(doorPrefab, position, Quaternion.identity, tileContainer);
                                    tileObj.name = $"Door_{x}_{y}";
                                    isDoor = true;
                                    break;
                                }
                            }
                            if (isDoor) break;
                        }
                        
                        // Regular walkable tile
                        if (!isDoor) 
                        {
                            tileObj = Instantiate(tilePrefab, position, Quaternion.identity, tileContainer);
                            tileObj.name = $"Tile_{x}_{y}";
                            
                            // Set color based on flags
                            SpriteRenderer renderer = tileObj.GetComponent<SpriteRenderer>();
                            if (renderer != null) 
                            {
                                if (tile.HasFlag(Tile.FLAG_MUD)) 
                                {
                                    renderer.color = new Color(0.5f, 0.25f, 0, 1); // Brown
                                }
                                else if (tile.HasFlag(Tile.FLAG_POISON)) 
                                {
                                    renderer.color = new Color(0, 0.7f, 0, 1); // Green
                                }
                                else if (tile.HasFlag(Tile.FLAG_WATER)) 
                                {
                                    renderer.color = new Color(0, 0.5f, 1, 1); // Blue
                                }
                                else 
                                {
                                    renderer.color = Color.gray;
                                }
                            }
                        }
                    }
                    else 
                    {
                        // Wall or obstacle
                        tileObj = Instantiate(wallPrefab, position, Quaternion.identity, tileContainer);
                        tileObj.name = $"Wall_{x}_{y}";
                    }
                }
            }
        }
        
        /// <summary>
        /// Add a new room at runtime
        /// </summary>
        public Room AddRoom(string name, BoundsInt bounds) 
        {
            // Create room
            Room room = new Room(bounds);
            rooms[name] = room;
            
            // Add to room graph
            roomGraph.AddRoom(room);
            
            // Make tiles in the room walkable
            for (int x = bounds.xMin; x < bounds.xMax; x++) 
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++) 
                {
                    int index = gridManager.GridToIndex(new Vector2Int(x, y));
                    gridManager.SetWalkable(index, true);
                }
            }
            
            // Update visualization
            if (visualizeMap) 
            {
                VisualizeMap();
            }
            
            return room;
        }
        
        /// <summary>
        /// Add a new door at runtime
        /// </summary>
        public Door AddDoor(string room1Name, string room2Name, Vector2Int position, float cost = 1.0f) 
        {
            // Get the rooms
            if (!rooms.TryGetValue(room1Name, out Room room1) || 
                !rooms.TryGetValue(room2Name, out Room room2)) 
            {
                Debug.LogWarning($"Could not find rooms for door: {room1Name} -> {room2Name}");
                return null;
            }
            
            // Get the door tile index
            int doorIndex = gridManager.GridToIndex(position);
            
            // Ensure the door position is walkable
            gridManager.SetWalkable(doorIndex, true);
            
            // Create the door
            Door door = roomGraph.AddDoor(doorIndex, room1, room2, cost);
            
            // Update visualization
            if (visualizeMap) 
            {
                VisualizeMap();
            }
            
            return door;
        }
        
        /// <summary>
        /// Get a room by name
        /// </summary>
        public Room GetRoom(string name) 
        {
            if (rooms.TryGetValue(name, out Room room)) 
            {
                return room;
            }
            
            return null;
        }
        
        /// <summary>
        /// Create a default map for testing
        /// </summary>
        public void CreateDefaultMap() 
        {
            roomDefinitions.Clear();
            doorDefinitions.Clear();
            
            // Define rooms
            roomDefinitions.Add(new RoomDefinition 
            {
                name = "Entrance",
                bounds = new BoundsInt(5, 5, 0, 10, 8, 1)
            });
            
            roomDefinitions.Add(new RoomDefinition 
            {
                name = "Hallway",
                bounds = new BoundsInt(15, 8, 0, 20, 5, 1)
            });
            
            roomDefinitions.Add(new RoomDefinition 
            {
                name = "MainRoom",
                bounds = new BoundsInt(35, 5, 0, 15, 15, 1)
            });
            
            roomDefinitions.Add(new RoomDefinition 
            {
                name = "SideRoom",
                bounds = new BoundsInt(20, 20, 0, 10, 10, 1)
            });
            
            // Define doors
            doorDefinitions.Add(new DoorDefinition 
            {
                room1Name = "Entrance",
                room2Name = "Hallway",
                position = new Vector2Int(15, 10),
                cost = 1.0f
            });
            
            doorDefinitions.Add(new DoorDefinition 
            {
                room1Name = "Hallway",
                room2Name = "MainRoom",
                position = new Vector2Int(35, 10),
                cost = 1.0f
            });
            
            doorDefinitions.Add(new DoorDefinition 
            {
                room1Name = "Hallway",
                room2Name = "SideRoom",
                position = new Vector2Int(25, 20),
                cost = 1.5f
            });
            
            // Build the map
            BuildMap();
        }
        
        /// <summary>
        /// Method to toggle a door's state (open/closed)
        /// </summary>
        public void ToggleDoor(int doorIndex) 
        {
            foreach (KeyValuePair<string, Room> entry in rooms) 
            {
                Room room = entry.Value;
                foreach (Door door in room.Doors) 
                {
                    if (door.TileIndex == doorIndex) 
                    {
                        door.SetOpen(!door.IsOpen);
                        Debug.Log($"Door toggled to {(door.IsOpen ? "open" : "closed")}");
                        return;
                    }
                }
            }
        }
    }
}