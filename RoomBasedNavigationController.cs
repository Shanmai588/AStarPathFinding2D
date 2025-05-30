using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class RoomBasedNavigationController : MonoBehaviour
{
    private GridManager gridManager;
    private PathRequestManager pathRequestManager;
    private ReservationTable reservationTable;
    private EventBus eventBus;
    private ReachabilityAnalyzer reachabilityAnalyzer;
    private HierarchicalPathfinder pathfinder;
    private PathCache pathCache;
    private List<Agent> registeredAgents;

    [SerializeField] private int maxRequestsPerFrame = 5;
    [SerializeField] private float reservationTimeStep = 0.5f;
    [SerializeField] private bool enableDebugVisualization = true;

    void Awake()
    {
        eventBus = new EventBus();
        gridManager = new GridManager(eventBus);
        pathCache = new PathCache();
        pathfinder = new HierarchicalPathfinder(pathCache, gridManager);
        pathRequestManager = new PathRequestManager(pathfinder, maxRequestsPerFrame);
        reservationTable = new ReservationTable(reservationTimeStep);
        reachabilityAnalyzer = new ReachabilityAnalyzer(gridManager);
        registeredAgents = new List<Agent>();

        // Register path cache for tile change events
        gridManager.RegisterForTileChanges(pathCache);
    }

    void Start()
    {
        // Initialize pathfinder with rooms
        pathfinder.Initialize(gridManager.GetAllRooms());
    }

    void Update()
    {
        CoordinatePathfinding();
        ManageReservations();
        HandleEvents();
    }

    public Path GetPath(PathRequest request)
    {
        pathRequestManager.QueueRequest(request);
        return null; // Async processing
    }

    public void RegisterAgent(Agent agent)
    {
        if (!registeredAgents.Contains(agent))
            registeredAgents.Add(agent);
    }

    public void UnregisterAgent(Agent agent)
    {
        registeredAgents.Remove(agent);
        reservationTable.ClearReservations(agent.AgentId);
    }

    public Room GetRoom(int roomId) => gridManager.GetRoom(roomId);
    public Tile GetTile(int roomId, int x, int y) => gridManager.GetTile(roomId, x, y);
    public Dictionary<int, Room> GetAllRooms() => gridManager.GetAllRooms();

    public Tile[,] GetTilesByRoom(int roomId)
    {
        var room = GetRoom(roomId);
        if (room == null) return null;

        var tiles = new Tile[room.Width, room.Height];
        for (int x = 0; x < room.Width; x++)
        {
            for (int y = 0; y < room.Height; y++)
            {
                tiles[x, y] = room.GetTile(x, y);
            }
        }
        return tiles;
    }

    public Bounds GetRoomBounds(int roomId)
    {
        var room = GetRoom(roomId);
        if (room == null) return new Bounds();

        var center = room.WorldPosition + new Vector2(room.Width / 2f, room.Height / 2f);
        return new Bounds(new Vector3(center.x, center.y, 0), new Vector3(room.Width, room.Height, 1));
    }

    public List<Door> GetAllDoors()
    {
        var allDoors = new List<Door>();
        foreach (var room in gridManager.GetAllRooms().Values)
        {
            allDoors.AddRange(room.GetDoors());
        }
        return allDoors;
    }

    public List<Door> GetDoorsInRoom(int roomId)
    {
        var room = GetRoom(roomId);
        return room?.GetDoors() ?? new List<Door>();
    }

    public List<Reservation> GetActiveReservations() => reservationTable.GetActiveReservations();

    public List<Reservation> GetActiveReservationsInRoom(int roomId)
    {
        return GetActiveReservations().Where(r => 
        {
            // Check if position is in specified room
            var room = GetRoom(roomId);
            return room != null && room.IsValidPosition(r.Position.x, r.Position.y);
        }).ToList();
    }

    public List<Path> GetActivePaths()
    {
        var paths = new List<Path>();
        foreach (var agent in registeredAgents)
        {
            var path = agent.GetCurrentPath();
            if (path != null && path.IsValid)
                paths.Add(path);
        }
        return paths;
    }

    public List<Agent> GetRegisteredAgents() => new List<Agent>(registeredAgents);

    public bool IsPositionWalkable(int roomId, Vector2Int position)
    {
        var tile = GetTile(roomId, position.x, position.y);
        return tile != null && tile.IsWalkable && !tile.IsOccupied();
    }

    public Vector2Int WorldToGrid(Vector2 worldPos, out int roomId)
    {
        // Find which room contains this world position
        foreach (var kvp in gridManager.GetAllRooms())
        {
            var room = kvp.Value;
            var gridPos = room.WorldToGrid(worldPos);
            
            if (room.IsValidPosition(gridPos.x, gridPos.y))
            {
                roomId = room.RoomId;
                return gridPos;
            }
        }

        // If no room found at exact position, find closest room
        roomId = -1;
        float minDistance = float.MaxValue;
        Room closestRoom = null;
        
        foreach (var kvp in gridManager.GetAllRooms())
        {
            var room = kvp.Value;
            var roomCenter = room.WorldPosition + new Vector2(room.Width / 2f, room.Height / 2f);
            float distance = Vector2.Distance(worldPos, roomCenter);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closestRoom = room;
                roomId = room.RoomId;
            }
        }

        if (closestRoom != null)
        {
            // Return the grid position even if it's outside the room bounds
            // This allows for negative coordinates
            return closestRoom.WorldToGrid(worldPos);
        }

        Debug.LogWarning($"No room found for world position {worldPos}");
        return Vector2Int.zero;
    }

    public Vector2 GridToWorld(int roomId, Vector2Int gridPos)
    {
        var room = GetRoom(roomId);
        return room?.GridToWorld(gridPos) ?? Vector2.zero;
    }

    public Vector2Int FindClosestReachablePoint(Vector2Int start, Vector2Int target, 
                                               int roomId, MovementCapabilities capabilities)
    {
        return reachabilityAnalyzer.FindClosestReachablePoint(start, target, roomId, capabilities);
    }

    private void CoordinatePathfinding()
    {
        pathRequestManager.ProcessRequests();
    }

    private void ManageReservations()
    {
        reservationTable.UpdateReservations(Time.deltaTime);
    }

    private void HandleEvents()
    {
        // Event processing if needed
    }

    // Public methods for adding/removing rooms
    public void AddRoom(Room room)
    {
        gridManager.AddRoom(room);
        pathfinder.Initialize(gridManager.GetAllRooms());
    }

    public void RemoveRoom(int roomId)
    {
        gridManager.RemoveRoom(roomId);
        pathfinder.Initialize(gridManager.GetAllRooms());
    }

    public void UpdateTile(int roomId, int x, int y, TileType newType)
    {
        gridManager.UpdateTile(roomId, x, y, newType);
    }
}
}