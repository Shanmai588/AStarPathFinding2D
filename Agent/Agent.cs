using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Agent : MonoBehaviour
{
    private int agentId;
    private Vector2Int currentPosition;
    private int currentRoomId;
    private AgentType type;
    private float moveSpeed = 5f;
    private Path currentPath;
    private ICostProvider costProvider;
    private MovementCapabilities movementCapabilities;
    private PathRequestManager pathRequestManager;
    private GridManager gridManager;
    private float moveTimer = 0f;

    public int AgentId => agentId;
    public Vector2Int CurrentPosition => currentPosition;
    public int CurrentRoomId => currentRoomId;
    public AgentType Type => type;
    
    // Expose path for RTSUnitMover
    public Path CurrentPath => currentPath;
    public bool HasPath => currentPath != null && currentPath.IsValid;

    void Awake()
    {
        agentId = GetInstanceID();
        gridManager = FindObjectOfType<GridManager>();
        pathRequestManager = Singleton<PathRequestManager>.Instance;
        InitializeMovementCapabilities();
    }

    void Start()
    {
        // Set default cost provider based on agent type
        switch (type)
        {
            case AgentType.Infantry:
                costProvider = new TerrainAwareCostProvider();
                break;
            case AgentType.Vehicle:
                costProvider = new StandardCostProvider();
                break;
            case AgentType.Flying:
                costProvider = new StandardCostProvider();
                break;
            case AgentType.Naval:
                costProvider = new TerrainAwareCostProvider();
                break;
        }
        
        // Initialize position from transform
        currentPosition = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );
    }

    private void InitializeMovementCapabilities()
    {
        movementCapabilities = new MovementCapabilities();
        
        switch (type)
        {
            case AgentType.Infantry:
                movementCapabilities.CanFly = false;
                movementCapabilities.CanSwim = false;
                movementCapabilities.Size = 1f;
                movementCapabilities.AllowedTerrain = new List<TileType>
                {
                    TileType.Ground, TileType.Road, TileType.Forest
                };
                break;
                
            case AgentType.Vehicle:
                movementCapabilities.CanFly = false;
                movementCapabilities.CanSwim = false;
                movementCapabilities.Size = 2f;
                movementCapabilities.AllowedTerrain = new List<TileType>
                {
                    TileType.Ground, TileType.Road
                };
                break;
                
            case AgentType.Flying:
                movementCapabilities.CanFly = true;
                movementCapabilities.CanSwim = false;
                movementCapabilities.Size = 1f;
                movementCapabilities.AllowedTerrain = new List<TileType>
                {
                    TileType.Ground, TileType.Road, TileType.Forest, 
                    TileType.Water, TileType.Mountain
                };
                break;
                
            case AgentType.Naval:
                movementCapabilities.CanFly = false;
                movementCapabilities.CanSwim = true;
                movementCapabilities.Size = 3f;
                movementCapabilities.AllowedTerrain = new List<TileType>
                {
                    TileType.Water
                };
                break;
        }
    }

    public void RequestPath(Vector2Int target, int targetRoom)
    {
        var request = new PathRequest
        {
            AgentId = agentId,
            StartPos = currentPosition,
            EndPos = target,
            StartRoomId = currentRoomId,
            EndRoomId = targetRoom,
            CostProvider = costProvider,
            OnComplete = OnPathComplete,
            Priority = RequestPriority.Normal
        };

        if (pathRequestManager != null)
        {
            pathRequestManager.QueueRequest(request);
        }
        else
        {
            Debug.LogError("PathRequestManager not found!");
        }
    }

    private void OnPathComplete(Path path)
    {
        if (path != null && path.IsValid)
        {
            currentPath = path;
            Debug.Log($"Agent {agentId} received valid path with {path.Waypoints.Count} waypoints");
        }
        else
        {
            if (path != null)
                Debug.Log($"Agent {agentId} received valid path");
            else
                Debug.Log($"Agent {agentId} received invalid path");
            currentPath = null;
        }
    }

    public void UpdateMovement(float deltaTime)
    {
        if (currentPath == null || !currentPath.IsValid)
            return;

        if (currentPath.IsComplete(currentPosition))
        {
            currentPath = null;
            return;
        }

        moveTimer += deltaTime;
        
        if (moveTimer >= 1f / moveSpeed)
        {
            moveTimer = 0f;
            
            var nextPos = currentPath.GetNextWaypoint(currentPosition);
            if (nextPos != currentPosition)
            {
                // Check if position is available
                var tile = gridManager.GetTile(currentRoomId, nextPos.x, nextPos.y);
                if (tile != null && !tile.IsOccupied() && tile.IsWalkable)
                {
                    // Clear old position
                    var oldTile = gridManager.GetTile(currentRoomId, currentPosition.x, currentPosition.y);
                    oldTile?.SetOccupant(null);
                    
                    // Move to new position
                    currentPosition = nextPos;
                    tile.SetOccupant(this);
                    
                    // Update visual position
                    transform.position = new Vector3(nextPos.x, nextPos.y, 0);
                }
                else
                {
                    // Path blocked, request new path
                    var target = currentPath.GetWaypoints()[currentPath.GetWaypoints().Count - 1];
                    RequestPath(target, currentRoomId);
                }
            }
        }
    }

    public MovementCapabilities GetMovementCapabilities()
    {
        return movementCapabilities;
    }
    
    public void SetPosition(Vector2Int position, int roomId)
    {
        currentPosition = position;
        currentRoomId = roomId;
    }
    
    public void ClearPath()
    {
        currentPath = null;
    }
}

    public class MovementCapabilities
    {
        public bool CanFly { get; set; }
        public bool CanSwim { get; set; }
        public float Size { get; set; }
        public List<TileType> AllowedTerrain { get; set; } = new();
    }
}