using System.Collections.Generic;
using UnityEngine;

namespace RTS.Pathfinding
{
    public class Agent : MonoBehaviour
    {
        private ICostProvider costProvider;
        private Path currentPath;
        private Vector2Int currentPosition;
        private GridManager gridManager;
        private MovementCapabilities movementCapabilities;
        private readonly float moveSpeed = 5f;
        private float moveTimer;
        private PathRequestManager pathRequestManager;

        public int AgentId { get; private set; }

        public Vector2Int CurrentPosition => currentPosition;
        public int CurrentRoomId { get; }

        public AgentType Type { get; }

        private void Awake()
        {
            AgentId = GetInstanceID();
            gridManager = FindObjectOfType<GridManager>();
            pathRequestManager = Singleton<PathRequestManager>.Instance;
            InitializeMovementCapabilities();
        }

        private void Start()
        {
            // Set default cost provider based on agent type
            switch (Type)
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
        }

        private void InitializeMovementCapabilities()
        {
            movementCapabilities = new MovementCapabilities();

            switch (Type)
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
                AgentId = AgentId,
                StartPos = currentPosition,
                EndPos = target,
                StartRoomId = CurrentRoomId,
                EndRoomId = targetRoom,
                CostProvider = costProvider,
                OnComplete = OnPathComplete,
                Priority = RequestPriority.Normal
            };

            pathRequestManager.QueueRequest(request);
        }

        private void OnPathComplete(Path path)
        {
            if (path != null && path.IsValid) currentPath = path;
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
                    var tile = gridManager.GetTile(CurrentRoomId, nextPos.x, nextPos.y);
                    if (tile != null && !tile.IsOccupied() && tile.IsWalkable)
                    {
                        // Clear old position
                        var oldTile = gridManager.GetTile(CurrentRoomId, currentPosition.x, currentPosition.y);
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
                        RequestPath(target, CurrentRoomId);
                    }
                }
            }
        }

        public MovementCapabilities GetMovementCapabilities()
        {
            return movementCapabilities;
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