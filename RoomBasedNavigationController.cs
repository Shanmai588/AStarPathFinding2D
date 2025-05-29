using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class RoomBasedNavigationController : MonoBehaviour
    {
        [Header("System Configuration")] [SerializeField]
        private int maxRequestsPerFrame = 5;

        [SerializeField] private float reservationTimeStep = 0.1f;
        [SerializeField] private bool enableDebugVisualization = false;

        // Core Systems
        private GridManager gridManager;
        private PathRequestManager pathRequestManager;
        private ReservationTable reservationTable;
        private EventBus eventBus;
        private ReachabilityAnalyzer reachabilityAnalyzer;
        private HierarchicalPathfinder pathfinder;

        // Agent Management
        private List<Agent> registeredAgents = new List<Agent>();
        private List<Path> activePaths = new List<Path>();

        public GridManager GridManager { get => gridManager; }
        public static RoomBasedNavigationController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSystems();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Initialize with some default rooms for testing
            CreateDefaultRooms();
        }

        private void Update()
        {
            CoordinatePathfinding();
            ManageReservations();
            HandleEvents();
        }

        private void InitializeSystems()
        {
            eventBus = new EventBus();
            gridManager = new GridManager(eventBus);
            reservationTable = new ReservationTable { timeStep = reservationTimeStep };
            reachabilityAnalyzer = new ReachabilityAnalyzer(gridManager);
            pathfinder = new HierarchicalPathfinder();
            pathRequestManager = new PathRequestManager(pathfinder);

            // Build room graph when pathfinder is ready
            pathfinder.SetGridManager(gridManager);
        }

        private void CreateDefaultRooms()
        {
            // Create a simple test room
            var room1 = new Room(0, 20, 20, Vector2.zero);
            var room2 = new Room(1, 15, 15, new Vector2(25, 0));

            // Add some walls for testing
            for (int x = 5; x < 15; x++)
            {
                var tile = room1.GetTile(x, 10);
                if (tile != null)
                {
                    tile.type = TileType.Wall;
                    tile.isWalkable = false;
                }
            }

            // Add a door between rooms
            var door = new Door
            {
                positionInRoom = new Vector2Int(19, 10),
                connectedRoomId = 1,
                connectedPosition = new Vector2Int(0, 10),
                isOpen = true
            };
            room1.doors.Add(door);

            gridManager.AddRoom(room1);
            gridManager.AddRoom(room2);
        }

        public void RequestPath(PathRequest request)
        {
            pathRequestManager.QueueRequest(request);

            // Wrap the original callback to track active paths
            var originalCallback = request.onComplete;
            request.onComplete = (path) =>
            {
                if (path != null && path.isValid)
                {
                    activePaths.Add(path);

                    // Remove path after some time or when agent reaches destination
                    StartCoroutine(RemovePathAfterDelay(path, 30f)); // Remove after 30 seconds
                }

                originalCallback?.Invoke(path);
            };
        }

        private System.Collections.IEnumerator RemovePathAfterDelay(Path path, float delay)
        {
            yield return new WaitForSeconds(delay);
            activePaths.Remove(path);
        }

        public Path GetPath(PathRequest request)
        {
            return pathfinder.FindPath(request);
        }

        public void RegisterAgent(Agent agent)
        {
            if (!registeredAgents.Contains(agent))
            {
                registeredAgents.Add(agent);
            }
        }

        public void UnregisterAgent(Agent agent)
        {
            registeredAgents.Remove(agent);
            reservationTable.ClearReservations(agent.AgentId);
        }

        public Room GetRoom(int roomId)
        {
            return gridManager.GetRoom(roomId);
        }

        public Tile GetTile(int roomId, int x, int y)
        {
            return gridManager.GetTile(roomId, x, y);
        }

        public Dictionary<int, Room> GetAllRooms()
        {
            return gridManager.GetAllRooms();
        }

        public Tile[,] GetTilesByRoom(int roomId)
        {
            var room = GetRoom(roomId);
            return room?.grid;
        }

        public Bounds GetRoomBounds(int roomId)
        {
            var room = GetRoom(roomId);
            if (room != null)
            {
                return new Bounds(
                    room.worldPosition + new Vector2(room.width / 2f, room.height / 2f),
                    new Vector3(room.width, room.height, 1));
            }

            return new Bounds();
        }

        public List<Door> GetAllDoors()
        {
            var doors = new List<Door>();
            foreach (var room in gridManager.GetAllRooms().Values)
            {
                doors.AddRange(room.doors);
            }

            return doors;
        }

        public List<Door> GetDoorsInRoom(int roomId)
        {
            var room = GetRoom(roomId);
            return room?.doors ?? new List<Door>();
        }

        public List<Reservation> GetActiveReservations()
        {
            return reservationTable.GetActiveReservations();
        }

        public List<Reservation> GetActiveReservationsInRoom(int roomId)
        {
            return reservationTable.GetActiveReservationsInRoom(roomId);
        }

        public List<Path> GetActivePaths()
        {
            return activePaths;
        }

        public List<Agent> GetRegisteredAgents()
        {
            return registeredAgents;
        }

        public bool IsPositionWalkable(int roomId, Vector2Int position)
        {
            var tile = GetTile(roomId, position.x, position.y);
            return tile != null && tile.isWalkable;
        }

        public Vector2Int WorldToGrid(Vector2 worldPos, out int roomId)
        {
            roomId = 0;

            foreach (var room in gridManager.GetAllRooms().Values)
            {
                var localPos = worldPos - room.worldPosition;
                if (localPos.x >= 0 && localPos.x < room.width &&
                    localPos.y >= 0 && localPos.y < room.height)
                {
                    roomId = room.roomId;
                    return new Vector2Int(Mathf.FloorToInt(localPos.x), Mathf.FloorToInt(localPos.y));
                }
            }

            return Vector2Int.zero;
        }

        public Vector2 GridToWorld(int roomId, Vector2Int gridPos)
        {
            var room = GetRoom(roomId);
            if (room != null)
            {
                return room.GridToWorld(gridPos);
            }

            return Vector2.zero;
        }

        public Vector2Int FindClosestReachablePoint(Vector2Int start, Vector2Int target, int roomId,
            MovementCapabilities capabilities)
        {
            return reachabilityAnalyzer.FindClosestReachablePoint(start, target, roomId, capabilities);
        }

        public HierarchicalPathfinder GetPathfinder()
        {
            return pathfinder;
        }

        public GridManager GetGridManager()
        {
            return gridManager;
        }

        public EventBus GetEventBus()
        {
            return eventBus;
        }

        public ReservationTable GetReservationTable()
        {
            return reservationTable;
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
            // Process any pending events
        }

        // Editor/Debug Methods
        public void UpdateTile(int roomId, int x, int y, TileType newType)
        {
            gridManager.UpdateTile(roomId, x, y, newType);
        }

        private void OnDrawGizmos()
        {
            if (!enableDebugVisualization) return;

            if (gridManager != null)
            {
                DrawRoomBounds();
                DrawDoors();
            }
        }

        private void DrawRoomBounds()
        {
            Gizmos.color = Color.blue;
            foreach (var room in gridManager.GetAllRooms().Values)
            {
                var bounds = GetRoomBounds(room.roomId);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }

        private void DrawDoors()
        {
            Gizmos.color = Color.green;
            foreach (var door in GetAllDoors())
            {
                var room = GetRoom(0); // This needs proper room lookup
                if (room != null)
                {
                    var worldPos = room.GridToWorld(door.positionInRoom);
                    Gizmos.DrawWireSphere(worldPos, 0.5f);
                }
            }
        }
    }
}